using System.Collections.Generic;
using System.Linq;

namespace Game.Core.Sim
{
    public sealed class CastleResult
    {
        public int Runs;
        public int Chests;
        /// <summary>Chests left on the board at the end of runs (burned unless KeepOnEnd).</summary>
        public int Burned;
        public double AvgGroup;
    }

    /// <summary>
    /// Balance bot, port of prototype/sim.js. It taps the best group, opens every chest,
    /// merges surplus parts upward and places everything it can.
    /// With the same seed it plays exactly the same games as sim.js.
    /// </summary>
    public sealed class BalanceBot
    {
        public const int MaxRuns = 300;
        private readonly GameLogic _game;
        private readonly IRandom _rng;

        public BalanceBot(GameLogic game, IRandom rng) { _game = game; _rng = rng; }

        /// <summary>Score = group size + 3 per chest above it that will drop. Taps the group's lowest cell.</summary>
        public static Pos BestTap(Board b)
        {
            List<Pos> best = null;
            int bestScore = -1;
            foreach (var g in b.AllGroups())
            {
                if (g.Count < 2) continue;
                var top = new Dictionary<int, int>();
                foreach (var p in g) top[p.C] = top.TryGetValue(p.C, out var y) ? System.Math.Min(y, p.R) : p.R;
                int chestsAbove = 0;
                foreach (var kv in top)
                    for (int y = 0; y < kv.Value; y++)
                        if (b[y, kv.Key] != null && b[y, kv.Key].IsChest) chestsAbove++;
                int score = g.Count + 3 * chestsAbove;
                if (score > bestScore) { bestScore = score; best = g; }
            }
            var pick = best[0];
            foreach (var p in best) if (p.R > pick.R) pick = p;
            return pick;
        }

        private int Surplus(GameState s, string id)
        {
            var slots = _game.CastleOf(s).Slots;
            int open = 0;
            for (int i = 0; i < slots.Length; i++) if (slots[i] == id && !s.Filled[i]) open++;
            return s.Count(id) - open;
        }

        private void PlaceAll(GameState s)
        {
            for (int i = 0; i < s.Filled.Length; i++) _game.Place(s, i);
        }

        public void AutoBuild(GameState s)
        {
            var castle = _game.CastleOf(s);
            var rules = _game.Rules;
            PlaceAll(s);
            for (int tier = 1; tier < rules.MaxTier; tier++)
            {
                int t0 = tier;
                bool higherNeed = _game.Catalog.Parts.Any(p => p.Tier > t0 && _game.Need(s, p.Id) > 0);
                if (!higherNeed) break;
                for (;;)
                {
                    var pool = new List<string>();
                    foreach (var p in castle.PartsOfTier(tier))
                        for (int k = 0, n = Surplus(s, p); k < n; k++) pool.Add(p);
                    if (pool.Count < 2) break;
                    var up = castle.PartsOfTier(tier + 1).OrderByDescending(p => _game.Need(s, p)).First();
                    if (!_game.Merge(s, pool[0], pool[1], up)) break;
                    PlaceAll(s);
                }
            }
        }

        public CastleResult PlayCastle(GameState s)
        {
            int chests = 0, burned = 0;
            long sizeSum = 0, taps = 0;
            while (!_game.IsComplete(s) && s.Runs < MaxRuns)
            {
                _game.NewRun(s, _rng);
                while (s.Run.Moves > 0)
                {
                    var t = BestTap(s.Run.Board);
                    var res = _game.Tap(s, t.R, t.C, _rng);
                    sizeSum += res.Size; taps++;
                    chests += res.Chests.Count;
                }
                var sum = _game.EndRun(s);
                if (!sum.Kept) burned += sum.Left;
                while (s.Chests.Count > 0)
                {
                    var c = s.Chests[0];
                    s.Chests.RemoveAt(0);
                    _game.OpenChest(s, c, _rng);
                }
                AutoBuild(s);
            }
            return new CastleResult { Runs = s.Runs, Chests = chests, Burned = burned, AvgGroup = taps > 0 ? (double)sizeSum / taps : 0 };
        }

        /// <summary>Plays every castle of the catalog in order, without ticket limits.</summary>
        public List<CastleResult> PlayGame(bool keepOnEnd = false)
        {
            var s = _game.NewGame();
            s.Tickets = int.MaxValue;
            s.Opts.KeepOnEnd = keepOnEnd;
            var results = new List<CastleResult>();
            for (int i = 0; i < _game.Catalog.Castles.Count; i++)
            {
                if (i > 0) _game.NextCastle(s);
                results.Add(PlayCastle(s));
            }
            return results;
        }
    }
}
