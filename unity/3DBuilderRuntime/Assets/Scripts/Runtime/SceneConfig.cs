using System;
using UnityEngine;

namespace ThreeDBuilder.Runtime
{
    /// <summary>
    /// Configuration data class for scene environment, lighting, and camera settings.
    /// Parsed from JSON scene input to control visual context.
    /// </summary>
    [Serializable]
    public class SceneConfig
    {
        public EnvironmentConfig environment;
        public LightingConfig lighting;
        public CameraConfig camera;
        
        /// <summary>
        /// Creates a SceneConfig with default values matching the original hardcoded settings.
        /// </summary>
        public static SceneConfig GetDefaults()
        {
            return new SceneConfig
            {
                environment = new EnvironmentConfig
                {
                    backgroundColor = new float[] { 0.55f, 0.7f, 0.9f }, // default sky color
                    skybox = new SkyboxConfig
                    {
                        type = "procedural",
                        sunSize = 0.04f,
                        sunSizeConvergence = 5f,
                        atmosphereThickness = 1.0f,
                        skyTint = new float[] { 0.5f, 0.5f, 0.5f },
                        groundColor = new float[] { 0.37f, 0.35f, 0.35f },
                        exposure = 1.3f
                    },
                    fog = new FogConfig
                    {
                        enabled = false, // default as requested
                        mode = "linear",
                        density = 0.02f,
                        start = 30f,
                        end = 120f,
                        color = new float[] { 0.75f, 0.82f, 0.9f }
                    },
                    ground = new GroundConfig
                    {
                        enabled = true,
                        size = new float[] { 200f, 1f, 200f },
                        color = new float[] { 0.38f, 0.38f, 0.4f },
                        metallic = 0.0f,
                        glossiness = 0.15f,
                        smoothness = 0.15f
                    }
                },
                lighting = new LightingConfig
                {
                    type = "directional",
                    rotation = new float[] { 50f, -30f, 0f },
                    intensity = 1.2f,
                    color = new float[] { 1.0f, 0.96f, 0.9f },
                    shadows = true,
                    shadowStrength = 0.75f,
                    shadowBias = 0.05f,
                    shadowNormalBias = 0.4f,
                    shadowNearPlane = 0.2f
                },
                camera = new CameraConfig
                {
                    mode = "orbit",
                    target = new float[] { 0f, 0f, 0f },
                    distance = 18f,
                    elevation = 25f
                }
            };
        }
    }

    [Serializable]
    public class EnvironmentConfig
    {
        public SkyboxConfig skybox;
        public FogConfig fog;
        public GroundConfig ground;
        public float[] backgroundColor;
    }

    [Serializable]
    public class SkyboxConfig
    {
        public string type; // "procedural", "color", "none"
        public float[] color; // Used when type = "color"
        public float sunSize;
        public float sunSizeConvergence;
        public float atmosphereThickness;
        public float[] skyTint;
        public float[] groundColor;
        public float exposure;
    }

    [Serializable]
    public class FogConfig
    {
        public bool enabled;
        public string mode; // "linear", "exp", "exp2"
        public float density;
        public float start;
        public float end;
        public float[] color;
    }

    [Serializable]
    public class GroundConfig
    {
        public bool enabled;
        public float[] size;
        public float[] color;
        public float metallic;
        public float glossiness;
        public float smoothness;
    }

    [Serializable]
    public class LightingConfig
    {
        public string preset; // "studio", "sunset", "outdoor", "museum"
        public string type; // "directional", "point"
        public float[] rotation; // For directional lights
        public float[] direction; // Alternative to rotation
        public float intensity;
        public float[] color;
        public bool shadows;
        public float shadowStrength;
        public float shadowBias;
        public float shadowNormalBias;
        public float shadowNearPlane;
    }

    [Serializable]
    public class CameraConfig
    {
        public string mode; // "orbit", "static"
        public float[] target;
        public float distance;
        public float elevation;
        public float[] position; // For static mode
        public float[] rotation; // Euler angles for static mode
        public float[] lookAt; // For static mode
    }
}
