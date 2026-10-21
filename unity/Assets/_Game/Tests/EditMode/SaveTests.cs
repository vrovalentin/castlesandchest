using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Core.Tests
{
    public class SaveTests
    {
        private Catalog _cat;
        private GameLogic _game;

        [SetUp]
        public void SetUp()
        {
            _cat = DemoContent.Create();
            _game = new GameLogic(_cat);
        }

        // Goes through Unity's JsonUtility, the same path the game uses.
        private GameState RoundTrip(GameState s)
        {
            string json = JsonUtility.ToJson(SaveMapper.ToSave(s, _cat));
            return SaveMapper.FromSave(JsonUtility.FromJson<SaveData>(json), _cat);
        }

        [Test]
        public void GameWithARunSurvivesSaveAndLoad()
        {
            var rng = new XorShift32(5);
            var s = _game.NewGame();
            _game.DailyGrant(s, "2026-10-21");
            s.Inventory["wall"] = 3;
            s.Inventory["dragon"] = 1;
            s.Filled[0] = true;
            _game.NewRun(s, rng);
            for (int i = 0; i < 5; i++)
            {
                var g = s.Run.Board.AllGroups().Find(x => x.Count >= 2);
                _game.Tap(s, g[0].R, g[0].C, rng);
            }

            var t = RoundTrip(s);

            Assert.AreEqual(s.CastleIndex, t.CastleIndex);
            Assert.AreEqual(3, t.Count("wall"));
            Assert.AreEqual(1, t.Count("dragon"));
            Assert.AreEqual(0, t.Count("clay"));
            CollectionAssert.AreEqual(s.Chests, t.Chests);
            CollectionAssert.AreEqual(s.Filled, t.Filled);
            Assert.AreEqual(s.Tickets, t.Tickets);
            Assert.AreEqual(s.Brooms, t.Brooms);
            Assert.AreEqual("2026-10-21", t.DailyDay);
            Assert.AreEqual(s.Run.Moves, t.Run.Moves);
            CollectionAssert.AreEqual(s.Run.Got, t.Run.Got);
            var a = s.Run.Board; var b = t.Run.Board;
            for (int r = 0; r < a.Rows; r++)
                for (int c = 0; c < a.Cols; c++)
                {
                    Assert.AreEqual(a[r, c].Id, b[r, c].Id);
                    Assert.AreEqual(a[r, c].Kind, b[r, c].Kind);
                    Assert.AreEqual(a[r, c].Color, b[r, c].Color);
                    Assert.AreEqual(a[r, c].Stars, b[r, c].Stars);
                }
            Assert.AreEqual(a.NextId, b.NextId);
        }

        [Test]
        public void GameWithoutRunAndNeverGiftedLoads()
        {
            var t = RoundTrip(_game.NewGame());
            Assert.IsNull(t.Run);
            Assert.IsNull(t.DailyDay);
            Assert.AreEqual(2, t.Chests.Count);
        }

        [Test]
        public void UnknownPartsAreSkippedAndNewerSavesRejected()
        {
            var d = SaveMapper.ToSave(_game.NewGame(), _cat);
            d.partIds = new[] { "clay", "removed_part" };
            d.partCounts = new[] { 2, 5 };
            var s = SaveMapper.FromSave(d, _cat);
            Assert.AreEqual(2, s.Count("clay"));
            Assert.IsFalse(s.Inventory.ContainsKey("removed_part"));

            d.version = GameState.CurrentVersion + 1;
            Assert.IsNull(SaveMapper.FromSave(d, _cat));
        }
    }
}
