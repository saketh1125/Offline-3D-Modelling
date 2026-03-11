import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:flutter_unity_widget/flutter_unity_widget.dart';

/// Controller coordinating UI interactions with the flutter_unity_widget bridge.
class SceneController {
  UnityWidgetController? _unityWidgetController;
  final ValueNotifier<bool> isReady = ValueNotifier(false);

  /// Called when UnityWidget is fully created natively and the IPC channel
  /// is open. Mark ready immediately — Unity's `unity_ready` message is sent
  /// during Start() before this channel listener exists, so we can't rely on it.
  void onUnityCreated(UnityWidgetController controller) {
    _unityWidgetController = controller;
    debugPrint(
        '[FLUTTER-DIAG] UnityWidgetController attached — sending initialize.');
    _sendInitialize();
    isReady.value = true;
  }

  /// Sends the initialize command to Unity so that _isInitialized becomes true.
  /// This must happen before any load_scene command.
  void _sendInitialize() {
    final initEnvelope = jsonEncode({
      'protocol_version': '1.0',
      'command': 'initialize',
      'request_id': 'init-${DateTime.now().millisecondsSinceEpoch}',
      'payload': '{}',
    });
    debugPrint('[FLUTTER-DIAG] Sending initialize envelope to Unity.');
    _unityWidgetController?.postMessage(
      'RuntimeManager',
      'ReceiveCommand',
      initEnvelope,
    );
  }

  /// Called when Unity emits a message string.
  void onUnityMessage(dynamic message) {
    final msgStr = message.toString();
    debugPrint('[FLUTTER-DIAG] SceneController received event: $msgStr');

    // Unity sends unity-ready during Start() which may arrive before the
    // listener is wired up. We handle it here as a belt-and-suspenders guard.
    if (msgStr.contains('unity-ready') || msgStr.contains('unity_ready')) {
      debugPrint('[DIAG] Unity engine fully booted — isReady confirmed.');
      if (!isReady.value) isReady.value = true;
    }
  }

  void onUnitySceneLoaded(SceneLoaded? sceneInfo) {
    debugPrint(
        '[FLUTTER-DIAG] Received onUnitySceneLoaded: ${sceneInfo?.name}');
  }

  /// Sends scene JSON wrapped in a CommandEnvelope to the Unity RuntimeManager.
  ///
  /// Unity's RuntimeManager validates a strict protocol envelope:
  ///   { "protocol_version": "1.0", "command": "load_scene",
  ///     "request_id": "...", "payload": "<raw-scene-json>" }
  ///
  /// Sending raw scene JSON without this wrapper silently fails validation.
  Future<void> generateScene(String sceneJson) async {
    if (_unityWidgetController == null) {
      debugPrint(
          '[FLUTTER-DIAG] Error: UnityWidgetController is null — cannot send command.');
      return;
    }

    final requestId = 'load_scene-${DateTime.now().millisecondsSinceEpoch}';
    final envelope = jsonEncode({
      'protocol_version': '1.0',
      'command': 'load_scene',
      'request_id': requestId,
      'payload': sceneJson,
    });

    debugPrint('[FLUTTER-DIAG] Sending CommandEnvelope to Unity: $envelope');
    _unityWidgetController?.postMessage(
      'RuntimeManager',
      'ReceiveCommand',
      envelope,
    );
  }

  /// Sends a raw pre-encoded command string to Unity.
  /// Used by the viewer page for camera controls, etc.
  void postRawCommand(String commandJson) {
    debugPrint('[FLUTTER-DIAG] Sending raw command to Unity: $commandJson');
    _unityWidgetController?.postMessage(
      'RuntimeManager',
      'ReceiveCommand',
      commandJson,
    );
  }
}
