using UnityEngine;

namespace ThreeDBuilder.Geometry.Generators
{
    /// <summary>
    /// Procedurally generates a flat horizontal plane mesh.
    /// </summary>
    public static class PlaneGenerator
    {
        public static UnityEngine.Mesh Generate(float width = 1f, float length = 1f)
        {
            float hw = width  * 0.5f;
            float hl = length * 0.5f;

            UnityEngine.Mesh mesh = new UnityEngine.Mesh();
            mesh.name = "ProceduralPlane";

            mesh.vertices = new Vector3[]
            {
                new Vector3(-hw, 0f, -hl),
                new Vector3( hw, 0f, -hl),
                new Vector3( hw, 0f,  hl),
                new Vector3(-hw, 0f,  hl),
            };

            // Two triangles forming the quad (upward-facing winding)
            mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };

            mesh.normals = new Vector3[]
            {
                Vector3.up, Vector3.up, Vector3.up, Vector3.up,
            };

            mesh.uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
            };

            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
