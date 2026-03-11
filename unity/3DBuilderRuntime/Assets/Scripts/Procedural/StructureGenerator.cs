using UnityEngine;
using System.Collections.Generic;
using ThreeDBuilder.Geometry;
using ThreeDBuilder.Materials;
using ThreeDBuilder.Procedural;
using ThreeDBuilder.Scene;

namespace ThreeDBuilder.Procedural
{
    /// <summary>
    /// Generates complex spatial structures from JSON configuration.
    /// Supports grid, circle, radial, line, and spiral layouts with GPU instancing and performance optimization.
    /// </summary>
    public class StructureGenerator
    {
        private readonly ProfessionalMaterialFactory _materialFactory;
        private readonly Dictionary<string, Mesh> _meshCache;

        public StructureGenerator(ProfessionalMaterialFactory materialFactory, Dictionary<string, Mesh> meshCache)
        {
            _materialFactory = materialFactory;
            _meshCache = meshCache;
        }

        public Dictionary<string, Mesh> MeshCache => _meshCache;

        /// <summary>
        /// Generates a structure based on the object model configuration.
        /// Returns list of instantiated GameObjects.
        /// </summary>
        public List<GameObject> GenerateStructure(ObjectModel objModel, GameObject parent, Dictionary<string, Material> materialLookup)
        {
            if (objModel.structure == null)
            {
                Debug.LogWarning($"StructureGenerator: No structure configuration found for object '{objModel.id}'");
                return new List<GameObject>();
            }

            string structureType = objModel.structure.type?.ToLowerInvariant();
            if (string.IsNullOrEmpty(structureType))
            {
                Debug.LogWarning($"StructureGenerator: Invalid structure type for object '{objModel.id}'");
                return new List<GameObject>();
            }

            // Get or create the mesh for this structure
            Mesh meshAsset = GetOrCreateMesh(objModel.primitive);
            if (meshAsset == null)
            {
                Debug.LogWarning($"StructureGenerator: Failed to create mesh for primitive '{objModel.primitive}'");
                return new List<GameObject>();
            }

            // Get material
            Material material = GetMaterial(objModel.materialRef, materialLookup);

            switch (structureType)
            {
                case "grid":
                    return GenerateGrid(objModel, meshAsset, material, parent);
                case "circle":
                    return GenerateCircle(objModel, meshAsset, material, parent);
                case "radial":
                    return GenerateRadial(objModel, meshAsset, material, parent);
                case "line":
                    return GenerateLine(objModel, meshAsset, material, parent);
                case "spiral":
                    return GenerateSpiral(objModel, meshAsset, material, parent);
                default:
                    Debug.LogWarning($"StructureGenerator: Unsupported structure type '{structureType}' for object '{objModel.id}'");
                    return new List<GameObject>();
            }
        }

        private List<GameObject> GenerateGrid(ObjectModel objModel, Mesh meshAsset, Material material, GameObject parent)
        {
            List<GameObject> objects = new List<GameObject>();
            
            int columns = objModel.structure.columns > 0 ? Mathf.Clamp(objModel.structure.columns, 1, 200) : 5;
            int rows = objModel.structure.rows > 0 ? Mathf.Clamp(objModel.structure.rows, 1, 200) : 5;
            float spacing = objModel.structure.spacing > 0 ? objModel.structure.spacing : 3f;

            Vector3 baseScale = GetScale(objModel);
            Vector3 basePosition = GetPosition(objModel);

            for (int x = 0; x < columns; x++)
            {
                for (int z = 0; z < rows; z++)
                {
                    Vector3 position = basePosition + new Vector3(x * spacing, 0, z * spacing);
                    GameObject instance = CreateInstance(objModel.id, x, z, position, baseScale, meshAsset, material, parent);
                    if (instance != null)
                    {
                        objects.Add(instance);
                    }
                }
            }

            return objects;
        }

        private List<GameObject> GenerateCircle(ObjectModel objModel, Mesh meshAsset, Material material, GameObject parent)
        {
            List<GameObject> objects = new List<GameObject>();
            
            int count = objModel.structure.count > 0 ? Mathf.Clamp(objModel.structure.count, 1, 200) : 8;
            float radius = objModel.structure.radius > 0 ? objModel.structure.radius : 10f;

            Vector3 baseScale = GetScale(objModel);
            Vector3 centerPosition = GetPosition(objModel);

            for (int i = 0; i < count; i++)
            {
                float angle = (float)i / count * 2f * Mathf.PI;
                Vector3 position = centerPosition + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                
                GameObject instance = CreateInstance(objModel.id, i, 0, position, baseScale, meshAsset, material, parent);
                if (instance != null)
                {
                    objects.Add(instance);
                }
            }

            return objects;
        }

        private List<GameObject> GenerateRadial(ObjectModel objModel, Mesh meshAsset, Material material, GameObject parent)
        {
            List<GameObject> objects = new List<GameObject>();
            
            int count = objModel.structure.count > 0 ? Mathf.Clamp(objModel.structure.count, 1, 200) : 8;
            float radius = objModel.structure.radius > 0 ? objModel.structure.radius : 10f;

            Vector3 baseScale = GetScale(objModel);
            Vector3 centerPosition = GetPosition(objModel);

            for (int i = 0; i < count; i++)
            {
                float angle = (float)i / count * 2f * Mathf.PI;
                Vector3 position = centerPosition + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                
                GameObject instance = CreateInstance(objModel.id, i, 0, position, baseScale, meshAsset, material, parent);
                if (instance != null)
                {
                    // Face objects toward center
                    Vector3 lookDirection = (centerPosition - position).normalized;
                    instance.transform.rotation = Quaternion.LookRotation(lookDirection);
                    objects.Add(instance);
                }
            }

            return objects;
        }

        private List<GameObject> GenerateLine(ObjectModel objModel, Mesh meshAsset, Material material, GameObject parent)
        {
            List<GameObject> objects = new List<GameObject>();
            
            int count = objModel.structure.count > 0 ? Mathf.Clamp(objModel.structure.count, 1, 200) : 10;
            float spacing = objModel.structure.spacing > 0 ? objModel.structure.spacing : 2f;

            Vector3 baseScale = GetScale(objModel);
            Vector3 basePosition = GetPosition(objModel);
            Vector3 direction = objModel.structure.direction != null && objModel.structure.direction.Length >= 3 
                ? new Vector3(objModel.structure.direction[0], objModel.structure.direction[1], objModel.structure.direction[2]).normalized
                : Vector3.forward;

            for (int i = 0; i < count; i++)
            {
                Vector3 position = basePosition + direction * (i * spacing);
                GameObject instance = CreateInstance(objModel.id, i, 0, position, baseScale, meshAsset, material, parent);
                if (instance != null)
                {
                    objects.Add(instance);
                }
            }

            return objects;
        }

        private List<GameObject> GenerateSpiral(ObjectModel objModel, Mesh meshAsset, Material material, GameObject parent)
        {
            List<GameObject> objects = new List<GameObject>();
            
            int count = objModel.structure.count > 0 ? Mathf.Clamp(objModel.structure.count, 1, 200) : 20;
            float radius = objModel.structure.radius > 0 ? objModel.structure.radius : 10f;
            float height = objModel.structure.height > 0 ? objModel.structure.height : 6f;
            float spacing = objModel.structure.spacing > 0 ? objModel.structure.spacing : 1f;

            Vector3 baseScale = GetScale(objModel);
            Vector3 basePosition = GetPosition(objModel);

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float angle = t * 4f * Mathf.PI; // 2 full rotations
                float currentRadius = radius * t;
                float currentHeight = height * t;
                
                Vector3 position = basePosition + new Vector3(
                    Mathf.Cos(angle) * currentRadius,
                    currentHeight,
                    Mathf.Sin(angle) * currentRadius
                );
                
                GameObject instance = CreateInstance(objModel.id, i, 0, position, baseScale, meshAsset, material, parent);
                if (instance != null)
                {
                    objects.Add(instance);
                }
            }

            return objects;
        }

        private GameObject CreateInstance(string baseId, int indexX, int indexZ, Vector3 position, Vector3 scale, Mesh meshAsset, Material material, GameObject parent)
        {
            string instanceName = $"{baseId}_{indexX}_{indexZ}";
            
            // Apply ground offset to prevent sinking
            position = SceneCompositionHelper.ApplyGroundOffset(position, scale);
            
            // Find non-overlapping position
            position = SceneCompositionHelper.FindNonOverlappingPosition(position, scale, instanceName);
            
            GameObject instance = new GameObject(instanceName);
            instance.transform.SetParent(parent.transform);
            instance.transform.position = position;
            instance.transform.localScale = scale;

            // Add mesh components
            MeshFilter meshFilter = instance.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = instance.AddComponent<MeshRenderer>();

            meshFilter.sharedMesh = meshAsset;
            meshRenderer.sharedMaterial = material;

            // Configure renderer for performance
            ProfessionalMaterialFactory.ConfigureRenderer(meshRenderer);

            // Apply procedural variation
            ProceduralVariationSystem.Apply(instance, meshRenderer);
            
            // Register occupied space
            SceneCompositionHelper.RegisterSpace(instanceName, position, scale);

            return instance;
        }

        private Mesh GetOrCreateMesh(string primitiveType)
        {
            if (string.IsNullOrEmpty(primitiveType)) return null;

            string key = primitiveType.ToLowerInvariant();
            if (!_meshCache.TryGetValue(key, out Mesh meshAsset))
            {
                meshAsset = MeshFactory.CreateMesh(key);
                if (meshAsset != null)
                {
                    _meshCache[key] = meshAsset;
                }
            }
            return meshAsset;
        }

        private Material GetMaterial(string materialRef, Dictionary<string, Material> materialLookup)
        {
            if (!string.IsNullOrEmpty(materialRef) && materialLookup.TryGetValue(materialRef, out Material resolvedMaterial))
            {
                return resolvedMaterial;
            }
            return _materialFactory.CreateMaterial(null);
        }

        private Vector3 GetPosition(ObjectModel objModel)
        {
            if (objModel.transform?.position != null && objModel.transform.position.Length >= 3)
            {
                return new Vector3(
                    objModel.transform.position[0],
                    objModel.transform.position[1],
                    objModel.transform.position[2]
                );
            }
            return Vector3.zero;
        }

        private Vector3 GetScale(ObjectModel objModel)
        {
            if (objModel.transform?.scale != null && objModel.transform.scale.Length >= 3)
            {
                return new Vector3(
                    objModel.transform.scale[0],
                    objModel.transform.scale[1],
                    objModel.transform.scale[2]
                );
            }
            return Vector3.one;
        }
    }
}
