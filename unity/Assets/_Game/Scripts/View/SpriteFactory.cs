using System;
using UnityEngine;

namespace Game.View
{
    /// <summary>
    /// Placeholder sprites drawn in code, so the board works before there is any art.
    /// Every sprite is 1 world unit wide. Body sprites are greyscale and get tinted by the block colour.
    /// </summary>
    public static class SpriteFactory
    {
        private const int Size = 64;
        private static Sprite _block, _chestBody, _chestTrim, _star, _starShadow, _panel;

        public static Sprite Block => _block != null ? _block : _block = Make(BlockPixel);
        public static Sprite ChestBody => _chestBody != null ? _chestBody : _chestBody = Make(ChestBodyPixel);
        public static Sprite ChestTrim => _chestTrim != null ? _chestTrim : _chestTrim = Make(ChestTrimPixel);
        public static Sprite Star => _star != null ? _star : _star = Make(StarPixel);
        /// <summary>Soft dark blur of the star, drawn under it at <see cref="StarShadowScale"/> for contrast.</summary>
        public static Sprite StarShadow => _starShadow != null ? _starShadow : _starShadow = Make(StarShadowPixel);
        public const float StarShadowScale = 1.25f;
        public static Sprite Panel => _panel != null ? _panel : _panel = Make(PanelPixel);

        private static readonly string[] ColorNames = { "green", "brown", "gray", "blue", "purple" };
        private static Sprite[] _blockArt, _chestArt;

        /// <summary>Drawn block of a colour from Art/Resources/Board, or null when there is no art for it.</summary>
        public static Sprite BlockArt(int color) => Art(ref _blockArt, "block_", color);

        /// <summary>Drawn closed chest of a colour from Art/Resources/Board, or null when there is no art for it.</summary>
        public static Sprite ChestArt(int color) => Art(ref _chestArt, "chest_", color);

        private static Sprite Art(ref Sprite[] cache, string prefix, int color)
        {
            if (cache == null)
            {
                cache = new Sprite[ColorNames.Length];
                for (int i = 0; i < ColorNames.Length; i++)
                {
                    var tex = Resources.Load<Texture2D>("Board/" + prefix + ColorNames[i]);
                    if (tex == null) continue;
                    // Slightly smaller than 1 unit, like the placeholder block with its gap.
                    cache[i] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width / 0.94f);
                    cache[i].hideFlags = HideFlags.DontSave;
                }
            }
            return color >= 0 && color < cache.Length ? cache[color] : null;
        }

        private static Sprite Make(Func<float, float, Color> pixel)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
            var px = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    // 2×2 supersampling for smooth edges.
                    Color acc = Color.clear;
                    for (int sy = 0; sy < 2; sy++)
                        for (int sx = 0; sx < 2; sx++)
                            acc += pixel((x + (sx + 0.5f) / 2f) / Size, (y + (sy + 0.5f) / 2f) / Size);
                    px[y * Size + x] = acc / 4f;
                }
            tex.SetPixels(px);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        /// <summary>Signed distance to a rounded square centred in the texture. Negative inside.</summary>
        private static float RoundRect(float u, float v, float half, float radius)
        {
            float x = Mathf.Abs(u - 0.5f) - (half - radius);
            float y = Mathf.Abs(v - 0.5f) - (half - radius);
            float ox = Mathf.Max(x, 0), oy = Mathf.Max(y, 0);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(x, y), 0) - radius;
        }

        private static Color Grey(float g) => new Color(g, g, g, 1);

        private static Color BlockPixel(float u, float v)
        {
            float d = RoundRect(u, v, 0.46f, 0.13f);
            if (d > 0) return Color.clear;
            if (d > -0.045f) return Grey(0.62f);
            // Lighter at the top, like a soft bevel.
            return Grey(Mathf.Lerp(0.8f, 1f, Mathf.InverseLerp(0.25f, 0.9f, v)));
        }

        private static Color ChestBodyPixel(float u, float v)
        {
            float d = RoundRect(u, v, 0.44f, 0.1f);
            if (d > 0) return Color.clear;
            return Grey(Mathf.Lerp(0.75f, 1f, Mathf.InverseLerp(0.2f, 0.9f, v)));
        }

        private static readonly Color Dark = new Color(0.29f, 0.2f, 0.125f, 1);
        private static readonly Color Gold = new Color(0.95f, 0.69f, 0.2f, 1);

        private static Color ChestTrimPixel(float u, float v)
        {
            float d = RoundRect(u, v, 0.44f, 0.1f);
            if (d > 0) return Color.clear;
            if (d > -0.04f) return Dark;
            bool lockBox = Mathf.Abs(u - 0.5f) < 0.09f && v > 0.5f && v < 0.72f;
            if (lockBox) return Mathf.Abs(u - 0.5f) > 0.06f || v < 0.53f || v > 0.69f ? Dark : Gold;
            if (v > 0.6f && v < 0.68f) return Gold;
            return Color.clear;
        }

        private static readonly Color StarTop = new Color(1f, 0.96f, 0.6f, 1);
        private static readonly Color StarBottom = new Color(1f, 0.74f, 0.1f, 1);

        private static Color StarPixel(float u, float v)
        {
            if (!InStar(u, v, 1f)) return Color.clear;
            if (!InStar(u, v, 0.74f)) return Dark;
            // Bright gold, lighter at the top, with a small highlight near the top point.
            var c = Color.Lerp(StarBottom, StarTop, Mathf.InverseLerp(0.25f, 0.85f, v));
            float hx = u - 0.46f, hy = v - 0.66f;
            float glint = Mathf.Clamp01(1f - (hx * hx + hy * hy) / 0.012f);
            return Color.Lerp(c, Color.white, glint * 0.7f);
        }

        private static Color StarShadowPixel(float u, float v)
        {
            // Star shrunk by the shadow's scale so it lines up with the star, then blurred outwards.
            const float k = 1f / StarShadowScale, blur = 0.1f;
            float su = 0.5f + (u - 0.5f) / k, sv = 0.5f + (v - 0.5f) / k;
            float d = InStar(su, sv, 1f) ? 0f : StarDistance(su, sv) * k;
            float a = 0.6f * Mathf.SmoothStep(0f, 1f, 1f - d / blur);
            return new Color(0.08f, 0.05f, 0.02f, a);
        }

        private static float StarDistance(float u, float v)
        {
            float best = float.MaxValue;
            for (int i = 0, j = 9; i < 10; j = i++)
            {
                StarVertex(i, 1f, out float xi, out float yi);
                StarVertex(j, 1f, out float xj, out float yj);
                float ex = xj - xi, ey = yj - yi;
                float t = Mathf.Clamp01(((u - xi) * ex + (v - yi) * ey) / (ex * ex + ey * ey));
                float dx = u - xi - t * ex, dy = v - yi - t * ey;
                best = Mathf.Min(best, dx * dx + dy * dy);
            }
            return Mathf.Sqrt(best);
        }

        private static Color PanelPixel(float u, float v) => RoundRect(u, v, 0.5f, 0.06f) > 0 ? Color.clear : Color.white;

        private static bool InStar(float u, float v, float scale)
        {
            // Five-point star, point up. Ray-casting point-in-polygon test.
            const int n = 10;
            bool inside = false;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                StarVertex(i, scale, out float xi, out float yi);
                StarVertex(j, scale, out float xj, out float yj);
                if ((yi > v) != (yj > v) && u < (xj - xi) * (v - yi) / (yj - yi) + xi) inside = !inside;
            }
            return inside;
        }

        private static void StarVertex(int i, float scale, out float x, out float y)
        {
            float a = Mathf.PI / 2 + i * Mathf.PI / 5;
            float r = (i % 2 == 0 ? 0.49f : 0.21f) * scale;
            x = 0.5f + r * Mathf.Cos(a);
            y = 0.5f + r * Mathf.Sin(a);
        }
    }
}
