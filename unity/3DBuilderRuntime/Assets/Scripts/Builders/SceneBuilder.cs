using UnityEngine;
using System.Collections.Generic;
using ThreeDBuilder.Scene;
using ThreeDBuilder.Geometry;
using ThreeDBuilder.Materials;

namespace ThreeDBuilder.Builders
{
    /// <summary>
    /// Constructs Unity GameObjects in the scene hierarchy from an internal SceneModel.
    /// Does not implement mesh generation or JSON parsing.
    /// </summary>
    public class SceneBuilder
    {
        public GameObject BuildScene(SceneModel scene)
        {
            if (scene == null || scene.objects == null)
            {
                Debug.LogError("SceneBuilder: Invalid SceneModel provided.");
                return null;
            }

            // Create the root container for the scene
            GameObject root = new GameObject("ProceduralSceneRoot");

            SetupEnvironmentAndCamera(scene);
            SetupLighting(scene, root);

            MaterialFactory materialFactory = new MaterialFactory();

            // --- Shared caches (populated once, reused across all instances) ---
            // Mesh cache key: primitive name (e.g. "cube")
            Dictionary<string, Mesh>     meshCache     = new Dictionary<string, Mesh>();
            // Material cache key: materialRef id from SceneModel
            Dictionary<string, Material> materialLookup = new Dictionary<string, Material>();

            if (scene.materials != null)
            {
                foreach (MaterialModel matModel in scene.materials)
                {
                    Material mat = materialFactory.CreateMaterial(matModel);
                    if (!string.IsNullOrEmpty(matModel.id))
                    {
                        materialLookup[matModel.id] = mat;
                    }
                }
            }

            foreach (ObjectModel objModel in scene.objects)
            {
                // Determine repetition grid dimensions (default to 1x1 if omitted)
                int gridX = 1;
                int gridZ = 1;
                float spaceX = 0f;
                float spaceZ = 0f;

                if (objModel.repeat != null)
                {
                    if (objModel.repeat.grid != null && objModel.repeat.grid.Length >= 2)
                    {
                        gridX = Mathf.Max(1, objModel.repeat.grid[0]);
                        gridZ = Mathf.Max(1, objModel.repeat.grid[1]);
                    }
                    if (objModel.repeat.spacing != null && objModel.repeat.spacing.Length >= 2)
                    {
                        spaceX = objModel.repeat.spacing[0];
                        spaceZ = objModel.repeat.spacing[1];
                    }
                }

                // Nested loop for grid instantiation
                for (int x = 0; x < gridX; x++)
                {
                    for (int z = 0; z < gridZ; z++)
                    {
                        // Generate unique names if repeating
                        string instanceName = (gridX == 1 && gridZ == 1) 
                            ? objModel.id 
                            : $"{objModel.id}_{x}_{z}";

                        GameObject objInstance = new GameObject(instanceName);
                        objInstance.transform.SetParent(root.transform);

                        // Attach rendering components
                        MeshFilter meshFilter = objInstance.AddComponent<MeshFilter>();
                        MeshRenderer meshRenderer = objInstance.AddComponent<MeshRenderer>();

                        // Apply base transforms (position, rotation, scale)
                        if (objModel.transform != null)
                        {
                            ApplyTransform(objInstance.transform, objModel.transform);
                        }

                        // Apply repetition offset on X and Z axes
                        objInstance.transform.localPosition += new Vector3(x * spaceX, 0f, z * spaceZ);

                        // --- Shared mesh lookup (generate once per primitive type) ---
                        Mesh meshAsset = null;
                        string primitiveKey = (objModel.primitive ?? string.Empty).ToLowerInvariant();
                        if (!string.IsNullOrEmpty(primitiveKey))
                        {
                            if (!meshCache.TryGetValue(primitiveKey, out meshAsset))
                            {
                                meshAsset = MeshFactory.CreateMesh(primitiveKey);
                                if (meshAsset != null)
                                {
                                    meshCache[primitiveKey] = meshAsset;
                                }
                            }
                        }

                        if (meshAsset != null)
                        {
                            // sharedMesh assigns the asset directly — no per-instance copy
                            meshFilter.sharedMesh = meshAsset;
                        }
                        else
                        {
                            Debug.LogWarning($"SceneBuilder: Mesh generation failed or unavailable for object '{objModel.id}' (primitive: {objModel.primitive}).");
                        }

                        // --- Shared material lookup ---
                        if (!string.IsNullOrEmpty(objModel.materialRef) && materialLookup.TryGetValue(objModel.materialRef, out Material resolvedMaterial))
                        {
                            // sharedMaterial avoids creating per-instance material copies
                            meshRenderer.sharedMaterial = resolvedMaterial;
                        }
                        else
                        {
                            Debug.LogWarning($"SceneBuilder: Failed to resolve material '{objModel.materialRef}' for object '{objModel.id}'. Using default.");
                            meshRenderer.sharedMaterial = materialFactory.CreateMaterial(null);
                        }
                    }
                }
            }

            return root;
        }

        /// <summary>
        /// Converts serializable float arrays into Unity Vector3 structures and applies them to the Transform.
        /// </summary>
        private void ApplyTransform(Transform unityTransform, TransformModel modelTransform)
        {
            if (modelTransform.position != null && modelTransform.position.Length >= 3)
            {
                unityTransform.localPosition = new Vector3(
                    modelTransform.position[0],
                    modelTransform.position[1],
                    modelTransform.position[2]
                );
            }

            if (modelTransform.rotation != null && modelTransform.rotation.Length >= 3)
            {
                unityTransform.localEulerAngles = new Vector3(
                    modelTransform.rotation[0],
                    modelTransform.rotation[1],
                    modelTransform.rotation[2]
                );
            }

            if (modelTransform.scale != null && modelTransform.scale.Length >= 3)
            {
                unityTransform.localScale = new Vector3(
                    modelTransform.scale[0],
                    modelTransform.scale[1],
                    modelTransform.scale[2]
                );
            }
        }

        private void SetupEnvironmentAndCamera(SceneModel scene)
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            // Environment Background Color
            if (scene.environment != null && scene.environment.backgroundColor != null && scene.environment.backgroundColor.Length >= 3)
            {
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = new Color(
                    scene.environment.backgroundColor[0],
                    scene.environment.backgroundColor[1],
                    scene.environment.backgroundColor[2]
                );
            }

            // Camera Transformation
            if (scene.camera != null)
            {
                if (scene.camera.position != null && scene.camera.position.Length >= 3)
                {
                    mainCam.transform.position = new Vector3(
                        scene.camera.position[0],
                        scene.camera.position[1],
                        scene.camera.position[2]
                    );
                }

                if (scene.camera.lookAt != null && scene.camera.lookAt.Length >= 3)
                {
                    Vector3 lookTarget = new Vector3(
                        scene.camera.lookAt[0],
                        scene.camera.lookAt[1],
                        scene.camera.lookAt[2]
                    );
                    mainCam.transform.LookAt(lookTarget);
                }
            }
        }

        private void SetupLighting(SceneModel scene, GameObject root)
        {
            if (scene.lighting != null)
            {
                GameObject lightObj = new GameObject("ProceduralLight");
                lightObj.transform.SetParent(root.transform);
                Light lightComp = lightObj.AddComponent<Light>();

                // Default to directional if unspecified or unmapped
                lightComp.type = LightType.Directional;

                if (!string.IsNullOrEmpty(scene.lighting.type) && scene.lighting.type.ToLowerInvariant() == "point")
                {
                    lightComp.type = LightType.Point;
                }

                if (scene.lighting.color != null && scene.lighting.color.Length >= 3)
                {
                    lightComp.color = new Color(
                        scene.lighting.color[0],
                        scene.lighting.color[1],
                        scene.lighting.color[2]
                    );
                }

                if (scene.lighting.intensity > 0)
                {
                    lightComp.intensity = scene.lighting.intensity;
                }
                else
                {
                    lightComp.intensity = 1.0f; // Default baseline fallback
                }

                if (scene.lighting.direction != null && scene.lighting.direction.Length >= 3)
                {
                    Vector3 dir = new Vector3(
                        scene.lighting.direction[0],
                        scene.lighting.direction[1],
                        scene.lighting.direction[2]
                    );
                    // Light direction via looking at offset target
                    lightObj.transform.rotation = Quaternion.LookRotation(dir);
                }
                else if (lightComp.type == LightType.Directional)
                {
                    // Default downward slant for standard directional light rendering
                    lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                }
            }
            else
            {
                // Unspecified entirely - Unity provides its own ambient default
                // Could spawn a default directional light here, but instructions say "if lighting configuration exists", so skip.
            }
        }
    }
}
