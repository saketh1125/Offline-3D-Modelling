using UnityEngine;

namespace ThreeDBuilder.Geometry.Generators
{
    /// <summary>
    /// Procedurally generates a unit cube mesh with correct normals and UVs.
    /// Does not create GameObjects. Returns a raw Mesh instance only.
    /// </summary>
    public static class CubeGenerator
    {
        /// <summary>
        /// Generates a procedural unit cube mesh (1x1x1) centered at the origin.
        /// Uses 24 vertices (4 per face) to enable correct per-face normals and UVs.
        /// </summary>
        /// <returns>A Unity Mesh representing a unit cube.</returns>
        public static UnityEngine.Mesh Generate()
        {
            UnityEngine.Mesh mesh = new UnityEngine.Mesh();
            mesh.name = "ProceduralCube";

            // ─────────────────────────────────────────────────────────────────
            // 24 vertices: 4 per face × 6 faces
            // Faces: +Z (Front), -Z (Back), +Y (Top), -Y (Bottom), +X (Right), -X (Left)
            // ─────────────────────────────────────────────────────────────────
            mesh.vertices = new Vector3[]
            {
                // Front (+Z)
                new Vector3(-0.5f, -0.5f,  0.5f),
                new Vector3( 0.5f, -0.5f,  0.5f),
                new Vector3( 0.5f,  0.5f,  0.5f),
                new Vector3(-0.5f,  0.5f,  0.5f),
                // Back (-Z)
                new Vector3( 0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f,  0.5f, -0.5f),
                new Vector3( 0.5f,  0.5f, -0.5f),
                // Top (+Y)
                new Vector3(-0.5f,  0.5f,  0.5f),
                new Vector3( 0.5f,  0.5f,  0.5f),
                new Vector3( 0.5f,  0.5f, -0.5f),
                new Vector3(-0.5f,  0.5f, -0.5f),
                // Bottom (-Y)
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3( 0.5f, -0.5f, -0.5f),
                new Vector3( 0.5f, -0.5f,  0.5f),
                new Vector3(-0.5f, -0.5f,  0.5f),
                // Right (+X)
                new Vector3( 0.5f, -0.5f,  0.5f),
                new Vector3( 0.5f, -0.5f, -0.5f),
                new Vector3( 0.5f,  0.5f, -0.5f),
                new Vector3( 0.5f,  0.5f,  0.5f),
                // Left (-X)
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f,  0.5f),
                new Vector3(-0.5f,  0.5f,  0.5f),
                new Vector3(-0.5f,  0.5f, -0.5f),
            };

            // ─────────────────────────────────────────────────────────────────
            // 36 triangle indices: 2 triangles × 3 indices × 6 faces
            // ─────────────────────────────────────────────────────────────────
            mesh.triangles = new int[]
            {
                // Front
                0, 2, 1,   0, 3, 2,
                // Back
                4, 6, 5,   4, 7, 6,
                // Top
                8, 10, 9,  8, 11, 10,
                // Bottom
                12, 14, 13, 12, 15, 14,
                // Right
                16, 18, 17, 16, 19, 18,
                // Left
                20, 22, 21, 20, 23, 22,
            };

            // ─────────────────────────────────────────────────────────────────
            // Per-face flat normals
            // ─────────────────────────────────────────────────────────────────
            mesh.normals = new Vector3[]
            {
                // Front
                Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
                // Back
                Vector3.back,    Vector3.back,    Vector3.back,    Vector3.back,
                // Top
                Vector3.up,      Vector3.up,      Vector3.up,      Vector3.up,
                // Bottom
                Vector3.down,    Vector3.down,    Vector3.down,    Vector3.down,
                // Right
                Vector3.right,   Vector3.right,   Vector3.right,   Vector3.right,
                // Left
                Vector3.left,    Vector3.left,    Vector3.left,    Vector3.left,
            };

            // ─────────────────────────────────────────────────────────────────
            // UV coordinates: simple 0-1 quad layout per face
            // ─────────────────────────────────────────────────────────────────
            Vector2[] faceUVs = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f),
            };

            mesh.uv = new Vector2[]
            {
                // Front
                faceUVs[0], faceUVs[1], faceUVs[2], faceUVs[3],
                // Back
                faceUVs[0], faceUVs[1], faceUVs[2], faceUVs[3],
                // Top
                faceUVs[0], faceUVs[1], faceUVs[2], faceUVs[3],
                // Bottom
                faceUVs[0], faceUVs[1], faceUVs[2], faceUVs[3],
                // Right
                faceUVs[0], faceUVs[1], faceUVs[2], faceUVs[3],
                // Left
                faceUVs[0], faceUVs[1], faceUVs[2], faceUVs[3],
            };

            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
