using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;

namespace ThreeDBuilder.Runtime
{
    /// <summary>
    /// Monitors performance during scene generation and runtime.
    /// Provides detailed logging for performance analysis.
    /// </summary>
    public static class PerformanceMonitor
    {
        private static readonly Dictionary<string, Stopwatch> _timers = new Dictionary<string, Stopwatch>();
        private static int _drawCallCount = 0;
        private static int _instanceCount = 0;

        /// <summary>
        /// Start timing a named operation.
        /// </summary>
        public static void StartTimer(string operationName)
        {
            if (!_timers.TryGetValue(operationName, out var timer))
            {
                timer = new Stopwatch();
                _timers[operationName] = timer;
            }
            timer.Restart();
        }

        /// <summary>
        /// End timing and log the result.
        /// </summary>
        public static void EndTimer(string operationName)
        {
            if (_timers.TryGetValue(operationName, out var timer))
            {
                timer.Stop();
                UnityEngine.Debug.Log($"[Performance] {operationName}: {timer.ElapsedMilliseconds}ms");
            }
        }

        /// <summary>
        /// Log scene generation statistics.
        /// </summary>
        public static void LogSceneStats(int objectCount, int materialCount, int meshCount)
        {
            UnityEngine.Debug.Log($"[Performance] Scene Stats - Objects: {objectCount}, Materials: {materialCount}, Meshes: {meshCount}");
            
            // Warn if material count is too high (breaks instancing)
            if (materialCount > 50)
            {
                UnityEngine.Debug.LogWarning($"[Performance] High material count ({materialCount}) may reduce GPU instancing performance.");
            }
        }

        /// <summary>
        /// Monitor GPU instancing effectiveness.
        /// </summary>
        public static void CheckInstancing(Material material, int userCount)
        {
            if (material != null && material.enableInstancing)
            {
                _instanceCount += userCount;
                if (_instanceCount % 100 == 0)
                {
                    UnityEngine.Debug.Log($"[Performance] GPU Instancing: {_instanceCount} instances rendered with shared materials.");
                }
            }
        }

        /// <summary>
        /// Reset all counters.
        /// </summary>
        public static void Reset()
        {
            _drawCallCount = 0;
            _instanceCount = 0;
            _timers.Clear();
        }
    }
}
