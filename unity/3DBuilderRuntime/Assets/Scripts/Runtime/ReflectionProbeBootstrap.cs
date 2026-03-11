using UnityEngine;
using UnityEngine.Rendering;

namespace ThreeDBuilder.Runtime
{
    /// <summary>
    /// Creates a single lightweight realtime reflection probe above the scene center.
    /// Gives Standard shader materials subtle ambient reflections from the skybox,
    /// eliminating the flat/lifeless look caused by Unity's default black reflection.
    ///
    /// Performance: single probe, 128×128 resolution, rendered via scripting only.
    /// The probe is never refreshed automatically — call Refresh() after scene build.
    /// </summary>
    public static class ReflectionProbeBootstrap
    {
        private static GameObject _probeObject;

        // Named constants for every environment object so cleanup is reliable.
        public const string PROBE_NAME = "EnvironmentReflectionProbe";

        /// <summary>
        /// Creates (or recreates) the reflection probe.
        /// Always destroys any pre-existing probe so scene reloads are clean.
        /// </summary>
        public static void Setup()
        {
            // Destroy any existing probe first — avoids duplicate probes on reload.
            Cleanup();

            _probeObject = new GameObject(PROBE_NAME);
            // Elevate probe above ground to capture scene content, not just the floor.
            _probeObject.transform.position = new Vector3(0f, 3f, 0f);

            ReflectionProbe probe = _probeObject.AddComponent<ReflectionProbe>();

            // Realtime + manual refresh only — never runs automatically every frame.
            probe.mode         = ReflectionProbeMode.Realtime;
            probe.refreshMode  = ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;

            probe.resolution    = 128;                          // upgraded from 64
            probe.size          = new Vector3(500f, 500f, 500f);
            probe.boxProjection = false;
            probe.intensity     = 0.6f;                         // was 0.8 — less aggressive
            probe.hdr           = false;                        // save memory on mobile

            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;

            // Do NOT render immediately — the scene may still be empty at this point.
            // Caller should call Refresh() after all objects are spawned.
            Debug.Log("[ReflectionProbeBootstrap] Probe created at (0,3,0). Call Refresh() after build.");
        }

        /// <summary>
        /// Re-bakes the probe after scene content is fully spawned.
        /// Safe to call multiple times; no-op if probe was not created.
        /// </summary>
        public static void Refresh()
        {
            if (_probeObject == null)
            {
                // Try to find a pre-existing probe in the scene.
                var existing = Object.FindObjectOfType<ReflectionProbe>();
                if (existing != null)
                    existing.RenderProbe();
                return;
            }

            ReflectionProbe probe = _probeObject.GetComponent<ReflectionProbe>();
            if (probe != null)
            {
                probe.RenderProbe();
                Debug.Log("[ReflectionProbeBootstrap] Reflection probe refreshed.");
            }
        }

        /// <summary>
        /// Destroys the probe object. Called before recreation to prevent duplicates.
        /// </summary>
        public static void Cleanup()
        {
            // Destroy our tracked reference.
            if (_probeObject != null)
            {
                Object.Destroy(_probeObject);
                _probeObject = null;
            }

            // Also destroy any orphan probe from previous sessions.
            GameObject orphan = GameObject.Find(PROBE_NAME);
            if (orphan != null) Object.Destroy(orphan);
        }

        /// <summary>
        /// Full reset: cleanup + clear flag. Compatible with ReflectionProbeBootstrap.Reset() callers.
        /// </summary>
        public static void Reset()
        {
            Cleanup();
        }
    }
}
