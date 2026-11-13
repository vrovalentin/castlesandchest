using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.View
{
    /// <summary>
    /// Art of the kingdom map: the picture (Art/Resources/Map/map_kingdom.png) and a small picture of every
    /// finished building (thumb_&lt;castle id&gt;.png), both made by tools/art/make_map_thumbs.py.
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

        /// <summary>The flat side-view ground piece a building stands on, in the look of its land; meadow when that one is missing.</summary>
        public static Texture2D Plinth(int place)
        {
            string land = Lands[Mathf.Clamp(place, 0, Lands.Length - 1)];
            return Load("Map/plinth_" + land) ?? Load("Map/plinth_meadow");
        }

        /// <summary>The land of each place, for its plinth (tools/art/cut_plinths.py).</summary>
        private static readonly string[] Lands =
        {
            "beach", "beach", "meadow", "meadow", "meadow", "forest", "forest", "meadow", "meadow",
            "snow", "snow", "desert", "desert", "desert", "volcano", "volcano", "volcano",
        };

        /// <summary>
        /// Where each building stands on the map, beside its road: the bottom middle of the building, as parts of
        /// the picture from its top left corner. The n-th building of the catalog stands on the n-th place.
        /// Measured on the 1024 px picture (art-source/karta in the project files): 1–2 beach, 3–8 meadows and
        /// forest, 9 upper meadow, 10–11 snow, 12–14 desert, 15–17 volcano.
        /// </summary>
        public static readonly Vector2[] Points =
        {
            P(480, 885), P(350, 862), P(690, 800), P(560, 805), P(720, 690), P(870, 700), P(780, 590), P(560, 590),
            P(440, 470), P(300, 300), P(390, 330), P(620, 330), P(730, 400), P(600, 200), P(380, 170), P(650, 150),
            P(330, 90),
        };

        private static Vector2 P(float x, float y) => new Vector2(x / 1024f, y / 1024f);
    }

    /// <summary>The kingdom map: built buildings on their plinths beside the road, the next one pale, the places of the rest still empty land.</summary>
    public sealed partial class BuildScreen
    {
        private bool _mapOpen;
        /// <summary>The place whose card is shown at the bottom.</summary>
        private int _mapPick;
        private float _mapOpenedAt;
        /// <summary>The place whose pale building fades in (the building that just became next), or -1.</summary>
        private int _revealIndex = -1;
        private bool _revealSounded;

        private static readonly Color Sky = new Color32(0x8c, 0xcd, 0xf2, 0xff);
        private static readonly Color Sea = new Color32(0x26, 0xa0, 0xd6, 0xff);

        /// <summary>True while the kingdom map is shown.</summary>
        public bool MapOpen => _mapOpen;

        /// <summary>Opens the map on the building being built now. <paramref name="reveal"/>: its pale picture fades in.</summary>
        public void OpenMap(bool reveal = false)
        {
            _mapOpen = true;
            _mapPick = State.CastleIndex;
            _mapOpenedAt = Time.time;
            _revealIndex = reveal ? State.CastleIndex : -1;
            _revealSounded = false;
        }

        public void CloseMap() => _mapOpen = false;

        private enum Spot { Built, Next, Locked }

        private Spot SpotOf(int i)
        {
            if (i < State.CastleIndex) return Spot.Built;
            if (i > State.CastleIndex) return Spot.Locked;
            return Game.IsComplete(State) ? Spot.Built : Spot.Next;
        }

        private GUIStyle _mapTitle, _mapName, _mapLine;

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
            const float headerH = 128, cardH = 300;
            float size = Mathf.Min(w * 1.15f, h - headerH - cardH * 0.6f);
            var mapRect = new Rect((w - size) / 2, headerH, size, size);
            if (map != null) GUI.DrawTexture(mapRect, map);
            Vector2 At(int j) => new Vector2(mapRect.x + MapArt.Points[j].x * size, mapRect.y + MapArt.Points[j].y * size);
            int n = Mathf.Min(castles.Count, MapArt.Points.Length);

            // Upper places first, so the lower buildings stand in front of them.
            float thumbH = size * 0.095f;
            float now = Time.time - _mapOpenedAt;
            foreach (int i in Enumerable.Range(0, n).OrderBy(k => MapArt.Points[k].y))
            {
                var c = At(i);
                var spot = SpotOf(i);
                var thumb = MapArt.Thumb(castles[i].Id);
                if (spot != Spot.Locked && thumb != null)
                {
                    float tw = thumbH * thumb.width / thumb.height;
                    var r = new Rect(c.x - tw / 2, c.y - thumbH, tw, thumbH);
                    // Every building stands on a flat ground piece of its land, all of one width.
                    var plinth = MapArt.Plinth(i);
                    if (plinth != null)
                    {
                        float pw = Mathf.Max(tw * 1.12f, size * 0.13f), ph = pw * plinth.height / plinth.width;
                        var pr = new Rect(c.x - pw / 2, c.y - ph * 0.35f, pw, ph);
                        GUI.DrawTexture(new Rect(pr.x, pr.y + ph * 0.45f, pw, ph * 0.4f), UiArt.SoftDisc(new Color(0.08f, 0.06f, 0.04f, 0.35f)));
                        GUI.color = new Color(1, 1, 1, spot == Spot.Next ? 0.8f : 1);
                        GUI.DrawTexture(pr, plinth);
                        GUI.color = Color.white;
                    }
                    if (spot == Spot.Next)
                    {
                        // The next building: a pale outline of it, breathing a little; just after «Далее» it fades in.
                        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3);
                        float appear = i == _revealIndex ? Mathf.Clamp01((now - 0.5f) / 0.8f) : 1;
                        GUI.color = new Color(1, 1, 1, (0.35f + 0.2f * pulse) * appear);
                        GUI.DrawTexture(r, thumb);
                        GUI.color = Color.white;
                    }
                    else GUI.DrawTexture(r, thumb);
                }
            }
            if (_revealIndex >= 0 && now > 0.5f && !_revealSounded) { _revealSounded = true; Sfx.Play("whoosh"); }

            // The arrow bobs over the next building.
            if (SpotOf(State.CastleIndex) == Spot.Next && Event.current.type == EventType.Repaint && now > 1.2f)
            {
                var c = At(Mathf.Min(State.CastleIndex, n - 1));
                float bob = Mathf.Abs(Mathf.Sin(Time.time * 4f)) * 14;
                GUI.DrawTexture(new Rect(c.x - 36, c.y - thumbH - 70 - bob, 72, 72), UiArt.Pointer, ScaleMode.ScaleToFit);
            }

            // Taps: a built or the next building shows its card; a locked one says what to build first.
            for (int i = 0; i < n; i++)
            {
                var c = At(i);
                var hit = new Rect(c.x - size * 0.06f, c.y - thumbH, size * 0.12f, thumbH + size * 0.04f);
                if (!GUI.Button(hit, GUIContent.none, _invisible)) continue;
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
