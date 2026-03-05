import 'dart:async';
import 'dart:convert';

import 'package:on_device_3d_builder/core/logging/app_logger.dart';
import 'package:on_device_3d_builder/engine/contract/engine_event.dart';
import 'package:on_device_3d_builder/engine/contract/render_engine.dart';

/// A mock implementation of [RenderEngine] for development and testing.
///
/// Simulates async initialization and scene loading with artificial delays.
/// Emits [EngineEvent]s via a broadcast [StreamController] to mimic
/// real engine behavior without any Unity or native dependency.
class MockEngineAdapter implements RenderEngine {
  final AppLogger _logger;
  final StreamController<EngineEvent> _eventController =
      StreamController<EngineEvent>.broadcast();

  bool _isInitialized = false;
  bool _isDisposed = false;

  MockEngineAdapter(this._logger);

  @override
  Stream<EngineEvent> get events => _eventController.stream;

  @override
  Future<void> initialize() async {
    _assertNotDisposed();
    _logger.info('MockEngine: Initializing...');

    // Simulate async initialization delay.
    await Future<void>.delayed(const Duration(milliseconds: 500));

    _isInitialized = true;
    _logger.info('MockEngine: Initialization complete.');
    _emit('engine_initialized');
  }

  @override
  Future<void> loadScene(String sceneJson) async {
    _assertNotDisposed();
    _assertInitialized();

    _logger.info('MockEngine: Loading scene...');

    // Validate that the input is parseable JSON.
    Map<String, dynamic> parsedScene;
    try {
      parsedScene = jsonDecode(sceneJson) as Map<String, dynamic>;
    } catch (e) {
      _logger.error('MockEngine: Invalid scene JSON', e);
      _emit('error', payload: {
        'code': 'INVALID_SCENE_JSON',
        'message': 'Failed to parse scene JSON: $e',
      });
      return;
    }

    // Simulate async scene loading delay.
    await Future<void>.delayed(const Duration(milliseconds: 300));

    _logger.info(
        'MockEngine: Scene loaded with ${parsedScene.length} top-level keys.');
    _emit('scene_ready', payload: {
      'keyCount': parsedScene.length,
    });
  }

  @override
  Future<void> clearScene() async {
    _assertNotDisposed();
    _assertInitialized();

    _logger.info('MockEngine: Clearing scene.');

    await Future<void>.delayed(const Duration(milliseconds: 100));

    _emit('scene_cleared');
  }

  @override
  Future<void> dispose() async {
    if (_isDisposed) return;

    _logger.info('MockEngine: Disposing...');
    _isDisposed = true;
    _isInitialized = false;

    _emit('engine_disposed');
    await _eventController.close();

    _logger.info('MockEngine: Disposed.');
  }

  // --- Private Helpers ---

  void _emit(String type, {Map<String, dynamic>? payload}) {
    if (!_eventController.isClosed) {
      _eventController.add(EngineEvent(type: type, payload: payload));
    }
  }

  void _assertInitialized() {
    if (!_isInitialized) {
      throw StateError('MockEngine: Engine is not initialized.');
    }
  }

  void _assertNotDisposed() {
    if (_isDisposed) {
      throw StateError('MockEngine: Engine has been disposed.');
    }
  }
}
