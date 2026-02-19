import 'dart:convert';

/// Provides a minimal, schema-compliant test scene JSON for development.
///
/// The scene conforms to Scene Schema v1.0 and can be used directly
/// with [SceneValidator.validateStrict] and the rendering pipeline.
class TestSceneFixture {
  TestSceneFixture._(); // Prevent instantiation.

  /// Returns a minimal valid Scene Schema v1.0 JSON string.
  static String minimalScene() {
    final scene = {
      'schema_version': '1.0',
      'engine_target': 'mock',
      'metadata': {
        'name': 'Test Scene',
        'author': 'On-Device 3D Builder',
        'created_at': DateTime.now().toIso8601String(),
      },
      'scene_environment': {
        'background': 'solid_color',
        'background_color': [0.1, 0.1, 0.15],
      },
      'camera': {
        'type': 'perspective',
        'position': [0.0, 2.0, 5.0],
        'target': [0.0, 0.0, 0.0],
        'fov': 60.0,
      },
      'lighting': [
        {
          'type': 'directional',
          'direction': [1.0, -1.0, -1.0],
          'intensity': 1.0,
        }
      ],
      'materials': [
        {
          'id': 'mat_default',
          'name': 'Default Material',
          'base_color': [0.8, 0.6, 0.2],
          'roughness': 0.5,
          'metallic': 0.0,
        }
      ],
      'objects': [
        {
          'id': 'obj_cube_01',
          'name': 'Main Cube',
          'geometry': {
            'primitive': 'cube',
            'scale': [1.0, 1.0, 1.0],
          },
          'transform': {
            'position': [0.0, 0.0, 0.0],
            'rotation': [0.0, 0.0, 0.0],
          },
          'material_ref': 'mat_default',
        }
      ],
    };

    return jsonEncode(scene);
  }
}
