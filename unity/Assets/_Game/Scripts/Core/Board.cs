using System;
using System.Collections.Generic;

namespace Game.Core
{
    public enum CellKind { Block, Chest }

    /// <summary>A block or a chest on the board. Id is stable while it moves, for animations.</summary>
    public sealed class Cell
    {
        public int Id;
        public CellKind Kind;
        /// <summary>Colour index, 0 = cheapest (tier 1).</summary>
        public int Color;
        /// <summary>Only for chests.</summary>
        public int Stars;

        public bool IsBlock => Kind == CellKind.Block;
        public bool IsChest => Kind == CellKind.Chest;
    }

    /// <summary>A collected chest, not yet opened.</summary>
    public struct Chest
    {
        public int Color;
        public int Stars;
        public Chest(int color, int stars) { Color = color; Stars = stars; }
        public override string ToString() => $"chest(c{Color},★{Stars})";
    }

    public struct Pos
    {
        public int R, C;
        public Pos(int r, int c) { R = r; C = c; }
    }

    public enum PhaseKind { Remove, Gravity, Collect, Refill, Shuffle }

    public struct Spawn
    {
        public int Id;
        /// <summary>Row the block falls from (negative = above the board).</summary>
        public int From;
    }

    /// <summary>What happened in one phase of a move. The view plays an animation per phase.</summary>
    public sealed class Phase
    {
        public PhaseKind Kind;
        public List<int> Removed;
        public Cell NewChest;
        public int Size;
        public Pos At;
        public List<Chest> Collected;
        public List<Spawn> Spawns;
    }

    public sealed class MoveResult
    {
        public int Size;
        public int Color;
        public List<Chest> Chests;
    }

    public enum Tool { Broom, Hammer }

    /// <summary>The mining board: groups, chests, gravity, refill. Port of the board part of prototype/core.js.</summary>
    public sealed class Board
    {
        public readonly int Rows;
        public readonly int Cols;
        private readonly Cell[,] _cells;
        private readonly Rules _rules;
        public int NextId = 1;

        public Board(Rules rules)
        {
            _rules = rules;
            Rows = rules.Rows;
            Cols = rules.Cols;
            _cells = new Cell[Rows, Cols];
        }

        public Cell this[int r, int c]
        {
            get => _cells[r, c];
            set => _cells[r, c] = value;
        }

        private Cell MakeBlock(int color) => new Cell { Id = NextId++, Kind = CellKind.Block, Color = color };

        private int RandColor(CastleInfo castle, IRandom rng)
        {
            if (rng.NextDouble() < castle.TrashShare) return 0;
            return 1 + rng.Range(_rules.ColorCount - 1);
        }

        public static Board Generate(Rules rules, CastleInfo castle, IRandom rng)
        {
            var b = new Board(rules);
            for (int r = 0; r < b.Rows; r++)
                for (int c = 0; c < b.Cols; c++)
                    b._cells[r, c] = b.MakeBlock(b.RandColor(castle, rng));
            if (!b.HasMove()) b.Shuffle(rng);
            return b;
        }

        private bool SameBlock(Cell a, Cell b) => a != null && b != null && a.IsBlock && b.IsBlock && a.Color == b.Color;

        public bool HasMove()
        {
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                {
                    var x = _cells[r, c];
                    if (x == null || !x.IsBlock) continue;
                    if (c + 1 < Cols && SameBlock(x, _cells[r, c + 1])) return true;
                    if (r + 1 < Rows && SameBlock(x, _cells[r + 1, c])) return true;
                }
            return false;
        }

        public void Shuffle(IRandom rng)
        {
            var pos = new List<Pos>();
            var blocks = new List<Cell>();
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    if (_cells[r, c] != null && _cells[r, c].IsBlock) { pos.Add(new Pos(r, c)); blocks.Add(_cells[r, c]); }
            for (int t = 0; t < 20; t++)
            {
                for (int i = blocks.Count - 1; i > 0; i--)
                {
                    int j = rng.Range(i + 1);
                    (blocks[i], blocks[j]) = (blocks[j], blocks[i]);
                }
                for (int i = 0; i < pos.Count; i++) _cells[pos[i].R, pos[i].C] = blocks[i];
                if (HasMove()) return;
            }
            // Could not shuffle into a move: force one pair.
            foreach (var p in pos)
            {
                var self = _cells[p.R, p.C];
                if (p.C + 1 < Cols && _cells[p.R, p.C + 1] != null && _cells[p.R, p.C + 1].IsBlock) { _cells[p.R, p.C + 1].Color = self.Color; return; }
                if (p.R + 1 < Rows && _cells[p.R + 1, p.C] != null && _cells[p.R + 1, p.C].IsBlock) { _cells[p.R + 1, p.C].Color = self.Color; return; }
            }
        }

        private static readonly int[] Dy = { 1, -1, 0, 0 };
        private static readonly int[] Dx = { 0, 0, 1, -1 };

        /// <summary>Connected blocks of the same colour starting at (r, c). Empty for chests and empty cells.</summary>
        public List<Pos> Group(int r, int c)
        {
            var output = new List<Pos>();
            if (r < 0 || r >= Rows || c < 0 || c >= Cols) return output;
            var start = _cells[r, c];
            if (start == null || !start.IsBlock) return output;
            var seen = new HashSet<int> { r * Cols + c };
            var stack = new List<Pos> { new Pos(r, c) };
            while (stack.Count > 0)
            {
                var p = stack[stack.Count - 1];
                stack.RemoveAt(stack.Count - 1);
                output.Add(p);
                for (int d = 0; d < 4; d++)
                {
                    int ny = p.R + Dy[d], nx = p.C + Dx[d];
                    if (ny < 0 || ny >= Rows || nx < 0 || nx >= Cols) continue;
                    var cell = _cells[ny, nx];
                    if (cell == null || !cell.IsBlock || cell.Color != start.Color || seen.Contains(ny * Cols + nx)) continue;
                    seen.Add(ny * Cols + nx);
                    stack.Add(new Pos(ny, nx));
                }
            }
            return output;
        }

        /// <summary>Every group on the board, for the "how many stars" hint.</summary>
        public List<List<Pos>> AllGroups()
        {
            var seen = new HashSet<int>();
            var output = new List<List<Pos>>();
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                {
                    if (seen.Contains(r * Cols + c)) continue;
                    var g = Group(r, c);
                    foreach (var p in g) seen.Add(p.R * Cols + p.C);
                    if (g.Count > 0) output.Add(g);
                }
            return output;
        }

        public bool Gravity()
        {
            bool moved = false;
            for (int c = 0; c < Cols; c++)
            {
                int w = Rows - 1;
                for (int r = Rows - 1; r >= 0; r--)
                {
                    if (_cells[r, c] == null) continue;
                    if (w != r) { _cells[w, c] = _cells[r, c]; _cells[r, c] = null; moved = true; }
                    w--;
                }
            }
            return moved;
        }

        public List<Cell> CollectBottom()
        {
            var got = new List<Cell>();
            for (int c = 0; c < Cols; c++)
            {
                var x = _cells[Rows - 1, c];
                if (x != null && x.IsChest) { got.Add(x); _cells[Rows - 1, c] = null; }
            }
            return got;
        }

        public List<Spawn> Refill(CastleInfo castle, IRandom rng)
        {
            var spawns = new List<Spawn>();
            for (int c = 0; c < Cols; c++)
            {
                int empty = 0;
                while (empty < Rows && _cells[empty, c] == null) empty++;
                for (int r = empty - 1; r >= 0; r--)
                {
                    var below = r + 1 < Rows ? _cells[r + 1, c] : null;
                    bool help = below != null && below.IsBlock && rng.NextDouble() < castle.HelpChance;
                    var blk = MakeBlock(help ? below.Color : RandColor(castle, rng));
                    _cells[r, c] = blk;
                    spawns.Add(new Spawn { Id = blk.Id, From = r - empty });
                }
            }
            return spawns;
        }

        /// <summary>One tap. Null if the tap does nothing (group smaller than 2).</summary>
        public MoveResult Tap(int r, int c, CastleInfo castle, IRandom rng, Action<Phase> onPhase = null)
        {
            var g = Group(r, c);
            if (g.Count < 2) return null;
            int color = _cells[r, c].Color;
            var removed = new List<int>(g.Count);
            foreach (var p in g) { removed.Add(_cells[p.R, p.C].Id); _cells[p.R, p.C] = null; }
            Cell chest = null;
            if (g.Count >= _rules.ChestMinGroup)
            {
                chest = new Cell { Id = NextId++, Kind = CellKind.Chest, Color = color, Stars = _rules.StarsFor(g.Count) };
                _cells[r, c] = chest;
            }
            onPhase?.Invoke(new Phase { Kind = PhaseKind.Remove, Removed = removed, NewChest = chest, Size = g.Count, At = new Pos(r, c) });
            var chests = Settle(castle, rng, onPhase);
            return new MoveResult { Size = g.Count, Color = color, Chests = chests };
        }

        /// <summary>Broom clears the blocks of a row, hammer one block. They spend no move and make no chest.</summary>
        public MoveResult UseTool(Tool tool, int r, int c, CastleInfo castle, IRandom rng, Action<Phase> onPhase = null)
        {
            var cells = new List<Pos>();
            if (tool == Tool.Broom)
            {
                for (int x = 0; x < Cols; x++)
                    if (_cells[r, x] != null && _cells[r, x].IsBlock) cells.Add(new Pos(r, x));
            }
            else if (_cells[r, c] != null && _cells[r, c].IsBlock) cells.Add(new Pos(r, c));
            if (cells.Count == 0) return null;
            var removed = new List<int>(cells.Count);
            foreach (var p in cells) { removed.Add(_cells[p.R, p.C].Id); _cells[p.R, p.C] = null; }
            onPhase?.Invoke(new Phase { Kind = PhaseKind.Remove, Removed = removed, Size = cells.Count, At = new Pos(r, c) });
            return new MoveResult { Size = cells.Count, Color = -1, Chests = Settle(castle, rng, onPhase) };
        }

        /// <summary>After cells are removed: fall, collect chests from the bottom row (cascading), refill, shuffle.</summary>
        public List<Chest> Settle(CastleInfo castle, IRandom rng, Action<Phase> onPhase = null)
        {
            var collected = new List<Chest>();
            for (;;)
            {
                if (Gravity()) onPhase?.Invoke(new Phase { Kind = PhaseKind.Gravity });
                var got = CollectBottom();
                if (got.Count == 0) break;
                var batch = new List<Chest>();
                foreach (var x in got) batch.Add(new Chest(x.Color, x.Stars));
                collected.AddRange(batch);
                onPhase?.Invoke(new Phase { Kind = PhaseKind.Collect, Collected = batch });
            }
            var spawns = Refill(castle, rng);
            onPhase?.Invoke(new Phase { Kind = PhaseKind.Refill, Spawns = spawns });
            if (!HasMove()) { Shuffle(rng); onPhase?.Invoke(new Phase { Kind = PhaseKind.Shuffle }); }
            return collected;
        }

        /// <summary>Chests still on the board (they burn at the end of a run unless the option keeps them).</summary>
        public List<Chest> ChestsLeft()
        {
            var left = new List<Chest>();
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    if (_cells[r, c] != null && _cells[r, c].IsChest) left.Add(new Chest(_cells[r, c].Color, _cells[r, c].Stars));
            return left;
        }
    }
}
