import 'package:on_device_3d_builder/core/logging/app_logger.dart';

/// Represents the possible states of the rendering engine.
enum EngineState {
  /// Engine has not been initialized yet.
  uninitialized,

  /// Engine is currently initializing.
  initializing,

  /// Engine is initialized and ready to accept scenes.
  ready,

  /// Engine is actively loading or rendering a scene.
  rendering,

  /// Engine encountered an error. Recovery is possible via re-initialization.
  error,

  /// Engine resources have been released. Terminal state.
  disposed,
}

/// Tracks and manages engine state transitions.
///
/// This class is the single source of truth for the current engine state.
/// It enforces valid state transitions, logs all changes for diagnostics,
/// and exposes a [reset] method to return the engine to [EngineState.uninitialized]
/// for graceful error recovery.
class EngineLifecycleManager {
  final AppLogger _logger;

  EngineState _currentState = EngineState.uninitialized;

  EngineLifecycleManager(this._logger);

  /// The current state of the engine.
  EngineState get currentState => _currentState;

  /// Returns true if the engine is in a terminal, non-operational state.
  bool get isDisposed => _currentState == EngineState.disposed;

  /// Returns true if the engine is currently busy (loading or initializing).
  bool get isBusy =>
      _currentState == EngineState.rendering ||
      _currentState == EngineState.initializing;

  /// Transitions the engine to [newState].
  ///
  /// Throws [StateError] if the transition is not permitted by the state machine.
  /// Logs all valid transitions for debugging.
  void transitionTo(EngineState newState) {
    if (!_isValidTransition(_currentState, newState)) {
      final message =
          'Invalid engine state transition: $_currentState -> $newState';
      _logger.error(message);
      throw StateError(message);
    }

    _logger.info('Engine state: $_currentState -> $newState');
    _currentState = newState;
  }

  /// Resets the lifecycle back to [EngineState.uninitialized] for recovery.
  ///
  /// Only valid from [EngineState.error]. Allows the orchestrator to
  /// re-initialize the engine after a recoverable failure.
  void reset() {
    if (_currentState != EngineState.error) {
      throw StateError(
        'Lifecycle reset is only valid from error state. '
        'Current state: $_currentState',
      );
    }
    _logger.info('Engine state: error -> uninitialized (recovery reset)');
    _currentState = EngineState.uninitialized;
  }

  // ---------------------------------------------------------------------------
  // Private: Transition Table
  // ---------------------------------------------------------------------------

  /// Defines the full set of permitted state transitions.
  bool _isValidTransition(EngineState from, EngineState to) {
    switch (from) {
      case EngineState.uninitialized:
        // Can only begin initialization.
        return to == EngineState.initializing;

      case EngineState.initializing:
        // Initialization succeeds → ready; fails → error.
        return to == EngineState.ready || to == EngineState.error;

      case EngineState.ready:
        // From ready: begin loading, encounter error, or shut down.
        return to == EngineState.rendering ||
            to == EngineState.disposed ||
            to == EngineState.error;

      case EngineState.rendering:
        // Loading completes → ready; fails → error; or disposed mid-render.
        return to == EngineState.ready ||
            to == EngineState.error ||
            to == EngineState.disposed;

      case EngineState.error:
        // Error: can only dispose. Recovery uses reset() which bypasses
        // the transition table to return to uninitialized.
        return to == EngineState.disposed;

      case EngineState.disposed:
        // Terminal state — no further transitions permitted.
        return false;
    }
  }
}
