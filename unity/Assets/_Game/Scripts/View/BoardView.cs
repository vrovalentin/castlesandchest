using System.Collections;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.View
{
    /// <summary>Where one cell is after a phase of a move.</summary>
    public struct Snap
    {
        public int R, C;
        public CellKind Kind;
        public int Color, Stars;
    }

    /// <summary>Board picture after one phase of a move (Core calls back after each phase; we copy the board).</summary>
    public sealed class Frame
    {
        public PhaseKind Kind;
        public Dictionary<int, Snap> Cells;
        /// <summary>For refill: the row each new block falls from.</summary>
        public Dictionary<int, int> SpawnFrom;
    }

    /// <summary>Draws a Core Board with sprites and animates moves frame by frame.</summary>
    public sealed class BoardView : MonoBehaviour
    {
        private Board _board;
        private Rules _rules;
        private readonly Dictionary<int, CellView> _views = new Dictionary<int, CellView>();
        private Dictionary<int, Snap> _shown = new Dictionary<int, Snap>();
        private Transform _cellsRoot;
        private SpriteRenderer _back;

        public int Count => _views.Count;

        public static Frame Capture(Board b, Phase p)
        {
            var f = new Frame { Kind = p.Kind, Cells = Snapshot(b) };
            if (p.Spawns != null)
            {
                f.SpawnFrom = new Dictionary<int, int>();
                foreach (var s in p.Spawns) f.SpawnFrom[s.Id] = s.From;
            }
            return f;
        }

        public static Dictionary<int, Snap> Snapshot(Board b)
        {
            var d = new Dictionary<int, Snap>();
            for (int r = 0; r < b.Rows; r++)
                for (int c = 0; c < b.Cols; c++)
                {
                    var x = b[r, c];
                    if (x != null) d[x.Id] = new Snap { R = r, C = c, Kind = x.Kind, Color = x.Color, Stars = x.Stars };
                }
            return d;
        }

        public Vector3 CellPos(float r, float c) =>
            new Vector3(c - (_board.Cols - 1) / 2f, (_board.Rows - 1) / 2f - r, 0);

        public bool TryGetCell(Vector3 world, out int r, out int c)
        {
            c = Mathf.RoundToInt(world.x - transform.position.x + (_board.Cols - 1) / 2f);
            r = Mathf.RoundToInt((_board.Rows - 1) / 2f - (world.y - transform.position.y));
            return r >= 0 && r < _board.Rows && c >= 0 && c < _board.Cols;
        }

        public void Bind(Board board, Rules rules)
        {
            _board = board;
            _rules = rules;
            foreach (var v in _views.Values) Destroy(v.gameObject);
            _views.Clear();
            if (_cellsRoot == null)
            {
                _cellsRoot = new GameObject("Cells").transform;
                _cellsRoot.SetParent(transform, false);
                _back = new GameObject("Back").AddComponent<SpriteRenderer>();
                _back.transform.SetParent(transform, false);
                _back.sprite = SpriteFactory.Panel;
                _back.color = Palette.BoardBack;
                _back.sortingOrder = -10;
            }
            _back.transform.localScale = new Vector3(board.Cols + 0.3f, board.Rows + 0.3f, 1);
            _shown = Snapshot(board);
            foreach (var kv in _shown) Spawn(kv.Key, kv.Value, CellPos(kv.Value.R, kv.Value.C));
            RefreshHints();
        }

        private CellView Spawn(int id, Snap s, Vector3 pos)
        {
            var v = CellView.Create(_cellsRoot, id);
            v.Apply(s.Kind, s.Color, s.Stars);
            v.transform.localPosition = pos;
            _views[id] = v;
            return v;
        }

        public void RefreshHints()
        {
            ClearHints();
            foreach (var g in _board.AllGroups())
            {
                int stars = _rules.StarsFor(g.Count);
                if (g.Count < _rules.ChestMinGroup || stars == 0) continue;
                foreach (var p in g)
                    if (_views.TryGetValue(_board[p.R, p.C].Id, out var v)) v.SetHint(stars);
            }
        }

        public void ClearHints()
        {
            foreach (var v in _views.Values) v.SetHint(0);
        }

        private static float Duration(PhaseKind k)
        {
            switch (k)
            {
                case PhaseKind.Remove: return 0.14f;
                case PhaseKind.Gravity: return 0.18f;
                case PhaseKind.Collect: return 0.3f;
                case PhaseKind.Refill: return 0.22f;
                default: return 0.35f;
            }
        }

        private static float Ease(float t) => t * t * (3 - 2 * t);

        /// <summary>Plays the frames of one move. Ends with the views matching the board exactly.</summary>
        public IEnumerator Play(List<Frame> frames)
        {
            foreach (var f in frames)
            {
                var gone = new List<CellView>();
                foreach (var id in _shown.Keys)
                    if (!f.Cells.ContainsKey(id) && _views.TryGetValue(id, out var v)) gone.Add(v);
                foreach (var v in gone) _views.Remove(v.Id);

                var from = new Dictionary<CellView, Vector3>();
                var to = new Dictionary<CellView, Vector3>();
                var popIn = new List<CellView>();
                foreach (var kv in f.Cells)
                {
                    var target = CellPos(kv.Value.R, kv.Value.C);
                    if (!_views.TryGetValue(kv.Key, out var v))
                    {
                        bool falls = f.SpawnFrom != null && f.SpawnFrom.ContainsKey(kv.Key);
                        var start = falls ? CellPos(f.SpawnFrom[kv.Key], kv.Value.C) : target;
                        v = Spawn(kv.Key, kv.Value, start);
                        if (!falls) { popIn.Add(v); v.transform.localScale = Vector3.zero; }
                    }
                    else v.Apply(kv.Value.Kind, kv.Value.Color, kv.Value.Stars);
                    from[v] = v.transform.localPosition;
                    to[v] = target;
                }
                var goneFrom = new Dictionary<CellView, Vector3>();
                foreach (var v in gone) goneFrom[v] = v.transform.localPosition;

                float dur = Duration(f.Kind);
                bool collect = f.Kind == PhaseKind.Collect;
                for (float t = 0; t < dur; t += Time.deltaTime)
                {
                    float k = Ease(Mathf.Clamp01(t / dur));
                    foreach (var kv in to) kv.Key.transform.localPosition = Vector3.LerpUnclamped(from[kv.Key], kv.Value, k);
                    foreach (var v in popIn) v.transform.localScale = Vector3.one * k;
                    foreach (var v in gone)
                    {
                        if (collect)
                        {
                            v.transform.localPosition = goneFrom[v] + Vector3.down * (1.5f * k);
                            v.SetAlpha(1 - k);
                        }
                        else v.transform.localScale = Vector3.one * (1 - k);
                    }
                    yield return null;
                }
                foreach (var kv in to) kv.Key.transform.localPosition = kv.Value;
                foreach (var v in popIn) v.transform.localScale = Vector3.one;
                foreach (var v in gone) Destroy(v.gameObject);
                _shown = f.Cells;
            }
        }

        /// <summary>Little wiggle for a tap that does nothing.</summary>
        public IEnumerator Shake(int id)
        {
            if (!_views.TryGetValue(id, out var v)) yield break;
            var basePos = v.transform.localPosition;
            for (float t = 0; t < 0.25f; t += Time.deltaTime)
            {
                if (v == null) yield break;
                v.transform.localPosition = basePos + Vector3.right * (Mathf.Sin(t * 60f) * 0.08f * (1 - t / 0.25f));
                yield return null;
            }
            if (v != null) v.transform.localPosition = basePos;
        }
    }
}
