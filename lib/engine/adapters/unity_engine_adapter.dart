import 'dart:async';

import 'package:on_device_3d_builder/core/logging/app_logger.dart';
import 'package:on_device_3d_builder/engine/contract/engine_event.dart';
import 'package:on_device_3d_builder/engine/contract/render_engine.dart';
import 'package:on_device_3d_builder/engine/protocol/command_envelope.dart';
import 'package:on_device_3d_builder/engine/protocol/event_envelope.dart';
import 'package:on_device_3d_builder/engine/protocol/protocol_constants.dart';

/// Skeleton adapter for the Unity rendering engine.
///
/// This adapter implements [RenderEngine] and will eventually bind to
/// a `MethodChannel` / `EventChannel` to communicate with the Unity
/// runtime embedded as a platform view.
///
/// **Current status: SKELETON — not connected to Unity.**
/// All methods throw [UnimplementedError] until the platform channel
/// binding is implemented in Level 2 Phase 2.
///
/// The class demonstrates usage of [CommandEnvelope] and [EventEnvelope]
/// to show how commands will be sent and events will be parsed.
class UnityEngineAdapter implements RenderEngine {
  final AppLogger _logger;

  // TODO(unity): Replace with MethodChannel('on_device_3d_builder/engine')
  // static const _channel = MethodChannel('on_device_3d_builder/engine');

  // TODO(unity): Replace with EventChannel('on_device_3d_builder/events')
  // static const _eventChannel = EventChannel('on_device_3d_builder/events');

  final StreamController<EngineEvent> _eventController =
      StreamController<EngineEvent>.broadcast();

  UnityEngineAdapter(this._logger);

  @override
  Stream<EngineEvent> get events => _eventController.stream;

  @override
  Future<void> initialize() async {
    final envelope = CommandEnvelope.create(EngineCommand.initialize);
    _logger.info('UnityAdapter: Sending command -> ${envelope.command.value} '
        '[${envelope.requestId}]');

    // TODO(unity): Send via MethodChannel
    // final result = await _channel.invokeMethod('sendCommand', envelope.toMap());

    throw UnimplementedError(
      'UnityEngineAdapter.initialize() is not yet connected to Unity. '
      'Command envelope prepared: $envelope',
    );
  }

  @override
  Future<void> loadScene(String sceneJson) async {
    final envelope = CommandEnvelope.create(
      EngineCommand.loadScene,
      payload: {'scene_json': sceneJson},
    );
    _logger.info('UnityAdapter: Sending command -> ${envelope.command.value} '
        '[${envelope.requestId}]');

    // TODO(unity): Send via MethodChannel
    throw UnimplementedError(
      'UnityEngineAdapter.loadScene() is not yet connected to Unity. '
      'Command envelope prepared: $envelope',
    );
  }

  @override
  Future<void> clearScene() async {
    final envelope = CommandEnvelope.create(EngineCommand.clearScene);
    _logger.info('UnityAdapter: Sending command -> ${envelope.command.value} '
        '[${envelope.requestId}]');

    // TODO(unity): Send via MethodChannel
    throw UnimplementedError(
      'UnityEngineAdapter.clearScene() is not yet connected to Unity. '
      'Command envelope prepared: $envelope',
    );
  }

  @override
  Future<void> dispose() async {
    final envelope = CommandEnvelope.create(EngineCommand.dispose);
    _logger.info('UnityAdapter: Sending command -> ${envelope.command.value} '
        '[${envelope.requestId}]');

    await _eventController.close();

    // TODO(unity): Send via MethodChannel, then clean up native resources
    throw UnimplementedError(
      'UnityEngineAdapter.dispose() is not yet connected to Unity. '
      'Command envelope prepared: $envelope',
    );
  }

  // ---------------------------------------------------------------------------
  // Event Handling (future: called by EventChannel listener)
  // ---------------------------------------------------------------------------

  /// Parses a raw event map from Unity and emits it as an [EngineEvent].
  ///
  /// This method will be called by the `EventChannel` listener once
  /// Unity integration is active. It validates the wire format using
  /// [EventEnvelope.fromMap] and converts it to an [EngineEvent].
  void handleRawEvent(Map<String, dynamic> rawEvent) {
    final envelope = EventEnvelope.fromMap(rawEvent); // validates strictly
    _logger.info('UnityAdapter: Received event -> ${envelope.event.value} '
        '[${envelope.requestId}]');

    if (!_eventController.isClosed) {
      _eventController.add(EngineEvent(
        type: envelope.event.value,
        payload: envelope.payload,
      ));
    }
  }
}
