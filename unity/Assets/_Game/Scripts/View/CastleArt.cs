using Game.Core;
using UnityEngine;

namespace Game.View
{
    /// <summary>
    /// Castle picture made of layers, one per slot, all on the same canvas.
    /// Files live in Art/Resources/Castles/&lt;castle id&gt;/: c&lt;N&gt;_base.png (faded blueprint), c&lt;N&gt;_decor.png (bushes on the site, optional),
    /// c&lt;N&gt;_&lt;slot 00&gt;_&lt;part id&gt;.png per slot, c&lt;N&gt;_line_&lt;slot 00&gt;.png (outline of an empty slot)
    /// and &lt;castle id&gt;_full.png.
    /// </summary>
    public sealed class CastleArt
    {
        public Texture2D Base { get; private set; }
        public Texture2D Full { get; private set; }
        public Texture2D[] Layers { get; private set; }
        /// <summary>Bushes and such that stand on the site from the start (c&lt;N&gt;_decor.png). May be null.</summary>
        public Texture2D Decor { get; private set; }
        /// <summary>Outline of each slot, drawn while the slot is empty. Entries may be null.</summary>
        public Texture2D[] Lines { get; private set; }

        /// <summary>Loads the art of a castle. Null when the castle has no art yet or a layer is missing.</summary>
        public static CastleArt Load(CastleInfo castle)
        {
            string dir = $"Castles/{castle.Id}/";
            string prefix = "c" + castle.Id.Substring(castle.Id.LastIndexOf('_') + 1);
            var art = new CastleArt
            {
                Base = Resources.Load<Texture2D>(dir + prefix + "_base"),
                Decor = Resources.Load<Texture2D>(dir + prefix + "_decor"),
                Full = Resources.Load<Texture2D>(dir + castle.Id + "_full"),
                Layers = new Texture2D[castle.Slots.Length],
                Lines = new Texture2D[castle.Slots.Length],
            };
            if (art.Base == null) return null;
            for (int i = 0; i < castle.Slots.Length; i++)
            {
                art.Layers[i] = Resources.Load<Texture2D>($"{dir}{prefix}_{i:00}_{castle.Slots[i]}");
                if (art.Layers[i] == null)
                {
                    Debug.LogWarning($"{castle.Id}: no layer for slot {i} ({castle.Slots[i]}), using the slot grid.");
                    return null;
                }
                art.Lines[i] = Resources.Load<Texture2D>($"{dir}{prefix}_line_{i:00}");
            }
            return art;
        }

        /// <summary>Draws the blueprint, outlines of empty slots and the filled slots. When every slot is filled, only the full picture.</summary>
        public void Draw(Rect r, bool[] filled)
        {
            bool all = true;
            foreach (bool f in filled) all &= f;
            if (all && Full != null) { GUI.DrawTexture(r, Full, ScaleMode.ScaleToFit); return; }
            GUI.DrawTexture(r, Base, ScaleMode.ScaleToFit);
            for (int i = 0; i < Lines.Length; i++)
                if (!filled[i] && Lines[i] != null) GUI.DrawTexture(r, Lines[i], ScaleMode.ScaleToFit);
            for (int i = 0; i < Layers.Length; i++)
                if (filled[i]) GUI.DrawTexture(r, Layers[i], ScaleMode.ScaleToFit);
        }

        private static readonly Color Glow = new Color32(0xff, 0xc8, 0x3c, 0xff);

        /// <summary>
        /// Only what is built: filled layers in full colour, the next slot as a pulsing gold outline,
        /// and the part just placed dropping into place (<paramref name="dropT"/> 0 → 1).
        /// </summary>
        public void DrawBuilt(Rect r, bool[] filled, int next, float pulse, int justPlaced, float dropT)
        {
            bool all = next < 0;
            if (all && Full != null && dropT >= 1) { GUI.DrawTexture(r, Full, ScaleMode.ScaleToFit); return; }
            var old = GUI.color;
            for (int i = 0; i < Layers.Length; i++)
            {
                if (!filled[i]) continue;
                if (i == justPlaced && dropT < 1)
                {
                    float k = 1 - dropT;
                    GUI.color = new Color(1, 1, 1, dropT);
                    GUI.DrawTexture(new Rect(r.x, r.y - k * k * r.height * 0.08f, r.width, r.height), Layers[i], ScaleMode.ScaleToFit);
                    GUI.color = old;
                }
                else GUI.DrawTexture(r, Layers[i], ScaleMode.ScaleToFit);
            }
            if (Decor != null) GUI.DrawTexture(r, Decor, ScaleMode.ScaleToFit);
            if (next < 0 || next >= Layers.Length) return;
            var line = Lines[next];
            if (line == null)
            {
                GUI.color = new Color(1, 1, 1, 0.25f + 0.25f * pulse);
                GUI.DrawTexture(r, Layers[next], ScaleMode.ScaleToFit);
                GUI.color = old;
                return;
            }
            // A thick gold glow from the outline drawn a few times around, then the outline itself.
            float o = r.width * 0.004f;
            GUI.color = new Color(Glow.r, Glow.g, Glow.b, 0.5f * pulse);
            foreach (var (dx, dy) in Around)
                GUI.DrawTexture(new Rect(r.x + dx * o, r.y + dy * o, r.width, r.height), line, ScaleMode.ScaleToFit);
            GUI.color = new Color(1, 0.97f, 0.85f, 0.7f + 0.3f * pulse);
            GUI.DrawTexture(r, line, ScaleMode.ScaleToFit);
            GUI.color = old;
        }

        private static readonly (float, float)[] Around = { (-1, 0), (1, 0), (0, -1), (0, 1), (-0.7f, -0.7f), (0.7f, 0.7f), (-0.7f, 0.7f), (0.7f, -0.7f) };
    }
}
