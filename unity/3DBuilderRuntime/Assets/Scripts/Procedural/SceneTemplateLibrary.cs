using UnityEngine;
using ThreeDBuilder.Materials;
using ThreeDBuilder.Scene;
using ThreeDBuilder.Runtime;
using System.Collections.Generic;
using System.Linq;

namespace ThreeDBuilder.Procedural
{
    /// <summary>
    /// Base class for structured scene templates.
    /// All templates must use existing primitives and cached materials.
    /// </summary>
    public abstract class SceneTemplate
    {
        protected readonly Dictionary<string, Mesh> _meshCache;
        protected readonly Dictionary<string, Material> _materialLookup;

        protected SceneTemplate(Dictionary<string, Mesh> meshCache, Dictionary<string, Material> materialLookup)
        {
            _meshCache = meshCache;
            _materialLookup = materialLookup;
        }

        /// <summary>
        /// Generate the template structure.
        /// </summary>
        public abstract List<GameObject> Generate(GameObject parent);

        /// <summary>
        /// Helper to create a primitive with proper configuration.
        /// </summary>
        protected GameObject CreatePrimitive(string type, string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;

            // Add mesh components
            MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();

            // Use cached mesh
            if (_meshCache.TryGetValue(type, out Mesh mesh))
            {
                meshFilter.sharedMesh = mesh;
            }

            meshRenderer.sharedMaterial = material;
            ProfessionalMaterialFactory.ConfigureRenderer(meshRenderer);

            return obj;
        }

        /// <summary>
        /// Apply composition rules to generated objects.
        /// </summary>
        protected void ApplyCompositionRules(List<GameObject> objects)
        {
            // Find and establish focal point
            var focalObject = objects.FirstOrDefault(o => o.name.Contains("dome") || o.name.Contains("main"));
            if (focalObject != null)
            {
                SceneCompositionHelper.EstablishFocalPoint(focalObject.transform.parent, focalObject);
            }

            // Apply subtle variations to break perfect patterns
            foreach (var obj in objects)
            {
                if (obj.name.Contains("decorative") || obj.name.Contains("detail"))
                {
                    Vector3 currentPos = obj.transform.localPosition;
                    obj.transform.localPosition = SceneCompositionHelper.ApplySubtleVariation(currentPos, 0.2f);
                }
            }
        }
    }

    /// <summary>
    /// Taj Mahal inspired template using basic primitives.
    /// </summary>
    public class TajMahalTemplate : SceneTemplate
    {
        public TajMahalTemplate(Dictionary<string, Mesh> meshCache, Dictionary<string, Material> materialLookup) 
            : base(meshCache, materialLookup) { }

        public override List<GameObject> Generate(GameObject parent)
        {
            var objects = new List<GameObject>();

            // Get materials
            if (!_materialLookup.TryGetValue("marble", out Material marbleMaterial))
            {
                Debug.LogError("TajMahalTemplate: Required 'marble' material not found");
                return objects;
            }
            
            if (!_materialLookup.TryGetValue("water", out Material waterMaterial))
            {
                waterMaterial = marbleMaterial; // Fallback
            }

            // Create grass ground (large plane)
            var ground = CreatePrimitive("plane", "grass_ground", Vector3.zero, Vector3.one * 100f, marbleMaterial, parent.transform);
            ground.transform.localPosition = new Vector3(0, -0.5f, 0);
            ground.transform.rotation = Quaternion.Euler(90, 0, 0);
            objects.Add(ground);

            // Create main platform
            var platform = CreatePrimitive("cube", "main_platform", Vector3.zero, new Vector3(60, 2, 60), marbleMaterial, parent.transform);
            objects.Add(platform);

            // Create water pool around platform
            var waterPool = CreatePrimitive("cube", "water_pool", new Vector3(0, -0.5f, 0), new Vector3(80, 1, 80), waterMaterial, parent.transform);
            var waterRenderer = waterPool.GetComponent<MeshRenderer>();
            waterRenderer.material.color = new Color(0.3f, 0.5f, 0.7f, 0.6f);
            objects.Add(waterPool);

            // Create main building base
            var baseStructure = CreatePrimitive("cube", "main_base", new Vector3(0, 1, 0), new Vector3(40, 10, 40), marbleMaterial, parent.transform);
            objects.Add(baseStructure);

            // Create dome drum (cylinder base for dome)
            var domeDrum = CreatePrimitive("cylinder", "dome_drum", new Vector3(0, 16, 0), new Vector3(20, 8, 20), marbleMaterial, parent.transform);
            objects.Add(domeDrum);

            // Create main dome (sphere)
            var mainDome = CreatePrimitive("sphere", "main_dome", new Vector3(0, 24, 0), new Vector3(20, 20, 20), marbleMaterial, parent.transform);
            objects.Add(mainDome);

            // Create 4 minarets at corners
            float minaretDistance = 35f;
            float minaretHeight = 50f;
            float minaretRadius = 3f;

            Vector3[] minaretPositions = {
                new Vector3(-minaretDistance, 0, -minaretDistance),
                new Vector3(minaretDistance, 0, -minaretDistance),
                new Vector3(-minaretDistance, 0, minaretDistance),
                new Vector3(minaretDistance, 0, minaretDistance)
            };

            for (int i = 0; i < 4; i++)
            {
                // Minaret base
                var minaret = CreatePrimitive("cylinder", $"minaret_{i}", minaretPositions[i], 
                    new Vector3(minaretRadius * 2f, minaretHeight, minaretRadius * 2f), marbleMaterial, parent.transform);
                minaret.transform.localPosition = new Vector3(minaretPositions[i].x, minaretHeight / 2f, minaretPositions[i].z);
                objects.Add(minaret);

                // Minaret cap (sphere on top)
                var minaretCap = CreatePrimitive("sphere", $"minaret_cap_{i}", 
                    new Vector3(minaretPositions[i].x, minaretHeight + 3f, minaretPositions[i].z),
                    new Vector3(minaretRadius * 2.5f, minaretRadius * 2.5f, minaretRadius * 2.5f), marbleMaterial, parent.transform);
                objects.Add(minaretCap);

                // Small decorative domes on minarets
                var smallDome = CreatePrimitive("sphere", $"small_dome_{i}",
                    new Vector3(minaretPositions[i].x, minaretHeight + 6f, minaretPositions[i].z),
                    new Vector3(minaretRadius * 1.5f, minaretRadius * 1.5f, minaretRadius * 1.5f), marbleMaterial, parent.transform);
                objects.Add(smallDome);
            }

            // Create entrance arches
            for (int i = -1; i <= 1; i++)
            {
                if (i == 0) continue; // Skip center for main entrance
                
                var arch = CreatePrimitive("cube", $"arch_{i}", new Vector3(i * 12f, 5f, 20f),
                    new Vector3(8, 10, 2), marbleMaterial, parent.transform);
                objects.Add(arch);
            }

            // Create central entrance
            var entrance = CreatePrimitive("cube", "main_entrance", new Vector3(0, 3f, 20f),
                new Vector3(12, 6, 2), marbleMaterial, parent.transform);
            objects.Add(entrance);

            // Apply composition rules
            ApplyCompositionRules(objects);

            Debug.Log($"TajMahalTemplate: Generated with {objects.Count} objects");
            return objects;
        }
    }

    /// <summary>
    /// Solar System template with accurate planetary representation.
    /// </summary>
    public class SolarSystemTemplate : SceneTemplate
    {
        public SolarSystemTemplate(Dictionary<string, Mesh> meshCache, Dictionary<string, Material> materialLookup) 
            : base(meshCache, materialLookup) { }

        public override List<GameObject> Generate(GameObject parent)
        {
            var objects = new List<GameObject>();

            // Get materials
            Material sunMaterial = _materialLookup.GetValueOrDefault("sun", CreateDefaultMaterial(new Color(1f, 0.9f, 0.3f)));
            Material planetMaterial = _materialLookup.GetValueOrDefault("planet", CreateDefaultMaterial(new Color(0.5f, 0.5f, 0.7f)));
            Material moonMaterial = _materialLookup.GetValueOrDefault("moon", CreateDefaultMaterial(new Color(0.7f, 0.7f, 0.7f)));

            // Create sun
            var sun = CreatePrimitive("sphere", "sun", Vector3.zero, Vector3.one * 8f, sunMaterial, parent.transform);
            objects.Add(sun);

            // Planet data (name, distance from sun, size, color, moons)
            var planets = new[]
            {
                new { Name = "mercury", Distance = 15f, Size = 1f, Color = new Color(0.7f, 0.6f, 0.5f), Moons = 0 },
                new { Name = "venus", Distance = 25f, Size = 2.5f, Color = new Color(0.9f, 0.8f, 0.5f), Moons = 0 },
                new { Name = "earth", Distance = 35f, Size = 2.5f, Color = new Color(0.2f, 0.5f, 0.8f), Moons = 1 },
                new { Name = "mars", Distance = 45f, Size = 1.5f, Color = new Color(0.8f, 0.3f, 0.2f), Moons = 2 },
                new { Name = "jupiter", Distance = 65f, Size = 6f, Color = new Color(0.8f, 0.7f, 0.5f), Moons = 4 },
                new { Name = "saturn", Distance = 85f, Size = 5f, Color = new Color(0.9f, 0.8f, 0.6f), Moons = 3 }
            };

            // Create planets
            foreach (var planet in planets)
            {
                // Create orbit ring first
                var orbitRing = CreatePrimitive("cylinder", $"{planet.Name}_orbit", Vector3.zero,
                    new Vector3(planet.Distance * 2f, 0.1f, planet.Distance * 2f), moonMaterial, parent.transform);
                orbitRing.transform.rotation = Quaternion.Euler(90, 0, 0);
                var orbitRenderer = orbitRing.GetComponent<MeshRenderer>();
                orbitRenderer.material.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                orbitRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                objects.Add(orbitRing);

                // Position planet
                float angle = Random.Range(0f, Mathf.PI * 2f);
                Vector3 planetPos = new Vector3(Mathf.Cos(angle) * planet.Distance, 0, Mathf.Sin(angle) * planet.Distance);
                
                var planetObj = CreatePrimitive("sphere", planet.Name, planetPos, Vector3.one * planet.Size, planetMaterial, parent.transform);
                var planetRenderer = planetObj.GetComponent<MeshRenderer>();
                planetRenderer.material.color = planet.Color;
                objects.Add(planetObj);

                // Create moons
                for (int i = 0; i < planet.Moons; i++)
                {
                    float moonAngle = (i * 360f / planet.Moons) * Mathf.Deg2Rad;
                    float moonDistance = planet.Size * 3f;
                    Vector3 moonPos = planetPos + new Vector3(Mathf.Cos(moonAngle) * moonDistance, 0, Mathf.Sin(moonAngle) * moonDistance);
                    
                    var moon = CreatePrimitive("sphere", $"{planet.Name}_moon_{i}", moonPos, Vector3.one * 0.5f, moonMaterial, parent.transform);
                    objects.Add(moon);
                }
            }

            ApplyCompositionRules(objects);
            Debug.Log($"SolarSystemTemplate: Generated with {objects.Count} objects");
            return objects;
        }

        private Material CreateDefaultMaterial(Color color)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Metallic", 0.2f);
            mat.SetFloat("_Glossiness", 0.5f);
            return mat;
        }
    }

    /// <summary>
    /// Atom structure template with nucleus and electron orbits.
    /// </summary>
    public class AtomTemplate : SceneTemplate
    {
        public AtomTemplate(Dictionary<string, Mesh> meshCache, Dictionary<string, Material> materialLookup) 
            : base(meshCache, materialLookup) { }

        public override List<GameObject> Generate(GameObject parent)
        {
            var objects = new List<GameObject>();

            Material nucleusMaterial = _materialLookup.GetValueOrDefault("nucleus", CreateDefaultMaterial(new Color(1f, 0.2f, 0.2f)));
            Material electronMaterial = _materialLookup.GetValueOrDefault("electron", CreateDefaultMaterial(new Color(0.2f, 0.5f, 1f)));
            Material orbitMaterial = _materialLookup.GetValueOrDefault("orbit", CreateDefaultMaterial(new Color(0.5f, 0.5f, 0.5f)));

            // Create nucleus (cluster of spheres)
            for (int i = 0; i < 6; i++)
            {
                Vector3 offset = Random.insideUnitSphere * 2f;
                var proton = CreatePrimitive("sphere", $"proton_{i}", offset, Vector3.one * 2f, nucleusMaterial, parent.transform);
                objects.Add(proton);
            }

            // Create electron shells
            int[] shellElectrons = { 2, 6, 10 };
            float[] shellRadii = { 10f, 18f, 28f };

            for (int shell = 0; shell < shellElectrons.Length; shell++)
            {
                // Create orbit ring
                var orbitRing = CreatePrimitive("cylinder", $"shell_{shell}_orbit", Vector3.zero,
                    new Vector3(shellRadii[shell] * 2f, 0.1f, shellRadii[shell] * 2f), orbitMaterial, parent.transform);
                orbitRing.transform.rotation = Quaternion.Euler(90, 0, 0);
                var orbitRenderer = orbitRing.GetComponent<MeshRenderer>();
                orbitRenderer.material.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);
                orbitRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                objects.Add(orbitRing);

                // Create electrons
                for (int i = 0; i < shellElectrons[shell]; i++)
                {
                    float angle = (i * 360f / shellElectrons[shell]) * Mathf.Deg2Rad;
                    Vector3 electronPos = new Vector3(Mathf.Cos(angle) * shellRadii[shell], 0, Mathf.Sin(angle) * shellRadii[shell]);
                    
                    var electron = CreatePrimitive("sphere", $"electron_{shell}_{i}", electronPos, Vector3.one * 1f, electronMaterial, parent.transform);
                    objects.Add(electron);
                }
            }

            ApplyCompositionRules(objects);
            Debug.Log($"AtomTemplate: Generated with {objects.Count} objects");
            return objects;
        }

        private Material CreateDefaultMaterial(Color color)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Metallic", 0.3f);
            mat.SetFloat("_Glossiness", 0.8f);
            mat.enableInstancing = true;
            return mat;
        }
    }

    /// <summary>
    /// DNA Helix template with double helix structure.
    /// </summary>
    public class DNAHelixTemplate : SceneTemplate
    {
        public DNAHelixTemplate(Dictionary<string, Mesh> meshCache, Dictionary<string, Material> materialLookup) 
            : base(meshCache, materialLookup) { }

        public override List<GameObject> Generate(GameObject parent)
        {
            var objects = new List<GameObject>();

            Material baseMaterial = _materialLookup.GetValueOrDefault("base_a", CreateDefaultMaterial(new Color(1f, 0.2f, 0.2f)));
            Material base2Material = _materialLookup.GetValueOrDefault("base_t", CreateDefaultMaterial(new Color(0.2f, 0.2f, 1f)));
            Material rungMaterial = _materialLookup.GetValueOrDefault("rung", CreateDefaultMaterial(new Color(0.7f, 0.7f, 0.7f)));

            float helixHeight = 40f;
            float helixRadius = 8f;
            int basePairs = 30;
            float pairSpacing = helixHeight / basePairs;
            float angleStep = Mathf.PI * 2f / 3f; // 120 degrees per pair

            for (int i = 0; i < basePairs; i++)
            {
                float y = i * pairSpacing - helixHeight / 2f;
                float angle = i * angleStep;

                // First strand
                Vector3 pos1 = new Vector3(Mathf.Cos(angle) * helixRadius, y, Mathf.Sin(angle) * helixRadius);
                var base1 = CreatePrimitive("sphere", $"base_a_{i}", pos1, Vector3.one * 1.5f, baseMaterial, parent.transform);
                objects.Add(base1);

                // Second strand (opposite)
                Vector3 pos2 = new Vector3(Mathf.Cos(angle + Mathf.PI) * helixRadius, y, Mathf.Sin(angle + Mathf.PI) * helixRadius);
                var base2 = CreatePrimitive("sphere", $"base_t_{i}", pos2, Vector3.one * 1.5f, base2Material, parent.transform);
                objects.Add(base2);

                // Connecting rung (cylinder)
                if (i < basePairs - 1)
                {
                    Vector3 rungCenter = (pos1 + pos2) / 2f;
                    float rungLength = Vector3.Distance(pos1, pos2);
                    
                    var rung = CreatePrimitive("cylinder", $"rung_{i}", rungCenter, 
                        new Vector3(0.3f, rungLength / 2f, 0.3f), rungMaterial, parent.transform);
                    rung.transform.LookAt(pos2);
                    rung.transform.Rotate(90, 0, 0);
                    objects.Add(rung);
                }
            }

            ApplyCompositionRules(objects);
            Debug.Log($"DNAHelixTemplate: Generated with {objects.Count} objects");
            return objects;
        }

        private Material CreateDefaultMaterial(Color color)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Metallic", 0.1f);
            mat.SetFloat("_Glossiness", 0.6f);
            mat.enableInstancing = true;
            return mat;
        }
    }

    /// <summary>
    /// Neural Network template with layers and connections.
    /// </summary>
    public class NeuralNetworkTemplate : SceneTemplate
    {
        public NeuralNetworkTemplate(Dictionary<string, Mesh> meshCache, Dictionary<string, Material> materialLookup) 
            : base(meshCache, materialLookup) { }

        public override List<GameObject> Generate(GameObject parent)
        {
            var objects = new List<GameObject>();

            Material neuronMaterial = _materialLookup.GetValueOrDefault("neuron", CreateDefaultMaterial(new Color(0.2f, 0.8f, 1f)));
            Material connectionMaterial = _materialLookup.GetValueOrDefault("connection", CreateDefaultMaterial(new Color(0.5f, 0.5f, 0.7f)));

            int layers = 4;
            int neuronsPerLayer = 6;
            float layerSpacing = 8f;
            float neuronSpacing = 3f;

            // Create neurons
            var allNeurons = new List<GameObject>[layers];

            for (int layer = 0; layer < layers; layer++)
            {
                allNeurons[layer] = new List<GameObject>();
                float x = (layer - layers / 2f) * layerSpacing;
                
                // Center the layer vertically
                float totalHeight = (neuronsPerLayer - 1) * neuronSpacing;
                float startY = -totalHeight / 2f;

                for (int neuron = 0; neuron < neuronsPerLayer; neuron++)
                {
                    float y = startY + neuron * neuronSpacing;
                    Vector3 position = new Vector3(x, y, 0);
                    
                    // Vary neuron sizes for visual hierarchy
                    float scale = SceneCompositionHelper.CalculateHierarchyScale(neuron, neuronsPerLayer, 1.2f);
                    
                    var neuronObj = CreatePrimitive("sphere", $"neuron_{layer}_{neuron}", position, 
                        Vector3.one * scale, neuronMaterial, parent.transform);
                    objects.Add(neuronObj);
                    allNeurons[layer].Add(neuronObj);
                }
            }

            // Create connections
            for (int layer = 0; layer < layers - 1; layer++)
            {
                for (int n1 = 0; n1 < allNeurons[layer].Count; n1++)
                {
                    for (int n2 = 0; n2 < allNeurons[layer + 1].Count; n2++)
                    {
                        Vector3 pos1 = allNeurons[layer][n1].transform.position;
                        Vector3 pos2 = allNeurons[layer + 1][n2].transform.position;
                        
                        var connection = CreatePrimitive("cylinder", $"connection_{layer}_{n1}_{n2}", 
                            (pos1 + pos2) / 2f, Vector3.one, connectionMaterial, parent.transform);
                        
                        float distance = Vector3.Distance(pos1, pos2);
                        connection.transform.localScale = new Vector3(0.05f, distance / 2f, 0.05f);
                        connection.transform.LookAt(pos2);
                        connection.transform.Rotate(90, 0, 0);
                        
                        objects.Add(connection);
                    }
                }
            }

            ApplyCompositionRules(objects);
            Debug.Log($"NeuralNetworkTemplate: Generated with {objects.Count} objects");
            return objects;
        }

        private Material CreateDefaultMaterial(Color color)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Metallic", 0.4f);
            mat.SetFloat("_Glossiness", 0.9f);
            mat.enableInstancing = true;
            return mat;
        }
    }

    /// <summary>
    /// Factory for creating scene templates.
    /// </summary>
    public static class SceneTemplateFactory
    {
        public static SceneTemplate CreateTemplate(string templateType, Dictionary<string, Mesh> meshCache, Dictionary<string, Material> materialLookup)
        {
            switch (templateType.ToLower())
            {
                case "taj_mahal":
                    return new TajMahalTemplate(meshCache, materialLookup);
                case "solar_system":
                    return new SolarSystemTemplate(meshCache, materialLookup);
                case "atom":
                    return new AtomTemplate(meshCache, materialLookup);
                case "dna_helix":
                    return new DNAHelixTemplate(meshCache, materialLookup);
                case "neural_network":
                    return new NeuralNetworkTemplate(meshCache, materialLookup);
                default:
                    Debug.LogWarning($"SceneTemplateFactory: Unknown template type '{templateType}'");
                    return null;
            }
        }
    }
}
