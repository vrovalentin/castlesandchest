using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using UnityEngine;

namespace Game.View
{
    /// <summary>
    /// Build screen drawing, all on one screen: the meadow with the castle, and under it the next part card,
    /// the «Сундуки» and «Склад» tiles and the mine button. Storage and merging live in their own window.
    /// IMGUI in a 720-wide virtual screen with UiArt panels until the UI art is ready.
    /// </summary>
    public sealed partial class BuildScreen
    {
        private static readonly Color Ink = new Color32(0x2e, 0x2a, 0x3d, 0xff);
        // Panels are white with soft lavender edges, in tune with the purple pop-up windows.
        private static readonly Color Brown = new Color32(0x6b, 0x5a, 0xa6, 0xff);
        private static readonly Color CardFill = new Color32(0xff, 0xff, 0xff, 0xf4);
        private static readonly Color CardEdge = new Color32(0xd8, 0xd2, 0xec, 0xff);
        private static readonly Color Cream = new Color32(0xff, 0xff, 0xff, 0xff);
        private static readonly Color Frame = new Color32(0x8f, 0x82, 0xc4, 0xff);
        private static readonly Color Gold = new Color32(0xd9, 0xa4, 0x41, 0xff);
        private static readonly Color Green = new Color32(0x4f, 0xb3, 0x5a, 0xff);
        private static readonly Color GreenEdge = new Color32(0x2f, 0x7d, 0x3a, 0xff);
        private static readonly Color WoodFill = new Color32(0xa8, 0x70, 0x3f, 0xff);
        private static readonly Color WoodEdge = new Color32(0x6b, 0x44, 0x23, 0xff);
        private static readonly Color Orange = new Color32(0xf5, 0xa6, 0x23, 0xff);
        private static readonly Color OrangeEdge = new Color32(0xc0, 0x76, 0x08, 0xff);
        private static readonly Color HaveFill = new Color32(0xdc, 0xf2, 0xd6, 0xff);
        private static readonly Color Red = new Color32(0xd9, 0x53, 0x4f, 0xff);

        private GUIStyle _pill, _card, _section, _text, _nextCard, _bigName, _sub, _sheet, _tile, _tilePicked, _count, _need,
            _wood, _green, _mineBtn, _toast, _panel, _panelTitle, _panelBody, _name, _invisible, _roundBtn,
            _nightPanel, _gold, _ribbonTitle, _onNight, _badge, _nightBody, _tileTitle;

        private Vector2 _scroll;
        private float _contentH = 1500;

        private void Styles()
        {
            if (_card != null) return;
            _pill = UiArt.Style(UiArt.Round(Cream, CardEdge, 26, 3), 26, 26, Ink, TextAnchor.MiddleLeft);
            _pill.fontStyle = FontStyle.Bold;
            _pill.padding = new RectOffset(20, 16, 4, 4);
            _card = UiArt.Style(UiArt.Round(CardFill, CardEdge, 18, 3), 18, 1, Ink);
            _section = new GUIStyle { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            _section.normal.textColor = Brown;
            _text = new GUIStyle { fontSize = 20, wordWrap = true, alignment = TextAnchor.MiddleLeft };
            _text.normal.textColor = Ink;
            _nextCard = UiArt.Style(UiArt.Round(Color.white, Gold, 22, 4), 22, 1, Ink);
            _bigName = new GUIStyle { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, wordWrap = false };
            _bigName.normal.textColor = Ink;
            _sub = new GUIStyle { fontSize = 20, wordWrap = true, alignment = TextAnchor.UpperLeft };
            _sub.normal.textColor = new Color32(0x6a, 0x66, 0x80, 0xff);
            _tileTitle = new GUIStyle(_bigName) { fontSize = 32 };
            _sheet = UiArt.Style(UiArt.Round(new Color32(0xf8, 0xf7, 0xfc, 0xff), new Color32(0x8f, 0x82, 0xc4, 0xff), 32, 4), 32, 1, Ink);
            _tile = UiArt.Style(UiArt.Round(Color.white, CardEdge, 16, 2), 16, 1, Ink);
            _tile.active.background = UiArt.Round(HaveFill, CardEdge, 16, 2);
            _tilePicked = new GUIStyle(_tile);
            _tilePicked.normal.background = UiArt.Round(new Color32(0xff, 0xf1, 0xc8, 0xff), Gold, 16, 5);
            _count = UiArt.Style(UiArt.Round(Gold, Ink, 14, 2), 14, 18, Ink);
            _count.fontStyle = FontStyle.Bold;
            _count.padding = new RectOffset(0, 0, 0, 0);
            _need = new GUIStyle(_count);
            _need.normal.background = UiArt.Round(Red, Ink, 14, 2);
            _need.normal.textColor = Color.white;
            _wood = UiArt.Style(UiArt.Round(WoodFill, WoodEdge, 16, 3), 16, 20, Color.white);
            _wood.fontStyle = FontStyle.Bold;
            _wood.active.background = UiArt.Round(WoodEdge, WoodEdge, 16, 3);
            _wood.active.textColor = Color.white;
            _green = UiArt.Style(UiArt.Round(Green, GreenEdge, 20, 4), 20, 28, Color.white);
            _green.fontStyle = FontStyle.Bold;
            _green.active.background = UiArt.Round(GreenEdge, GreenEdge, 20, 4);
            _green.active.textColor = Color.white;
            _mineBtn = UiArt.Style(UiArt.Round(Orange, OrangeEdge, 24, 5), 24, 32, Color.white);
            _mineBtn.fontStyle = FontStyle.Bold;
            _mineBtn.active.background = UiArt.Round(OrangeEdge, OrangeEdge, 24, 5);
            _mineBtn.active.textColor = Color.white;
            _mineBtn.richText = true;
            _toast = UiArt.Style(UiArt.Round(new Color(0, 0, 0, 0.82f), new Color(0, 0, 0, 0.82f), 18, 0), 18, 22, Color.white);
            _panel = UiArt.Style(UiArt.Round(Cream, Frame, 22, 5), 22, 1, Ink);
            _panelTitle = new GUIStyle { fontSize = 32, fontStyle = FontStyle.Bold, wordWrap = true, alignment = TextAnchor.MiddleCenter };
            _panelTitle.normal.textColor = Ink;
            _panelBody = new GUIStyle(_panelTitle) { fontSize = 22, fontStyle = FontStyle.Normal };
            _name = new GUIStyle { fontSize = 17, alignment = TextAnchor.UpperCenter, wordWrap = true, fontStyle = FontStyle.Bold };
            _name.normal.textColor = Ink;
            _invisible = new GUIStyle();
            _roundBtn = UiArt.Style(UiArt.Round(Cream, CardEdge, 22, 3), 22, 1, Ink);
            _roundBtn.active.background = UiArt.Round(HaveFill, CardEdge, 22, 3);
            // The chest window: night sky in a wooden frame, a red ribbon title and a gold button.
            _nightPanel = GameArt.NightPanel != null ? UiArt.Style(GameArt.NightPanel, 56, 1, Color.white) : _panel;
            if (GameArt.NightPanel != null)
            {
                // Messages: a small purple plaque instead of a black box.
                _toast = new GUIStyle(_nightPanel) { fontSize = 22, fontStyle = FontStyle.Bold, wordWrap = true, alignment = TextAnchor.MiddleCenter };
                _toast.padding = new RectOffset(34, 34, 18, 18);
                _toast.normal.textColor = new Color32(0xfb, 0xf0, 0xdc, 0xff);
            }
            _gold = GameArt.GoldButton != null ? UiArt.Style(GameArt.GoldButton, 40, 40, Color.white) : _green;
            if (GameArt.GoldButton != null)
            {
                _gold.border = new RectOffset(40, 40, 40, 44);
                _gold.active.background = GameArt.GoldButtonDown;
                _gold.active.textColor = Color.white;
            }
            _gold.fontStyle = FontStyle.Bold;
            _ribbonTitle = new GUIStyle { fontSize = 36, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _ribbonTitle.normal.textColor = new Color32(0xff, 0xf8, 0xe6, 0xff);
            _onNight = new GUIStyle(_ribbonTitle) { fontSize = 22, alignment = TextAnchor.UpperCenter, wordWrap = true };
            _nightBody = new GUIStyle { fontSize = 23, alignment = TextAnchor.UpperCenter, wordWrap = true };
            _nightBody.normal.textColor = new Color32(0xee, 0xea, 0xdc, 0xff);
            _badge = UiArt.Style(UiArt.Round(Red, new Color32(0xff, 0xf8, 0xe6, 0xff), 18, 3), 18, 20, Color.white);
            _badge.fontStyle = FontStyle.Bold;
            _badge.padding = new RectOffset(0, 0, 0, 2);
            UiArt.UseGameFont(this);
        }

        /// <summary>Text with a dark outline, so it reads on any background.</summary>
        private static void Outlined(Rect r, string text, GUIStyle style, Color outline, float width = 2.5f)
        {
            var main = style.normal.textColor;
            style.normal.textColor = outline;
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4;
                GUI.Label(new Rect(r.x + Mathf.Cos(a) * width, r.y + Mathf.Sin(a) * width, r.width, r.height), text, style);
            }
            style.normal.textColor = main;
            GUI.Label(r, text, style);
        }

        private bool _pressInView, _dragging;
        private Vector2 _pressAt;
        private float _flingSpeed;

        /// <summary>
        /// Finger scrolling for the page: IMGUI's scroll view only scrolls with a wheel or its bar.
        /// A press that moves more than a few points becomes a drag, and its release presses no button.
        /// </summary>
        private void DragScroll(Rect view)
        {
            var e = Event.current;
            float max = Mathf.Max(0, _contentH - view.height);
            switch (e.type)
            {
                case EventType.MouseDown when view.Contains(e.mousePosition):
                    _pressInView = true;
                    _dragging = false;
                    _pressAt = e.mousePosition;
                    _flingSpeed = 0;
                    break;
                case EventType.MouseDrag when _pressInView:
                    if (!_dragging && Mathf.Abs(e.mousePosition.y - _pressAt.y) > 12) _dragging = true;
                    if (_dragging)
                    {
                        _scroll.y = Mathf.Clamp(_scroll.y - e.delta.y, 0, max);
                        _flingSpeed = Mathf.Lerp(_flingSpeed, -e.delta.y / Mathf.Max(Time.unscaledDeltaTime, 0.008f), 0.5f);
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break;
                case EventType.MouseUp when _pressInView:
                    _pressInView = false;
                    if (_dragging)
                    {
                        _dragging = false;
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    else _flingSpeed = 0;
                    break;
                case EventType.ScrollWheel when view.Contains(e.mousePosition):
                    _scroll.y = Mathf.Clamp(_scroll.y + e.delta.y * 24, 0, max);
                    _flingSpeed = 0;
                    e.Use();
                    break;
                case EventType.Repaint when !_pressInView && Mathf.Abs(_flingSpeed) > 20:
                    _scroll.y = Mathf.Clamp(_scroll.y + _flingSpeed * Time.unscaledDeltaTime, 0, max);
                    _flingSpeed *= Mathf.Exp(-4f * Time.unscaledDeltaTime);
                    if (_scroll.y <= 0 || _scroll.y >= max) _flingSpeed = 0;
                    break;
            }
        }

        private void OnGUI()
        {
            if (_s == null) return;
            Styles();
            float scale = Mathf.Min(Screen.width / 720f, Screen.height / 1280f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float w = Screen.width / scale, h = Screen.height / scale;
            float colW = Mathf.Min(w, 760), x0 = (w - colW) / 2, pad = 16, inner = colW - 2 * pad;

            // Everything fits on one screen, from the bottom up: the mine button, the chests and storage tiles,
            // the next part card, and the meadow with the castle filling the rest.
            float mineY = h - 16 - 100, tilesY = mineY - 14 - TilesH;
            bool hasNext = Game.NextSlot(State) >= 0;
            float cardsTop = hasNext ? tilesY - 14 - NextH : tilesY;
            float groundY = cardsTop - 30;
            AskHint(new Rect(x0 + 40, 150, colW - 80, 300));
            var bg = GameArt.BuildBackground;
            // The meadow fills the screen; its horizon (about 60% down the picture) sits a little behind the building site.
            if (bg != null) GameArt.DrawCoverAnchored(new Rect(0, 0, w, h), bg, 0.6f, groundY - 150);
            if (_donePanel)
            {
                // The finished building gets the whole screen; the page comes back with the next building.
                DrawShowcase(w, h);
                Hints.Draw();
                return;
            }
            DrawScene(new Rect(x0, 0, colW, groundY + 74), groundY);

            // Behind an open window the page doesn't take clicks.
            GUI.enabled = _reward == null && !_donePanel && !_askStartOver && !_storageOpen && !_chestsOpen && !_settingsOpen;
            DrawHeader(new Rect(x0 + pad, 16, inner, 100));
            if (GUI.enabled) CastleTap(cardsTop - 8);
            if (hasNext) DrawNext(x0 + pad, cardsTop, inner);
            var storageTile = DrawTiles(new Rect(x0 + pad, tilesY, inner, TilesH));
            DrawMineButton(new Rect(x0 + pad, mineY, inner, 100));
            GUI.enabled = true;
            DrawPlaceFlight();

            // Short messages show over the sky, under the header, where they cover nothing.
            if (_message != null && Time.time < _messageUntil && !_storageOpen && !_chestsOpen)
                GUI.Box(new Rect(x0 + pad + 20, 172, inner - 40, 96), _message, _toast);

            // Parts from the last opening fly down to the storage tile.
            DrawFlights(storageTile.center);
            ShowPointer();

            if (_reward != null) DrawRewardPanel(w, h);
            else if (_askStartOver) DrawStartOverPanel(w, h);
            else if (_settingsOpen) DrawSettingsPanel(w, h);
            else if (_storageOpen) DrawStorageWindow(w, h);
            else if (_chestsOpen) DrawChestsWindow(w, h);
            Hints.Draw();
        }

        private Rect _placeAt, _chestsAt, _mineAt;

        /// <summary>For a new player: an arrow over the button to tap next, until they have done it once.</summary>
        private void ShowPointer()
        {
            if (Hints.Showing || _reward != null || _donePanel || _askStartOver || _settingsOpen || _storageOpen || _chestsOpen || _flying.Count > 0 || Placing) return;
            int next = Game.NextSlot(State);
            if (next < 0) return;
            bool have = State.Count(Game.CastleOf(State).Slots[next]) > 0;
            if (have) Pointer.Show("place", _placeAt);
            else if (State.Chests.Count > 0) Pointer.Show("chest", _chestsAt, into: true);
            else if (State.Run != null || State.Tickets > 0) Pointer.Show("mine", _mineAt);
        }

        /// <summary>The first-time hint that fits what the player can do now, if they haven't seen it.</summary>
        private void AskHint(Rect bubble)
        {
            if (_reward != null || _donePanel || _askStartOver || _settingsOpen || _storageOpen || _chestsOpen || _flying.Count > 0) return;
            int next = Game.NextSlot(State);
            if (next < 0) return;
            bool have = State.Count(Game.CastleOf(State).Slots[next]) > 0;
            if (State.Chests.Count > 0 && !have)
                Hints.Want("chests", bubble, "Замок строится из деталей, снизу вверх.\nДетали лежат в сундуках: тапни плитку «Сундуки» внизу.");
            else if (have)
                Hints.Want("place", bubble, "Светится деталь, которую ставить следующей.\nТапни по ней на рисунке или жми «Поставить».");
            else if (State.Chests.Count == 0)
                Hints.Want("mine", bubble, "Деталей не хватает.\nСундуки с деталями добывают в шахте: жми оранжевую кнопку внизу.");
        }

        // ---------- header ----------

        private void DrawHeader(Rect r)
        {
            var castle = Game.CastleOf(State);
            int done = State.Filled.Count(f => f), total = State.Filled.Length;
            const float progW = 150, btn = 64;
            if (GUI.Button(new Rect(r.x, r.y, btn, btn), GUIContent.none, _roundBtn)) Later(AskStartOver);
            GUI.DrawTexture(new Rect(r.x + 11, r.y + 11, btn - 22, btn - 22), UiArt.Restart, ScaleMode.ScaleToFit);
            // Settings: music, sounds and vibration. The speaker is crossed out when both music and sounds are off.
            var snd = new Rect(r.x + btn + 8, r.y, btn, btn);
            if (GUI.Button(snd, GUIContent.none, _roundBtn)) Later(() => _settingsOpen = true);
            GUI.DrawTexture(new Rect(snd.x + 11, snd.y + 11, btn - 22, btn - 22), Sfx.Muted && Music.Off ? UiArt.SoundOff : UiArt.SoundOn, ScaleMode.ScaleToFit);
            float px = snd.xMax + 10;
            var title = new GUIStyle(_pill) { fontSize = 23, wordWrap = false, clipping = TextClipping.Clip, padding = new RectOffset(16, 10, 4, 4) };
            GUI.Label(new Rect(px, r.y, r.xMax - progW - 12 - px, 64), $"{State.CastleIndex + 1}. {castle.Name}", title);
            var center = new GUIStyle(_pill) { alignment = TextAnchor.MiddleCenter, richText = true, padding = new RectOffset(4, 4, 4, 4) };
            GUI.Label(new Rect(r.xMax - progW, r.y, progW, 64), $"<color=#6b5aa6>{done}</color> из {total}", center);

            var bar = new Rect(r.x, r.y + 76, r.width, 16);
            GUI.DrawTexture(bar, UiArt.Round(new Color(1, 1, 1, 0.75f), CardEdge, 8, 2));
            float fw = Mathf.Max(16, bar.width * done / Mathf.Max(1, total));
            if (done > 0) GUI.DrawTexture(new Rect(bar.x, bar.y, fw, bar.height), UiArt.Round(Green, GreenEdge, 8, 2));

            // Total points under the bar, on the right; it swells a little when points come in.
            if (State.Score != _shownScore) { if (_shownScore >= 0) _scoreAt = Time.time; _shownScore = State.Score; }
            float k = 1 + 0.15f * Mathf.Clamp01(1 - (Time.time - _scoreAt) / 0.35f);
            var score = new GUIStyle(center) { fontSize = 22, wordWrap = false };
            string text = $"Очки: <color=#6b5aa6>{State.Score}</color>";
            float sw = Mathf.Max(170, score.CalcSize(new GUIContent($"Очки: {State.Score}")).x + 34);
            var sr = new Rect(r.xMax - sw, bar.yMax + 10, sw, 46);
            var m = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(k, k), sr.center);
            GUI.Label(sr, text, score);
            GUI.matrix = m;
        }

        private int _shownScore = -1;
        private float _scoreAt = -10;

        // ---------- castle ----------

        private int _placedSlot = -1;
        private float _placedAt;

        /// <summary>Soft shadow, then a trodden dirt patch with a few stones, centred on <paramref name="cx"/>.</summary>
        private static void DrawSite(float cx, float groundY, float siteW, float siteH)
        {
            GUI.DrawTexture(new Rect(cx - siteW * 0.6f, groundY - siteH * 0.7f, siteW * 1.2f, siteH * 1.75f), UiArt.SoftDisc(new Color(0.15f, 0.1f, 0.05f, 0.45f)));
            GUI.DrawTexture(new Rect(cx - siteW / 2, groundY - siteH * 0.55f, siteW, siteH), UiArt.Disc(SiteFill, SiteEdge, 2.5f));
            GUI.DrawTexture(new Rect(cx - siteW * 0.45f, groundY - siteH * 0.47f, siteW * 0.9f, siteH * 0.72f), UiArt.Disc(SiteLight, SiteLight, 0));
            foreach (var (dx, dy) in Pebbles)
                GUI.DrawTexture(new Rect(cx + dx * siteW - 7, groundY + dy * siteH - 4, 14, 8), UiArt.Disc(SiteEdge, SiteEdge, 0));
        }

        /// <summary>The building site on the meadow and the castle standing on it, bottom at <paramref name="groundY"/>.</summary>
        private void DrawScene(Rect r, float groundY)
        {
            // As big as the space allows: the castle's own width fits the screen, its top stays under the header.
            float foot = Art != null ? Art.FootRight - Art.FootLeft : 0.46f;
            float bottomFrac = Art != null ? Art.FootBottom : 0.98f;
            float size = Mathf.Min(r.width * 0.96f / Mathf.Max(0.3f, foot), (groundY - 128) / bottomFrac, r.width * 1.6f);
            // The dirt site is a little wider than the castle standing on it (castle 2 is much wider than castle 1).
            float cx = r.center.x, siteW = Mathf.Min(r.width * 0.98f, Mathf.Max(size * 0.5f, size * foot * 1.15f)), siteH = Mathf.Min(size, 760) * 0.13f;
            DrawSite(cx, groundY, siteW, siteH);

            var pic = new Rect(cx - size / 2, groundY + siteH * 0.05f - size * (Art != null ? Art.FootBottom - 0.01f : 0.98f), size, size);
            _picRect = pic;
            int next = Game.NextSlot(State);
            if (Art != null)
            {
                float t = _placedSlot >= 0 ? Mathf.Clamp01((Time.time - _placedAt) / DropTime) : 1;
                var shaken = new Rect(pic.x + LandingShake(), pic.y, pic.width, pic.height);
                Art.DrawBuilt(shaken, State.Filled, next, 0.55f + 0.45f * Mathf.Sin(Time.time * 4f), _placedSlot, t);
                DrawLanding(pic);
                return;
            }
            // No art for this castle yet: its slots as a grid of icons on the site, built ones solid.
            var castle = Game.CastleOf(State);
            const int perRow = 6;
            float cell = Mathf.Min(64, r.width * 0.8f / perRow);
            int rows = Mathf.CeilToInt(castle.Slots.Length / (float)perRow);
            float gx = cx - perRow * cell / 2, gy = groundY - siteH * 0.3f - rows * cell;
            for (int i = 0; i < castle.Slots.Length; i++)
            {
                // Bottom row first, like the building order.
                var c = new Rect(gx + (i % perRow) * cell, gy + (rows - 1 - i / perRow) * cell, cell - 4, cell - 4);
                var old = GUI.color;
                if (!State.Filled[i]) GUI.color = new Color(1, 1, 1, i == next ? 0.55f + 0.3f * Mathf.Sin(Time.time * 4f) : 0.18f);
                GameArt.DrawPart(c, castle.Slots[i], Game.Catalog.TierOf(castle.Slots[i]));
                GUI.color = old;
            }
        }

        private Rect _picRect;
        private Vector2? _castleDown;

        /// <summary>A tap on a part of the castle drawing: places it, or says why not, or names it.</summary>
        private void CastleTap(float sheetTop)
        {
            if (Art == null) return;
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.mousePosition.y < sheetTop &&
                Art.SlotAt(_picRect, e.mousePosition, State.Filled, Game.NextSlot(State)) >= 0)
            {
                _castleDown = e.mousePosition;
                e.Use();
            }
            else if (e.type == EventType.MouseUp && _castleDown.HasValue)
            {
                bool tap = (e.mousePosition - _castleDown.Value).magnitude < 24;
                _castleDown = null;
                int slot = tap ? Art.SlotAt(_picRect, e.mousePosition, State.Filled, Game.NextSlot(State)) : -1;
                if (slot >= 0) Later(() => { if (State.Filled[slot]) TapSlot(slot); else StartPlace(slot); }, click: false);
                e.Use();
            }
        }

        private static readonly Color SiteFill = new Color32(0xb0, 0x80, 0x52, 0xff);
        private static readonly Color SiteEdge = new Color32(0x78, 0x54, 0x32, 0xff);
        private static readonly Color SiteLight = new Color32(0xc4, 0x96, 0x64, 0xff);
        private static readonly (float, float)[] Pebbles = { (-0.36f, 0.05f), (0.29f, 0.1f), (-0.14f, 0.28f), (0.14f, -0.22f), (0.4f, -0.12f), (-0.42f, -0.16f) };

        // ---------- the next part ----------

        /// <summary>Places the next part of the castle if it is this one. False otherwise.</summary>
        public bool PlacePart(string id)
        {
            int next = Game.NextSlot(State);
            return next >= 0 && Game.CastleOf(State).Slots[next] == id && PlaceSlot(next);
        }

        /// <summary>How many parts in a row, starting from the next one, storage covers.</summary>
        private int PlaceableInARow()
        {
            var slots = Game.CastleOf(State).Slots;
            var used = new Dictionary<string, int>();
            int n = 0;
            for (int i = Game.NextSlot(State); i >= 0 && i < slots.Length; i++, n++)
            {
                used.TryGetValue(slots[i], out int u);
                if (State.Count(slots[i]) <= u) break;
                used[slots[i]] = u + 1;
            }
            return n;
        }

        private float DrawNext(float x, float y, float inner)
        {
            int next = Game.NextSlot(State);
            if (next < 0) return y - 12;
            var slots = Game.CastleOf(State).Slots;
            string id = slots[next];
            int tier = Game.Catalog.TierOf(id);
            int nth = 0, total = 0;
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] == id) { total++; if (i <= next) nth++; }
            int have = State.Count(id), row = PlaceableInARow();

            const float padX = 18, hgt = 196;
            var card = new Rect(x, y, inner, hgt);
            GUI.Box(card, GUIContent.none, _nextCard);
            GUI.Label(new Rect(x + padX, y + 12, 400, 30), "СЛЕДУЮЩАЯ ДЕТАЛЬ", _section);
            var icon = new Rect(x + padX, y + 52, 124, 124);
            _iconAt = icon;
            GUI.Box(icon, GUIContent.none, have > 0 ? _tilePicked : _tile);
            GameArt.DrawPart(new Rect(icon.x + 12, icon.y + 12, 100, 100), id, tier);

            float tx = icon.xMax + 18, btnW = 210, textW = card.xMax - padX - tx - (have > 0 ? btnW + 12 : 0);
            GUI.Label(new Rect(tx, y + 56, textW, 44), Game.Catalog.NameOf(id), _bigName);
            string count = total > 1 ? $"{nth}-я из {total}" : "";
            string sub = have > 0 ? (count.Length > 0 ? count + " · " : "") + $"на складе {have}"
                : (count.Length > 0 ? count + ". " : "") + "Нет на складе. " + WhereFrom(id);
            GUI.Label(new Rect(tx, y + 104, textW, hgt - 112), sub, _sub);

            if (have > 0)
            {
                var b = new Rect(card.xMax - padX - btnW, y + 58, btnW, row > 1 ? 66 : 80);
                if (row <= 1) b.y += 10;
                // While a part is on its way the buttons wait for it to land.
                bool was = GUI.enabled;
                GUI.enabled = was && !Placing;
                if (GUI.Button(b, "Поставить", _green)) Later(() => StartPlace(next), click: false);
                _placeAt = b;
                if (row > 1 && GUI.Button(new Rect(b.x, b.yMax + 10, btnW, 50), $"Подряд · {row}", _wood)) Later(() => StartPlace(next, row), click: false);
                GUI.enabled = was;
            }
            return card.yMax;
        }

        // ---------- chests and storage tiles ----------

        private const float TilesH = 140, NextH = 196;

        /// <summary>Two tiles side by side: «Сундуки» opens a chest, «Склад» opens the storage window. Returns the storage tile.</summary>
        private Rect DrawTiles(Rect r)
        {
            const float gap = 12;
            float tw = (r.width - gap) / 2;
            var chests = new Rect(r.x, r.y, tw, r.height);
            var store = new Rect(r.x + tw + gap, r.y, tw, r.height);
            int n = State.Chests.Count;

            // The tile's card first, then the «все» button, then the tile's own (invisible) button:
            // IMGUI gives a click to the first button drawn under it, so «все» must come before the tile.
            GUI.Box(chests, GUIContent.none, _card);
            var all = new Rect(chests.xMax - 112, chests.yMax - 62, 102, 54);
            if (n > 1 && SmallGoldButton(all, "все", 34)) Later(() => OpenChests());
            _chestsAt = new Rect(chests.x + 12, chests.y + 14, 112, 112);
            if (GUI.Button(chests, GUIContent.none, _invisible) && n > 0)
                Later(() => { if (State.Chests.Count == 1) OpenChests(0); else _chestsOpen = true; });
            var chestIcon = new Rect(chests.x + 12, chests.y + 14, 112, 112);
            if (n > 0) GameArt.DrawChest(chestIcon, State.Chests[n - 1]);
            else
            {
                var old = GUI.color; GUI.color = new Color(1, 1, 1, 0.35f);
                GameArt.DrawChest(chestIcon, new Chest(1, 1));
                GUI.color = old;
            }
            GUI.Label(new Rect(chestIcon.xMax + 10, chests.y + 26, chests.xMax - chestIcon.xMax - 16, 44), "Сундуки", _tileTitle);
            GUI.Label(new Rect(chestIcon.xMax + 10, chests.y + 72, chests.xMax - chestIcon.xMax - (n > 1 ? 122 : 16), 56),
                n > 1 ? "выбрать" : n > 0 ? "тапни, чтобы открыть" : "добываются в шахте", _sub);
            if (n > 0) Badge(new Vector2(chestIcon.xMax - 6, chestIcon.y + 4), n);

            int parts = Game.Catalog.Parts.Sum(p => State.Count(p.Id));
            if (GUI.Button(store, GUIContent.none, _card)) Later(() => { _storageOpen = true; _pick.Clear(); });
            var top = Game.Catalog.Parts.Where(p => State.Count(p.Id) > 0).OrderByDescending(p => p.Tier).FirstOrDefault();
            var partIcon = new Rect(store.x + 18, store.y + 20, 100, 100);
            float pop = _pulseAt.Count > 0 ? Mathf.Clamp01((Time.time - _pulseAt.Values.Max()) / 0.35f) : 1;
            float grow = pop < 1 ? 1 + 0.22f * Mathf.Sin(pop * Mathf.PI) : 1;
            if (top != null) GameArt.DrawPart(Scale(partIcon, grow), top.Id, top.Tier);
            else
            {
                var old = GUI.color; GUI.color = new Color(1, 1, 1, 0.35f);
                GameArt.DrawPart(partIcon, "wall", Game.Catalog.TierOf("wall"));
                GUI.color = old;
            }
            GUI.Label(new Rect(partIcon.xMax + 16, store.y + 26, store.xMax - partIcon.xMax - 22, 44), "Склад", _tileTitle);
            GUI.Label(new Rect(partIcon.xMax + 16, store.y + 72, store.xMax - partIcon.xMax - 22, 50), parts > 0 ? "собрать детали" : "пусто", _sub);
            if (parts > 0) Badge(new Vector2(partIcon.xMax, partIcon.y), parts);
            return store;
        }

        private void Badge(Vector2 center, int n)
        {
            string t = n.ToString();
            float bw = t.Length > 1 ? 50 : 40;
            GUI.Label(new Rect(center.x - bw / 2, center.y - 20, bw, 40), t, _badge);
        }

        // ---------- chests window ----------

        private bool _chestsOpen;

        /// <summary>True while the chests window is open.</summary>
        public bool ChestsOpen => _chestsOpen;

        /// <summary>All chests on hand, alike ones grouped with a count. Tap one to open it, or open them all.</summary>
        private void DrawChestsWindow(float w, float h)
        {
            var groups = State.Chests.Select((ch, i) => (ch, i)).GroupBy(x => (x.ch.Color, x.ch.Stars))
                .OrderByDescending(g => g.Key.Stars).ThenBy(g => g.Key.Color)
                .Select(g => (chest: g.First().ch, last: g.Max(x => x.i), n: g.Count())).ToList();
            if (groups.Count == 0) { _chestsOpen = false; return; }
            Dim(w, h);
            float pw = Mathf.Min(680, w - 40);
            const float gap = 22;
            float size = 132;
            int perRow = 1;
            // The biggest chests that keep the grid within 4 rows.
            for (; size > 72; size -= 4)
            {
                perRow = Mathf.Max(1, Mathf.FloorToInt((pw - 80 + gap) / (size + gap)));
                if (Mathf.CeilToInt(groups.Count / (float)perRow) <= 4) break;
            }
            int rows = Mathf.CeilToInt(groups.Count / (float)perRow);
            float ph = Mathf.Min(h - 150, 170 + rows * (size + gap) + 30 + 96 + 24 + 72 + 46);
            var r = new Rect((w - pw) / 2, Mathf.Max(110, (h - ph) / 2), pw, ph);
            NightWindow(r, "Сундуки");
            GUI.Label(new Rect(r.x + 40, r.y + 92, r.width - 80, 64), $"Всего: {State.Chests.Count}. Тапни сундук, чтобы открыть его, или открой все сразу.", _nightBody);
            float gy = r.y + 170;
            for (int i = 0; i < groups.Count; i++)
            {
                int row = i / perRow, col = i % perRow;
                int inRow = Mathf.Min(perRow, groups.Count - row * perRow);
                float rowW = inRow * size + (inRow - 1) * gap;
                var cell = new Rect(r.center.x - rowW / 2 + col * (size + gap), gy + row * (size + gap), size, size);
                bool over = cell.Contains(Event.current.mousePosition);
                GameArt.DrawChest(over ? Scale(cell, 1.06f) : cell, groups[i].chest);
                if (groups[i].n > 1) Badge(new Vector2(cell.xMax - 12, cell.y + 8), groups[i].n);
                int idx = groups[i].last;
                if (GUI.Button(cell, GUIContent.none, _invisible)) Later(() => OpenChests(idx));
            }
            if (GoldButton(new Rect(r.x + 70, r.yMax - 238, r.width - 140, 96), "Открыть все", 34))
                Later(() => { _chestsOpen = false; OpenChests(); }, click: false);
            if (GUI.Button(new Rect(r.x + 130, r.yMax - 122, r.width - 260, 72), "Закрыть", new GUIStyle(_wood) { fontSize = 28 }))
                Later(() => _chestsOpen = false);
        }

        // ---------- storage window ----------

        private bool _storageOpen;
        private GUIStyle _strip, _targetName;

        /// <summary>True while the storage window is open.</summary>
        public bool StorageOpen => _storageOpen;

        private void DrawStorageWindow(float w, float h)
        {
            Dim(w, h);
            var rules = Game.Rules;
            var castle = Game.CastleOf(State);
            var parts = Game.Catalog.Parts.Where(p => State.Count(p.Id) > 0).OrderBy(p => p.Tier).Select(p => p.Id).ToList();
            float pw = Mathf.Min(680, w - 40);
            // As tall as the parts need: header 176, the grid (tile, level tag and name), the «Собрать» strip and the close button.
            int gridRows = Mathf.Max(1, Mathf.CeilToInt(parts.Count / 4f));
            float ph = Mathf.Min(h - 150, 176 + gridRows * 186 + 20 + 190 + 30 + 96 + 50);
            var r = new Rect((w - pw) / 2, Mathf.Max(110, (h - ph) / 2), pw, ph);
            NightWindow(r, "Склад");
            bool choosing = _pick.Count == 2;
            bool fresh = _message != null && Time.time < _messageUntil;
            string hint = parts.Count == 0 ? "Пусто. Детали выпадают из сундуков."
                : fresh ? _message
                : _pick.Count == 0 ? "Тапни две детали одного уровня, чтобы собрать из них деталь уровнем выше."
                : _pick.Count == 1 ? $"Выбрано: {Game.Catalog.NameOf(_pick[0])}. Выбери вторую того же уровня."
                : "Во что собрать? Тапни нужную деталь справа.";
            GUI.Label(new Rect(r.x + 40, r.y + 92, r.width - 80, 64), hint, _nightBody);

            // The parts, four in a row.
            const float tile = 124, nameH = 46, gap = 16;
            int perRow = Mathf.Max(1, Mathf.FloorToInt((r.width - 60 + gap) / (tile + gap)));
            float gridW = perRow * tile + (perRow - 1) * gap, gx = r.center.x - gridW / 2, gy = r.y + 176;
            var nameOnNight = new GUIStyle(_name);
            nameOnNight.normal.textColor = new Color32(0xfb, 0xf0, 0xdc, 0xff);
            for (int i = 0; i < parts.Count; i++)
            {
                string id = parts[i];
                int t = Game.Catalog.TierOf(id);
                int free = State.Count(id) - _pick.Count(p => p == id);
                bool picked = _pick.Contains(id);
                bool canAdd = t < rules.MaxTier && free > 0 && _pick.Count < 2 && (_pick.Count == 0 || Game.Catalog.TierOf(_pick[0]) == t);
                var cell = new Rect(gx + (i % perRow) * (tile + gap), gy + (i / perRow) * (tile + nameH + gap), tile, tile);
                var old = GUI.color;
                if (_pick.Count > 0 && !canAdd && !picked) GUI.color = new Color(1, 1, 1, 0.45f);
                if (GUI.Button(cell, GUIContent.none, picked ? _tilePicked : _tile) && (canAdd || picked))
                {
                    string part = id;
                    // Tap adds the part (the same part twice is fine); tap again when it can't be added removes it.
                    if (canAdd) Later(() => _pick.Add(part)); else Later(() => _pick.Remove(part));
                }
                // A part just made pops and throws out sparkles.
                float mt = id == _mergedId ? Time.time - _mergedAt : 9;
                float popK = mt < 0.35f ? 1 + 0.28f * Mathf.Sin(Mathf.PI * mt / 0.35f) : 1;
                GameArt.DrawPart(Scale(new Rect(cell.x + 14, cell.y + 12, tile - 28, tile - 28), popK), id, t);
                GUI.color = old;
                if (mt < 0.7f) DrawBurst(cell.center, mt / 0.7f);
                Badge(new Vector2(cell.xMax - 8, cell.y + 8), State.Count(id));
                int need = Math.Max(0, Game.Need(State, id));
                if (need > 0) GUI.Label(new Rect(cell.x - 6, cell.y - 6, 44, 28), "−" + need, _need);
                TierTag(new Rect(cell.center.x - 42, cell.yMax - 17, 84, 32), t, picked || _pick.Count == 0 || canAdd);
                GUI.Label(new Rect(cell.x - 10, cell.yMax + 18, tile + 20, nameH), Game.Catalog.NameOf(id), nameOnNight);
            }

            // What is being merged: two picked parts, an arrow and what they can become.
            var strip = new Rect(r.x + 30, r.yMax - 176 - 190, r.width - 60, 190);
            _strip ??= UiArt.Style(UiArt.Round(new Color32(0x3c, 0x18, 0x56, 0xff), new Color32(0xaa, 0x78, 0xd2, 0xff), 24, 3), 24, 1, Color.white);
            GUI.Box(strip, GUIContent.none, _strip);
            var stripTitle = new GUIStyle(_onNight) { fontSize = 26, alignment = TextAnchor.MiddleCenter };
            stripTitle.normal.textColor = new Color32(0xff, 0xd6, 0x6e, 0xff);
            GUI.Label(new Rect(strip.x, strip.y + 10, strip.width, 36), "Собрать", stripTitle);
            const float st = 92;
            float sy = strip.y + 62;
            for (int k = 0; k < 2; k++)
            {
                var c = new Rect(strip.x + 20 + k * (st + 34), sy, st, st);
                GUI.DrawTexture(c, UiArt.Round(new Color32(0xff, 0xff, 0xff, (byte)(k < _pick.Count ? 0xff : 0x55)), Gold, 18, k < _pick.Count ? 4 : 2));
                if (k < _pick.Count)
                {
                    GameArt.DrawPart(new Rect(c.x + 12, c.y + 12, st - 24, st - 24), _pick[k], Game.Catalog.TierOf(_pick[k]));
                    if (GUI.Button(c, GUIContent.none, _invisible)) { int idx = k; Later(() => { if (idx < _pick.Count) _pick.RemoveAt(idx); }); }
                }
                if (k == 0) GUI.Label(new Rect(c.xMax, c.y, 34, st), "+", new GUIStyle(stripTitle) { fontSize = 44, normal = { textColor = Color.white } });
            }
            float ax = strip.x + 20 + 2 * st + 34 + 6;
            GUI.Label(new Rect(ax, sy, 40, st), "→", new GUIStyle(stripTitle) { fontSize = 44 });
            float tx = ax + 46;
            if (choosing)
            {
                // Every possible target fits in the strip (smaller when there are many), the ones the castle needs first.
                var targets = castle.PartsOfTier(Game.Catalog.TierOf(_pick[0]) + 1)
                    .OrderByDescending(t => Game.Need(State, t) > 0).ThenByDescending(t => Game.Need(State, t)).ToList();
                const float tg = 14;
                float ts = Mathf.Min(st, (strip.xMax - 14 - tx - tg * (targets.Count - 1)) / Mathf.Max(1, targets.Count));
                float ty = sy + (st - ts) / 2;
                foreach (var target in targets)
                {
                    var c = new Rect(tx, ty, ts, ts);
                    bool needed = Game.Need(State, target) > 0;
                    if (GUI.Button(c, GUIContent.none, needed ? _tilePicked : _tile))
                    {
                        string pa = _pick[0], pb = _pick[1], to = target;
                        Later(() => Merge(pa, pb, to), click: false);
                    }
                    float pad = ts * 0.13f;
                    GameArt.DrawPart(new Rect(c.x + pad, c.y + pad, ts - 2 * pad, ts - 2 * pad), target, Game.Catalog.TierOf(target));
                    // What it is, under the tile: the choice is made by name, not only by the small picture.
                    _targetName ??= new GUIStyle(_onNight) { fontSize = 15, alignment = TextAnchor.UpperCenter, wordWrap = false, clipping = TextClipping.Clip };
                    GUI.Label(new Rect(c.x - tg / 2, c.yMax + 2, ts + tg, 24), Game.Catalog.NameOf(target), _targetName);
                    tx += ts + tg;
                }
            }
            else
                GUI.DrawTexture(new Rect(tx, sy, st, st), UiArt.Round(new Color32(0xff, 0xff, 0xff, 0x55), Gold, 18, 2));

            if (GoldButton(new Rect(r.center.x - 160, r.yMax - 140, 320, 96), "Закрыть", 34))
                Later(() => { _storageOpen = false; _pick.Clear(); }, click: false);
        }

        // ---------- to the mine ----------

        private void DrawMineButton(Rect r)
        {
            bool ad = Game.CanWatchAdForTickets(State);
            string text = State.Run != null ? $"Продолжить · {State.Run.Moves} ход."
                : State.Tickets > 0 ? $"Добывать · {State.Opts.Moves} ходов  <size=22>билетов: {State.Tickets}</size>"
                : ad ? $"+{Game.Rules.AdTickets} билета · реклама  <size=22>билеты кончились</size>"
                : "Билеты кончились, новые завтра";
            _mineAt = r;
            if (GUI.Button(r, text, _mineBtn) && _reward == null)
            {
                if (State.Run == null && State.Tickets < 1)
                {
                    if (ad) Later(() => WatchAdForTickets(), click: false);
                    else Say("Билеты кончились. Новые придут завтра.");
                }
                else Later(() => { Pointer.MarkDone("mine"); _toMine?.Invoke(); });
            }
        }

        // ---------- windows ----------

        private static void Dim(float w, float h)
        {
            GUI.color = new Color(0, 0, 0, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        /// <summary>The night-sky window with its title on a red ribbon, like the chest window.</summary>
        private void NightWindow(Rect r, string title)
        {
            GUI.Box(r, GUIContent.none, _nightPanel);
            if (GameArt.Ribbon == null) { GUI.Label(new Rect(r.x, r.y + 24, r.width, 50), title, _panelTitle); return; }
            var rib = new Rect(r.center.x - 256, r.y - 44, 512, 128);
            GUI.DrawTexture(rib, GameArt.Ribbon);
            Outlined(new Rect(rib.x + 44, rib.y + 14, rib.width - 88, 82), title, _ribbonTitle, RibbonInk);
        }

        /// <summary>A chunky gold button with an outlined label that sinks when pressed.</summary>
        private bool GoldButton(Rect b, string text, int fontSize = 38)
        {
            bool down = GUIUtility.hotControl != 0 && b.Contains(Event.current.mousePosition);
            bool hit = GUI.Button(b, GUIContent.none, _gold);
            var face = new Rect(b.x, b.y + (down ? 10 : 0), b.width, b.height - 10);
            Outlined(face, text, new GUIStyle(_ribbonTitle) { fontSize = fontSize }, ButtonInk, 3f);
            return hit;
        }

        /// <summary>The gold button drawn smaller: the art is 9-sliced at full size and then scaled down as a whole.</summary>
        private bool SmallGoldButton(Rect b, string text, int fontSize, float k = 0.6f)
        {
            var m = GUI.matrix;
            GUI.matrix = m * Matrix4x4.TRS(new Vector3(b.x, b.y, 0), Quaternion.identity, new Vector3(k, k, 1));
            bool hit = GoldButton(new Rect(0, 0, b.width / k, b.height / k), text, Mathf.RoundToInt(fontSize / k));
            GUI.matrix = m;
            return hit;
        }

        private void DrawRays(Vector2 c, float size)
        {
            if (GameArt.Rays == null) return;
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(Time.time * 8f, c);
            GUI.DrawTexture(new Rect(c.x - size / 2, c.y - size / 2, size, size), GameArt.Rays);
            GUI.matrix = m;
        }

        /// <summary>A part's level on a little plaque in its chest colour, under its storage tile.</summary>
        private void TierTag(Rect r, int tier, bool bright)
        {
            var c = Palette.Tier(tier);
            var old = GUI.color;
            if (!bright) GUI.color = new Color(1, 1, 1, 0.45f);
            GUI.DrawTexture(r, UiArt.Round(c, Color.Lerp(c, Color.black, 0.45f), 15, 3));
            // A soft light top half, like the gold buttons.
            GUI.DrawTexture(new Rect(r.x + 6, r.y + 4, r.width - 12, r.height * 0.36f), UiArt.Round(new Color(1, 1, 1, 0.28f), new Color(1, 1, 1, 0), 6, 0));
            Outlined(r, $"ур. {tier}", _tierTag ??= new GUIStyle(_ribbonTitle) { fontSize = 19 }, Color.Lerp(c, Color.black, 0.6f), 2f);
            GUI.color = old;
        }

        private GUIStyle _tierTag, _showName, _showText;

        private static float EaseOut(float t) { t = Mathf.Clamp01(t); return 1 - (1 - t) * (1 - t) * (1 - t); }

        /// <summary>
        /// The finished building: it grows from its place on the meadow to the whole screen, rays turn behind it and
        /// confetti falls. The card with the reward and «Далее» comes only after that (a tap brings it sooner).
        /// </summary>
        private void DrawShowcase(float w, float h)
        {
            const float cardAt = 1.8f;
            float t = Time.time - _doneAt;
            if (t < cardAt && Event.current.type == EventType.MouseDown) { _doneAt = Time.time - cardAt; Event.current.Use(); }
            bool last = Game.IsLastCastle(State);
            float grow = EaseOut(t / 0.9f);

            // The meadow darkens a little so the building and its rays stand out.
            GUI.color = new Color(0.05f, 0.03f, 0.12f, 0.5f * grow);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Where the building ends up: as big as fits between the title and the card.
            const float top = 196;
            float cardH = 330, cardTop = h - 24 - cardH, bottom = cardTop - 24;
            float foot = Art != null ? Art.FootRight - Art.FootLeft : 0.6f;
            float fb = Art != null ? Art.FootBottom : 0.98f;
            float size = Mathf.Min(w * 0.94f / Mathf.Max(0.45f, foot), (bottom - top) / fb);
            var target = new Rect(w / 2 - size / 2, bottom - size * fb, size, size);
            var from = _picRect.width > 0 ? _picRect : target;
            var pic = new Rect(Mathf.Lerp(from.x, target.x, grow), Mathf.Lerp(from.y, target.y, grow),
                Mathf.Lerp(from.width, target.width, grow), Mathf.Lerp(from.height, target.height, grow));
            // After it has grown it breathes a little.
            pic = Scale(pic, 1 + 0.012f * grow * Mathf.Sin(Time.time * 2.2f));

            var mid = new Vector2(pic.center.x, pic.y + pic.height * fb * 0.55f);
            GUI.color = new Color(1, 1, 1, grow);
            DrawRays(mid, pic.height * 1.35f);
            GUI.color = Color.white;
            // The building stands on its dirt site, as on the build screen, not in the air.
            float siteW = Mathf.Max(pic.width * 0.5f, pic.width * foot * 1.15f), siteH = Mathf.Min(pic.width, 760) * 0.13f;
            DrawSite(pic.center.x, pic.y + pic.height * (fb - 0.01f) - siteH * 0.05f, siteW, siteH);
            if (Art?.Full != null) GUI.DrawTexture(pic, Art.Full, ScaleMode.ScaleToFit);
            else Art?.Draw(pic, State.Filled);

            DrawConfetti(w, h, t);

            // «Построено!» on the ribbon drops in, the building's name under it.
            float drop = EaseOut((t - 0.15f) / 0.5f);
            if (drop > 0)
            {
                GUI.color = new Color(1, 1, 1, drop);
                var rib = new Rect(w / 2 - 280, 26 - 150 * (1 - drop), 560, 140);
                if (GameArt.Ribbon != null) GUI.DrawTexture(rib, GameArt.Ribbon);
                Outlined(new Rect(rib.x + 44, rib.y + 16, rib.width - 88, 88), "Построено!", new GUIStyle(_ribbonTitle) { fontSize = 46 }, RibbonInk, 3f);
                GUI.color = Color.white;
            }
            // The name comes once the ribbon has landed, on a dark plate, so it never reads as grey on the bright meadow.
            float nameIn = EaseOut((t - 0.55f) / 0.3f);
            if (nameIn > 0)
            {
                _showName ??= new GUIStyle(_ribbonTitle) { fontSize = 34 };
                string name = Game.CastleOf(State).Name;
                float nw = _showName.CalcSize(new GUIContent(name)).x + 90;
                var plate = new Rect(w / 2 - nw / 2, 26 + 140 - 2, nw, 54);
                GUI.color = new Color(1, 1, 1, nameIn);
                GUI.DrawTexture(Scale(plate, 1.1f), UiArt.SoftDisc(new Color(0.06f, 0.05f, 0.16f, 0.75f)));
                Outlined(plate, name, _showName, NightInk, 3f);
                GUI.color = Color.white;
            }

            // The card with what the player gets, then «Далее».
            float show = EaseOut((t - cardAt) / 0.45f);
            if (show <= 0) return;
            GUI.color = new Color(1, 1, 1, show);
            var card = new Rect(30, cardTop + (1 - show) * 120, w - 60, cardH);
            GUI.Box(card, GUIContent.none, _nightPanel);
            var lines = new List<string> { $"Бонус за постройку: <color=#ffd66e>+{Scoring.Building(State.CastleIndex)}</color> очков. Всего: {State.Score}." };
            if (!last)
            {
                lines.Add($"Награда: <color=#ffd66e>+{Game.Rules.TicketsPerCastle} билета</color> на следующую постройку.");
                var bp = Game.PreviewBurn(State);
                var lost = new List<string>();
                if (bp.Parts > 0) lost.Add($"деталей: {bp.Parts}");
                if (bp.Chests > 0) lost.Add($"закрытых сундуков: {bp.Chests}");
                lines.Add(lost.Count > 0 ? $"<size=19>Сгорят {string.Join(", ", lost)}. Фигурки остаются.</size>" : "<size=19>Склад пуст, сгорать нечему. Фигурки остаются.</size>");
            }
            else
            {
                lines.Add($"<color=#ffd66e>Все {Game.Catalog.Castles.Count} построек готовы!</color> Новые замки скоро появятся.");
                lines.Add("<size=19>А пока можно пройти всё заново: очки, рекорд и билеты останутся.</size>");
            }
            _showText ??= new GUIStyle(_nightBody) { richText = true, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(card.x + 34, card.y + 26, card.width - 68, cardH - 160), string.Join("\n", lines), _showText);
            if (GoldButton(new Rect(card.center.x - 170, card.yMax - 126, 340, 100), last ? "Пройти заново" : "Далее", 40) && show >= 1)
                Later(NextCastle, click: false);
            GUI.color = Color.white;
        }

        private static readonly Color32[] ConfettiColors =
        {
            new Color32(0xff, 0xd6, 0x6e, 0xff), new Color32(0xf2, 0x5f, 0x5c, 0xff), new Color32(0x4f, 0xb3, 0x5a, 0xff),
            new Color32(0x4f, 0x8f, 0xe0, 0xff), new Color32(0xb0, 0x7c, 0xe8, 0xff), new Color32(0xff, 0xff, 0xff, 0xff),
        };

        /// <summary>Paper confetti falling and turning; each piece has its own fixed path, so no state is kept.</summary>
        private static void DrawConfetti(float w, float h, float t)
        {
            if (Event.current.type != EventType.Repaint) return;
            var m = GUI.matrix;
            for (int i = 0; i < 46; i++)
            {
                float a = Mathf.Repeat(Mathf.Sin(i * 12.9898f) * 43758.5453f, 1);
                float b = Mathf.Repeat(Mathf.Sin(i * 78.233f) * 12543.1234f, 1);
                float start = b * 1.2f, speed = 170 + a * 160;
                float lt = t - start;
                if (lt < 0) continue;
                float y = -30 + Mathf.Repeat(lt * speed, h + 60);
                float x = a * w + Mathf.Sin(lt * (1.5f + b * 2) + i) * 26;
                var c = (Color)ConfettiColors[i % ConfettiColors.Length];
                var r = new Rect(x - 6, y - 9, 12, 18 * Mathf.Abs(Mathf.Cos(lt * (3 + 4 * a) + i)) + 3);
                GUI.matrix = m;
                GUIUtility.RotateAroundPivot((lt * (90 + 200 * b) + i * 37) % 360, r.center);
                GUI.DrawTexture(r, UiArt.Round(c, Color.Lerp(c, Color.black, 0.25f), 3, 1));
            }
            GUI.matrix = m;
        }

        private void DrawStartOverPanel(float w, float h)
        {
            Dim(w, h);
            float pw = Mathf.Min(620, w - 60);
            var r = new Rect((w - pw) / 2, h / 2 - 230, pw, 460);
            NightWindow(r, "Начать сначала?");
            GUI.Label(new Rect(r.x + 44, r.y + 96, r.width - 88, 110),
                "Весь прогресс сотрётся: замки, склад и сундуки. Игра начнётся с первого замка.", _nightBody);
            if (GoldButton(new Rect(r.x + 70, r.yMax - 228, r.width - 140, 96), "Начать сначала", 34)) Later(StartOver);
            if (GUI.Button(new Rect(r.x + 110, r.yMax - 116, r.width - 220, 72), "Отмена", _wood)) Later(() => _askStartOver = false);
        }

        /// <summary>Music, sounds and (on phones) vibration, each on or off.</summary>
        private void DrawSettingsPanel(float w, float h)
        {
            Dim(w, h);
            bool buzz = Haptics.Supported;
            float pw = Mathf.Min(620, w - 60), ph = buzz ? 600 : 500;
            var r = new Rect((w - pw) / 2, h / 2 - ph / 2, pw, ph);
            NightWindow(r, "Настройки");
            float y = r.y + 110;
            bool Row(string onText, string offText, bool on)
            {
                bool hit = GUI.Button(new Rect(r.x + 60, y, r.width - 120, 80), on ? onText : offText, on ? _green : _wood);
                y += 96;
                return hit;
            }
            if (Row("Музыка: включена", "Музыка: выключена", !Music.Off)) Later(() => Music.Off = !Music.Off, click: true);
            if (Row("Звуки: включены", "Звуки: выключены", !Sfx.Muted)) Later(() => { Sfx.Muted = !Sfx.Muted; if (!Sfx.Muted) Sfx.Play("click"); }, click: false);
            if (buzz && Row("Вибрация: включена", "Вибрация: выключена", !Haptics.Off)) Later(() => { Haptics.Off = !Haptics.Off; Haptics.Tap(40); }, click: true);
            if (GoldButton(new Rect(r.x + 70, r.yMax - 124, r.width - 140, 96), "Готово", 34)) Later(() => _settingsOpen = false);
        }

        /// <summary>Eight sparkles flying out from <paramref name="at"/> and fading, <paramref name="k"/> 0 → 1.</summary>
        private static void DrawBurst(Vector2 at, float k)
        {
            var sp = GameArt.Sparkle;
            if (sp == null || Event.current.type != EventType.Repaint) return;
            var old = GUI.color;
            GUI.color = new Color(1, 0.95f, 0.7f, 1 - k * k);
            float d = 30 + 70 * EaseOut(k), s = 46 * (1 - 0.5f * k);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4 + 0.3f;
                GUI.DrawTexture(new Rect(at.x + Mathf.Cos(a) * d - s / 2, at.y + Mathf.Sin(a) * d - s / 2, s, s), sp, ScaleMode.ScaleToFit);
            }
            GUI.color = old;
        }

        private static Rect Scale(Rect r, float k) =>
            new Rect(r.center.x - r.width * k / 2, r.center.y - r.height * k / 2, r.width * k, r.height * k);

        private void DrawFlights(Vector2 target)
        {
            const float dur = 0.45f;
            for (int i = _flying.Count - 1; i >= 0; i--)
            {
                var f = _flying[i];
                float t = (Time.time - f.Start) / dur;
                if (t >= 1) { _flying.RemoveAt(i); continue; }
                if (t < 0) t = 0;
                float e = t * t;                                   // speeds up as it falls
                var from = f.From.center;
                var mid = new Vector2((from.x + target.x) / 2, Mathf.Min(from.y, target.y) - 80);
                var p = Vector2.Lerp(Vector2.Lerp(from, mid, e), Vector2.Lerp(mid, target, e), e);
                float size = Mathf.Lerp(f.From.width * 0.8f, 56, e);
                var old = GUI.color;
                GUI.color = new Color(1, 1, 1, 1 - Mathf.Max(0, t - 0.8f) * 5);
                GameArt.DrawPart(new Rect(p.x - size / 2, p.y - size / 2, size, size), f.Id, Game.Catalog.TierOf(f.Id));
                GUI.color = old;
            }
        }

        private static readonly Color NightInk = new Color32(0x1e, 0x26, 0x4e, 0xff);
        private static readonly Color RibbonInk = new Color32(0x78, 0x1e, 0x14, 0xff);
        private static readonly Color ButtonInk = new Color32(0x96, 0x4c, 0x06, 0xff);

        // Where the open chest's glints twinkle, relative to the chest, and how big each one gets.
        private static readonly Vector2[] GlintAt =
        {
            new Vector2(-0.40f, -0.28f), new Vector2(0.36f, -0.36f), new Vector2(0.44f, 0.10f), new Vector2(-0.44f, 0.12f),
            new Vector2(0.02f, -0.46f), new Vector2(-0.20f, -0.42f), new Vector2(0.24f, 0.20f), new Vector2(-0.30f, 0.26f),
        };
        private static readonly float[] GlintSize = { 40, 30, 36, 26, 44, 24, 28, 32 };

        private void DrawRewardPanel(float w, float h)
        {
            var rw = _reward;
            Dim(w, h);
            var groups = rw.Parts.GroupBy(x => x).ToList();
            const int perRow = 3;
            float tile = 150, gap = 14, rowH = tile + 50;
            int rows = Mathf.Min(2, (groups.Count + perRow - 1) / perRow);
            bool more = groups.Count > perRow * 2;
            const float partsTop = 450;
            float ph = partsTop + rows * rowH + (more ? 44 : 0) + 150;
            float pw = Mathf.Min(620, w - 60);
            var r = new Rect((w - pw) / 2, (h - ph) / 2 + 16, pw, ph);
            GUI.Box(r, GUIContent.none, _nightPanel);
            float t = Time.time - rw.Since;
            const float shakeEnd = 0.45f, popEnd = 0.65f;
            var chestCenter = new Vector2(r.center.x, r.y + 260);

            // Golden rays turning slowly behind the chest; they swell in as the lid opens.
            if (GameArt.Rays != null)
            {
                float k = t < shakeEnd ? 0.35f : Mathf.Lerp(0.35f, 1, Mathf.Clamp01((t - shakeEnd) / 0.3f));
                float rs = Mathf.Min(r.width - 40, 580) * (0.75f + 0.25f * k);
                var m = GUI.matrix;
                GUIUtility.RotateAroundPivot(t * 8f, chestCenter);
                GUI.color = new Color(1, 1, 1, k);
                GUI.DrawTexture(new Rect(chestCenter.x - rs / 2, chestCenter.y - rs / 2, rs, rs), GameArt.Rays);
                GUI.color = Color.white;
                GUI.matrix = m;
            }

            // The title on a red ribbon over the top edge of the frame.
            string title = t < shakeEnd ? (rw.Chests > 1 ? $"Открываем {rw.Chests} сундука…" : "Открываем…")
                : rw.Chests > 1 ? $"Открыто сундуков: {rw.Chests}" : "Сундук открыт!";
            var rib = new Rect(r.center.x - 256, r.y - 44, 512, 128);
            if (GameArt.Ribbon != null)
            {
                GUI.DrawTexture(rib, GameArt.Ribbon);
                Outlined(new Rect(rib.x + 44, rib.y + 14, rib.width - 88, 82), title, _ribbonTitle, RibbonInk);
            }
            else GUI.Label(new Rect(r.x, r.y + 24, r.width, 50), title, _panelTitle);

            // 1) the closed chest shakes, 2) it pops open with a burst of glints, 3) the parts fly out to their stands.
            if (t < shakeEnd)
            {
                float k = t / shakeEnd;
                float cs = 280 + 20 * k;
                var cr = new Rect(chestCenter.x - cs / 2, chestCenter.y - cs / 2, cs, cs);
                var m = GUI.matrix;
                GUIUtility.RotateAroundPivot(Mathf.Sin(t * 45f) * 9f * k, chestCenter);
                GameArt.DrawChest(cr, new Chest(rw.Color, rw.Stars));
                GUI.matrix = m;
            }
            else
            {
                float k = Mathf.Clamp01((t - shakeEnd) / (popEnd - shakeEnd));
                float cs = 300 * (k < 1 ? 1 + 0.18f * Mathf.Sin(k * Mathf.PI) : 1);
                var cr = new Rect(chestCenter.x - cs / 2, chestCenter.y - cs / 2, cs, cs);
                var open = GameArt.ChestOpen(rw.Color);
                if (open != null) GUI.DrawTexture(cr, open, ScaleMode.ScaleToFit);
                else GameArt.DrawChest(cr, new Chest(rw.Color, 0));
            }
            if (Event.current.type == EventType.Repaint && t >= shakeEnd && rw.Sounded == 0) { rw.Sounded = 1; Sfx.Play("open"); }
            var sp = GameArt.Glint != null ? GameArt.Glint : GameArt.Sparkle;
            if (sp != null && t >= shakeEnd)
            {
                // A burst outward when the lid opens...
                float bt = (t - shakeEnd) / 0.7f;
                if (bt < 1)
                    for (int i = 0; i < 12; i++)
                    {
                        float ang = i * Mathf.PI * 2 / 12 + 0.3f;
                        float dist = 60 + 220 * (1 - (1 - bt) * (1 - bt));
                        float s = (i % 2 == 0 ? 46 : 32) * (1 - bt) + 12;
                        var c = chestCenter + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.8f) * dist;
                        GUI.color = new Color(1, 1, 1, 1 - bt);
                        GUI.DrawTexture(new Rect(c.x - s / 2, c.y - s / 2, s, s), sp);
                    }
                // ...then small glints that keep twinkling around the open chest, quick and out of step.
                float fadeIn = Mathf.Clamp01((t - popEnd) / 0.3f);
                for (int i = 0; i < GlintAt.Length; i++)
                {
                    float k = Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * (8f + i * 0.7f) + i * 1.9f)), 2);
                    float s = GlintSize[i] * (0.35f + 0.65f * k);
                    var c = chestCenter + GlintAt[i] * 300;
                    GUI.color = new Color(1, 1, 1, (0.25f + 0.75f * k) * fadeIn);
                    GUI.DrawTexture(new Rect(c.x - s / 2, c.y - s / 2, s, s), sp);
                }
                GUI.color = Color.white;
            }

            // What fell out: each part flies from the chest onto its glowing stand, one after another.
            float y0 = r.y + partsTop;
            _rewardCells.Clear();
            const float flyDur = 0.38f, stagger = 0.13f;
            float lastLand = popEnd;
            for (int i = 0; i < groups.Count && i < perRow * 2; i++)
            {
                int row = i / perRow, col = i % perRow;
                int inRow = Mathf.Min(perRow, groups.Count - row * perRow);
                float x = r.center.x - (inRow * tile + (inRow - 1) * gap) / 2 + col * (tile + gap);
                var cell = new Rect(x, y0 + row * rowH, tile, tile);
                string id = groups[i].Key;
                var iconRect = new Rect(cell.x + 14, cell.y - 6, tile - 28, tile - 28);
                _rewardCells.Add((id, iconRect));
                float ft = (t - popEnd - i * stagger) / flyDur;
                lastLand = popEnd + i * stagger + flyDur;
                if (ft <= 0) continue;
                float land = Mathf.Clamp01(ft);
                var plate = new Rect(cell.center.x - tile * 0.5f, iconRect.yMax - 24, tile, 38);
                if (GameArt.Plate != null)
                {
                    GUI.color = new Color(1, 1, 1, Mathf.Clamp01(ft * 3));
                    GUI.DrawTexture(Scale(plate, 0.6f + 0.4f * Mathf.Min(1, ft * 1.5f)), GameArt.Plate);
                    GUI.color = Color.white;
                }
                else
                {
                    GUI.color = new Color(1, 1, 1, Mathf.Clamp01(ft * 3));
                    GUI.Box(Scale(cell, 0.6f + 0.4f * Mathf.Min(1, ft * 1.5f)), GUIContent.none, _tile);
                    GUI.color = Color.white;
                }
                // the icon arcs up out of the chest and drops onto its stand with a small overshoot
                var from = chestCenter;
                var to = iconRect.center;
                var mid = new Vector2((from.x + to.x) / 2, Mathf.Min(from.y, to.y) - 120);
                float e = 1 - (1 - land) * (1 - land);
                var p = Vector2.Lerp(Vector2.Lerp(from, mid, e), Vector2.Lerp(mid, to, e), e);
                float size = iconRect.width * (land < 1 ? Mathf.Lerp(0.35f, 1, e) : 1);
                if (land >= 1) size = iconRect.width * (1 + 0.15f * Mathf.Sin(Mathf.Clamp01((ft - 1) * flyDur / 0.25f) * Mathf.PI));
                GameArt.DrawPart(new Rect(p.x - size / 2, p.y - size / 2, size, size), id, Game.Catalog.TierOf(id));
                if (land >= 1)
                {
                    if (Event.current.type == EventType.Repaint && rw.Sounded < 2 + i) { rw.Sounded = 2 + i; Sfx.Play("tick", 0.8f, 1f + 0.06f * i); }
                    if (groups[i].Count() > 1) GUI.Label(new Rect(cell.xMax - 50, cell.y - 10, 46, 46), "×" + groups[i].Count(), _badge);
                    Outlined(new Rect(cell.x - 10, plate.yMax + 2, tile + 20, 60), Game.Catalog.NameOf(id), _onNight, NightInk, 2f);
                }
            }
            if (more && t > lastLand)
                Outlined(new Rect(r.x + 30, y0 + 2 * rowH, r.width - 60, 40),
                    "и ещё: " + Describe(rw.Parts.Where(p => groups.Skip(perRow * 2).Any(g => g.Key == p)).ToList()), _onNight, NightInk, 2f);

            // «Забрать» shows up once everything has landed; a tap on the window before that skips the show.
            if (t > lastLand)
            {
                if (GoldButton(new Rect(r.x + 70, r.yMax - 136, r.width - 140, 96), "Забрать", 40)) Later(CloseReward, click: false);
            }
            else if (GUI.Button(r, GUIContent.none, _invisible)) rw.Since = Time.time - lastLand - 0.01f;
        }
    }
}
