using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.View.Tests
{
    public class AppTests
    {
        private GameObject _go;
        private AppController _app;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            AppController.NoSaves = true;
            _go = new GameObject("App");
            _app = _go.AddComponent<AppController>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_go);
            AppController.NoSaves = false;
            yield return null;
        }

        [UnityTest]
        public IEnumerator NewGameStartsOnTheBuildScreen()
        {
            Assert.IsFalse(_app.InMine);
            Assert.IsTrue(_app.Build.enabled);
            Assert.AreEqual(2, _app.Session.State.Chests.Count, "starting gift");
            yield break;
        }

        [UnityTest]
        public IEnumerator ChestsGivePartsAndPartsGoOnTheCastle()
        {
            var s = _app.Session.State;
            var got = _app.Build.OpenChests();
            Assert.AreEqual(0, s.Chests.Count);
            Assert.GreaterOrEqual(got.Count, 2);
            Assert.IsTrue(_app.Build.RewardPanel, "the opening shows what fell out");
            _app.Build.CloseReward();
            Assert.IsFalse(_app.Build.RewardPanel);

            // Give enough clay for the foundation and place it.
            s.Inventory["clay"] = 4;
            Assert.GreaterOrEqual(_app.Build.PlaceAll(), 4);
            Assert.GreaterOrEqual(s.Filled.Count(f => f), 4);

            // A tap on «стенка» in «Осталось поставить» places one wall.
            int filled = s.Filled.Count(f => f);
            s.Inventory["wall"] = 1;
            Assert.IsTrue(_app.Build.PlacePart("wall"));
            Assert.AreEqual(filled + 1, s.Filled.Count(f => f));
            Assert.IsFalse(_app.Build.PlacePart("wall"), "none left in storage");

            // Merge 2 clay into a brick. The starting brown chest may already have given a brick.
            s.Inventory["clay"] = 2;
            s.Inventory["brick"] = 0;
            Assert.IsTrue(_app.Build.Merge("clay", "clay", "brick"));
            Assert.AreEqual(1, s.Count("brick"));
            yield break;
        }

        [UnityTest]
        public IEnumerator TappingTheCastleDrawingFindsItsParts()
        {
            var s = _app.Session.State;
            var art = CastleArt.Load(_app.Session.Game.CastleOf(s));
            Assert.IsNotNull(art);
            var r = new Rect(0, 0, 1000, 1000);
            // The foundation at the bottom left, a window in the middle, the spire on top, nothing in the corner.
            Assert.AreEqual(0, art.SlotAt(r, new Vector2(338, 904), s.Filled, 0));
            Assert.AreEqual(12, art.SlotAt(r, new Vector2(499, 447), s.Filled, 0));
            Assert.AreEqual(15, art.SlotAt(r, new Vector2(500, 92), s.Filled, 0));
            Assert.AreEqual(-1, art.SlotAt(r, new Vector2(20, 20), s.Filled, 0));

            // A tap places the next part when storage has it; a later part or a built one stays as is.
            s.Inventory["clay"] = 1;
            Assert.IsFalse(_app.Build.TapSlot(12));
            Assert.IsTrue(_app.Build.TapSlot(0));
            Assert.IsTrue(s.Filled[0]);
            Assert.IsFalse(_app.Build.TapSlot(0));
            yield break;
        }

        [UnityTest]
        public IEnumerator TheBrowserAddressCanOpenACastleToTry()
        {
            var session = _app.Session;
            session.TestStart("http://192.168.1.235:8080/");
            Assert.AreEqual(0, session.State.CastleIndex, "a plain address changes nothing");
            session.TestStart("http://192.168.1.235:8080/?castle=2&parts");
            Assert.AreEqual(1, session.State.CastleIndex);
            Assert.AreEqual(48, session.State.Filled.Length);
            Assert.AreEqual(15, session.State.Count("clay"), "every part castle 2 needs is in storage");
            yield break;
        }

        [UnityTest]
        public IEnumerator FinishingTheCastleMovesToTheNext()
        {
            var s = _app.Session.State;
            var slots = _app.Session.Game.CastleOf(s).Slots;
            foreach (var id in slots) s.Add(id, 1);
            s.Add("dragon", 1);
            _app.Build.PlaceAll();
            Assert.IsTrue(_app.Build.CastleDonePanel);
            Assert.AreEqual(1, s.Built);
            _app.Build.NextCastle();
            Assert.AreEqual(1, s.CastleIndex);
            Assert.AreEqual(48, s.Filled.Length);
            Assert.AreEqual(1, s.Count("dragon"), "figurines stay");
            yield break;
        }

        [UnityTest]
        public IEnumerator StartOverGoesBackToTheFirstCastle()
        {
            var s = _app.Session.State;
            foreach (var id in _app.Session.Game.CastleOf(s).Slots) s.Add(id, 1);
            _app.Build.PlaceAll();
            _app.Build.NextCastle();
            Assert.AreEqual(1, _app.Session.State.CastleIndex);

            _app.Build.AskStartOver();
            Assert.IsTrue(_app.Build.StartOverPanel);
            _app.Build.StartOver();
            Assert.IsFalse(_app.Build.StartOverPanel);
            var fresh = _app.Session.State;
            Assert.AreEqual(0, fresh.CastleIndex);
            Assert.AreEqual(0, fresh.Built);
            Assert.IsFalse(fresh.Filled.Any(f => f));
            Assert.AreEqual(2, fresh.Chests.Count, "starting gift again");
            Assert.Greater(fresh.Tickets, 0);
            yield break;
        }

        [UnityTest]
        public IEnumerator MineAndBackKeepsTheRun()
        {
            _app.ShowMine();
            yield return null;
            Assert.IsTrue(_app.InMine);
            var run = _app.Session.State.Run;
            Assert.IsNotNull(run);
            Assert.AreEqual(14, _app.Session.State.Tickets);

            _app.ShowBuild();
            yield return null;
            Assert.IsFalse(_app.InMine);
            _app.ShowMine();
            yield return null;
            Assert.AreSame(run, _app.Session.State.Run, "the same run continues");
            Assert.AreEqual(14, _app.Session.State.Tickets, "no second ticket");
            Assert.AreEqual(100, _app.Mine.CellViewCount);
        }

        [UnityTest]
        public IEnumerator FirstCastleHasLayerArt()
        {
            var art = _app.Build.Art;
            Assert.IsNotNull(art, "castle_1 layers in Art/Resources/Castles/castle_1");
            Assert.IsNotNull(art.Base);
            Assert.IsNotNull(art.Full);
            Assert.AreEqual(17, art.Layers.Length);
            Assert.IsTrue(art.Layers.All(t => t != null && t.width == art.Base.width && t.height == art.Base.height),
                "every layer is on the same canvas as the blueprint");
            Assert.IsTrue(art.Lines.All(t => t != null), "every slot has an outline");

            _app.Session.Game.NextCastle(_app.Session.State);
            art = _app.Build.Art;
            Assert.IsNotNull(art, "castle_2 layers in Art/Resources/Castles/castle_2");
            Assert.AreEqual(48, art.Layers.Length);
            Assert.IsTrue(art.Lines.All(t => t != null), "every slot of castle_2 has an outline");
            // A tap on the gate finds it, on castle 2's own tap map.
            var r = new Rect(0, 0, 1000, 1000);
            Assert.AreEqual(20, art.SlotAt(r, new Vector2(500, 780), _app.Session.State.Filled, 0));
            yield return null;
        }
    }
}
