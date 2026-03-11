using UnityEngine;
using ThreeDBuilder.Scene;
using ThreeDBuilder.Materials;
using ThreeDBuilder.Runtime;
using System.Collections.Generic;

namespace ThreeDBuilder.Procedural
{
    /// <summary>
    /// Generates structured educational scene templates using StructureGenerator.
    /// Creates visually coherent scenes with proper composition and spacing.
    /// </summary>
    public class SceneTemplateGenerator
    {
        private readonly StructureGenerator _structureGenerator;

        public SceneTemplateGenerator(ProfessionalMaterialFactory materialFactory, Dictionary<string, Mesh> meshCache)
        {
            _structureGenerator = new StructureGenerator(materialFactory, meshCache);
        }

        /// <summary>
        /// Generates a scene template based on the specified type and parameters.
        /// </summary>
        public List<GameObject> GenerateTemplate(SceneTemplateModel template, GameObject parent, Dictionary<string, Material> materialLookup)
        {
            if (template == null) return new List<GameObject>();

            switch (template.type.ToLower())
            {
                case "temple":
                    return GenerateTemple(template.parameters, parent, materialLookup);
                case "solar_system":
                    return GenerateSolarSystem(template.parameters, parent, materialLookup);
                case "neural_network":
                    return GenerateNeuralNetwork(template.parameters, parent, materialLookup);
                case "dna_helix":
                    return GenerateDNAHelix(template.parameters, parent, materialLookup);
                case "city_grid":
                    return GenerateCityGrid(template.parameters, parent, materialLookup);
                default:
                    Debug.LogWarning($"[SceneTemplateGenerator] Unknown template type: {template.type}");
                    return new List<GameObject>();
            }
        }

        private List<GameObject> GenerateTemple(TemplateParamsModel parameters, GameObject parent, Dictionary<string, Material> materialLookup)
        {
            var objects = new List<GameObject>();
            
            // Use default values if not specified
            float radius = parameters?.radius ?? 12f;
            int pillars = parameters?.pillars != null ? Mathf.Clamp((int)parameters.pillars, 4, 100) : 16;
            
            // Ensure pillars are divisible by 4 for symmetry
            pillars = Mathf.RoundToInt(pillars / 4f) * 4;
            if (pillars < 8) pillars = 8;
            
            // Create temple base
            var baseStructure = new StructureModel
            {
                type = "circle",
                count = 1,
                radius = radius * 0.8f
            };
            
            var baseObject = new ObjectModel
            {
                id = "temple_base",
                primitive = "cylinder",
                materialRef = "stone",
                structure = baseStructure,
                transform = new TransformModel
                {
                    position = new float[] { 0, 0.5f, 0 },
                    scale = new float[] { radius * 1.6f, 1f, radius * 1.6f }
                }
            };
            
            objects.AddRange(_structureGenerator.GenerateStructure(baseObject, parent, materialLookup));
            
            // Create pillars in a circle with perfect symmetry
            var pillarAngles = SceneCompositionHelper.GetSymmetricRadialAngles(pillars);
            
            for (int i = 0; i < pillars; i++)
            {
                float x = Mathf.Cos(pillarAngles[i]) * radius;
                float z = Mathf.Sin(pillarAngles[i]) * radius;
                
                var pillar = new GameObject($"temple_pillar_{i}");
                pillar.transform.SetParent(parent.transform);
                pillar.transform.localPosition = new Vector3(x, 3f, z);
                
                var pillarFilter = pillar.AddComponent<MeshFilter>();
                var pillarRenderer = pillar.AddComponent<MeshRenderer>();
                
                if (_structureGenerator.MeshCache.TryGetValue("cylinder", out Mesh cylinderMesh))
                {
                    pillarFilter.sharedMesh = cylinderMesh;
                }
                
                if (materialLookup.TryGetValue("marble", out Material marbleMaterial))
                {
                    pillarRenderer.sharedMaterial = marbleMaterial;
                }
                
                pillar.transform.localScale = new Vector3(0.5f, 6f, 0.5f);
                objects.Add(pillar);
                
                // Add decorative caps to pillars
                if (materialLookup.TryGetValue("gold", out Material capMaterial))
                {
                    SceneVisualEnhancer.AddPillarCaps(pillar, capMaterial);
                    // Find and add the caps to our objects list
                    var topCap = pillar.transform.Find("pillar_top_cap")?.gameObject;
                    var bottomCap = pillar.transform.Find("pillar_bottom_cap")?.gameObject;
                    if (topCap) objects.Add(topCap);
                    if (bottomCap) objects.Add(bottomCap);
                }
            }
            
            // Create roof with proper scaling
            var roofStructure = new StructureModel
            {
                type = "circle",
                count = 1,
                radius = radius * 0.9f
            };
            
            var roofObject = new ObjectModel
            {
                id = "temple_roof",
                primitive = "cone",
                materialRef = "tile",
                structure = roofStructure,
                transform = new TransformModel
                {
                    position = new float[] { 0, 7f, 0 },
                    scale = new float[] { radius * 1.8f, 3f, radius * 1.8f }
                }
            };
            
            objects.AddRange(_structureGenerator.GenerateStructure(roofObject, parent, materialLookup));
            
            Debug.Log($"[SceneTemplateGenerator] Generated temple with {pillars} pillars, radius {radius}");
            return objects;
        }

        private List<GameObject> GenerateSolarSystem(TemplateParamsModel parameters, GameObject parent, Dictionary<string, Material> materialLookup)
        {
            var objects = new List<GameObject>();
            
            float orbitRadius = parameters?.orbitRadius ?? 20f;
            int planets = parameters?.planets != null ? Mathf.Clamp((int)parameters.planets, 1, 50) : 5;
            
            // Create sun (larger than planets)
            var sun = new GameObject("sun");
            sun.transform.SetParent(parent.transform);
            sun.transform.localPosition = Vector3.zero;
            
            var sunFilter = sun.AddComponent<MeshFilter>();
            var sunRenderer = sun.AddComponent<MeshRenderer>();
            
            // Use cached mesh
            if (_structureGenerator.MeshCache.TryGetValue("sphere", out Mesh sphereMesh))
            {
                sunFilter.sharedMesh = sphereMesh;
            }
            
            if (materialLookup.TryGetValue("sun", out Material sunMaterial))
            {
                sunRenderer.sharedMaterial = sunMaterial;
            }
            
            // Sun is significantly larger than planets
            sun.transform.localScale = Vector3.one * 4f;
            objects.Add(sun);
            
            // Create orbit rings for visual clarity
            if (materialLookup.TryGetValue("moon", out Material ringMaterial))
            {
                var orbitRadii = new float[planets];
                for (int i = 0; i < planets; i++)
                {
                    orbitRadii[i] = orbitRadius * (i + 1) * 0.6f;
                }
                SceneVisualEnhancer.AddOrbitRings(parent, orbitRadii, ringMaterial);
            }
            
            // Create planets with proper orbital spacing
            for (int i = 0; i < planets; i++)
            {
                // Use golden ratio for pleasing orbital spacing
                float planetRadius = orbitRadius * (i + 1) * 0.6f;
                int moons = Random.Range(0, 3);
                
                // Planet size decreases with distance (like real solar system)
                float planetSize = Mathf.Lerp(2f, 0.5f, (float)i / planets);
                
                var planet = new GameObject($"planet_{i}");
                planet.transform.SetParent(parent.transform);
                
                // Position planet in orbit
                float angle = Random.Range(0f, Mathf.PI * 2f);
                planet.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * planetRadius,
                    Random.Range(-1f, 1f), // Slight vertical variation
                    Mathf.Sin(angle) * planetRadius
                );
                
                var planetFilter = planet.AddComponent<MeshFilter>();
                var planetRenderer = planet.AddComponent<MeshRenderer>();
                
                if (_structureGenerator.MeshCache.TryGetValue("sphere", out sphereMesh))
                {
                    planetFilter.sharedMesh = sphereMesh;
                }
                
                if (materialLookup.TryGetValue($"planet_{i % 3}", out Material planetMaterial))
                {
                    planetRenderer.sharedMaterial = planetMaterial;
                }
                
                planet.transform.localScale = Vector3.one * planetSize;
                objects.Add(planet);
                
                // Add moons with smaller orbits
                if (moons > 0)
                {
                    for (int j = 0; j < moons; j++)
                    {
                        float moonAngle = (j * 360f / moons) * Mathf.Deg2Rad;
                        float moonOrbitRadius = planetSize * 3f;
                        
                        var moon = new GameObject($"moon_{i}_{j}");
                        moon.transform.SetParent(parent.transform);
                        
                        moon.transform.localPosition = planet.transform.localPosition + new Vector3(
                            Mathf.Cos(moonAngle) * moonOrbitRadius,
                            0,
                            Mathf.Sin(moonAngle) * moonOrbitRadius
                        );
                        
                        var moonFilter = moon.AddComponent<MeshFilter>();
                        var moonRenderer = moon.AddComponent<MeshRenderer>();
                        
                        if (_structureGenerator.MeshCache.TryGetValue("sphere", out sphereMesh))
                        {
                            moonFilter.sharedMesh = sphereMesh;
                        }
                        
                        if (materialLookup.TryGetValue("moon", out Material moonMaterial))
                        {
                            moonRenderer.sharedMaterial = moonMaterial;
                        }
                        
                        // Moons are much smaller than planets
                        moon.transform.localScale = Vector3.one * (planetSize * 0.3f);
                        objects.Add(moon);
                    }
                }
            }
            
            Debug.Log($"[SceneTemplateGenerator] Generated solar system with {planets} planets");
            return objects;
        }

        private List<GameObject> GenerateNeuralNetwork(TemplateParamsModel parameters, GameObject parent, Dictionary<string, Material> materialLookup)
        {
            var objects = new List<GameObject>();
            
            int layers = parameters?.layers != null ? Mathf.Clamp((int)parameters.layers, 1, 20) : 4;
            int neuronsPerLayer = parameters?.neuronsPerLayer != null ? Mathf.Clamp((int)parameters.neuronsPerLayer, 1, 50) : 5;
            
            // Create neurons for each layer with proper spacing
            float layerSpacing = 6f; // Clear spacing between layers
            float neuronSpacing = 2.5f; // Spacing between neurons in a layer
            
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
                    
                    var neuronObj = new GameObject($"neuron_{layer}_{neuron}");
                    neuronObj.transform.SetParent(parent.transform);
                    neuronObj.transform.localPosition = new Vector3(x, y, 0);
                    
                    var neuronFilter = neuronObj.AddComponent<MeshFilter>();
                    var neuronRenderer = neuronObj.AddComponent<MeshRenderer>();
                    
                    if (_structureGenerator.MeshCache.TryGetValue("sphere", out Mesh sphereMesh))
                    {
                        neuronFilter.sharedMesh = sphereMesh;
                    }
                    
                    if (materialLookup.TryGetValue("neuron", out Material neuronMaterial))
                    {
                        neuronRenderer.sharedMaterial = neuronMaterial;
                    }
                    
                    neuronObj.transform.localScale = Vector3.one * 0.8f;
                    objects.Add(neuronObj);
                    allNeurons[layer].Add(neuronObj);
                }
            }
            
            // Create connections between layers
            for (int layer = 0; layer < layers - 1; layer++)
            {
                for (int n1 = 0; n1 < allNeurons[layer].Count; n1++)
                {
                    for (int n2 = 0; n2 < allNeurons[layer + 1].Count; n2++)
                    {
                        // Create connection as a thin cylinder
                        var connection = new GameObject($"connection_{layer}_{n1}_{n2}");
                        connection.transform.SetParent(parent.transform);
                        
                        var pos1 = allNeurons[layer][n1].transform.position;
                        var pos2 = allNeurons[layer + 1][n2].transform.position;
                        
                        connection.transform.position = (pos1 + pos2) / 2f;
                        connection.transform.LookAt(pos2);
                        connection.transform.Rotate(90, 0, 0);
                        
                        var connectionFilter = connection.AddComponent<MeshFilter>();
                        var connectionRenderer = connection.AddComponent<MeshRenderer>();
                        
                        if (_structureGenerator.MeshCache.TryGetValue("cylinder", out Mesh cylinderMesh))
                        {
                            connectionFilter.sharedMesh = cylinderMesh;
                        }
                        
                        if (materialLookup.TryGetValue("connection", out Material connectionMaterial))
                        {
                            connectionRenderer.sharedMaterial = connectionMaterial;
                        }
                        
                        float distance = Vector3.Distance(pos1, pos2);
                        connection.transform.localScale = new Vector3(0.03f, distance / 2f, 0.03f);
                        objects.Add(connection);
                    }
                }
            }
            
            Debug.Log($"[SceneTemplateGenerator] Generated neural network with {layers} layers, {neuronsPerLayer} neurons per layer");
            return objects;
        }

        private List<GameObject> GenerateDNAHelix(TemplateParamsModel parameters, GameObject parent, Dictionary<string, Material> materialLookup)
        {
            var objects = new List<GameObject>();
            
            float height = parameters?.height ?? 20f;
            float radius = parameters?.radius ?? 3f;
            int pairs = parameters?.pairs != null ? Mathf.Clamp((int)parameters.pairs, 1, 200) : 20;
            
            float pairSpacing = height / pairs;
            float angleStep = Mathf.PI * 2f / 3f; // 120 degrees per pair for smooth helix
            
            for (int i = 0; i < pairs; i++)
            {
                float y = i * pairSpacing - height / 2f;
                float angle = i * angleStep;
                
                // First strand
                var pos1 = new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
                var base1 = CreateBasePair("base_a", pos1, parent, materialLookup);
                objects.Add(base1);
                
                // Second strand (opposite)
                var pos2 = new Vector3(Mathf.Cos(angle + Mathf.PI) * radius, y, Mathf.Sin(angle + Mathf.PI) * radius);
                var base2 = CreateBasePair("base_t", pos2, parent, materialLookup);
                objects.Add(base2);
                
                // Create connecting rung (except at very top)
                if (i < pairs - 1)
                {
                    var rung = new GameObject($"rung_{i}");
                    rung.transform.SetParent(parent.transform);
                    
                    // Position rung at midpoint
                    Vector3 rungCenter = (pos1 + pos2) / 2f;
                    rung.transform.position = rungCenter;
                    
                    // Look along the connection
                    rung.transform.LookAt(pos2);
                    rung.transform.Rotate(90, 0, 0);
                    
                    var rungFilter = rung.AddComponent<MeshFilter>();
                    var rungRenderer = rung.AddComponent<MeshRenderer>();
                    
                    if (_structureGenerator.MeshCache.TryGetValue("cylinder", out Mesh cylinderMesh))
                    {
                        rungFilter.sharedMesh = cylinderMesh;
                    }
                    
                    if (materialLookup.TryGetValue("rung", out Material rungMaterial))
                    {
                        rungRenderer.sharedMaterial = rungMaterial;
                    }
                    
                    // Rung spans the full diameter
                    float rungLength = radius * 2f;
                    rung.transform.localScale = new Vector3(0.08f, rungLength / 2f, 0.08f);
                    objects.Add(rung);
                }
            }
            
            Debug.Log($"[SceneTemplateGenerator] Generated DNA helix with {pairs} base pairs");
            return objects;
        }

        private List<GameObject> GenerateCityGrid(TemplateParamsModel parameters, GameObject parent, Dictionary<string, Material> materialLookup)
        {
            var objects = new List<GameObject>();
            
            int gridSize = parameters?.gridSize != null ? Mathf.Clamp((int)parameters.gridSize, 1, 50) : 8;
            float blockSpacing = parameters?.blockSpacing ?? 10f;
            
            // Create ground plane
            var ground = new GameObject("city_ground");
            ground.transform.SetParent(parent.transform);
            ground.transform.localPosition = Vector3.zero;
            
            var groundFilter = ground.AddComponent<MeshFilter>();
            var groundRenderer = ground.AddComponent<MeshRenderer>();
            
            if (_structureGenerator.MeshCache.TryGetValue("plane", out Mesh planeMesh))
            {
                groundFilter.sharedMesh = planeMesh;
            }
            
            if (materialLookup.TryGetValue("ground", out Material groundMaterial))
            {
                groundRenderer.sharedMaterial = groundMaterial;
            }
            
            ground.transform.localScale = Vector3.one * gridSize * blockSpacing;
            objects.Add(ground);
            
            // Generate buildings
            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    // Skip some positions for variety
                    if (Random.value < 0.3f) continue;
                    
                    // Create stacked building instead of single cube
                    int floors = Random.Range(3, 12);
                    float buildingWidth = Random.Range(3f, 6f);
                    float buildingDepth = Random.Range(3f, 6f);
                    
                    var building = new GameObject($"building_{x}_{z}");
                    building.transform.SetParent(parent.transform);
                    
                    // Position building
                    float posX = (x - gridSize / 2f) * blockSpacing;
                    float posZ = (z - gridSize / 2f) * blockSpacing;
                    building.transform.localPosition = new Vector3(posX, floors * 2f, posZ);
                    
                    // Use stacked building enhancement
                    if (materialLookup.TryGetValue($"building_{Random.Range(0, 3)}", out Material buildingMaterial))
                    {
                        SceneVisualEnhancer.CreateStackedBuilding(building, floors, buildingMaterial);
                    }
                    
                    objects.Add(building);
                }
            }
            
            Debug.Log($"[SceneTemplateGenerator] Generated city grid {gridSize}x{gridSize}");
            return objects;
        }

        private GameObject CreateBasePair(string type, Vector3 position, GameObject parent, Dictionary<string, Material> materialLookup)
        {
            var baseObj = new GameObject($"base_{type}");
            baseObj.transform.SetParent(parent.transform);
            baseObj.transform.localPosition = position;
            
            var baseFilter = baseObj.AddComponent<MeshFilter>();
            var baseRenderer = baseObj.AddComponent<MeshRenderer>();
            
            if (_structureGenerator.MeshCache.TryGetValue("sphere", out Mesh sphereMesh))
            {
                baseFilter.sharedMesh = sphereMesh;
            }
            
            if (materialLookup.TryGetValue(type, out Material baseMaterial))
            {
                baseRenderer.sharedMaterial = baseMaterial;
            }
            
            baseObj.transform.localScale = Vector3.one * 0.8f;
            return baseObj;
        }
    }
}
