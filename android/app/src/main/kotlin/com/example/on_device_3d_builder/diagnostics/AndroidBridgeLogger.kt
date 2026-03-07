package com.example.on_device_3d_builder.diagnostics

import android.content.Context
import android.util.Log
import java.io.File
import java.io.FileWriter
import java.io.IOException
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

object AndroidBridgeLogger {
    private const val TAG = "AndroidBridgeLogger"
    private var logFile: File? = null
    private val dateFormat = SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSS'Z'", Locale.US)

    /**
     * Call this inside MainActivity.onCreate or configureFlutterEngine to setup path.
     */
    fun initialize(context: Context) {
        try {
            val dir = File(context.cacheDir, "diagnostics")
            if (!dir.exists()) {
                dir.mkdirs()
            }
            logFile = File(dir, "android_bridge.log")
            Log.i(TAG, "Initialized diagnostic logger at \${logFile?.absolutePath}")
            log("--- Session Started ---")
        } catch (e: Exception) {
            Log.e(TAG, "Failed to initialize diagnostic logger", e)
        }
    }

    fun log(message: String) {
        val timestamp = dateFormat.format(Date())
        val entry = "[\$timestamp] \$message\n"
        
        Log.i(TAG, message)
        
        logFile?.let {
            try {
                FileWriter(it, true).use { writer ->
                    writer.append(entry)
                }
            } catch (e: IOException) {
                Log.e(TAG, "Failed to write log", e)
            }
        }
    }

    fun logError(contextMsg: String, exception: Exception) {
        val timestamp = dateFormat.format(Date())
        val entry = "[\$timestamp] [ERROR] \$contextMsg: \${Log.getStackTraceString(exception)}\n"
        
        Log.e(TAG, contextMsg, exception)
        
        logFile?.let {
            try {
                FileWriter(it, true).use { writer ->
                    writer.append(entry)
                }
            } catch (e: IOException) {
                Log.e(TAG, "Failed to write error log", e)
            }
        }
    }
}
