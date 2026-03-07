using UnityEngine;
using ThreeDBuilder.Scene;

namespace ThreeDBuilder.Materials
{
    /// <summary>
    /// Factory for generating Unity Material instances from internal MaterialModel definitions.
    /// Operates purely on Materials and does not construct GameObjects.
    /// </summary>
    public class MaterialFactory
    {
        /// <summary>
        /// Creates a Unity Material using the Standard shader and applies properties
        /// defined in the MaterialModel.
        /// </summary>
        /// <param name="materialModel">The parsed POCO material data.</param>
        /// <returns>A configured Unity Material instance.</returns>
        public Material CreateMaterial(MaterialModel materialModel)
        {
            if (materialModel == null)
            {
                Debug.LogWarning("MaterialFactory: Provided MaterialModel is null. Returning default material.");
                return new Material(Shader.Find("Standard"));
            }

            // Create a new Material using Unity's built-in Standard shader
            Material material = new Material(Shader.Find("Standard"));
            material.name = string.IsNullOrWhiteSpace(materialModel.id) ? "ProceduralMaterial" : materialModel.id;
            material.enableInstancing = true; // Enable GPU instancing for identical object batching

            // Extract baseColor float array and convert to Unity Color
            if (materialModel.baseColor != null && materialModel.baseColor.Length >= 3)
            {
                float r = materialModel.baseColor[0];
                float g = materialModel.baseColor[1];
                float b = materialModel.baseColor[2];
                float a = materialModel.baseColor.Length >= 4 ? materialModel.baseColor[3] : 1f;

                material.color = new Color(r, g, b, a);
            }
            else
            {
                // Fallback to white if baseColor is missing or invalid
                material.color = Color.white;
            }

            return material;
        }
    }
}
