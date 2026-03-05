using UnityEngine;
using ThreeDBuilder.Geometry.Generators;

namespace ThreeDBuilder.Geometry
{
    /// <summary>
    /// Central dispatcher responsible for returning procedural meshes based on primitive type.
    /// Does not generate geometry mathematically—routes requests to specific Generator classes.
    /// </summary>
    public static class MeshFactory
    {
        /// <summary>
        /// Instantiates and returns a Unity Mesh based on the primitive identifier.
        /// </summary>
        /// <param name="primitive">The string identifier for the primitive (e.g., "cube").</param>
        /// <returns>A procedural Mesh instance.</returns>
        public static UnityEngine.Mesh CreateMesh(string primitive)
        {
            if (string.IsNullOrWhiteSpace(primitive))
            {
                Debug.LogWarning("MeshFactory: Primitive identifier is null or empty. Returning null mesh.");
                return null;
            }

            // Route standard primitives to dedicated generator classes Let future implementations map appropriately.
            switch (primitive.ToLowerInvariant())
            {
                case "cube":
                    return CubeGenerator.Generate();
                
                default:
                    Debug.LogWarning($"MeshFactory: Unsupported primitive '{primitive}'. Returning null mesh.");
                    return null;
            }
        }
    }
}
