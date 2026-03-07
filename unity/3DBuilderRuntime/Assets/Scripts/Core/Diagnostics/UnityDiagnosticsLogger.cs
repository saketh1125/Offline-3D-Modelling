using System;
using System.IO;
using UnityEngine;

namespace ThreeDBuilder.Core.Diagnostics
{
    public static class UnityDiagnosticsLogger
    {
        private static string logFilePath;
        private static bool isInitialized = false;

        public static void Initialize()
        {
            if (isInitialized) return;

            try
            {
                // Appends to the application's persistent data path which is accessible natively
                Directory.CreateDirectory(Application.persistentDataPath + "/diagnostics");
                logFilePath = Application.persistentDataPath + "/diagnostics/unity_runtime.log";
                
                isInitialized = true;
                Log("============== SESSION START (Unity C#) ==============");
            }
            catch (Exception ex)
            {
                Debug.LogError($"UnityDiagnosticsLogger initialization failed: {ex.Message}");
            }
        }

        public static void Log(string message)
        {
            if (!isInitialized) Initialize();
            
            try
            {
                string timestamp = DateTime.UtcNow.ToString("o");
                string entry = $"[{timestamp}] {message}\n";
                File.AppendAllText(logFilePath, entry);
            }
            catch (Exception)
            {
                // Suppress recursive I/O logging errors gracefully.
            }
        }

        public static void LogError(string context, string errorMessage)
        {
            Log($"[ERROR] {context}: {errorMessage}");
        }
    }
}
