using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using UnityEngine;

namespace Game.View
{
    /// <summary>
    /// The build screen: castle slots, chests, storage by tier, merging 2 → 1, moving to the next castle.
    /// Temporary IMGUI with the same flow as the prototype; the castle is drawn from its layer art when it has one.
    /// </summary>
    public sealed class BuildScreen : MonoBehaviour
    {
        private GameSession _s;
        private Action _toMine;
        private GameLogic Game => _s.Game;
        private GameState State => _s.State;

        private readonly List<string> _pick = new List<string>();
        private Vector2 _scroll;
        private string _message;
        private float _messageUntil;
        private bool _donePanel;
        // IMGUI must not change what it draws in the middle of an event, so clicks run on the next Update.
        private Action _pending;

        private void Later(Action a) => _pending += a;

        private void Update()
        {
            var p = _pending;
            _pending = null;
            p?.Invoke();
        }

        public void Init(GameSession session, Action toMine)
        {
            _s = session;
            _toMine = toMine;
        }

        // ---------- actions (public for tests) ----------

        /// <summary>Opens chests (all when index is null). Returns the part ids they gave.</summary>
        public List<string> OpenChests(int? index = null)
        {
            var got = new List<string>();
            var list = index.HasValue ? new List<int> { index.Value } : Enumerable.Range(0, State.Chests.Count).Reverse().ToList();
            int opened = 0, firstColor = 0, bestStars = 0;
            foreach (int i in list)
            {
                if (i < 0 || i >= State.Chests.Count) continue;
                var ch = State.Chests[i];
                State.Chests.RemoveAt(i);
                if (opened == 0 || ch.Stars > bestStars) { firstColor = ch.Color; bestStars = ch.Stars; }
                opened++;
                got.AddRange(Game.OpenChest(State, ch, _s.Rng));
            }
            _s.Save();
            if (got.Count > 0)
                _reward = new Reward { Color = firstColor, Chests = opened, Parts = got, Since = Time.time };
            return got;
        }

        public bool PlaceSlot(int slot)
        {
            var id = Game.CastleOf(State).Slots[slot];
            if (State.Filled[slot]) return false;
            if (!Game.Place(State, slot))
            {
                Say($"{Game.Catalog.NameOf(id)}: нет на складе. {WhereFrom(id)}");
                return false;
            }
            _s.Save();
            if (Game.IsComplete(State)) { State.Built++; _s.Save(); _donePanel = true; }
            return true;
        }

        /// <summary>Places every part that is in storage. Returns how many were placed.</summary>
        public int PlaceAll()
        {
            int n = 0;
            for (int i = 0; i < State.Filled.Length; i++)
                if (!State.Filled[i] && State.Count(Game.CastleOf(State).Slots[i]) > 0 && PlaceSlot(i)) n++;
            return n;
        }

        public bool Merge(string a, string b, string target)
        {
            if (!Game.Merge(State, a, b, target)) return false;
            _pick.Clear();
            _s.Save();
            Say($"Собрано: {Game.Catalog.NameOf(target)}");
            return true;
        }

        public void NextCastle()
        {
            if (Game.IsLastCastle(State)) _s.RestartDemo();
            else { Game.NextCastle(State); _s.Save(); }
            _donePanel = false;
            _pick.Clear();
        }

        // ---------- helpers ----------

        private string Describe(List<string> ids) =>
            string.Join(", ", ids.GroupBy(x => x).Select(g => Game.Catalog.NameOf(g.Key) + (g.Count() > 1 ? $" ×{g.Count()}" : "")));

        private string WhereFrom(string id)
        {
            int t = Game.Catalog.TierOf(id);
            var rules = Game.Rules;
            var ways = new List<string>();
            if (t > 1) ways.Add($"собрать из 2 деталей {t - 1}-го уровня");
            if (t <= rules.ColorTiers) ways.Add($"{Palette.BlockNames[t - 1]} сундук");
            if (t >= 6) ways.Add($"{Palette.BlockNames[rules.ColorTiers - 1]} сундук ★4–5");
            return "Где взять: " + string.Join(", ", ways) + ".";
        }

        private void Say(string text)
        {
            _message = text;
            _messageUntil = Time.time + 3.5f;
        }

        // ---------- temporary IMGUI ----------

        private GUIStyle _title, _label, _small, _button, _box;

        private void Styles()
        {
            if (_title != null) return;
            _label = new GUIStyle(GUI.skin.label) { fontSize = 24, wordWrap = true };
            _label.normal.textColor = new Color(0.25f, 0.18f, 0.1f);
            _title = new GUIStyle(_label) { fontSize = 32, fontStyle = FontStyle.Bold };
            _small = new GUIStyle(_label) { fontSize = 20 };
            _button = new GUIStyle(GUI.skin.button) { fontSize = 22, wordWrap = true };
            _box = new GUIStyle(GUI.skin.box);
            _button.imagePosition = ImagePosition.ImageLeft;
            var ink = new Color32(0x3a, 0x2a, 0x1c, 0xff);
            var frame = new Color32(0x8a, 0x6a, 0x48, 0xff);
            _panel = UiArt.Style(UiArt.Round(new Color32(0xf6, 0xec, 0xd8, 0xff), frame, 22, 5), 22, 1, ink);
            _tile = UiArt.Style(UiArt.Round(new Color32(0xff, 0xf8, 0xea, 0xff), new Color32(0xd9, 0xc4, 0xa0, 0xff), 18, 3), 18, 1, ink);
            _count = UiArt.Style(UiArt.Round(new Color32(0xd9, 0xa4, 0x41, 0xff), ink, 14, 2), 14, 20, ink);
            _count.fontStyle = FontStyle.Bold;
            _count.padding = new RectOffset(0, 0, 0, 0);
            _green = UiArt.Style(UiArt.Round(new Color32(0x4f, 0xb3, 0x5a, 0xff), new Color32(0x2f, 0x7d, 0x3a, 0xff), 20, 4), 20, 30, Color.white);
            _green.fontStyle = FontStyle.Bold;
            _chestBtn = new GUIStyle();
        }

        private GUIStyle _panel, _tile, _count, _green, _chestBtn;

        private bool ColorButton(string text, Color color, float width, float height = 64, bool enabled = true, Texture2D icon = null)
        {
            var bg = GUI.backgroundColor;
            GUI.backgroundColor = color;
            bool was = GUI.enabled;
            GUI.enabled = was && enabled;
            bool hit = GUILayout.Button(new GUIContent(text, icon), _button, GUILayout.Width(width), GUILayout.Height(height));
            GUI.enabled = was;
            GUI.backgroundColor = bg;
            return hit;
        }

        private void OnGUI()
        {
            if (_s == null) return;
            Styles();
            float scale = Mathf.Min(Screen.width / 720f, Screen.height / 1280f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float w = Screen.width / scale, h = Screen.height / scale;
            float inner = w - 60;

            // Behind an open window the page doesn't take clicks.
            GUI.enabled = _reward == null && !_donePanel;
            GUILayout.BeginArea(new Rect(20, 10, w - 40, h - 130));
            _scroll = GUILayout.BeginScrollView(_scroll);
            DrawHeader(inner);
            DrawCastle(inner);
            DrawNeeds();
            DrawChests(inner);
            DrawStorage(inner);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.enabled = true;

            // Bottom: to the mine.
            string mine = State.Run != null ? "Вернуться в шахту" : $"В шахту · билет ({State.Tickets})";
            GUI.backgroundColor = new Color(1f, 0.85f, 0.4f);
            if (GUI.Button(new Rect(40, h - 110, w - 80, 90), mine, _button) && _reward == null)
            {
                if (State.Run == null && State.Tickets < 1) Say("Билеты кончились. Новые придут завтра.");
                else Later(() => _toMine?.Invoke());
            }
            GUI.backgroundColor = Color.white;

            if (_message != null && Time.time < _messageUntil)
            {
                var r = new Rect(30, h - 260, w - 60, 130);
                GUI.color = new Color(0, 0, 0, 0.8f);
                GUI.Box(r, GUIContent.none, _box);
                GUI.color = Color.white;
                var st = new GUIStyle(_label) { alignment = TextAnchor.MiddleCenter };
                st.normal.textColor = Color.white;
                GUI.Label(new Rect(r.x + 15, r.y, r.width - 30, r.height), _message, st);
            }

            if (_reward != null) DrawRewardPanel(w, h);
            else if (_donePanel) DrawDonePanel(w, h);
        }

        private void DrawHeader(float inner)
        {
            var castle = Game.CastleOf(State);
            int done = State.Filled.Count(f => f), total = State.Filled.Length;
            GUILayout.Label($"Замок {State.CastleIndex + 1}: {castle.Name}", _title);
            GUILayout.Label($"Построено {done} из {total} · забегов: {State.Runs} · билеты: {State.Tickets} · метлы: {State.Brooms} · молотки: {State.Hammers}", _small);
            var r = GUILayoutUtility.GetRect(inner, 16);
            GUI.color = new Color(0, 0, 0, 0.15f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = new Color(0.31f, 0.7f, 0.35f);
            GUI.DrawTexture(new Rect(r.x, r.y, r.width * done / Mathf.Max(1, total), r.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUILayout.Space(10);
        }

        private CastleArt _art;
        private string _artId;

        /// <summary>Layered art of the current castle, or null when it has none yet.</summary>
        public CastleArt Art
        {
            get
            {
                var castle = Game.CastleOf(State);
                if (_artId != castle.Id) { _artId = castle.Id; _art = CastleArt.Load(castle); }
                return _art;
            }
        }

        // Castle picture (when the castle has art), then the slots as rows by tier, highest tier on top.
        private void DrawCastle(float inner)
        {
            var castle = Game.CastleOf(State);
            if (Art != null)
            {
                var pic = GUILayoutUtility.GetRect(inner, inner * 0.8f);
                // The castle stands in the meadow from bg_build.
                var meadow = GameArt.BuildBackground;
                if (meadow != null) GameArt.DrawCover(pic, meadow, 0.6f);
                Art.Draw(pic, State.Filled);
                GUILayout.Space(6);
            }
            var byTier = Enumerable.Range(0, castle.Slots.Length)
                .GroupBy(i => Game.Catalog.TierOf(castle.Slots[i]))
                .OrderByDescending(g => g.Key);
            const int perRow = 5;
            float cw = (inner - perRow * 8) / perRow;
            foreach (var g in byTier)
            {
                var slots = g.ToList();
                for (int start = 0; start < slots.Count; start += perRow)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    foreach (int i in slots.Skip(start).Take(perRow))
                    {
                        string id = castle.Slots[i];
                        bool filled = State.Filled[i];
                        bool have = !filled && State.Count(id) > 0;
                        string text = Game.Catalog.NameOf(id) + (filled ? " ✓" : have ? "\n+ поставить" : "");
                        var col = filled ? Palette.Tier(g.Key) : have ? Color.Lerp(Palette.Tier(g.Key), Color.white, 0.5f) : new Color(0.8f, 0.8f, 0.8f);
                        if (ColorButton(text, col, cw, 70, true, GameArt.Part(id)) && !filled) { int slot = i; Later(() => PlaceSlot(slot)); }
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
            }
            if (Enumerable.Range(0, castle.Slots.Length).Any(i => !State.Filled[i] && State.Count(castle.Slots[i]) > 0))
            {
                GUILayout.Space(6);
                if (ColorButton("Поставить всё, что есть", new Color(0.6f, 0.9f, 0.6f), inner, 56)) Later(() => PlaceAll());
            }
            GUILayout.Space(10);
        }

        private void DrawNeeds()
        {
            var missing = Game.Missing(State);
            string text = missing.Count == 0
                ? "Замку всего хватает: ставь детали со склада."
                : "Нужно замку: " + string.Join(", ", missing.Select(m => Game.Catalog.NameOf(m.Id).ToLowerInvariant() + (m.N > 1 ? $" ×{m.N}" : "")));
            GUILayout.Label(text, _label);
            GUILayout.Space(10);
        }

        private void DrawChests(float inner)
        {
            GUILayout.Label($"Сундуки · {State.Chests.Count}", _title);
            if (State.Chests.Count == 0) { GUILayout.Label("Пусто. Сундуки добываются в шахте.", _small); GUILayout.Space(10); return; }
            const int perRow = 6;
            float cw = (inner - perRow * 8) / perRow;
            for (int start = 0; start < State.Chests.Count; start += perRow)
            {
                GUILayout.BeginHorizontal();
                for (int i = start; i < Mathf.Min(start + perRow, State.Chests.Count); i++)
                {
                    var ch = State.Chests[i];
                    var cell = GUILayoutUtility.GetRect(cw, cw, GUILayout.Width(cw), GUILayout.Height(cw));
                    if (GUI.Button(cell, GUIContent.none, _chestBtn)) { int idx = i; Later(() => OpenChests(idx)); }
                    GameArt.DrawChest(cell, ch);
                }
                GUILayout.EndHorizontal();
            }
            if (ColorButton("Открыть все", new Color(1f, 0.85f, 0.4f), inner, 56)) Later(() => OpenChests());
            GUILayout.Space(10);
        }

        private void DrawStorage(float inner)
        {
            GUILayout.Label("Склад по уровням", _title);
            GUILayout.Label(_pick.Count == 0 ? "Тапни две детали одного уровня, чтобы собрать из них деталь следующего уровня."
                : _pick.Count == 1 ? $"Выбрано: {Game.Catalog.NameOf(_pick[0])}. Выбери вторую деталь того же уровня."
                : $"Выбрано: {Game.Catalog.NameOf(_pick[0])} + {Game.Catalog.NameOf(_pick[1])}. Во что собрать?", _small);
            var castle = Game.CastleOf(State);
            var rules = Game.Rules;
            const int perRow = 4;
            float cw = (inner - 170 - perRow * 8) / perRow;
            for (int t = 1; t <= rules.MaxTier; t++)
            {
                var parts = Game.Catalog.Parts.Where(p => p.Tier == t && State.Count(p.Id) > 0).Select(p => p.Id).ToList();
                GUILayout.BeginHorizontal();
                var badge = new GUIStyle(_small) { fontStyle = FontStyle.Bold };
                badge.normal.textColor = Palette.Tier(t);
                GUILayout.Label($"{t} · {Palette.TierNames[t]}", badge, GUILayout.Width(160));
                if (parts.Count == 0) GUILayout.Label("—", _small);
                foreach (var id in parts.Take(perRow))
                {
                    int free = State.Count(id) - _pick.Count(p => p == id);
                    int need = Math.Max(0, Game.Need(State, id));
                    bool picked = _pick.Contains(id);
                    string text = $"{Game.Catalog.NameOf(id)} {State.Count(id)}" + (need > 0 ? $" (−{need})" : "");
                    var col = picked ? Color.yellow : Color.Lerp(Palette.Tier(t), Color.white, 0.35f);
                    bool canAdd = t < rules.MaxTier && free > 0 && _pick.Count < 2 && (_pick.Count == 0 || Game.Catalog.TierOf(_pick[0]) == t);
                    if (ColorButton(text, col, cw, 56, canAdd || picked, GameArt.Part(id)))
                    {
                        string part = id;
                        // Tap adds the part (the same part twice is fine); tap again when it can't be added removes it.
                        if (canAdd) Later(() => _pick.Add(part)); else Later(() => _pick.Remove(part));
                    }
                }
                GUILayout.EndHorizontal();
            }

            if (_pick.Count == 2)
            {
                int next = Game.Catalog.TierOf(_pick[0]) + 1;
                GUILayout.BeginHorizontal();
                foreach (var target in castle.PartsOfTier(next))
                {
                    bool needed = Game.Need(State, target) > 0;
                    if (ColorButton(Game.Catalog.NameOf(target) + (needed ? " ★" : ""), Palette.Tier(next), (inner - 140) / Math.Max(1, castle.PartsOfTier(next).Count), 64, true, GameArt.Part(target)))
                    {
                        string a = _pick[0], b = _pick[1], to = target;
                        Later(() => Merge(a, b, to));
                    }
                }
                if (ColorButton("Отмена", Color.white, 120, 64)) Later(() => _pick.Clear());
                GUILayout.EndHorizontal();
            }
            if (_pick.Count == 1 && ColorButton("Отмена", Color.white, 160, 50)) Later(() => _pick.Clear());
        }

        private void DrawDonePanel(float w, float h)
        {
            GUI.color = new Color(0, 0, 0, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var r = new Rect(50, h / 2 - 280, w - 100, 560);
            GUI.Box(r, GUIContent.none, _box);
            GUI.Box(r, GUIContent.none, _box);
            GUILayout.BeginArea(new Rect(r.x + 30, r.y + 30, r.width - 60, r.height - 60));
            var white = new GUIStyle(_title); white.normal.textColor = Color.white;
            var body = new GUIStyle(_label); body.normal.textColor = Color.white;
            bool last = Game.IsLastCastle(State);
            GUILayout.Label($"{Game.CastleOf(State).Name}: построено!", white);
            GUILayout.Space(15);
            GUILayout.Label($"Забегов на этот замок: {State.Runs}." + (last ? " Это последний замок демоверсии." : ""), body);
            if (!last)
            {
                var bp = Game.PreviewBurn(State);
                var lost = new List<string>();
                if (bp.Parts > 0) lost.Add($"деталей: {bp.Parts}");
                if (bp.Chests > 0) lost.Add($"закрытых сундуков: {bp.Chests}");
                string kept = string.Join(", ", bp.Kept.Select(k => Game.Catalog.NameOf(k.Id).ToLowerInvariant() + (k.N > 1 ? $" ×{k.N}" : "")));
                GUILayout.Space(10);
                GUILayout.Label((lost.Count > 0 ? $"При переходе сгорят {string.Join(", ", lost)}." : "Склад пуст, сгорать нечему.")
                    + " " + (kept.Length > 0 ? $"Фигурки остаются: {kept}." : "С собой переходят только фигурки."), body);
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(last ? "Пройти демо заново" : "Следующий замок", _button, GUILayout.Height(80))) Later(NextCastle);
            GUILayout.EndArea();
        }

        /// <summary>What the last opening gave; shown in a window until "Забрать".</summary>
        private sealed class Reward
        {
            public int Color, Chests;
            public List<string> Parts;
            public float Since;
        }

        private Reward _reward;

        /// <summary>True while the window with what the chests gave is shown.</summary>
        public bool RewardPanel => _reward != null;

        public void CloseReward() => _reward = null;

        private void DrawRewardPanel(float w, float h)
        {
            var rw = _reward;
            GUI.color = new Color(0, 0, 0, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var groups = rw.Parts.GroupBy(x => x).ToList();
            const int perRow = 3;
            float tile = 150, gap = 14;
            int rows = Mathf.Min(2, (groups.Count + perRow - 1) / perRow);
            bool more = groups.Count > perRow * 2;
            float ph = 410 + rows * (tile + 70) + (more ? 44 : 0) + 130;
            float pw = Mathf.Min(620, w - 60);
            var r = new Rect((w - pw) / 2, (h - ph) / 2, pw, ph);
            GUI.Box(r, GUIContent.none, _panel);

            var title = new GUIStyle(_title) { alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(r.x, r.y + 24, r.width, 50), rw.Chests > 1 ? $"Открыто сундуков: {rw.Chests}" : "Сундук открыт!", title);

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
            var name = new GUIStyle(_small) { alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold };
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

        /// <summary>True while the "castle built" panel is shown.</summary>
        public bool CastleDonePanel => _donePanel;
    }
}
