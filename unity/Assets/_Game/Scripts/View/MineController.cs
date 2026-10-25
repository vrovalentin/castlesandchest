using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using UnityEngine;

namespace Game.View
{
    /// <summary>
    /// The mine screen: one run on the board. Works on the shared GameSession and saves after every step.
    /// The HUD (MineController.Hud.cs) is IMGUI with code-drawn panels until the UI art is ready.
    /// </summary>
    public sealed partial class MineController : MonoBehaviour
    {
        private enum Mode { Play, OutOfMoves, Summary, NoTickets, ConfirmRestart }

        public GameLogic Game => _s.Game;
        public GameState State => _s.State;
        public bool Busy { get; private set; }
        public Tool? Armed { get; private set; }
        public int CellViewCount => _view != null ? _view.Count : 0;

        private GameSession _s;
        private Action _toBuild;
        private IRandom _rng => _s.Rng;
        private BoardView _view;
        private Camera _cam;
        private Mode _mode;
        private RunSummary _summary;
        private string _toast;
        private float _toastUntil;
        private Vector2 _lastScreen;

        private void Awake()
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                camGo.transform.SetParent(transform, false);
                _cam = camGo.AddComponent<Camera>();
            }
            _cam.orthographic = true;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Palette.MineBackground;
            _cam.transform.position = new Vector3(0, 0, -10);

            _view = new GameObject("Board").AddComponent<BoardView>();
            _view.transform.SetParent(transform, false);

            var bg = GameArt.MineBackground;
            if (bg != null)
            {
                _bg = new GameObject("Background").AddComponent<SpriteRenderer>();
                _bg.transform.SetParent(transform, false);
                _bg.sprite = Sprite.Create(bg, new Rect(0, 0, bg.width, bg.height), new Vector2(0.5f, 0.5f), bg.width);
                _bg.sortingOrder = -100;
                // A little darker than the painting, so the board and the HUD stay in front.
                _bg.color = new Color(0.78f, 0.78f, 0.78f, 1);
            }
        }

        private SpriteRenderer _bg;

        // The mine painting covers the whole view, whatever the screen shape.
        private void FitBackground()
        {
            if (_bg == null) return;
            float hgt = 2 * _cam.orthographicSize, wid = hgt * _cam.aspect;
            var p = _cam.transform.position;
            _bg.transform.position = new Vector3(p.x, p.y, 5);
            float k = Mathf.Max(wid / _bg.sprite.bounds.size.x, hgt / _bg.sprite.bounds.size.y);
            _bg.transform.localScale = new Vector3(k, k, 1);
        }

        // The mine is dark; the build screen keeps the light background.
        private void OnEnable() { if (_cam != null) _cam.backgroundColor = Palette.MineBackground; }
        private void OnDisable() { if (_cam != null) _cam.backgroundColor = Palette.Background; }

        /// <summary>toBuild: what the "to the build screen" buttons do; null hides them (standalone mine).</summary>
        public void Init(GameSession session, Action toBuild)
        {
            _s = session;
            _toBuild = toBuild;
        }

        // Standalone use (a scene or test with only the mine): play on a fresh, unsaved game.
        private void Start()
        {
            if (_s != null) return;
            Init(GameSession.Create(false), null);
            Enter();
        }

        /// <summary>Show the mine: continue the current run, or start a new one for a ticket.</summary>
        public void Enter()
        {
            Armed = null;
            if (State.Run == null) { StartRun(); return; }
            _summary = null;
            _view.Bind(State.Run.Board, Game.Rules);
            _mode = State.Run.Moves > 0 ? Mode.Play : Mode.OutOfMoves;
            FitCamera();
        }

        private void StartRun()
        {
            Armed = null;
            _summary = null;
            _doubled = false;
            if (Game.NewRun(State, _rng) == null) { _mode = Mode.NoTickets; return; }
            _s.Save();
            _view.Bind(State.Run.Board, Game.Rules);
            _mode = Mode.Play;
            FitCamera();
        }

        private bool _doubled;

        // The board fills the gap the HUD leaves between the tools row and the «Добыто» panel.
        private void FitCamera()
        {
            if (State.Run == null) return;
            var b = State.Run.Board;
            _lastScreen = new Vector2(Screen.width, Screen.height);
            var area = Layout().Board;
            float s = HudScale;
            // Board tray is (Cols + 0.3) × (Rows + 0.3) units; world units per screen pixel:
            float u = Mathf.Max((b.Cols + 0.3f) / Mathf.Max(1, area.width * s), (b.Rows + 0.3f) / Mathf.Max(1, area.height * s));
            _cam.orthographicSize = Screen.height * u / 2f;
            // GUI y runs down; move the camera so the board centre lands in the middle of the area.
            float dy = area.center.y * s - Screen.height / 2f;
            float dx = area.center.x * s - Screen.width / 2f;
            _cam.transform.position = new Vector3(-dx * u, dy * u, -10);
            FitBackground();
        }

        private void Update()
        {
            if (_s == null) return;
            if (Screen.width != _lastScreen.x || Screen.height != _lastScreen.y) FitCamera();
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
            StartCoroutine(PlayThen(frames, () => { if (State.Run.Moves <= 0) _mode = Mode.OutOfMoves; }));
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
            _s.Save();
        }

        private void FinishRun()
        {
            _summary = Game.EndRun(State);
            _mode = Mode.Summary;
            _s.Save();
        }

        /// <summary>The ↻ button: ask first, since it costs a ticket.</summary>
        public void AskRestart()
        {
            if (Busy || State.Run == null || _mode != Mode.Play) return;
            if (State.Tickets < 1) { Toast("Билеты кончились, начать заново можно завтра"); return; }
            _mode = Mode.ConfirmRestart;
        }

        /// <summary>New board and full moves for one ticket; fallen chests stay. False without a ticket.</summary>
        public bool Restart()
        {
            if (Busy || Game.Restart(State, _rng) == null) return false;
            Armed = null;
            _summary = null;
            _doubled = false;
            _s.Save();
            _view.Bind(State.Run.Board, Game.Rules);
            _mode = Mode.Play;
            FitCamera();
            Toast($"Новое поле. Билетов осталось: {State.Tickets}");
            return true;
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
            if (!State.Run.ExtraUsed && GUILayout.Button("+5 ходов · реклама", _button, GUILayout.Height(76)))
            {
                State.Run.Moves += 5;
                State.Run.ExtraUsed = true;
                _mode = Mode.Play;
                _s.Save();
            }
            GUILayout.Space(12);
            if (GUILayout.Button("Закончить", State.Run.ExtraUsed ? _button : _button2, GUILayout.Height(76))) FinishRun();
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
            if (_summary.Got.Count > 0 && !_doubled && GUILayout.Button("Удвоить · реклама", _button, GUILayout.Height(76)))
            {
                State.Chests.AddRange(_summary.Got);
                _doubled = true;
                _s.Save();
            }
            GUILayout.Space(12);
            if (_toBuild != null && GUILayout.Button("Открывать на Стройке", _button, GUILayout.Height(76))) _toBuild();
            GUILayout.Space(12);
            if (GUILayout.Button($"Новый забег (билетов: {State.Tickets})", _button2, GUILayout.Height(76))) StartRun();
        }
    }
}
