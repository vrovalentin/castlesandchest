using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using UnityEngine;

namespace Game.View
{
    /// <summary>
    /// Build screen drawing, laid out like the wireframe: title and progress on top, the castle on the meadow,
    /// «Осталось поставить», chests, storage, and the big button to the mine at the bottom.
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

        private GUIStyle _pill, _card, _section, _text, _chip, _chipHave, _tile, _tilePicked, _count, _need,
            _wood, _green, _mineBtn, _toast, _panel, _panelTitle, _panelBody, _name, _invisible;

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
            _chip = UiArt.Style(UiArt.Round(Color.white, CardEdge, 16, 2), 16, 19, Ink, TextAnchor.MiddleLeft);
            _chip.wordWrap = false;
            _chip.padding = new RectOffset(42, 12, 2, 2);
            _chipHave = new GUIStyle(_chip);
            _chipHave.normal.background = UiArt.Round(HaveFill, Green, 16, 3);
            _chipHave.active.background = UiArt.Round(Green, GreenEdge, 16, 3);
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
            _green = UiArt.Style(UiArt.Round(Green, GreenEdge, 20, 4), 20, 26, Color.white);
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
        }

        private void OnGUI()
        {
            if (_s == null) return;
            Styles();
            float scale = Mathf.Min(Screen.width / 720f, Screen.height / 1280f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float w = Screen.width / scale, h = Screen.height / scale;
            float colW = Mathf.Min(w, 760), x0 = (w - colW) / 2, pad = 16, inner = colW - 2 * pad;

            var bg = GameArt.BuildBackground;
            // The meadow's horizon (about 60% down the picture) goes under the castle's base.
            if (bg != null) GameArt.DrawCoverAnchored(new Rect(0, 0, w, h), bg, 0.6f, 128 + inner * 0.8f * 0.86f);

            // Behind an open window the page doesn't take clicks.
            GUI.enabled = _reward == null && !_donePanel;
            DrawHeader(new Rect(x0 + pad, 16, inner, 100));

            float top = 128, bottom = h - 16 - 100 - 12;
            var view = new Rect(x0, top, colW, bottom - top);
            _scroll = GUI.BeginScrollView(view, _scroll, new Rect(0, 0, colW - 20, _contentH), false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
            float y = 0;
            y = DrawCastle(pad, y, inner);
            y = DrawTodo(pad, y + 12, inner);
            y = DrawChests(pad, y + 12, inner);
            y = DrawStorage(pad, y + 12, inner);
            _contentH = y + 16;
            GUI.EndScrollView();

            DrawMineButton(new Rect(x0 + pad, h - 16 - 100, inner, 100));
            GUI.enabled = true;

            if (_message != null && Time.time < _messageUntil)
                GUI.Box(new Rect(x0 + pad + 20, bottom - 120, inner - 40, 110), _message, _toast);

            if (_reward != null) DrawRewardPanel(w, h);
            else if (_donePanel) DrawDonePanel(w, h);
        }

        // ---------- header ----------

        private void DrawHeader(Rect r)
        {
            var castle = Game.CastleOf(State);
            int done = State.Filled.Count(f => f), total = State.Filled.Length;
            const float progW = 150;
            GUI.Label(new Rect(r.x, r.y, r.width - progW - 12, 64), $"Замок {State.CastleIndex + 1}: {castle.Name}", _pill);
            var center = new GUIStyle(_pill) { alignment = TextAnchor.MiddleCenter, richText = true, padding = new RectOffset(4, 4, 4, 4) };
            GUI.Label(new Rect(r.xMax - progW, r.y, progW, 64), $"<color=#8a5a34>{done}</color> из {total}", center);

            var bar = new Rect(r.x, r.y + 76, r.width, 16);
            GUI.DrawTexture(bar, UiArt.Round(new Color(1, 1, 1, 0.75f), CardEdge, 8, 2));
            float fw = Mathf.Max(16, bar.width * done / Mathf.Max(1, total));
            if (done > 0) GUI.DrawTexture(new Rect(bar.x, bar.y, fw, bar.height), UiArt.Round(Green, GreenEdge, 8, 2));
        }

        // ---------- castle ----------

        private float DrawCastle(float x, float y, float inner)
        {
            var pic = new Rect(x, y, inner, inner * 0.8f);
            if (Art != null) Art.Draw(pic, State.Filled);
            else
            {
                // No art for this castle yet: the slots as a grid of icons, filled ones solid.
                var castle = Game.CastleOf(State);
                int perRow = 6;
                float cell = (inner - 40) / perRow;
                var back = new Rect(x, y, inner, Mathf.Ceil(castle.Slots.Length / (float)perRow) * cell + 40);
                GUI.Box(back, GUIContent.none, _card);
                for (int i = 0; i < castle.Slots.Length; i++)
                {
                    var c = new Rect(x + 20 + (i % perRow) * cell, y + 20 + (i / perRow) * cell, cell - 6, cell - 6);
                    var old = GUI.color;
                    if (!State.Filled[i]) GUI.color = new Color(1, 1, 1, 0.3f);
                    GameArt.DrawPart(c, castle.Slots[i], Game.Catalog.TierOf(castle.Slots[i]));
                    GUI.color = old;
                }
                return back.yMax;
            }
            return pic.yMax;
        }

        // ---------- what is left to place ----------

        /// <summary>Places one part of this kind on its first empty slot. False if none is in storage.</summary>
        public bool PlacePart(string id)
        {
            var slots = Game.CastleOf(State).Slots;
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] == id && !State.Filled[i]) return PlaceSlot(i);
            return false;
        }

        private float DrawTodo(float x, float y, float inner)
        {
            var slots = Game.CastleOf(State).Slots;
            var left = new List<(string id, int n)>();
            foreach (var g in Enumerable.Range(0, slots.Length).Where(i => !State.Filled[i]).GroupBy(i => slots[i]))
                left.Add((g.Key, g.Count()));
            if (left.Count == 0) return y - 12;
            int ready = Enumerable.Range(0, slots.Length).Count(i => !State.Filled[i] && State.Count(slots[i]) > 0);

            const float ch = 40, gap = 8, padX = 16, padTop = 12;
            float rowW = inner - 2 * padX;
            var title = new GUIContent("ОСТАЛОСЬ ПОСТАВИТЬ");
            var items = left.Select(l => new GUIContent(Game.Catalog.NameOf(l.id).ToLowerInvariant() + (l.n > 1 ? $" ×{l.n}" : ""))).ToList();
            var sizes = items.Select(c => _chip.CalcSize(c).x).ToList();
            int rows = FlowRows(sizes, rowW, gap);
            float hgt = padTop + 30 + rows * (ch + gap) + (ready > 0 ? 64 : 0) + 8;
            var card = new Rect(x, y, inner, hgt);
            GUI.Box(card, GUIContent.none, _card);
            GUI.Label(new Rect(x + padX, y + padTop, rowW, 26), title, _section);

            float cx = 0, cy = y + padTop + 32;
            for (int i = 0; i < items.Count; i++)
            {
                if (cx + sizes[i] > rowW && cx > 0) { cx = 0; cy += ch + gap; }
                var r = new Rect(x + padX + cx, cy, Mathf.Min(sizes[i], rowW), ch);
                bool have = State.Count(left[i].id) > 0;
                if (GUI.Button(r, items[i], have ? _chipHave : _chip) && have) { string id = left[i].id; Later(() => PlacePart(id)); }
                GameArt.DrawPart(new Rect(r.x + 6, r.y + 3, 34, 34), left[i].id, Game.Catalog.TierOf(left[i].id));
                cx += sizes[i] + gap;
            }
            if (ready > 0 && GUI.Button(new Rect(x + padX, card.yMax - 70, rowW, 58), $"Поставить всё со склада · {ready}", _green))
                Later(() => PlaceAll());
            return card.yMax;
        }

        private static int FlowRows(List<float> sizes, float width, float gap)
        {
            float x = 0;
            int rows = sizes.Count > 0 ? 1 : 0;
            foreach (float s in sizes)
            {
                if (x + s > width && x > 0) { x = 0; rows++; }
                x += s + gap;
            }
            return rows;
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
