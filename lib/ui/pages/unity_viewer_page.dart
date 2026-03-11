import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/gestures.dart';
import 'package:flutter_unity_widget/flutter_unity_widget.dart';
import 'package:on_device_3d_builder/controllers/scene_controller.dart';

/// Page 2 — Unity Runtime Viewer
///
/// This page hosts the Unity PlatformView in isolation from the editor.
/// Unity only starts when this page is pushed onto the navigation stack,
/// preventing widget-tree rebuilds from crashing the native runtime.
class UnityViewerPage extends StatefulWidget {
  final String sceneJson;
  final SceneController controller;

  const UnityViewerPage({
    super.key,
    required this.sceneJson,
    required this.controller,
  });

  @override
  State<UnityViewerPage> createState() => _UnityViewerPageState();
}

class _UnityViewerPageState extends State<UnityViewerPage> {
  bool _isLoading = true;
  String _statusText = 'Initializing Unity Engine...';
  bool _hasError = false;

  @override
  void initState() {
    super.initState();
    // Listen for ready state changes
    widget.controller.isReady.addListener(_onReadyChanged);
  }

  @override
  void dispose() {
    widget.controller.isReady.removeListener(_onReadyChanged);
    super.dispose();
  }

  void _onReadyChanged() {
    if (widget.controller.isReady.value && _isLoading) {
      // Engine is ready, send the scene
      _sendScene();
    }
  }

  void _onUnityCreated(UnityWidgetController controller) {
    debugPrint('[VIEWER] Unity widget created — initializing.');
    widget.controller.onUnityCreated(controller);
    // If already ready (fast init), send scene immediately
    if (widget.controller.isReady.value) {
      _sendScene();
    }
  }

  void _onUnityMessage(dynamic message) {
    widget.controller.onUnityMessage(message);
    final msgStr = message.toString();

    if (msgStr.contains('scene_ready')) {
      setState(() {
        _isLoading = false;
        _statusText = 'Scene loaded';
      });
    } else if (msgStr.contains('SCENE_BUILD_FAILED') ||
        msgStr.contains('"event":"error"')) {
      setState(() {
        _isLoading = false;
        _hasError = true;
        _statusText = 'Scene build failed';
      });
    } else if (msgStr.contains('scene_loading')) {
      setState(() {
        _statusText = 'Building scene...';
      });
    }
  }

  void _sendScene() {
    debugPrint('[VIEWER] Sending scene JSON to Unity.');
    setState(() {
      _statusText = 'Sending scene to Unity...';
    });
    widget.controller.generateScene(widget.sceneJson);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0F0F13),
      appBar: AppBar(
        title: const Text('Scene Viewer'),
        backgroundColor: const Color(0xFF1A1A22),
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => Navigator.of(context).pop(),
        ),
        actions: [
          if (_hasError)
            IconButton(
              icon: const Icon(Icons.refresh, color: Colors.orangeAccent),
              onPressed: _sendScene,
              tooltip: 'Retry',
            ),
        ],
      ),
      body: Stack(
        children: [
          // Unity viewport — fills the entire body
          Positioned.fill(
            child: UnityWidget(
              onUnityCreated: _onUnityCreated,
              onUnityMessage: _onUnityMessage,
              onUnitySceneLoaded: widget.controller.onUnitySceneLoaded,
              useAndroidViewSurface: true,
              // CRITICAL: Allow touch events to pass through to Unity
              gestureRecognizers: const <Factory<OneSequenceGestureRecognizer>>{
                Factory<OneSequenceGestureRecognizer>(
                    EagerGestureRecognizer.new),
              },
            ),
          ),

          // Loading overlay
          if (_isLoading)
            Positioned.fill(
              child: Container(
                color: const Color(0xCC0F0F13),
                child: Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const CircularProgressIndicator(
                        color: Color(0xFF5B6EF5),
                        strokeWidth: 3,
                      ),
                      const SizedBox(height: 20),
                      Text(
                        _statusText,
                        style: const TextStyle(
                          color: Colors.white70,
                          fontSize: 16,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),

          // Error overlay
          if (_hasError && !_isLoading)
            Positioned(
              bottom: 80,
              left: 16,
              right: 16,
              child: Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                decoration: BoxDecoration(
                  color: Colors.red.withAlpha(40),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: Colors.redAccent.withAlpha(100)),
                ),
                child: Row(
                  children: [
                    const Icon(Icons.error_outline,
                        color: Colors.redAccent, size: 20),
                    const SizedBox(width: 12),
                    const Expanded(
                      child: Text(
                        'Scene build failed. Check your JSON and retry.',
                        style: TextStyle(color: Colors.redAccent, fontSize: 13),
                      ),
                    ),
                    TextButton(
                      onPressed: () {
                        setState(() {
                          _hasError = false;
                          _isLoading = true;
                        });
                        _sendScene();
                      },
                      child: const Text('Retry',
                          style: TextStyle(color: Colors.white)),
                    ),
                  ],
                ),
              ),
            ),
        ],
      ),
    );
  }
}
