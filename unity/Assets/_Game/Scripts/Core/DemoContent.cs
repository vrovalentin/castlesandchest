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
            new PartInfo("ladder", 5, "Лестница"), new PartInfo("vines", 5, "Лоза"),
            new PartInfo("beacon", 6, "Огонь"), new PartInfo("sign", 6, "Вывеска"), new PartInfo("telescope", 6, "Телескоп"),
            new PartInfo("squirrel", 7, "Белка"), new PartInfo("catsailor", 7, "Кот-моряк"), new PartInfo("fairy", 7, "Фея"),
            new PartInfo("innkeeper", 7, "Трактирщик"), new PartInfo("wizard", 7, "Волшебник"),
            new PartInfo("shrooms", 5, "Грибочки"), new PartInfo("cannon", 5, "Пушка"), new PartInfo("palm", 5, "Пальма"),
            new PartInfo("clock", 6, "Часы"), new PartInfo("bells", 6, "Колокольчики"), new PartInfo("pearl", 6, "Жемчужина"),
            new PartInfo("nest", 6, "Гнездо"),
            new PartInfo("hedgehog", 7, "Ёжик"), new PartInfo("owl", 7, "Сова"), new PartInfo("panda", 7, "Панда"),
            new PartInfo("mermaid", 7, "Русалочка"), new PartInfo("parrot", 7, "Попугай"), new PartInfo("camel", 7, "Верблюжонок"),
        };

        /// <summary>The parts every castle offers; the rest belong to one building each.</summary>
        private static readonly string[] Shared =
            { "clay", "brick", "wall", "floor", "roof", "tiles", "window", "door", "spire", "porch", "lantern", "princess", "knight", "dragon", "unicorn" };

        /// <summary>For the demo every castle offers every shared part of its tier, as in the prototype, plus its own parts.</summary>
        private static Dictionary<int, string[]> AllTiers(params string[] own) =>
            Parts.Where(p => Shared.Contains(p.Id) || own.Contains(p.Id))
                .GroupBy(p => p.Tier).ToDictionary(g => g.Key, g => g.Select(p => p.Id).ToArray());

        private static string[] Repeat(string id, int n) => Enumerable.Repeat(id, n).ToArray();

        /// <summary>Slots written as "clay*6 wall door", in building order.</summary>
        private static string[] Slots(string list) =>
            list.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(s => s.Contains("*") ? Repeat(s.Split('*')[0], int.Parse(s.Split('*')[1])) : new[] { s })
                .ToArray();

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
            slots3.AddRange(new[] { "brick", "brick", "wall", "wall" });             // candy-cane pillars, lower walls
            slots3.AddRange(new[] { "door", "window", "window", "wall", "window" }); // door, windows, gable
            slots3.AddRange(new[] { "roof", "tiles", "chimney", "spire" });          // icing roof, chimney, lollipop
            slots3.AddRange(new[] { "porch", "lantern", "lantern", "baker" });       // jelly steps, lanterns, the baker
            var castle3 = new CastleInfo("castle_3", "Пряничный домик", 30, 0.25, 0.2, slots3.ToArray(), AllTiers("chimney", "baker"));

            // Building 4, the windmill (tools/art/cut_house_4.py).
            var slots4 = new List<string>();
            slots4.AddRange(Repeat("clay", 6));                                       // stone base
            slots4.AddRange(new[] { "wall", "wall", "window", "floor" });            // lower tower, window, balcony
            slots4.AddRange(new[] { "window", "wall" });                              // upper window, upper tower
            slots4.AddRange(Repeat("sails", 4));                                      // the sails hold the roof up in the picture
            slots4.AddRange(new[] { "roof", "tiles", "spire" });
            slots4.AddRange(new[] { "door", "porch", "lantern", "lantern", "miller" });
            var castle4 = new CastleInfo("castle_4", "Мельница", 30, 0.25, 0.2, slots4.ToArray(), AllTiers("sails", "miller"));

            // Buildings 5-10, cut by tools/art/cut_house_N.py; the slots follow their layers.
            var castle5 = new CastleInfo("castle_5", "Домик на дереве", 30, 0.25, 0.2, Slots(
                "clay*6 wall*4 floor floor brick door window window roof chimney " +
                "brick floor door window tiles spire roof tiles roof ladder porch lantern lantern squirrel"), AllTiers("ladder", "chimney", "squirrel"));
            var castle6 = new CastleInfo("castle_6", "Маяк", 30, 0.25, 0.2, Slots(
                "clay*8 wall wall window brick door wall wall window tiles window chimney floor floor " +
                "beacon roof spire porch lantern lantern catsailor"), AllTiers("beacon", "chimney", "catsailor"));
            var castle7 = new CastleInfo("castle_7", "Тыквенный домик", 30, 0.25, 0.2, Slots(
                "clay*8 wall wall door vines window wall wall window window wall window roof tiles spire chimney " +
                "brick window roof spire vines vines porch lantern lantern fairy"), AllTiers("vines", "chimney", "fairy"));
            var castle8 = new CastleInfo("castle_8", "Трактир", 30, 0.25, 0.2, Slots(
                "clay*6 wall wall door window tiles sign floor wall wall window*3 roof tiles wall window*3 chimney " +
                "porch lantern innkeeper"), AllTiers("sign", "chimney", "innkeeper"));
            var castle9 = new CastleInfo("castle_9", "Башня волшебника", 30, 0.25, 0.2, Slots(
                "clay*6 wall wall door wall floor window wall window wall window floor wall window " +
                "roof spire tiles spire roof spire telescope lantern lantern wizard"), AllTiers("telescope", "wizard"));
            var castle10 = new CastleInfo("castle_10", "Ледяной дворец", 30, 0.25, 0.2, Slots(
                "clay*6 wall*4 door window*4 wall floor*3 wall*5 window*5 roof roof tiles tiles roof spire*5 " +
                "porch lantern lantern princess"), AllTiers());

            // Buildings 11-17, cut by tools/art/cut_house_11..17.py. The dragon castle (castle_15) comes last.
            var castle11 = new CastleInfo("castle_11", "Грибной домик", 30, 0.25, 0.2, Slots(
                "clay*6 wall wall door wall wall window brick floor roof floor wall window roof floor roof tiles window chimney " +
                "vines porch lantern*3 shrooms hedgehog"), AllTiers("vines", "chimney", "shrooms", "hedgehog"));
            var castle12 = new CastleInfo("castle_12", "Часовая башня", 30, 0.25, 0.2, Slots(
                "clay*6 wall wall window wall door window roof tiles wall wall clock floor wall roof tiles spire chimney " +
                "porch lantern lantern owl"), AllTiers("clock", "chimney", "owl"));
            var castle13 = new CastleInfo("castle_13", "Пагода", 30, 0.25, 0.2, Slots(
                "clay*6 brick wall wall door window window roof tiles wall window roof wall window tiles spire bells " +
                "porch lantern lantern panda"), AllTiers("bells", "panda"));
            var castle14 = new CastleInfo("castle_14", "Дворец-ракушка", 30, 0.25, 0.2, Slots(
                "clay*6 wall wall tiles tiles window window brick door floor roof roof wall wall wall window spire*4 pearl " +
                "porch lantern lantern mermaid"), AllTiers("pearl", "mermaid"));
            var castle15 = new CastleInfo("castle_15", "Драконья крепость", 30, 0.25, 0.2, Slots(
                "clay*6 wall wall door roof tiles wall wall window window wall sign window floor window window roof tiles " +
                "spire spire roof spire porch lantern lantern nest dragon"), AllTiers("sign", "nest"));
            var castle16 = new CastleInfo("castle_16", "Пиратская крепость", 30, 0.25, 0.2, Slots(
                "clay*6 wall wall cannon brick wall wall door window sign roof tiles window chimney wall window floor ladder floor " +
                "telescope spire porch lantern lantern parrot"), AllTiers("cannon", "sign", "chimney", "ladder", "telescope", "parrot"));
            var castle17 = new CastleInfo("castle_17", "Песчаный дворец", 30, 0.25, 0.2, Slots(
                "clay*6 floor wall wall door window window brick floor wall floor roof spire roof roof wall window tiles roof spire " +
                "palm porch lantern lantern camel"), AllTiers("palm", "camel"));

            var all = new[] { castle1, castle2, castle3, castle4, castle5, castle6, castle7, castle8, castle9, castle10,
                castle11, castle12, castle13, castle16, castle17, castle14, castle15 };
            return new Catalog(new Rules(), Parts, all.Take(Math.Max(1, Math.Min(castles, all.Length))).ToArray());
        }
    }
}
