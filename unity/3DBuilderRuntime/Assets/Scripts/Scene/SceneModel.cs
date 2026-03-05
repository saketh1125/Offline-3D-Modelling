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
        public List<MaterialModel> materials;
        public List<ObjectModel> objects;
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
    }

    [Serializable]
    public class TransformModel
    {
        public float[] position;
        public float[] rotation;
        public float[] scale;
    }
}
