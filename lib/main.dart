import 'package:flutter/material.dart';
import 'package:on_device_3d_builder/controllers/scene_controller.dart';
import 'package:on_device_3d_builder/core/config/app_config.dart';
import 'package:on_device_3d_builder/core/logging/app_logger.dart';
import 'package:on_device_3d_builder/ui/scene_editor_page.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Initialize core services.
  final config = AppConfig.development();
  final logger = AppLogger(config);

  logger.info('Application Starting: ${config.appName}');

  // Create our interaction controller tailored for flutter_unity_widget
  final sceneController = SceneController();

  runApp(
      OnDevice3DBuilderApp(config: config, sceneController: sceneController));
}

class OnDevice3DBuilderApp extends StatelessWidget {
  final AppConfig config;
  final SceneController sceneController;

  const OnDevice3DBuilderApp({
    super.key,
    required this.config,
    required this.sceneController,
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
      home: SceneEditorPage(sceneController: sceneController),
      debugShowCheckedModeBanner: config.isDebugMode,
    );
  }
}
