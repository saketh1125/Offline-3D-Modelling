using UnityEngine;

namespace ThreeDBuilder.Core
{
    /// <summary>
    /// Structured logging wrapper with [3DBuilder] prefix for Unity console filtering.
    /// </summary>
    public static class Logger
    {
        private const string TAG = "[3DBuilder]";

        public static void Info(string message)
        {
            Debug.Log($"{TAG} {message}");
        }

        public static void Warning(string message)
        {
            Debug.LogWarning($"{TAG} {message}");
        }

        public static void Error(string message)
        {
            Debug.LogError($"{TAG} {message}");
        }

        public static void Error(string message, System.Exception exception)
        {
            Debug.LogError($"{TAG} {message}\n{exception}");
        }
    }
}
