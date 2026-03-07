using ThreeDBuilder.Core;
using ThreeDBuilder.Protocol;
using ThreeDBuilder.Core.Diagnostics;

using CoreLogger = ThreeDBuilder.Core.Logger;

namespace ThreeDBuilder.Communication
{
    /// <summary>
    /// Bridge for Flutter ↔ Unity communication.
    /// 
    /// In production, this will use:
    /// - UnitySendMessage (Unity → Flutter via Android/iOS native)
    /// - MethodChannel callbacks (Flutter → Unity)
    /// 
    /// Currently, all methods log only — no native binding.
    /// 
    /// <b>Thread safety:</b>
    /// Platform channel callbacks from Flutter are delivered on the Unity main thread
    /// by the flutter-unity-widget bridge. All calls here are therefore main-thread safe.
    /// </summary>
    public static class FlutterBridge
    {
        /// <summary>
        /// Sends a serialized EventEnvelope JSON string to Flutter.
        /// Currently logs only. Will be replaced with native messaging.
        /// </summary>
        public static void SendToFlutter(string eventJson)
        {
            UnityDiagnosticsLogger.Log($"FlutterBridge.SendToFlutter: {eventJson}");
            if (string.IsNullOrEmpty(eventJson))
            {
                CoreLogger.Warning("FlutterBridge.SendToFlutter: Attempted to send null/empty JSON.");
                return;
            }

            #if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new UnityEngine.AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<UnityEngine.AndroidJavaObject>("currentActivity"))
                {
                    activity.CallStatic("onUnityEvent", eventJson);
                }
            }
            catch (System.Exception ex)
            {
                CoreLogger.Error("FlutterBridge.SendToFlutter: JNI CallStatic failed. ", ex);
            }
            #endif

            UnityEngine.Debug.Log($"[DIAG] FlutterBridge sending event={eventJson}");
        }

        /// <summary>
        /// Called by the native platform when Flutter sends a command.
        /// Forwards the raw JSON to RuntimeManager.
        /// 
        /// This method NEVER throws — it catches all exceptions and logs them,
        /// ensuring the native caller is never surprised by unhandled C# exceptions.
        /// </summary>
        /// <param name="commandJson">Raw JSON command envelope string.</param>
        public static void OnCommandReceived(string commandJson)
        {
            UnityDiagnosticsLogger.Log($"FlutterBridge.OnCommandReceived: {commandJson}");
            try
            {
                // Guard null/empty before accessing .Length
                if (string.IsNullOrEmpty(commandJson))
                {
                    CoreLogger.Error("FlutterBridge.OnCommandReceived: Received null or empty JSON.");
                    // Attempt to emit error event if RuntimeManager is available.
                    TryEmitError(null, "INVALID_COMMAND_JSON",
                        "FlutterBridge received null or empty command JSON.");
                    return;
                }

                CoreLogger.Info($"FlutterBridge.OnCommandReceived: ({commandJson.Length} chars)");

                // Use cached Instance instead of FindObjectOfType for performance.
                var manager = Runtime.RuntimeManager.Instance;
                if (manager == null)
                {
                    CoreLogger.Error("FlutterBridge: RuntimeManager.Instance is null — not in scene.");
                    TryEmitError(null, "RUNTIME_NOT_FOUND",
                        "RuntimeManager is not available in the current scene.");
                    return;
                }

                manager.ReceiveCommand(commandJson);
            }
            catch (System.Exception e)
            {
                // Outermost catch — never let exceptions escape to native caller.
                CoreLogger.Error("FlutterBridge.OnCommandReceived: Unhandled exception.", e);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Private: Error Emission Fallback
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to emit an error event directly via SendToFlutter.
        /// Used when RuntimeManager is unavailable.
        /// </summary>
        private static void TryEmitError(string requestId, string code, string message)
        {
            try
            {
                var envelope = EventEnvelope.Create(
                    EngineEventType.Error,
                    requestId,
                    $"{{\"code\":\"{code}\",\"message\":\"{message}\"}}"
                );
                SendToFlutter(envelope.ToJson());
            }
            catch (System.Exception e)
            {
                CoreLogger.Error("FlutterBridge.TryEmitError: Failed to emit fallback error.", e);
            }
        }
    }
}
