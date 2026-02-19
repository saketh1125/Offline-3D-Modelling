class ValidationException implements Exception {
  final String message;
  final String fieldName;
  final String? reason;

  ValidationException({
    required this.message,
    required this.fieldName,
    this.reason,
  });

  @override
  String toString() {
    var result = 'ValidationException: $message (Field: $fieldName)';
    if (reason != null) {
      result += ' Reason: $reason';
    }
    return result;
  }
}
