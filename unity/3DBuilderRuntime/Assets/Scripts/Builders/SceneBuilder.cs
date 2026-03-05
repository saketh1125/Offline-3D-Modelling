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

            MaterialFactory materialFactory = new MaterialFactory();
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
                // Create a generic object node per entry
                GameObject objInstance = new GameObject(objModel.id);
                objInstance.transform.SetParent(root.transform);

                // Attach rendering components
                MeshFilter meshFilter = objInstance.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = objInstance.AddComponent<MeshRenderer>();

                // Apply transforms (position, rotation, scale)
                if (objModel.transform != null)
                {
                    ApplyTransform(objInstance.transform, objModel.transform);
                }

                // Process mesh generation dynamically
                Mesh meshAsset = MeshFactory.CreateMesh(objModel.primitive);
                if (meshAsset != null)
                {
                    meshFilter.mesh = meshAsset;
                }
                else
                {
                    Debug.LogWarning($"SceneBuilder: Mesh generation failed or unavailable for object '{objModel.id}' (primitive: {objModel.primitive}).");
                }

                // Resolve and assign material
                if (!string.IsNullOrEmpty(objModel.materialRef) && materialLookup.TryGetValue(objModel.materialRef, out Material resolvedMaterial))
                {
                    meshRenderer.material = resolvedMaterial;
                }
                else
                {
                    Debug.LogWarning($"SceneBuilder: Failed to resolve material '{objModel.materialRef}' for object '{objModel.id}'. Using default.");
                    meshRenderer.material = materialFactory.CreateMaterial(null);
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
    }
}
