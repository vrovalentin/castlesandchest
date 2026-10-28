using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.View
{
    /// <summary>
    /// Drawn art loaded from Art/Resources for the IMGUI screens: open chests, part icons, backgrounds, sparkle.
    /// Every getter returns null when the picture isn't there yet, and callers fall back to code-drawn shapes.
    /// </summary>
    public static class GameArt
    {
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        private static Texture2D Load(string path)
        {
            if (!Cache.TryGetValue(path, out var t)) Cache[path] = t = Resources.Load<Texture2D>(path);
            return t;
        }

        public static Texture2D ChestOpen(int color) =>
            Load($"Board/chest_{SpriteFactory.ColorNames[Mathf.Clamp(color, 0, SpriteFactory.ColorNames.Length - 1)]}_open");

        /// <summary>Icon of a part (Art/Resources/Parts/part_&lt;id&gt;.png).</summary>
        public static Texture2D Part(string id) => Load("Parts/part_" + id);

        public static Texture2D Sparkle => Load("Fx/fx_sparkle");
        /// <summary>Bright golden four-point star without an outline (tools/art/make_reward_ui.py), like the rest below.</summary>
        public static Texture2D Glint => Load("Fx/fx_glint");
        public static Texture2D Rays => Load("Fx/fx_rays");
        public static Texture2D Plate => Load("Fx/fx_plate");
        public static Texture2D NightPanel => Load("UI/ui_panel_night");
        public static Texture2D Ribbon => Load("UI/ui_ribbon");
        public static Texture2D GoldButton => Load("UI/ui_btn_gold");
        public static Texture2D GoldButtonDown => Load("UI/ui_btn_gold_down");
        public static Texture2D MineBackground => Load("UI/bg_mine");
        public static Texture2D BuildBackground => Load("UI/bg_build");

        /// <summary>Closed chest with its stars, as on the board.</summary>
        public static void DrawChest(Rect cell, Chest chest)
        {
            var art = SpriteFactory.ChestArt(chest.Color);
            if (art != null) GUI.DrawTexture(cell, art.texture, ScaleMode.ScaleToFit);
            else
            {
                var old = GUI.color;
                GUI.color = Palette.Block(chest.Color);
                GUI.DrawTexture(cell, UiArt.Round(Color.white, UiArt.Ink, 12, 3));
                GUI.color = old;
            }
            DrawStars(cell, chest.Stars, -0.22f);
        }

        /// <summary>Same star layout as CellView.ShowStars, in GUI space (cell = the 0.94-unit picture).</summary>
        public static void DrawStars(Rect cell, int n, float y)
        {
            const float size = 0.36f, step = 0.29f;
            float unit = cell.width / 0.94f, px = size * unit;
            int top = n <= 3 ? n : 3;
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < n; i++)
                {
                    int row = i < top ? 0 : 1;
                    int inRow = row == 0 ? top : n - top;
                    int idx = row == 0 ? i : i - top;
                    float x = (idx - (inRow - 1) / 2f) * step;
                    float yy = y + (n > 3 ? (row == 0 ? step / 2 : -step / 2) : 0);
                    var c = new Vector2(cell.center.x + x * unit, cell.center.y - yy * unit);
                    if (pass == 0)
                    {
                        float sp = px * SpriteFactory.StarShadowScale;
                        GUI.DrawTexture(new Rect(c.x + 0.04f * px - sp / 2, c.y + 0.07f * px - sp / 2, sp, sp), SpriteFactory.StarShadow.texture);
                    }
                    else GUI.DrawTexture(new Rect(c.x - px / 2, c.y - px / 2, px, px), SpriteFactory.Star.texture);
                }
        }

        /// <summary>Part icon, or a rounded square in the tier colour when there is no icon yet.</summary>
        public static void DrawPart(Rect r, string id, int tier)
        {
            var icon = Part(id);
            if (icon != null) { GUI.DrawTexture(r, icon, ScaleMode.ScaleToFit); return; }
            var old = GUI.color;
            GUI.color = Palette.Tier(tier);
            float m = r.width * 0.12f;
            GUI.DrawTexture(new Rect(r.x + m, r.y + m, r.width - 2 * m, r.height - 2 * m), UiArt.Round(Color.white, UiArt.Ink, 14, 3));
            GUI.color = old;
        }

        /// <summary>
        /// Covers the rect with a texture so that the texture row at <paramref name="rowFrac"/> (0 = top)
        /// lands at screen y <paramref name="atY"/>, zooming in as much as needed. Used to put the
        /// meadow's horizon under the castle.
        /// </summary>
        public static void DrawCoverAnchored(Rect r, Texture2D tex, float rowFrac, float atY)
        {
            float tw = tex.width, th = tex.height;
            float s = Mathf.Max(r.width / tw, r.height / th);
            // Above the anchor the texture needs rowFrac*th*s ≥ atY - r.y, below it (1-rowFrac)*th*s ≥ r.yMax - atY.
            if (rowFrac > 0) s = Mathf.Max(s, (atY - r.y) / (rowFrac * th));
            if (rowFrac < 1) s = Mathf.Max(s, (r.yMax - atY) / ((1 - rowFrac) * th));
            float dh = th * s, dw = tw * s;
            var dst = new Rect(r.center.x - dw / 2, atY - rowFrac * dh, dw, dh);
            GUI.BeginClip(r);
            GUI.DrawTexture(new Rect(dst.x - r.x, dst.y - r.y, dw, dh), tex);
            GUI.EndClip();
        }

        /// <summary>Draws a texture so it covers the rect, cropping the overflow (backgrounds).</summary>
        public static void DrawCover(Rect r, Texture2D tex, float alignY = 0.5f)
        {
            float ta = (float)tex.width / tex.height, ra = r.width / r.height;
            var uv = new Rect(0, 0, 1, 1);
            if (ra > ta) { uv.height = ta / ra; uv.y = (1 - uv.height) * (1 - alignY); }
            else { uv.width = ra / ta; uv.x = (1 - uv.width) / 2; }
            GUI.DrawTextureWithTexCoords(r, tex, uv);
        }
    }
}
