using UnityEngine;
using System.Collections.Generic;

namespace ThreeDBuilder.Geometry.Generators
{
    /// <summary>
    /// Procedurally generates a dome mesh (upper hemisphere of a UV sphere).
    /// </summary>
    public static class DomeGenerator
    {
        public static UnityEngine.Mesh Generate(int latitudeSegments = 12, int longitudeSegments = 24, float radius = 0.5f)
        {
            List<Vector3> vertices  = new List<Vector3>();
            List<int>     triangles = new List<int>();
            List<Vector3> normals   = new List<Vector3>();
            List<Vector2> uvs       = new List<Vector2>();

            // Only the top hemisphere: theta from 0 (north pole) to π/2 (equator)
            for (int lat = 0; lat <= latitudeSegments; lat++)
            {
                float theta    = Mathf.PI * 0.5f * lat / latitudeSegments; // 0 → π/2
                float sinTheta = Mathf.Sin(theta);
                float cosTheta = Mathf.Cos(theta);
                float v        = (float)lat / latitudeSegments;

                for (int lon = 0; lon <= longitudeSegments; lon++)
                {
                    float phi    = 2f * Mathf.PI * lon / longitudeSegments;
                    float sinPhi = Mathf.Sin(phi);
                    float cosPhi = Mathf.Cos(phi);

                    Vector3 normal = new Vector3(sinTheta * cosPhi, cosTheta, sinTheta * sinPhi);
                    vertices.Add(normal * radius);
                    normals.Add(normal);
                    uvs.Add(new Vector2((float)lon / longitudeSegments, 1f - v));
                }
            }

            // Stitch rings
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

            // Close the open base with a flat disc cap facing downward
            int baseCenter = vertices.Count;
            vertices.Add(new Vector3(0f, 0f, 0f));
            normals.Add(Vector3.down);
            uvs.Add(new Vector2(0.5f, 0.5f));

            int equipRingStart = vertices.Count;
            int equatorialLat  = latitudeSegments; // index of the equator ring
            int ringOffset     = equatorialLat * (longitudeSegments + 1);

            for (int lon = 0; lon <= longitudeSegments; lon++)
            {
                float phi = 2f * Mathf.PI * lon / longitudeSegments;
                float x   = Mathf.Cos(phi) * radius;
                float z   = Mathf.Sin(phi) * radius;
                vertices.Add(new Vector3(x, 0f, z));
                normals.Add(Vector3.down);
                uvs.Add(new Vector2(x / (2f * radius) + 0.5f, z / (2f * radius) + 0.5f));
            }

            for (int i = 0; i < longitudeSegments; i++)
            {
                triangles.Add(baseCenter);
                triangles.Add(equipRingStart + i);
                triangles.Add(equipRingStart + i + 1);
            }

            UnityEngine.Mesh mesh = new UnityEngine.Mesh();
            mesh.name      = "ProceduralDome";
            mesh.vertices  = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.normals   = normals.ToArray();
            mesh.uv        = uvs.ToArray();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
