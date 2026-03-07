import 'package:flutter/foundation.dart';
import 'package:on_device_3d_builder/engine/contract/render_engine.dart';

/// Controller coordinating UI interactions with the RenderEngine bridge.
class SceneController {
  final RenderEngine engine;
  final ValueNotifier<bool> isReady = ValueNotifier(false);

  SceneController({required this.engine}) {
    engine.events.listen((event) {
      if (event.type == 'initialized') {
        debugPrint('[DIAG] Unity initialized event detected — unlocking UI');
        isReady.value = true;
      }
    });
  }

  /// Sends raw JSON payload directly to the Unity runtime.
  Future<void> generateScene(String json) async {
    await engine.loadScene(json);
  }
}
