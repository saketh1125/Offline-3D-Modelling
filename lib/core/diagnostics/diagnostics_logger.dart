import 'dart:io';
import 'package:flutter/foundation.dart';

class FlutterDiagnosticsLogger {
  static final FlutterDiagnosticsLogger _instance = FlutterDiagnosticsLogger._internal();
  factory FlutterDiagnosticsLogger() => _instance;
  
  FlutterDiagnosticsLogger._internal();

  File? _logFile;
  bool _initialized = false;
  final List<String> _buffer = [];
  static const int maxLogs = 1000;

  Future<void> initialize() async {
    if (_initialized) return;
    try {
      final diagDir = Directory('${Directory.systemTemp.path}/diagnostics');
      if (!await diagDir.exists()) {
        await diagDir.create(recursive: true);
      }
      _logFile = File('${diagDir.path}/flutter_runtime.log');
      _initialized = true;
      _writeBuffer();
    } catch (e) {
      debugPrint("Diagnostics: Failed to initialize logger: $e");
    }
  }

  void log(String message) {
    final timestamp = DateTime.now().toIso8601String();
    final entry = "[$timestamp] $message\n";
    
    _buffer.add(entry);
    if (_buffer.length > maxLogs) {
      _buffer.removeAt(0);
    }
    
    if (_initialized && _logFile != null) {
      _logFile!.writeAsStringSync(entry, mode: FileMode.append);
    }
  }

  void logError(String context, dynamic error) {
    log("[ERROR] $context: $error");
  }

  void _writeBuffer() {
    if (_logFile != null) {
      for (var line in _buffer) {
        _logFile!.writeAsStringSync(line, mode: FileMode.append);
      }
    }
  }

  Future<String> readLogs() async {
    if (_logFile != null && await _logFile!.exists()) {
      return await _logFile!.readAsString();
    }
    return "No logs found.";
  }
}
