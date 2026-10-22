using Game.Core;
using UnityEngine;

namespace Game.View
{
    /// <summary>
    /// Castle picture made of layers, one per slot, all on the same canvas.
    /// Files live in Art/Resources/Castles/&lt;castle id&gt;/: c&lt;N&gt;_base.png (faded blueprint),
    /// c&lt;N&gt;_&lt;slot 00&gt;_&lt;part id&gt;.png per slot and &lt;castle id&gt;_full.png.
    /// </summary>
    public sealed class CastleArt
    {
        public Texture2D Base { get; private set; }
        public Texture2D Full { get; private set; }
        public Texture2D[] Layers { get; private set; }

        /// <summary>Loads the art of a castle. Null when the castle has no art yet or a layer is missing.</summary>
        public static CastleArt Load(CastleInfo castle)
        {
            string dir = $"Castles/{castle.Id}/";
            string prefix = "c" + castle.Id.Substring(castle.Id.LastIndexOf('_') + 1);
            var art = new CastleArt
            {
                Base = Resources.Load<Texture2D>(dir + prefix + "_base"),
                Full = Resources.Load<Texture2D>(dir + castle.Id + "_full"),
                Layers = new Texture2D[castle.Slots.Length],
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
            }
            return art;
        }

        /// <summary>Draws the blueprint and the filled slots. When every slot is filled, only the full picture.</summary>
        public void Draw(Rect r, bool[] filled)
        {
            bool all = true;
            foreach (bool f in filled) all &= f;
            if (all && Full != null) { GUI.DrawTexture(r, Full, ScaleMode.ScaleToFit); return; }
            GUI.DrawTexture(r, Base, ScaleMode.ScaleToFit);
            for (int i = 0; i < Layers.Length; i++)
                if (filled[i]) GUI.DrawTexture(r, Layers[i], ScaleMode.ScaleToFit);
        }
    }
}
