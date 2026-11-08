using Game.Core;
using UnityEngine;

namespace Game.View
{
    /// <summary>
    /// Castle picture made of layers, one per slot, all on the same canvas.
    /// Files live in Art/Resources/Castles/&lt;castle id&gt;/: c&lt;N&gt;_base.png (faded blueprint), c&lt;N&gt;_decor.png (bushes on the site, optional),
    /// c&lt;N&gt;_&lt;slot 00&gt;_&lt;part id&gt;.png per slot, c&lt;N&gt;_line_&lt;slot 00&gt;.png (outline of an empty slot)
    /// and &lt;castle id&gt;_full.png.
    /// Layers, outlines and decor may be trimmed to the box around their picture (tools/art/trim_layers.py):
    /// c&lt;N&gt;_rects.txt then says where each one sits on the canvas. Without an outline file the outline of an
    /// empty slot is drawn from its layer (Shaders/SlotOutline).
    /// </summary>
    public sealed class CastleArt
    {
        public Texture2D Base { get; private set; }
        public Texture2D Full { get; private set; }
        public Texture2D[] Layers { get; private set; }
        /// <summary>Bushes and such that stand on the site from the start (c&lt;N&gt;_decor.png). May be null.</summary>
        public Texture2D Decor { get; private set; }
        /// <summary>Slot the bushes are drawn under (the porch stands in front of them); Layers.Length when none.</summary>
        private int _decorBefore;
        /// <summary>Outline of each slot, drawn while the slot is empty. Entries may be null.</summary>
        public Texture2D[] Lines { get; private set; }
        /// <summary>Which slots cover each point (c&lt;N&gt;_hit.bytes from tools/art/make_hitmap.py). May be null.</summary>
        private byte[] _hit;
        /// <summary>Where each layer, outline and the decor sit on the canvas, as fractions of its side (x, y from the top, w, h).</summary>
        private Rect[] _layerAt, _lineAt;
        private Rect _decorAt = Full01;
        private static readonly Rect Full01 = new Rect(0, 0, 1, 1);
        private static Material _outline;
        private static bool _outlineTried;
        private int _hitSize;

        /// <summary>Where the castle stands in its picture, as fractions of the side: left and right edges and the bottom.
        /// Read from the tap map; the defaults fit castle 1.</summary>
        public float FootLeft { get; private set; } = 0.28f;
        public float FootRight { get; private set; } = 0.74f;
        public float FootBottom { get; private set; } = 0.98f;

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
            var hit = Resources.Load<TextAsset>(dir + prefix + "_hit");
            if (hit != null)
            {
                int size = Mathf.RoundToInt(Mathf.Sqrt(hit.bytes.Length / 8f));
                if (size * size * 8 == hit.bytes.Length) { art._hit = hit.bytes; art._hitSize = size; art.MeasureFoot(); }
            }
            var at = ReadRects(Resources.Load<TextAsset>(dir + prefix + "_rects"));
            art._layerAt = new Rect[castle.Slots.Length];
            art._lineAt = new Rect[castle.Slots.Length];
            if (at.TryGetValue(prefix + "_decor", out var dr)) art._decorAt = dr;
            int porch = System.Array.IndexOf(castle.Slots, "porch");
            art._decorBefore = porch >= 0 ? porch : castle.Slots.Length;
            for (int i = 0; i < castle.Slots.Length; i++)
            {
                art.Layers[i] = Resources.Load<Texture2D>($"{dir}{prefix}_{i:00}_{castle.Slots[i]}");
                if (art.Layers[i] == null)
                {
                    Debug.LogWarning($"{castle.Id}: no layer for slot {i} ({castle.Slots[i]}), using the slot grid.");
                    return null;
                }
                art.Lines[i] = Resources.Load<Texture2D>($"{dir}{prefix}_line_{i:00}");
                art._layerAt[i] = at.TryGetValue($"{prefix}_{i:00}_{castle.Slots[i]}", out var lr) ? lr : Full01;
                art._lineAt[i] = at.TryGetValue($"{prefix}_line_{i:00}", out var ln) ? ln : Full01;
            }
            return art;
        }

        private static System.Collections.Generic.Dictionary<string, Rect> ReadRects(TextAsset t)
        {
            var d = new System.Collections.Generic.Dictionary<string, Rect>();
            if (t == null) return d;
            var lines = t.text.Split('\n');
            if (lines.Length == 0 || !float.TryParse(lines[0].Trim(), out float size) || size <= 0) return d;
            for (int k = 1; k < lines.Length; k++)
            {
                var v = lines[k].Trim().Split(' ');
                if (v.Length != 5) continue;
                d[v[0]] = new Rect(int.Parse(v[1]) / size, int.Parse(v[2]) / size, int.Parse(v[3]) / size, int.Parse(v[4]) / size);
            }
            return d;
        }

        /// <summary>Where slot i's layer sits on the canvas, as fractions of its side.</summary>
        public Rect LayerRect(int i) => _layerAt[i];

        /// <summary>True when slot i has an outline to draw while it is empty: its own file, or one drawn from its layer.</summary>
        public bool HasOutline(int i) => Lines[i] != null || OutlineMaterial != null;

        private static Material OutlineMaterial
        {
            get
            {
                if (_outlineTried) return _outline;
                _outlineTried = true;
                var sh = Resources.Load<Shader>("Shaders/SlotOutline");
                if (sh != null && sh.isSupported) _outline = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
                return _outline;
            }
        }

        /// <summary>The part of the square picture (ScaleToFit in r) that a piece at <paramref name="at"/> covers.</summary>
        private static Rect Place(Rect r, Rect at)
        {
            float side = Mathf.Min(r.width, r.height);
            float x = r.x + (r.width - side) / 2, y = r.y + (r.height - side) / 2;
            return new Rect(x + at.x * side, y + at.y * side, at.width * side, at.height * side);
        }

        private void DrawLayer(Rect r, int i) => GUI.DrawTexture(Place(r, _layerAt[i]), Layers[i], ScaleMode.StretchToFill);

        /// <summary>The outline of an empty slot, tinted by GUI.color.</summary>
        private void DrawLine(Rect r, int i)
        {
            if (Lines[i] != null) { GUI.DrawTexture(Place(r, _lineAt[i]), Lines[i], ScaleMode.StretchToFill); return; }
            var mat = OutlineMaterial;
            if (mat == null || Event.current == null || Event.current.type != EventType.Repaint) return;
            var c = GUI.color;
            Graphics.DrawTexture(Place(r, _layerAt[i]), Layers[i], Full01, 0, 0, 0, 0, new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, c.a * 0.5f), mat);
        }

        private void MeasureFoot()
        {
            int x0 = _hitSize, x1 = -1, y1 = -1;
            for (int y = 0; y < _hitSize; y++)
                for (int x = 0; x < _hitSize; x++)
                    if (System.BitConverter.ToUInt64(_hit, (y * _hitSize + x) * 8) != 0)
                    {
                        if (x < x0) x0 = x;
                        if (x > x1) x1 = x;
                        if (y > y1) y1 = y;
                    }
            if (x1 < 0) return;
            FootLeft = (float)x0 / _hitSize;
            FootRight = (x1 + 1f) / _hitSize;
            FootBottom = (y1 + 1f) / _hitSize;
        }

        /// <summary>Draws the blueprint, outlines of empty slots and the filled slots. When every slot is filled, only the full picture.</summary>
        public void Draw(Rect r, bool[] filled)
        {
            bool all = true;
            foreach (bool f in filled) all &= f;
            if (all && Full != null) { GUI.DrawTexture(r, Full, ScaleMode.ScaleToFit); return; }
            GUI.DrawTexture(r, Base, ScaleMode.ScaleToFit);
            for (int i = 0; i < Lines.Length; i++)
                if (!filled[i]) DrawLine(r, i);
            for (int i = 0; i < Layers.Length; i++)
                if (filled[i]) DrawLayer(r, i);
        }

        /// <summary>
        /// The slot under a point of the picture drawn in <paramref name="r"/>, or -1.
        /// The glowing next slot wins, then the top built part, then the lowest part still to build.
        /// </summary>
        public int SlotAt(Rect r, Vector2 point, bool[] filled, int next)
        {
            if (_hit == null || r.width <= 0 || r.height <= 0) return -1;
            // The picture is square and drawn ScaleToFit.
            float side = Mathf.Min(r.width, r.height);
            var pic = new Rect(r.center.x - side / 2, r.center.y - side / 2, side, side);
            if (!pic.Contains(point)) return -1;
            int x = Mathf.Clamp((int)((point.x - pic.x) / side * _hitSize), 0, _hitSize - 1);
            int y = Mathf.Clamp((int)((point.y - pic.y) / side * _hitSize), 0, _hitSize - 1);
            ulong mask = System.BitConverter.ToUInt64(_hit, (y * _hitSize + x) * 8);
            if (mask == 0) return -1;
            bool Has(int i) => i < 64 && (mask & 1UL << i) != 0;
            if (next >= 0 && Has(next)) return next;
            for (int i = filled.Length - 1; i >= 0; i--)
                if (filled[i] && Has(i)) return i;
            for (int i = 0; i < filled.Length; i++)
                if (Has(i)) return i;
            return -1;
        }

        private Vector2[] _centers;
        private float[] _bottoms;

        /// <summary>Where a slot's part sits in the picture drawn in <paramref name="r"/>: the middle of its taps, and its lowest point.</summary>
        public bool SlotSpot(Rect r, int slot, out Vector2 center, out float bottom)
        {
            center = r.center; bottom = r.center.y;
            if (_hit == null || slot < 0 || slot >= 64) return false;
            if (_centers == null)
            {
                int n = Layers.Length;
                var sx = new double[n]; var sy = new double[n]; var cnt = new int[n]; var by = new int[n];
                for (int y = 0; y < _hitSize; y++)
                    for (int x = 0; x < _hitSize; x++)
                    {
                        ulong m = System.BitConverter.ToUInt64(_hit, (y * _hitSize + x) * 8);
                        for (int i = 0; m != 0 && i < n && i < 64; i++)
                            if ((m & 1UL << i) != 0) { sx[i] += x; sy[i] += y; cnt[i]++; if (y > by[i]) by[i] = y; }
                    }
                _centers = new Vector2[n]; _bottoms = new float[n];
                for (int i = 0; i < n; i++)
                {
                    _centers[i] = cnt[i] > 0 ? new Vector2((float)(sx[i] / cnt[i] + 0.5) / _hitSize, (float)(sy[i] / cnt[i] + 0.5) / _hitSize) : new Vector2(0.5f, 0.5f);
                    _bottoms[i] = cnt[i] > 0 ? (by[i] + 1f) / _hitSize : 0.5f;
                }
            }
            if (slot >= _centers.Length) return false;
            float side = Mathf.Min(r.width, r.height);
            var pic = new Rect(r.center.x - side / 2, r.center.y - side / 2, side, side);
            center = new Vector2(pic.x + _centers[slot].x * side, pic.y + _centers[slot].y * side);
            bottom = pic.y + _bottoms[slot] * side;
            return true;
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
            // Flower pots and bushes appear only once the castle in front of them is built.
            int last = Mathf.Min(_decorBefore, Layers.Length) - 1;
            bool decor = Decor != null && (last < 0 || filled[last]);
            for (int i = 0; i < Layers.Length; i++)
            {
                if (i == _decorBefore && decor) GUI.DrawTexture(Place(r, _decorAt), Decor, ScaleMode.StretchToFill);
                if (!filled[i]) continue;
                if (i == justPlaced && dropT < 1)
                {
                    // Falls in from above, lands, bounces once a little.
                    float fall = Mathf.Clamp01(dropT / 0.55f), k = 1 - fall;
                    float bounce = dropT > 0.55f ? Mathf.Sin((dropT - 0.55f) / 0.45f * Mathf.PI) * 0.012f : 0;
                    GUI.color = new Color(1, 1, 1, Mathf.Clamp01(dropT * 10));
                    DrawLayer(new Rect(r.x, r.y - (k * k * 0.12f + bounce) * r.height, r.width, r.height), i);
                    GUI.color = old;
                }
                else DrawLayer(r, i);
            }
            if (_decorBefore >= Layers.Length && decor) GUI.DrawTexture(Place(r, _decorAt), Decor, ScaleMode.StretchToFill);
            if (next < 0 || next >= Layers.Length) return;
            if (!HasOutline(next))
            {
                GUI.color = new Color(1, 1, 1, 0.25f + 0.25f * pulse);
                DrawLayer(r, next);
                GUI.color = old;
                return;
            }
            // A thick gold glow from the outline drawn a few times around, then the outline itself.
            float o = r.width * 0.004f;
            GUI.color = new Color(Glow.r, Glow.g, Glow.b, 0.5f * pulse);
            foreach (var (dx, dy) in Around)
                DrawLine(new Rect(r.x + dx * o, r.y + dy * o, r.width, r.height), next);
            GUI.color = new Color(1, 0.97f, 0.85f, 0.7f + 0.3f * pulse);
            DrawLine(r, next);
            GUI.color = old;
        }

        private static readonly (float, float)[] Around = { (-1, 0), (1, 0), (0, -1), (0, 1), (-0.7f, -0.7f), (0.7f, 0.7f), (-0.7f, 0.7f), (0.7f, -0.7f) };
    }
}
