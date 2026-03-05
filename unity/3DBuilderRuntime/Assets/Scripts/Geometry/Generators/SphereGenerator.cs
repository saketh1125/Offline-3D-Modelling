using UnityEngine;
using System.Collections.Generic;
using System;

namespace ThreeDBuilder.Geometry.Generators
{
    /// <summary>
    /// Procedurally generates a UV sphere using latitude-longitude spherical coordinates.
    /// </summary>
    public static class SphereGenerator
    {
        public static UnityEngine.Mesh Generate(int latitudeSegments = 16, int longitudeSegments = 24, float radius = 0.5f)
        {
            List<Vector3> vertices  = new List<Vector3>();
            List<int>     triangles = new List<int>();
            List<Vector3> normals   = new List<Vector3>();
            List<Vector2> uvs       = new List<Vector2>();

            // Build vertices ring by ring, north pole to south pole
            for (int lat = 0; lat <= latitudeSegments; lat++)
            {
                float theta = Mathf.PI * lat / latitudeSegments;       // 0 → π
                float sinTheta = Mathf.Sin(theta);
                float cosTheta = Mathf.Cos(theta);
                float v = (float)lat / latitudeSegments;

                for (int lon = 0; lon <= longitudeSegments; lon++)
                {
                    float phi = 2f * Mathf.PI * lon / longitudeSegments; // 0 → 2π
                    float sinPhi = Mathf.Sin(phi);
                    float cosPhi = Mathf.Cos(phi);

                    Vector3 normal = new Vector3(sinTheta * cosPhi, cosTheta, sinTheta * sinPhi);
                    vertices.Add(normal * radius);
                    normals.Add(normal);
                    uvs.Add(new Vector2((float)lon / longitudeSegments, 1f - v));
                }
            }

            // Stitch rings into triangle pairs
            for (int lat = 0; lat < latitudeSegments; lat++)
            {
                for (int lon = 0; lon < longitudeSegments; lon++)
                {
                    int current = lat * (longitudeSegments + 1) + lon;
                    int next    = current + longitudeSegments + 1;

                    triangles.Add(current);
                    triangles.Add(next);
                    triangles.Add(current + 1);

                    triangles.Add(current + 1);
                    triangles.Add(next);
                    triangles.Add(next + 1);
                }
            }

            UnityEngine.Mesh mesh = new UnityEngine.Mesh();
            mesh.name      = "ProceduralSphere";
            mesh.vertices  = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.normals   = normals.ToArray();
            mesh.uv        = uvs.ToArray();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
