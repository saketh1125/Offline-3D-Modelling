using UnityEngine;

namespace ThreeDBuilder.Runtime
{
    /// <summary>
    /// Computes encapsulating bounds of a procedural scene and positions the camera
    /// to frame all content with cinematic readability.
    ///
    /// Called by SceneBuilder after all objects are spawned, but ONLY if the scene
    /// JSON did not specify explicit camera data (we respect intentional placements).
    ///
    /// Camera is offset diagonally (azimuth ~22°) so objects show depth and shadow
    /// contrast rather than appearing flat when viewed straight-on.
    /// </summary>
    public static class SceneBoundsFramer
    {
        // Margin multiplier — 1.15 keeps content filling the frame without edge crowding
        private const float FRAME_MARGIN = 1.15f;  // was 1.3

        // Camera distance clamp (mobile-safe range)
        private const float MIN_CAMERA_DISTANCE =  5f;  // was 8
        private const float MAX_CAMERA_DISTANCE = 80f;  // was 100

        // Elevation angle relative to scene center (degrees)
        private const float MIN_ELEVATION_ANGLE = 20f;
        private const float MAX_ELEVATION_ANGLE = 35f;

        // Diagonal azimuth offset for cinematic depth (degrees)
        private const float AZIMUTH_OFFSET_DEG = 22f;

        // Fallback values when bounds are degenerate
        private static readonly Vector3    FALLBACK_POSITION = new Vector3(0f, 12f, -20f);
        private static readonly Quaternion FALLBACK_ROTATION = Quaternion.Euler(20f, 0f, 0f);

        /// <summary>
        /// Frames the camera to fit all renderers under the scene root.
        /// Automatically calculates optimal distance and applies a cinematic diagonal angle.
        /// </summary>
        public static void FrameCamera(Camera cam, GameObject sceneRoot)
        {
            if (cam == null || sceneRoot == null)
            {
                Debug.LogWarning("[SceneBoundsFramer] Camera or sceneRoot is null. Skipping.");
                return;
            }

            Bounds sceneBounds = ComputeSceneBounds(sceneRoot);

            if (sceneBounds.size == Vector3.zero)
            {
                cam.transform.position = FALLBACK_POSITION;
                cam.transform.rotation = FALLBACK_ROTATION;

                TouchOrbitCamera touchCam = cam.GetComponent<TouchOrbitCamera>();
                if (touchCam != null) touchCam.SetTarget(Vector3.zero, 20f);

                Debug.Log("[SceneBoundsFramer] Degenerate bounds — using fallback position.");
                return;
            }

            FrameCameraToFitBounds(cam, sceneBounds);
        }

        /// <summary>
        /// Computes an encapsulating Bounds from all Renderer components under root.
        /// </summary>
        public static Bounds ComputeSceneBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.zero);

            Bounds bounds = new Bounds();
            bool initialized = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || renderers[i].bounds.size == Vector3.zero)
                    continue;

                if (!initialized) { bounds = renderers[i].bounds; initialized = true; }
                else              { bounds.Encapsulate(renderers[i].bounds); }
            }

            return initialized ? bounds : new Bounds(Vector3.zero, Vector3.zero);
        }

        // ─────────────────────────────────────────────────────────────────
        // Internal
        // ─────────────────────────────────────────────────────────────────

        private static void FrameCameraToFitBounds(Camera cam, Bounds bounds)
        {
            Vector3 center = bounds.center;
            float   radius = bounds.extents.magnitude;

            // ── Required distance from scene center ─────────────────────────
            float fov               = cam.fieldOfView;
            float halfFovRad        = fov * 0.5f * Mathf.Deg2Rad;
            float halfHorizFovRad   = Mathf.Atan(Mathf.Tan(halfFovRad) * cam.aspect);
            float effectiveHalfFov  = Mathf.Min(halfFovRad, halfHorizFovRad);

            float distance = (radius * FRAME_MARGIN) / Mathf.Sin(effectiveHalfFov);
            distance = Mathf.Clamp(distance, MIN_CAMERA_DISTANCE, MAX_CAMERA_DISTANCE);

            // ── Dynamic elevation (larger scenes → lower angle) ─────────────
            float sceneSizeRatio  = Mathf.InverseLerp(10f, 100f, radius);
            float elevation       = Mathf.Lerp(MAX_ELEVATION_ANGLE, MIN_ELEVATION_ANGLE, sceneSizeRatio);

            // ── Diagonal azimuth offset for cinematic depth ─────────────────
            float azimuthRad  = AZIMUTH_OFFSET_DEG * Mathf.Deg2Rad;
            float elevRad     = elevation           * Mathf.Deg2Rad;

            // Camera offset: rotate around Y by azimuth, then tilt by elevation
            Vector3 forward      = new Vector3(Mathf.Sin(azimuthRad), 0f, -Mathf.Cos(azimuthRad)).normalized;
            Vector3 cameraOffset = new Vector3(
                forward.x  * Mathf.Cos(elevRad) * distance,
                Mathf.Sin(elevRad) * distance,
                forward.z  * Mathf.Cos(elevRad) * distance);

            cam.transform.position = center + cameraOffset;
            cam.transform.LookAt(center);

            // Expand far clip plane to cover the full scene
            float requiredFarClip = distance + radius * 2f;
            if (cam.farClipPlane < requiredFarClip)
                cam.farClipPlane = requiredFarClip + 50f;

            // ── Sync with TouchOrbitCamera ──────────────────────────────────
            TouchOrbitCamera touchCam = cam.GetComponent<TouchOrbitCamera>();
            if (touchCam != null)
            {
                touchCam.SetTarget(center, distance);
                touchCam.ApplyConfig(new CameraConfig
                {
                    target    = new float[] { center.x, center.y, center.z },
                    distance  = distance,
                    elevation = cam.transform.eulerAngles.x
                });
            }

            Debug.Log($"[SceneBoundsFramer] Framed: center={center}, dist={distance:F1}, " +
                      $"radius={radius:F1}, elev={elevation:F1}°, azimuth={AZIMUTH_OFFSET_DEG}°, " +
                      $"farClip={cam.farClipPlane:F0}");
        }
    }
}
