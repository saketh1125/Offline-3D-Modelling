import 'package:on_device_3d_builder/core/errors/engine_exception.dart';
import 'package:on_device_3d_builder/engine/protocol/protocol_constants.dart';

/// An immutable event envelope received from Unity → Flutter.
///
/// Every inbound event is parsed and validated via [EventEnvelope.fromMap].
/// Unknown event types and protocol version mismatches are rejected with
/// structured [EngineException]s.
///
/// ```dart
/// final map = await methodChannel.invokeMethod<Map>('getEvent');
/// final envelope = EventEnvelope.fromMap(map!.cast<String, dynamic>());
/// print(envelope.event); // EngineEventType.sceneReady
/// ```
class EventEnvelope {
  /// Protocol version tag from the sender.
  final String protocolVersion;

  /// The parsed event type.
  final EngineEventType event;

  /// The request ID that this event correlates to (may be null for
  /// unsolicited events like `performance_stats`).
  final String? requestId;

  /// Optional payload data from the event.
  final Map<String, dynamic>? payload;

  const EventEnvelope._({
    required this.protocolVersion,
    required this.event,
    this.requestId,
    this.payload,
  });

  /// Parses and validates an incoming event envelope from a raw map.
  ///
  /// **Validations:**
  /// - `protocol_version` must be present and match a supported version.
  /// - `event` must be present and map to a known [EngineEventType].
  ///
  /// Throws [EngineException] with structured codes on failure:
  /// - `MISSING_PROTOCOL_VERSION`
  /// - `UNSUPPORTED_PROTOCOL_VERSION`
  /// - `MISSING_EVENT_TYPE`
  /// - `UNKNOWN_EVENT_TYPE`
  factory EventEnvelope.fromMap(Map<String, dynamic> map) {
    // --- Validate protocol version ---
    final version = map['protocol_version'];
    if (version == null || version is! String) {
      throw EngineException(
        message: 'Event envelope missing required field: protocol_version',
        code: 'MISSING_PROTOCOL_VERSION',
        metadata: {'raw': map},
      );
    }
    if (!ProtocolVersion.supported.contains(version)) {
      throw EngineException(
        message: 'Unsupported protocol version: $version',
        code: 'UNSUPPORTED_PROTOCOL_VERSION',
        metadata: {
          'received': version,
          'supported': ProtocolVersion.supported.toList()
        },
      );
    }

    // --- Validate event type ---
    final eventValue = map['event'];
    if (eventValue == null || eventValue is! String) {
      throw EngineException(
        message: 'Event envelope missing required field: event',
        code: 'MISSING_EVENT_TYPE',
        metadata: {'raw': map},
      );
    }
    final eventType = EngineEventType.fromValue(eventValue);
    if (eventType == null) {
      throw EngineException(
        message: 'Unknown event type: $eventValue',
        code: 'UNKNOWN_EVENT_TYPE',
        metadata: {
          'received': eventValue,
          'known': EngineEventType.values.map((e) => e.value).toList(),
        },
      );
    }

    // --- Parse optional fields ---
    final requestId = map['request_id'] as String?;
    final payload = map['payload'] as Map<String, dynamic>?;

    return EventEnvelope._(
      protocolVersion: version,
      event: eventType,
      requestId: requestId,
      payload: payload,
    );
  }

  @override
  String toString() =>
      'EventEnvelope(v=$protocolVersion, event=${event.value}, '
      'id=$requestId, payload=$payload)';
}
