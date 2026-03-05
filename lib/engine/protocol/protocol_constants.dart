/// Protocol version and type-safe enums for the Flutter ↔ Unity wire format.
///
/// All commands sent to Unity and events received from Unity must use
/// these constant values. Unknown commands or events must be rejected.
class ProtocolVersion {
  ProtocolVersion._();

  /// The current protocol version used for all envelopes.
  static const String v1 = '1.0';

  /// The set of all valid protocol versions.
  static const Set<String> supported = {v1};
}

/// Commands sent from Flutter → Unity.
enum EngineCommand {
  initialize('initialize'),
  loadScene('load_scene'),
  clearScene('clear_scene'),
  dispose('dispose');

  const EngineCommand(this.value);

  /// The wire-format string sent over the platform channel.
  final String value;

  /// Looks up a command by its wire value. Returns null if unknown.
  static EngineCommand? fromValue(String value) {
    for (final cmd in values) {
      if (cmd.value == value) return cmd;
    }
    return null;
  }
}

/// Events received from Unity → Flutter.
enum EngineEventType {
  initialized('initialized'),
  sceneLoading('scene_loading'),
  sceneReady('scene_ready'),
  error('error'),
  performanceStats('performance_stats');

  const EngineEventType(this.value);

  /// The wire-format string received from the platform channel.
  final String value;

  /// Looks up an event type by its wire value. Returns null if unknown.
  static EngineEventType? fromValue(String value) {
    for (final evt in values) {
      if (evt.value == value) return evt;
    }
    return null;
  }
}
