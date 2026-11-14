using System.Collections.Generic;

namespace Game.View
{
    /// <summary>
    /// Who lives in each building and what they say: a wish on the map before building, thanks when it is built.
    /// The portrait is the building's own figurine part. Texts from plan/zhiteli.md in the project files.
    /// </summary>
    public static class Residents
    {
        public sealed class Resident
        {
            /// <summary>Part id of the figurine used as the portrait; null when the narrator speaks.</summary>
            public string Portrait;
            public string Start, Done;
        }

        private static readonly Dictionary<string, Resident> ByCastle = new Dictionary<string, Resident>
        {
            ["castle_1"] = new Resident { Start = "Начнём с маленькой башни. С неё видно всё королевство!", Done = "Башня готова! Сверху видно, куда идти дальше." },
            ["castle_2"] = new Resident { Portrait = "princess", Start = "Принцесса устала жить в палатке. Рыцарь тоже, но молчит.", Done = "Принцесса довольна. Рыцарь наконец снял доспехи." },
            ["castle_3"] = new Resident { Portrait = "baker", Start = "Пекарю негде печь пряники. Тесто уже убегает!", Done = "Пахнет корицей! Первый пряник — тебе." },
            ["castle_4"] = new Resident { Portrait = "miller", Start = "Мельник три дня ждёт ветра. Ветер есть, мельницы нет.", Done = "Крылья крутятся! Будет мука для пекаря." },
            ["castle_5"] = new Resident { Portrait = "squirrel", Start = "Белка спрятала орехи и забыла где. Нужен дом с кладовкой.", Done = "Белка переехала. Орехи нашлись — все сорок два." },
            ["castle_6"] = new Resident { Portrait = "catsailor", Start = "Кот-моряк видит корабли, а они его — нет. Нужен огонь!", Done = "Огонь горит. Корабли машут коту." },
            ["castle_7"] = new Resident { Portrait = "fairy", Start = "Фея вырастила тыкву размером с дом. Надо же в ней жить!", Done = "Фея счастлива. Только не зови её в гости в полночь." },
            ["castle_8"] = new Resident { Portrait = "innkeeper", Start = "Путникам негде отдохнуть, а у трактирщика остывает суп.", Done = "Двери открыты! Суп горячий, лавки тёплые." },
            ["castle_9"] = new Resident { Portrait = "wizard", Start = "Волшебнику нужна башня повыше: звёзды плохо видно.", Done = "Звёзды как на ладони. Волшебник обещал не взрывать её." },
            ["castle_10"] = new Resident { Portrait = "princess", Start = "Снежная принцесса мёрзнет без дворца. Да, так бывает.", Done = "Принцесса нарядилась в иней и машет из окна." },
            ["castle_18"] = new Resident { Portrait = "emelya", Start = "Емеля приехал на печи. Печь есть, а терема нет.", Done = "Терем готов! Печь заехала в горницу и греет всех." },
            ["castle_11"] = new Resident { Portrait = "hedgehog", Start = "Ёжик набрал грибов на целый дом. Так и построим!", Done = "Ёжик дома. Грибы, правда, чуть-чуть надкусаны." },
            ["castle_12"] = new Resident { Portrait = "owl", Start = "Сова путает день и ночь. Нужны часы!", Done = "Часы идут. Сова спит днём, как положено." },
            ["castle_13"] = new Resident { Portrait = "panda", Start = "Панда ищет тихое место, чтобы жевать бамбук.", Done = "Тихо, звенят колокольчики. Панда жуёт." },
            ["castle_14"] = new Resident { Portrait = "mermaid", Start = "Русалочка нашла жемчужину. Ей нужен дворец под стать.", Done = "Жемчужина сияет над морем. Русалочка поёт." },
            ["castle_15"] = new Resident { Portrait = "dragon", Start = "Дракон охраняет гнездо, а у гнезда нет крыши.", Done = "Дракон доволен и почти не дымит. Королевство построено!" },
            ["castle_16"] = new Resident { Portrait = "parrot", Start = "Пираты уплыли, а попугая забыли. Он не против своей крепости.", Done = "Попугай кричит «Полный вперёд!» всем прохожим." },
            ["castle_17"] = new Resident { Portrait = "camel", Start = "Верблюжонок пришёл из пустыни и очень хочет тень.", Done = "В тени прохладно. Верблюжонок улыбается." },
        };

        private static readonly Resident Nobody = new Resident { Start = "Здесь будет новая постройка.", Done = "Готово!" };

        public static Resident Of(string castleId) => castleId != null && ByCastle.TryGetValue(castleId, out var r) ? r : Nobody;
    }
}
