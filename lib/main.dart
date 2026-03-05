import 'package:flutter/material.dart';
import 'package:on_device_3d_builder/core/config/app_config.dart';
import 'package:on_device_3d_builder/core/logging/app_logger.dart';
import 'package:on_device_3d_builder/engine/adapters/mock_engine_adapter.dart';
import 'package:on_device_3d_builder/engine/lifecycle/engine_lifecycle_manager.dart';
import 'package:on_device_3d_builder/engine/orchestrator/rendering_orchestrator.dart';
import 'package:on_device_3d_builder/features/scene_host/scene_host_screen.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();

  // Initialize core services.
  final config = AppConfig.development();
  final logger = AppLogger(config);

  logger.info('Application Starting: ${config.appName}');

  // Wire the rendering pipeline.
  final engine = MockEngineAdapter(logger);
  final lifecycle = EngineLifecycleManager(logger);
  final orchestrator = RenderingOrchestrator(
    engine: engine,
    lifecycle: lifecycle,
    logger: logger,
  );

  runApp(OnDevice3DBuilderApp(config: config, orchestrator: orchestrator));
}

class OnDevice3DBuilderApp extends StatelessWidget {
  final AppConfig config;
  final RenderingOrchestrator orchestrator;

  const OnDevice3DBuilderApp({
    super.key,
    required this.config,
    required this.orchestrator,
  });

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: config.appName,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFF5B6EF5),
          brightness: Brightness.dark,
        ),
        useMaterial3: true,
      ),
      home: SceneHostScreen(orchestrator: orchestrator),
      debugShowCheckedModeBanner: config.isDebugMode,
    );
  }
}
