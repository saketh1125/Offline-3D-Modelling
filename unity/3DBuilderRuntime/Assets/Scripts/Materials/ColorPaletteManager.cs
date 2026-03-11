using UnityEngine;

namespace ThreeDBuilder.Materials
{
    /// <summary>
    /// Maps input colors into a curated 12-color educational palette, preventing neon/ugly
    /// colors and maintaining stylized consistency across procedural scenes.
    /// </summary>
    public static class ColorPaletteManager
    {
        // ─────────────────────────────────────────────────────────────────
        // Curated 12-color Palette (educational / hackathon friendly)
        // ─────────────────────────────────────────────────────────────────

        private static readonly Color[] CuratedPalette = new Color[]
        {
            new Color(0.10f, 0.20f, 0.40f), // 00 Deep Blue
            new Color(1.00f, 0.82f, 0.18f), // 01 Gold
            new Color(0.18f, 0.55f, 0.55f), // 02 Teal
            new Color(0.90f, 0.38f, 0.32f), // 03 Coral
            new Color(0.44f, 0.46f, 0.52f), // 04 Slate
            new Color(0.18f, 0.45f, 0.22f), // 05 Forest Green
            new Color(0.92f, 0.62f, 0.20f), // 06 Warm Amber
            new Color(0.58f, 0.36f, 0.52f), // 07 Mauve
            new Color(0.94f, 0.94f, 0.94f), // 08 Soft White
            new Color(0.18f, 0.18f, 0.22f), // 09 Charcoal
            new Color(0.50f, 0.72f, 0.90f), // 10 Sky Blue
            new Color(0.72f, 0.38f, 0.26f), // 11 Terracotta
        };

        // Thresholds
        private const float NEON_SATURATION_THRESHOLD = 0.85f;
        private const float NEON_VALUE_THRESHOLD      = 0.80f;
        private const float MAX_SATURATION  = 0.75f;  // was 0.70
        private const float MIN_BRIGHTNESS  = 0.18f;  // was 0.20
        private const float MAX_BRIGHTNESS  = 0.90f;  // was 0.85

        // Ground contrast threshold
        private const float GROUND_CONTRAST_THRESHOLD = 0.3f;

        // ─────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Sanitizes an input color: snaps to palette if neon, otherwise clamps
        /// saturation and brightness to professional ranges.
        /// </summary>
        public static Color SanitizeColor(Color input)
        {
            float h, s, v;
            Color.RGBToHSV(input, out h, out s, out v);

            // Neon detection: high saturation + high value = eye-burning
            if (s > NEON_SATURATION_THRESHOLD && v > NEON_VALUE_THRESHOLD)
                return FindNearestPaletteColor(input);

            // Clamp saturation and brightness to professional ranges
            s = Mathf.Clamp(s, 0f, MAX_SATURATION);
            v = Mathf.Clamp(v, MIN_BRIGHTNESS, MAX_BRIGHTNESS);

            return Color.HSVToRGB(h, s, v);
        }

        /// <summary>
        /// Applies ±variationPercent% random brightness jitter to a color.
        /// Used to break uniformity in repeated procedural objects.
        /// </summary>
        public static Color ApplyVariation(Color baseColor, float variationPercent = 0.05f)
        {
            float h, s, v;
            Color.RGBToHSV(baseColor, out h, out s, out v);

            float jitter = Random.Range(-variationPercent, variationPercent);
            v = Mathf.Clamp(v + jitter, MIN_BRIGHTNESS, MAX_BRIGHTNESS);

            Color result = Color.HSVToRGB(h, s, v);
            result.a = baseColor.a;
            return result;
        }

        /// <summary>
        /// Clamps extremely dark colors up and extremely bright colors down.
        /// Used by SceneReadabilityEnhancer as a post-pass.
        /// </summary>
        public static Color ClampBrightness(Color input)
        {
            float h, s, v;
            Color.RGBToHSV(input, out h, out s, out v);

            if (v < 0.12f) v = MIN_BRIGHTNESS;
            if (v > 0.95f) v = MAX_BRIGHTNESS;

            Color result = Color.HSVToRGB(h, s, v);
            result.a = input.a;
            return result;
        }

        /// <summary>
        /// Shifts a color's hue by the given amount (wrapping around 0-1).
        /// Used to create contrast between adjacent same-color objects.
        /// </summary>
        public static Color ShiftHue(Color input, float hueShift)
        {
            float h, s, v;
            Color.RGBToHSV(input, out h, out s, out v);

            h = (h + hueShift) % 1f;
            if (h < 0f) h += 1f;

            // Nudge brightness slightly for extra contrast
            v = Mathf.Clamp(v + 0.08f, MIN_BRIGHTNESS, MAX_BRIGHTNESS);

            Color result = Color.HSVToRGB(h, s, v);
            result.a = input.a;
            return result;
        }

        /// <summary>
        /// Ensures the object color has sufficient contrast with ground color.
        /// </summary>
        public static Color EnsureGroundContrast(Color objectColor, Color groundColor)
        {
            float objectLuminance = objectColor.grayscale;
            float groundLuminance = groundColor.grayscale;
            float luminanceDiff = Mathf.Abs(objectLuminance - groundLuminance);

            if (luminanceDiff < GROUND_CONTRAST_THRESHOLD)
            {
                float h, s, v;
                Color.RGBToHSV(objectColor, out h, out s, out v);

                if (groundLuminance > 0.5f)
                    v = Mathf.Max(v - 0.3f, MIN_BRIGHTNESS);  // ground light → darken object
                else
                    v = Mathf.Min(v + 0.3f, MAX_BRIGHTNESS);  // ground dark  → lighten object

                Color adjusted = Color.HSVToRGB(h, s, v);
                adjusted.a = objectColor.a;

                Debug.Log($"[ColorPaletteManager] Adjusted for ground contrast: {objectColor} → {adjusted}");
                return adjusted;
            }

            return objectColor;
        }

        // ─────────────────────────────────────────────────────────────────
        // Internal
        // ─────────────────────────────────────────────────────────────────

        private static Color FindNearestPaletteColor(Color input)
        {
            Color nearest = CuratedPalette[0];
            float minDist = float.MaxValue;

            for (int i = 0; i < CuratedPalette.Length; i++)
            {
                float dist = ColorDistanceSq(input, CuratedPalette[i]);
                if (dist < minDist) { minDist = dist; nearest = CuratedPalette[i]; }
            }

            return nearest;
        }

        private static float ColorDistanceSq(Color a, Color b)
        {
            float dr = a.r - b.r, dg = a.g - b.g, db = a.b - b.b;
            return dr * dr + dg * dg + db * db;
        }
    }
}
