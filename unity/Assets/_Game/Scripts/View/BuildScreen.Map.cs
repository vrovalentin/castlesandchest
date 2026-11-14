using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.View
{
    /// <summary>
    /// Art of the kingdom map: the picture (Art/Resources/Map/map_kingdom.jpg), a small picture of every finished
    /// building (thumb_&lt;castle id&gt;.png) and its grey silhouette (sil_&lt;castle id&gt;.png), all made by
    /// tools/art/make_map_thumbs.py.
    /// </summary>
    public static class MapArt
    {
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        private static Texture2D Load(string path)
        {
            if (!Cache.TryGetValue(path, out var t)) Cache[path] = t = Resources.Load<Texture2D>(path);
            return t;
        }

        public static Texture2D Map => Load("Map/map_kingdom");

        public static Texture2D Thumb(string castleId) => Load("Map/thumb_" + castleId);

        public static Texture2D Silhouette(string castleId) => Load("Map/sil_" + castleId);

        /// <summary>
        /// Where the medallion of each building sits on the map: its centre, as parts of the picture from its top
        /// left corner. The n-th building of the catalog sits on the n-th place. Measured on the 1024 px picture
        /// (art-source/karta in the project files): 1–9 beach, meadows and forest, 10–12 snow, 13–15 desert,
        /// 16–18 volcano. x stays within 130..850: the picture is wider than the screen and its sides are cut off.
        /// </summary>
        public static readonly Vector2[] Points =
        {
            P(560, 905), P(320, 865), P(170, 760), P(390, 700), P(600, 770), P(810, 820), P(840, 650), P(640, 600),
            P(430, 500), P(340, 355), P(160, 330), P(270, 215), P(570, 330), P(820, 300), P(700, 190), P(500, 150),
            P(300, 90), P(640, 75),
        };

        private static Vector2 P(float x, float y) => new Vector2(x / 1024f, y / 1024f);
    }

    /// <summary>
    /// The kingdom map: every building in a round medallion joined by a dotted path. Built ones in colour in a gold
    /// ring, the next one bigger and glowing under an arrow, the rest a grey silhouette.
    /// </summary>
    public sealed partial class BuildScreen
    {
        private bool _mapOpen;
        /// <summary>The place whose card is shown at the bottom.</summary>
        private int _mapPick;
        private float _mapOpenedAt;
        /// <summary>The place whose medallion pops in (the building that just became next), or -1.</summary>
        private int _revealIndex = -1;
        private bool _revealSounded;
        /// <summary>Left edge of the map on screen when it is wider than the screen (NaN: centre it on the next building).</summary>
        private float _mapPanX = float.NaN;
        private float _dragFromX, _dragPanX;
        private bool _mapDragging, _mapDragMoved;

        /// <summary>Radius of a medallion and of the next one, as a part of the map's width.</summary>
        private const float MedalR = 0.0435f, MedalNextR = 0.053f;

        private static readonly Color Sky = new Color32(0x8c, 0xcd, 0xf2, 0xff);
        private static readonly Color Sea = new Color32(0x26, 0xa0, 0xd6, 0xff);
        private static readonly Color MedalEdge = new Color32(0x6b, 0x47, 0x22, 0xff);
        private static readonly Color MedalGold = new Color32(0xd9, 0xa4, 0x3c, 0xff);
        private static readonly Color MedalFace = new Color32(0xff, 0xfa, 0xee, 0xff);
        private static readonly Color MedalLocked = new Color32(0xe1, 0xe1, 0xe8, 0xff);
        private static readonly Color MedalLockedRing = new Color32(0xc8, 0xc8, 0xd2, 0xff);
        private static readonly Color PathBuilt = new Color32(0xff, 0xf6, 0xdc, 0xff);
        private static readonly Color PathLocked = new Color32(0xe6, 0xe6, 0xee, 0xff);

        /// <summary>True while the kingdom map is shown.</summary>
        public bool MapOpen => _mapOpen;

        /// <summary>Opens the map on the building being built now. <paramref name="reveal"/>: its medallion pops in.</summary>
        public void OpenMap(bool reveal = false)
        {
            _mapOpen = true;
            _mapPick = State.CastleIndex;
            _mapOpenedAt = Time.time;
            _revealIndex = reveal ? State.CastleIndex : -1;
            _revealSounded = false;
            _mapPanX = float.NaN;
            _mapDragging = false;
        }

        public void CloseMap() => _mapOpen = false;

        private enum Spot { Built, Next, Locked }

        private Spot SpotOf(int i)
        {
            if (i < State.CastleIndex) return Spot.Built;
            if (i > State.CastleIndex) return Spot.Locked;
            return Game.IsComplete(State) ? Spot.Built : Spot.Next;
        }

        private GUIStyle _mapTitle, _mapName, _mapLine, _numBuilt, _numNext, _numLocked;

        private void DrawMap(float w, float h)
        {
            var map = MapArt.Map;
            var castles = Game.Catalog.Castles;
            int built = Enumerable.Range(0, castles.Count).Count(k => SpotOf(k) == Spot.Built);

            // Sky above the picture, sea below it; the picture is a little wider than the screen so it fills more.
            GUI.color = Sky;
            GUI.DrawTexture(new Rect(0, 0, w, h / 2), Texture2D.whiteTexture);
            GUI.color = Sea;
            GUI.DrawTexture(new Rect(0, h / 2, w, h / 2), Texture2D.whiteTexture);
            GUI.color = Color.white;
            // The map fills the height from the title down to the card (its sea runs a little under the card). On a
            // tall phone it is wider than the screen: it opens on the next building and slides sideways under a finger.
            const float headerH = 128, cardH = 300;
            float size = h - cardH - 20 - headerH + 40;
            // Narrower than the screen: centred, nothing to slide. Wider: its edges stay at the screen's edges.
            float minX = size <= w ? (w - size) / 2 : w - size, maxX = size <= w ? minX : 0;
            if (size <= w) _mapPanX = minX;
            else if (float.IsNaN(_mapPanX))
                _mapPanX = w / 2 - MapArt.Points[Mathf.Clamp(State.CastleIndex, 0, MapArt.Points.Length - 1)].x * size;
            _mapPanX = Mathf.Clamp(_mapPanX, minX, maxX);
            DragMap(new Rect(0, headerH, w, h - cardH - 20 - headerH), minX, maxX);
            var mapRect = new Rect(_mapPanX, headerH, size, size);
            if (map != null) GUI.DrawTexture(mapRect, map);
            Vector2 At(int j) => new Vector2(mapRect.x + MapArt.Points[j].x * size, mapRect.y + MapArt.Points[j].y * size);
            int n = Mathf.Min(castles.Count, MapArt.Points.Length);
            float now = Time.time - _mapOpenedAt;
            float r0 = size * MedalR, r1 = size * MedalNextR;
            float RadiusOf(int j) => SpotOf(j) == Spot.Next ? r1 : r0;

            // The dotted path from each medallion to the next: cream where it is already walked, grey ahead.
            if (Event.current.type == EventType.Repaint)
            {
                float step = size * 0.02f, dot = size * 0.005f;
                var walked = UiArt.Disc(PathBuilt, MedalEdge, 2);
                var ahead = UiArt.Disc(PathLocked, new Color(0.35f, 0.35f, 0.4f, 1), 2);
                for (int i = 0; i + 1 < n; i++)
                {
                    Vector2 a = At(i), b = At(i + 1);
                    float len = Vector2.Distance(a, b), from = RadiusOf(i) + dot, to = len - RadiusOf(i + 1) - dot;
                    if (to <= from) continue;
                    int dots = Mathf.Max(1, Mathf.RoundToInt((to - from) / step));
                    var tex = SpotOf(i + 1) == Spot.Locked ? ahead : walked;
                    for (int k = 0; k <= dots; k++)
                    {
                        var p = Vector2.Lerp(a, b, (from + (to - from) * k / dots) / len);
                        GUI.DrawTexture(new Rect(p.x - dot, p.y - dot, dot * 2, dot * 2), tex);
                    }
                }
            }

            // Medallions, upper ones first, so the lower ones lie over them.
            foreach (int i in Enumerable.Range(0, n).OrderBy(k => MapArt.Points[k].y))
                DrawMedal(i, At(i), RadiusOf(i), now);
            if (_revealIndex >= 0 && now > 0.5f && !_revealSounded) { _revealSounded = true; Sfx.Play("whoosh"); }

            // The arrow bobs over the next building.
            if (SpotOf(State.CastleIndex) == Spot.Next && Event.current.type == EventType.Repaint && now > 1.2f)
            {
                var c = At(Mathf.Min(State.CastleIndex, n - 1));
                float bob = Mathf.Abs(Mathf.Sin(Time.time * 4f)) * 12;
                GUI.DrawTexture(new Rect(c.x - 30, c.y - r1 - 62 - bob, 60, 60), UiArt.Pointer, ScaleMode.ScaleToFit);
            }

            // Taps: a built or the next building shows its card; a locked one says what to build first.
            for (int i = 0; i < n; i++)
            {
                var c = At(i);
                float rr = RadiusOf(i) * 1.15f;
                if (!GUI.Button(new Rect(c.x - rr, c.y - rr, rr * 2, rr * 2), GUIContent.none, _invisible)) continue;
                int pick = i;
                if (SpotOf(i) == Spot.Locked)
                    Later(() => Say($"Сначала построй: {castles[State.CastleIndex].Name}."));
                else Later(() => _mapPick = pick);
            }

            // Title on top.
            _mapTitle ??= new GUIStyle(_pill) { alignment = TextAnchor.MiddleCenter, fontSize = 28, padding = new RectOffset(10, 10, 4, 4) };
            GUI.Label(new Rect(24, 22, w - 48, 84), $"Королевство · построено {built} из {castles.Count}", _mapTitle);

            if (_message != null && Time.time < _messageUntil)
                GUI.Box(new Rect(60, headerH + 20, w - 120, 96), _message, _toast);

            DrawMapCard(new Rect(24, h - cardH - 20, w - 48, cardH), Mathf.Clamp(_mapPick, 0, castles.Count - 1));
        }

        /// <summary>
        /// Sliding the map sideways. A press that moves more than a little is a drag: it does not tap the medallion
        /// it started on. Handled before the medallions' buttons, so they never see a drag.
        /// </summary>
        private void DragMap(Rect area, float minX, float maxX)
        {
            var e = Event.current;
            if (minX >= maxX) { _mapDragging = false; return; }
            switch (e.type)
            {
                case EventType.MouseDown when area.Contains(e.mousePosition):
                    _mapDragging = true; _mapDragMoved = false;
                    _dragFromX = e.mousePosition.x; _dragPanX = _mapPanX;
                    break;
                case EventType.MouseDrag when _mapDragging:
                    float dx = e.mousePosition.x - _dragFromX;
                    if (Mathf.Abs(dx) > 12) _mapDragMoved = true;
                    if (_mapDragMoved) { _mapPanX = Mathf.Clamp(_dragPanX + dx, minX, maxX); e.Use(); }
                    break;
                case EventType.MouseUp when _mapDragging:
                    _mapDragging = false;
                    if (_mapDragMoved) { GUIUtility.hotControl = 0; e.Use(); }
                    break;
            }
        }

        /// <summary>One medallion: shadow, (glow,) round face with a ring, the building or its silhouette, the number.</summary>
        private void DrawMedal(int i, Vector2 c, float r, float now)
        {
            var spot = SpotOf(i);
            var id = Game.Catalog.Castles[i].Id;
            bool picked = i == _mapPick;
            // The building that just became next pops in after the map has opened.
            float appear = i == _revealIndex ? Mathf.Clamp01((now - 0.5f) / 0.5f) : 1;
            if (appear <= 0) return;
            float pop = appear < 1 ? Mathf.Sin(appear * Mathf.PI * 0.5f) * (1 + 0.15f * Mathf.Sin(appear * Mathf.PI)) : 1;
            r *= pop;
            if (Event.current.type != EventType.Repaint) return;

            GUI.DrawTexture(new Rect(c.x - r + 3, c.y - r + 6, r * 2, r * 2), UiArt.SoftDisc(new Color(0, 0, 0, 0.45f)));
            if (spot == Spot.Next)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3);
                float g = r * (1.45f + 0.08f * pulse);
                GUI.DrawTexture(new Rect(c.x - g, c.y - g, g * 2, g * 2), UiArt.SoftDisc(new Color(1f, 0.86f, 0.43f, 0.75f)));
            }
            bool locked = spot == Spot.Locked;
            var face = new Rect(c.x - r, c.y - r, r * 2, r * 2);
            GUI.DrawTexture(face, UiArt.Disc(locked ? MedalLocked : MedalFace, MedalEdge, 2));
            // The ring: gold for built and next, grey for locked; the picked medallion's ring is a little thicker.
            // (Fixed widths in the 64 px texture: the textures are cached by their look.)
            float ringPx = picked ? 6.5f : 5f, ring = r * ringPx / 32;
            GUI.DrawTexture(face, UiArt.Disc(new Color(0, 0, 0, 0), locked ? MedalLockedRing : MedalGold, ringPx));

            var pic = locked ? MapArt.Silhouette(id) : MapArt.Thumb(id);
            if (pic != null)
            {
                float box = (r - ring) * 1.55f;
                float ph = box, pw = ph * pic.width / pic.height;
                if (pw > box) { pw = box; ph = pw * pic.height / pic.width; }
                GUI.DrawTexture(new Rect(c.x - pw / 2, c.y - ph / 2 + r * 0.04f, pw, ph), pic);
            }

            // The number on a small plaque under the medallion.
            if (_numBuilt == null)
            {
                GUIStyle Num(Color fill) => new GUIStyle(_pill)
                {
                    font = UiArt.BoldFont != null ? UiArt.BoldFont : _pill.font,
                    alignment = TextAnchor.MiddleCenter, fontSize = 20, fontStyle = FontStyle.Normal, padding = new RectOffset(0, 0, 0, 0),
                    border = new RectOffset(12, 12, 12, 12), normal = { background = UiArt.Round(fill, Color.white, 12, 3), textColor = Color.white },
                };
                _numBuilt = Num(new Color32(0x6d, 0xa8, 0x4e, 0xff));
                _numNext = Num(new Color32(0xf0, 0x9a, 0x1c, 0xff));
                _numLocked = Num(new Color32(0x9a, 0x9a, 0xa6, 0xff));
            }
            float bw = 40 * pop, bh = 28 * pop;
            GUI.Label(new Rect(c.x - bw / 2, c.y + r - bh * 0.45f, bw, bh), (i + 1).ToString(),
                spot == Spot.Built ? _numBuilt : spot == Spot.Next ? _numNext : _numLocked);
        }

        /// <summary>The card at the bottom: the resident, what they say, and the button back to building.</summary>
        private void DrawMapCard(Rect card, int i)
        {
            var castle = Game.Catalog.Castles[i];
            var who = Residents.Of(castle.Id);
            bool next = SpotOf(i) == Spot.Next;
            GUI.Box(card, GUIContent.none, _nightPanel);
            var portrait = who.Portrait != null ? GameArt.Part(who.Portrait) : MapArt.Thumb(castle.Id);
            var pr = new Rect(card.x + 26, card.y + 26, 150, 150);
            if (portrait != null) GUI.DrawTexture(pr, portrait, ScaleMode.ScaleToFit);
            _mapName ??= new GUIStyle(_ribbonTitle) { fontSize = 32, alignment = TextAnchor.UpperLeft, wordWrap = true };
            _mapName.normal.textColor = new Color32(0xff, 0xd6, 0x6e, 0xff);
            _mapLine ??= new GUIStyle(_nightBody) { fontSize = 24, alignment = TextAnchor.UpperLeft };
            float tx = pr.xMax + 20, tw = card.xMax - 26 - tx;
            GUI.Label(new Rect(tx, card.y + 26, tw, 44), $"{i + 1}. {castle.Name}", _mapName);
            GUI.Label(new Rect(tx, card.y + 76, tw, 110), next ? who.Start : who.Done, _mapLine);
            string label = next ? "Строить" : "К стройке";
            if (GoldButton(new Rect(card.center.x - 160, card.yMax - 112, 320, 92), label, 34))
                Later(CloseMap, click: false);
        }
    }
}
