using UnityEngine;
using System.Linq;
using ThreeDBuilder.Materials;

namespace ThreeDBuilder.Runtime
{
    /// <summary>
    /// Creates visual enhancements for scene elements without external assets.
    /// Adds grid patterns, edge falloff, and decorative elements using existing primitives.
    /// </summary>
    public static class SceneVisualEnhancer
    {
        /// <summary>
        /// Enhances ground plane with grid pattern and edge falloff.
        /// </summary>
        public static void EnhanceGround(GameObject ground, Material groundMaterial)
        {
            if (ground == null) return;

            // Create grid pattern using additional planes
            Transform groundTransform = ground.transform;
            Vector3 groundScale = groundTransform.localScale;
            
            // Add subtle grid lines
            CreateGridPattern(groundTransform, groundScale, groundMaterial);
            
            // Add edge falloff using darker planes
            CreateEdgeFalloff(groundTransform, groundScale);
        }

        private static void CreateGridPattern(Transform groundTransform, Vector3 groundScale, Material baseMaterial)
        {
            // Create grid lines material (darker version of ground)
            Material gridMaterial = new Material(baseMaterial);
            Color baseColor = baseMaterial.color;
            gridMaterial.color = new Color(baseColor.r * 0.7f, baseColor.g * 0.7f, baseColor.b * 0.7f);
            
            // Grid spacing
            float gridSize = 5f;
            int gridCountX = Mathf.FloorToInt(groundScale.x / gridSize);
            int gridCountZ = Mathf.FloorToInt(groundScale.z / gridSize);
            
            // Create horizontal lines
            for (int i = 0; i <= gridCountX; i++)
            {
                float x = (i - gridCountX / 2f) * gridSize;
                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = $"grid_line_x_{i}";
                line.transform.SetParent(groundTransform);
                line.transform.localPosition = new Vector3(x, 0.01f, 0);
                line.transform.localScale = new Vector3(0.1f, 0.02f, groundScale.z);
                
                var renderer = line.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = gridMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = true;
            }
            
            // Create vertical lines
            for (int i = 0; i <= gridCountZ; i++)
            {
                float z = (i - gridCountZ / 2f) * gridSize;
                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = $"grid_line_z_{i}";
                line.transform.SetParent(groundTransform);
                line.transform.localPosition = new Vector3(0, 0.01f, z);
                line.transform.localScale = new Vector3(groundScale.x, 0.02f, 0.1f);
                
                var renderer = line.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = gridMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = true;
            }
        }

        private static void CreateEdgeFalloff(Transform groundTransform, Vector3 groundScale)
        {
            // Create darker edges for depth
            Material edgeMaterial = new Material(Shader.Find("Standard"));
            edgeMaterial.color = new Color(0.1f, 0.1f, 0.15f);
            edgeMaterial.SetFloat("_Metallic", 0.2f);
            edgeMaterial.SetFloat("_Glossiness", 0.3f);
            
            // Edge width
            float edgeWidth = 2f;
            
            // Create 4 edge planes
            // North edge
            CreateEdgePlane(groundTransform, new Vector3(0, 0.02f, groundScale.z / 2f + edgeWidth / 2f), 
                           new Vector3(groundScale.x + edgeWidth * 2f, 0.05f, edgeWidth), edgeMaterial);
            
            // South edge
            CreateEdgePlane(groundTransform, new Vector3(0, 0.02f, -groundScale.z / 2f - edgeWidth / 2f), 
                           new Vector3(groundScale.x + edgeWidth * 2f, 0.05f, edgeWidth), edgeMaterial);
            
            // East edge
            CreateEdgePlane(groundTransform, new Vector3(groundScale.x / 2f + edgeWidth / 2f, 0.02f, 0), 
                           new Vector3(edgeWidth, 0.05f, groundScale.z + edgeWidth * 2f), edgeMaterial);
            
            // West edge
            CreateEdgePlane(groundTransform, new Vector3(-groundScale.x / 2f - edgeWidth / 2f, 0.02f, 0), 
                           new Vector3(edgeWidth, 0.05f, groundScale.z + edgeWidth * 2f), edgeMaterial);
        }

        private static void CreateEdgePlane(Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            edge.name = "ground_edge";
            edge.transform.SetParent(parent);
            edge.transform.localPosition = position;
            edge.transform.localScale = scale;
            
            var renderer = edge.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        /// <summary>
        /// Adds decorative caps to cylinders (e.g., pillars)
        /// </summary>
        public static void AddPillarCaps(GameObject pillar, Material capMaterial)
        {
            if (pillar == null) return;

            Vector3 position = pillar.transform.position;
            Vector3 scale = pillar.transform.localScale;
            float radius = scale.x / 2f;
            float height = scale.y;
            
            // Top cap
            GameObject topCap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            topCap.name = "pillar_top_cap";
            topCap.transform.SetParent(pillar.transform.parent);
            topCap.transform.position = new Vector3(position.x, position.y + height / 2f + radius * 0.8f, position.z);
            topCap.transform.localScale = new Vector3(radius * 1.6f, radius * 1.6f, radius * 1.6f);
            
            var topRenderer = topCap.GetComponent<MeshRenderer>();
            topRenderer.sharedMaterial = capMaterial;
            ProfessionalMaterialFactory.ConfigureRenderer(topRenderer);
            
            // Bottom cap
            GameObject bottomCap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bottomCap.name = "pillar_bottom_cap";
            bottomCap.transform.SetParent(pillar.transform.parent);
            bottomCap.transform.position = new Vector3(position.x, position.y - height / 2f - radius * 0.8f, position.z);
            bottomCap.transform.localScale = new Vector3(radius * 1.6f, radius * 1.6f, radius * 1.6f);
            
            var bottomRenderer = bottomCap.GetComponent<MeshRenderer>();
            bottomRenderer.sharedMaterial = capMaterial;
            ProfessionalMaterialFactory.ConfigureRenderer(bottomRenderer);
        }

        /// <summary>
        /// Adds orbit rings to solar system
        /// </summary>
        public static void AddOrbitRings(GameObject parent, float[] orbitRadii, Material ringMaterial)
        {
            if (parent == null || orbitRadii == null) return;

            for (int i = 0; i < orbitRadii.Length; i++)
            {
                GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = $"orbit_ring_{i}";
                ring.transform.SetParent(parent.transform);
                ring.transform.localPosition = Vector3.zero;
                ring.transform.localScale = new Vector3(orbitRadii[i] * 2f, 0.05f, orbitRadii[i] * 2f);
                ring.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                
                var renderer = ring.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = ringMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = true;
            }
        }

        /// <summary>
        /// Enhances building with stacked cubes
        /// </summary>
        public static void CreateStackedBuilding(GameObject building, int floors, Material buildingMaterial)
        {
            if (building == null) return;

            Vector3 baseScale = building.transform.localScale;
            Vector3 basePosition = building.transform.position;
            
            // Remove original building
            Object.DestroyImmediate(building);
            
            // Create stacked floors
            for (int i = 0; i < floors; i++)
            {
                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = $"building_floor_{i}";
                floor.transform.position = new Vector3(basePosition.x, basePosition.y + (i * baseScale.y), basePosition.z);
                floor.transform.localScale = baseScale;
                
                var renderer = floor.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = buildingMaterial;
                ProfessionalMaterialFactory.ConfigureRenderer(renderer);
            }
        }

        /// <summary>
        /// Creates a center focal object for scene composition
        /// </summary>
        public static void CreateCenterFocalObject(Transform parent, Material focalMaterial)
        {
            // Create a central monument
            GameObject basePlatform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basePlatform.name = "focal_base";
            basePlatform.transform.SetParent(parent);
            basePlatform.transform.localPosition = Vector3.zero;
            basePlatform.transform.localScale = new Vector3(8f, 0.5f, 8f);
            
            var baseRenderer = basePlatform.GetComponent<MeshRenderer>();
            baseRenderer.sharedMaterial = focalMaterial;
            ProfessionalMaterialFactory.ConfigureRenderer(baseRenderer);
            
            // Add central spire
            GameObject spire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spire.name = "focal_spire";
            spire.transform.SetParent(parent);
            spire.transform.localPosition = new Vector3(0, 3f, 0);
            spire.transform.localScale = new Vector3(2f, 6f, 2f);
            
            var spireRenderer = spire.GetComponent<MeshRenderer>();
            spireRenderer.sharedMaterial = focalMaterial;
            ProfessionalMaterialFactory.ConfigureRenderer(spireRenderer);
        }

        /// <summary>
        /// Apply general visual enhancements to the entire scene
        /// </summary>
        public static void Enhance(GameObject sceneRoot)
        {
            // Find ground plane and enhance it
            var ground = FindGroundPlane(sceneRoot);
            if (ground != null)
            {
                var groundRenderer = ground.GetComponent<MeshRenderer>();
                if (groundRenderer != null)
                {
                    EnhanceGround(ground, groundRenderer.sharedMaterial);
                }
            }

            // Add decorative elements to pillars
            var pillars = sceneRoot.GetComponentsInChildren<Transform>()
                .Where(t => t.name.ToLower().Contains("pillar") || t.name.ToLower().Contains("column"))
                .Select(t => t.gameObject)
                .ToArray();

            foreach (var pillar in pillars)
            {
                var renderer = pillar.GetComponent<MeshRenderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    // Use a slightly different material for caps if available
                    Material capMaterial = renderer.sharedMaterial;
                    AddPillarCaps(pillar, capMaterial);
                }
            }

            // Add subtle animations to some objects (optional, for visual interest)
            var animatedObjects = sceneRoot.GetComponentsInChildren<Transform>()
                .Where(t => t.name.ToLower().Contains("electron") || t.name.ToLower().Contains("planet"))
                .Select(t => t.gameObject)
                .ToArray();

            foreach (var obj in animatedObjects.Take(5)) // Limit to 5 objects for performance
            {
                var rotator = obj.AddComponent<SimpleRotator>();
                rotator.rotationSpeed = Random.Range(10f, 30f);
            }
        }

        private static GameObject FindGroundPlane(GameObject sceneRoot)
        {
            // Try different naming conventions
            string[] groundNames = { "ground", "floor", "plane", "platform", "base" };
            
            foreach (var name in groundNames)
            {
                var ground = GameObject.Find(name);
                if (ground != null) return ground;
                
                // Also search as child
                var childGround = sceneRoot.transform.Find(name)?.gameObject;
                if (childGround != null) return childGround;
            }
            
            // Try to find by size (large flat object)
            var renderers = sceneRoot.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                var scale = renderer.transform.localScale;
                if (scale.y < 2f && scale.x > 20f && scale.z > 20f)
                {
                    return renderer.gameObject;
                }
            }
            
            return null;
        }
    }

    /// <summary>
    /// Simple rotation component for animated objects
    /// </summary>
    public class SimpleRotator : MonoBehaviour
    {
        public float rotationSpeed = 20f;
        public Vector3 rotationAxis = Vector3.up;

        void Update()
        {
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime);
        }
    }
}
