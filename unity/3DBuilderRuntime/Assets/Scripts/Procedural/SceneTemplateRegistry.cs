using UnityEngine;
using System.Collections.Generic;
using ThreeDBuilder.Scene;

namespace ThreeDBuilder.Procedural
{
    /// <summary>
    /// Interface for procedural scene templates that generate raw ObjectModels instead
    /// of directly instantiating GameObjects. This ensures templates pass through the
    /// standard SceneBuilder pipeline (instancing, material caching, count limits).
    /// </summary>
    public interface ISceneTemplate
    {
        List<ObjectModel> Generate(TemplateParamsModel parameters);
    }

    /// <summary>
    /// Registry mapping template names to their generator implementations.
    /// </summary>
    public static class SceneTemplateRegistry
    {
        private static readonly Dictionary<string, ISceneTemplate> _templates = new Dictionary<string, ISceneTemplate>
        {
            { "taj_mahal",      new TajMahalTemplateModel() },
            { "solar_system",   new SolarSystemTemplateModel() },
            { "dna_helix",      new DNAHelixTemplateModel() },
            { "neural_network", new NeuralNetworkTemplateModel() }
        };

        /// <summary>
        /// Generates ObjectModels for the requested template.
        /// Returns an empty list if the template is not found or fails.
        /// </summary>
        public static List<ObjectModel> GenerateObjects(string templateType, TemplateParamsModel parameters)
        {
            if (string.IsNullOrEmpty(templateType)) return new List<ObjectModel>();

            if (_templates.TryGetValue(templateType.ToLowerInvariant(), out ISceneTemplate template))
            {
                try
                {
                    return template.Generate(parameters);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[SceneTemplateRegistry] Failed to generate '{templateType}': {ex.Message}\n{ex.StackTrace}");
                }
            }
            else
            {
                Debug.LogWarning($"[SceneTemplateRegistry] Unknown template type: {templateType}");
            }

            // Fallback object so the scene isn't completely empty
            return new List<ObjectModel>
            {
                new ObjectModel
                {
                    id = "template_fallback_cube",
                    primitive = "cube",
                    materialRef = "fallback",
                    transform = new TransformModel { position = new float[] { 0, 0, 0 }, scale = new float[] { 1, 1, 1 }, rotation = new float[] { 0, 0, 0 } }
                }
            };
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Taj Mahal Template
    // ─────────────────────────────────────────────────────────────────────────
    public class TajMahalTemplateModel : ISceneTemplate
    {
        public List<ObjectModel> Generate(TemplateParamsModel parameters)
        {
            float radius = parameters?.radius > 0 ? Mathf.Clamp(parameters.radius, 5f, 50f) : 12f;
            float height = parameters?.height > 0 ? Mathf.Clamp(parameters.height, 5f, 60f) : 10f;
            int minarets = parameters?.count > 0 ? Mathf.Clamp(parameters.count, 4, 16) : 4;
            
            // Adjust scale based on radius/height
            float platformScale = radius * 5f;
            float baseScale = radius * 3.3f;
            float minaretDist = radius * 5.8f;

            var objects = new List<ObjectModel>
            {
                // Main platform
                new ObjectModel { id="tm_platform", primitive="cube", materialRef="marble",
                    transform = new TransformModel { position=new float[]{0, 2f, 0}, scale=new float[]{platformScale, 2, platformScale} } },
                
                // Central building base
                new ObjectModel { id="tm_base", primitive="cube", materialRef="marble",
                    transform = new TransformModel { position=new float[]{0, height * 0.8f + 2f, 0}, scale=new float[]{baseScale, height, baseScale} } },
                
                // Dome drum
                new ObjectModel { id="tm_drum", primitive="cylinder", materialRef="marble",
                    transform = new TransformModel { position=new float[]{0, height * 1.6f + 2f, 0}, scale=new float[]{baseScale * 0.5f, height * 0.8f, baseScale * 0.5f} } },
                
                // Main Dome
                new ObjectModel { id="tm_dome", primitive="sphere", materialRef="marble",
                    transform = new TransformModel { position=new float[]{0, height * 2.4f + 2f, 0}, scale=new float[]{baseScale * 0.5f, baseScale * 0.5f, baseScale * 0.5f} } },

                // Front entrance arch block
                new ObjectModel { id="tm_arch_front", primitive="cube", materialRef="dark_stone",
                    transform = new TransformModel { position=new float[]{0, height * 0.8f + 2f, baseScale * 0.5f}, scale=new float[]{baseScale * 0.3f, height * 0.8f, 1.5f} } }
            };

            // Minarets distribution
            // If count is 4, use 2x2. Otherwise distribute radially.
            if (minarets == 4)
            {
                objects.Add(new ObjectModel
                {
                    id = "tm_minarets",
                    primitive = "cylinder",
                    materialRef = "marble",
                    transform = new TransformModel { position = new float[] { 0, height, 0 }, scale = new float[] { 4f, height * 2f, 4f } },
                    repeat = new RepeatModel { grid = new int[] { 2, 2 }, spacing = new float[] { minaretDist, minaretDist } }
                });

                objects.Add(new ObjectModel
                {
                    id = "tm_minaret_domes",
                    primitive = "sphere",
                    materialRef = "marble",
                    transform = new TransformModel { position = new float[] { 0, height * 2.2f, 0 }, scale = new float[] { 5f, 5f, 5f } },
                    repeat = new RepeatModel { grid = new int[] { 2, 2 }, spacing = new float[] { minaretDist, minaretDist } }
                });
            }
            else
            {
                // Radial distribution for other counts
                for (int i = 0; i < minarets; i++)
                {
                    float angle = (i * 360f / minarets) * Mathf.Deg2Rad;
                    float x = Mathf.Cos(angle) * (minaretDist * 0.7f);
                    float z = Mathf.Sin(angle) * (minaretDist * 0.7f);

                    objects.Add(new ObjectModel {
                        id = $"tm_minaret_{i}", primitive = "cylinder", materialRef = "marble",
                        transform = new TransformModel { position=new float[]{x, height, z}, scale=new float[]{3f, height * 2f, 3f} }
                    });
                    
                    objects.Add(new ObjectModel {
                        id = $"tm_minaret_dome_{i}", primitive = "sphere", materialRef = "marble",
                        transform = new TransformModel { position=new float[]{x, height * 2.1f, z}, scale=new float[]{4f, 4f, 4f} }
                    });
                }
            }

            return objects;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Solar System Template
    // ─────────────────────────────────────────────────────────────────────────
    public class SolarSystemTemplateModel : ISceneTemplate
    {
        public List<ObjectModel> Generate(TemplateParamsModel parameters)
        {
            var objects = new List<ObjectModel>();
            int count = parameters?.planetCount > 0 ? parameters.planetCount : (parameters?.count > 0 ? parameters.count : 5);
            count = Mathf.Clamp(count, 3, 12);

            float spacing = parameters?.orbitSpacing > 0 ? parameters.orbitSpacing : (parameters?.orbitRadius > 0 ? parameters.orbitRadius : 25f);
            float orbitRadius = Mathf.Clamp(spacing, 10f, 100f);

            // Sun
            objects.Add(new ObjectModel {
                id = "sun", primitive = "sphere", materialRef = "sun",
                transform = new TransformModel { position=new float[]{0,0,0}, scale=new float[]{10, 10, 10} }
            });

            // Planets
            for (int i = 0; i < count; i++)
            {
                float dist = orbitRadius * (i + 1) * 0.6f;
                float angle = (i * (360f / count) + Random.Range(-10f, 10f)) * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * dist;
                float z = Mathf.Sin(angle) * dist;
                float size = Mathf.Lerp(2.5f, 0.8f, (float)i / count);

                objects.Add(new ObjectModel {
                    id = $"planet_{i}", primitive = "sphere", materialRef = $"planet_{i%3}",
                    transform = new TransformModel { position=new float[]{x, 0, z}, scale=new float[]{size, size, size} }
                });

                // Orbit ring
                objects.Add(new ObjectModel {
                    id = $"orbit_{i}", primitive = "cylinder", materialRef = "orbit_line",
                    transform = new TransformModel { position=new float[]{0, -0.1f, 0}, scale=new float[]{dist*2f, 0.02f, dist*2f} }
                });
            }

            return objects;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DNA Helix Template
    // ─────────────────────────────────────────────────────────────────────────
    public class DNAHelixTemplateModel : ISceneTemplate
    {
        public List<ObjectModel> Generate(TemplateParamsModel parameters)
        {
            var objects = new List<ObjectModel>();
            float height = parameters?.height > 0 ? Mathf.Clamp(parameters.height, 10f, 100f) : 30f;
            float radius = parameters?.radius > 0 ? Mathf.Clamp(parameters.radius, 2f, 15f) : 4f;
            int pairs = parameters?.pairs > 0 ? Mathf.Clamp(parameters.pairs, 5, 50) : 20;
            
            float pairSpacing = height / pairs;
            float angleStep = Mathf.PI * 2f / 8f; // Smoother spiral

            for (int i = 0; i < pairs; i++)
            {
                float y = i * pairSpacing - height / 2f;
                float angle = i * angleStep;

                // Strand A
                float ax = Mathf.Cos(angle) * radius;
                float az = Mathf.Sin(angle) * radius;
                objects.Add(new ObjectModel {
                    id = $"base_a_{i}", primitive = "sphere", materialRef = "base_a",
                    transform = new TransformModel { position=new float[]{ax, y, az}, scale=new float[]{1.5f, 1.5f, 1.5f} }
                });

                // Strand B
                float bx = Mathf.Cos(angle + Mathf.PI) * radius;
                float bz = Mathf.Sin(angle + Mathf.PI) * radius;
                objects.Add(new ObjectModel {
                    id = $"base_t_{i}", primitive = "sphere", materialRef = "base_t",
                    transform = new TransformModel { position=new float[]{bx, y, bz}, scale=new float[]{1.5f, 1.5f, 1.5f} }
                });

                // Rung
                float rotY = -(angle * Mathf.Rad2Deg);
                objects.Add(new ObjectModel {
                    id = $"rung_{i}", primitive = "cylinder", materialRef = "rung",
                    transform = new TransformModel { 
                        position=new float[]{0, y, 0}, 
                        rotation=new float[]{90, rotY, 0}, 
                        scale=new float[]{0.3f, radius, 0.3f} 
                    }
                });
            }

            return objects;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Neural Network Template
    // ─────────────────────────────────────────────────────────────────────────
    public class NeuralNetworkTemplateModel : ISceneTemplate
    {
        public List<ObjectModel> Generate(TemplateParamsModel parameters)
        {
            var objects = new List<ObjectModel>();
            int layers = parameters?.layers > 0 ? Mathf.Clamp(parameters.layers, 2, 8) : 4;
            int countPerLayer = parameters?.nodesPerLayer > 0 ? parameters.nodesPerLayer : (parameters?.count > 0 ? parameters.count : 5);
            countPerLayer = Mathf.Clamp(countPerLayer, 2, 20);

            float layerSpacing = parameters?.spacing > 0 ? Mathf.Clamp(parameters.spacing, 2f, 20f) : 10f;
            float nodeSpacing = 4f;

            // Simple grid of nodes
            objects.Add(new ObjectModel {
                id = "nn_nodes", primitive = "sphere", materialRef = "neuron",
                transform = new TransformModel { position = new float[] { 0, 0, 0 }, scale = new float[] { 1.5f, 1.5f, 1.5f } },
                repeat = new RepeatModel { grid = new int[] { layers, countPerLayer }, spacing = new float[] { layerSpacing, nodeSpacing } }
            });

            // Positioning start offset
            float startX = -((layers - 1) * layerSpacing) / 2f;
            float startY = -((countPerLayer - 1) * nodeSpacing) / 2f;
            objects[0].transform.position = new float[] { startX, startY + 12f, 0f };

            return objects;
        }
    }
}
