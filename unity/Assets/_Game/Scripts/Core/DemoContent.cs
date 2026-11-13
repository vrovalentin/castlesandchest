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
            new PartInfo("sled", 5, "Сани"), new PartInfo("emelya", 7, "Емеля"),
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
            // Buildings 1-4, cut by their drawing (tools/art/cut_house_1..4.py); the slots follow their layers.
            var castle1 = new CastleInfo("castle_1", "Башня в 2 этажа", 30, 0.25, 0.2, Slots(
                "clay*4 wall*2 window wall*2 window floor*2 roof tiles spire porch lantern"), AllTiers());
            var castle2 = new CastleInfo("castle_2", "Замок с башнями", 30, 0.25, 0.2, Slots(
                "clay*15 wall*5 door window*2 floor*3 wall*3 window*3 roof*5 spire*5 porch*2 lantern*2 knight princess"), AllTiers());
            var castle3 = new CastleInfo("castle_3", "Пряничный домик", 30, 0.25, 0.2, Slots(
                "clay*6 brick*2 wall*2 door window*2 wall window roof tiles chimney spire porch lantern*2 baker"), AllTiers("chimney", "baker"));
            var castle4 = new CastleInfo("castle_4", "Мельница", 30, 0.25, 0.2, Slots(
                "clay*6 wall*2 window floor wall window sails*4 roof tiles spire door porch lantern*2 miller"), AllTiers("sails", "miller"));

            // Buildings 5-10, cut by tools/art/cut_house_N.py; the slots follow their layers.
            var castle5 = new CastleInfo("castle_5", "Домик на дереве", 30, 0.25, 0.2, Slots(
                "clay*6 wall*4 floor*2 brick door window*2 roof chimney brick floor door window tiles spire roof tiles roof " +
                "ladder porch lantern*2 squirrel"), AllTiers("ladder", "chimney", "squirrel"));
            var castle6 = new CastleInfo("castle_6", "Маяк", 30, 0.25, 0.2, Slots(
                "clay*8 wall*2 window brick door wall*2 window tiles window chimney floor*2 beacon roof spire porch lantern*2 " +
                "catsailor"), AllTiers("beacon", "chimney", "catsailor"));
            var castle7 = new CastleInfo("castle_7", "Тыквенный домик", 30, 0.25, 0.2, Slots(
                "clay*8 wall*2 door vines window wall*2 window*2 wall window roof tiles spire chimney brick window roof spire " +
                "vines*2 porch lantern*2 fairy"), AllTiers("vines", "chimney", "fairy"));
            var castle8 = new CastleInfo("castle_8", "Трактир", 30, 0.25, 0.2, Slots(
                "clay*6 wall*2 door window tiles sign floor wall*2 window*3 roof tiles wall window*3 chimney porch lantern " +
                "innkeeper"), AllTiers("sign", "chimney", "innkeeper"));
            var castle9 = new CastleInfo("castle_9", "Башня волшебника", 30, 0.25, 0.2, Slots(
                "clay*6 wall*2 door wall floor window wall window wall window floor wall window roof spire tiles spire roof " +
                "spire telescope lantern*2 wizard"), AllTiers("telescope", "wizard"));
            var castle10 = new CastleInfo("castle_10", "Ледяной дворец", 30, 0.25, 0.2, Slots(
                "clay*6 wall*7 door window*4 floor*4 roof*3 wall*4 window*2 floor*3 wall*3 window*5 roof*2 tiles*2 roof spire*5 " +
                "porch lantern*2 princess"), AllTiers());

            // Buildings 11-17, cut by tools/art/cut_house_11..17.py. The dragon castle (castle_15) comes last.
            var castle11 = new CastleInfo("castle_11", "Грибной домик", 30, 0.25, 0.2, Slots(
                "clay*6 wall*2 door wall*2 window brick floor*2 roof wall window roof floor roof tiles window chimney vines " +
                "porch lantern*3 shrooms hedgehog"), AllTiers("vines", "chimney", "shrooms", "hedgehog"));
            var castle12 = new CastleInfo("castle_12", "Часовая башня", 30, 0.25, 0.2, Slots(
                "clay*6 wall*2 window wall door window roof tiles wall*2 clock floor wall roof tiles spire chimney porch " +
                "lantern*2 owl"), AllTiers("clock", "chimney", "owl"));
            var castle13 = new CastleInfo("castle_13", "Пагода", 30, 0.25, 0.2, Slots(
                "clay*6 brick wall*2 door window*2 roof tiles wall window roof wall window tiles spire bells porch lantern*2 " +
                "panda"), AllTiers("bells", "panda"));
            var castle14 = new CastleInfo("castle_14", "Дворец-ракушка", 30, 0.25, 0.2, Slots(
                "clay*6 wall*2 tiles*2 window*2 brick door floor roof*2 wall*3 window spire*4 pearl porch lantern*2 mermaid"), AllTiers("pearl", "mermaid"));
            var castle15 = new CastleInfo("castle_15", "Драконья крепость", 30, 0.25, 0.2, Slots(
                "clay*6 wall*2 door roof tiles wall*2 window*2 wall sign window floor window*2 roof tiles spire*2 roof spire " +
                "porch lantern*2 nest dragon"), AllTiers("sign", "nest"));
            var castle16 = new CastleInfo("castle_16", "Пиратская крепость", 30, 0.25, 0.2, Slots(
                "clay*6 floor wall*2 cannon brick wall*2 door window sign roof tiles window chimney wall window floor ladder " +
                "floor telescope spire porch lantern*2 parrot"), AllTiers("cannon", "sign", "chimney", "ladder", "telescope", "parrot"));
            var castle17 = new CastleInfo("castle_17", "Песчаный дворец", 30, 0.25, 0.2, Slots(
                "clay*6 floor wall*2 door window*2 brick floor wall floor roof spire roof*2 wall window tiles roof spire palm " +
                "porch lantern*2 camel"), AllTiers("palm", "camel"));

            // Building 18, the boyar terem, cut by tools/art/cut_house_18.py; it stands in the snow after the ice palace.
            var castle18 = new CastleInfo("castle_18", "Боярский терем", 30, 0.25, 0.2, Slots(
                "clay*6 wall*2 floor*2 wall*2 door porch brick window*4 roof*3 floor*2 wall*5 tiles window*5 floor*2 bells floor " +
                "tiles*2 roof tiles*2 roof*3 spire*5 lantern*2 sled emelya"), AllTiers("bells", "sled", "emelya"));

            // In the order of the kingdom map, along its road: beach, forest and meadows, snow, desert, volcano.
            // The dragon castle on the volcano is the finale.
            var all = new[] { castle1, castle6, castle3, castle4, castle2, castle5, castle7, castle11, castle9, castle10,
                castle18, castle13, castle8, castle17, castle12, castle16, castle14, castle15 };
            return new Catalog(new Rules(), Parts, all.Take(Math.Max(1, Math.Min(castles, all.Length))).ToArray());
        }
    }
}
