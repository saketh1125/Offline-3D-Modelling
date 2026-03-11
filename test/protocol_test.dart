import 'package:flutter_test/flutter_test.dart';
import 'package:on_device_3d_builder/core/errors/engine_exception.dart';
import 'package:on_device_3d_builder/engine/protocol/command_envelope.dart';
import 'package:on_device_3d_builder/engine/protocol/event_envelope.dart';
import 'package:on_device_3d_builder/engine/protocol/protocol_constants.dart';

void main() {
  // ---------------------------------------------------------------------------
  // Protocol Constants
  // ---------------------------------------------------------------------------

  group('ProtocolVersion', () {
    test('v1 is "1.0"', () {
      expect(ProtocolVersion.v1, '1.0');
    });

    test('supported set contains v1', () {
      expect(ProtocolVersion.supported, contains('1.0'));
    });
  });

  group('EngineCommand', () {
    test('all commands have unique wire values', () {
      final values = EngineCommand.values.map((e) => e.value).toSet();
      expect(values.length, EngineCommand.values.length);
    });

    test('fromValue resolves known commands', () {
      expect(EngineCommand.fromValue('load_scene'), EngineCommand.loadScene);
      expect(EngineCommand.fromValue('clear_scene'), EngineCommand.clearScene);
      expect(EngineCommand.fromValue('dispose'), EngineCommand.dispose);
    });

    test('fromValue returns null for unknown commands', () {
      expect(EngineCommand.fromValue('explode'), isNull);
    });
  });

  group('EngineEventType', () {
    test('all events have unique wire values', () {
      final values = EngineEventType.values.map((e) => e.value).toSet();
      expect(values.length, EngineEventType.values.length);
    });

    test('fromValue resolves known events', () {
      expect(EngineEventType.fromValue('scene_loading'),
          EngineEventType.sceneLoading);
      expect(
          EngineEventType.fromValue('scene_ready'), EngineEventType.sceneReady);
      expect(EngineEventType.fromValue('error'), EngineEventType.error);
      expect(EngineEventType.fromValue('performance_stats'),
          EngineEventType.performanceStats);
    });

    test('fromValue returns null for unknown events', () {
      expect(EngineEventType.fromValue('magic'), isNull);
    });
  });

  // ---------------------------------------------------------------------------
  // Command Envelope
  // ---------------------------------------------------------------------------

  group('CommandEnvelope', () {
    test('create() auto-fills protocol version v1', () {
      final env = CommandEnvelope.create(EngineCommand.loadScene);
      expect(env.protocolVersion, ProtocolVersion.v1);
    });

    test('create() generates a non-empty request ID', () {
      final env = CommandEnvelope.create(EngineCommand.loadScene);
      expect(env.requestId, isNotEmpty);
    });

    test('create() generates unique request IDs', () {
      final ids = List.generate(
        10,
        (_) => CommandEnvelope.create(EngineCommand.loadScene).requestId,
      );
      expect(ids.toSet().length, 10);
    });

    test('toMap() serializes all fields correctly', () {
      final env = CommandEnvelope.create(
        EngineCommand.loadScene,
        payload: {'scene_json': '{}'},
      );
      final map = env.toMap();

      expect(map['protocol_version'], ProtocolVersion.v1);
      expect(map['command'], 'load_scene');
      expect(map['request_id'], env.requestId);
      expect(map['payload'], {'scene_json': '{}'});
    });

    test('toMap() omits payload when null', () {
      final env = CommandEnvelope.create(EngineCommand.clearScene);
      final map = env.toMap();

      expect(map.containsKey('payload'), isFalse);
    });

    test('toMap() includes command wire value, not enum name', () {
      final env = CommandEnvelope.create(EngineCommand.loadScene);
      final map = env.toMap();

      expect(map['command'], 'load_scene'); // wire value, not 'loadScene'
    });
  });

  // ---------------------------------------------------------------------------
  // Event Envelope
  // ---------------------------------------------------------------------------

  group('EventEnvelope.fromMap()', () {
    Map<String, dynamic> validEventMap({
      String version = '1.0',
      String event = 'scene_ready',
      String? requestId = 'req-1',
      Map<String, dynamic>? payload,
    }) {
      return {
        'protocol_version': version,
        'event': event,
        if (requestId != null) 'request_id': requestId,
        if (payload != null) 'payload': payload,
      };
    }

    test('parses a valid event envelope', () {
      final env = EventEnvelope.fromMap(validEventMap());

      expect(env.protocolVersion, '1.0');
      expect(env.event, EngineEventType.sceneReady);
      expect(env.requestId, 'req-1');
    });

    test('parses all known event types', () {
      for (final type in EngineEventType.values) {
        final env = EventEnvelope.fromMap(validEventMap(event: type.value));
        expect(env.event, type);
      }
    });

    test('parses envelope with payload', () {
      final payload = {'fps': 60, 'triangles': 1200};
      final env = EventEnvelope.fromMap(validEventMap(payload: payload));

      expect(env.payload, payload);
    });

    test('parses envelope without request_id', () {
      final env = EventEnvelope.fromMap(validEventMap(requestId: null));
      expect(env.requestId, isNull);
    });

    // --- Rejection tests ---

    test('rejects missing protocol_version', () {
      expect(
        () => EventEnvelope.fromMap({'event': 'scene_ready'}),
        throwsA(
          isA<EngineException>()
              .having((e) => e.code, 'code', 'MISSING_PROTOCOL_VERSION'),
        ),
      );
    });

    test('rejects unsupported protocol_version', () {
      expect(
        () => EventEnvelope.fromMap(validEventMap(version: '99.0')),
        throwsA(
          isA<EngineException>()
              .having((e) => e.code, 'code', 'UNSUPPORTED_PROTOCOL_VERSION'),
        ),
      );
    });

    test('rejects missing event type', () {
      expect(
        () => EventEnvelope.fromMap({'protocol_version': '1.0'}),
        throwsA(
          isA<EngineException>()
              .having((e) => e.code, 'code', 'MISSING_EVENT_TYPE'),
        ),
      );
    });

    test('rejects unknown event type', () {
      expect(
        () => EventEnvelope.fromMap(validEventMap(event: 'self_destruct')),
        throwsA(
          isA<EngineException>()
              .having((e) => e.code, 'code', 'UNKNOWN_EVENT_TYPE'),
        ),
      );
    });

    test('rejection metadata includes received value', () {
      try {
        EventEnvelope.fromMap(validEventMap(event: 'bogus'));
        fail('Expected EngineException');
      } on EngineException catch (e) {
        expect(e.metadata?['received'], 'bogus');
        expect(e.metadata?['known'], isA<List>());
      }
    });
  });
}
