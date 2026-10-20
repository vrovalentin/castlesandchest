using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{
    /// <summary>
    /// The demo castles from prototype/core.js, built in code. Used by tests and the balance bot
    /// until the same content exists as ScriptableObjects.
    /// </summary>
    public static class DemoContent
    {
        public static readonly PartInfo[] Parts =
        {
            new PartInfo("clay", 1),
            new PartInfo("brick", 2),
            new PartInfo("wall", 3),
            new PartInfo("floor", 4), new PartInfo("roof", 4), new PartInfo("tiles", 4),
            new PartInfo("window", 5), new PartInfo("door", 5),
            new PartInfo("spire", 6), new PartInfo("porch", 6), new PartInfo("lantern", 6),
            new PartInfo("princess", 7), new PartInfo("knight", 7), new PartInfo("dragon", 7), new PartInfo("unicorn", 7),
        };

        /// <summary>For the demo every castle offers every part of its tier, as in the prototype.</summary>
        private static Dictionary<int, string[]> AllTiers() =>
            Parts.GroupBy(p => p.Tier).ToDictionary(g => g.Key, g => g.Select(p => p.Id).ToArray());

        private static string[] Repeat(string id, int n) => Enumerable.Repeat(id, n).ToArray();

        public static Catalog Create()
        {
            var castle1 = new CastleInfo("castle_1", "Башня в 2 этажа", 30, 0.25, 0.2,
                new[]
                {
                    "clay", "clay", "clay", "clay",
                    "wall", "wall", "wall", "wall",
                    "floor", "floor",
                    "roof", "tiles",
                    "window", "window",
                    "porch", "spire", "lantern",
                }, AllTiers());

            var slots2 = new List<string>();
            slots2.AddRange(Repeat("clay", 10));
            slots2.AddRange(Repeat("wall", 8));
            slots2.AddRange(Repeat("floor", 3));
            slots2.AddRange(Repeat("window", 6));
            slots2.AddRange(new[] { "porch", "porch", "roof", "roof", "spire", "spire", "lantern", "princess", "knight" });
            var castle2 = new CastleInfo("castle_2", "Замок с башнями", 30, 0.25, 0.2, slots2.ToArray(), AllTiers());

            return new Catalog(new Rules(), Parts, new[] { castle1, castle2 });
        }
    }
}
