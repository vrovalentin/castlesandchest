using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.View
{
    /// <summary>
    /// Mine HUD, laid out like the wireframe: top bar (back, moves, chests, start over), «Нужно замку»,
    /// broom and hammer, the board, and the «Добыто» panel with the chests that fell.
    /// IMGUI in a 720 × 1280 virtual screen; panels and icons come from UiArt until the UI art is ready.
    /// </summary>
    public sealed partial class MineController
    {
        public struct HudRects
        {
            public Rect Bar, Needs, Tools, Board, Got;
        }

        private static readonly Color Cream = new Color32(0xf0, 0xe6, 0xd6, 0xff);
        private static readonly Color Gold = new Color32(0xd9, 0xa4, 0x41, 0xff);
        private static readonly Color PanelFill = new Color32(0x3b, 0x2d, 0x21, 0xff);
        private static readonly Color PanelEdge = new Color32(0x6b, 0x50, 0x38, 0xff);
        private static readonly Color ToolFill = new Color32(0x5c, 0x45, 0x32, 0xff);
        private static readonly Color ToolOn = new Color32(0x7a, 0x5a, 0x3a, 0xff);
        private static readonly Color Parchment = new Color32(0xf6, 0xec, 0xd8, 0xff);
        private static readonly Color Frame = new Color32(0x8a, 0x6a, 0x48, 0xff);
        private static readonly Color Green = new Color32(0x4f, 0xb3, 0x5a, 0xff);
        private static readonly Color GreenEdge = new Color32(0x2f, 0x7d, 0x3a, 0xff);
        private static readonly Color Sand = new Color32(0xe3, 0xc9, 0x9c, 0xff);

        private GUIStyle _pill, _roundBtn, _needsBox, _needsTitle, _chip, _tool, _toolOn, _badge, _hint,
            _gotBox, _gotTitle, _gotEmpty, _toastStyle, _dim, _panel, _panelTitle, _panelBody, _button, _button2;

        private readonly List<float> _gotAt = new List<float>();

        /// <summary>Screen pixels per virtual HUD pixel.</summary>
        private static float HudScale => Mathf.Max(0.01f, Mathf.Min(Screen.width / 720f, Screen.height / 1280f));

        // Height of «Нужно замку»: grows with the chip rows it needs (see DrawNeeds).
        private float _needsHeight = NeedsHeight(2);
        private static float NeedsHeight(int rows) => 20 + rows * 30 + (rows - 1) * 6;

        /// <summary>
        /// Where each HUD block goes, in virtual pixels (GUI coordinates, y down). The board takes the
        /// largest size that fits; «Добыто» sits right under it and takes the rest of the screen.
        /// </summary>
        public HudRects Layout()
        {
            float s = HudScale, w = Screen.width / s, h = Screen.height / s;
            float colW = Mathf.Min(w, 760f), x0 = (w - colW) / 2f, pad = 16f, inner = colW - 2 * pad;
            const float gotMin = 150, gap = 12;
            var r = new HudRects
            {
                Bar = new Rect(x0 + pad, 16, inner, 76),
                Needs = new Rect(x0 + pad, 104, inner, _needsHeight),
            };
            r.Tools = new Rect(x0 + pad, r.Needs.yMax + 10, inner, 72);
            float top = r.Tools.yMax + gap;
            float maxH = Mathf.Max(100, h - pad - gotMin - gap - top);
            var b = State?.Run?.Board;
            float aspect = b != null ? (b.Cols + 0.3f) / (b.Rows + 0.3f) : 1f;
            float bh = Mathf.Min(maxH, inner / aspect);
            r.Board = new Rect(x0 + pad, top, inner, bh);
            r.Got = new Rect(x0 + pad, r.Board.yMax + gap, inner, Mathf.Max(gotMin, h - pad - r.Board.yMax - gap));
            return r;
        }

        private void Styles()
        {
            if (_button != null) return;
            _pill = UiArt.Style(UiArt.Round(Cream, Cream, 30, 0), 30, 26, UiArt.Ink);
            _pill.fontStyle = FontStyle.Bold;
            _roundBtn = UiArt.Style(UiArt.Round(Cream, Frame, 22, 3), 22, 1, UiArt.Ink);
            _roundBtn.active.background = UiArt.Round(Sand, Frame, 22, 3);
            _needsBox = UiArt.Style(UiArt.Round(PanelFill, Gold, 16, 3), 16, 1, Cream);
            _needsTitle = new GUIStyle { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            _needsTitle.normal.textColor = Gold;
            _chip = UiArt.Style(UiArt.Round(Cream, Cream, 14, 0), 14, 19, UiArt.Ink, TextAnchor.MiddleLeft);
            _chip.wordWrap = false;
            _chip.padding = new RectOffset(40, 12, 2, 2);
            _tool = UiArt.Style(UiArt.Round(ToolFill, PanelEdge, 18, 3), 18, 22, Cream, TextAnchor.MiddleLeft);
            _tool.padding = new RectOffset(70, 8, 4, 4);
            _tool.fontStyle = FontStyle.Bold;
            _tool.active.background = UiArt.Round(ToolOn, PanelEdge, 18, 3);
            _tool.active.textColor = Cream;
            _toolOn = new GUIStyle(_tool);
            _toolOn.normal.background = UiArt.Round(ToolOn, Gold, 18, 5);
            _badge = UiArt.Style(UiArt.Round(Gold, UiArt.Ink, 15, 2), 15, 18, UiArt.Ink);
            _badge.fontStyle = FontStyle.Bold;
            _badge.padding = new RectOffset(0, 0, 0, 0);
            _hint = new GUIStyle { fontSize = 19, wordWrap = true, alignment = TextAnchor.MiddleLeft };
            _hint.normal.textColor = Cream;
            _gotBox = UiArt.Style(UiArt.Round(PanelFill, PanelEdge, 18, 3), 18, 1, Cream);
            _gotTitle = new GUIStyle(_needsTitle) { fontSize = 20 };
            _gotTitle.normal.textColor = Cream;
            _gotEmpty = new GUIStyle(_hint) { alignment = TextAnchor.MiddleCenter };
            _gotEmpty.normal.textColor = new Color(Cream.r, Cream.g, Cream.b, 0.7f);
            _toastStyle = UiArt.Style(UiArt.Round(new Color(0, 0, 0, 0.82f), new Color(0, 0, 0, 0.82f), 18, 0), 18, 24, Color.white);
            _dim = new GUIStyle { normal = { background = Texture2D.whiteTexture } };
            _panel = UiArt.Style(UiArt.Round(Parchment, Frame, 22, 5), 22, 1, UiArt.Ink);
            _panelTitle = new GUIStyle { fontSize = 34, fontStyle = FontStyle.Bold, wordWrap = true, alignment = TextAnchor.MiddleCenter };
            _panelTitle.normal.textColor = UiArt.Ink;
            _panelBody = new GUIStyle(_panelTitle) { fontSize = 24, fontStyle = FontStyle.Normal };
            _button = UiArt.Style(UiArt.Round(Green, GreenEdge, 20, 4), 20, 26, Color.white);
            _button.fontStyle = FontStyle.Bold;
            _button.active.background = UiArt.Round(GreenEdge, GreenEdge, 20, 4);
            _button.active.textColor = Color.white;
            _button2 = UiArt.Style(UiArt.Round(Sand, Frame, 20, 4), 20, 26, UiArt.Ink);
            _button2.fontStyle = FontStyle.Bold;
            _button2.active.background = UiArt.Round(Frame, Frame, 20, 4);
            _button2.active.textColor = Cream;
            UiArt.UseGameFont(this);
        }

        private void OnGUI()
        {
            if (_s == null) return;
            Styles();
            float s = HudScale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1));
            float w = Screen.width / s, h = Screen.height / s;
            var L = Layout();

            var run = State.Run;
            if (run != null)
            {
                GUI.enabled = !Busy && _mode == Mode.Play;
                DrawBar(L.Bar, run);
                DrawNeeds(L.Needs);
                DrawTools(L.Tools);
                GUI.enabled = true;
                DrawGot(L.Got, run.Got);
            }

            if (_toast != null && Time.time < _toastUntil)
                GUI.Box(new Rect(L.Board.x + 24, L.Board.y + 16, L.Board.width - 48, 84), _toast, _toastStyle);

            if (_mode == Mode.OutOfMoves) Panel(w, h, "Ходы кончились", OutOfMovesText(), OutOfMovesButtons);
            else if (_mode == Mode.Summary) Panel(w, h, $"Добыто сундуков: {_summary.Got.Count * (_doubled ? 2 : 1)}", SummaryText(), SummaryButtons);
            else if (_mode == Mode.ConfirmRestart) Panel(w, h, "Начать заново?", RestartText(), RestartButtons);
            else if (_mode == Mode.NoTickets) Panel(w, h, "Билеты кончились", "Новые билеты придут завтра.", () =>
            {
                if (_toBuild != null && GUILayout.Button("На Стройку", _button, GUILayout.Height(76))) _toBuild();
            });
        }

        // ---------- top bar ----------

        private void DrawBar(Rect r, RunState run)
        {
            float b = r.height;
            if (_toBuild != null && IconButton(new Rect(r.x, r.y, b, b), UiArt.Back)) _toBuild();
            if (IconButton(new Rect(r.xMax - b, r.y, b, b), UiArt.Restart)) AskRestart();

            const float movesW = 200, chestsW = 236, gap = 14;
            float x = r.center.x - (movesW + gap + chestsW) / 2f, y = r.y + 8, ph = b - 16;
            string movesCol = run.Moves <= 5 ? "#d9534f" : "#8a5a34";
            GUI.Label(new Rect(x, y, movesW, ph), $"Ходы: <color={movesCol}>{run.Moves}</color>", _pill);

            var chestRect = new Rect(x + movesW + gap, y, chestsW, ph);
            var pill = new GUIStyle(_pill) { padding = new RectOffset(56, 14, 6, 6) };
            GUI.Label(chestRect, $"Сундуки: <color=#8a5a34>{run.Got.Count}</color>", pill);
            var icon = SpriteFactory.ChestArt(1);
            if (icon != null) GUI.DrawTexture(new Rect(chestRect.x + 10, chestRect.y + 4, ph - 8, ph - 8), icon.texture, ScaleMode.ScaleToFit);
        }

        private bool IconButton(Rect r, Texture2D icon)
        {
            bool hit = GUI.Button(r, GUIContent.none, _roundBtn);
            if (hit) Sfx.Play("click");
            float m = r.width * 0.2f;
            GUI.DrawTexture(new Rect(r.x + m, r.y + m, r.width - 2 * m, r.height - 2 * m), icon, ScaleMode.ScaleToFit);
            return hit;
        }

        // ---------- what the castle needs ----------

        private struct NeedChip
        {
            public string Text;
            public Color Swatch;
            /// <summary>Part whose icon the chip shows.</summary>
            public string Part;
        }

        private List<NeedChip> NeedChips()
        {
            var chips = new List<NeedChip>();
            int decor = 0, figures = 0;
            string decorPart = null, figurePart = null;
            foreach (var p in Game.Missing(State))
            {
                int tier = Game.Catalog.TierOf(p.Id);
                if (tier == 6) { decor += p.N; decorPart ??= p.Id; continue; }
                if (tier >= 7) { figures += p.N; figurePart ??= p.Id; continue; }
                chips.Add(new NeedChip { Text = Game.Catalog.NameOf(p.Id).ToLowerInvariant() + Times(p.N), Swatch = Palette.Tier(tier), Part = p.Id });
            }
            // Decor and figurines don't drop by block colour, so one chip per tier.
            if (decor > 0) chips.Add(new NeedChip { Text = "декор" + Times(decor), Swatch = Palette.Tier(6), Part = decorPart });
            if (figures > 0) chips.Add(new NeedChip { Text = "фигурки" + Times(figures), Swatch = Palette.Tier(7), Part = figurePart });
            return chips;
        }

        private static string Times(int n) => n > 1 ? $" ×{n}" : "";

        private void DrawNeeds(Rect r)
        {
            GUI.Box(r, GUIContent.none, _needsBox);
            var chips = NeedChips();
            const float ch = 30, gapX = 8, gapY = 6, padX = 14, padY = 10;
            float x = r.x + padX, y = r.y + padY, right = r.xMax - padX;
            if (chips.Count == 0)
            {
                if (!Mathf.Approximately(NeedsHeight(1), _needsHeight)) { _needsHeight = NeedsHeight(1); FitCamera(); }
                GUI.Label(new Rect(x, y, right - x, r.height - 2 * padY), "Замку всего хватает: открывай сундуки и строй", _needsTitle);
                return;
            }
            int rows = CountRows(chips, r.width - 2 * padX, gapX);
            if (!Mathf.Approximately(NeedsHeight(rows), _needsHeight))
            {
                _needsHeight = NeedsHeight(rows);
                FitCamera();
            }
            var title = new GUIContent("Нужно замку:");
            float tw = _needsTitle.CalcSize(title).x;
            GUI.Label(new Rect(x, y, tw, ch), title, _needsTitle);
            x += tw + gapX;
            foreach (var c in chips)
            {
                var content = new GUIContent(c.Text);
                float cw = _chip.CalcSize(content).x;
                if (x + cw > right && x > r.x + padX) { x = r.x + padX; y += ch + gapY; }
                if (y + ch > r.yMax - 4) break;
                var cr = new Rect(x, y, Mathf.Min(cw, right - x), ch);
                GUI.Label(cr, content, _chip);
                // Part icon with a dot in the colour of the blocks it drops from.
                var icon = c.Part != null ? GameArt.Part(c.Part) : null;
                if (icon != null) GUI.DrawTexture(new Rect(cr.x + 4, cr.y + 1, 28, 28), icon, ScaleMode.ScaleToFit);
                var old = GUI.color;
                GUI.color = c.Swatch;
                var dot = icon != null ? new Rect(cr.x + 24, cr.y + 18, 11, 11) : new Rect(cr.x + 10, cr.y + 7, 16, 16);
                GUI.DrawTexture(dot, UiArt.Round(Color.white, Color.white, icon != null ? 31 : 5, 0));
                GUI.color = old;
                x += cw + gapX;
            }
        }

        private int CountRows(List<NeedChip> chips, float width, float gapX)
        {
            float x = _needsTitle.CalcSize(new GUIContent("Нужно замку:")).x + gapX;
            int rows = 1;
            foreach (var c in chips)
            {
                float cw = _chip.CalcSize(new GUIContent(c.Text)).x;
                if (x + cw > width && x > 0) { x = 0; rows++; }
                x += cw + gapX;
            }
            return Mathf.Clamp(rows, 1, 4);
        }

        // ---------- broom and hammer ----------

        private void DrawTools(Rect r)
        {
            const float bw = 176, gap = 12;
            ToolButton(new Rect(r.x, r.y, bw, r.height), Tool.Broom, UiArt.Broom, State.Brooms);
            ToolButton(new Rect(r.x + bw + gap, r.y, bw, r.height), Tool.Hammer, UiArt.Hammer, State.Hammers);
            string hint = Armed == Tool.Broom ? "Тапни ряд: все блоки в нём исчезнут. Ход не тратится."
                : Armed == Tool.Hammer ? "Тапни блок: он исчезнет. Ход не тратится."
                : "Тапни группу из 2+ блоков. Из 4+ получится сундук.";
            float hx = r.x + 2 * bw + gap + 16;
            GUI.Label(new Rect(hx, r.y, r.xMax - hx, r.height), hint, _hint);
        }

        private void ToolButton(Rect r, Tool tool, Texture2D icon, int left)
        {
            var old = GUI.color;
            if (left < 1) GUI.color = new Color(1, 1, 1, 0.55f);
            if (GUI.Button(r, ToolName(tool), Armed == tool ? _toolOn : _tool)) Arm(tool);
            GUI.DrawTexture(new Rect(r.x + 10, r.y + (r.height - 54) / 2, 54, 54), icon, ScaleMode.ScaleToFit);
            GUI.color = old;
            GUI.Label(new Rect(r.xMax - 26, r.y - 8, 32, 32), left.ToString(), _badge);
        }

        // ---------- «Добыто» ----------

        private void DrawGot(Rect r, List<Chest> got)
        {
            GUI.Box(r, GUIContent.none, _gotBox);
            const float padX = 16, padTop = 10, titleH = 28;
            GUI.Label(new Rect(r.x + padX, r.y + padTop, r.width - 2 * padX, titleH), "ДОБЫТО", _gotTitle);

            // Pop-in time for each chest, so a new one bounces when it lands here.
            if (got.Count < _gotAt.Count) _gotAt.Clear();
            while (_gotAt.Count < got.Count) _gotAt.Add(Time.time);

            var area = new Rect(r.x + padX, r.y + padTop + titleH + 4, r.width - 2 * padX, r.height - padTop - titleH - 14);
            if (got.Count == 0)
            {
                GUI.Label(area, "Сундук упадёт сюда, когда уйдёт за нижний край поля", _gotEmpty);
                return;
            }

            // Largest chest size that fits every chest; if even the smallest doesn't, the rest is "+N".
            const float gap = 6;
            float size = 40;
            int perRow = 1, rows = 1;
            for (float s = 84; s >= 40; s -= 4)
            {
                perRow = Mathf.Max(1, Mathf.FloorToInt((area.width + gap) / (s + gap)));
                rows = Mathf.Max(1, Mathf.FloorToInt((area.height + gap) / (s + gap)));
                size = s;
                if (perRow * rows >= got.Count) break;
            }
            int capacity = perRow * rows, shown = got.Count <= capacity ? got.Count : capacity - 1;
            for (int i = 0; i < shown; i++)
            {
                int row = i / perRow, col = i % perRow;
                var cell = new Rect(area.x + col * (size + gap), area.y + row * (size + gap), size, size);
                float t = (Time.time - _gotAt[i]) / 0.35f;
                if (t < 1) cell = Grow(cell, 1 + 0.3f * Mathf.Sin(t * Mathf.PI));
                GameArt.DrawChest(cell, got[i]);
            }
            if (shown < got.Count)
            {
                int i = shown, row = i / perRow, col = i % perRow;
                GUI.Label(new Rect(area.x + col * (size + gap), area.y + row * (size + gap), size, size), $"+{got.Count - shown}", _gotTitle);
            }
        }

        private static Rect Grow(Rect r, float k)
        {
            var c = r.center;
            r.size *= k;
            r.center = c;
            return r;
        }

        // ---------- windows ----------

        private string RestartText()
        {
            int left = State.Run.Board.ChestsLeft().Count;
            string s = $"Будет новое поле и {State.Opts.Moves} ходов. Это стоит 1 билет, у тебя их {State.Tickets}.\n\nДобытые сундуки останутся.";
            if (left > 0) s += State.Opts.KeepOnEnd ? $" Сундуки с поля ({left}) тоже достанутся тебе." : $" Сундуки на поле ({left}) сгорят.";
            return s;
        }

        private void RestartButtons()
        {
            if (GUILayout.Button("Начать заново", _button, GUILayout.Height(76))) Restart();
            GUILayout.Space(12);
            if (GUILayout.Button("Отмена", _button2, GUILayout.Height(76))) _mode = Mode.Play;
        }

        private void Panel(float w, float h, string title, string text, Action buttons)
        {
            var old = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.55f);
            GUI.Box(new Rect(0, 0, w, h), GUIContent.none, _dim);
            GUI.color = old;
            float pw = Mathf.Min(600, w - 80);
            var r = new Rect((w - pw) / 2, h / 2 - 350, pw, 700);
            GUI.Box(r, GUIContent.none, _panel);
            GUILayout.BeginArea(new Rect(r.x + 36, r.y + 36, r.width - 72, r.height - 72));
            GUILayout.Label(title, _panelTitle);
            GUILayout.Space(20);
            GUILayout.Label(text, _panelBody);
            GUILayout.FlexibleSpace();
            buttons();
            GUILayout.EndArea();
        }
    }
}
