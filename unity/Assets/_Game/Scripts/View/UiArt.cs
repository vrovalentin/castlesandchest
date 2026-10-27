using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.View
{
    /// <summary>
    /// Code-drawn UI textures for the temporary IMGUI screens: rounded panels and a few icons.
    /// They stand in until the UI art from the art list (Art/UI) is ready.
    /// </summary>
    public static class UiArt
    {
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        public static readonly Color Ink = new Color32(0x3a, 0x2a, 0x1c, 0xff);
        private static readonly Color Outline = new Color32(0x3a, 0x2a, 0x1c, 0xff);
        private static readonly Color Wood = new Color32(0xc0, 0x86, 0x4c, 0xff);
        private static readonly Color Steel = new Color32(0x9a, 0xa3, 0xae, 0xff);
        private static readonly Color Straw = new Color32(0xf2, 0xc4, 0x5a, 0xff);

        /// <summary>
        /// Rounded rectangle, 64×64, for a GUIStyle background with border = <paramref name="radius"/> on every side.
        /// </summary>
        public static Texture2D Round(Color fill, Color border, int radius = 18, float borderWidth = 3)
        {
            string key = $"round{(Color32)fill}{(Color32)border}{radius}{borderWidth}";
            if (Cache.TryGetValue(key, out var t) && t != null) return t;
            const int n = 64;
            t = Draw(n, (x, y) =>
            {
                // Signed distance to a rounded box filling the texture.
                float qx = Mathf.Abs(x - n / 2f) - (n / 2f - radius);
                float qy = Mathf.Abs(y - n / 2f) - (n / 2f - radius);
                float d = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0) - radius;
                float a = Mathf.Clamp01(0.5f - d);
                float inner = Mathf.Clamp01(0.5f - (d + borderWidth));
                var c = Color.Lerp(border, fill, inner);
                c.a *= a;
                return c;
            });
            Cache[key] = t;
            return t;
        }

        private static Font _text, _bold;

        /// <summary>Nunito Bold, with Cyrillic. The browser build has no system fonts, so the UI must bring its own.</summary>
        public static Font TextFont => _text != null ? _text : (_text = Resources.Load<Font>("Fonts/Nunito-Bold"));

        /// <summary>Nunito Black, used where a style asks for bold.</summary>
        public static Font BoldFont => _bold != null ? _bold : (_bold = Resources.Load<Font>("Fonts/Nunito-Black"));

        /// <summary>
        /// Puts the game font on the skin and on every GUIStyle field of <paramref name="owner"/>.
        /// Styles marked bold get the Black weight instead of a faked bold.
        /// </summary>
        public static void UseGameFont(object owner)
        {
            if (TextFont == null) return;
            GUI.skin.font = TextFont;
            const System.Reflection.BindingFlags all = System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            foreach (var f in owner.GetType().GetFields(all))
            {
                if (f.FieldType != typeof(GUIStyle) || !(f.GetValue(owner) is GUIStyle st)) continue;
                bool bold = st.fontStyle == FontStyle.Bold || st.fontStyle == FontStyle.BoldAndItalic;
                st.font = bold && BoldFont != null ? BoldFont : TextFont;
                if (bold) st.fontStyle = FontStyle.Normal;
            }
        }

        /// <summary>A filled circle with a border; stretched over a rect it makes an ellipse.</summary>
        public static Texture2D Disc(Color fill, Color border, float borderWidth) => Round(fill, border, 32, borderWidth);

        /// <summary>A round soft blob that fades out to the edge, for shadows.</summary>
        public static Texture2D SoftDisc(Color c)
        {
            string key = $"soft{(Color32)c}";
            if (Cache.TryGetValue(key, out var t) && t != null) return t;
            const int n = 64;
            t = Draw(n, (x, y) =>
            {
                float d = new Vector2(x - n / 2f, y - n / 2f).magnitude / (n / 2f);
                var k = c;
                k.a *= Mathf.SmoothStep(1, 0, Mathf.InverseLerp(0.35f, 1f, d));
                return k;
            });
            Cache[key] = t;
            return t;
        }

        public static GUIStyle Style(Texture2D back, int radius, int fontSize, Color text, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var s = new GUIStyle
            {
                border = new RectOffset(radius, radius, radius, radius),
                fontSize = fontSize,
                alignment = align,
                wordWrap = true,
                padding = new RectOffset(12, 12, 6, 6),
                richText = true,
            };
            s.normal.background = back;
            s.normal.textColor = text;
            return s;
        }

        public static Texture2D Back => Icon("back", 96, (u, v) => Min(
            Seg(u, v, 0.24f, 0.5f, 0.78f, 0.5f),
            Seg(u, v, 0.24f, 0.5f, 0.47f, 0.73f),
            Seg(u, v, 0.24f, 0.5f, 0.47f, 0.27f)) - 0.055f, Ink);

        public static Texture2D Restart => Icon("restart", 96, RestartShape, Ink);

        public static Texture2D Hammer => Layered("hammer", 96, new (Func<float, float, float>, Color)[]
        {
            ((u, v) => Seg(u, v, 0.26f, 0.18f, 0.58f, 0.58f) - 0.06f, Wood),
            ((u, v) => Seg(u, v, 0.43f, 0.79f, 0.79f, 0.49f) - 0.12f, Steel),
        });

        public static Texture2D Broom => Layered("broom", 96, new (Func<float, float, float>, Color)[]
        {
            ((u, v) => Seg(u, v, 0.78f, 0.88f, 0.5f, 0.46f) - 0.045f, Wood),
            ((u, v) => Min(Seg(u, v, 0.48f, 0.44f, 0.16f, 0.2f), Seg(u, v, 0.48f, 0.44f, 0.27f, 0.12f),
                Seg(u, v, 0.48f, 0.44f, 0.4f, 0.1f)) - 0.08f, Straw),
            ((u, v) => Seg(u, v, 0.4f, 0.5f, 0.56f, 0.4f) - 0.04f, Outline),
        });

        private static float RestartShape(float u, float v)
        {
            // An open ring from 100° round to 400° (= 40°), with an arrow head at the 40° end.
            const float r = 0.27f, cx = 0.5f, cy = 0.5f, from = 100f, to = 400f;
            float dx = u - cx, dy = v - cy;
            float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            while (ang < from) ang += 360f;
            float ring = ang <= to ? Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - r) : float.MaxValue;
            float e = to * Mathf.Deg2Rad;
            float ex = cx + r * Mathf.Cos(e), ey = cy + r * Mathf.Sin(e);
            float tx = -Mathf.Sin(e), ty = Mathf.Cos(e);
            float tipX = ex + tx * 0.09f, tipY = ey + ty * 0.09f;
            float bx = ex - tx * 0.07f, by = ey - ty * 0.07f;
            float head = Min(
                Seg(u, v, tipX, tipY, bx + ty * 0.15f, by - tx * 0.15f),
                Seg(u, v, tipX, tipY, bx - ty * 0.15f, by + tx * 0.15f));
            return Mathf.Min(ring, head) - 0.055f;
        }

        private static Texture2D Icon(string key, int n, Func<float, float, float> sdf, Color color) =>
            Layered(key, n, new[] { (sdf, color) }, false);

        /// <summary>Shapes drawn in order, each with a dark outline, from signed distances in 0..1 units.</summary>
        private static Texture2D Layered(string key, int n, (Func<float, float, float> sdf, Color color)[] layers, bool outline = true)
        {
            if (Cache.TryGetValue(key, out var t) && t != null) return t;
            const float line = 0.035f;
            t = Draw(n, (x, y) =>
            {
                float u = x / n, v = y / n;
                var c = Color.clear;
                foreach (var (sdf, color) in layers)
                {
                    float d = sdf(u, v);
                    if (outline) c = Over(c, Outline, Mathf.Clamp01(0.5f - (d - line) * n));
                    c = Over(c, color, Mathf.Clamp01(0.5f - d * n));
                }
                return c;
            });
            Cache[key] = t;
            return t;
        }

        private static Color Over(Color under, Color top, float a)
        {
            float outA = a + under.a * (1 - a);
            if (outA <= 0) return Color.clear;
            var c = (top * a + under * under.a * (1 - a)) / outA;
            c.a = outA;
            return c;
        }

        private static float Min(params float[] d)
        {
            float m = float.MaxValue;
            foreach (var x in d) m = Mathf.Min(m, x);
            return m;
        }

        private static float Seg(float u, float v, float ax, float ay, float bx, float by)
        {
            float ex = bx - ax, ey = by - ay;
            float t = Mathf.Clamp01(((u - ax) * ex + (v - ay) * ey) / (ex * ex + ey * ey));
            float dx = u - ax - t * ex, dy = v - ay - t * ey;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>Texture from a pixel function; (x, y) is the pixel centre, y up.</summary>
        private static Texture2D Draw(int n, Func<float, float, Color> pixel)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    px[y * n + x] = pixel(x + 0.5f, y + 0.5f);
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }
    }
}
