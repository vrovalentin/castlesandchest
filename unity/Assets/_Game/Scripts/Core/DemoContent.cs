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
            new PartInfo("clay", 1, "Глина"),
            new PartInfo("brick", 2, "Кирпич"),
            new PartInfo("wall", 3, "Стенка"),
            new PartInfo("floor", 4, "Перекрытие"), new PartInfo("roof", 4, "Крыша"), new PartInfo("tiles", 4, "Черепица"),
            new PartInfo("window", 5, "Окно"), new PartInfo("door", 5, "Дверь"),
            new PartInfo("spire", 6, "Шпиль"), new PartInfo("porch", 6, "Крыльцо"), new PartInfo("lantern", 6, "Фонарик"),
            new PartInfo("princess", 7, "Принцесса"), new PartInfo("knight", 7, "Рыцарь"), new PartInfo("dragon", 7, "Дракон"), new PartInfo("unicorn", 7, "Единорог"),
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

            // In building order, like the layers in Art/Resources/Castles/castle_2 (tools/art/cut_castle_2.py).
            var slots2 = new List<string>();
            slots2.AddRange(Repeat("clay", 15));                        // grey foundation
            slots2.AddRange(Repeat("wall", 5));                         // lower walls of the five towers
            slots2.AddRange(new[] { "door", "window", "window" });     // gate, lower windows
            slots2.AddRange(Repeat("floor", 3));                        // battlements and the balcony
            slots2.AddRange(Repeat("wall", 3));                         // upper walls
            slots2.AddRange(Repeat("window", 3));                       // upper windows
            slots2.AddRange(Repeat("roof", 5));
            slots2.AddRange(Repeat("spire", 5));
            slots2.AddRange(new[] { "porch", "porch", "lantern", "lantern", "knight", "princess" });
            var castle2 = new CastleInfo("castle_2", "Замок с башнями", 30, 0.25, 0.2, slots2.ToArray(), AllTiers());

            return new Catalog(new Rules(), Parts, new[] { castle1, castle2 });
        }
    }
}
