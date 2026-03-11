import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/gestures.dart';
import 'package:flutter_unity_widget/flutter_unity_widget.dart';
import 'package:on_device_3d_builder/controllers/scene_controller.dart';

class UnityViewport extends StatelessWidget {
  final SceneController controller;

  const UnityViewport({super.key, required this.controller});

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.black,
        border: Border.all(color: Colors.white12),
        borderRadius: BorderRadius.circular(12),
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(12),
        child: UnityWidget(
          onUnityCreated: controller.onUnityCreated,
          onUnityMessage: controller.onUnityMessage,
          onUnitySceneLoaded: controller.onUnitySceneLoaded,
          useAndroidViewSurface: true, // Recommended for complex 3D rendering
          borderRadius: const BorderRadius.all(Radius.circular(12)),
          // CRITICAL: Allow touch events to pass through to Unity
          gestureRecognizers: const <Factory<OneSequenceGestureRecognizer>>{
            Factory<OneSequenceGestureRecognizer>(EagerGestureRecognizer.new),
          },
        ),
      ),
    );
  }
}
