class EngineEvent {
  final String type;
  final Map<String, dynamic>? payload;
  final DateTime timestamp;

  EngineEvent({
    required this.type,
    this.payload,
  }) : timestamp = DateTime.now();

  @override
  String toString() =>
      'EngineEvent(type: $type, payload: $payload, timestamp: $timestamp)';
}
