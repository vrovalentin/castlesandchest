using System.Collections.Generic;
using System.Linq;
using Game.Core.Sim;
using NUnit.Framework;

namespace Game.Core.Tests
{
    /// <summary>Returns the given numbers in a loop. For tests that need exact random outcomes.</summary>
    internal sealed class FixedRandom : IRandom
    {
        private readonly double[] _v;
        private int _i;
        public FixedRandom(params double[] v) { _v = v; }
        public double NextDouble() => _v[_i++ % _v.Length];
    }

    public class CoreTests
    {
        private Catalog _cat;
        private GameLogic _game;
        private CastleInfo _castle1;

        [SetUp]
        public void SetUp()
        {
            _cat = DemoContent.Create();
            _game = new GameLogic(_cat);
            _castle1 = _cat.Castles[0];
        }

        // Board with no groups at all: right neighbour differs by 1, lower by 2 (mod 5).
        private Board NoGroupBoard()
        {
            var b = new Board(_cat.Rules);
            for (int r = 0; r < b.Rows; r++)
                for (int c = 0; c < b.Cols; c++)
                    b[r, c] = new Cell { Id = b.NextId++, Kind = CellKind.Block, Color = (r * 2 + c) % 5 };
            return b;
        }

        // ---------- content and random ----------

        [Test]
        public void XorShiftMatchesPrototype()
        {
            var rng = new XorShift32(12345);
            Assert.AreEqual(3336926330u, rng.NextUInt());
            Assert.AreEqual(1697253807u, rng.NextUInt());
            Assert.AreEqual(2816511904u, rng.NextUInt());
        }

        [Test]
        public void DemoContentIsValid()
        {
            CollectionAssert.IsEmpty(_cat.Validate());
            Assert.AreEqual(17, _cat.Castles[0].Slots.Length);
            Assert.AreEqual(48, _cat.Castles[1].Slots.Length);
        }

        [Test]
        public void ValidateCatchesSlotOutsideCastleSet()
        {
            var tiers = new Dictionary<int, string[]>
            {
                { 1, new[] { "clay" } }, { 2, new[] { "brick" } }, { 3, new[] { "wall" } },
                { 4, new[] { "floor" } }, { 5, new[] { "window" } },
            };
            var bad = new CastleInfo("bad", "bad", 30, 0.25, 0.2, new[] { "clay", "roof" }, tiers);
            var cat = new Catalog(new Rules(), DemoContent.Parts, new[] { bad });
            Assert.AreEqual(1, cat.Validate().Count);
        }

        [TestCase(3, 0)]
        [TestCase(4, 1)]
        [TestCase(5, 1)]
        [TestCase(6, 2)]
        [TestCase(9, 3)]
        [TestCase(13, 4)]
        [TestCase(18, 4)]
        [TestCase(19, 5)]
        public void StarsByGroupSize(int size, int stars)
        {
            Assert.AreEqual(stars, _cat.Rules.StarsFor(size));
        }

        // ---------- board ----------

        [Test]
        public void GeneratedBoardIsFullAndHasAMove()
        {
            var b = Board.Generate(_cat.Rules, _castle1, new XorShift32(7));
            Assert.AreEqual(10, b.Rows);
            Assert.AreEqual(10, b.Cols);
            for (int r = 0; r < b.Rows; r++)
                for (int c = 0; c < b.Cols; c++)
                    Assert.IsNotNull(b[r, c]);
            Assert.IsTrue(b.HasMove());
        }

        [Test]
        public void SingleBlockTapDoesNothing()
        {
            var b = NoGroupBoard();
            Assert.IsNull(b.Tap(0, 0, _castle1, new XorShift32(1)));
        }

        [Test]
        public void GroupOfFourInBottomRowGivesOneStarChestAtOnce()
        {
            var b = NoGroupBoard();
            for (int c = 0; c < 4; c++) b[9, c].Color = 0;
            Assert.AreEqual(4, b.Group(9, 0).Count);

            var phases = new List<PhaseKind>();
            var res = b.Tap(9, 0, _castle1, new XorShift32(1), p => phases.Add(p.Kind));

            Assert.AreEqual(4, res.Size);
            Assert.AreEqual(1, res.Chests.Count);
            Assert.AreEqual(new Chest(0, 1), res.Chests[0]);
            Assert.AreEqual(PhaseKind.Remove, phases[0]);
            Assert.Contains(PhaseKind.Collect, phases);
            Assert.Contains(PhaseKind.Refill, phases);
            Assert.AreEqual(0, b.ChestsLeft().Count);
        }

        [Test]
        public void ChestInTopRowStaysOnBoard()
        {
            var b = NoGroupBoard();
            for (int c = 0; c < 4; c++) b[0, c].Color = 3;
            b[1, 1].Color = 4; // keep the group at exactly 4: (1,1) was colour 3
            Assert.AreEqual(4, b.Group(0, 0).Count);
            var res = b.Tap(0, 2, _castle1, new XorShift32(1));
            Assert.AreEqual(0, res.Chests.Count);
            Assert.IsTrue(b[0, 2].IsChest);
            Assert.AreEqual(1, b.ChestsLeft().Count);
        }

        [Test]
        public void ChestFallsAndIsCollected()
        {
            var b = NoGroupBoard();
            b[8, 0] = new Cell { Id = b.NextId++, Kind = CellKind.Chest, Color = 2, Stars = 3 };
            b[9, 0] = null;
            var got = b.Settle(_castle1, new XorShift32(1));
            Assert.AreEqual(1, got.Count);
            Assert.AreEqual(new Chest(2, 3), got[0]);
            for (int r = 0; r < b.Rows; r++) Assert.IsNotNull(b[r, 0]);
        }

        // ---------- chests ----------

        [Test]
        public void ChestTiersByStars()
        {
            var noPremium = new FixedRandom(0.99);
            CollectionAssert.AreEqual(new[] { 3 }, _game.ChestTiers(new Chest(2, 1), noPremium));
            CollectionAssert.AreEqual(new[] { 1, 1 }, _game.ChestTiers(new Chest(0, 2), noPremium));
            CollectionAssert.AreEqual(new[] { 3, 2 }, _game.ChestTiers(new Chest(2, 2), noPremium));
            CollectionAssert.AreEqual(new[] { 3, 3 }, _game.ChestTiers(new Chest(2, 3), noPremium));
            CollectionAssert.AreEqual(new[] { 2, 2 }, _game.ChestTiers(new Chest(1, 4), new FixedRandom(0.5)));
            CollectionAssert.AreEqual(new[] { 2, 2, 3 }, _game.ChestTiers(new Chest(1, 4), new FixedRandom(0.1)));
            CollectionAssert.AreEqual(new[] { 2, 2, 3 }, _game.ChestTiers(new Chest(1, 5), new FixedRandom(0.3)));
            // Top colour: premium is decor (6) or a figurine (7).
            CollectionAssert.AreEqual(new[] { 5, 5, 7 }, _game.ChestTiers(new Chest(4, 5), new FixedRandom(0.1, 0.1)));
            CollectionAssert.AreEqual(new[] { 5, 5, 6 }, _game.ChestTiers(new Chest(4, 5), new FixedRandom(0.1, 0.9)));
        }

        [Test]
        public void ChestGivesThePartTheCastleNeedsMost()
        {
            var s = _game.NewGame();
            // Castle 1, tier 4: floor ×2, roof ×1, tiles ×1 → floor first.
            var got = _game.OpenChest(s, new Chest(3, 1), new FixedRandom(0.5));
            CollectionAssert.AreEqual(new[] { "floor" }, got);
            Assert.AreEqual(1, s.Count("floor"));
        }

        // ---------- storage and castle ----------

        [Test]
        public void MergeRules()
        {
            var s = _game.NewGame();
            s.Inventory["clay"] = 2;
            s.Inventory["wall"] = 1;
            Assert.IsFalse(_game.CanMerge(s, "clay", "wall"), "different tiers");
            Assert.IsFalse(_game.Merge(s, "clay", "clay", "floor"), "target must be next tier");
            Assert.IsTrue(_game.Merge(s, "clay", "clay", "brick"));
            Assert.AreEqual(0, s.Count("clay"));
            Assert.AreEqual(1, s.Count("brick"));
            Assert.IsFalse(_game.CanMerge(s, "clay", "clay"), "nothing left");

            s.Inventory["princess"] = 1;
            s.Inventory["knight"] = 1;
            Assert.IsFalse(_game.CanMerge(s, "princess", "knight"), "top tier does not merge");

            s.Inventory["floor"] = 1;
            s.Inventory["roof"] = 1;
            Assert.IsTrue(_game.Merge(s, "floor", "roof", "door"), "two different parts of one tier");
        }

        [Test]
        public void TheCastleIsBuiltBottomUp()
        {
            var s = _game.NewGame();
            s.Inventory["wall"] = 1;
            Assert.AreEqual(0, _game.NextSlot(s));
            Assert.IsFalse(_game.Place(s, 4), "a wall can't go up before the foundation");
            s.Inventory["clay"] = 4;
            for (int i = 0; i < 4; i++) Assert.IsTrue(_game.Place(s, i));
            Assert.AreEqual(4, _game.NextSlot(s));
            Assert.IsTrue(_game.Place(s, 4));
            Assert.AreEqual("wall", _game.Missing(s)[0].Id, "the next part comes first in what the castle needs");
        }

        [Test]
        public void PlaceNeedsThePartAndFillsOnce()
        {
            var s = _game.NewGame();
            Assert.IsFalse(_game.Place(s, 0));
            s.Inventory["clay"] = 1;
            Assert.IsTrue(_game.Place(s, 0));
            Assert.IsTrue(s.Filled[0]);
            s.Inventory["clay"] = 1;
            Assert.IsFalse(_game.Place(s, 0), "already filled");
            Assert.IsFalse(_game.IsComplete(s));
            Assert.AreEqual(2, _game.Need(s, "clay"), "3 open clay slots minus 1 in storage");
        }

        [Test]
        public void MovingToNextCastleBurnsAllButFigurines()
        {
            var s = _game.NewGame();
            s.Inventory["clay"] = 5;
            s.Inventory["window"] = 2;
            s.Inventory["dragon"] = 1;
            s.Runs = 6;
            var bp = _game.PreviewBurn(s);
            Assert.AreEqual(7, bp.Parts);
            Assert.AreEqual(2, bp.Chests);
            Assert.AreEqual(1, bp.Kept.Count);
            Assert.AreEqual("dragon", bp.Kept[0].Id);

            _game.NextCastle(s);
            Assert.AreEqual(1, s.CastleIndex);
            Assert.AreEqual(0, s.Count("clay"));
            Assert.AreEqual(0, s.Count("window"));
            Assert.AreEqual(1, s.Count("dragon"));
            Assert.AreEqual(0, s.Chests.Count);
            Assert.AreEqual(48, s.Filled.Length);
            Assert.AreEqual(0, s.Runs);
            Assert.IsTrue(_game.IsLastCastle(s));
        }

        // ---------- runs, tools, daily gift ----------

        [Test]
        public void RunCostsATicket()
        {
            var s = _game.NewGame();
            Assert.IsNull(_game.NewRun(s, new XorShift32(1)));
            s.Tickets = 2;
            var run = _game.NewRun(s, new XorShift32(1));
            Assert.IsNotNull(run);
            Assert.AreEqual(1, s.Tickets);
            Assert.AreEqual(30, run.Moves);
            Assert.AreEqual(1, s.Runs);
        }

        [Test]
        public void TapSpendsAMoveAndCollectsChests()
        {
            var s = _game.NewGame();
            int startChests = s.Chests.Count;
            var b = NoGroupBoard();
            for (int c = 0; c < 4; c++) b[9, c].Color = 0;
            s.Run = new RunState { Board = b, Moves = 30 };
            var res = _game.Tap(s, 9, 1, new XorShift32(3));
            Assert.IsNotNull(res);
            Assert.AreEqual(29, s.Run.Moves);
            Assert.AreEqual(startChests + 1, s.Chests.Count);
            Assert.AreEqual(1, s.Run.Got.Count);
        }

        [Test]
        public void BroomClearsRowBlocksButNotChests()
        {
            var s = _game.NewGame();
            var b = NoGroupBoard();
            b[0, 5] = new Cell { Id = b.NextId++, Kind = CellKind.Chest, Color = 1, Stars = 2 };
            s.Run = new RunState { Board = b, Moves = 30 };
            Assert.IsNull(_game.UseTool(s, Tool.Broom, 0, 0, new XorShift32(1)), "no brooms");
            s.Brooms = 1;
            var res = _game.UseTool(s, Tool.Broom, 0, 0, new XorShift32(1));
            Assert.AreEqual(9, res.Size);
            Assert.AreEqual(0, s.Brooms);
            Assert.AreEqual(30, s.Run.Moves, "tools spend no move");
            Assert.AreEqual(1, b.ChestsLeft().Count);
        }

        [Test]
        public void HammerRemovesOneBlockOnly()
        {
            var s = _game.NewGame();
            var b = NoGroupBoard();
            b[4, 4] = new Cell { Id = b.NextId++, Kind = CellKind.Chest, Color = 1, Stars = 2 };
            s.Run = new RunState { Board = b, Moves = 30 };
            s.Hammers = 1;
            Assert.IsNull(_game.UseTool(s, Tool.Hammer, 4, 4, new XorShift32(1)), "chest is not a block");
            var res = _game.UseTool(s, Tool.Hammer, 3, 3, new XorShift32(1));
            Assert.AreEqual(1, res.Size);
            Assert.AreEqual(0, s.Hammers);
        }

        [Test]
        public void DailyGiftTopsUpOncePerDay()
        {
            var s = _game.NewGame();
            var g = _game.DailyGrant(s, "2026-10-20");
            Assert.IsTrue(g.HasValue);
            Assert.AreEqual(3, s.Brooms);
            Assert.AreEqual(3, s.Hammers);
            Assert.AreEqual(15, s.Tickets);
            Assert.IsNull(_game.DailyGrant(s, "2026-10-20"));

            s.Brooms = 1; s.Tickets = 20;
            g = _game.DailyGrant(s, "2026-10-21");
            Assert.AreEqual(2, g.Value.Brooms);
            Assert.AreEqual(0, g.Value.Tickets, "tops up to 15, never takes away");
            Assert.AreEqual(3, s.Brooms);
            Assert.AreEqual(20, s.Tickets);
        }

        [Test]
        public void EndRunBurnsOrKeepsBoardChests()
        {
            var s = _game.NewGame();
            int start = s.Chests.Count;
            var b = NoGroupBoard();
            b[0, 0] = new Cell { Id = b.NextId++, Kind = CellKind.Chest, Color = 1, Stars = 2 };
            s.Run = new RunState { Board = b, Moves = 0 };
            var sum = _game.EndRun(s);
            Assert.AreEqual(1, sum.Left);
            Assert.AreEqual(start, s.Chests.Count);
            Assert.IsNull(s.Run);

            s.Opts.KeepOnEnd = true;
            s.Run = new RunState { Board = b, Moves = 0 };
            _game.EndRun(s);
            Assert.AreEqual(start + 1, s.Chests.Count);
        }

        [Test]
        public void RestartCostsATicketAndKeepsFallenChests()
        {
            var s = _game.NewGame();
            s.Tickets = 1;
            var b = NoGroupBoard();
            b[0, 0] = new Cell { Id = b.NextId++, Kind = CellKind.Chest, Color = 1, Stars = 2 };
            var fallen = new Chest { Color = 0, Stars = 1 };
            s.Chests.Add(fallen);
            s.Run = new RunState { Board = b, Moves = 3 };
            s.Run.Got.Add(fallen);
            int chests = s.Chests.Count;

            var run = _game.Restart(s, new XorShift32(3));
            Assert.IsNotNull(run);
            Assert.AreSame(run, s.Run);
            Assert.AreEqual(0, s.Tickets);
            Assert.AreEqual(s.Opts.Moves, run.Moves);
            Assert.IsEmpty(run.Got);
            Assert.AreEqual(chests, s.Chests.Count, "fallen chest stays, board chest burns");

            Assert.IsNull(_game.Restart(s, new XorShift32(3)), "no ticket");
            Assert.AreSame(run, s.Run, "a refused restart keeps the run");
        }

        // ---------- balance bot ----------

        /// <summary>
        /// Same seed as prototype/sim.js. Expected numbers are what `node` printed for the first 3 games
        /// (castle 1 runs, castle 2 runs, castle 1 chests, castle 2 chests, castle 1 burned chests).
        /// If this fails after a rules change, update both the prototype and these numbers.
        /// </summary>
        [Test]
        public void BotPlaysTheSameGamesAsThePrototype()
        {
            var rng = new XorShift32(12345);
            var bot = new BalanceBot(_game, rng);
            var expected = new[] { new[] { 4, 14, 21, 77, 49 }, new[] { 4, 15, 32, 67, 37 }, new[] { 8, 20, 27, 73, 99 } };
            foreach (var exp in expected)
            {
                var r = bot.PlayGame();
                CollectionAssert.AreEqual(exp, new[] { r[0].Runs, r[1].Runs, r[0].Chests, r[1].Chests, r[0].Burned });
            }
            Assert.AreEqual(3407147384u, rng.State);
        }

        [Test]
        public void EveryCastleIsBuildable()
        {
            var bot = new BalanceBot(_game, new XorShift32(2026));
            var runs = new List<int>[_cat.Castles.Count];
            for (int i = 0; i < runs.Length; i++) runs[i] = new List<int>();
            for (int g = 0; g < 10; g++)
            {
                var res = bot.PlayGame();
                for (int i = 0; i < res.Count; i++) runs[i].Add(res[i].Runs);
            }
            for (int i = 0; i < runs.Length; i++)
            {
                var sorted = runs[i].OrderBy(x => x).ToList();
                TestContext.WriteLine($"{_cat.Castles[i].Id}: median {sorted[sorted.Count / 2]} runs, max {sorted.Last()}");
                Assert.Less(sorted.Last(), BalanceBot.MaxRuns, _cat.Castles[i].Id);
            }
        }
    }
}
