using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using UnityEngine;

namespace Game.View
{
    /// <summary>
    /// Build screen drawing: a fixed scene on top (the meadow, the building site and the castle standing on it),
    /// and a sheet under it whose cards scroll: the next part, the building order, chests and storage.
    /// IMGUI in a 720-wide virtual screen with UiArt panels until the UI art is ready.
    /// </summary>
    public sealed partial class BuildScreen
    {
        private static readonly Color Ink = new Color32(0x3a, 0x2a, 0x1c, 0xff);
        private static readonly Color Brown = new Color32(0x8a, 0x5a, 0x34, 0xff);
        private static readonly Color CardFill = new Color32(0xfb, 0xf4, 0xe6, 0xf2);
        private static readonly Color CardEdge = new Color32(0xd9, 0xc4, 0xa0, 0xff);
        private static readonly Color Cream = new Color32(0xf6, 0xec, 0xd8, 0xff);
        private static readonly Color Frame = new Color32(0x8a, 0x6a, 0x48, 0xff);
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
            _wood, _green, _mineBtn, _toast, _panel, _panelTitle, _panelBody, _name, _invisible, _roundBtn;

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
            _sub.normal.textColor = new Color32(0x6e, 0x5a, 0x46, 0xff);
            _sheet = UiArt.Style(UiArt.Round(new Color32(0xfa, 0xf0, 0xdc, 0xff), new Color32(0xa0, 0x78, 0x50, 0xff), 32, 4), 32, 1, Ink);
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
            UiArt.UseGameFont(this);
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

            // The scene: meadow, building site and the castle. It never scrolls.
            float sceneH = Mathf.Round(Mathf.Clamp(h * 0.54f, 600, 840));
            float groundY = sceneH - 74;
            var bg = GameArt.BuildBackground;
            // The meadow's horizon (about 60% down the picture) sits a little behind the building site.
            if (bg != null) GameArt.DrawCoverAnchored(new Rect(0, 0, w, sceneH + 40), bg, 0.6f, groundY - 150);
            DrawScene(new Rect(x0, 0, colW, sceneH), groundY);

            // Behind an open window the page doesn't take clicks.
            GUI.enabled = _reward == null && !_donePanel && !_askStartOver;
            DrawHeader(new Rect(x0 + pad, 16, inner, 100));

            // The sheet under the scene; only its contents scroll.
            GUI.Box(new Rect(-8, sceneH - 26, w + 16, h - sceneH + 80), GUIContent.none, _sheet);
            GUI.DrawTexture(new Rect(w / 2 - 44, sceneH - 16, 88, 7), UiArt.Round(new Color32(0xc8, 0xaf, 0x8c, 0xff), new Color32(0xc8, 0xaf, 0x8c, 0xff), 3, 0));
            float bottom = h - 16 - 100 - 8;
            var view = new Rect(x0, sceneH - 4, colW, bottom - sceneH + 4);
            DragScroll(view);
            _scroll.y = Mathf.Clamp(_scroll.y, 0, Mathf.Max(0, _contentH - view.height));
            // Taps outside the sheet must not reach cards scrolled out of sight.
            var e = Event.current;
            var hidden = e.isMouse && !view.Contains(e.mousePosition) ? e.type : EventType.Ignore;
            if (hidden != EventType.Ignore) e.type = EventType.Ignore;
            GUI.BeginGroup(view);
            float y = 6 - _scroll.y;
            y = DrawNext(pad, y, inner);
            y = DrawOrder(pad, y + 12, inner);
            y = DrawChests(pad, y + 12, inner);
            y = DrawStorage(pad, y + 12, inner);
            _contentH = y + _scroll.y + 16;
            GUI.EndGroup();
            if (hidden != EventType.Ignore && e.type == EventType.Ignore) e.type = hidden;

            DrawMineButton(new Rect(x0 + pad, h - 16 - 100, inner, 100));
            GUI.enabled = true;

            if (_message != null && Time.time < _messageUntil)
                GUI.Box(new Rect(x0 + pad + 20, bottom - 118, inner - 40, 110), _message, _toast);

            if (_reward != null) DrawRewardPanel(w, h);
            else if (_donePanel) DrawDonePanel(w, h);
            else if (_askStartOver) DrawStartOverPanel(w, h);
        }

        // ---------- header ----------

        private void DrawHeader(Rect r)
        {
            var castle = Game.CastleOf(State);
            int done = State.Filled.Count(f => f), total = State.Filled.Length;
            const float progW = 150, btn = 64;
            if (GUI.Button(new Rect(r.x, r.y, btn, btn), GUIContent.none, _roundBtn)) Later(AskStartOver);
            GUI.DrawTexture(new Rect(r.x + 13, r.y + 13, btn - 26, btn - 26), UiArt.Restart, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(r.x + btn + 10, r.y, r.width - progW - btn - 22, 64), $"Замок {State.CastleIndex + 1}: {castle.Name}", _pill);
            var center = new GUIStyle(_pill) { alignment = TextAnchor.MiddleCenter, richText = true, padding = new RectOffset(4, 4, 4, 4) };
            GUI.Label(new Rect(r.xMax - progW, r.y, progW, 64), $"<color=#8a5a34>{done}</color> из {total}", center);

            var bar = new Rect(r.x, r.y + 76, r.width, 16);
            GUI.DrawTexture(bar, UiArt.Round(new Color(1, 1, 1, 0.75f), CardEdge, 8, 2));
            float fw = Mathf.Max(16, bar.width * done / Mathf.Max(1, total));
            if (done > 0) GUI.DrawTexture(new Rect(bar.x, bar.y, fw, bar.height), UiArt.Round(Green, GreenEdge, 8, 2));
        }

        // ---------- castle ----------

        private int _placedSlot = -1;
        private float _placedAt;

        /// <summary>The building site on the meadow and the castle standing on it, bottom at <paramref name="groundY"/>.</summary>
        private void DrawScene(Rect r, float groundY)
        {
            float size = Mathf.Min(r.width * 0.92f, (groundY - 104) / 0.97f);
            float cx = r.center.x, siteW = size * 0.8f, siteH = size * 0.13f;
            // Soft shadow, then a trodden dirt patch with a few stones.
            GUI.DrawTexture(new Rect(cx - siteW * 0.6f, groundY - siteH * 0.7f, siteW * 1.2f, siteH * 1.75f), UiArt.SoftDisc(new Color(0.15f, 0.1f, 0.05f, 0.45f)));
            GUI.DrawTexture(new Rect(cx - siteW / 2, groundY - siteH * 0.55f, siteW, siteH), UiArt.Disc(SiteFill, SiteEdge, 2.5f));
            GUI.DrawTexture(new Rect(cx - siteW * 0.45f, groundY - siteH * 0.47f, siteW * 0.9f, siteH * 0.72f), UiArt.Disc(SiteLight, SiteLight, 0));
            foreach (var (dx, dy) in Pebbles)
                GUI.DrawTexture(new Rect(cx + dx * siteW - 7, groundY + dy * siteH - 4, 14, 8), UiArt.Disc(SiteEdge, SiteEdge, 0));

            var pic = new Rect(cx - size / 2, groundY + siteH * 0.05f - size * 0.98f, size, size);
            int next = Game.NextSlot(State);
            if (Art != null)
            {
                float t = _placedSlot >= 0 ? Mathf.Clamp01((Time.time - _placedAt) / 0.35f) : 1;
                Art.DrawBuilt(pic, State.Filled, next, 0.55f + 0.45f * Mathf.Sin(Time.time * 4f), _placedSlot, t);
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
                if (GUI.Button(b, "Поставить", _green)) Later(() => PlacePart(id));
                if (row > 1 && GUI.Button(new Rect(b.x, b.yMax + 10, btnW, 50), $"Подряд · {row}", _wood)) Later(() => PlaceAll());
            }
            return card.yMax;
        }

        // ---------- building order ----------

        private float DrawOrder(float x, float y, float inner)
        {
            var slots = Game.CastleOf(State).Slots;
            int next = Game.NextSlot(State);
            const float padX = 14, cell = 42, gap = 4;
            float rowW = inner - 2 * padX;
            int perRow = Mathf.Max(1, Mathf.FloorToInt((rowW + gap) / (cell + gap)));
            int rows = (slots.Length + perRow - 1) / perRow;
            var card = new Rect(x, y, inner, 52 + rows * (cell + gap) + 8);
            GUI.Box(card, GUIContent.none, _card);
            GUI.Label(new Rect(x + padX, y + 10, 400, 30), "ПОРЯДОК СБОРКИ", _section);
            // Centre the rows of cells in the card.
            float x0 = x + (inner - (Mathf.Min(perRow, slots.Length) * (cell + gap) - gap)) / 2;
            for (int i = 0; i < slots.Length; i++)
            {
                var c = new Rect(x0 + (i % perRow) * (cell + gap), y + 48 + (i / perRow) * (cell + gap), cell, cell);
                bool done = State.Filled[i], isNext = i == next;
                GUI.DrawTexture(c, done ? UiArt.Round(HaveFill, GreenEdge, 10, 2)
                    : isNext ? UiArt.Round(new Color32(0xff, 0xec, 0xb0, 0xff), Gold, 10, 4)
                    : UiArt.Round(new Color32(0xf2, 0xe9, 0xda, 0xff), CardEdge, 10, 2));
                var old = GUI.color;
                if (!done && !isNext) GUI.color = new Color(1, 1, 1, 0.4f);
                GameArt.DrawPart(new Rect(c.x + 5, c.y + 5, cell - 10, cell - 10), slots[i], Game.Catalog.TierOf(slots[i]));
                GUI.color = old;
            }
            return card.yMax;
        }

        // ---------- chests ----------

        private float DrawChests(float x, float y, float inner)
        {
            const float padX = 16, padTop = 12, size = 92, gap = 8;
            int n = State.Chests.Count;
            float rowW = inner - 2 * padX;
            int perRow = Mathf.Max(1, Mathf.FloorToInt((rowW + gap) / (size + gap)));
            int rows = n == 0 ? 0 : (n + perRow - 1) / perRow;
            float hgt = padTop + 44 + (n == 0 ? 40 : rows * (size + gap)) + 10;
            var card = new Rect(x, y, inner, hgt);
            GUI.Box(card, GUIContent.none, _card);
            GUI.Label(new Rect(x + padX, y + padTop, 300, 36), n > 0 ? $"СУНДУКИ · {n}" : "СУНДУКИ", _section);
            if (n > 0 && GUI.Button(new Rect(card.xMax - padX - 190, y + padTop - 2, 190, 44), "Открыть все", _wood)) Later(() => OpenChests());
            float top = y + padTop + 48;
            if (n == 0)
            {
                GUI.Label(new Rect(x + padX, top, rowW, 36), "Пусто. Сундуки добываются в шахте.", _text);
                return card.yMax;
            }
            for (int i = 0; i < n; i++)
            {
                var cell = new Rect(x + padX + (i % perRow) * (size + gap), top + (i / perRow) * (size + gap), size, size);
                if (GUI.Button(cell, GUIContent.none, _invisible)) { int idx = i; Later(() => OpenChests(idx)); }
                GameArt.DrawChest(cell, State.Chests[i]);
            }
            return card.yMax;
        }

        // ---------- storage ----------

        private float DrawStorage(float x, float y, float inner)
        {
            const float padX = 16, padTop = 12, tile = 104, gap = 10, nameH = 40;
            var castle = Game.CastleOf(State);
            var rules = Game.Rules;
            var parts = Game.Catalog.Parts.Where(p => State.Count(p.Id) > 0).OrderBy(p => p.Tier).Select(p => p.Id).ToList();
            float rowW = inner - 2 * padX;
            int perRow = Mathf.Max(1, Mathf.FloorToInt((rowW + gap) / (tile + gap)));
            int rows = (parts.Count + perRow - 1) / perRow;
            bool choosing = _pick.Count == 2;
            var targets = choosing ? castle.PartsOfTier(Game.Catalog.TierOf(_pick[0]) + 1) : null;
            string hint = parts.Count == 0 ? "Пусто. Детали выпадают из сундуков."
                : _pick.Count == 0 ? "Тапни две детали одного уровня, чтобы собрать из них деталь уровнем выше."
                : _pick.Count == 1 ? $"Выбрано: {Game.Catalog.NameOf(_pick[0])}. Выбери вторую того же уровня."
                : "Во что собрать?";
            float hintH = _text.CalcHeight(new GUIContent(hint), rowW);
            float hgt = padTop + 44 + hintH + 10 + rows * (tile + nameH + gap) + (choosing ? tile + nameH + 20 : 0) + 10;
            var card = new Rect(x, y, inner, hgt);
            GUI.Box(card, GUIContent.none, _card);
            GUI.Label(new Rect(x + padX, y + padTop, 300, 36), "СКЛАД", _section);
            if (_pick.Count > 0 && GUI.Button(new Rect(card.xMax - padX - 150, y + padTop - 2, 150, 44), "Отмена", _wood)) Later(() => _pick.Clear());
            float top = y + padTop + 44;
            GUI.Label(new Rect(x + padX, top, rowW, hintH), hint, _text);
            top += hintH + 10;

            for (int i = 0; i < parts.Count; i++)
            {
                string id = parts[i];
                int t = Game.Catalog.TierOf(id);
                int free = State.Count(id) - _pick.Count(p => p == id);
                bool picked = _pick.Contains(id);
                bool canAdd = t < rules.MaxTier && free > 0 && _pick.Count < 2 && (_pick.Count == 0 || Game.Catalog.TierOf(_pick[0]) == t);
                var cell = new Rect(x + padX + (i % perRow) * (tile + gap), top + (i / perRow) * (tile + nameH + gap), tile, tile);
                var old = GUI.color;
                if (_pick.Count > 0 && !canAdd && !picked) GUI.color = new Color(1, 1, 1, 0.45f);
                if (GUI.Button(cell, GUIContent.none, picked ? _tilePicked : _tile) && (canAdd || picked))
                {
                    string part = id;
                    // Tap adds the part (the same part twice is fine); tap again when it can't be added removes it.
                    if (canAdd) Later(() => _pick.Add(part)); else Later(() => _pick.Remove(part));
                }
                GameArt.DrawPart(new Rect(cell.x + 12, cell.y + 10, tile - 24, tile - 24), id, t);
                GUI.color = old;
                GUI.Label(new Rect(cell.xMax - 40, cell.yMax - 30, 38, 28), State.Count(id).ToString(), _count);
                int need = Math.Max(0, Game.Need(State, id));
                if (need > 0) GUI.Label(new Rect(cell.x + 2, cell.y + 2, 40, 26), "−" + need, _need);
                GUI.Label(new Rect(cell.x - 6, cell.yMax + 2, tile + 12, nameH), Game.Catalog.NameOf(id), _name);
            }

            if (choosing)
            {
                float ty = top + rows * (tile + nameH + gap) + 10;
                float tx = x + padX;
                foreach (var target in targets)
                {
                    var cell = new Rect(tx, ty, tile, tile);
                    bool needed = Game.Need(State, target) > 0;
                    if (GUI.Button(cell, GUIContent.none, needed ? _tilePicked : _tile))
                    {
                        string a = _pick[0], b = _pick[1], to = target;
                        Later(() => Merge(a, b, to));
                    }
                    GameArt.DrawPart(new Rect(cell.x + 12, cell.y + 10, tile - 24, tile - 24), target, Game.Catalog.TierOf(target));
                    GUI.Label(new Rect(cell.x - 6, cell.yMax + 2, tile + 12, nameH), Game.Catalog.NameOf(target) + (needed ? " ★" : ""), _name);
                    tx += tile + gap;
                }
            }
            return card.yMax;
        }

        // ---------- to the mine ----------

        private void DrawMineButton(Rect r)
        {
            string text = State.Run != null ? $"Продолжить · {State.Run.Moves} ход."
                : State.Tickets > 0 ? $"Добывать · {State.Opts.Moves} ходов  <size=22>билетов: {State.Tickets}</size>"
                : "Билеты кончились, новые завтра";
            if (GUI.Button(r, text, _mineBtn) && _reward == null)
            {
                if (State.Run == null && State.Tickets < 1) Say("Билеты кончились. Новые придут завтра.");
                else Later(() => _toMine?.Invoke());
            }
        }

        // ---------- windows ----------

        private static void Dim(float w, float h)
        {
            GUI.color = new Color(0, 0, 0, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawDonePanel(float w, float h)
        {
            Dim(w, h);
            float pw = Mathf.Min(620, w - 60);
            var r = new Rect((w - pw) / 2, h / 2 - 330, pw, 660);
            GUI.Box(r, GUIContent.none, _panel);
            GUILayout.BeginArea(new Rect(r.x + 36, r.y + 36, r.width - 72, r.height - 72));
            bool last = Game.IsLastCastle(State);
            GUILayout.Label($"{Game.CastleOf(State).Name}: построено!", _panelTitle);
            GUILayout.Space(12);
            var full = Art?.Full;
            if (full != null) GUILayout.Label(full, new GUIStyle { alignment = TextAnchor.MiddleCenter }, GUILayout.Height(220));
            GUILayout.Space(12);
            GUILayout.Label($"Забегов на этот замок: {State.Runs}." + (last ? " Это последний замок демоверсии." : ""), _panelBody);
            if (!last)
            {
                var bp = Game.PreviewBurn(State);
                var lost = new List<string>();
                if (bp.Parts > 0) lost.Add($"деталей: {bp.Parts}");
                if (bp.Chests > 0) lost.Add($"закрытых сундуков: {bp.Chests}");
                string kept = string.Join(", ", bp.Kept.Select(k => Game.Catalog.NameOf(k.Id).ToLowerInvariant() + (k.N > 1 ? $" ×{k.N}" : "")));
                GUILayout.Space(8);
                GUILayout.Label((lost.Count > 0 ? $"При переходе сгорят {string.Join(", ", lost)}." : "Склад пуст, сгорать нечему.")
                    + " " + (kept.Length > 0 ? $"Фигурки остаются: {kept}." : "С собой переходят только фигурки."), _panelBody);
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(last ? "Пройти демо заново" : "Следующий замок", _green, GUILayout.Height(84))) Later(NextCastle);
            GUILayout.EndArea();
        }

        private void DrawStartOverPanel(float w, float h)
        {
            Dim(w, h);
            float pw = Mathf.Min(620, w - 60);
            var r = new Rect((w - pw) / 2, h / 2 - 220, pw, 440);
            GUI.Box(r, GUIContent.none, _panel);
            GUI.Label(new Rect(r.x + 36, r.y + 36, r.width - 72, 60), "Начать сначала?", _panelTitle);
            GUI.Label(new Rect(r.x + 36, r.y + 110, r.width - 72, 120),
                "Весь прогресс сотрётся: замки, склад и сундуки. Игра начнётся с первого замка.", _panelBody);
            if (GUI.Button(new Rect(r.x + 40, r.yMax - 210, r.width - 80, 84), "Начать сначала", _mineBtn)) Later(StartOver);
            if (GUI.Button(new Rect(r.x + 40, r.yMax - 112, r.width - 80, 76), "Отмена", _wood)) Later(() => _askStartOver = false);
        }

        private void DrawRewardPanel(float w, float h)
        {
            var rw = _reward;
            Dim(w, h);
            var groups = rw.Parts.GroupBy(x => x).ToList();
            const int perRow = 3;
            float tile = 150, gap = 14;
            int rows = Mathf.Min(2, (groups.Count + perRow - 1) / perRow);
            bool more = groups.Count > perRow * 2;
            float ph = 410 + rows * (tile + 70) + (more ? 44 : 0) + 130;
            float pw = Mathf.Min(620, w - 60);
            var r = new Rect((w - pw) / 2, (h - ph) / 2, pw, ph);
            GUI.Box(r, GUIContent.none, _panel);
            GUI.Label(new Rect(r.x, r.y + 24, r.width, 50), rw.Chests > 1 ? $"Открыто сундуков: {rw.Chests}" : "Сундук открыт!", _panelTitle);

            // Open chest with a short bounce and twinkling sparkles around it.
            float t = Time.time - rw.Since;
            float bounce = t < 0.4f ? 1 + 0.12f * Mathf.Sin(t / 0.4f * Mathf.PI) : 1;
            float cs = 300 * bounce;
            var chestRect = new Rect(r.center.x - cs / 2, r.y + 90 + (300 - cs) / 2, cs, cs);
            var open = GameArt.ChestOpen(rw.Color);
            if (open != null) GUI.DrawTexture(chestRect, open, ScaleMode.ScaleToFit);
            else GameArt.DrawChest(chestRect, new Chest(rw.Color, 0));
            var sp = GameArt.Sparkle;
            if (sp != null)
            {
                Vector2[] at = { new Vector2(-0.42f, -0.30f), new Vector2(0.40f, -0.38f), new Vector2(0.47f, 0.12f), new Vector2(-0.48f, 0.15f), new Vector2(0.02f, -0.50f) };
                for (int i = 0; i < at.Length; i++)
                {
                    float k = 0.5f + 0.5f * Mathf.Sin(t * 4f + i * 1.7f);
                    float s = 36 + 26 * k;
                    var c = new Vector2(r.center.x + at[i].x * 300, r.y + 240 + at[i].y * 300);
                    GUI.color = new Color(1, 1, 1, 0.35f + 0.65f * k);
                    GUI.DrawTexture(new Rect(c.x - s / 2, c.y - s / 2, s, s), sp);
                }
                GUI.color = Color.white;
            }

            // What fell out: icon, name, count.
            float y0 = r.y + 410;
            var name = new GUIStyle(_name) { fontSize = 20 };
            for (int i = 0; i < groups.Count && i < perRow * 2; i++)
            {
                int row = i / perRow, col = i % perRow;
                int inRow = Mathf.Min(perRow, groups.Count - row * perRow);
                float x = r.center.x - (inRow * tile + (inRow - 1) * gap) / 2 + col * (tile + gap);
                var cell = new Rect(x, y0 + row * (tile + 70), tile, tile);
                GUI.Box(cell, GUIContent.none, _tile);
                string id = groups[i].Key;
                GameArt.DrawPart(new Rect(cell.x + 14, cell.y + 10, tile - 28, tile - 28), id, Game.Catalog.TierOf(id));
                if (groups[i].Count() > 1) GUI.Label(new Rect(cell.xMax - 56, cell.yMax - 40, 50, 34), "×" + groups[i].Count(), _count);
                GUI.Label(new Rect(cell.x - 10, cell.yMax + 4, tile + 20, 60), Game.Catalog.NameOf(id), name);
            }
            if (more)
                GUI.Label(new Rect(r.x, y0 + 2 * (tile + 70), r.width, 40), "и ещё: " + Describe(rw.Parts.Where(p => groups.Skip(perRow * 2).Any(g => g.Key == p)).ToList()), new GUIStyle(name) { alignment = TextAnchor.MiddleCenter });

            if (GUI.Button(new Rect(r.x + 40, r.yMax - 116, r.width - 80, 84), "Забрать", _green)) Later(CloseReward);
        }
    }
}
