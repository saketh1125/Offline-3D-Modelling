import 'package:flutter/material.dart';
import 'package:on_device_3d_builder/controllers/scene_controller.dart';
import 'package:on_device_3d_builder/ui/pages/unity_viewer_page.dart';

/// Page 1 — Scene Editor
///
/// Pure Flutter page with the JSON editor and a "Run" button.
/// Unity is NOT loaded here — it only starts when navigating to the viewer.
class SceneEditorPage extends StatefulWidget {
  final SceneController sceneController;

  const SceneEditorPage({super.key, required this.sceneController});

  @override
  State<SceneEditorPage> createState() => _SceneEditorPageState();
}

class _SceneEditorPageState extends State<SceneEditorPage> {
  final TextEditingController _jsonController = TextEditingController(
    text: '''{
  "schema_version": "1.0",
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

  void _onRunPressed() {
    FocusScope.of(context).unfocus();

    final json = _jsonController.text.trim();
    if (json.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Please enter scene JSON before running.'),
          backgroundColor: Colors.redAccent,
        ),
      );
      return;
    }

    // Navigate to the Unity viewer page
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (_) => UnityViewerPage(
          sceneJson: json,
          controller: widget.sceneController,
        ),
      ),
    );
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
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Title
            const Text(
              'Procedural Scene JSON',
              style: TextStyle(
                color: Colors.white,
                fontSize: 18,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 4),
            const Text(
              'Edit your scene definition below, then tap Run to view it in the Unity engine.',
              style: TextStyle(color: Colors.white38, fontSize: 13),
            ),
            const SizedBox(height: 16),

            // JSON Editor
            Expanded(
              child: Container(
                decoration: BoxDecoration(
                  color: const Color(0xFF1A1A22),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: Colors.white12),
                ),
                padding: const EdgeInsets.all(12),
                child: Scrollbar(
                  child: SingleChildScrollView(
                    child: TextField(
                      controller: _jsonController,
                      keyboardType: TextInputType.multiline,
                      maxLines: null,
                      style: const TextStyle(
                        color: Colors.greenAccent,
                        fontFamily: 'monospace',
                        fontSize: 14,
                        height: 1.5,
                      ),
                      decoration: const InputDecoration(
                        border: InputBorder.none,
                        hintText: 'Paste scene JSON here...',
                        hintStyle: TextStyle(color: Colors.white30),
                      ),
                    ),
                  ),
                ),
              ),
            ),

            const SizedBox(height: 16),

            // Run Button
            SizedBox(
              height: 56,
              child: ElevatedButton.icon(
                onPressed: _onRunPressed,
                icon: const Icon(Icons.play_arrow_rounded, size: 28),
                label: const Text('Run Scene'),
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFF5B6EF5),
                  foregroundColor: Colors.white,
                  textStyle: const TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(14),
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
