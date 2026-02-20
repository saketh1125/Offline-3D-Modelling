import 'package:on_device_3d_builder/engine/protocol/protocol_constants.dart';

/// An immutable command envelope sent from Flutter → Unity.
///
/// Every command carries a [protocolVersion], a typed [command],
/// a unique [requestId] for correlation, and an optional [payload].
///
/// ```dart
/// final envelope = CommandEnvelope.create(
///   EngineCommand.loadScene,
///   payload: {'scene_json': jsonString},
/// );
/// final map = envelope.toMap(); // send over MethodChannel
/// ```
class CommandEnvelope {
  /// Protocol version tag (must match Unity-side contract).
  final String protocolVersion;

  /// The command type.
  final EngineCommand command;

  /// Unique request ID for request ↔ response correlation.
  final String requestId;

  /// Optional payload data for the command.
  final Map<String, dynamic>? payload;

  const CommandEnvelope._({
    required this.protocolVersion,
    required this.command,
    required this.requestId,
    this.payload,
  });

  /// Creates a new command envelope with auto-generated [requestId].
  ///
  /// Uses [ProtocolVersion.v1] as the default protocol version.
  /// The [requestId] is generated from a monotonically increasing counter
  /// prefixed with the command name for easy log tracing.
  factory CommandEnvelope.create(
    EngineCommand command, {
    Map<String, dynamic>? payload,
  }) {
    return CommandEnvelope._(
      protocolVersion: ProtocolVersion.v1,
      command: command,
      requestId: _generateRequestId(command),
      payload: payload,
    );
  }

  /// Serializes this envelope to a map suitable for platform channel transport.
  Map<String, dynamic> toMap() {
    return {
      'protocol_version': protocolVersion,
      'command': command.value,
      'request_id': requestId,
      if (payload != null) 'payload': payload,
    };
  }

  @override
  String toString() =>
      'CommandEnvelope(v=$protocolVersion, cmd=${command.value}, '
      'id=$requestId, payload=$payload)';

  // ---------------------------------------------------------------------------
  // Private
  // ---------------------------------------------------------------------------

  /// Counter for unique request ID generation.
  static int _counter = 0;

  /// Generates a deterministic, traceable request ID.
  static String _generateRequestId(EngineCommand command) {
    _counter++;
    return '${command.value}-$_counter-${DateTime.now().millisecondsSinceEpoch}';
  }
}
