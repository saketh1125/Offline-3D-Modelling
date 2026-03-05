using UnityEngine;
using System.Collections.Generic;

namespace ThreeDBuilder.Geometry.Generators
{
    /// <summary>
    /// Procedurally generates a cylinder mesh with closed caps.
    /// </summary>
    public static class CylinderGenerator
    {
        public static UnityEngine.Mesh Generate(int radialSegments = 24, float radius = 0.5f, float height = 1f)
        {
            List<Vector3> vertices  = new List<Vector3>();
            List<int>     triangles = new List<int>();
            List<Vector3> normals   = new List<Vector3>();
            List<Vector2> uvs       = new List<Vector2>();

            float halfH = height * 0.5f;

            // ── Side wall ─────────────────────────────────────────────────
            int sideBase = 0;
            for (int i = 0; i <= radialSegments; i++)
            {
                float t   = (float)i / radialSegments;
                float ang = 2f * Mathf.PI * t;
                float x   = Mathf.Cos(ang) * radius;
                float z   = Mathf.Sin(ang) * radius;

                vertices.Add(new Vector3(x, -halfH, z));  // bottom ring
                normals.Add(new Vector3(x, 0, z).normalized);
                uvs.Add(new Vector2(t, 0f));

                vertices.Add(new Vector3(x,  halfH, z));  // top ring
                normals.Add(new Vector3(x, 0, z).normalized);
                uvs.Add(new Vector2(t, 1f));
            }

            for (int i = 0; i < radialSegments; i++)
            {
                int a = sideBase + i * 2;
                int b = a + 1;
                int c = a + 2;
                int d = a + 3;
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
                triangles.Add(c); triangles.Add(b); triangles.Add(d);
            }

            // ── Top cap ───────────────────────────────────────────────────
            int topCenterIdx = vertices.Count;
            vertices.Add(new Vector3(0f, halfH, 0f));
            normals.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f, 0.5f));

            int topRimStart = vertices.Count;
            for (int i = 0; i <= radialSegments; i++)
            {
                float ang = 2f * Mathf.PI * i / radialSegments;
                float x   = Mathf.Cos(ang) * radius;
                float z   = Mathf.Sin(ang) * radius;
                vertices.Add(new Vector3(x, halfH, z));
                normals.Add(Vector3.up);
                uvs.Add(new Vector2(x / (2f * radius) + 0.5f, z / (2f * radius) + 0.5f));
            }

            for (int i = 0; i < radialSegments; i++)
            {
                triangles.Add(topCenterIdx);
                triangles.Add(topRimStart + i + 1);
                triangles.Add(topRimStart + i);
            }

            // ── Bottom cap ────────────────────────────────────────────────
            int botCenterIdx = vertices.Count;
            vertices.Add(new Vector3(0f, -halfH, 0f));
            normals.Add(Vector3.down);
            uvs.Add(new Vector2(0.5f, 0.5f));

            int botRimStart = vertices.Count;
            for (int i = 0; i <= radialSegments; i++)
            {
                float ang = 2f * Mathf.PI * i / radialSegments;
                float x   = Mathf.Cos(ang) * radius;
                float z   = Mathf.Sin(ang) * radius;
                vertices.Add(new Vector3(x, -halfH, z));
                normals.Add(Vector3.down);
                uvs.Add(new Vector2(x / (2f * radius) + 0.5f, z / (2f * radius) + 0.5f));
            }

            for (int i = 0; i < radialSegments; i++)
            {
                triangles.Add(botCenterIdx);
                triangles.Add(botRimStart + i);
                triangles.Add(botRimStart + i + 1);
            }

            UnityEngine.Mesh mesh = new UnityEngine.Mesh();
            mesh.name      = "ProceduralCylinder";
            mesh.vertices  = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.normals   = normals.ToArray();
            mesh.uv        = uvs.ToArray();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
