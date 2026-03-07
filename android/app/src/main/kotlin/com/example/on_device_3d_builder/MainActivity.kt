package com.example.on_device_3d_builder

import android.os.Handler
import android.os.Looper
import android.util.Log
import com.example.on_device_3d_builder.diagnostics.AndroidBridgeLogger
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodChannel

/**
 * MainActivity — Flutter ↔ Unity bridge via MethodChannel.
 *
 * Flutter side calls:
 *   channel.invokeMethod('sendCommand', jsonString)
 *
 * Unity side calls (from C#):
 *   AndroidJavaClass("com.example.on_device_3d_builder.MainActivity")
 *       .CallStatic("onUnityEvent", eventJson)
 *
 * Threading:
 *   - UnityPlayer.UnitySendMessage is safe to call from any thread.
 *   - onUnityEvent posts to main looper before invoking the MethodChannel.
 */
class MainActivity : FlutterActivity() {

    companion object {
        private const val TAG = "UnityBridge"
        private const val CHANNEL_NAME = "com.sankalp.unity.bridge"
        private const val UNITY_GAMEOBJECT = "RuntimeManager"
        private const val UNITY_METHOD = "ReceiveCommand"

        /** Weak reference to the channel, set during configureFlutterEngine. */
        private var channel: MethodChannel? = null

        /** Main thread handler for posting callbacks. */
        private val mainHandler = Handler(Looper.getMainLooper())

        /**
         * Static entry point for Unity → Flutter events.
         *
         * Called from C# via:
         *   AndroidJavaClass("com.example.on_device_3d_builder.MainActivity")
         *       .CallStatic("onUnityEvent", json)
         *
         * @param json EventEnvelope JSON string from Unity RuntimeManager.
         */
        @JvmStatic
        fun onUnityEvent(json: String?) {
            if (json.isNullOrEmpty()) {
                Log.w(TAG, "onUnityEvent: received null/empty JSON, ignoring.")
                return
            }
            AndroidBridgeLogger.log("onUnityEvent received: $json")
            Log.i(TAG, "[DIAG] onUnityEvent received: $json")

            val ch = channel
            if (ch == null) {
                Log.w(TAG, "onUnityEvent: MethodChannel not initialized, dropping event.")
                return
            }

            // Ensure we invoke on the main (UI) thread.
            mainHandler.post {
                try {
                    ch.invokeMethod("onUnityEvent", json)
                } catch (e: Exception) {
                    AndroidBridgeLogger.logError("Failed to invoke Flutter method onUnityEvent", e)
                    Log.e(TAG, "onUnityEvent: failed to invoke Flutter method.", e)
                }
            }
        }
    }

    override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)
        
        AndroidBridgeLogger.initialize(this)
        AndroidBridgeLogger.log("============== SESSION START (Android Native) ==============")

        val methodChannel = MethodChannel(
            flutterEngine.dartExecutor.binaryMessenger,
            CHANNEL_NAME
        )

        methodChannel.setMethodCallHandler { call, result ->
            when (call.method) {
                "sendCommand" -> {
                    val json = call.argument<String>("json")
                    if (json.isNullOrEmpty()) {
                        result.error(
                            "INVALID_ARGUMENT",
                            "sendCommand requires a non-empty 'json' argument.",
                            null
                        )
                        return@setMethodCallHandler
                    }
                    handleSendCommand(json, result)
                }
                else -> {
                    result.notImplemented()
                }
            }
        }

        channel = methodChannel
        Log.i(TAG, "MethodChannel '$CHANNEL_NAME' initialized.")
    }

    override fun cleanUpFlutterEngine(flutterEngine: FlutterEngine) {
        channel = null
        Log.i(TAG, "MethodChannel cleaned up.")
        super.cleanUpFlutterEngine(flutterEngine)
    }

    /**
     * Forward a command JSON to Unity's RuntimeManager.
     *
     * Uses UnityPlayer.UnitySendMessage which is thread-safe and
     * internally queues the message to Unity's main thread.
     */
    private fun handleSendCommand(json: String, result: MethodChannel.Result) {
        try {
            // UnityPlayer.UnitySendMessage is provided by the unityLibrary.
            // It sends a string message to a named GameObject's method.
            com.unity3d.player.UnityPlayer.UnitySendMessage(
                UNITY_GAMEOBJECT,
                UNITY_METHOD,
                json
            )
            AndroidBridgeLogger.log("Forwarded command to Unity (UnitySendMessage): $UNITY_GAMEOBJECT.$UNITY_METHOD")
            result.success(null)
        } catch (e: Exception) {
            AndroidBridgeLogger.logError("handleSendCommand: UnitySendMessage failed", e)
            Log.e(TAG, "handleSendCommand: UnitySendMessage failed.", e)
            result.error("UNITY_SEND_FAILED", e.message, null)
        }
    }
}
