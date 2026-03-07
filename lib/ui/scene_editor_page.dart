import 'package:flutter/material.dart';
import 'package:on_device_3d_builder/controllers/scene_controller.dart';
import 'package:on_device_3d_builder/features/scene_host/engine_container.dart';

class SceneEditorPage extends StatefulWidget {
  final SceneController sceneController;

  const SceneEditorPage({super.key, required this.sceneController});

  @override
  State<SceneEditorPage> createState() => _SceneEditorPageState();
}

class _SceneEditorPageState extends State<SceneEditorPage> {
  final TextEditingController _jsonController = TextEditingController(
    text: '''{
  "materials":[
    {"id":"red","baseColor":[1,0,0]}
  ],
  "objects":[
    {
      "id":"cube1",
      "primitive":"cube",
      "materialRef":"red",
      "transform":{
        "position":[0,0,0],
        "rotation":[0,0,0],
        "scale":[2,2,2]
      }
    }
  ]
}''',
  );

  @override
  void dispose() {
    _jsonController.dispose();
    super.dispose();
  }

  void _onGeneratePressed() {
    FocusScope.of(context).unfocus(); // Dismiss keyboard if open
    widget.sceneController.generateScene(_jsonController.text);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0F0F13),
      appBar: AppBar(
        title: const Text('Procedural Generator'),
        backgroundColor: const Color(0xFF1A1A22),
        elevation: 0,
      ),
      body: Row(
        children: [
          // Left Side: Editor Area
          Expanded(
            flex: 1,
            child: Padding(
              padding: const EdgeInsets.all(16.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const Text(
                    "Procedural Scene JSON",
                    style: TextStyle(
                      color: Colors.white,
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  const SizedBox(height: 12),
                  // Editor Text Field
                  Expanded(
                    child: Container(
                      decoration: BoxDecoration(
                        color: const Color(0xFF1A1A22),
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(color: Colors.white24),
                      ),
                      padding: const EdgeInsets.all(12),
                      child: TextField(
                        controller: _jsonController,
                        maxLines: null,
                        expands: true,
                        style: const TextStyle(
                          color: Colors.greenAccent,
                          fontFamily: 'monospace',
                          fontSize: 14,
                        ),
                        decoration: const InputDecoration(
                          border: InputBorder.none,
                          hintText: 'Paste scene JSON here...',
                          hintStyle: TextStyle(color: Colors.white30),
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),
                  ValueListenableBuilder<bool>(
                    valueListenable: widget.sceneController.isReady,
                    builder: (context, isReady, child) {
                      return ElevatedButton.icon(
                        // Disable when engine is not initialized to prevent crashing
                        onPressed: isReady ? _onGeneratePressed : null,
                        icon: const Icon(Icons.code),
                        label: Text(isReady
                            ? 'Generate Scene'
                            : 'Waiting for Unity Engine...'),
                        style: ElevatedButton.styleFrom(
                          backgroundColor: const Color(0xFF5B6EF5),
                          disabledBackgroundColor: const Color(0xFF5B6EF5)
                              .withAlpha(128), // Faded out
                          foregroundColor: Colors.white,
                          disabledForegroundColor: Colors.white70,
                          padding: const EdgeInsets.symmetric(vertical: 16),
                          textStyle: const TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      );
                    },
                  ),
                ],
              ),
            ),
          ),

          const VerticalDivider(width: 1, color: Colors.white24),

          // Right Side: Unity Engine Container
          const Expanded(
            flex: 1,
            child: Padding(
              padding: EdgeInsets.all(16.0),
              child: EngineContainer(stateLabel: 'READY', lastEvent: null),
            ),
          ),
        ],
      ),
    );
  }
}
