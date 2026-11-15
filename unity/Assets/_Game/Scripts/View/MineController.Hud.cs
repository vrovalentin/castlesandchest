using System;
using System.Collections.Generic;
using System.Linq;
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
        private static readonly Color Parchment = new Color32(0xff, 0xff, 0xff, 0xff);
        private static readonly Color Frame = new Color32(0x8f, 0x82, 0xc4, 0xff);
        private static readonly Color Green = new Color32(0x4f, 0xb3, 0x5a, 0xff);
        private static readonly Color GreenEdge = new Color32(0x2f, 0x7d, 0x3a, 0xff);
        private static readonly Color Sand = new Color32(0xe6, 0xe1, 0xf5, 0xff);

        private GUIStyle _pill, _roundBtn, _needsBox, _needsTitle, _chip, _tool, _toolOn, _badge, _hint,
            _gotBox, _gotTitle, _gotEmpty, _toastStyle, _dim, _panel, _panelTitle, _panelBody, _button, _button2;

        private readonly List<float> _gotAt = new List<float>();
        private bool _obstacleLine;

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
            // «Добыто» is one strip under the board; spare height goes around the board, not into an empty panel.
            const float gotMin = 130, gap = 12;
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
            // «Добыто» sits at the bottom like a tray. Spare height on tall phones goes in three even gaps
            // (under the bar, above the board, under it), so no single empty hole shows.
            r.Got = new Rect(x0 + pad, h - pad - gotMin, inner, gotMin);
            float g = Mathf.Max(0, r.Got.y - gap - top - bh) / 3f;
            r.Needs.y += g;
            r.Tools.y += g;
            r.Board = new Rect(x0 + pad, top + 2 * g, inner, bh);
            return r;
        }

        private void Styles()
        {
            if (_button != null) return;
            _pill = UiArt.Style(UiArt.Round(Parchment, Parchment, 30, 0), 30, 26, UiArt.Ink);
            _pill.fontStyle = FontStyle.Bold;
            _roundBtn = UiArt.Style(UiArt.Round(Parchment, Frame, 22, 3), 22, 1, UiArt.Ink);
            _roundBtn.active.background = UiArt.Round(Sand, Frame, 22, 3);
            _needsBox = UiArt.Style(UiArt.Round(PanelFill, Gold, 16, 3), 16, 1, Cream);
            _needsTitle = new GUIStyle { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            _needsTitle.normal.textColor = Gold;
            _chip = UiArt.Style(UiArt.Round(Parchment, Parchment, 14, 0), 14, 19, UiArt.Ink, TextAnchor.MiddleLeft);
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
            // Windows in the chest window's look when its art is there: night sky, ribbon title, gold and wood buttons.
            if (GameArt.NightPanel != null)
            {
                _panel = UiArt.Style(GameArt.NightPanel, 56, 1, Color.white);
                _panelTitle.normal.textColor = new Color32(0xff, 0xf8, 0xe6, 0xff);
                _panelBody.normal.textColor = new Color32(0xee, 0xea, 0xdc, 0xff);
            }
            if (GameArt.GoldButton != null)
            {
                _button = UiArt.Style(GameArt.GoldButton, 40, 28, new Color32(0x5a, 0x2e, 0x04, 0xff));
                _button.border = new RectOffset(40, 40, 40, 44);
                _button.padding = new RectOffset(16, 16, 4, 14);
                _button.fontStyle = FontStyle.Bold;
                _button.active.background = GameArt.GoldButtonDown;
                _button.active.textColor = new Color32(0x5a, 0x2e, 0x04, 0xff);
                _button2 = UiArt.Style(UiArt.Round(new Color32(0xa8, 0x70, 0x3f, 0xff), new Color32(0x6b, 0x44, 0x23, 0xff), 20, 4), 20, 26, Cream);
                _button2.fontStyle = FontStyle.Bold;
                _button2.active.background = UiArt.Round(new Color32(0x6b, 0x44, 0x23, 0xff), new Color32(0x6b, 0x44, 0x23, 0xff), 20, 4);
                _button2.active.textColor = Cream;
            }
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
            // First-run lines in a strip under the board: they don't block the board and go away after a move.
            string line = null;
            _obstacleLine = false;
            if (run != null && _mode == Mode.Play)
            {
                if (!Hints.Seen("board")) line = Loc.T("Тапай 2+ одинаковых блока.\nИз 4+ получится сундук!");
                else if (!Hints.Seen("obstacles") && HasObstacles(run.Board))
                {
                    line = Loc.T("Лёд треснет от взрыва рядом.\nКамень не взрывается, он падает с поля.");
                    _obstacleLine = true;
                }
            }
            if (run != null)
            {
                GUI.enabled = !Busy && _mode == Mode.Play;
                DrawBar(L.Bar, run);
                DrawNeeds(L.Needs);
                DrawTools(L.Tools);
                GUI.enabled = true;
                DrawGot(L.Got, run.Got);
            }

            if (line != null && !(_toast != null && Time.time < _toastUntil))
                GUI.Box(new Rect(L.Got.x, L.Got.y + 8, L.Got.width, L.Got.height - 16), line, _toastStyle);
            if (_toast != null && Time.time < _toastUntil)
                GUI.Box(new Rect(L.Board.x + 24, L.Board.y + 16, L.Board.width - 48, 84), _toast, _toastStyle);

            if (_mode == Mode.Play) _panelMode = Mode.Play;
            if (_mode == Mode.OutOfMoves) Panel(w, h, Loc.T("Ходы кончились"), OutOfMovesText(), OutOfMovesButtons);
            else if (_mode == Mode.Summary) Panel(w, h, SummaryTitle(), SummaryText(), SummaryButtons, SummaryChests);
            else if (_mode == Mode.ConfirmRestart) Panel(w, h, Loc.T("Начать заново?"), RestartText(), RestartButtons);
            else if (_mode == Mode.NoTickets) Panel(w, h, Loc.T("Билеты кончились"),
                Game.CanWatchAdForTickets(State) ? Loc.F("Посмотри рекламу и получи {0} билета. Или жди завтра: придут новые.", Game.Rules.AdTickets) : Loc.T("Новые билеты придут завтра."), () =>
            {
                if (Game.CanWatchAdForTickets(State) && GUILayout.Button(Loc.F("+{0} билета · реклама", Game.Rules.AdTickets), _button2, GUILayout.Height(76)))
                    WatchAdForTickets();
                GUILayout.Space(12);
                if (_toBuild != null && GUILayout.Button(Loc.T("На стройку"), _button, GUILayout.Height(76))) _toBuild();
            });
            else if (_mode == Mode.NoTools) Panel(w, h, Loc.T("Нет инструментов"),
                Loc.F("Посмотри рекламу и получи {0} метлы и {1} молотка. Или жди завтра: придут новые.", Game.Rules.AdTools, Game.Rules.AdTools), () =>
            {
                if (Game.CanWatchAdForTools(State) && GUILayout.Button(Loc.F("+{0} и +{1} · реклама", Game.Rules.AdTools, Game.Rules.AdTools), _button2, GUILayout.Height(76)))
                    WatchAdForTools();
                GUILayout.Space(12);
                if (GUILayout.Button(Loc.T("Позже"), _button, GUILayout.Height(76))) _mode = Mode.Play;
            });
            Hints.Draw();
        }

        private static bool HasObstacles(Board b)
        {
            for (int r = 0; r < b.Rows; r++)
                for (int c = 0; c < b.Cols; c++)
                    if (b[r, c] != null && (b[r, c].Ice || b[r, c].IsStone)) return true;
            return false;
        }

        // ---------- top bar ----------

        private void DrawBar(Rect r, RunState run)
        {
            float b = r.height;
            if (_toBuild != null && IconButton(new Rect(r.x, r.y, b, b), UiArt.Back)) _toBuild();
            if (IconButton(new Rect(r.xMax - b, r.y, b, b), UiArt.Restart)) AskRestart();

            // Moves, chests and points in three pills between the back and restart buttons.
            const float movesW = 150, chestsW = 116, scoreW = 226, gap = 10;
            float x = r.center.x - (movesW + chestsW + scoreW + 2 * gap) / 2f, y = r.y + 8, ph = b - 16;
            var small = new GUIStyle(_pill) { fontSize = 24, wordWrap = false, padding = new RectOffset(14, 10, 6, 6), alignment = TextAnchor.MiddleCenter };
            string movesCol = run.Moves <= 5 ? "#d9534f" : "#6b5aa6";
            GUI.Label(new Rect(x, y, movesW, ph), Loc.F("Ходы: <color={0}>{1}</color>", movesCol, run.Moves), small);

            var chestRect = new Rect(x + movesW + gap, y, chestsW, ph);
            var pill = new GUIStyle(small) { padding = new RectOffset(52, 10, 6, 6) };
            GUI.Label(chestRect, $"<color=#6b5aa6>{run.Got.Count}</color>", pill);
            var icon = SpriteFactory.ChestArt(1);
            if (icon != null) GUI.DrawTexture(new Rect(chestRect.x + 8, chestRect.y + 4, ph - 8, ph - 8), icon.texture, ScaleMode.ScaleToFit);

            // The points pill swells a little when points come in.
            if (run.Score != _shownScore) { _shownScore = run.Score; _scoreAt = Time.time; }
            float k = 1 + 0.12f * Mathf.Clamp01(1 - (Time.time - _scoreAt) / 0.3f);
            var sr = new Rect(chestRect.xMax + gap, y, scoreW, ph);
            var m = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(k, k), sr.center);
            GUI.Label(sr, Loc.F("Очки: <color=#6b5aa6>{0}</color>", run.Score), small);
            GUI.matrix = m;
        }

        private int _shownScore = -1;
        private float _scoreAt = -10;

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
                chips.Add(new NeedChip { Text = Times(p.N), Swatch = Palette.Tier(tier), Part = p.Id });
            }
            // Decor and figurines don't drop by block colour, so one chip per tier.
            if (decor > 0) chips.Add(new NeedChip { Text = Times(decor), Swatch = Palette.Tier(6), Part = decorPart });
            if (figures > 0) chips.Add(new NeedChip { Text = Times(figures), Swatch = Palette.Tier(7), Part = figurePart });
            return chips;
        }

        // Only the number: the part icon says what it is.
        private static string Times(int n) => n > 1 ? $"×{n}" : "";

        private void DrawNeeds(Rect r)
        {
            GUI.Box(r, GUIContent.none, _needsBox);
            var chips = NeedChips();
            const float ch = 30, gapX = 8, gapY = 6, padX = 14, padY = 10;
            float x = r.x + padX, y = r.y + padY, right = r.xMax - padX;
            if (chips.Count == 0)
            {
                if (!Mathf.Approximately(NeedsHeight(1), _needsHeight)) { _needsHeight = NeedsHeight(1); FitCamera(); }
                GUI.Label(new Rect(x, y, right - x, r.height - 2 * padY), Loc.T("Замку всего хватает: открывай сундуки и строй"), _needsTitle);
                return;
            }
            int rows = CountRows(chips, r.width - 2 * padX, gapX);
            if (!Mathf.Approximately(NeedsHeight(rows), _needsHeight))
            {
                _needsHeight = NeedsHeight(rows);
                FitCamera();
            }
            var title = new GUIContent(Loc.T("Нужно замку:"));
            float tw = _needsTitle.CalcSize(title).x;
            GUI.Label(new Rect(x, y, tw, ch), title, _needsTitle);
            x += tw + gapX;
            foreach (var c in chips)
            {
                var content = new GUIContent(c.Text);
                float cw = ChipWidth(c);
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

        private float ChipWidth(NeedChip c) =>
            string.IsNullOrEmpty(c.Text) ? 40 : _chip.CalcSize(new GUIContent(c.Text)).x;

        private int CountRows(List<NeedChip> chips, float width, float gapX)
        {
            float x = _needsTitle.CalcSize(new GUIContent(Loc.T("Нужно замку:"))).x + gapX;
            int rows = 1;
            foreach (var c in chips)
            {
                float cw = ChipWidth(c);
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
            // A line only while a tool is armed; the board itself is taught by the first-run hint.
            string hint = Armed == Tool.Broom ? Loc.T("Тапни ряд: все блоки в нём исчезнут. Ход не тратится.")
                : Armed == Tool.Hammer ? Loc.T("Тапни блок: он исчезнет. Ход не тратится.")
                : null;
            if (hint == null) return;
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
            GUI.Label(new Rect(r.x + padX, r.y + padTop, r.width - 2 * padX, titleH), Loc.T("ДОБЫТО"), _gotTitle);

            // Pop-in time for each chest, so a new one bounces when it lands here.
            if (got.Count < _gotAt.Count) _gotAt.Clear();
            while (_gotAt.Count < got.Count) _gotAt.Add(Time.time);

            var area = new Rect(r.x + padX, r.y + padTop + titleH + 4, r.width - 2 * padX, r.height - padTop - titleH - 14);
            if (got.Count == 0)
            {
                GUI.Label(area, Loc.T("Сундук упадёт сюда, когда уйдёт за нижний край поля"), _gotEmpty);
                return;
            }

            // Largest chest size that fits every chest; if even the smallest doesn't, the rest is "+N".
            const float gap = 6;
            float size = 40;
            int perRow = 1, rows = 1;
            for (float s = Mathf.Min(84, area.height); s >= 40; s -= 4)
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
            if (State.Tickets < 1)
                return Loc.F("Новое поле стоит 1 билет, а билеты кончились.\n\nПосмотри рекламу и получи {0} билета.", Game.Rules.AdTickets);
            int left = State.Run.Board.ChestsLeft().Count;
            string s = Loc.F("Будет новое поле и {0} ходов. Это стоит 1 билет, у тебя их {1}.\n\nДобытые сундуки останутся.", State.Opts.Moves, State.Tickets);
            if (left > 0) s += State.Opts.KeepOnEnd ? Loc.F(" Сундуки с поля ({0}) тоже достанутся тебе.", left) : Loc.F(" Сундуки на поле ({0}) сгорят.", left);
            return s;
        }

        private void RestartButtons()
        {
            if (State.Tickets < 1)
            {
                if (Game.CanWatchAdForTickets(State) && GUILayout.Button(Loc.F("+{0} билета · реклама", Game.Rules.AdTickets), _button, GUILayout.Height(76)))
                    WatchAdForTickets();
            }
            else if (GUILayout.Button(Loc.T("Начать заново"), _button, GUILayout.Height(76))) Restart();
            GUILayout.Space(12);
            if (GUILayout.Button(Loc.T("Отмена"), _button2, GUILayout.Height(76))) _mode = Mode.Play;
        }

        /// <summary>The chests of the finished run as pictures, one per kind, with a count badge top-right.</summary>
        private void SummaryChests()
        {
            var groups = _summary.Got.GroupBy(ch => (ch.Color, ch.Stars))
                .OrderByDescending(g => g.Key.Stars).ThenBy(g => g.Key.Color)
                .Select(g => (chest: g.First(), n: g.Count() * (_doubled ? 2 : 1))).ToList();
            if (groups.Count == 0) { _contentH = 0; return; }
            float width = _panelInnerW;
            // Largest chest that keeps the grid within 3 rows (and at most 112 px).
            const float gap = 18;
            float size = 112;
            int perRow = 1;
            for (; size > 56; size -= 4)
            {
                perRow = Mathf.Max(1, Mathf.FloorToInt((width + gap) / (size + gap)));
                if (Mathf.CeilToInt(groups.Count / (float)perRow) <= 3) break;
            }
            int rows = Mathf.CeilToInt(groups.Count / (float)perRow);
            _contentH = rows * (size + gap) + 8;
            var area = GUILayoutUtility.GetRect(width, rows * (size + gap) + 8, GUILayout.ExpandWidth(true));
            for (int i = 0; i < groups.Count; i++)
            {
                int row = i / perRow, col = i % perRow;
                int inRow = Mathf.Min(perRow, groups.Count - row * perRow);
                float rowW = inRow * size + (inRow - 1) * gap;
                var cell = new Rect(area.center.x - rowW / 2 + col * (size + gap), area.y + 12 + row * (size + gap), size, size);
                GameArt.DrawChest(cell, groups[i].chest);
                string n = groups[i].n.ToString();
                float bw = n.Length > 1 ? 44 : 36;
                GUI.Label(new Rect(cell.xMax - bw + 8, cell.y - 10, bw, 36), n, _badge);
            }
        }

        private float _panelInnerW = 520;
        private Mode _panelMode;
        private float _panelAt;
        private bool _panelAwake;
        /// <summary>Height the panel's content took last frame, so the window fits it without a gap.</summary>
        private float _contentH = 140;

        private void Panel(float w, float h, string title, string text, Action buttons, Action content = null)
        {
            var old = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.55f);
            GUI.Box(new Rect(0, 0, w, h), GUIContent.none, _dim);
            GUI.color = old;
            float pw = Mathf.Min(600, w - 80);
            bool ribbon0 = GameArt.Ribbon != null;
            float textH = string.IsNullOrEmpty(text) ? 0 : _panelBody.CalcHeight(new GUIContent(text), pw - 80);
            float ph = Mathf.Min(h - 160, content != null ? 520 + _contentH : (ribbon0 ? 136 : 72) + 20 + textH + 30 + 176);
            // Fast taps on the board must not land on a window that has just opened under the finger
            // (they took the ad for +5 moves, ended the run and started a new one for a ticket). The buttons
            // wake up only once the window has been open a moment AND the finger has stopped tapping.
            // Once awake they stay awake: the press on a button must not put it back to sleep.
            if (_mode != _panelMode) { _panelMode = _mode; _panelAt = Time.unscaledTime; _panelAwake = false; }
            if (!_panelAwake && Time.unscaledTime - Mathf.Max(_panelAt, _lastDownAt) > 1.0f) _panelAwake = true;
            bool wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && _panelAwake;
            var r = new Rect((w - pw) / 2, (h - ph) / 2, pw, ph);
            GUI.Box(r, GUIContent.none, _panel);
            bool ribbon = GameArt.Ribbon != null;
            if (ribbon)
            {
                var rib = new Rect(r.center.x - 256, r.y - 44, 512, 128);
                GUI.DrawTexture(rib, GameArt.Ribbon);
                var t = new Rect(rib.x + 44, rib.y + 14, rib.width - 88, 82);
                var col = _panelTitle.normal.textColor;
                _panelTitle.normal.textColor = new Color32(0x78, 0x1e, 0x14, 0xff);
                for (int i = 0; i < 8; i++)
                    GUI.Label(new Rect(t.x + Mathf.Cos(i * Mathf.PI / 4) * 2.5f, t.y + Mathf.Sin(i * Mathf.PI / 4) * 2.5f, t.width, t.height), title, _panelTitle);
                _panelTitle.normal.textColor = col;
                GUI.Label(t, title, _panelTitle);
            }
            _panelInnerW = r.width - 80;
            GUILayout.BeginArea(new Rect(r.x + 40, r.y + (ribbon ? 100 : 36), r.width - 80, r.height - (ribbon ? 136 : 72)));
            if (!ribbon) GUILayout.Label(title, _panelTitle);
            GUILayout.Space(content != null ? 8 : 20);
            content?.Invoke();
            if (!string.IsNullOrEmpty(text)) GUILayout.Label(text, _panelBody);
            GUILayout.FlexibleSpace();
            buttons();
            GUILayout.EndArea();
            GUI.enabled = wasEnabled;
        }
    }
}
