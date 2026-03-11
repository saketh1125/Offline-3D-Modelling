using UnityEngine;
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
        public SceneTemplateModel sceneTemplate; // Optional scene template
        public CityModel city;                  // Optional procedural city parameters
        public List<MaterialModel> materials;
        public List<ObjectModel> objects;
    }

    [Serializable]
    public class EnvironmentModel
    {
        public float[] backgroundColor;
        public float[] skyColor;        // Alias for background/sky color
        public SkyboxModel skybox;
        public FogModel fog;
        public GroundModel ground;
        public LightingModel lighting;   // Moved/Duplicated for specific schema overrides
    }

    [Serializable]
    public class CityModel
    {
        public int[] grid_size;            // [columns, rows]
        public float block_spacing;        // Distance between blocks
        public float[] building_height_range; // [minHeight, maxHeight]
        public float building_width;       // Width/depth of a building footprint
        public float road_width;           // Width of the road separating blocks
    }

    [Serializable]
    public class CameraModel
    {
        public string mode;        // "orbit", "static"
        public float[] position;
        public float[] rotation;   // Euler angles for static mode
        public float[] lookAt;
        public float[] target;     // For orbit mode
        public float distance;     // For orbit mode
        public float elevation;    // For orbit mode
    }

    [Serializable]
    public class LightingModel
    {
        public string preset;      // "studio", "sunset", "outdoor", "museum"
        public string type;         // "directional", "point", etc.
        public float[] rotation;    // For directional lights (Euler angles)
        public float[] direction;   // Alternative to rotation for directional lights
        public float[] color;
        public float intensity;
        public bool shadows;
        public float shadowStrength;
        public float shadowBias;
        public float shadowNormalBias;
        public float shadowNearPlane;
    }

    [Serializable]
    public class MaterialModel
    {
        public string id;
        public float[] baseColor;

        // PBR overrides — optional.
        // Values < 0 mean "use ProfessionalMaterialFactory default".
        public float metallic   = -1f;
        public float smoothness = -1f;
    }

    [Serializable]
    public class ObjectModel
    {
        public string id;
        public string primitive;
        public string materialRef;
        public TransformModel transform;
        public RepeatModel repeat; // Optional
        public StructureModel structure; // Optional for procedural generation
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

    [Serializable]
    public class SkyboxModel
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
    public class FogModel
    {
        public bool enabled;
        public string mode; // "linear", "exp", "exp2"
        public float density;
        public float start;
        public float end;
        public float[] color;
    }

    [Serializable]
    public class GroundModel
    {
        public bool enabled;
        public float[] size;
        public float[] color;
        public float metallic;
        public float glossiness;
        public float smoothness; // Alias for glossiness
    }

    [Serializable]
    public class StructureModel
    {
        public string type; // "grid", "circle", "radial", "line", "spiral"
        
        // Grid parameters
        public int columns;
        public int rows;
        
        // Circle/Radial parameters
        public int count;
        public float radius;
        
        // Line parameters
        public float spacing;
        public float[] direction;
        
        // Spiral parameters
        public float height;
    }

    [Serializable]
    public class SceneTemplateModel
    {
        public string type; // "temple", "solar_system", "neural_network", "dna_helix", "city_grid"
        public TemplateParamsModel parameters;
    }

    [Serializable]
    public class TemplateParamsModel
    {
        // Common parameters
        public float radius;
        public float height;
        public int pairs;
        public int layers;
        public int count;
        public int planetCount;      // New field
        public int nodesPerLayer;    // New field
        public float spacing;
        public float orbitSpacing;   // New field
        public float orbitRadius;
        public float platformSize;
        
        // Legacy field mappings (for backward compatibility)
        public int pillars => count; 
        public int planets => planetCount > 0 ? planetCount : count;
        public int neuronsPerLayer => nodesPerLayer > 0 ? nodesPerLayer : count;
        public int gridSize => (int)Mathf.Sqrt(count);
        public float blockSpacing => orbitSpacing > 0 ? orbitSpacing : spacing;
    }
}
