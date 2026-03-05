import 'package:flutter_test/flutter_test.dart';
import 'package:on_device_3d_builder/core/config/app_config.dart';
import 'package:on_device_3d_builder/core/logging/app_logger.dart';
import 'package:on_device_3d_builder/engine/adapters/mock_engine_adapter.dart';
import 'package:on_device_3d_builder/engine/lifecycle/engine_lifecycle_manager.dart';
import 'package:on_device_3d_builder/engine/orchestrator/rendering_orchestrator.dart';
import 'package:on_device_3d_builder/features/scene_host/scene_host_screen.dart';
import 'package:on_device_3d_builder/main.dart';

void main() {
  testWidgets('SceneHostScreen mounts correctly', (WidgetTester tester) async {
    final config = AppConfig.development();
    final logger = AppLogger(config);
    final engine = MockEngineAdapter(logger);
    final lifecycle = EngineLifecycleManager(logger);
    final orchestrator = RenderingOrchestrator(
      engine: engine,
      lifecycle: lifecycle,
      logger: logger,
    );

    // runAsync lets real Future.delayed calls complete (engine init uses real timers).
    await tester.runAsync(() async {
      await tester.pumpWidget(
        OnDevice3DBuilderApp(config: config, orchestrator: orchestrator),
      );

      // Verify scaffold renders immediately.
      expect(find.byType(SceneHostScreen), findsOneWidget);
      expect(find.text('Scene Host'), findsOneWidget);

      // Let addPostFrameCallback fire + engine init complete (500ms delay).
      await tester.pump();
      await Future<void>.delayed(const Duration(milliseconds: 700));
      await tester.pump();

      // Full pipeline async flow is tested in pipeline_integration_test.dart.
    });
  });
}
