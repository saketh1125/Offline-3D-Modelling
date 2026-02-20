using System;
using UnityEngine;

namespace ThreeDBuilder.Protocol
{
    /// <summary>
    /// Represents an outgoing event envelope from Unity → Flutter.
    /// 
    /// Wire format:
    /// {
    ///   "protocol_version": "1.0",
    ///   "event": "scene_ready",
    ///   "request_id": "load_scene-1-1708123456789",
    ///   "payload": "{ ... }"
    /// }
    /// </summary>
    [Serializable]
    public class EventEnvelope
    {
        // Fields use snake_case to match the JSON wire format.

        public string protocol_version;
        [SerializeField] public string @event; // 'event' is a C# keyword, escaped with @
        public string request_id;
        public string payload;

        /// <summary>
        /// Creates a new event envelope with the current protocol version.
        /// </summary>
        /// <param name="eventType">The event type to emit.</param>
        /// <param name="requestId">The originating request ID (nullable for unsolicited events).</param>
        /// <param name="payload">Optional JSON payload string.</param>
        public static EventEnvelope Create(
            EngineEventType eventType,
            string requestId = null,
            string payload = null)
        {
            return new EventEnvelope
            {
                protocol_version = ProtocolVersion.V1,
                @event = eventType.ToWireValue(),
                request_id = requestId ?? "",
                payload = payload ?? ""
            };
        }

        /// <summary>
        /// Serializes this envelope to a JSON string for transport.
        /// </summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public override string ToString()
        {
            return $"EventEnvelope(v={protocol_version}, event={@event}, id={request_id})";
        }
    }
}
