import 'package:flutter/material.dart';
import 'package:on_device_3d_builder/engine/lifecycle/engine_lifecycle_manager.dart';
import 'package:on_device_3d_builder/engine/orchestrator/rendering_orchestrator.dart';
import 'package:on_device_3d_builder/features/scene_host/engine_container.dart';
import 'package:on_device_3d_builder/scene/fixtures/test_scene_fixture.dart';

/// Root screen for the rendering host framework.
///
/// Displays the engine lifecycle state, last engine event, and allows
/// the user to trigger mock scene loading. All calls are routed through
/// the [RenderingOrchestrator] — never directly to the engine.
class SceneHostScreen extends StatefulWidget {
  /// The orchestrator that drives the rendering pipeline.
  final RenderingOrchestrator orchestrator;

  const SceneHostScreen({super.key, required this.orchestrator});

  @override
  State<SceneHostScreen> createState() => _SceneHostScreenState();
}

class _SceneHostScreenState extends State<SceneHostScreen> {
  final ValueNotifier<EngineState> _stateNotifier =
      ValueNotifier(EngineState.uninitialized);
  final ValueNotifier<String?> _lastEventNotifier = ValueNotifier(null);
  final ValueNotifier<String?> _errorNotifier = ValueNotifier(null);

  bool _isWorking = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _initializeEngine());
  }

  @override
  void dispose() {
    // Note: The orchestrator is injected and NOT owned by this widget.
    // Disposal of the orchestrator is the responsibility of the caller
    // (e.g., main.dart or a parent lifecycle owner), not this screen.
    _stateNotifier.dispose();
    _lastEventNotifier.dispose();
    _errorNotifier.dispose();
    super.dispose();
  }

  // ---------------------------------------------------------------------------
  // Engine Operations (all routed through orchestrator)
  // ---------------------------------------------------------------------------

  Future<void> _initializeEngine() async {
    // Subscribe to engine events relayed through the orchestrator.
    widget.orchestrator.events.listen((event) {
      if (!mounted) return;
      _lastEventNotifier.value = event.type;
      _stateNotifier.value = widget.orchestrator.currentState;
    });

    _setWorking(true);
    try {
      await widget.orchestrator.initialize();
      _syncState();
    } catch (e) {
      _setError('Initialization failed: $e');
    } finally {
      _setWorking(false);
    }
  }

  Future<void> _loadTestScene() async {
    _errorNotifier.value = null;
    _setWorking(true);
    try {
      final json = TestSceneFixture.minimalScene();
      await widget.orchestrator.loadScene(json);
      _syncState();
    } catch (e) {
      _setError('$e');
      _syncState();
    } finally {
      _setWorking(false);
    }
  }

  Future<void> _clearScene() async {
    _errorNotifier.value = null;
    _setWorking(true);
    try {
      await widget.orchestrator.clearScene();
      _syncState();
    } catch (e) {
      _setError('Clear failed: $e');
    } finally {
      _setWorking(false);
    }
  }

  // ---------------------------------------------------------------------------
  // Helpers
  // ---------------------------------------------------------------------------

  void _syncState() {
    if (!mounted) return;
    _stateNotifier.value = widget.orchestrator.currentState;
  }

  void _setWorking(bool working) {
    if (!mounted) return;
    setState(() => _isWorking = working);
  }

  void _setError(String message) {
    if (!mounted) return;
    _errorNotifier.value = message;
  }

  // ---------------------------------------------------------------------------
  // Build
  // ---------------------------------------------------------------------------

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0F0F13),
      appBar: AppBar(
        backgroundColor: const Color(0xFF1A1A22),
        title: const Text(
          'Scene Host',
          style: TextStyle(color: Colors.white, fontWeight: FontWeight.w600),
        ),
        centerTitle: false,
        elevation: 0,
      ),
      body: Column(
        children: [
          // Engine container area.
          Expanded(
            flex: 3,
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: ValueListenableBuilder<EngineState>(
                valueListenable: _stateNotifier,
                builder: (_, state, __) {
                  return ValueListenableBuilder<String?>(
                    valueListenable: _lastEventNotifier,
                    builder: (_, lastEvent, __) {
                      return EngineContainer(
                        stateLabel: state.name,
                        lastEvent: lastEvent,
                      );
                    },
                  );
                },
              ),
            ),
          ),

          // Status & controls panel.
          Expanded(
            flex: 2,
            child: _ControlPanel(
              stateNotifier: _stateNotifier,
              lastEventNotifier: _lastEventNotifier,
              errorNotifier: _errorNotifier,
              isWorking: _isWorking,
              onLoadScene: _loadTestScene,
              onClearScene: _clearScene,
            ),
          ),
        ],
      ),
    );
  }
}

// ---------------------------------------------------------------------------
// Control Panel Widget
// ---------------------------------------------------------------------------

class _ControlPanel extends StatelessWidget {
  final ValueNotifier<EngineState> stateNotifier;
  final ValueNotifier<String?> lastEventNotifier;
  final ValueNotifier<String?> errorNotifier;
  final bool isWorking;
  final VoidCallback onLoadScene;
  final VoidCallback onClearScene;

  const _ControlPanel({
    required this.stateNotifier,
    required this.lastEventNotifier,
    required this.errorNotifier,
    required this.isWorking,
    required this.onLoadScene,
    required this.onClearScene,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: const BoxDecoration(
        color: Color(0xFF1A1A22),
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
      ),
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Info rows.
          ValueListenableBuilder<EngineState>(
            valueListenable: stateNotifier,
            builder: (_, state, __) => _InfoRow(
              label: 'Engine State',
              value: state.name.toUpperCase(),
              valueColor: _stateColor(state),
            ),
          ),
          const SizedBox(height: 8),
          ValueListenableBuilder<String?>(
            valueListenable: lastEventNotifier,
            builder: (_, event, __) => _InfoRow(
              label: 'Last Event',
              value: event ?? '—',
              valueColor: Colors.white70,
            ),
          ),
          const SizedBox(height: 8),
          ValueListenableBuilder<String?>(
            valueListenable: errorNotifier,
            builder: (_, error, __) {
              if (error == null) return const SizedBox.shrink();
              return Container(
                margin: const EdgeInsets.only(bottom: 8),
                padding:
                    const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                decoration: BoxDecoration(
                  color: Colors.red.withAlpha(25),
                  border: Border.all(color: Colors.red.withAlpha(80)),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  error,
                  style: const TextStyle(color: Colors.redAccent, fontSize: 12),
                ),
              );
            },
          ),

          const Spacer(),

          // Action buttons.
          Row(
            children: [
              Expanded(
                child: FilledButton.icon(
                  onPressed: isWorking ? null : onLoadScene,
                  icon: isWorking
                      ? const SizedBox(
                          width: 14,
                          height: 14,
                          child: CircularProgressIndicator(
                              strokeWidth: 2, color: Colors.white),
                        )
                      : const Icon(Icons.play_arrow, size: 18),
                  label: Text(isWorking ? 'Working…' : 'Load Test Scene'),
                  style: FilledButton.styleFrom(
                    backgroundColor: const Color(0xFF5B6EF5),
                    padding: const EdgeInsets.symmetric(vertical: 14),
                  ),
                ),
              ),
              const SizedBox(width: 12),
              OutlinedButton.icon(
                onPressed: isWorking ? null : onClearScene,
                icon: const Icon(Icons.clear, size: 18),
                label: const Text('Clear'),
                style: OutlinedButton.styleFrom(
                  foregroundColor: Colors.white70,
                  side: const BorderSide(color: Colors.white24),
                  padding:
                      const EdgeInsets.symmetric(vertical: 14, horizontal: 16),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Color _stateColor(EngineState state) {
    switch (state) {
      case EngineState.ready:
        return Colors.greenAccent;
      case EngineState.rendering:
        return Colors.blueAccent;
      case EngineState.initializing:
        return Colors.orangeAccent;
      case EngineState.error:
        return Colors.redAccent;
      case EngineState.disposed:
        return Colors.grey;
      case EngineState.uninitialized:
        return Colors.white38;
    }
  }
}

class _InfoRow extends StatelessWidget {
  final String label;
  final String value;
  final Color valueColor;

  const _InfoRow({
    required this.label,
    required this.value,
    required this.valueColor,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Text(
          '$label: ',
          style: const TextStyle(color: Colors.white38, fontSize: 13),
        ),
        Text(
          value,
          style: TextStyle(
            color: valueColor,
            fontSize: 13,
            fontWeight: FontWeight.w600,
          ),
        ),
      ],
    );
  }
}
