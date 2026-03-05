import 'dart:convert';

class JsonUtils {
  static Map<String, dynamic> decode(String jsonString) {
    try {
      return jsonDecode(jsonString) as Map<String, dynamic>;
    } catch (e) {
      throw FormatException('Invalid JSON string: $e');
    }
  }

  static String encode(Map<String, dynamic> data) {
    return jsonEncode(data);
  }
}
