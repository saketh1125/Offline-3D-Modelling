using UnityEngine;
using ThreeDBuilder.Protocol;
using ThreeDBuilder.Core;
using ThreeDBuilder.Communication;
using ThreeDBuilder.Scene;
using ThreeDBuilder.Builders;
using ThreeDBuilder.Core.Diagnostics;


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
        private GameObject _currentSceneRoot;

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

            UnityDiagnosticsLogger.Initialize();
            UnityDiagnosticsLogger.Log("RuntimeManager: Awake. Instance registered.");

            CoreLogger.Info("RuntimeManager: Awake. Instance registered and pipeline components initialized.");
            Debug.Log("[UNITY DIAG] RuntimeManager Awake executed");
        }

        private void Start()
        {
            Debug.Log("[UNITY DIAG] RuntimeManager.Start() called");
            UnityDiagnosticsLogger.Log("RuntimeManager: Start. Unity initialized, emitting unity_ready.");
            CoreLogger.Info("RuntimeManager: Start. Emitting unity_ready via protocol enum.");

            EmitEvent(EngineEventType.UnityReady, "unity-ready", "{}");

            GameObject debug = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debug.name = "UNITY_RENDER_TEST";
            debug.transform.position = new Vector3(0f, 1f, 5f);
            debug.transform.localScale = new Vector3(2f, 2f, 2f);

            Debug.Log("[Render Diagnostic] Debug cube spawned at (0,1,5)");

            Debug.Log("[UNITY DIAG] unity_ready event emitted successfully");
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
            Debug.Log("[RuntimeManager] JSON RECEIVED: " + json);
            UnityDiagnosticsLogger.Log($"RuntimeManager.ReceiveCommand: {json}");
            // Outermost catch: nothing escapes this method.
            try
            {
                ReceiveCommandInternal(json);
            }
            catch (System.Exception e)
            {
                // Last-resort handler. If we get here, something truly unexpected
                // happened in validation or dispatch. Log and emit generic error.
                UnityEngine.Debug.LogError("ReceiveCommand exception: " + e.ToString());
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
                Debug.Log("[RuntimeManager] COMMAND TYPE: " + envelope.command);
                Debug.Log("[RuntimeManager] PAYLOAD: " + envelope.payload);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError("Command parse error: " + e.ToString());
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
            UnityEngine.Debug.Log("Executing command: " + envelope.command);
            switch (command)
            {
                case EngineCommand.Initialize:
                    UnityEngine.Debug.Log("Executing command: initialize");
                    HandleInitialize(envelope);
                    break;

                case EngineCommand.LoadScene:
                    UnityEngine.Debug.Log("Executing command: load_scene");
                    HandleLoadScene(envelope);
                    break;

                case EngineCommand.ClearScene:
                    HandleClearScene(envelope);
                    break;

                case EngineCommand.Dispose:
                    HandleDispose(envelope);
                    break;
                
                case EngineCommand.CameraMove:
                    UnityEngine.Debug.Log("Executing command: move_camera");
                    HandleCameraMove(envelope);
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

            // Initialize Unity subsystems: environment, lighting, camera, reflections
            SceneEnvironmentBootstrap.Setup();
            ReflectionProbeBootstrap.Setup();

            _isInitialized = true;
            CoreLogger.Info("RuntimeManager: Initialization complete (environment + reflections bootstrapped).");
            EmitEvent(EngineEventType.Initialized, envelope.request_id);
        }

        private void HandleLoadScene(CommandEnvelope envelope)
        {
            CoreLogger.Info("RuntimeManager: Loading scene...");

            if (!_isInitialized)
            {
                EmitErrorEvent(envelope.request_id, "NOT_INITIALIZED",
                    "Engine must be initialized before loading a scene.");
                return;
            }

            EmitEvent(EngineEventType.SceneLoading, envelope.request_id);

            try
            {
                UnityEngine.Debug.Log("RuntimeManager: Clearing previous scene");
                UnityEngine.Debug.Log("RuntimeManager: Child count before clearing = " + transform.childCount);
                
                // --- CLEAR PREVIOUS SCENE ---
                if (_currentSceneRoot != null)
                {
                    UnityEngine.Debug.Log("RuntimeManager: Destroying previous scene root: " + _currentSceneRoot.name);
                    DestroyImmediate(_currentSceneRoot);
                    _currentSceneRoot = null;
                }

                // Remove all children using reverse for-loop to safely modify hierarchy
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = transform.GetChild(i);
                    UnityEngine.Debug.Log("RuntimeManager: Destroying child: " + child.name);
                    DestroyImmediate(child.gameObject);
                }

                UnityEngine.Debug.Log("RuntimeManager: Scene cleared. Child count now = " + transform.childCount);

                Debug.Log("RuntimeManager: Parsing scene JSON");

                SceneModel parsedScene = _sceneInterpreter.ParseScene(envelope.payload);

                if (parsedScene == null)
                {
                    Debug.LogError("[RuntimeManager] Scene parsing returned NULL.");
                    EmitErrorEvent(envelope.request_id, "PARSE_FAILED", "Scene JSON invalid");
                    return;
                }
                else
                {
                    Debug.Log("[RuntimeManager] Scene parsed successfully.");
                    Debug.Log("[RuntimeManager] Objects count: " + parsedScene.objects.Count);
                }

                Debug.Log("RuntimeManager: Building scene");

                GameObject generatedRoot = _sceneBuilder.BuildScene(parsedScene);

                if (generatedRoot == null)
                {
                    Debug.LogError("RuntimeManager: SceneBuilder returned null");
                    EmitErrorEvent(envelope.request_id, "BUILD_FAILED", "SceneBuilder returned null");
                    return;
                }

                UnityEngine.Debug.Log("RuntimeManager: Scene built successfully. Root: " + generatedRoot.name);
                UnityEngine.Debug.Log("RuntimeManager: Child count after build = " + transform.childCount);
                
                // Count total objects in the new scene
                int totalObjects = generatedRoot.transform.GetComponentsInChildren<Transform>().Length;
                UnityEngine.Debug.Log("RuntimeManager: Total objects in new scene = " + totalObjects);

                generatedRoot.transform.SetParent(this.transform, true);
                Debug.Log("[RuntimeManager] Scene root attached. Child count: " + this.transform.childCount);
                _currentSceneRoot = generatedRoot;

                Debug.Log("RuntimeManager: Scene ready");

                EmitEvent(EngineEventType.SceneReady, envelope.request_id);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("RuntimeManager: Scene build exception: " + ex.ToString());
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

            if (_currentSceneRoot != null)
            {
                DestroyImmediate(_currentSceneRoot);
                _currentSceneRoot = null;
            }

            CoreLogger.Info("RuntimeManager: Disposed.");
            // Note: no event emitted after dispose — Flutter orchestrator handles this.
        }

        private void HandleCameraMove(CommandEnvelope envelope)
        {
            if (string.IsNullOrEmpty(envelope.payload)) return;

            try
            {
                var moveData = JsonUtility.FromJson<CameraMovePayload>(envelope.payload);
                if (moveData == null || string.IsNullOrEmpty(moveData.direction)) return;

                CoreLogger.Info($"RuntimeManager: Moving camera -> {moveData.direction}");

                GameObject camObj = GameObject.FindWithTag("MainCamera");
                if (camObj == null) camObj = Camera.main?.gameObject;
                
                if (camObj != null)
                {
                    float moveAmount = 1.0f;
                    switch (moveData.direction.ToLower())
                    {
                        case "up": case "down": case "left": case "right":
                        case "forward": case "back": case "zoom_in": case "zoom_out":
                            CoreLogger.Info($"RuntimeManager: Moving camera -> {moveData.direction}");
                            break;
                        case "reset":
                            UnityEngine.Debug.Log("Executing command: reset_camera");
                            CoreLogger.Info("RuntimeManager: Resetting camera");
                            // Try to find the TouchOrbitCamera and call reset
                            TouchOrbitCamera touchCam = camObj.GetComponent<TouchOrbitCamera>();
                            if (touchCam != null)
                            {
                                touchCam.ResetCamera();
                            }
                            else
                            {
                                // Fallback
                                camObj.transform.position = new Vector3(0, 5, -10);
                                camObj.transform.LookAt(Vector3.zero);
                            }
                            break;
                        default:
                            CoreLogger.Warning($"RuntimeManager: Camera command '{moveData.direction}' is ignored. Using touch controls now.");
                            break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                CoreLogger.Error("RuntimeManager: Failed to handle camera move.", ex);
            }
        }

        [System.Serializable]
        private class CameraMovePayload
        {
            public string direction;
        }

        // ─────────────────────────────────────────────────────────────────
        // Procedural Scene Regeneration
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Clears the existing procedural scene hierarchy entirely and mathematically rebuilds 
        /// it parsing the new JSON payload natively. Exposed publicly bypassing the 
        /// protocol event handler for direct testing / runtime integration if required by host.
        /// </summary>
        public void RegenerateScene(string jsonScene)
        {
            CoreLogger.Info("RuntimeManager: RegenerateScene requested. Clearing old hierarchy.");

            if (_currentSceneRoot != null)
            {
                DestroyImmediate(_currentSceneRoot);
                _currentSceneRoot = null;
            }

            try
            {
                SceneModel parsedScene = _sceneInterpreter.ParseScene(jsonScene);

                if (parsedScene != null)
                {
                    GameObject generatedRoot = _sceneBuilder.BuildScene(parsedScene);

                    if (generatedRoot != null)
                    {
                        generatedRoot.transform.SetParent(this.transform);
                        _currentSceneRoot = generatedRoot;

                        CoreLogger.Info($"RuntimeManager: Scene regenerated successfully with {parsedScene.objects.Count} base objects.");
                        EmitEvent(EngineEventType.SceneReady, "REGENERATE_SCENE");
                    }
                    else
                    {
                        CoreLogger.Error("RuntimeManager: BuildScene returned null root object during regeneration.");
                        EmitErrorEvent("REGENERATE_SCENE", "BUILD_FAILED", "Scene regeneration failed (null root).");
                    }
                }
            }
            catch (System.Exception ex)
            {
                CoreLogger.Error("RuntimeManager: Error processing RegenerateScene payload.", ex);
                EmitErrorEvent("REGENERATE_SCENE", "SCENE_REGENERATION_FAILED", ex.Message);
            }
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

            Debug.Log("[DIAG] Unity EmitEvent JSON=" + json);
            FlutterBridge.SendToFlutter(json);
        }

        private void EmitErrorEvent(string requestId, string code, string message)
        {
            Debug.Log($"RuntimeManager: Emitting error event -> {code} : {message}");

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
