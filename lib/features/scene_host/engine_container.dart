import 'package:flutter/material.dart';

/// A placeholder container for the future Unity rendering widget.
///
/// This widget occupies the rendering area of the screen without
/// containing any rendering logic. Once a native engine (e.g., Unity)
/// is integrated, this widget will be replaced with the engine's
/// platform view.
class EngineContainer extends StatelessWidget {
  /// The current lifecycle state label to display.
  final String stateLabel;

  /// The last engine event type to display.
  final String? lastEvent;

  const EngineContainer({
    super.key,
    required this.stateLabel,
    this.lastEvent,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.black87,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: Colors.white24),
      ),
      child: Stack(
        children: [
          // Placeholder for the future engine view.
          const Center(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(Icons.threed_rotation, size: 72, color: Colors.white24),
                SizedBox(height: 12),
                Text(
                  'Rendering Engine Placeholder',
                  style: TextStyle(color: Colors.white30, fontSize: 14),
                ),
                SizedBox(height: 4),
                Text(
                  'Unity view will mount here',
                  style: TextStyle(color: Colors.white24, fontSize: 12),
                ),
              ],
            ),
          ),
          // Overlay: lifecycle state badge.
          Positioned(
            top: 12,
            left: 12,
            child: _StateBadge(label: stateLabel),
          ),
          // Overlay: last engine event badge.
          if (lastEvent != null)
            Positioned(
              top: 12,
              right: 12,
              child: _EventBadge(event: lastEvent!),
            ),
        ],
      ),
    );
  }
}

class _StateBadge extends StatelessWidget {
  final String label;

  const _StateBadge({required this.label});

  Color get _color {
    switch (label) {
      case 'ready':
        return Colors.green;
      case 'rendering':
        return Colors.blue;
      case 'initializing':
        return Colors.orange;
      case 'error':
        return Colors.red;
      case 'disposed':
        return Colors.grey;
      default:
        return Colors.white38;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: _color.withAlpha(40),
        border: Border.all(color: _color.withAlpha(120)),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 8,
            height: 8,
            decoration: BoxDecoration(color: _color, shape: BoxShape.circle),
          ),
          const SizedBox(width: 6),
          Text(
            label.toUpperCase(),
            style: TextStyle(
              color: _color,
              fontSize: 11,
              fontWeight: FontWeight.bold,
              letterSpacing: 0.8,
            ),
          ),
        ],
      ),
    );
  }
}

class _EventBadge extends StatelessWidget {
  final String event;

  const _EventBadge({required this.event});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: Colors.white10,
        border: Border.all(color: Colors.white24),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        'EVT: $event',
        style: const TextStyle(
          color: Colors.white54,
          fontSize: 11,
          letterSpacing: 0.6,
        ),
      ),
    );
  }
}
