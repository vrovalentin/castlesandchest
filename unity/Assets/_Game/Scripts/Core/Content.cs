using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{
    // Plain content models. Game.Content turns ScriptableObjects into these, so Core never sees Unity types.
    // Rules know only tiers; which parts exist on each tier is decided by the castle (design/architecture.md).

    /// <summary>One part type. Id never changes once shipped, saves use it.</summary>
    public sealed class PartInfo
    {
        public readonly string Id;
        public readonly int Tier;
        /// <summary>Display name (later a localisation key).</summary>
        public readonly string Name;
        public PartInfo(string id, int tier, string name = null) { Id = id; Tier = tier; Name = name ?? id; }
    }

    public sealed class CastleInfo
    {
        public readonly string Id;
        public readonly string Name;
        public readonly int Moves;
        /// <summary>Share of the cheapest colour (tier 1) among new blocks.</summary>
        public readonly double TrashShare;
        /// <summary>Chance that a falling block copies the colour of the block below it.</summary>
        public readonly double HelpChance;
        /// <summary>Part id for every place on the castle picture, in picture order.</summary>
        public readonly string[] Slots;
        private readonly Dictionary<int, string[]> _tierParts;

        public CastleInfo(string id, string name, int moves, double trashShare, double helpChance,
            string[] slots, Dictionary<int, string[]> tierParts)
        {
            Id = id; Name = name; Moves = moves; TrashShare = trashShare; HelpChance = helpChance;
            Slots = slots; _tierParts = tierParts;
        }

        /// <summary>Parts this castle offers on a tier, in a fixed order.</summary>
        public IReadOnlyList<string> PartsOfTier(int tier) =>
            _tierParts.TryGetValue(tier, out var p) ? p : Array.Empty<string>();
    }

    public sealed class Rules
    {
        public int Rows = 10;
        public int Cols = 10;
        public int MaxTier = 7;
        /// <summary>Tiers 1..ColorTiers drop from chests of their own colour; colour index = tier - 1.</summary>
        public int ColorTiers = 5;
        /// <summary>Parts of this tier and higher survive the move to the next castle (figurines).</summary>
        public int KeepTier = 7;
        /// <summary>Smallest group that leaves a chest.</summary>
        public int ChestMinGroup = 4;
        /// <summary>Group size needed for 1..5 stars.</summary>
        public int[] StarThresholds = { 4, 6, 9, 13, 19 };
        /// <summary>Chance of a premium part by stars (index = stars).</summary>
        public double[] PremiumChance = { 0, 0, 0, 0, 0.2, 0.4 };
        /// <summary>Share of figurines (tier 7) among premium parts of the top-colour chest.</summary>
        public double FigureShare = 0.25;
        /// <summary>Brooms and hammers added every day (they pile up).</summary>
        public int DailyBrooms = 3;
        public int DailyHammers = 3;
        /// <summary>Tickets are topped up to this every day.</summary>
        public int DailyTickets = 15;
        /// <summary>Extra gift by day of the login streak (1..7); a missed day starts it over from day 1.</summary>
        public DailyGift[] Calendar =
        {
            new DailyGift { Tickets = 3 },
            new DailyGift { Brooms = 1, Hammers = 1 },
            new DailyGift { Tickets = 5 },
            new DailyGift { Hammers = 2 },
            new DailyGift { Brooms = 2 },
            new DailyGift { Tickets = 8 },
            new DailyGift { Brooms = 3, Hammers = 3, Tickets = 10 },
        };
        /// <summary>Brooms and hammers (each) for watching an ad, and how many such ads a day.</summary>
        public int AdTools = 3;
        public int AdToolsPerDay = 1;
        /// <summary>Ice and stones on the board from this castle on (0-based: 2 = the third castle).</summary>
        public int ObstaclesFrom = 2;
        /// <summary>Tickets given for moving on to the next building.</summary>
        public int TicketsPerCastle = 2;
        /// <summary>Tickets for watching an ad when they ran out; as many ads as the player likes.</summary>
        public int AdTickets = 3;

        public int ColorCount => ColorTiers;

        /// <summary>Iced blocks at the start of a run on castle <paramref name="castle"/> (0-based).</summary>
        public int IceFor(int castle) => castle < ObstaclesFrom ? 0 : Math.Min(8, 3 + (castle - ObstaclesFrom));

        /// <summary>Stones at the start of a run on castle <paramref name="castle"/> (0-based).</summary>
        public int StonesFor(int castle) => castle < ObstaclesFrom ? 0 : Math.Min(3, 1 + (castle - ObstaclesFrom) / 4);

        public int StarsFor(int size)
        {
            for (int s = StarThresholds.Length; s >= 1; s--)
                if (size >= StarThresholds[s - 1]) return s;
            return 0;
        }
    }

    public sealed class Catalog
    {
        public readonly Rules Rules;
        public readonly IReadOnlyList<CastleInfo> Castles;
        /// <summary>Every part the game knows, in a fixed order (used for lists and stable iteration).</summary>
        public readonly IReadOnlyList<PartInfo> Parts;
        private readonly Dictionary<string, PartInfo> _byId;

        public Catalog(Rules rules, IReadOnlyList<PartInfo> parts, IReadOnlyList<CastleInfo> castles)
        {
            Rules = rules; Parts = parts; Castles = castles;
            _byId = parts.ToDictionary(p => p.Id);
        }

        public bool TryGetPart(string id, out PartInfo part) => _byId.TryGetValue(id, out part);

        public int TierOf(string id) => _byId.TryGetValue(id, out var p) ? p.Tier : 0;

        public string NameOf(string id) => _byId.TryGetValue(id, out var p) ? p.Name : id;

        /// <summary>Index of the castle with this id, or -1.</summary>
        public int CastleIndexOf(string id)
        {
            for (int i = 0; i < Castles.Count; i++) if (Castles[i].Id == id) return i;
            return -1;
        }

        /// <summary>Content problems a designer must fix. Empty list means the catalog is fine.</summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (Castles.Count == 0) errors.Add("no castles");
            foreach (var c in Castles)
            {
                for (int t = 1; t <= Rules.ColorTiers; t++)
                    if (c.PartsOfTier(t).Count == 0) errors.Add($"{c.Id}: no parts on tier {t}");
                for (int t = 1; t <= Rules.MaxTier; t++)
                    foreach (var id in c.PartsOfTier(t))
                    {
                        if (!_byId.TryGetValue(id, out var p)) errors.Add($"{c.Id}: unknown part {id}");
                        else if (p.Tier != t) errors.Add($"{c.Id}: {id} is tier {p.Tier}, listed on tier {t}");
                    }
                foreach (var id in c.Slots)
                {
                    int t = TierOf(id);
                    if (t == 0 || !c.PartsOfTier(t).Contains(id)) errors.Add($"{c.Id}: slot part {id} is not in the castle's set");
                }
            }
            return errors;
        }
    }
}
