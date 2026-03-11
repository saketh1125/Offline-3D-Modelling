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

            UnityEngine.Mesh mesh = null;

            // Route primitives to dedicated geometry generator classes.
            switch (primitive.ToLowerInvariant())
            {
                case "cube":          mesh = CubeGenerator.Generate();     break;
                case "sphere":        mesh = SphereGenerator.Generate();   break;
                case "cylinder":      mesh = CylinderGenerator.Generate(); break;
                case "plane":         mesh = PlaneGenerator.Generate();    break;
                case "dome":          mesh = DomeGenerator.Generate();     break;
                case "arch":          mesh = ArchGenerator.Generate();     break;
                case "torus":         mesh = GenerateTorus();              break;
                case "cone":          mesh = GenerateCone();               break;
                case "capsule":       mesh = GenerateCapsule();            break;
                case "pyramid":       mesh = GeneratePyramid();            break;
                case "rounded_cube":  mesh = GenerateRoundedCube();        break;
                default:
                    Debug.LogWarning($"MeshFactory: Unsupported primitive '{primitive}'. Returning null mesh.");
                    return null;
            }

            if (mesh != null)
            {
                ApplyVerticalGradient(mesh);
            }

            return mesh;
        }

        /// <summary>
        /// Applies a vertical brightness gradient to mesh vertex colors (0.85 at bottom to 1.1 at top).
        /// This creates pseudo ambient occlusion and enhances volume readability at zero GPU cost
        /// and without breaking GPU instancing.
        /// </summary>
        private static void ApplyVerticalGradient(UnityEngine.Mesh mesh)
        {
            if (mesh == null || mesh.vertexCount == 0) return;

            Vector3[] vertices = mesh.vertices;
            Color[] colors = new Color[vertices.Length];

            // 1. Find min and max Y across all vertices
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            for (int i = 0; i < vertices.Length; i++)
            {
                float y = vertices[i].y;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            // Handle flat meshes safely (e.g. Plane primitive)
            float heightRange = maxY - minY;
            if (heightRange <= 0.0001f)
            {
                // Flat shape — assign neutral color
                for (int i = 0; i < vertices.Length; i++)
                    colors[i] = Color.white;
                
                mesh.colors = colors;
                Debug.Log("[MeshFactory] Gradient shading applied (flat object).");
                return;
            }

            // 2. Map Y coordinate to brightness gradient (0.85 -> 1.1)
            const float MIN_BRIGHTNESS = 0.85f;
            const float MAX_BRIGHTNESS = 1.1f;

            for (int i = 0; i < vertices.Length; i++)
            {
                float normalizedY = (vertices[i].y - minY) / heightRange; // 0.0 (bottom) to 1.0 (top)
                float brightness = Mathf.Lerp(MIN_BRIGHTNESS, MAX_BRIGHTNESS, normalizedY);
                colors[i] = new Color(brightness, brightness, brightness, 1f);
            }

            // 3. Assign to mesh (Standard shader multiplies Albedo by Vertex Color by default)
            mesh.colors = colors;

            Debug.Log("[MeshFactory] Gradient shading applied.");
        }
        /// <summary>Generates a torus mesh with 24 radial and 16 tube segments.</summary>
        private static Mesh GenerateTorus()
        {
            const int radialSegments = 24;
            const int tubeSegments  = 16;
            const float majorRadius  = 0.5f;
            const float tubeRadius   = 0.2f;

            var vertices  = new System.Collections.Generic.List<Vector3>();
            var triangles = new System.Collections.Generic.List<int>();

            for (int r = 0; r <= radialSegments; r++)
            {
                float phi = 2f * Mathf.PI * r / radialSegments;
                Vector3 center = new Vector3(Mathf.Cos(phi) * majorRadius, 0f, Mathf.Sin(phi) * majorRadius);

                for (int t = 0; t <= tubeSegments; t++)
                {
                    float theta = 2f * Mathf.PI * t / tubeSegments;
                    Vector3 vert = center +
                        new Vector3(
                            Mathf.Cos(phi) * Mathf.Cos(theta) * tubeRadius,
                            Mathf.Sin(theta) * tubeRadius,
                            Mathf.Sin(phi) * Mathf.Cos(theta) * tubeRadius);
                    vertices.Add(vert);
                }
            }

            int stride = tubeSegments + 1;
            for (int r = 0; r < radialSegments; r++)
            {
                for (int t = 0; t < tubeSegments; t++)
                {
                    int a = r * stride + t;
                    int b = a + stride;
                    triangles.Add(a); triangles.Add(b);     triangles.Add(a + 1);
                    triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
                }
            }

            Mesh mesh = new Mesh { name = "Torus" };
            mesh.vertices  = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Debug.Log("[MeshFactory] Generated primitive: torus");
            return mesh;
        }

        /// <summary>Generates a cone with 24 base segments and a closed base.</summary>
        private static Mesh GenerateCone()
        {
            const int   segments = 24;
            const float radius   = 0.5f;
            const float height   = 1.0f;

            var vertices  = new System.Collections.Generic.List<Vector3>();
            var triangles = new System.Collections.Generic.List<int>();

            // Base ring + center
            int baseCenter = 0;
            vertices.Add(new Vector3(0f, 0f, 0f));

            for (int i = 0; i <= segments; i++)
            {
                float angle = 2f * Mathf.PI * i / segments;
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }

            int tip = vertices.Count;
            vertices.Add(new Vector3(0f, height, 0f));

            // Base triangles
            for (int i = 1; i <= segments; i++)
                triangles.AddRange(new[]{ baseCenter, i + 1 > segments ? 1 : i + 1, i });

            // Side triangles
            for (int i = 1; i <= segments; i++)
                triangles.AddRange(new[]{ i, i + 1 > segments ? 1 : i + 1, tip });

            Mesh mesh = new Mesh { name = "Cone" };
            mesh.vertices  = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Debug.Log("[MeshFactory] Generated primitive: cone");
            return mesh;
        }

        /// <summary>Generates a capsule (cylinder + hemispherical caps).</summary>
        private static Mesh GenerateCapsule()
        {
            const int   segments    = 24;
            const int   capRings    = 8;
            const float radius      = 0.5f;
            const float halfHeight  = 0.5f;

            var vertices  = new System.Collections.Generic.List<Vector3>();
            var triangles = new System.Collections.Generic.List<int>();

            // Top hemisphere
            for (int r = 0; r <= capRings; r++)
            {
                float polar = Mathf.PI * 0.5f * r / capRings;
                float y = halfHeight + Mathf.Sin(polar) * radius;
                float rRing = Mathf.Cos(polar) * radius;
                for (int s = 0; s <= segments; s++)
                {
                    float azimuth = 2f * Mathf.PI * s / segments;
                    vertices.Add(new Vector3(Mathf.Cos(azimuth) * rRing, y, Mathf.Sin(azimuth) * rRing));
                }
            }

            // Bottom hemisphere
            for (int r = 0; r <= capRings; r++)
            {
                float polar = Mathf.PI * 0.5f * r / capRings;
                float y = -halfHeight - Mathf.Sin(polar) * radius;
                float rRing = Mathf.Cos(polar) * radius;
                for (int s = 0; s <= segments; s++)
                {
                    float azimuth = 2f * Mathf.PI * s / segments;
                    vertices.Add(new Vector3(Mathf.Cos(azimuth) * rRing, y, Mathf.Sin(azimuth) * rRing));
                }
            }

            int stride = segments + 1;
            int totalRings = (capRings + 1) * 2;
            for (int r = 0; r < totalRings - 1; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int a = r * stride + s;
                    int b = a + stride;
                    triangles.Add(a); triangles.Add(a + 1); triangles.Add(b);
                    triangles.Add(a + 1); triangles.Add(b + 1); triangles.Add(b);
                }
            }

            Mesh mesh = new Mesh { name = "Capsule" };
            mesh.vertices  = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Debug.Log("[MeshFactory] Generated primitive: capsule");
            return mesh;
        }

        /// <summary>Generates a pyramid with a square base and 4 triangular sides.</summary>
        private static Mesh GeneratePyramid()
        {
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-0.5f, 0f, -0.5f), // 0 base
                new Vector3( 0.5f, 0f, -0.5f), // 1
                new Vector3( 0.5f, 0f,  0.5f), // 2
                new Vector3(-0.5f, 0f,  0.5f), // 3
                new Vector3( 0f,   1f,  0f),   // 4 apex
            };

            int[] triangles = new int[]
            {
                // Base (two tris)
                0, 2, 1,
                0, 3, 2,
                // Sides
                0, 1, 4,
                1, 2, 4,
                2, 3, 4,
                3, 0, 4,
            };

            Mesh mesh = new Mesh { name = "Pyramid" };
            mesh.vertices  = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Debug.Log("[MeshFactory] Generated primitive: pyramid");
            return mesh;
        }

        /// <summary>Generates a rounded cube approximated by a subdivided cuboid with smooth normals.</summary>
        private static Mesh GenerateRoundedCube()
        {
            const int   divs   = 4;    // subdivisions per face axis
            const float bevel  = 0.15f;
            const float half   = 0.5f;

            var vertices  = new System.Collections.Generic.List<Vector3>();
            var triangles = new System.Collections.Generic.List<int>();

            // Generate faces for each of the 6 cube faces with beveled inset grid
            Vector3[] normals6 = { Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
            Vector3[] tangU6   = { Vector3.right, Vector3.right, Vector3.forward, Vector3.forward, Vector3.right, Vector3.right };
            Vector3[] tangV6   = { Vector3.forward, Vector3.back, Vector3.up, Vector3.up, Vector3.up, Vector3.up };

            for (int f = 0; f < 6; f++)
            {
                int baseIdx = vertices.Count;
                Vector3 n = normals6[f];
                Vector3 u = tangU6[f];
                Vector3 v = tangV6[f];

                for (int j = 0; j <= divs; j++)
                {
                    for (int i = 0; i <= divs; i++)
                    {
                        float s = (float)i / divs * 2f - 1f; // -1 to 1
                        float t = (float)j / divs * 2f - 1f;
                        // Shrink face edges inward to approximate bevel
                        float su = s * (half - bevel);
                        float tv = t * (half - bevel);
                        Vector3 vert = n * half + u * su + v * tv;
                        // Normalize slightly to round corners
                        vert = Vector3.Lerp(vert, vert.normalized * half, 0.35f);
                        vertices.Add(vert);
                    }
                }

                int stride = divs + 1;
                for (int j = 0; j < divs; j++)
                {
                    for (int i = 0; i < divs; i++)
                    {
                        int a = baseIdx + j * stride + i;
                        int b = a + 1;
                        int c = a + stride;
                        int d = c + 1;
                        // Wind based on face orientation
                        if (f % 2 == 0) { triangles.Add(a); triangles.Add(c); triangles.Add(b); triangles.Add(b); triangles.Add(c); triangles.Add(d); }
                        else            { triangles.Add(a); triangles.Add(b); triangles.Add(c); triangles.Add(b); triangles.Add(d); triangles.Add(c); }
                    }
                }
            }

            Mesh mesh = new Mesh { name = "RoundedCube" };
            mesh.vertices  = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Debug.Log("[MeshFactory] Generated primitive: rounded_cube");
            return mesh;
        }
    }
}
