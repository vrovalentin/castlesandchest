using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{
    /// <summary>
    /// Meta rules: storage, chests, merging, building the castle, runs, daily gift.
    /// Port of the meta part of prototype/core.js. Holds no state of its own; everything is in GameState.
    /// </summary>
    public sealed class GameLogic
    {
        public readonly Catalog Catalog;
        public Rules Rules => Catalog.Rules;

        public GameLogic(Catalog catalog) { Catalog = catalog; }

        public GameState NewGame()
        {
            var s = new GameState
            {
                CastleIndex = 0,
                Filled = new bool[Catalog.Castles[0].Slots.Length],
            };
            foreach (var p in Catalog.Parts) s.Inventory[p.Id] = 0;
            // Starting gift so the build screen is not empty.
            s.Chests.Add(new Chest(0, 2));
            s.Chests.Add(new Chest(1, 1));
            s.Opts.Moves = Catalog.Castles[0].Moves;
            return s;
        }

        public CastleInfo CastleOf(GameState s) => Catalog.Castles[Math.Min(s.CastleIndex, Catalog.Castles.Count - 1)];

        /// <summary>Open slots of this part minus what is already in storage. Negative = surplus.</summary>
        public int Need(GameState s, string partId)
        {
            var slots = CastleOf(s).Slots;
            int n = 0;
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] == partId && !s.Filled[i]) n++;
            return n - s.Count(partId);
        }

        /// <summary>What the castle still lacks after counting storage, in catalog order.</summary>
        public List<PartCount> Missing(GameState s)
        {
            var output = new List<PartCount>();
            foreach (var p in Catalog.Parts)
            {
                int n = Need(s, p.Id);
                if (n > 0) output.Add(new PartCount(p.Id, n));
            }
            return output;
        }

        /// <summary>The option the castle needs most; random among ties.</summary>
        public string PickNeeded(GameState s, IReadOnlyList<string> options, IRandom rng)
        {
            int best = int.MinValue;
            var list = new List<string>();
            foreach (var o in options)
            {
                int n = Need(s, o);
                if (n > best) { best = n; list.Clear(); list.Add(o); }
                else if (n == best) list.Add(o);
            }
            return list[rng.Range(list.Count)];
        }

        /// <summary>
        /// Tiers a chest of colour T (tier T = color + 1) gives:
        /// ★1 one T; ★2 one T + one T−1; ★3 two T; ★4 two T + 20% premium; ★5 two T + 40% premium.
        /// Premium is one tier higher; for the top colour it is decor (6) or a figurine (7).
        /// </summary>
        public List<int> ChestTiers(Chest chest, IRandom rng)
        {
            int t = chest.Color + 1;
            var tiers = chest.Stars <= 1 ? new List<int> { t }
                : chest.Stars == 2 ? new List<int> { t, Math.Max(1, t - 1) }
                : new List<int> { t, t };
            double p = chest.Stars >= 0 && chest.Stars < Rules.PremiumChance.Length ? Rules.PremiumChance[chest.Stars] : 0;
            if (p > 0 && rng.NextDouble() < p)
            {
                if (t >= Rules.ColorTiers) tiers.Add(rng.NextDouble() < Rules.FigureShare ? 7 : 6);
                else tiers.Add(t + 1);
            }
            return tiers;
        }

        /// <summary>Opens a chest into storage. Returns the part ids it gave.</summary>
        public List<string> OpenChest(GameState s, Chest chest, IRandom rng)
        {
            var output = new List<string>();
            var castle = CastleOf(s);
            foreach (int t in ChestTiers(chest, rng))
            {
                var options = castle.PartsOfTier(t);
                if (options.Count == 0) continue; // castle has no parts on this tier
                var id = PickNeeded(s, options, rng);
                s.Add(id, 1);
                output.Add(id);
            }
            return output;
        }

        /// <summary>Any 2 parts of one tier → 1 part of the next tier.</summary>
        public bool CanMerge(GameState s, string a, string b)
        {
            int ta = Catalog.TierOf(a), tb = Catalog.TierOf(b);
            if (ta == 0 || ta != tb || ta >= Rules.MaxTier) return false;
            return a == b ? s.Count(a) >= 2 : s.Count(a) >= 1 && s.Count(b) >= 1;
        }

        public bool Merge(GameState s, string a, string b, string target)
        {
            if (!CanMerge(s, a, b) || Catalog.TierOf(target) != Catalog.TierOf(a) + 1) return false;
            s.Add(a, -1); s.Add(b, -1); s.Add(target, 1);
            return true;
        }

        public bool Place(GameState s, int slot)
        {
            var slots = CastleOf(s).Slots;
            if (slot < 0 || slot >= slots.Length) return false;
            var id = slots[slot];
            if (s.Filled[slot] || s.Count(id) < 1) return false;
            s.Add(id, -1);
            s.Filled[slot] = true;
            return true;
        }

        public bool IsComplete(GameState s) => s.Filled.All(f => f);

        public bool IsLastCastle(GameState s) => s.CastleIndex >= Catalog.Castles.Count - 1;

        /// <summary>What moving to the next castle will burn and keep.</summary>
        public BurnPreview PreviewBurn(GameState s)
        {
            var bp = new BurnPreview { Chests = s.Chests.Count };
            foreach (var p in Catalog.Parts)
            {
                int n = s.Count(p.Id);
                if (p.Tier < Rules.KeepTier) bp.Parts += n;
                else if (n > 0) bp.Kept.Add(new PartCount(p.Id, n));
            }
            return bp;
        }

        /// <summary>Storage below KeepTier and closed chests burn; figurines stay.</summary>
        public void NextCastle(GameState s)
        {
            foreach (var id in s.Inventory.Keys.ToList())
                if (Catalog.TierOf(id) < Rules.KeepTier) s.Inventory[id] = 0;
            s.Chests.Clear();
            s.CastleIndex = Math.Min(s.CastleIndex + 1, Catalog.Castles.Count - 1);
            s.Filled = new bool[CastleOf(s).Slots.Length];
            s.Runs = 0;
        }

        /// <summary>A run costs one ticket. Null if there are no tickets.</summary>
        public RunState NewRun(GameState s, IRandom rng)
        {
            if (s.Tickets < 1) return null;
            if (s.Tickets != int.MaxValue) s.Tickets--;
            var castle = CastleOf(s);
            s.Run = new RunState { Board = Board.Generate(Rules, castle, rng), Moves = s.Opts.Moves };
            s.Runs++;
            return s.Run;
        }

        /// <summary>One tap in the current run: spends a move, chests collected go to the player.</summary>
        public MoveResult Tap(GameState s, int r, int c, IRandom rng, Action<Phase> onPhase = null)
        {
            if (s.Run == null || s.Run.Moves <= 0) return null;
            var res = s.Run.Board.Tap(r, c, CastleOf(s), rng, onPhase);
            if (res == null) return null;
            s.Run.Moves--;
            Collect(s, res.Chests);
            return res;
        }

        /// <summary>Broom or hammer: spends the tool, not a move.</summary>
        public MoveResult UseTool(GameState s, Tool tool, int r, int c, IRandom rng, Action<Phase> onPhase = null)
        {
            if (s.Run == null) return null;
            if (tool == Tool.Broom ? s.Brooms < 1 : s.Hammers < 1) return null;
            var res = s.Run.Board.UseTool(tool, r, c, CastleOf(s), rng, onPhase);
            if (res == null) return null;
            if (tool == Tool.Broom) s.Brooms--; else s.Hammers--;
            Collect(s, res.Chests);
            return res;
        }

        private static void Collect(GameState s, List<Chest> chests)
        {
            s.Chests.AddRange(chests);
            s.Run.Got.AddRange(chests);
        }

        /// <summary>End of a run. Chests left on the board burn unless Opts.KeepOnEnd.</summary>
        public RunSummary EndRun(GameState s)
        {
            var left = s.Run.Board.ChestsLeft();
            if (s.Opts.KeepOnEnd) { s.Chests.AddRange(left); s.Run.Got.AddRange(left); }
            var summary = new RunSummary { Got = s.Run.Got, Left = left.Count, Kept = s.Opts.KeepOnEnd };
            s.Run = null;
            return summary;
        }

        /// <summary>Once a day tools and tickets are topped up to the daily amounts. Null if already given today.</summary>
        public DailyGift? DailyGrant(GameState s, string day)
        {
            if (s.DailyDay == day) return null;
            var got = new DailyGift
            {
                Brooms = Math.Max(0, Rules.DailyBrooms - s.Brooms),
                Hammers = Math.Max(0, Rules.DailyHammers - s.Hammers),
                Tickets = Math.Max(0, Rules.DailyTickets - s.Tickets),
            };
            s.Brooms += got.Brooms;
            s.Hammers += got.Hammers;
            s.Tickets += got.Tickets;
            s.DailyDay = day;
            return got;
        }
    }
}
