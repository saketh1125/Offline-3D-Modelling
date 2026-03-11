using UnityEngine;

namespace ThreeDBuilder.Procedural
{
    /// <summary>
    /// Applies subtle random variation to procedurally spawned objects to break
    /// the perfect-grid look while preserving GPU instancing.
    ///
    /// Transform variation (scale/rotation) is applied directly.
    /// Brightness variation is applied via MaterialPropertyBlock — this preserves
    /// GPU instancing because no material instance is created.
    /// </summary>
    public static class ProceduralVariationSystem
    {
        // Variation magnitudes
        private const float SCALE_JITTER_PERCENT    = 0.08f;  // ±8%  (was ±5%)
        private const float ROTATION_JITTER_DEGREES = 6.0f;   // ±6°  (was ±3°)
        private const float BRIGHTNESS_JITTER       = 0.08f;  // ±8% brightness via PropertyBlock

        private const float MIN_SCALE = 0.1f; // prevent degenerate zero-size objects

        /// <summary>
        /// Applies scale and rotation jitter to the GameObject's transform.
        /// </summary>
        public static void ApplyTransformVariation(GameObject obj)
        {
            if (obj == null) return;

            Transform t = obj.transform;

            // Scale jitter: ±8% per axis independently, clamped to prevent zero
            Vector3 scale = t.localScale;
            scale.x = Mathf.Max(scale.x * (1f + Random.Range(-SCALE_JITTER_PERCENT, SCALE_JITTER_PERCENT)), MIN_SCALE);
            scale.y = Mathf.Max(scale.y * (1f + Random.Range(-SCALE_JITTER_PERCENT, SCALE_JITTER_PERCENT)), MIN_SCALE);
            scale.z = Mathf.Max(scale.z * (1f + Random.Range(-SCALE_JITTER_PERCENT, SCALE_JITTER_PERCENT)), MIN_SCALE);
            t.localScale = scale;

            // Rotation jitter: Y axis only to keep objects upright
            Vector3 euler = t.localEulerAngles;
            euler.y += Random.Range(-ROTATION_JITTER_DEGREES, ROTATION_JITTER_DEGREES);
            t.localEulerAngles = euler;
        }

        /// <summary>
        /// Applies a subtle brightness variation via MaterialPropertyBlock.
        /// This does NOT create a material instance, so GPU instancing is preserved.
        /// </summary>
        public static void ApplyColorVariation(MeshRenderer renderer)
        {
            if (renderer == null || renderer.sharedMaterial == null) return;

            float jitter = 1f + Random.Range(-BRIGHTNESS_JITTER, BRIGHTNESS_JITTER);
            Color baseColor = renderer.sharedMaterial.color;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_Color", new Color(
                Mathf.Clamp01(baseColor.r * jitter),
                Mathf.Clamp01(baseColor.g * jitter),
                Mathf.Clamp01(baseColor.b * jitter),
                baseColor.a));
            renderer.SetPropertyBlock(block);
        }

        /// <summary>
        /// Convenience method: applies transform variation and brightness variation.
        /// </summary>
        public static void Apply(GameObject obj, MeshRenderer renderer)
        {
            ApplyTransformVariation(obj);
            ApplyColorVariation(renderer);
        }
    }
}
