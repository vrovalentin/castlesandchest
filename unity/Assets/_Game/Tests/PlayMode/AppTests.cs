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

            // Give enough clay for the foundation and place it.
            s.Inventory["clay"] = 4;
            Assert.GreaterOrEqual(_app.Build.PlaceAll(), 4);
            Assert.GreaterOrEqual(s.Filled.Count(f => f), 4);

            // Merge 2 clay into a brick.
            s.Inventory["clay"] = 2;
            Assert.IsTrue(_app.Build.Merge("clay", "clay", "brick"));
            Assert.AreEqual(1, s.Count("brick"));
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
            Assert.AreEqual(36, s.Filled.Length);
            Assert.AreEqual(1, s.Count("dragon"), "figurines stay");
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
    }
}
