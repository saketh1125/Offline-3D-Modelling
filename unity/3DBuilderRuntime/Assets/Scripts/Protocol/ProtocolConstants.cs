using System;

namespace ThreeDBuilder.Protocol
{
    /// <summary>
    /// Supported protocol versions for Flutter ↔ Unity communication.
    /// </summary>
    public static class ProtocolVersion
    {
        public const string V1 = "1.0";

        /// <summary>
        /// Returns true if the given version string is a supported protocol version.
        /// </summary>
        public static bool IsSupported(string version)
        {
            return version == V1;
        }
    }

    /// <summary>
    /// Commands sent from Flutter → Unity.
    /// Wire values use snake_case to match the Dart-side protocol constants.
    /// </summary>
    public enum EngineCommand
    {
        Initialize,
        LoadScene,
        ClearScene,
        Dispose
    }

    /// <summary>
    /// Events emitted from Unity → Flutter.
    /// Wire values use snake_case to match the Dart-side protocol constants.
    /// </summary>
    public enum EngineEventType
    {
        Initialized,
        SceneLoading,
        SceneReady,
        Error,
        PerformanceStats
    }

    /// <summary>
    /// Conversion helpers between enum values and their snake_case wire strings.
    /// </summary>
    public static class ProtocolEnumExtensions
    {
        // ── EngineCommand ────────────────────────────────────────────────

        public static string ToWireValue(this EngineCommand command)
        {
            switch (command)
            {
                case EngineCommand.Initialize:  return "initialize";
                case EngineCommand.LoadScene:    return "load_scene";
                case EngineCommand.ClearScene:   return "clear_scene";
                case EngineCommand.Dispose:      return "dispose";
                default:
                    throw new ArgumentOutOfRangeException(nameof(command), command, "Unknown command");
            }
        }

        /// <summary>
        /// Parses a wire-format string to an EngineCommand.
        /// Returns false if the value is unknown.
        /// </summary>
        public static bool TryParseCommand(string wireValue, out EngineCommand command)
        {
            switch (wireValue)
            {
                case "initialize":  command = EngineCommand.Initialize;  return true;
                case "load_scene":  command = EngineCommand.LoadScene;   return true;
                case "clear_scene": command = EngineCommand.ClearScene;  return true;
                case "dispose":     command = EngineCommand.Dispose;     return true;
                default:            command = default;                    return false;
            }
        }

        // ── EngineEventType ──────────────────────────────────────────────

        public static string ToWireValue(this EngineEventType eventType)
        {
            switch (eventType)
            {
                case EngineEventType.Initialized:      return "initialized";
                case EngineEventType.SceneLoading:     return "scene_loading";
                case EngineEventType.SceneReady:       return "scene_ready";
                case EngineEventType.Error:            return "error";
                case EngineEventType.PerformanceStats: return "performance_stats";
                default:
                    throw new ArgumentOutOfRangeException(nameof(eventType), eventType, "Unknown event type");
            }
        }
    }
}
