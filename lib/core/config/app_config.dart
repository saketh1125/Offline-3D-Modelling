enum Environment {
  development,
  staging,
  production,
}

class AppConfig {
  final Environment environment;
  final bool isLoggingEnabled;
  final bool isDebugMode;
  final String appName;

  const AppConfig({
    required this.environment,
    required this.isLoggingEnabled,
    required this.isDebugMode,
    required this.appName,
  });

  factory AppConfig.development() {
    return const AppConfig(
      environment: Environment.development,
      isLoggingEnabled: true,
      isDebugMode: true,
      appName: 'On-Device 3D Builder (Dev)',
    );
  }

  factory AppConfig.staging() {
    return const AppConfig(
      environment: Environment.staging,
      isLoggingEnabled: true,
      isDebugMode: true,
      appName: 'On-Device 3D Builder (Staging)',
    );
  }

  factory AppConfig.production() {
    return const AppConfig(
      environment: Environment.production,
      isLoggingEnabled: false,
      isDebugMode: false,
      appName: 'On-Device 3D Builder',
    );
  }
}
