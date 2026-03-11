import 'package:on_device_3d_builder/engine/contract/engine_event.dart';

/// Abstract contract for any rendering engine implementation.
///
/// This interface defines the boundary between the Flutter host application
/// and any embedded rendering engine (e.g., Unity, custom OpenGL, mock).
/// All engine-specific logic must live behind this interface.
abstract class RenderEngine {
  /// Initializes the rendering engine.
  ///
  /// Must be called before any other engine operations.
  /// Implementations should handle async setup (e.g., native bridge init).
  Future<void> initialize();

  /// Loads a scene from a JSON string.
  ///
  /// The [sceneJson] must be a valid JSON string conforming to
  /// the application's scene schema. Validation should happen
  /// in the orchestrator layer before invoking this method.
  Future<void> loadScene(String sceneJson);

  /// Clears the currently loaded scene from the engine.
  Future<void> clearScene();

  /// Releases all engine resources.
  ///
  /// After calling dispose, this engine instance must not be reused.
  /// Implementations must close any open streams or native resources.
  Future<void> dispose();

  /// Emits structural events (e.g., scene_loading, errors) from the native engine.
  Stream<EngineEvent> get events;

  /// Whether the engine has natively broadcasted it is fully loaded and ready for commands.
  bool get isReady;
}
