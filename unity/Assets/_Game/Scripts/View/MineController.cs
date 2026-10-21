using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using UnityEngine;

namespace Game.View
{
    /// <summary>
    /// The mine screen: one run on the board. Owns the game state for now (no saves yet).
    /// The HUD is temporary IMGUI so the board can be played; the real UI comes from the wireframe later.
    /// </summary>
    public sealed class MineController : MonoBehaviour
    {
        private enum Mode { Play, OutOfMoves, Summary, NoTickets }

        public GameLogic Game { get; private set; }
        public GameState State { get; private set; }
        public bool Busy { get; private set; }
        public Tool? Armed { get; private set; }
        public int CellViewCount => _view != null ? _view.Count : 0;

        private IRandom _rng;
        private BoardView _view;
        private Camera _cam;
        private Mode _mode;
        private RunSummary _summary;
        private string _toast;
        private float _toastUntil;
        private float _lastAspect;

        private void Awake()
        {
            Game = new GameLogic(DemoContent.Create());
            _rng = new SystemRandom();
            State = Game.NewGame();
            Game.DailyGrant(State, DateTime.Now.ToString("yyyy-MM-dd"));

            _cam = Camera.main;
            if (_cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                camGo.transform.SetParent(transform, false);
                _cam = camGo.AddComponent<Camera>();
            }
            _cam.orthographic = true;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Palette.Background;
            _cam.transform.position = new Vector3(0, 0, -10);

            _view = new GameObject("Board").AddComponent<BoardView>();
            _view.transform.SetParent(transform, false);
            StartRun();
        }

        private void StartRun()
        {
            Armed = null;
            _summary = null;
            if (Game.NewRun(State, _rng) == null) { _mode = Mode.NoTickets; return; }
            _view.Bind(State.Run.Board, Game.Rules);
            _mode = Mode.Play;
            FitCamera();
        }

        // Board is Cols × Rows units; leave room for the HUD above and the tools below.
        private void FitCamera()
        {
            if (State.Run == null) return;
            var b = State.Run.Board;
            float aspect = Mathf.Max(0.1f, _cam.aspect);
            _lastAspect = aspect;
            _cam.orthographicSize = Mathf.Max((b.Rows + 5f) / 2f, (b.Cols + 0.8f) / 2f / aspect);
            _cam.transform.position = new Vector3(0, -0.3f, -10);
        }

        private void Update()
        {
            if (!Mathf.Approximately(_cam.aspect, _lastAspect)) FitCamera();
            if (Busy || _mode != Mode.Play || !Input.GetMouseButtonDown(0)) return;
            if (GUIUtility.hotControl != 0) return;
            var world = _cam.ScreenToWorldPoint(Input.mousePosition);
            if (_view.TryGetCell(world, out int r, out int c)) TryTap(r, c);
        }

        /// <summary>A tap on cell (r, c). True if it started a move or a tool.</summary>
        public bool TryTap(int r, int c)
        {
            if (Busy || _mode != Mode.Play || State.Run == null) return false;
            var board = State.Run.Board;
            var cell = board[r, c];
            if (cell == null) return false;
            if (Armed.HasValue) return TryTool(Armed.Value, r, c);
            if (cell.IsChest) { Toast("Сундук не нажимается. Убери блоки под ним, и он упадёт."); return false; }
            if (board.Group(r, c).Count < 2) { StartCoroutine(_view.Shake(cell.Id)); return false; }

            var frames = new List<Frame>();
            var res = Game.Tap(State, r, c, _rng, p => frames.Add(BoardView.Capture(board, p)));
            if (res == null) return false;
            StartCoroutine(PlayThen(frames, () =>
            {
                if (res.Chests.Count >= 2) Toast($"Серия: {res.Chests.Count} сундука за ход!");
                if (State.Run.Moves <= 0) _mode = Mode.OutOfMoves;
            }));
            return true;
        }

        public void Arm(Tool tool)
        {
            if (Busy || _mode != Mode.Play) return;
            if (Armed == tool) { Armed = null; return; }
            int left = tool == Tool.Broom ? State.Brooms : State.Hammers;
            if (left < 1) { Toast($"{ToolName(tool)}: кончились, новые завтра"); return; }
            Armed = tool;
        }

        private bool TryTool(Tool tool, int r, int c)
        {
            var board = State.Run.Board;
            if (tool == Tool.Hammer && board[r, c].IsChest) { Toast("Молоток бьёт только блоки, сундук не разбить"); return false; }
            var frames = new List<Frame>();
            var res = Game.UseTool(State, tool, r, c, _rng, p => frames.Add(BoardView.Capture(board, p)));
            if (res == null) { Toast("Тут нечего убирать"); return false; }
            Armed = null;
            StartCoroutine(PlayThen(frames, null));
            return true;
        }

        private IEnumerator PlayThen(List<Frame> frames, Action after)
        {
            Busy = true;
            _view.ClearHints();
            yield return _view.Play(frames);
            _view.RefreshHints();
            Busy = false;
            after?.Invoke();
        }

        private void FinishRun()
        {
            _summary = Game.EndRun(State);
            _mode = Mode.Summary;
        }

        private void Toast(string text)
        {
            _toast = text;
            _toastUntil = Time.time + 2.5f;
        }

        private static string ToolName(Tool t) => t == Tool.Broom ? "Метла" : "Молоток";

        private static string Plural(int n, string one, string few, string many)
        {
            int m10 = n % 10, m100 = n % 100;
            if (m10 == 1 && m100 != 11) return one;
            if (m10 >= 2 && m10 <= 4 && (m100 < 12 || m100 > 14)) return few;
            return many;
        }

        // ---------- temporary HUD ----------

        private GUIStyle _label, _big, _button, _box, _toastStyle;
        private float _scale;

        private void Styles()
        {
            if (_label != null) return;
            _label = new GUIStyle(GUI.skin.label) { fontSize = 26, alignment = TextAnchor.MiddleCenter };
            _label.normal.textColor = new Color(0.25f, 0.18f, 0.1f);
            _big = new GUIStyle(_label) { fontSize = 34, fontStyle = FontStyle.Bold, wordWrap = true };
            _button = new GUIStyle(GUI.skin.button) { fontSize = 26 };
            _box = new GUIStyle(GUI.skin.box);
            _toastStyle = new GUIStyle(_label) { wordWrap = true };
            _toastStyle.normal.textColor = Color.white;
        }

        private void OnGUI()
        {
            Styles();
            // Layout in a 720-wide virtual screen, scaled to fit.
            _scale = Mathf.Min(Screen.width / 720f, Screen.height / 1280f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(_scale, _scale, 1));
            float w = Screen.width / _scale, h = Screen.height / _scale;

            var run = State.Run;
            if (run != null)
            {
                GUI.Label(new Rect(0, 20, w / 3, 50), $"Ходы: {run.Moves}", _label);
                GUI.Label(new Rect(w / 3, 20, w / 3, 50), $"Сундуки: {run.Got.Count}", _label);
                GUI.Label(new Rect(2 * w / 3, 20, w / 3, 50), $"Билеты: {State.Tickets}", _label);

                float bw = 220, bh = 70, y = h - 150;
                GUI.color = Armed == Tool.Broom ? Color.yellow : Color.white;
                if (GUI.Button(new Rect(w / 2 - bw - 10, y, bw, bh), $"Метла ({State.Brooms})", _button)) Arm(Tool.Broom);
                GUI.color = Armed == Tool.Hammer ? Color.yellow : Color.white;
                if (GUI.Button(new Rect(w / 2 + 10, y, bw, bh), $"Молоток ({State.Hammers})", _button)) Arm(Tool.Hammer);
                GUI.color = Color.white;
                string hint = Armed == Tool.Broom ? "Тапни ряд: все блоки в нём исчезнут. Ход не тратится."
                    : Armed == Tool.Hammer ? "Тапни блок: он исчезнет. Ход не тратится."
                    : "Тапни группу из 2+ блоков. Из 4+ получится сундук.";
                GUI.Label(new Rect(0, y + bh + 5, w, 50), hint, _label);
            }

            if (_toast != null && Time.time < _toastUntil)
            {
                var r = new Rect(40, 90, w - 80, 80);
                GUI.color = new Color(0, 0, 0, 0.75f);
                GUI.Box(r, GUIContent.none, _box);
                GUI.color = Color.white;
                GUI.Label(r, _toast, _toastStyle);
            }

            if (_mode == Mode.OutOfMoves) Panel(w, h, "Ходы кончились", OutOfMovesText(), OutOfMovesButtons);
            else if (_mode == Mode.Summary) Panel(w, h, $"Добыто сундуков: {_summary.Got.Count}", SummaryText(), SummaryButtons);
            else if (_mode == Mode.NoTickets) Panel(w, h, "Билеты кончились", "Новые билеты придут завтра.", () => { });
        }

        private string OutOfMovesText()
        {
            int left = State.Run.Board.ChestsLeft().Count;
            string s = left > 0
                ? $"На поле ещё {left} {Plural(left, "сундук", "сундука", "сундуков")}. " + (State.Opts.KeepOnEnd ? "Они достанутся тебе." : "Если закончить, они сгорят.")
                : "Сундуков на поле не осталось.";
            if (State.Run.ExtraUsed) s += "\nДополнительные ходы уже брали в этом забеге.";
            return s;
        }

        private void OutOfMovesButtons()
        {
            if (!State.Run.ExtraUsed && GUILayout.Button("+5 ходов · реклама", _button, GUILayout.Height(70)))
            {
                State.Run.Moves += 5;
                State.Run.ExtraUsed = true;
                _mode = Mode.Play;
            }
            if (GUILayout.Button("Закончить", _button, GUILayout.Height(70))) FinishRun();
        }

        private string SummaryText()
        {
            var lines = new List<string>();
            if (_summary.Got.Count == 0) lines.Add("Ни один сундук не упал за край.");
            else
                lines.AddRange(_summary.Got
                    .GroupBy(ch => (ch.Color, ch.Stars))
                    .OrderByDescending(g => g.Key.Stars)
                    .Select(g => $"{Palette.BlockNames[g.Key.Color]} ★{g.Key.Stars} × {g.Count()}"));
            if (_summary.Left > 0) lines.Add(_summary.Kept ? $"Из них {_summary.Left} с поля, они достались тебе." : $"Сгорело на поле: {_summary.Left}.");
            lines.Add($"Всего сундуков ждёт на Стройке: {State.Chests.Count}");
            return string.Join("\n", lines);
        }

        private void SummaryButtons()
        {
            if (GUILayout.Button($"Новый забег (билетов: {State.Tickets})", _button, GUILayout.Height(70))) StartRun();
        }

        private void Panel(float w, float h, string title, string text, Action buttons)
        {
            GUI.color = new Color(0, 0, 0, 0.5f);
            GUI.Box(new Rect(0, 0, w, h), GUIContent.none, _box);
            GUI.color = Color.white;
            var r = new Rect(60, h / 2 - 300, w - 120, 600);
            GUI.Box(r, GUIContent.none, _box);
            GUI.Box(r, GUIContent.none, _box);
            GUILayout.BeginArea(new Rect(r.x + 30, r.y + 30, r.width - 60, r.height - 60));
            var light = new GUIStyle(_big); light.normal.textColor = Color.white;
            var body = new GUIStyle(_label) { wordWrap = true }; body.normal.textColor = Color.white;
            GUILayout.Label(title, light);
            GUILayout.Space(20);
            GUILayout.Label(text, body);
            GUILayout.FlexibleSpace();
            buttons();
            GUILayout.EndArea();
        }
    }
}
