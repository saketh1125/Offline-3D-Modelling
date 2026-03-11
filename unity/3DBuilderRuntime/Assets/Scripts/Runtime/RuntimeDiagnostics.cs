using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ThreeDBuilder.Runtime
{
    /// <summary>
    /// Provides diagnostic information about the generated scene.
    /// Logs object counts, material usage, mesh reuse, and performance metrics.
    /// </summary>
    public static class RuntimeDiagnostics
    {
        private static bool _hasLogged = false;

        /// <summary>
        /// Logs comprehensive scene diagnostics.
        /// Call once after scene generation is complete.
        /// </summary>
        public static void LogSceneDiagnostics(GameObject sceneRoot)
        {
            if (_hasLogged) return;
            _hasLogged = true;

            Debug.Log("=== RUNTIME DIAGNOSTICS ===");

            // Count objects and components
            var allObjects = sceneRoot.GetComponentsInChildren<Transform>();
            int objectCount = allObjects.Length - 1; // Exclude the root itself
            var renderers = sceneRoot.GetComponentsInChildren<MeshRenderer>();
            var filters = sceneRoot.GetComponentsInChildren<MeshFilter>();

            Debug.Log($"Scene Objects: {objectCount}");
            Debug.Log($"Mesh Renderers: {renderers.Length}");
            Debug.Log($"Mesh Filters: {filters.Length}");

            // Analyze material usage
            AnalyzeMaterialUsage(renderers);

            // Analyze mesh reuse
            AnalyzeMeshReuse(filters);

            // Check GPU instancing
            CheckGPUInstancing(renderers);

            // Analyze shadows
            AnalyzeShadows(renderers);

            // Check for common issues
            CheckForIssues(renderers, filters);

            Debug.Log("=== END DIAGNOSTICS ===");
        }

        private static void AnalyzeMaterialUsage(MeshRenderer[] renderers)
        {
            var materialCounts = new Dictionary<Material, int>();
            var uniqueMaterials = new HashSet<Material>();

            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterial != null)
                {
                    materialCounts[renderer.sharedMaterial] = materialCounts.GetValueOrDefault(renderer.sharedMaterial, 0) + 1;
                    uniqueMaterials.Add(renderer.sharedMaterial);
                }
            }

            Debug.Log($"Unique Materials: {uniqueMaterials.Count}");
            
            // Log material reuse stats
            var sortedMaterials = materialCounts.OrderByDescending(kvp => kvp.Value).Take(5);
            foreach (var kvp in sortedMaterials)
            {
                Debug.Log($"  Material '{kvp.Key.name}': {kvp.Value} instances");
            }

            // Check for material instances (bad for performance)
            int instanceCount = renderers.Count(r => r.material != null && r.material != r.sharedMaterial);
            if (instanceCount > 0)
            {
                Debug.LogWarning($"PERFORMANCE WARNING: {instanceCount} renderers have material instances (breaks batching)");
            }
        }

        private static void AnalyzeMeshReuse(MeshFilter[] filters)
        {
            var meshCounts = new Dictionary<Mesh, int>();
            var uniqueMeshes = new HashSet<Mesh>();

            foreach (var filter in filters)
            {
                if (filter.sharedMesh != null)
                {
                    meshCounts[filter.sharedMesh] = meshCounts.GetValueOrDefault(filter.sharedMesh, 0) + 1;
                    uniqueMeshes.Add(filter.sharedMesh);
                }
            }

            Debug.Log($"Unique Meshes: {uniqueMeshes.Count}");
            
            // Calculate mesh reuse ratio
            int totalMeshes = filters.Length;
            float reuseRatio = totalMeshes > 0 ? (float)(totalMeshes - uniqueMeshes.Count) / totalMeshes * 100f : 0f;
            Debug.Log($"Mesh Reuse Ratio: {reuseRatio:F1}%");

            // Log most reused meshes
            var sortedMeshes = meshCounts.OrderByDescending(kvp => kvp.Value).Take(3);
            foreach (var kvp in sortedMeshes)
            {
                Debug.Log($"  Mesh '{kvp.Key.name}': {kvp.Value} instances");
            }
        }

        private static void CheckGPUInstancing(MeshRenderer[] renderers)
        {
            int instancingEnabled = 0;
            int instancingDisabled = 0;

            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterial != null && renderer.sharedMaterial.enableInstancing)
                {
                    instancingEnabled++;
                }
                else
                {
                    instancingDisabled++;
                }
            }

            Debug.Log($"GPU Instancing: {instancingEnabled} enabled, {instancingDisabled} disabled");

            if (instancingDisabled > 0)
            {
                Debug.LogWarning("PERFORMANCE WARNING: Some materials don't have GPU instancing enabled");
            }
        }

        private static void AnalyzeShadows(MeshRenderer[] renderers)
        {
            int shadowCasters = 0;
            int shadowReceivers = 0;

            foreach (var renderer in renderers)
            {
                if (renderer.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off)
                {
                    shadowCasters++;
                }
                if (renderer.receiveShadows)
                {
                    shadowReceivers++;
                }
            }

            Debug.Log($"Shadow Casters: {shadowCasters}");
            Debug.Log($"Shadow Receivers: {shadowReceivers}");
        }

        private static void CheckForIssues(MeshRenderer[] renderers, MeshFilter[] filters)
        {
            // Check for missing components
            int missingRenderer = filters.Count(f => f.GetComponent<MeshRenderer>() == null);
            if (missingRenderer > 0)
            {
                Debug.LogWarning($"{missingRenderer} MeshFilters without MeshRenderer");
            }

            int missingFilter = renderers.Count(r => r.GetComponent<MeshFilter>() == null);
            if (missingFilter > 0)
            {
                Debug.LogWarning($"{missingFilter} MeshRenderers without MeshFilter");
            }

            // Check for zero-scale objects
            int zeroScale = 0;
            foreach (var renderer in renderers)
            {
                if (renderer.transform.lossyScale == Vector3.zero)
                {
                    zeroScale++;
                }
            }
            if (zeroScale > 0)
            {
                Debug.LogWarning($"{zeroScale} objects have zero scale");
            }

            // Check for objects at origin (potential overlap)
            int atOrigin = renderers.Count(r => r.transform.position == Vector3.zero);
            if (atOrigin > 1)
            {
                Debug.LogWarning($"{atOrigin} objects positioned at origin (potential overlap)");
            }
        }

        /// <summary>
        /// Reset the diagnostic flag for next scene generation.
        /// </summary>
        public static void Reset()
        {
            _hasLogged = false;
        }
    }
}
