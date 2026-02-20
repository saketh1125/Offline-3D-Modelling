using UnityEngine;
using ThreeDBuilder.Protocol;
using ThreeDBuilder.Core;
using ThreeDBuilder.Communication;

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
                Logger.Warning("RuntimeManager: Duplicate instance detected. Destroying self.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Logger.Info("RuntimeManager: Awake. Instance registered.");
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
                Logger.Info("RuntimeManager: Application paused (Android activity backgrounded).");
                // Future: pause rendering, reduce GPU usage
            }
            else
            {
                Logger.Info("RuntimeManager: Application resumed.");
                // Future: resume rendering
            }
        }

        /// <summary>
        /// Called when the GameObject or scene is destroyed.
        /// Performs graceful cleanup without emitting events (Flutter is gone).
        /// </summary>
        private void OnDestroy()
        {
            Logger.Info("RuntimeManager: OnDestroy. Cleaning up.");

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
                Logger.Error("RuntimeManager: Unhandled exception in ReceiveCommand.", e);
                try
                {
                    EmitErrorEvent(null, "INTERNAL_ERROR", e.Message);
                }
                catch (System.Exception inner)
                {
                    // If even error emission fails, log and give up.
                    Logger.Error("RuntimeManager: Failed to emit error event.", inner);
                }
            }
        }

        private void ReceiveCommandInternal(string json)
        {
            // Guard: null/empty input
            if (string.IsNullOrEmpty(json))
            {
                Logger.Error("RuntimeManager: Received null or empty command JSON.");
                EmitErrorEvent(null, "INVALID_COMMAND_JSON", "Command JSON is null or empty.");
                return;
            }

            Logger.Info($"RuntimeManager: Received command JSON ({json.Length} chars)");

            // Step 1: Deserialize
            CommandEnvelope envelope;
            try
            {
                envelope = CommandEnvelope.FromJson(json);
            }
            catch (System.Exception e)
            {
                Logger.Error("RuntimeManager: Failed to deserialize command.", e);
                EmitErrorEvent(null, "INVALID_COMMAND_JSON", e.Message);
                return;
            }

            Logger.Info($"RuntimeManager: Parsed -> {envelope}");

            // Step 2: Validate
            string validationError = envelope.Validate();
            if (validationError != null)
            {
                Logger.Error($"RuntimeManager: Validation failed -> {validationError}");
                EmitErrorEvent(envelope.request_id, "VALIDATION_FAILED", validationError);
                return;
            }

            // Step 3: Guard disposed
            if (_isDisposed)
            {
                Logger.Warning("RuntimeManager: Command received after dispose. Ignoring.");
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
                    Logger.Error($"RuntimeManager: Unknown command after validation: {command}");
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
            Logger.Info("RuntimeManager: Initializing...");

            if (_isInitialized)
            {
                Logger.Warning("RuntimeManager: Already initialized. Re-emitting initialized event.");
                EmitEvent(EngineEventType.Initialized, envelope.request_id);
                return;
            }

            // TODO: Initialize Unity subsystems (lighting, camera, scene graph)

            _isInitialized = true;
            Logger.Info("RuntimeManager: Initialization complete.");
            EmitEvent(EngineEventType.Initialized, envelope.request_id);
        }

        private void HandleLoadScene(CommandEnvelope envelope)
        {
            Logger.Info("RuntimeManager: Loading scene...");

            if (!_isInitialized)
            {
                Logger.Error("RuntimeManager: Cannot load scene — not initialized.");
                EmitErrorEvent(envelope.request_id, "NOT_INITIALIZED",
                    "Engine must be initialized before loading a scene.");
                return;
            }

            // Emit loading event
            EmitEvent(EngineEventType.SceneLoading, envelope.request_id);

            // TODO: Parse envelope.payload as scene JSON and build mesh/materials
            Logger.Info($"RuntimeManager: Scene payload size: " +
                        $"{(string.IsNullOrEmpty(envelope.payload) ? 0 : envelope.payload.Length)} chars");

            // For now, immediately emit scene_ready (will be async with real implementation)
            Logger.Info("RuntimeManager: Scene loaded (stub).");
            EmitEvent(EngineEventType.SceneReady, envelope.request_id);
        }

        private void HandleClearScene(CommandEnvelope envelope)
        {
            Logger.Info("RuntimeManager: Clearing scene...");

            // TODO: Destroy all spawned GameObjects, reset materials

            Logger.Info("RuntimeManager: Scene cleared (stub).");
            EmitEvent(EngineEventType.SceneReady, envelope.request_id);
        }

        private void HandleDispose(CommandEnvelope envelope)
        {
            Logger.Info("RuntimeManager: Disposing via command...");

            if (_isDisposed)
            {
                Logger.Warning("RuntimeManager: Already disposed. Ignoring.");
                return;
            }

            _isDisposed = true;
            _isInitialized = false;

            // TODO: Release native resources, destroy spawned objects

            Logger.Info("RuntimeManager: Disposed.");
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

            Logger.Info($"RuntimeManager: Emitting -> {envelope}");

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
