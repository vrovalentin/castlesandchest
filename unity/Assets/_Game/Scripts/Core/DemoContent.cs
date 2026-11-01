using System;
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
            // Parts of one building only: each is offered by its own building alone (see Create).
            new PartInfo("sails", 5, "Крыло"),
            new PartInfo("chimney", 6, "Труба"),
            new PartInfo("baker", 7, "Пекарь"), new PartInfo("miller", 7, "Мельник"),
        };

        /// <summary>The parts every castle offers; the rest belong to one building each.</summary>
        private static readonly string[] Shared =
            { "clay", "brick", "wall", "floor", "roof", "tiles", "window", "door", "spire", "porch", "lantern", "princess", "knight", "dragon", "unicorn" };

        /// <summary>For the demo every castle offers every shared part of its tier, as in the prototype, plus its own parts.</summary>
        private static Dictionary<int, string[]> AllTiers(params string[] own) =>
            Parts.Where(p => Shared.Contains(p.Id) || own.Contains(p.Id))
                .GroupBy(p => p.Tier).ToDictionary(g => g.Key, g => g.Select(p => p.Id).ToArray());

        private static string[] Repeat(string id, int n) => Enumerable.Repeat(id, n).ToArray();

        /// <summary>The demo catalog. <paramref name="castles"/> limits it to the first buildings (the prototype has two).</summary>
        public static Catalog Create(int castles = int.MaxValue)
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

            // Building 3, the gingerbread house (tools/art/cut_house_3.py).
            var slots3 = new List<string>();
            slots3.AddRange(Repeat("clay", 6));                                       // cookie stones
            slots3.AddRange(new[] { "wall", "wall", "brick", "brick" });             // lower walls, candy-cane pillars
            slots3.AddRange(new[] { "door", "window", "window", "wall", "window" }); // door, windows, gable
            slots3.AddRange(new[] { "chimney", "roof", "tiles", "spire" });          // chimney, icing roof, lollipop
            slots3.AddRange(new[] { "porch", "lantern", "lantern", "baker" });       // jelly steps, lanterns, the baker
            var castle3 = new CastleInfo("castle_3", "Пряничный домик", 30, 0.25, 0.2, slots3.ToArray(), AllTiers("chimney", "baker"));

            // Building 4, the windmill (tools/art/cut_house_4.py).
            var slots4 = new List<string>();
            slots4.AddRange(Repeat("clay", 6));                                       // stone base
            slots4.AddRange(new[] { "wall", "wall", "window", "floor" });            // lower tower, window, balcony
            slots4.AddRange(new[] { "wall", "window", "roof", "tiles", "spire" });   // upper tower, window, roof, spire
            slots4.AddRange(Repeat("sails", 4));
            slots4.AddRange(new[] { "door", "porch", "lantern", "lantern", "miller" });
            var castle4 = new CastleInfo("castle_4", "Мельница", 30, 0.25, 0.2, slots4.ToArray(), AllTiers("sails", "miller"));

            var all = new[] { castle1, castle2, castle3, castle4 };
            return new Catalog(new Rules(), Parts, all.Take(Math.Max(1, Math.Min(castles, all.Length))).ToArray());
        }
    }
}
