import 'package:on_device_3d_builder/core/config/app_config.dart';

enum LogLevel {
  info,
  warning,
  error,
}

class AppLogger {
  final AppConfig _config;

  AppLogger(this._config);

  void log(String message,
      {LogLevel level = LogLevel.info, Object? error, StackTrace? stackTrace}) {
    if (!_config.isLoggingEnabled) return;

    final timestamp = DateTime.now().toIso8601String();
    final prefix = _getPrefix(level);
    final logMessage = '[$timestamp] $prefix $message';

    // In a real app, this would route to console, file, or remote service.
    // For now, we use print, formatted for potential console readers.
    // ignore: avoid_print
    print(logMessage);

    if (error != null) {
      // ignore: avoid_print
      print('  Error: $error');
    }
    if (stackTrace != null) {
      // ignore: avoid_print
      print('  StackTrace: $stackTrace');
    }
  }

  void info(String message) => log(message, level: LogLevel.info);

  void warning(String message, [Object? error]) =>
      log(message, level: LogLevel.warning, error: error);

  void error(String message, [Object? error, StackTrace? stackTrace]) =>
      log(message, level: LogLevel.error, error: error, stackTrace: stackTrace);

  String _getPrefix(LogLevel level) {
    switch (level) {
      case LogLevel.info:
        return '[INFO]';
      case LogLevel.warning:
        return '[WARN]';
      case LogLevel.error:
        return '[ERR]';
    }
  }
}
