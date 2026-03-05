using System;
using System.Collections.Generic;

namespace ThreeDBuilder.Scene
{
    /// <summary>
    /// Represents a parsed JSON scene in strongly-typed runtime classes
    /// before building Unity GameObjects.
    /// </summary>
    [Serializable]
    public class SceneModel
    {
        public string schema_version;
        public EnvironmentModel environment;    // Optional global environment settings
        public CameraModel camera;              // Optional camera settings
        public LightingModel lighting;          // Optional main lighting settings
        public List<MaterialModel> materials;
        public List<ObjectModel> objects;
    }

    [Serializable]
    public class EnvironmentModel
    {
        public float[] backgroundColor;
    }

    [Serializable]
    public class CameraModel
    {
        public float[] position;
        public float[] lookAt;
        // Optionally float fov, but let's stick to standard payload
    }

    [Serializable]
    public class LightingModel
    {
        public string type;         // "directional", "point", etc.
        public float[] color;
        public float intensity;
        public float[] direction;   // For directional lights
    }

    [Serializable]
    public class MaterialModel
    {
        public string id;
        public float[] baseColor;
    }

    [Serializable]
    public class ObjectModel
    {
        public string id;
        public string primitive;
        public string materialRef;
        public TransformModel transform;
        public RepeatModel repeat; // Optional
    }

    [Serializable]
    public class RepeatModel
    {
        public int[] grid;     // [columns, rows]
        public float[] spacing; // [x_spacing, z_spacing]
    }

    [Serializable]
    public class TransformModel
    {
        public float[] position;
        public float[] rotation;
        public float[] scale;
    }
}
