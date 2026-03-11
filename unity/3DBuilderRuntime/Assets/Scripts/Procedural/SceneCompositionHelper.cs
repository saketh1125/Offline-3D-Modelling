using UnityEngine;
using System.Collections.Generic;

namespace ThreeDBuilder.Procedural
{
    /// <summary>
    /// Helper class for ensuring proper scene composition and visual coherence.
    /// Prevents overlap, maintains symmetry, and applies intelligent spacing.
    /// </summary>
    public static class SceneCompositionHelper
    {
        private static readonly Dictionary<string, Bounds> _occupiedSpaces = new Dictionary<string, Bounds>();

        /// <summary>
        /// Clears the occupied spaces cache. Call when generating a new scene.
        /// </summary>
        public static void ClearCache()
        {
            _occupiedSpaces.Clear();
        }

        /// <summary>
        /// Checks if a position would overlap with existing objects.
        /// </summary>
        public static bool WouldOverlap(Vector3 position, Vector3 size, string excludeId = null)
        {
            Bounds newBounds = new Bounds(position, size);
            
            foreach (var kvp in _occupiedSpaces)
            {
                if (excludeId != null && kvp.Key == excludeId) continue;
                
                if (kvp.Value.Intersects(newBounds))
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Registers an object's occupied space.
        /// </summary>
        public static void RegisterSpace(string id, Vector3 position, Vector3 size)
        {
            _occupiedSpaces[id] = new Bounds(position, size);
        }

        /// <summary>
        /// Calculates optimal spacing between objects based on their sizes.
        /// </summary>
        public static float CalculateOptimalSpacing(Vector3 size1, Vector3 size2)
        {
            float maxDimension = Mathf.Max(
                Mathf.Max(size1.x, size1.y, size1.z),
                Mathf.Max(size2.x, size2.y, size2.z)
            );
            
            return maxDimension * 1.2f; // 20% buffer
        }

        /// <summary>
        /// Applies elevation offset to prevent objects from sinking into ground.
        /// </summary>
        public static Vector3 ApplyGroundOffset(Vector3 position, Vector3 size)
        {
            return new Vector3(position.x, position.y + size.y / 2f, position.z);
        }

        /// <summary>
        /// Ensures symmetry in radial structures by evenly distributing angles.
        /// </summary>
        public static float[] GetSymmetricRadialAngles(int count, float startAngle = 0f)
        {
            float[] angles = new float[count];
            float angleStep = 360f / count;
            
            for (int i = 0; i < count; i++)
            {
                angles[i] = (startAngle + i * angleStep) * Mathf.Deg2Rad;
            }
            
            return angles;
        }

        /// <summary>
        /// Adjusts spacing in a grid to prevent overlap based on object sizes.
        /// </summary>
        public static Vector2 AdjustGridSpacing(Vector2 baseSpacing, Vector3 objectSize)
        {
            float minSpacing = Mathf.Max(objectSize.x, objectSize.z) * 1.1f;
            return new Vector2(Mathf.Max(baseSpacing.x, minSpacing), Mathf.Max(baseSpacing.y, minSpacing));
        }

        /// <summary>
        /// Finds a non-overlapping position near the desired position.
        /// </summary>
        public static Vector3 FindNonOverlappingPosition(Vector3 desiredPosition, Vector3 size, string id, float maxSearchRadius = 5f)
        {
            if (!WouldOverlap(desiredPosition, size))
            {
                RegisterSpace(id, desiredPosition, size);
                return desiredPosition;
            }

            // Search in expanding circles
            for (float radius = 0.5f; radius <= maxSearchRadius; radius += 0.5f)
            {
                int attempts = Mathf.RoundToInt(radius * 8);
                
                for (int i = 0; i < attempts; i++)
                {
                    float angle = (i * 360f / attempts) * Mathf.Deg2Rad;
                    Vector3 testPosition = desiredPosition + new Vector3(
                        Mathf.Cos(angle) * radius,
                        0,
                        Mathf.Sin(angle) * radius
                    );
                    
                    if (!WouldOverlap(testPosition, size))
                    {
                        RegisterSpace(id, testPosition, size);
                        return testPosition;
                    }
                }
            }

            // If no position found, return original and accept overlap
            Debug.LogWarning($"[SceneCompositionHelper] Could not find non-overlapping position for {id}");
            RegisterSpace(id, desiredPosition, size);
            return desiredPosition;
        }

        /// <summary>
        /// Calculates a visually pleasing arrangement height based on object count.
        /// </summary>
        public static float CalculateArrangementHeight(int objectCount, float baseHeight = 0f)
        {
            // Create a gentle curve for more objects
            return baseHeight + Mathf.Sqrt(objectCount) * 0.5f;
        }

        /// <summary>
        /// Ensures objects are within a reasonable distance from the center.
        /// </summary>
        public static Vector3 ClampToSceneBounds(Vector3 position, float maxRadius)
        {
            if (position.magnitude > maxRadius)
            {
                return position.normalized * maxRadius;
            }
            return position;
        }

        /// <summary>
        /// Applies subtle randomization to break up perfect patterns while maintaining structure.
        /// </summary>
        public static Vector3 ApplySubtleVariation(Vector3 position, float variationAmount = 0.1f)
        {
            return position + new Vector3(
                Random.Range(-variationAmount, variationAmount),
                0,
                Random.Range(-variationAmount, variationAmount)
            );
        }

        /// <summary>
        /// Ensures balanced spacing in a grid arrangement.
        /// </summary>
        public static Vector2 CalculateBalancedGridSpacing(int totalObjects, float availableArea)
        {
            // Calculate optimal grid based on object count
            int columns = Mathf.CeilToInt(Mathf.Sqrt(totalObjects));
            int rows = Mathf.CeilToInt((float)totalObjects / columns);
            
            // Calculate spacing to fit available area
            float spacingX = availableArea / columns;
            float spacingZ = availableArea / rows;
            
            return new Vector2(spacingX, spacingZ);
        }

        /// <summary>
        /// Creates visual hierarchy by varying object sizes based on importance.
        /// </summary>
        public static float CalculateHierarchyScale(int index, int totalObjects, float baseScale)
        {
            // Center objects are larger (more important)
            float centerDistance = Mathf.Abs(index - totalObjects / 2f);
            float maxDistance = totalObjects / 2f;
            float normalizedDistance = centerDistance / maxDistance;
            
            // Scale from 1.5x at center to 0.7x at edges
            float scaleFactor = Mathf.Lerp(1.5f, 0.7f, normalizedDistance);
            return baseScale * scaleFactor;
        }

        /// <summary>
        /// Ensures objects are arranged around a clear center focal point.
        /// </summary>
        public static Vector3 ArrangeAroundCenter(Vector3 center, int index, int totalObjects, float radius)
        {
            // Use golden angle for better distribution
            float goldenAngle = 137.5f * Mathf.Deg2Rad;
            float angle = index * goldenAngle;
            
            // Vary radius slightly for visual interest
            float radiusVariation = radius * (0.8f + 0.4f * Mathf.PerlinNoise(index * 0.1f, 0));
            
            return center + new Vector3(
                Mathf.Cos(angle) * radiusVariation,
                0,
                Mathf.Sin(angle) * radiusVariation
            );
        }

        /// <summary>
        /// Creates a clear focal point by positioning the most important object at center.
        /// </summary>
        public static void EstablishFocalPoint(Transform parent, GameObject focalObject)
        {
            // Place focal object at scene center
            focalObject.transform.SetParent(parent);
            focalObject.transform.localPosition = Vector3.zero;
            
            // Make it slightly larger for emphasis
            focalObject.transform.localScale *= 1.2f;
        }
    }
}
