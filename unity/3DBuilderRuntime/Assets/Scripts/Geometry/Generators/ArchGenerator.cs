using UnityEngine;
using System.Collections.Generic;

namespace ThreeDBuilder.Geometry.Generators
{
    /// <summary>
    /// Procedurally generates a semi-circular arch mesh by extruding a torus-like ring
    /// over the upper 180° arc and capping both open ends.
    /// </summary>
    public static class ArchGenerator
    {
        public static UnityEngine.Mesh Generate(int segments = 16, float radius = 0.5f, float thickness = 0.2f)
        {
            List<Vector3> vertices  = new List<Vector3>();
            List<int>     triangles = new List<int>();
            List<Vector3> normals   = new List<Vector3>();
            List<Vector2> uvs       = new List<Vector2>();

            float outerR = radius;
            float innerR = radius - thickness;

            // Build outer-outer and inner rings across the semicircle (0° → 180°)
            // Each step produces 4 vertices: outer-top, outer-bottom, inner-top, inner-bottom
            // where "top" and "bottom" refer to the Z-depth of the extrusion.
            float depth = thickness * 0.5f;

            for (int i = 0; i <= segments; i++)
            {
                float t   = (float)i / segments;
                float ang = Mathf.PI * t;           // 0 → π (left foot → right foot)
                float cosA = Mathf.Cos(ang);
                float sinA = Mathf.Sin(ang);

                Vector3 outerPos = new Vector3(outerR * cosA, outerR * sinA, 0f);
                Vector3 innerPos = new Vector3(innerR * cosA, innerR * sinA, 0f);

                // Front face (+Z side)
                vertices.Add(outerPos + Vector3.forward * depth);
                normals.Add((outerPos).normalized);
                uvs.Add(new Vector2(t, 1f));

                vertices.Add(innerPos + Vector3.forward * depth);
                normals.Add((innerPos).normalized);
                uvs.Add(new Vector2(t, 0f));

                // Back face (-Z side)
                vertices.Add(outerPos - Vector3.forward * depth);
                normals.Add((outerPos).normalized);
                uvs.Add(new Vector2(t, 1f));

                vertices.Add(innerPos - Vector3.forward * depth);
                normals.Add((innerPos).normalized);
                uvs.Add(new Vector2(t, 0f));
            }

            // Stitch rings (4 verts per station: frontOuter, frontInner, backOuter, backInner)
            for (int i = 0; i < segments; i++)
            {
                int b = i * 4;

                // Front face quad (frontOuter0, frontOuter1, frontInner0, frontInner1)
                triangles.Add(b + 0); triangles.Add(b + 4); triangles.Add(b + 1);
                triangles.Add(b + 1); triangles.Add(b + 4); triangles.Add(b + 5);

                // Back face quad (winding reversed)
                triangles.Add(b + 2); triangles.Add(b + 3); triangles.Add(b + 6);
                triangles.Add(b + 3); triangles.Add(b + 7); triangles.Add(b + 6);

                // Outer cap quad (connecting front-outer to back-outer)
                triangles.Add(b + 0); triangles.Add(b + 2); triangles.Add(b + 4);
                triangles.Add(b + 2); triangles.Add(b + 6); triangles.Add(b + 4);

                // Inner cap quad (connecting front-inner to back-inner, reversed)
                triangles.Add(b + 1); triangles.Add(b + 5); triangles.Add(b + 3);
                triangles.Add(b + 3); triangles.Add(b + 5); triangles.Add(b + 7);
            }

            UnityEngine.Mesh mesh = new UnityEngine.Mesh();
            mesh.name      = "ProceduralArch";
            mesh.vertices  = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.normals   = normals.ToArray();
            mesh.uv        = uvs.ToArray();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
