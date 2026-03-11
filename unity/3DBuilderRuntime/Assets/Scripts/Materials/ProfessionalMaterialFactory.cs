using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using ThreeDBuilder.Scene;

namespace ThreeDBuilder.Materials
{
    /// <summary>
    /// Creates physically reasonable Standard shader materials with PBR properties,
    /// shadow configuration, and GPU instancing. Caches materials by sanitized color +
    /// quantized metallic + quantized smoothness so that different PBR properties produce
    /// distinct materials without blowing up the material count.
    ///
    /// Brightness variation is applied via MaterialPropertyBlock so GPU instancing
    /// is fully preserved — no per-instance material copies.
    /// </summary>
    public class ProfessionalMaterialFactory
    {
        // Cache key: quantized color + metallic + smoothness
        private readonly Dictionary<long, Material> _materialCache = new Dictionary<long, Material>();

        // PBR defaults
        private const float DEFAULT_METALLIC    = 0.05f;
        private const float DEFAULT_SMOOTHNESS  = 0.55f; // Upgraded from 0.45

        // Shared shader reference (cached once)
        private Shader _standardShader;
       
        public ProfessionalMaterialFactory()
        {
            _standardShader = Shader.Find("Standard");
            if (_standardShader == null)
                Debug.LogError("[ProfessionalMaterialFactory] Standard shader not found!");
        }

        /// <summary>
        /// Creates or retrieves a cached material for the given MaterialModel.
        /// Cache key includes color + metallic + smoothness so per-object PBR
        /// overrides work without creating unwanted duplicates.
        /// </summary>
        public Material CreateMaterial(MaterialModel materialModel)
        {
            // Ensure we have a valid shader
            if (_standardShader == null)
            {
                Debug.LogWarning("[ProfessionalMaterialFactory] Standard shader missing. Using fallback.");
                _standardShader = Shader.Find("Standard");

                if (_standardShader == null)
                {
                    Debug.LogError("[ProfessionalMaterialFactory] Failed to find Standard shader.");
                    return new Material(Shader.Find("Standard"));
                }
            }

            // ── Base color ─────────────────────────────────────────────
            Color rawColor = Color.white;

            if (materialModel != null &&
                materialModel.baseColor != null &&
                materialModel.baseColor.Length >= 3)
            {
                float alpha = materialModel.baseColor.Length >= 4
                    ? materialModel.baseColor[3]
                    : 1f;

                rawColor = new Color(
                    materialModel.baseColor[0],
                    materialModel.baseColor[1],
                    materialModel.baseColor[2],
                    alpha
                );
            }

            // Sanitize color using palette manager
            Color sanitized = ColorPaletteManager.SanitizeColor(rawColor);

            // ── PBR defaults ───────────────────────────────────────────
            float metallic = DEFAULT_METALLIC;
            float smoothness = DEFAULT_SMOOTHNESS;

            if (materialModel != null)
            {
                if (materialModel.metallic >= 0f)
                    metallic = Mathf.Clamp01(materialModel.metallic);

                if (materialModel.smoothness >= 0f)
                    smoothness = Mathf.Clamp01(materialModel.smoothness);
            }

            // ── Cache lookup ───────────────────────────────────────────
            long cacheKey = BuildCacheKey(sanitized, metallic, smoothness);

            if (_materialCache.TryGetValue(cacheKey, out Material cached))
                return cached;

            // ── Create new material ─────────────────────────────────────
            Material mat = new Material(_standardShader);

            mat.name = (materialModel != null && !string.IsNullOrWhiteSpace(materialModel.id))
                ? $"Pro_{materialModel.id}"
                : $"Pro_{ColorUtility.ToHtmlStringRGB(sanitized)}";

            SetOpaqueMode(mat);

            mat.color = sanitized;
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Glossiness", smoothness);

            // Enable GPU instancing
            mat.enableInstancing = true;

            // Enable vertex color keyword for gradient shading
            mat.EnableKeyword("_VERTEX_COLOR");

            Debug.Log("[ProfessionalMaterialFactory] Material created with metallic=" + metallic + " smoothness=" + smoothness);

            // Store in cache
            _materialCache[cacheKey] = mat;

            return mat;
        }

        /// <summary>
        /// Configures a MeshRenderer with professional shadow settings.
        /// Call this on every renderer after assigning the material.
        /// </summary>
        public static void ConfigureRenderer(MeshRenderer renderer)
        {
            if (renderer == null) return;
            renderer.receiveShadows  = true;
            renderer.shadowCastingMode = ShadowCastingMode.On;
        }

        /// <summary>
        /// Applies a brightness variation to a renderer via MaterialPropertyBlock,
        /// preserving GPU instancing (no material instance is created).
        /// variationPercent: e.g. 0.08 = ±8% brightness.
        /// </summary>
        public static void ApplyBrightnessVariation(MeshRenderer renderer, float variationPercent = 0.08f)
        {
            if (renderer == null) return;

            float jitter = 1f + Random.Range(-variationPercent, variationPercent);
            Color base_col = renderer.sharedMaterial != null ? renderer.sharedMaterial.color : Color.white;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_Color", base_col * jitter);
            renderer.SetPropertyBlock(block);
        }

        public int CachedMaterialCount => _materialCache.Count;

        public void ClearCache()
        {
            _materialCache.Clear();
        }

        // ─────────────────────────────────────────────────────────────────
        // Internal Helpers
        // ─────────────────────────────────────────────────────────────────

        private static void SetOpaqueMode(Material mat)
        {
            mat.SetFloat("_Mode", 0f);
            mat.SetInt("_SrcBlend", (int)BlendMode.One);
            mat.SetInt("_DstBlend", (int)BlendMode.Zero);
            mat.SetInt("_ZWrite", 1);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = -1;
        }

        /// <summary>
        /// Builds a 64-bit cache key from quantized color (24-bit) + metallic (8-bit) + smoothness (8-bit).
        /// Avoids float-precision mismatches.
        /// </summary>
        private static long BuildCacheKey(Color c, float metallic, float smoothness)
        {
            int r  = Mathf.RoundToInt(c.r * 255f);
            int g  = Mathf.RoundToInt(c.g * 255f);
            int b  = Mathf.RoundToInt(c.b * 255f);
            int m  = Mathf.RoundToInt(metallic   * 255f);
            int sm = Mathf.RoundToInt(smoothness  * 255f);

            return ((long)r << 32) | ((long)g << 24) | ((long)b << 16) | ((long)m << 8) | sm;
        }
    }
}
