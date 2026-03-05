import 'dart:async';
import 'dart:convert';

import 'package:on_device_3d_builder/core/errors/engine_exception.dart';
import 'package:on_device_3d_builder/core/errors/validation_exception.dart';
import 'package:on_device_3d_builder/core/logging/app_logger.dart';
import 'package:on_device_3d_builder/engine/contract/engine_event.dart';
import 'package:on_device_3d_builder/engine/contract/render_engine.dart';
import 'package:on_device_3d_builder/engine/lifecycle/engine_lifecycle_manager.dart';
import 'package:on_device_3d_builder/scene/validators/scene_validator.dart';
export 'package:on_device_3d_builder/engine/contract/engine_event.dart';

/// Bridges scene validation with engine invocation.
///
/// The orchestrator is the single entry point for all rendering operations.
/// It enforces defensive guards on every public method:
///
/// - **Post-disposal guard**: throws [EngineException] if called after [dispose].
/// - **Concurrent-load guard**: throws [EngineException] if [loadScene] is called
///   while already in the `rendering` state.
/// - **Error recovery**: [reinitialize] resets the lifecycle and re-runs
///   engine initialization without requiring a new orchestrator instance.
/// - **Stream safety**: the internal event relay is closed only during [dispose],
///   and all add calls check `isClosed` before writing.
class RenderingOrchestrator {
  final RenderEngine _engine;
  final EngineLifecycleManager _lifecycle;
  final AppLogger _logger;

  StreamSubscription<EngineEvent>? _eventSubscription;
  bool _isDisposed = false;

  /// A broadcast stream of [EngineEvent]s re-emitted by the orchestrator.
  ///
  /// UI layers should subscribe to this instead of the engine directly,
  /// preserving separation of concerns.
  Stream<EngineEvent> get events => _eventRelay.stream;
  final StreamController<EngineEvent> _eventRelay =
      StreamController<EngineEvent>.broadcast();

  RenderingOrchestrator({
    required RenderEngine engine,
    required EngineLifecycleManager lifecycle,
    required AppLogger logger,
  })  : _engine = engine,
        _lifecycle = lifecycle,
        _logger = logger;

  /// The current engine state, delegated to [EngineLifecycleManager].
  EngineState get currentState => _lifecycle.currentState;

  // ---------------------------------------------------------------------------
  // Initialization
  // ---------------------------------------------------------------------------

  /// Initializes the rendering engine.
  ///
  /// Transitions: `uninitialized → initializing → ready`.
  /// Subscribes to engine events. Throws [EngineException] on failure.
  Future<void> initialize() async {
    _guardDisposed('initialize');
    _logger.info('Orchestrator: Initializing engine...');

    try {
      _lifecycle.transitionTo(EngineState.initializing);
      _startListeningToEvents();
      await _engine.initialize();
      _lifecycle.transitionTo(EngineState.ready);
      _logger.info('Orchestrator: Engine initialized successfully.');
    } catch (e, st) {
      if (e is EngineException) rethrow;
      _handleFatalError(
          'Engine initialization failed.', 'ENGINE_INIT_FAILED', e, st);
    }
  }

  // ---------------------------------------------------------------------------
  // Error Recovery
  // ---------------------------------------------------------------------------

  /// Re-initializes the engine after a recoverable error.
  ///
  /// Valid only when [currentState] is [EngineState.error].
  /// Resets the lifecycle via [EngineLifecycleManager.reset] and
  /// re-runs the full initialization flow.
  Future<void> reinitialize() async {
    _guardDisposed('reinitialize');

    if (_lifecycle.currentState != EngineState.error) {
      throw EngineException(
        message: 'reinitialize() is only valid from error state. '
            'Current state: ${_lifecycle.currentState}',
        code: 'INVALID_RECOVERY_STATE',
      );
    }

    _logger.info(
        'Orchestrator: Attempting error recovery via re-initialization...');

    // Cancel the stale event subscription before resetting.
    await _eventSubscription?.cancel();
    _eventSubscription = null;

    // Reset lifecycle to uninitialized for recovery.
    _lifecycle.reset();

    // Re-run the full initialization flow.
    await initialize();
  }

  // ---------------------------------------------------------------------------
  // Scene Loading
  // ---------------------------------------------------------------------------

  /// Validates and loads a scene into the engine.
  ///
  /// **Guards:**
  /// - Throws [EngineException] (`LOAD_ALREADY_IN_PROGRESS`) if the engine
  ///   is already in the `rendering` state.
  /// - Throws [EngineException] if disposed.
  ///
  /// **Validation:**
  /// - Parses [sceneJson] into a `Map<String, dynamic>`.
  /// - Runs [SceneValidator.validateStrict] — throws [ValidationException] on failure.
  ///
  /// **Lifecycle:**
  /// - Transitions: `ready → rendering`.
  /// - Transitions back to `ready` via the `scene_ready` engine event.
  Future<void> loadScene(String sceneJson) async {
    _guardDisposed('loadScene');

    // Guard: reject duplicate / concurrent load.
    if (_lifecycle.currentState == EngineState.rendering) {
      const message =
          'Orchestrator: loadScene rejected — engine is already rendering.';
      _logger.warning(message);
      throw EngineException(
        message: 'Cannot load scene while engine is already rendering.',
        code: 'LOAD_ALREADY_IN_PROGRESS',
        metadata: {'currentState': _lifecycle.currentState.name},
      );
    }

    _logger.info('Orchestrator: Preparing to load scene...');

    // Step 1: Parse raw JSON string.
    final Map<String, dynamic> parsedScene;
    try {
      final decoded = jsonDecode(sceneJson);
      if (decoded is! Map<String, dynamic>) {
        throw const FormatException('Scene JSON must be a JSON object.');
      }
      parsedScene = decoded;
    } catch (e) {
      _logger.error('Orchestrator: Scene JSON parsing failed.', e);
      throw EngineException(
        message: 'Invalid scene JSON format.',
        code: 'INVALID_SCENE_JSON',
        originalError: e,
      );
    }

    // Step 2: Strict schema validation (throws ValidationException on failure).
    _logger.info('Orchestrator: Running strict schema validation...');
    SceneValidator.validateStrict(parsedScene);
    _logger.info('Orchestrator: Schema validation passed.');

    // Step 3: Engine invocation.
    try {
      _lifecycle.transitionTo(EngineState.rendering);
      await _engine.loadScene(sceneJson);
      // Lifecycle transitions back to `ready` via the `scene_ready` engine
      // event handled in _onEngineEvent.
      _logger.info('Orchestrator: Scene submitted to engine.');
    } catch (e, st) {
      if (e is EngineException) rethrow;
      _handleFatalError('Scene loading failed.', 'SCENE_LOAD_FAILED', e, st);
    }
  }

  // ---------------------------------------------------------------------------
  // Scene Clearing
  // ---------------------------------------------------------------------------

  /// Clears the current scene from the engine.
  Future<void> clearScene() async {
    _guardDisposed('clearScene');
    _logger.info('Orchestrator: Clearing scene...');
    try {
      await _engine.clearScene();
      _logger.info('Orchestrator: Scene cleared.');
    } catch (e, st) {
      _logger.error('Orchestrator: Failed to clear scene.', e, st);
      throw EngineException(
        message: 'Failed to clear scene.',
        code: 'SCENE_CLEAR_FAILED',
        originalError: e,
      );
    }
  }

  // ---------------------------------------------------------------------------
  // Disposal
  // ---------------------------------------------------------------------------

  /// Disposes the engine and releases all resources.
  ///
  /// **Idempotent**: safe to call multiple times.
  /// Cancels the event stream subscription, closes the relay stream,
  /// and transitions the lifecycle to `disposed`.
  Future<void> dispose() async {
    if (_isDisposed) {
      _logger
          .warning('Orchestrator: dispose() called more than once. Ignoring.');
      return;
    }

    _isDisposed = true;
    _logger.info('Orchestrator: Disposing...');

    await _eventSubscription?.cancel();
    _eventSubscription = null;

    if (!_eventRelay.isClosed) {
      await _eventRelay.close();
    }

    try {
      _lifecycle.transitionTo(EngineState.disposed);
      await _engine.dispose();
      _logger.info('Orchestrator: Disposed.');
    } catch (e, st) {
      _logger.error('Orchestrator: Error during disposal.', e, st);
    }
  }

  // ---------------------------------------------------------------------------
  // Private: Event Handling
  // ---------------------------------------------------------------------------

  /// Subscribes to the engine's event stream and relays through [events].
  void _startListeningToEvents() {
    _eventSubscription = _engine.events.listen(
      (event) {
        _onEngineEvent(event);
        if (!_eventRelay.isClosed) _eventRelay.add(event);
      },
      onError: (Object error) {
        _logger.error(
            'Orchestrator: Engine event stream error.', error);
        _tryTransitionTo(EngineState.error);
      },
    );
  }

  /// Routes engine events to lifecycle transitions.
  void _onEngineEvent(EngineEvent event) {
    _logger.info('Orchestrator: Engine event -> ${event.type}');

    switch (event.type) {
      case 'engine_initialized':
        // Handled by the synchronous flow in initialize(); no action needed here.
        break;

      case 'scene_ready':
        _tryTransitionTo(EngineState.ready);
        break;

      case 'scene_cleared':
        _tryTransitionTo(EngineState.ready);
        break;

      case 'error':
        _logger.error(
          'Orchestrator: Engine error event received.',
          event.payload,
        );
        _tryTransitionTo(EngineState.error);
        break;

      case 'engine_disposed':
        // Disposal is handled in dispose(); no action needed.
        break;

      default:
        _logger.info(
            'Orchestrator: Unhandled event type: ${event.type}');
    }
  }

  // ---------------------------------------------------------------------------
  // Private: Helpers
  // ---------------------------------------------------------------------------

  /// Guards all public methods against post-disposal calls.
  ///
  /// Throws [EngineException] with code `ORCHESTRATOR_DISPOSED` if disposed.
  void _guardDisposed(String operation) {
    if (_isDisposed) {
      final message = 'Cannot call $operation() on a disposed orchestrator.';
      _logger.error('Orchestrator: $message');
      throw EngineException(
        message: message,
        code: 'ORCHESTRATOR_DISPOSED',
        metadata: {'operation': operation},
      );
    }
  }

  /// Attempts a lifecycle transition, logging any [StateError] without throwing.
  void _tryTransitionTo(EngineState state) {
    try {
      _lifecycle.transitionTo(state);
    } on StateError catch (e) {
      _logger.warning(
          'Orchestrator: Could not transition to $state: $e');
    }
  }

  /// Handles unrecoverable errors by transitioning to error state and throwing.
  Never _handleFatalError(
      String message, String code, Object error, StackTrace? st) {
    _tryTransitionTo(EngineState.error);
    _logger.error('Orchestrator: $message', error, st);
    throw EngineException(
      message: message,
      code: code,
      originalError: error,
    );
  }
}
