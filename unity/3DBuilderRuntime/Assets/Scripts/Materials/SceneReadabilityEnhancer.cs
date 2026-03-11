using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace ThreeDBuilder.Materials
{
    /// <summary>
    /// Post-build pass that improves visual readability of procedural scenes.
    /// 
    /// PERFORMANCE OPTIMIZED:
    /// - Disabled brightness clamping (creates material instances)
    /// - Uses spatial partitioning for adjacent color checking
    /// - Only processes objects that actually need color changes
    /// </summary>
    public static class SceneReadabilityEnhancer
    {
        // Squared distance threshold for "near-identical" colors
        private const float COLOR_SIMILARITY_THRESHOLD_SQ = 0.015f;

        // Spatial distance threshold for "adjacent" objects (world units)
        private const float ADJACENCY_DISTANCE = 5.0f;

        // Spatial hash cell size for optimization
        private const float SPATIAL_HASH_CELL_SIZE = 10.0f;

        // Hue shift applied to break identical-color adjacency
        private const float HUE_SHIFT_AMOUNT = 0.08f;

        // PERFORMANCE: Cache for materials that need hue shifting
        private static Dictionary<Color, Material> _hueShiftedMaterials = new Dictionary<Color, Material>();

        /// <summary>
        /// Enhances readability of all renderers under the given root.
        /// PERFORMANCE: Only processes adjacent color conflicts.
        /// </summary>
        public static void Enhance(GameObject sceneRoot)
        {
            if (sceneRoot == null) return;

            MeshRenderer[] renderers = sceneRoot.GetComponentsInChildren<MeshRenderer>();
            if (renderers == null || renderers.Length == 0) return;

            // PERFORMANCE: Skip brightness clamping to avoid material instances
            // ClampAllBrightness(renderers);

            // Use optimized spatial partitioning for adjacent color checking
            FixAdjacentColorsOptimized(renderers);

            Debug.Log($"[SceneReadabilityEnhancer] Processed {renderers.Length} renderers with spatial optimization.");
        }

        // ─────────────────────────────────────────────────────────────────
        // DISABLED: Brightness Clamping (creates material instances)
        // ─────────────────────────────────────────────────────────────────

        private static void ClampAllBrightness(MeshRenderer[] renderers)
        {
            // DISABLED: Brightness clamping creates material instances
            // which breaks GPU instancing and causes performance issues
            return;
        }

        // ─────────────────────────────────────────────────────────────────
        // Optimized Adjacent Color Contrast with Spatial Partitioning
        // ─────────────────────────────────────────────────────────────────

        private static void FixAdjacentColorsOptimized(MeshRenderer[] renderers)
        {
            // Build spatial hash for O(n) adjacent lookups instead of O(n²)
            var spatialHash = BuildSpatialHash(renderers);
            
            // Track which renderers need material updates
            var renderersToUpdate = new List<(MeshRenderer renderer, Color newColor)>();

            // Check each renderer against its spatial neighbors
            foreach (var kvp in spatialHash)
            {
                var cellRenderers = kvp.Value;
                
                // Check pairs within this cell
                for (int i = 0; i < cellRenderers.Count; i++)
                {
                    for (int j = i + 1; j < cellRenderers.Count; j++)
                    {
                        var r1 = cellRenderers[i];
                        var r2 = cellRenderers[j];

                        // Skip if either has no material
                        if (r1.sharedMaterial == null || r2.sharedMaterial == null) continue;

                        // Check spatial adjacency (already in same cell, but verify distance)
                        float dist = Vector3.Distance(r1.transform.position, r2.transform.position);
                        if (dist > ADJACENCY_DISTANCE) continue;

                        // Check color similarity
                        Color color1 = r1.sharedMaterial.color;
                        Color color2 = r2.sharedMaterial.color;

                        float colorDist = ColorDistanceSq(color1, color2);
                        if (colorDist < COLOR_SIMILARITY_THRESHOLD_SQ)
                        {
                            // Shift the second object's color to create contrast
                            Color shifted = ColorPaletteManager.ShiftHue(color2, HUE_SHIFT_AMOUNT);
                            renderersToUpdate.Add((r2, shifted));
                        }
                    }
                }
            }

            // Apply material updates in batch (create shared materials for identical colors)
            ApplyMaterialUpdates(renderersToUpdate);
        }

        private static Dictionary<Vector3Int, List<MeshRenderer>> BuildSpatialHash(MeshRenderer[] renderers)
        {
            var spatialHash = new Dictionary<Vector3Int, List<MeshRenderer>>();

            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;

                Vector3 pos = renderer.transform.position;
                Vector3Int cell = WorldToCell(pos);

                if (!spatialHash.TryGetValue(cell, out var list))
                {
                    list = new List<MeshRenderer>();
                    spatialHash[cell] = list;
                }
                list.Add(renderer);
            }

            return spatialHash;
        }

        private static Vector3Int WorldToCell(Vector3 worldPos)
        {
            return new Vector3Int(
                Mathf.FloorToInt(worldPos.x / SPATIAL_HASH_CELL_SIZE),
                Mathf.FloorToInt(worldPos.y / SPATIAL_HASH_CELL_SIZE),
                Mathf.FloorToInt(worldPos.z / SPATIAL_HASH_CELL_SIZE)
            );
        }

        private static void ApplyMaterialUpdates(List<(MeshRenderer renderer, Color newColor)> updates)
        {
            // Group by color to reuse materials
            var colorGroups = new Dictionary<Color, List<MeshRenderer>>();

            foreach (var (renderer, newColor) in updates)
            {
                if (!colorGroups.TryGetValue(newColor, out var list))
                {
                    list = new List<MeshRenderer>();
                    colorGroups[newColor] = list;
                }
                list.Add(renderer);
            }

            // Create one material per unique color and reuse it
            foreach (var kvp in colorGroups)
            {
                Color color = kvp.Key;
                var renderers = kvp.Value;

                // Check if we already have a material for this color
                if (!_hueShiftedMaterials.TryGetValue(color, out Material shiftedMaterial))
                {
                    // Create new material with shifted color
                    shiftedMaterial = new Material(Shader.Find("Standard"));
                    shiftedMaterial.color = color;
                    shiftedMaterial.SetFloat("_Metallic", 0.05f);
                    shiftedMaterial.SetFloat("_Glossiness", 0.45f);
                    shiftedMaterial.enableInstancing = true;
                    
                    // Force opaque mode
                    shiftedMaterial.SetFloat("_Mode", 0f);
                    shiftedMaterial.SetInt("_SrcBlend", (int)BlendMode.One);
                    shiftedMaterial.SetInt("_DstBlend", (int)BlendMode.Zero);
                    shiftedMaterial.SetInt("_ZWrite", 1);
                    shiftedMaterial.renderQueue = -1;
                    
                    _hueShiftedMaterials[color] = shiftedMaterial;
                }

                // Apply the shared material to all renderers in this group
                foreach (var renderer in renderers)
                {
                    renderer.sharedMaterial = shiftedMaterial;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────

        private static float ColorDistanceSq(Color a, Color b)
        {
            float dr = a.r - b.r;
            float dg = a.g - b.g;
            float db = a.b - b.b;
            return dr * dr + dg * dg + db * db;
        }

        private static bool ColorsEqual(Color a, Color b)
        {
            return Mathf.Approximately(a.r, b.r) &&
                   Mathf.Approximately(a.g, b.g) &&
                   Mathf.Approximately(a.b, b.b);
        }

        /// <summary>
        /// Clear the material cache. Call when clearing the scene.
        /// </summary>
        public static void ClearCache()
        {
            _hueShiftedMaterials.Clear();
        }
    }
}
