using UnityEngine;
using ThreeDBuilder.Protocol;
using ThreeDBuilder.Core;
using ThreeDBuilder.Communication;
using ThreeDBuilder.Scene;
using ThreeDBuilder.Builders;


using CoreLogger = ThreeDBuilder.Core.Logger;

namespace ThreeDBuilder.Runtime
{
    /// <summary>
    /// Central command dispatcher for the 3D Builder Unity runtime.
    /// 
    /// Receives JSON command envelopes, validates them against Protocol v1.0,
    /// dispatches to the appropriate handler, and emits structured event responses.
    /// 
    /// <b>Embedded lifecycle:</b>
    /// This MonoBehaviour is attached to a persistent GameObject in the RuntimeScene.
    /// It is NOT a global DontDestroyOnLoad singleton. When the Flutter host destroys
    /// the UnityPlayer, Unity's normal scene teardown calls OnDestroy(), which
    /// performs graceful cleanup.
    /// 
    /// <b>Thread safety:</b>
    /// All calls to ReceiveCommand must originate on the Unity main thread.
    /// Platform channel callbacks from Flutter are dispatched on the main thread
    /// by the flutter-unity-widget bridge, so this is safe by default.
    /// 
    /// <b>Duplicate guard:</b>
    /// Awake() checks for duplicate instances and self-destructs if one exists,
    /// guaranteeing exactly one RuntimeManager per scene.
    /// </summary>
    public class RuntimeManager : MonoBehaviour
    {
        private bool _isInitialized = false;
        private bool _isDisposed = false;

        private SceneInterpreter _sceneInterpreter;
        private SceneBuilder _sceneBuilder;

        /// <summary>
        /// Exposed for FlutterBridge to cache a reference instead of using
        /// FindObjectOfType on every call.
        /// </summary>
        public static RuntimeManager Instance { get; private set; }

        // ─────────────────────────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────────────────────────

        private void Awake()
        {
            // Duplicate guard: only one RuntimeManager may exist.
            if (Instance != null && Instance != this)
            {
                CoreLogger.Warning("RuntimeManager: Duplicate instance detected. Destroying self.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _sceneInterpreter = new SceneInterpreter();
            _sceneBuilder = new SceneBuilder();

            CoreLogger.Info("RuntimeManager: Awake. Instance registered and pipeline components initialized.");
        }

        /// <summary>
        /// Called by Unity when the Android activity is paused/resumed.
        /// When embedded in Flutter, the host activity pausing should NOT
        /// dispose the runtime — only pause rendering if needed.
        /// </summary>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                CoreLogger.Info("RuntimeManager: Application paused (Android activity backgrounded).");
                // Future: pause rendering, reduce GPU usage
            }
            else
            {
                CoreLogger.Info("RuntimeManager: Application resumed.");
                // Future: resume rendering
            }
        }

        /// <summary>
        /// Called when the GameObject or scene is destroyed.
        /// Performs graceful cleanup without emitting events (Flutter is gone).
        /// </summary>
        private void OnDestroy()
        {
            CoreLogger.Info("RuntimeManager: OnDestroy. Cleaning up.");

            _isDisposed = true;
            _isInitialized = false;

            if (Instance == this)
                Instance = null;
        }

        // ─────────────────────────────────────────────────────────────────
        // Public API: called by FlutterBridge
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Entry point for all commands from Flutter.
        /// 
        /// <b>Contract:</b> This method NEVER throws. All failures are caught
        /// and emitted as structured error events. This is critical for embedded
        /// operation — an uncaught exception here would crash the Unity runtime
        /// and take the host Flutter app down with it.
        /// 
        /// <b>Thread:</b> Must be called on the Unity main thread.
        /// </summary>
        public void ReceiveCommand(string json)
        {
            // Outermost catch: nothing escapes this method.
            try
            {
                ReceiveCommandInternal(json);
            }
            catch (System.Exception e)
            {
                // Last-resort handler. If we get here, something truly unexpected
                // happened in validation or dispatch. Log and emit generic error.
                CoreLogger.Error("RuntimeManager: Unhandled exception in ReceiveCommand.", e);
                try
                {
                    EmitErrorEvent(null, "INTERNAL_ERROR", e.Message);
                }
                catch (System.Exception inner)
                {
                    // If even error emission fails, log and give up.
                    CoreLogger.Error("RuntimeManager: Failed to emit error event.", inner);
                }
            }
        }

        private void ReceiveCommandInternal(string json)
        {
            // Guard: null/empty input
            if (string.IsNullOrEmpty(json))
            {
                CoreLogger.Error("RuntimeManager: Received null or empty command JSON.");
                EmitErrorEvent(null, "INVALID_COMMAND_JSON", "Command JSON is null or empty.");
                return;
            }

            CoreLogger.Info($"RuntimeManager: Received command JSON ({json.Length} chars)");

            // Step 1: Deserialize
            CommandEnvelope envelope;
            try
            {
                envelope = CommandEnvelope.FromJson(json);
            }
            catch (System.Exception e)
            {
                CoreLogger.Error("RuntimeManager: Failed to deserialize command.", e);
                EmitErrorEvent(null, "INVALID_COMMAND_JSON", e.Message);
                return;
            }

            CoreLogger.Info($"RuntimeManager: Parsed -> {envelope}");

            // Step 2: Validate
            string validationError = envelope.Validate();
            if (validationError != null)
            {
                CoreLogger.Error($"RuntimeManager: Validation failed -> {validationError}");
                EmitErrorEvent(envelope.request_id, "VALIDATION_FAILED", validationError);
                return;
            }

            // Step 3: Guard disposed
            if (_isDisposed)
            {
                CoreLogger.Warning("RuntimeManager: Command received after dispose. Ignoring.");
                EmitErrorEvent(envelope.request_id, "RUNTIME_DISPOSED",
                    "RuntimeManager has been disposed.");
                return;
            }

            // Step 4: Dispatch (handler exceptions caught by outer try/catch)
            EngineCommand command = envelope.GetCommand();
            DispatchCommand(command, envelope);
        }

        // ─────────────────────────────────────────────────────────────────
        // Command Dispatch
        // ─────────────────────────────────────────────────────────────────

        private void DispatchCommand(EngineCommand command, CommandEnvelope envelope)
        {
            switch (command)
            {
                case EngineCommand.Initialize:
                    HandleInitialize(envelope);
                    break;

                case EngineCommand.LoadScene:
                    HandleLoadScene(envelope);
                    break;

                case EngineCommand.ClearScene:
                    HandleClearScene(envelope);
                    break;

                case EngineCommand.Dispose:
                    HandleDispose(envelope);
                    break;

                default:
                    CoreLogger.Error($"RuntimeManager: Unknown command after validation: {command}");
                    EmitErrorEvent(envelope.request_id, "UNKNOWN_COMMAND",
                        $"Unhandled command: {envelope.command}");
                    break;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Command Handlers
        // ─────────────────────────────────────────────────────────────────

        private void HandleInitialize(CommandEnvelope envelope)
        {
            CoreLogger.Info("RuntimeManager: Initializing...");

            if (_isInitialized)
            {
                CoreLogger.Warning("RuntimeManager: Already initialized. Re-emitting initialized event.");
                EmitEvent(EngineEventType.Initialized, envelope.request_id);
                return;
            }

            // TODO: Initialize Unity subsystems (lighting, camera, scene graph)

            _isInitialized = true;
            CoreLogger.Info("RuntimeManager: Initialization complete.");
            EmitEvent(EngineEventType.Initialized, envelope.request_id);
        }

        private void HandleLoadScene(CommandEnvelope envelope)
        {
            CoreLogger.Info("RuntimeManager: Loading scene...");

            if (!_isInitialized)
            {
                CoreLogger.Error("RuntimeManager: Cannot load scene — not initialized.");
                EmitErrorEvent(envelope.request_id, "NOT_INITIALIZED",
                    "Engine must be initialized before loading a scene.");
                return;
            }

            // Emit loading event
            EmitEvent(EngineEventType.SceneLoading, envelope.request_id);

            CoreLogger.Info($"RuntimeManager: Scene payload size: " +
                        $"{(string.IsNullOrEmpty(envelope.payload) ? 0 : envelope.payload.Length)} chars");

            try
            {
                // Step 1: Parse JSON into our plain-data SceneModel mapping
                SceneModel parsedScene = _sceneInterpreter.ParseScene(envelope.payload);

                if (parsedScene != null)
                {
                    CoreLogger.Info($"RuntimeManager: Parsed JSON successfully. Found {parsedScene.objects.Count} objects.");

                    // Step 2: Use SceneBuilder to translate the model into Unity standard GameObjects
                    GameObject generatedRoot = _sceneBuilder.BuildScene(parsedScene);

                    if (generatedRoot != null)
                    {
                        // Attach the resulting hierarchy to this executing MonoBehaviour
                        generatedRoot.transform.SetParent(this.transform);
                        CoreLogger.Info("RuntimeManager: Scene built successfully!");
                        EmitEvent(EngineEventType.SceneReady, envelope.request_id);
                    }
                    else
                    {
                        CoreLogger.Error("RuntimeManager: BuildScene returned null root object.");
                        EmitErrorEvent(envelope.request_id, "BUILD_FAILED", "Scene built successfully but root object is null.");
                    }
                }
            }
            catch (System.Exception ex)
            {
                CoreLogger.Error("RuntimeManager: Failed to build procedural scene.", ex);
                EmitErrorEvent(envelope.request_id, "SCENE_BUILD_FAILED", ex.Message);
            }
        }

        private void HandleClearScene(CommandEnvelope envelope)
        {
            CoreLogger.Info("RuntimeManager: Clearing scene...");

            // TODO: Destroy all spawned GameObjects, reset materials

            CoreLogger.Info("RuntimeManager: Scene cleared (stub).");
            EmitEvent(EngineEventType.SceneReady, envelope.request_id);
        }

        private void HandleDispose(CommandEnvelope envelope)
        {
            CoreLogger.Info("RuntimeManager: Disposing via command...");

            if (_isDisposed)
            {
                CoreLogger.Warning("RuntimeManager: Already disposed. Ignoring.");
                return;
            }

            _isDisposed = true;
            _isInitialized = false;

            // TODO: Release native resources, destroy spawned objects

            CoreLogger.Info("RuntimeManager: Disposed.");
            // Note: no event emitted after dispose — Flutter orchestrator handles this.
        }

        // ─────────────────────────────────────────────────────────────────
        // Event Emission
        // ─────────────────────────────────────────────────────────────────

        private void EmitEvent(EngineEventType eventType, string requestId,
                               string payload = null)
        {
            var envelope = EventEnvelope.Create(eventType, requestId, payload);
            string json = envelope.ToJson();

            CoreLogger.Info($"RuntimeManager: Emitting -> {envelope}");

            FlutterBridge.SendToFlutter(json);
        }

        private void EmitErrorEvent(string requestId, string code, string message)
        {
            // Build error payload JSON manually (JsonUtility doesn't handle dictionaries).
            string errorPayload = $"{{\"code\":\"{EscapeJson(code)}\",\"message\":\"{EscapeJson(message)}\"}}";

            EmitEvent(EngineEventType.Error, requestId, errorPayload);
        }

        // ─────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Minimal JSON string escaping for error payloads.
        /// </summary>
        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");
        }
    }
}
