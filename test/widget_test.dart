import 'package:flutter_test/flutter_test.dart';
import 'package:on_device_3d_builder/controllers/scene_controller.dart';
import 'package:on_device_3d_builder/core/config/app_config.dart';
import 'package:on_device_3d_builder/core/logging/app_logger.dart';
import 'package:on_device_3d_builder/engine/adapters/mock_engine_adapter.dart';
import 'package:on_device_3d_builder/ui/scene_editor_page.dart';
import 'package:on_device_3d_builder/main.dart';

void main() {
  testWidgets('SceneEditor mounts correctly', (WidgetTester tester) async {
    final config = AppConfig.development();
    final logger = AppLogger(config);
    final engine = MockEngineAdapter(logger);
    final sceneController = SceneController(engine: engine);

    // runAsync lets real Future.delayed calls complete
    await tester.runAsync(() async {
      await tester.pumpWidget(
        OnDevice3DBuilderApp(config: config, sceneController: sceneController),
      );

      // Verify scaffold renders immediately.
      expect(find.byType(SceneEditorPage), findsOneWidget);
      expect(find.text('Procedural Generator'), findsOneWidget);

      await tester.pump();
      await Future<void>.delayed(const Duration(milliseconds: 700));
      await tester.pump();
    });
  });
}
