class EngineException implements Exception {
  final String message;
  final String code;
  final Map<String, dynamic>? metadata;
  final dynamic originalError;

  EngineException({
    required this.message,
    required this.code,
    this.metadata,
    this.originalError,
  });

  @override
  String toString() {
    final buffer = StringBuffer('EngineException [$code]: $message');
    if (metadata != null) {
      buffer.write('\nMetadata: $metadata');
    }
    if (originalError != null) {
      buffer.write('\nCaused by: $originalError');
    }
    return buffer.toString();
  }
}
