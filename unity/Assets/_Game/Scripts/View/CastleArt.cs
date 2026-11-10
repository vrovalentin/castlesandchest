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
            DrawScaffold(r, filled, next, justPlaced >= 0 && dropT < 1 ? dropT : 1);
            DrawDebris(r);
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

        private ulong _scaffoldFor = ulong.MaxValue;
        private bool _scaffoldOn;
        private float _scaffoldY, _scaffoldL, _scaffoldR;

        /// <summary>
        /// Finds where the scaffolding goes: the height where most built pieces end with an empty slot right above
        /// them (the straight cut edges of unfinished walls), and how wide that edge runs. Worked out from the tap map
        /// once per set of built slots.
        /// </summary>
        private void MeasureScaffold(bool[] filled)
        {
            ulong fm = 0;
            for (int i = 0; i < filled.Length && i < 64; i++)
                if (filled[i]) fm |= 1UL << i;
            if (fm == _scaffoldFor) return;
            _scaffoldFor = fm;
            _scaffoldOn = false;
            if (_hit == null || fm == 0) return;
            const float bin = 0.03f;
            int n = _hitSize;
            var perBin = new int[(int)(1 / bin) + 2];
            var xs = new System.Collections.Generic.List<int>();
            var ys = new System.Collections.Generic.List<int>();
            for (int y = 1; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    ulong m = System.BitConverter.ToUInt64(_hit, (y * n + x) * 8);
                    if ((m & fm) == 0) continue;
                    ulong up = System.BitConverter.ToUInt64(_hit, ((y - 1) * n + x) * 8);
                    if (up == 0 || (up & fm) != 0) continue;
                    xs.Add(x); ys.Add(y);
                    perBin[(int)((float)y / n / bin)]++;
                }
            int best = 0;
            for (int i = 1; i < perBin.Length; i++)
                if (perBin[i] > perBin[best]) best = i;
            float mid = (best + 0.5f) * bin;
            int x0 = n, x1 = -1, count = 0;
            var near = new System.Collections.Generic.List<int>();
            for (int k = 0; k < ys.Count; k++)
            {
                if (Mathf.Abs((float)ys[k] / n - mid) >= 0.04f) continue;
                near.Add(ys[k]); count++;
                if (xs[k] < x0) x0 = xs[k];
                if (xs[k] > x1) x1 = xs[k];
            }
            if (count < n * 0.04f) return;      // only a few loose points: nothing that looks cut
            near.Sort();
            _scaffoldY = (float)near[near.Count / 2] / n;
            _scaffoldL = (float)x0 / n;
            _scaffoldR = (x1 + 1f) / n;
            _scaffoldOn = true;
        }

        // Where the posts and the deck are in the scaffolding pictures (tools/art/cut_scaffold.py), as fractions of
        // their width and height: the left post's middle, the distance between the posts, the top of the deck.
        private const float SecPost = 39 / 512f, SecSpan = 434 / 512f, SecDeck = 57 / 484f;
        private const float TopPost = 40 / 512f, TopSpan = 431 / 512f, TopDeck = 163 / 462f;

        /// <summary>
        /// Wooden scaffolding at an unfinished building, from the kit in UI/scaff_*: a tower of storeys on each side
        /// of the cut edge of the unfinished walls, the top deck with a rail lying across the edge between them, and a
        /// ladder at the side. Fractions are of the picture's side.
        /// </summary>
        private void DrawScaffold(Rect r, bool[] filled, int next, float dropT)
        {
            var sec = GameArt.ScaffoldSection;
            var top = GameArt.ScaffoldTop;
            if (sec == null || top == null) return;
            MeasureScaffold(filled);
            if (!_scaffoldOn) return;
            float y = _scaffoldY, ground = FootBottom;
            if (ground - y < 0.25f) return;    // only the foundation so far: just the piles at the foot
            // Two towers of storeys stand just outside the cut, on the left and on the right; between them only the
            // top deck with its rail lies across the cut edge, so the finished walls below stay in sight.
            const float inset = 0.015f;
            float span = _scaffoldR - _scaffoldL - 2 * inset;
            int mid = span < 0.1f ? 0 : Mathf.Max(1, Mathf.RoundToInt(span / 0.14f));
            float w = mid > 0 ? span / mid : 0.13f;                       // one bay, the same in the towers and between
            float lx = _scaffoldL + inset - w, rx = _scaffoldR - inset;   // left post of each tower
            float tk = w / (TopSpan * top.width), sk = w / (SecSpan * sec.width);   // picture fractions per texture pixel
            float tw = top.width * tk, th = top.height * tk, sw = sec.width * sk, sh = sec.height * sk;
            float ty = y - TopDeck * th;
            for (int t = 0; t < 2; t++)
            {
                float x0 = t == 0 ? lx : rx;
                // Storeys down to the ground, the lowest cut off at it; the brace turns every other storey.
                int j = 0;
                for (float sy = ty + th - 0.002f; sy < ground; sy += sh - 0.002f, j++)
                {
                    float vis = Mathf.Min(1, (ground - sy + 0.004f) / sh);
                    bool flip = (t + j) % 2 == 1;
                    var uv = new Rect(flip ? 1 : 0, 1 - vis, flip ? -1 : 1, vis);
                    GUI.DrawTextureWithTexCoords(Place(r, new Rect(x0 - (flip ? 1 - SecPost - SecSpan : SecPost) * sw, sy, sw, sh * vis)), sec, uv);
                }
                GUI.DrawTexture(Place(r, new Rect(x0 - TopPost * tw, ty, tw, th)), top, ScaleMode.StretchToFill);
            }
            // Between the towers: the deck and rail only, without the legs.
            const float keep = 0.6f;    // the top piece down to just under its deck
            for (int c = 0; c < mid; c++)
            {
                float x0 = _scaffoldL + inset + c * w;
                GUI.DrawTextureWithTexCoords(Place(r, new Rect(x0 - TopPost * tw, ty, tw, th * keep)), top, new Rect(0, 1 - keep, 1, keep));
            }
            // A ladder by the right tower, from the ground up to the top deck, made of whole ladders one on another.
            var ladder = GameArt.ScaffoldLadder;
            if (ladder != null)
            {
                float lw = 0.035f, lh = lw * ladder.height / ladder.width, from = y - 0.03f;
                int n = Mathf.Max(1, Mathf.CeilToInt((ground - from) / lh));
                lh = (ground - from) / n;
                for (int i = 0; i < n; i++)
                    GUI.DrawTexture(Place(r, new Rect(rx + w + 0.004f, from + i * lh, lw, lh)), ladder, ScaleMode.StretchToFill);
            }
            DrawCrane(r, rx, ty, next, dropT);
        }

        // Where the jib is fixed to the post and where its pulley hangs, as fractions of the jib picture
        // (tools/art/cut_scaffold.py); and where the ring of the crate is, as a fraction of the crate picture's width.
        private const float JibMountX = 0.93f, JibMountY = 0.09f, PulleyX = 0.08f, PulleyY = 0.53f, CrateRingX = 0.5f;

        /// <summary>
        /// A wooden crane on top of the right scaffolding tower: its jib reaches over the next slot, and a crate of
        /// stones hangs from it on a rope, swaying a little. When a part is placed (<paramref name="dropT"/> 0 → 1)
        /// the crate goes down and comes back up empty.
        /// </summary>
        private void DrawCrane(Rect r, float postX, float postTop, int next, float dropT)
        {
            var jib = GameArt.ScaffoldJib;
            // Full of stones; when a part is placed it goes down full and comes back up empty.
            var crate = dropT < 1 && dropT >= 0.5f && GameArt.ScaffoldCrateEmpty != null ? GameArt.ScaffoldCrateEmpty : GameArt.ScaffoldCrate;
            var rope = GameArt.ScaffoldRope;
            if (jib == null || crate == null || rope == null || next < 0) return;
            if (!SlotSpot(new Rect(0, 0, 1, 1), next, out var target, out _)) return;
            float reach = Mathf.Clamp(postX - target.x, 0.08f, 0.35f);
            float jw = reach / (JibMountX - PulleyX), jh = Mathf.Min(0.14f, jw * jib.height / jib.width);
            float cw = 0.085f, ch = cw * crate.height / crate.width;
            // The crate hangs right at the next slot. When the slot is higher than the scaffolding, the jib goes up
            // on a mast above the tower's post, so the crate never hangs in front of what is already built.
            float crateTop = target.y - ch / 2;
            float mount = Mathf.Min(postTop, crateTop - 0.03f - (PulleyY - JibMountY) * jh);
            if (mount < postTop)
            {
                var sec = GameArt.ScaffoldSection;
                float mw = 0.016f;
                if (sec != null)
                    GUI.DrawTextureWithTexCoords(Place(r, new Rect(postX - mw / 2, mount, mw, postTop - mount + 0.01f)), sec, new Rect(0.02f, 0, 0.12f, 1));
            }
            float jx = postX - JibMountX * jw, jy = mount - JibMountY * jh;
            GUI.DrawTexture(Place(r, new Rect(jx, jy, jw, jh)), jib, ScaleMode.StretchToFill);

            var pulley = new Vector2(jx + PulleyX * jw, jy + PulleyY * jh);
            float dip = dropT < 1 ? Mathf.Sin(dropT * Mathf.PI) * 0.06f : 0;
            float ropeLen = Mathf.Clamp(crateTop - pulley.y, 0.02f, 0.4f) + dip;
            var m = GUI.matrix;
            var pivot = Place(r, new Rect(pulley.x, pulley.y, 0, 0)).position;
            GUIUtility.RotateAroundPivot(Mathf.Sin(Time.time * 1.3f) * 3f, pivot);
            float rw = 0.008f;
            GUI.DrawTexture(Place(r, new Rect(pulley.x - rw / 2, pulley.y, rw, ropeLen)), rope, ScaleMode.StretchToFill);
            GUI.DrawTexture(Place(r, new Rect(pulley.x - CrateRingX * cw, pulley.y + ropeLen - 0.004f, cw, ch)), crate, ScaleMode.StretchToFill);
            GUI.matrix = m;
        }

        /// <summary>Piles of blocks, planks and sand at the foot of an unfinished building.</summary>
        private void DrawDebris(Rect r)
        {
            float l = FootLeft, rr = FootRight, g = FootBottom;
            DrawPile(r, GameArt.ScaffoldDebris(0), l + 0.02f, g, 0.17f);
            DrawPile(r, GameArt.ScaffoldDebris(1), rr - 0.03f, g, 0.16f);
            DrawPile(r, GameArt.ScaffoldDebris(2), (l + rr) / 2 + 0.05f, g, 0.12f);
        }

        private static void DrawPile(Rect r, Texture2D t, float cx, float ground, float w)
        {
            if (t == null) return;
            float h = w * t.height / t.width;
            GUI.DrawTexture(Place(r, new Rect(cx - w / 2, ground - h + 0.02f, w, h)), t, ScaleMode.StretchToFill);
        }

        private static readonly (float, float)[] Around = { (-1, 0), (1, 0), (0, -1), (0, 1), (-0.7f, -0.7f), (0.7f, 0.7f), (-0.7f, 0.7f), (0.7f, -0.7f) };
    }
}
