import 'dart:async';
import 'dart:convert';
import 'package:flutter/services.dart';
import 'package:on_device_3d_builder/core/diagnostics/diagnostics_logger.dart';
import 'package:on_device_3d_builder/core/logging/app_logger.dart';
import 'package:on_device_3d_builder/engine/contract/engine_event.dart';
import 'package:on_device_3d_builder/engine/contract/render_engine.dart';
import 'package:on_device_3d_builder/engine/protocol/command_envelope.dart';
import 'package:on_device_3d_builder/engine/protocol/event_envelope.dart';
import 'package:on_device_3d_builder/engine/protocol/protocol_constants.dart';

/// Production adapter that connects to the Unity rendering engine
/// via a [MethodChannel] bridge.
///
/// **Flutter → Unity**: Commands are serialized to JSON and sent via
/// `sendCommand` on the MethodChannel. The Android `MainActivity`
/// forwards them to `UnityPlayer.UnitySendMessage`.
///
/// **Unity → Flutter**: Unity calls the static `onUnityEvent` method
/// on `MainActivity`, which invokes `onUnityEvent` on the MethodChannel.
/// This adapter listens for those invocations, deserializes the JSON
/// into an [EventEnvelope], and emits an [EngineEvent] on [events].
class UnityEngineAdapter implements RenderEngine {
  static const String _channelName = 'com.sankalp.unity.bridge';

  final AppLogger _logger;
  final MethodChannel _channel;

  final StreamController<EngineEvent> _eventController =
      StreamController<EngineEvent>.broadcast();

  bool _disposed = false;
  bool _unityReady = false;

  UnityEngineAdapter(this._logger)
      : _channel = const MethodChannel(_channelName) {
    // Listen for Unity → Flutter event callbacks.
    _channel.setMethodCallHandler(_handleMethodCall);
    _logger.info('UnityAdapter: MethodChannel "$_channelName" initialized.');
  }

  // ---------------------------------------------------------------------------
  // RenderEngine contract
  // ---------------------------------------------------------------------------

  @override
  Stream<EngineEvent> get events => _eventController.stream;

  @override
  Future<void> initialize() async {
    _guardDisposed('initialize');
    
    // Initialize file-based diagnostics logic
    await FlutterDiagnosticsLogger().initialize();
    FlutterDiagnosticsLogger().log("============== SESSION START ==============");

    _logger.info(
        "UnityAdapter: Waiting 500ms for MethodChannel native registration...");
   
    // Added safety buffer ensuring Kotlin MainActivity mounts its native listeners completely.
    await Future.delayed(const Duration(milliseconds: 500));
    
    await _sendCommand(EngineCommand.initialize);
  }

  @override
  Future<void> loadScene(String sceneJson) async {
    _guardDisposed('loadScene');
    await _sendCommand(
      EngineCommand.loadScene,
      payload: {'scene_json': sceneJson},
    );
  }

  @override
  Future<void> clearScene() async {
    _guardDisposed('clearScene');
    await _sendCommand(EngineCommand.clearScene);
  }

  @override
  Future<void> dispose() async {
    if (_disposed) {
      _logger.warning('UnityAdapter: Already disposed, ignoring.');
      return;
    }
    _disposed = true;

    // Send dispose command to Unity (best-effort).
    try {
      await _sendCommand(EngineCommand.dispose);
    } catch (e) {
      _logger.error('UnityAdapter: Error sending dispose command: $e');
    }

    // Tear down the method call handler and event stream.
    _channel.setMethodCallHandler(null);
    await _eventController.close();
    _logger.info('UnityAdapter: Disposed.');
  }

  // ---------------------------------------------------------------------------
  // Flutter → Unity: command sending
  // ---------------------------------------------------------------------------

  /// Serializes a [CommandEnvelope] to JSON and sends it to Unity
  /// via the MethodChannel's `sendCommand` method.
  Future<void> _sendCommand(
    EngineCommand command, {
    Map<String, dynamic>? payload,
  }) async {
    if (!_unityReady && command != EngineCommand.initialize) {
      throw StateError("Unity runtime not ready yet");
    }

    final envelope = CommandEnvelope.create(command, payload: payload);
    final json = jsonEncode(envelope.toMap());

    _logger.info(
      'UnityAdapter: Sending ${command.value} [${envelope.requestId}]',
    );

    try {
      await _channel.invokeMethod<void>('sendCommand', {'json': json});
    } on PlatformException catch (e) {
      _logger.error(
        'UnityAdapter: PlatformException sending ${command.value}: '
        '${e.code} — ${e.message}',
      );
      rethrow;
    } on MissingPluginException {
      _logger.error(
        'UnityAdapter: No handler registered for sendCommand. '
        'Is the Android bridge configured?',
      );
      rethrow;
    }
  }

  // ---------------------------------------------------------------------------
  // Unity → Flutter: event receiving
  // ---------------------------------------------------------------------------

  /// Handles incoming method calls from the Android MethodChannel.
  ///
  /// Only `onUnityEvent` is expected. All other method names are ignored
  /// with a [MissingPluginException] returned to the platform.
  Future<dynamic> _handleMethodCall(MethodCall call) async {
    _logger.info("RAW UNITY EVENT: ${call.arguments}");
    FlutterDiagnosticsLogger().log("Received Platform Call: ${call.method}");
    if (call.arguments != null) {
      FlutterDiagnosticsLogger().log("Payload String: ${call.arguments.toString()}");
    }

    if (call.method != 'onUnityEvent') {
      _logger.warning(
        'UnityAdapter: Unknown method invoked from platform: ${call.method}',
      );
      throw MissingPluginException(
        'UnityEngineAdapter does not handle method: ${call.method}',
      );
    }

    final rawJson = call.arguments;
    if (rawJson == null || rawJson is! String || rawJson.isEmpty) {
      _logger.warning(
        'UnityAdapter: Received onUnityEvent with null/empty argument.',
      );
      return;
    }

    try {
      final map = jsonDecode(rawJson) as Map<String, dynamic>;
      final envelope = EventEnvelope.fromMap(map);

      _logger.info(
        '[DIAG] UnityAdapter: Received ${envelope.event.value} '
        '[${envelope.requestId}]',
      );

      if (envelope.event.value == "initialized") {
        _unityReady = true;
        _logger.info("Unity runtime ready");
      }

      if (!_eventController.isClosed) {
        _eventController.add(EngineEvent(
          type: envelope.event.value,
          payload: envelope.payload,
        ));
      }
    } catch (e) {
      _logger.error('UnityAdapter: Failed to parse Unity event: $e');

      // Emit a generic error event so the orchestrator can react.
      if (!_eventController.isClosed) {
        _eventController.add(EngineEvent(
          type: EngineEventType.error.value,
          payload: {
            'code': 'EVENT_PARSE_FAILED',
            'message': e.toString(),
            'raw': rawJson,
          },
        ));
      }
    }
  }

  // ---------------------------------------------------------------------------
  // Guards
  // ---------------------------------------------------------------------------

  void _guardDisposed(String method) {
    if (_disposed) {
      throw StateError(
        'UnityEngineAdapter.$method() called after dispose.',
      );
    }
  }
}
