using System;
using UnityEngine;

namespace ThreeDBuilder.Protocol
{
    /// <summary>
    /// Represents an incoming command envelope from Flutter.
    /// 
    /// Wire format:
    /// {
    ///   "protocol_version": "1.0",
    ///   "command": "load_scene",
    ///   "request_id": "load_scene-1-1708123456789",
    ///   "payload": "{ ... }"
    /// }
    /// 
    /// Note: Unity's JsonUtility does not support nested objects as Map/Dictionary,
    /// so 'payload' is kept as a raw JSON string to be parsed separately by handlers.
    /// </summary>
    [Serializable]
    public class CommandEnvelope
    {
        // Fields use snake_case to match the JSON wire format.
        // JsonUtility maps field names directly to JSON keys.

        public string protocol_version;
        public string command;
        public string request_id;
        public string payload;

        /// <summary>
        /// Deserializes a JSON string into a CommandEnvelope.
        /// Throws ArgumentException if the JSON is malformed.
        /// </summary>
        public static CommandEnvelope FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("Command JSON is null or empty.");

            try
            {
                return JsonUtility.FromJson<CommandEnvelope>(json);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Failed to parse command envelope: {e.Message}", e);
            }
        }

        /// <summary>
        /// Validates the envelope against Protocol v1.0 rules.
        /// Returns a validation error message, or null if valid.
        /// </summary>
        public string Validate()
        {
            // 1. Protocol version
            if (string.IsNullOrEmpty(protocol_version))
                return "Missing required field: protocol_version";

            if (!ProtocolVersion.IsSupported(protocol_version))
                return $"Unsupported protocol version: {protocol_version} (expected: {ProtocolVersion.V1})";

            // 2. Command type
            if (string.IsNullOrEmpty(command))
                return "Missing required field: command";

            if (!ProtocolEnumExtensions.TryParseCommand(command, out _))
                return $"Unknown command: {command}";

            // 3. Request ID
            if (string.IsNullOrEmpty(request_id))
                return "Missing required field: request_id";

            return null; // Valid
        }

        /// <summary>
        /// Returns the parsed EngineCommand enum value.
        /// Call Validate() first to ensure this is safe.
        /// </summary>
        public EngineCommand GetCommand()
        {
            if (ProtocolEnumExtensions.TryParseCommand(command, out var cmd))
                return cmd;

            throw new InvalidOperationException($"Cannot parse command: {command}. Call Validate() first.");
        }

        public override string ToString()
        {
            return $"CommandEnvelope(v={protocol_version}, cmd={command}, id={request_id})";
        }
    }
}
