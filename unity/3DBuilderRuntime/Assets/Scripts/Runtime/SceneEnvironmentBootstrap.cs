using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace ThreeDBuilder.Runtime
{
    /// <summary>
    /// Configures a professional baseline environment when the runtime initializes.
    /// Called from SceneBuilder before building each scene.
    ///
    /// Sets up:
    ///   • Primary directional light (warm, soft shadows)
    ///   • Fill light      (cool-sky hemisphere, shadows off — softens harsh contrast)
    ///   • Ambient trilight
    ///   • Fog
    ///   • Skybox (procedural)
    ///   • Ground plane    (warm sandstone tone, subtle sheen)
    ///   • Shadow cascade settings (mobile-safe)
    ///   • Default camera position
    ///
    /// CLEANUP: always destroys existing light / ground / probe objects before
    /// recreating, so scene reloads never accumulate duplicates.
    /// The _hasRun guard has been removed — each BuildScene call gets fresh visuals.
    /// </summary>
    public static class SceneEnvironmentBootstrap
    {
        // ─────────────────────────────────────────────────────────────────
        // Well-known object names (used for cleanup)
        // ─────────────────────────────────────────────────────────────────

        public const string PRIMARY_LIGHT_NAME = "MainDirectionalLight";
        public const string FILL_LIGHT_NAME    = "FillLight";
        public const string GROUND_NAME        = "GroundPlane";

        // ─────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────

        public static void Setup()
        {
            Setup(SceneConfig.GetDefaults());
        }

        public static void Setup(SceneConfig config)
        {
            if (config == null)
                config = SceneConfig.GetDefaults();

            // Destroy any previous environment objects to avoid duplicates
            CleanupEnvironment();

            // Lighting
            if (config.lighting != null)
                SetupDirectionalLight(config.lighting);

            SetupFillLight();
            SetupAmbientLighting();

            // Environment components
            if (config.environment != null)
            {
                SetupFog(config.environment.fog);
                SetupSkybox(config.environment);   // FIX: pass EnvironmentConfig instead of SkyboxConfig
                SetupGroundPlane(config.environment.ground);
            }
            else
            {
                // Safe fallback
                SetupFog(null);
                SetupSkybox(null);
                SetupGroundPlane(null);
            }

            // Shadow configuration
            SetupShadowSettings();

            // Camera
            if (config.camera != null)
                SetupDefaultCamera(config.camera);

            // Disable real-time GI — not needed on mobile and costs CPU/GPU.

#if UNITY_EDITOR
    // Disable realtime GI in editor (mobile optimization)
    UnityEditor.Lightmapping.realtimeGI = false;
#endif

    Debug.Log("[SceneEnvironmentBootstrap] Environment setup complete.");
}

        /// <summary>
        /// Destroys all known environment objects so they can be recreated cleanly.
        /// </summary>
        public static void CleanupEnvironment()
        {
            DestroyNamed(PRIMARY_LIGHT_NAME);
            DestroyNamed(FILL_LIGHT_NAME);
            DestroyNamed(GROUND_NAME);
            // Reflection probe is managed by ReflectionProbeBootstrap.
            ReflectionProbeBootstrap.Cleanup();
        }

        /// <summary>
        /// Alias for compatibility with callers using Reset().
        /// </summary>
        public static void Reset()
        {
            CleanupEnvironment();
        }

        // ─────────────────────────────────────────────────────────────────
        // Primary Directional Light
        // ─────────────────────────────────────────────────────────────────

        private static void SetupDirectionalLight(LightingConfig config)
        {
            if (config == null) config = SceneConfig.GetDefaults().lighting;

            if (!string.IsNullOrEmpty(config.preset))
                config = ApplyLightingPreset(config.preset, config);

            GameObject lightObj = new GameObject(PRIMARY_LIGHT_NAME);
            Light dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;

            // Rotation
            if (config.rotation != null && config.rotation.Length >= 3)
                dirLight.transform.rotation = Quaternion.Euler(config.rotation[0], config.rotation[1], config.rotation[2]);
            else if (config.direction != null && config.direction.Length >= 3)
                dirLight.transform.rotation = Quaternion.LookRotation(new Vector3(config.direction[0], config.direction[1], config.direction[2]));
            else
                dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            dirLight.intensity = config.intensity > 0 ? config.intensity : 1.2f;

            // Pure white primary light for accurate color representation
            dirLight.color = (config.color != null && config.color.Length >= 3)
                ? new Color(config.color[0], config.color[1], config.color[2])
                : Color.white;

            // Shadows
            dirLight.shadows        = LightShadows.Soft;
            dirLight.shadowStrength = config.shadowStrength > 0 ? config.shadowStrength : 0.8f;
            dirLight.shadowBias         = config.shadowBias       > 0 ? config.shadowBias       : 0.05f;
            dirLight.shadowNormalBias   = config.shadowNormalBias > 0 ? config.shadowNormalBias : 0.4f;
            dirLight.shadowNearPlane    = config.shadowNearPlane  > 0 ? config.shadowNearPlane  : 0.2f;

            Debug.Log("[SceneEnvironmentBootstrap] Professional lighting setup applied.");
        }

        // ─────────────────────────────────────────────────────────────────
        // Fill Light (cool hemisphere, no shadows)
        // ─────────────────────────────────────────────────────────────────

        private static void SetupFillLight()
        {
            GameObject fillObj = new GameObject(FILL_LIGHT_NAME);
            Light fill = fillObj.AddComponent<Light>();
            fill.type = LightType.Directional;

            // Softer angle from the opposite side to prevent harsh black shadows
            fill.transform.rotation = Quaternion.Euler(-40f, 30f, 0f);
            fill.intensity  = 0.3f;
            fill.color      = new Color(0.75f, 0.85f, 1.0f); // subtle sky blue
            fill.shadows    = LightShadows.None;               // no shadow cost
        }

        // ─────────────────────────────────────────────────────────────────
        // Lighting Presets
        // ─────────────────────────────────────────────────────────────────

        private static LightingConfig ApplyLightingPreset(string preset, LightingConfig baseConfig)
        {
            LightingConfig config = new LightingConfig
            {
                preset          = preset,
                type            = baseConfig?.type ?? "directional",
                rotation        = baseConfig?.rotation,
                direction       = baseConfig?.direction,
                color           = baseConfig?.color,
                intensity       = baseConfig?.intensity ?? 1.2f,
                shadows         = baseConfig?.shadows ?? true,
                shadowStrength  = baseConfig?.shadowStrength ?? 0.75f,
                shadowBias      = baseConfig?.shadowBias ?? 0.05f,
                shadowNormalBias= baseConfig?.shadowNormalBias ?? 0.4f,
                shadowNearPlane = baseConfig?.shadowNearPlane ?? 0.2f
            };

            switch (preset.ToLower())
            {
                case "studio":
                    config.rotation  = new float[] { 60f, -45f, 0f };
                    config.color     = new float[] { 1.0f, 1.0f, 1.0f };
                    config.intensity = 1.5f;
                    config.shadowStrength = 1.0f;
                    RenderSettings.ambientMode  = AmbientMode.Flat;
                    RenderSettings.ambientLight = new Color(0.4f, 0.4f, 0.45f);
                    break;

                case "sunset":
                    config.rotation  = new float[] { 15f, -75f, 0f };
                    config.color     = new float[] { 1.0f, 0.7f, 0.4f };
                    config.intensity = 1.8f;
                    config.shadowStrength = 0.8f;
                    RenderSettings.ambientMode         = AmbientMode.Trilight;
                    RenderSettings.ambientSkyColor     = new Color(0.9f, 0.6f, 0.4f);
                    RenderSettings.ambientEquatorColor = new Color(0.5f, 0.4f, 0.3f);
                    RenderSettings.ambientGroundColor  = new Color(0.3f, 0.2f, 0.2f);
                    break;

                case "outdoor":
                    config.rotation  = new float[] { 45f, -30f, 0f };
                    config.color     = new float[] { 1.0f, 0.95f, 0.80f };
                    config.intensity = 1.3f;
                    config.shadowStrength = 0.95f;
                    RenderSettings.ambientMode         = AmbientMode.Trilight;
                    RenderSettings.ambientSkyColor     = new Color(0.7f, 0.8f, 0.9f);
                    RenderSettings.ambientEquatorColor = new Color(0.4f, 0.5f, 0.6f);
                    RenderSettings.ambientGroundColor  = new Color(0.2f, 0.2f, 0.15f);
                    break;

                case "museum":
                    config.rotation  = new float[] { 70f, -20f, 0f };
                    config.color     = new float[] { 1.0f, 0.98f, 0.95f };
                    config.intensity = 1.0f;
                    config.shadowStrength = 0.6f;
                    RenderSettings.ambientMode  = AmbientMode.Flat;
                    RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.65f);
                    break;

                default:
                    Debug.LogWarning($"[SceneEnvironmentBootstrap] Unknown preset: {preset}");
                    break;
            }

            Debug.Log($"[SceneEnvironmentBootstrap] Applied preset: {preset}");
            return config;
        }

        // ─────────────────────────────────────────────────────────────────
        // Ambient Lighting (trilight gradient)
        // ─────────────────────────────────────────────────────────────────

        private static void SetupAmbientLighting()
        {
            RenderSettings.ambientMode         = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor     = new Color(0.65f, 0.75f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.45f, 0.50f);
            RenderSettings.ambientGroundColor  = new Color(0.25f, 0.22f, 0.20f);
        }

        // ─────────────────────────────────────────────────────────────────
        // Fog
        // ─────────────────────────────────────────────────────────────────

        private static void SetupFog(FogConfig config)
        {
            if (config == null) config = SceneConfig.GetDefaults().environment.fog;

            if (!config.enabled) { RenderSettings.fog = false; return; }

            RenderSettings.fog = true;

            switch (config.mode?.ToLowerInvariant())
            {
                case "exp":
                    RenderSettings.fogMode    = FogMode.Exponential;
                    RenderSettings.fogDensity = config.density > 0 ? config.density : 0.02f;
                    break;
                case "exp2":
                    RenderSettings.fogMode    = FogMode.ExponentialSquared;
                    RenderSettings.fogDensity = config.density > 0 ? config.density : 0.02f;
                    break;
                default:
                    RenderSettings.fogMode          = FogMode.Linear;
                    RenderSettings.fogStartDistance = config.start > 0 ? config.start : 30f;
                    RenderSettings.fogEndDistance   = config.end   > 0 ? config.end   : 120f;
                    break;
            }

            RenderSettings.fogColor = (config.color != null && config.color.Length >= 3)
                ? new Color(config.color[0], config.color[1], config.color[2])
                : new Color(0.75f, 0.82f, 0.90f);
        }

        // ─────────────────────────────────────────────────────────────────
        // Skybox
        // ─────────────────────────────────────────────────────────────────

        private static void SetupSkybox(EnvironmentConfig envConfig)
        {
            SkyboxConfig config = envConfig?.skybox;
            if (config == null) config = SceneConfig.GetDefaults().environment.skybox;

            // Apply background color to camera clear flags if provided
            if (envConfig?.backgroundColor != null && envConfig.backgroundColor.Length >= 3)
            {
                Color bgColor = new Color(envConfig.backgroundColor[0], envConfig.backgroundColor[1], envConfig.backgroundColor[2]);
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    mainCam.backgroundColor = bgColor;
                    mainCam.clearFlags = CameraClearFlags.SolidColor;
                }
            }

            switch (config?.type?.ToLower())
            {
                case "procedural":
                    RenderSettings.skybox    = CreateProceduralSkybox(config);
                    RenderSettings.ambientMode = AmbientMode.Skybox;
                    Camera.main.clearFlags = CameraClearFlags.Skybox;
                    break;

                case "color":
                    Color skyColor = (config.color != null && config.color.Length >= 3)
                        ? new Color(config.color[0], config.color[1], config.color[2])
                        : (envConfig?.backgroundColor != null && envConfig.backgroundColor.Length >= 3)
                            ? new Color(envConfig.backgroundColor[0], envConfig.backgroundColor[1], envConfig.backgroundColor[2])
                            : new Color(0.55f, 0.7f, 0.9f);
                    
                    RenderSettings.ambientMode  = AmbientMode.Flat;
                    RenderSettings.ambientLight = skyColor * 0.7f;
                    RenderSettings.fogColor     = skyColor;
                    RenderSettings.skybox       = null;
                    if (Camera.main != null)
                    {
                        Camera.main.backgroundColor = skyColor;
                        Camera.main.clearFlags      = CameraClearFlags.SolidColor;
                    }
                    break;

                default:
                    RenderSettings.ambientMode = AmbientMode.Trilight;
                    break;
            }
        }

        private static Material CreateProceduralSkybox(SkyboxConfig config)
        {
            Material skybox = new Material(Shader.Find("Skybox/Procedural"));

            skybox.SetFloat("_SunSize",           config.sunSize           > 0 ? config.sunSize           : 0.04f);
            skybox.SetFloat("_SunSizeConvergence", config.sunSizeConvergence > 0 ? config.sunSizeConvergence : 5f);
            skybox.SetFloat("_AtmosphereThickness",config.atmosphereThickness > 0 ? config.atmosphereThickness: 1.2f);
            skybox.SetFloat("_Exposure",           config.exposure          > 0 ? config.exposure          : 1.3f);

            if (config.skyTint != null && config.skyTint.Length >= 3)
            {
                Color tint = Color.Lerp(new Color(config.skyTint[0], config.skyTint[1], config.skyTint[2]), Color.white, 0.2f);
                skybox.SetColor("_SkyTint", tint);
            }
            else
            {
                skybox.SetColor("_SkyTint", new Color(0.7f, 0.85f, 1.0f));
            }

            skybox.SetColor("_GroundColor",
                (config.groundColor != null && config.groundColor.Length >= 3)
                    ? new Color(config.groundColor[0], config.groundColor[1], config.groundColor[2])
                    : new Color(0.2f, 0.15f, 0.1f));

            return skybox;
        }

        // ─────────────────────────────────────────────────────────────────
        // Ground Plane (warm sandstone, subtle sheen)
        // ─────────────────────────────────────────────────────────────────

        private static void SetupGroundPlane(GroundConfig config)
        {
            if (config == null) config = SceneConfig.GetDefaults().environment.ground;

            // Destroy existing ground plane if it exists.
            DestroyNamed(GROUND_NAME);

            if (!config.enabled) return;

            GameObject ground = new GameObject(GROUND_NAME);
            GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Plane);
            primitive.transform.SetParent(ground.transform);
            primitive.transform.localPosition = Vector3.zero;
            ground.transform.position = Vector3.zero;

            // Unity's Plane is 10×10 units at scale 1.
            if (config.size != null && config.size.Length >= 3)
                ground.transform.localScale = new Vector3(config.size[0] / 10f, 1f, config.size[2] / 10f);
            else
                ground.transform.localScale = new Vector3(20f, 1f, 20f);

            // Remove collider — visual only
            Collider col = primitive.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            // Material setup.
            Shader standard = Shader.Find("Standard");
            if (standard != null)
            {
                Material mat = new Material(standard);
                mat.name = "GroundMaterial";

                mat.color = (config.color != null && config.color.Length >= 3)
                    ? new Color(config.color[0], config.color[1], config.color[2])
                    : new Color(0.48f, 0.46f, 0.44f);

                mat.SetFloat("_Metallic",   config.metallic);
                mat.SetFloat("_Glossiness", config.glossiness);
                mat.enableInstancing = true;

                MeshRenderer renderer = primitive.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial    = mat;
                    renderer.receiveShadows    = true;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }

            ApplyGroundVertexDarkening(primitive);
            Debug.Log("[SceneEnvironmentBootstrap] Ground plane created.");
        }

        private static void ApplyGroundVertexDarkening(GameObject ground)
        {
            MeshFilter mf = ground.GetComponent<MeshFilter>();
            if (mf == null || mf.mesh == null) return;

            Mesh mesh = mf.mesh;
            Vector3[] vertices = mesh.vertices;
            Color[]   colors   = new Color[vertices.Length];
            Bounds    bounds   = mesh.bounds;
            float     maxDist  = Mathf.Max(bounds.extents.x, bounds.extents.z);

            for (int i = 0; i < vertices.Length; i++)
            {
                float dist       = new Vector2(vertices[i].x, vertices[i].z).magnitude / maxDist;
                float brightness = Mathf.Lerp(0.85f, 1.05f, Mathf.Pow(dist, 0.6f));
                colors[i] = new Color(brightness, brightness, brightness, 1f);
            }

            mesh.colors = colors;
        }

        // ─────────────────────────────────────────────────────────────────
        // Shadow Quality (mobile-safe)
        // ─────────────────────────────────────────────────────────────────

        private static void SetupShadowSettings()
        {
            QualitySettings.shadows           = ShadowQuality.All;
            QualitySettings.shadowResolution  = ShadowResolution.High;
            QualitySettings.shadowDistance    = 60f;
            QualitySettings.shadowCascades    = 2;
            QualitySettings.shadowProjection  = ShadowProjection.StableFit;
        }

        // ─────────────────────────────────────────────────────────────────
        // Default Camera
        // ─────────────────────────────────────────────────────────────────

        private static void SetupDefaultCamera(CameraConfig config)
        {
            Camera cam = Camera.main;
            if(cam != null)
            {
                cam.transform.position = new Vector3(0f, 8f, -20f);
                cam.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
                
                cam.farClipPlane  = 500f;
                cam.nearClipPlane = 0.3f;
            }

            Debug.Log("[Camera Diagnostic] Forced camera position applied.");
        }

        // ─────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────

        private static void DestroyNamed(string name)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null) Object.Destroy(obj);
        }
    }
}
