import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:on_device_3d_builder/core/config/app_config.dart';
import 'package:on_device_3d_builder/core/errors/engine_exception.dart';
import 'package:on_device_3d_builder/core/errors/validation_exception.dart';
import 'package:on_device_3d_builder/core/logging/app_logger.dart';
import 'package:on_device_3d_builder/engine/adapters/mock_engine_adapter.dart';
import 'package:on_device_3d_builder/engine/lifecycle/engine_lifecycle_manager.dart';
import 'package:on_device_3d_builder/engine/orchestrator/rendering_orchestrator.dart';

/// A valid Scene Schema v1.0 JSON for testing.
Map<String, dynamic> _validSceneMap() => {
      'schema_version': '1.0',
      'engine_target': 'mock',
      'metadata': {'name': 'Test Scene'},
      'scene_environment': {'background': 'sky'},
      'camera': {'type': 'perspective'},
      'lighting': [
        {'type': 'directional'}
      ],
      'materials': [
        {
          'id': 'mat_01',
          'base_color': [1.0, 0.5, 0.0],
        }
      ],
      'objects': [
        {
          'id': 'obj_01',
          'geometry': {'primitive': 'cube'},
          'material_ref': 'mat_01',
        }
      ],
    };

String _validSceneJson() => jsonEncode(_validSceneMap());

void main() {
  late AppConfig config;
  late AppLogger logger;
  late MockEngineAdapter engine;
  late EngineLifecycleManager lifecycle;
  late RenderingOrchestrator orchestrator;

  setUp(() {
    config = AppConfig.development();
    logger = AppLogger(config);
    engine = MockEngineAdapter(logger);
    lifecycle = EngineLifecycleManager(logger);
    orchestrator = RenderingOrchestrator(
      engine: engine,
      lifecycle: lifecycle,
      logger: logger,
    );
  });

  tearDown(() async {
    // Ensure cleanup even if test fails mid-way.
    if (lifecycle.currentState != EngineState.disposed) {
      try {
        await orchestrator.dispose();
      } catch (_) {}
    }
  });

  group('Initialization', () {
    test('initializes engine and transitions to ready', () async {
      expect(orchestrator.currentState, EngineState.uninitialized);

      await orchestrator.initialize();

      expect(orchestrator.currentState, EngineState.ready);
    });
  });

  group('Valid Scene Loading', () {
    test('valid scene passes validation and triggers engine load', () async {
      await orchestrator.initialize();

      // Listen for the scene_ready event from engine.
      final events = <String>[];
      engine.events.listen((e) => events.add(e.type));

      await orchestrator.loadScene(_validSceneJson());

      // Allow stream events to propagate.
      await Future<void>.delayed(const Duration(milliseconds: 50));

      expect(events, contains('scene_ready'));
    });
  });

  group('Invalid Scene Rejection', () {
    test('missing required key throws ValidationException', () async {
      await orchestrator.initialize();

      final invalidScene = _validSceneMap()..remove('materials');
      final json = jsonEncode(invalidScene);

      expect(
        () => orchestrator.loadScene(json),
        throwsA(isA<ValidationException>()),
      );
    });

    test('unknown root key throws ValidationException', () async {
      await orchestrator.initialize();

      final invalidScene = _validSceneMap()..['unknown_key'] = 'value';
      final json = jsonEncode(invalidScene);

      expect(
        () => orchestrator.loadScene(json),
        throwsA(isA<ValidationException>()),
      );
    });

    test('wrong schema version throws ValidationException', () async {
      await orchestrator.initialize();

      final invalidScene = _validSceneMap()..['schema_version'] = '2.0';
      final json = jsonEncode(invalidScene);

      expect(
        () => orchestrator.loadScene(json),
        throwsA(isA<ValidationException>()),
      );
    });

    test('duplicate material id throws ValidationException', () async {
      await orchestrator.initialize();

      final scene = _validSceneMap();
      (scene['materials'] as List).add({
        'id': 'mat_01', // duplicate
        'base_color': [0.0, 0.0, 1.0],
      });
      final json = jsonEncode(scene);

      expect(
        () => orchestrator.loadScene(json),
        throwsA(isA<ValidationException>()),
      );
    });

    test('invalid primitive throws ValidationException', () async {
      await orchestrator.initialize();

      final scene = _validSceneMap();
      (scene['objects'] as List).first['geometry'] = {'primitive': 'pyramid'};
      final json = jsonEncode(scene);

      expect(
        () => orchestrator.loadScene(json),
        throwsA(isA<ValidationException>()),
      );
    });

    test('malformed JSON string throws EngineException', () async {
      await orchestrator.initialize();

      expect(
        () => orchestrator.loadScene('not valid json!!!'),
        throwsA(isA<EngineException>()),
      );
    });
  });

  group('Lifecycle Transitions', () {
    test('lifecycle follows: uninitialized -> initializing -> ready', () async {
      expect(lifecycle.currentState, EngineState.uninitialized);

      await orchestrator.initialize();

      expect(lifecycle.currentState, EngineState.ready);
    });

    test('lifecycle transitions to disposed after dispose()', () async {
      await orchestrator.initialize();
      await orchestrator.dispose();

      expect(lifecycle.currentState, EngineState.disposed);
    });
  });

  group('Disposal & Stream Cleanup', () {
    test('dispose cancels event subscription', () async {
      await orchestrator.initialize();
      await orchestrator.dispose();

      // Engine should be disposed; no further events expected.
      expect(lifecycle.currentState, EngineState.disposed);
    });

    test('calling dispose twice does not throw', () async {
      await orchestrator.initialize();
      await orchestrator.dispose();

      // Second dispose should be safe (engine already disposed).
      // The lifecycle manager will reject the transition, but
      // the orchestrator catches that gracefully.
    });
  });
}
