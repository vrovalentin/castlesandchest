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
            // Map order: beach, forest, snow, desert, volcano (see MapPoints in BuildScreen.Map.cs).
            CollectionAssert.AreEqual(new[] { 1, 6, 3, 4, 2, 5, 7, 11, 9, 10, 18, 13, 8, 17, 12, 16, 14, 15 },
                _cat.Castles.Select(c => int.Parse(c.Id.Substring("castle_".Length))).ToArray());
            CollectionAssert.AreEqual(new[] { 17, 28, 23, 24, 48, 32, 33, 31, 29, 56, 56, 26, 28, 30, 27, 31, 30, 32 },
                _cat.Castles.Select(c => c.Slots.Length).ToArray());
            Assert.AreEqual(18, _cat.Castles.Count);
            Assert.AreEqual("castle_15", _cat.Castles[17].Id, "the dragon castle is the finale");
            CollectionAssert.DoesNotContain(_cat.Castles[0].PartsOfTier(7), "baker", "a building's own parts stay in that building");
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

        // ---------- points ----------

        [Test]
        public void TapsAndChestsScoreForTheRunAndTheTotal()
        {
            var s = _game.NewGame();
            var b = NoGroupBoard();
            for (int c = 0; c < 4; c++) b[9, c].Color = 0; // a group of 4 in the bottom row: one ★1 chest at once
            s.Run = new RunState { Board = b, Moves = 5 };
            var res = _game.Tap(s, 9, 0, new XorShift32(1));
            int expected = Scoring.Pop(4) + res.Chests.Sum(Scoring.Chest);
            Assert.AreEqual(4 * 4 * 5 + 100, expected);
            Assert.AreEqual(expected, s.Run.Score);
            Assert.AreEqual(expected, s.Score);

            var sum = _game.EndRun(s);
            Assert.AreEqual(expected, sum.Score);
            Assert.IsTrue(sum.NewRecord);
            Assert.AreEqual(expected, s.BestRun);

            s.Run = new RunState { Board = NoGroupBoard(), Moves = 5 };
            sum = _game.EndRun(s);
            Assert.AreEqual(0, sum.Score);
            Assert.IsFalse(sum.NewRecord);
            Assert.AreEqual(expected, s.BestRun, "a worse run keeps the record");
            Assert.AreEqual(expected, s.Score);
        }

        [Test]
        public void PlacingPartsAndFinishingABuildingScore()
        {
            var s = _game.NewGame();
            var slots = _castle1.Slots;
            foreach (var id in slots) s.Add(id, 1);
            int expected = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                Assert.IsTrue(_game.Place(s, i));
                expected += Scoring.Place(_cat.TierOf(slots[i]));
            }
            expected += Scoring.Building(0);
            Assert.AreEqual(expected, s.Score);
            Assert.AreEqual(0, s.BestRun, "the building site doesn't touch the run record");
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
        public void AdGivesTicketsOnlyWhenOutOfThemAsOftenAsWanted()
        {
            var s = _game.NewGame();
            _game.DailyGrant(s, "2026-11-03");
            Assert.IsFalse(_game.AdTickets(s), "not while tickets are left");
            for (int i = 0; i < 10; i++)
            {
                s.Tickets = 0;
                Assert.IsTrue(_game.AdTickets(s), "no daily limit");
                Assert.AreEqual(3, s.Tickets);
            }
            s.Tickets = 1;
            Assert.IsNotNull(_game.NewRun(s, new XorShift32(1)));
            Assert.IsTrue(_game.AdTickets(s), "in the middle of a run too, to start it over");
            Assert.AreEqual(3, s.Tickets);
        }

        [Test]
        public void MovingToNextCastleBurnsAllButFigurines()
        {
            var s = _game.NewGame();
            s.Inventory["clay"] = 5;
            s.Inventory["window"] = 2;
            s.Inventory["dragon"] = 1;
            s.Runs = 6;
            s.Tickets = 4;
            var bp = _game.PreviewBurn(s);
            Assert.AreEqual(7, bp.Parts);
            Assert.AreEqual(2, bp.Chests);
            Assert.AreEqual(1, bp.Kept.Count);
            Assert.AreEqual("dragon", bp.Kept[0].Id);

            _game.NextCastle(s);
            Assert.AreEqual(1, s.CastleIndex);
            Assert.AreEqual(6, s.Tickets, "a new building gives 2 tickets");
            Assert.AreEqual(0, s.Count("clay"));
            Assert.AreEqual(0, s.Count("window"));
            Assert.AreEqual(1, s.Count("dragon"));
            Assert.AreEqual(0, s.Chests.Count);
            Assert.AreEqual(_cat.Castles[1].Slots.Length, s.Filled.Length);
            Assert.AreEqual(0, s.Runs);
            Assert.IsFalse(_game.IsLastCastle(s));
            while (!_game.IsLastCastle(s)) _game.NextCastle(s);
            Assert.AreEqual(_cat.Castles.Count - 1, s.CastleIndex);
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
        public void DailyGiftAddsToolsTopsUpTicketsOncePerDay()
        {
            var s = _game.NewGame();
            var g = _game.DailyGrant(s, "2026-10-20");
            Assert.IsTrue(g.HasValue);
            Assert.AreEqual(1, g.Value.Day);
            Assert.AreEqual(3, s.Brooms);
            Assert.AreEqual(3, s.Hammers);
            Assert.AreEqual(15 + 3, s.Tickets, "topped up to 15, plus 3 for day 1 of the calendar");
            Assert.IsNull(_game.DailyGrant(s, "2026-10-20"));

            s.Brooms = 1; s.Tickets = 20;
            g = _game.DailyGrant(s, "2026-10-21");
            Assert.AreEqual(2, g.Value.Day);
            Assert.AreEqual(3 + 1, g.Value.Brooms, "3 a day plus 1 for day 2");
            Assert.AreEqual(0, g.Value.Tickets, "tops up to 15, never takes away");
            Assert.AreEqual(5, s.Brooms, "brooms pile up");
            Assert.AreEqual(20, s.Tickets);
        }

        [Test]
        public void LoginCalendarGoesRoundAWeekAndStartsOverAfterAMissedDay()
        {
            var s = _game.NewGame();
            string[] days = { "2026-10-30", "2026-10-31", "2026-11-01", "2026-11-02", "2026-11-03", "2026-11-04", "2026-11-05", "2026-11-06" };
            for (int i = 0; i < days.Length; i++)
                Assert.AreEqual(i % 7 + 1, _game.DailyGrant(s, days[i]).Value.Day, days[i]);
            int hammers = s.Hammers;
            var g = _game.DailyGrant(s, "2026-11-08");
            Assert.AreEqual(1, g.Value.Day, "a missed day starts over");
            Assert.AreEqual(hammers + 3, s.Hammers);

            s.Streak = 6; s.DailyDay = "2026-12-31";
            g = _game.DailyGrant(s, "2027-01-01");
            Assert.AreEqual(7, g.Value.Day, "across the new year");
            Assert.AreEqual(3 + 3, g.Value.Hammers, "day 7: 3 more hammers");
            Assert.IsTrue(GameLogic.IsDayAfter("2028-02-28", "2028-02-29"));
            Assert.IsFalse(GameLogic.IsDayAfter("2026-11-01", "2026-11-03"));
            Assert.IsFalse(GameLogic.IsDayAfter(null, "2026-11-03"));
        }

        [Test]
        public void AdGivesToolsWhenOneRanOutOnceADay()
        {
            var s = _game.NewGame();
            _game.DailyGrant(s, "2026-11-03");
            Assert.IsFalse(_game.AdTools(s), "not while both are left");
            s.Hammers = 0;
            int brooms = s.Brooms;
            Assert.IsTrue(_game.AdTools(s));
            Assert.AreEqual(3, s.Hammers);
            Assert.AreEqual(brooms + 3, s.Brooms);
            s.Hammers = 0;
            Assert.IsFalse(_game.AdTools(s), "once a day");
            _game.DailyGrant(s, "2026-11-04");
            s.Brooms = 0;
            Assert.IsTrue(_game.AdTools(s), "again the next day");
        }

        [Test]
        public void IceKeepsABlockOutOfGroupsUntilAnExplosionNextToIt()
        {
            var b = NoGroupBoard();
            // Row 9: 0 0 [0 iced] — a pair to tap and an iced block of the same colour next to it.
            b[9, 0].Color = 0; b[9, 1].Color = 0; b[9, 2].Color = 0; b[9, 2].Ice = true;
            b[8, 2].Color = 0; b[8, 2].Ice = true;
            Cell near = b[9, 2], far = b[8, 2];
            Assert.AreEqual(2, b.Group(9, 0).Count, "iced blocks don't join");
            Assert.AreEqual(0, b.Group(9, 2).Count, "an iced block can't be tapped");
            int thawed = -1;
            var res = b.Tap(9, 0, _castle1, new XorShift32(1), p => { if (p.Kind == PhaseKind.Remove) thawed = p.Thawed; });
            Assert.AreEqual(2, res.Size);
            Assert.AreEqual(1, thawed, "only the block next to the explosion thaws");
            Assert.IsFalse(near.Ice);
            Assert.IsTrue(far.Ice);
        }

        [Test]
        public void StonesOnlyFallAndLeaveAtTheBottom()
        {
            var b = NoGroupBoard();
            b[8, 0] = new Cell { Id = b.NextId++, Kind = CellKind.Stone };
            b[9, 1].Color = 3; b[9, 0].Color = 3;
            Assert.AreEqual(0, b.Group(8, 0).Count, "a stone is not a block");
            int stones = 0;
            b.Tap(9, 0, _castle1, new XorShift32(1), p => { if (p.Kind == PhaseKind.Collect) stones += p.Stones; });
            Assert.AreEqual(1, stones, "the stone fell into the bottom row and left");
            for (int r = 0; r < b.Rows; r++)
                for (int c = 0; c < b.Cols; c++) Assert.IsFalse(b[r, c].IsStone);
        }

        [Test]
        public void ObstaclesStartOnTheThirdCastle()
        {
            var rules = _cat.Rules;
            Assert.AreEqual(0, rules.IceFor(1));
            Assert.AreEqual(0, rules.StonesFor(1));
            Assert.Greater(rules.IceFor(2), 0);
            Assert.Greater(rules.StonesFor(2), 0);
            var b = Board.Generate(rules, _castle1, new XorShift32(5), rules.IceFor(2), rules.StonesFor(2));
            int ice = 0, stones = 0;
            for (int r = 0; r < b.Rows; r++)
                for (int c = 0; c < b.Cols; c++) { if (b[r, c].Ice) ice++; if (b[r, c].IsStone) { stones++; Assert.Less(r, b.Rows / 2); } }
            Assert.AreEqual(rules.StonesFor(2), stones);
            Assert.LessOrEqual(ice, rules.IceFor(2));
            Assert.Greater(ice, 0);
            Assert.IsTrue(b.HasMove());
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
            // The prototype plays buildings 1 and 2; the map order puts others second, so pick them by id.
            var full = DemoContent.Create();
            var two = new Catalog(full.Rules, full.Parts, full.Castles.Where(c => c.Id == "castle_1" || c.Id == "castle_2").OrderBy(c => c.Id).ToArray());
            var bot = new BalanceBot(new GameLogic(two), rng);
            var expected = new[] { new[] { 4, 16, 21, 98, 49 }, new[] { 4, 16, 31, 82, 33 }, new[] { 5, 21, 30, 108, 59 } };
            foreach (var exp in expected)
            {
                var r = bot.PlayGame();
                CollectionAssert.AreEqual(exp, new[] { r[0].Runs, r[1].Runs, r[0].Chests, r[1].Chests, r[0].Burned });
            }
            Assert.AreEqual(1681680551u, rng.State);
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

        [Test]
        public void BuiltCastleCanBeBuiltAgainAndTheCastleBeingBuiltWaits()
        {
            var s = _game.NewGame();
            _game.NextCastle(s); _game.NextCastle(s);           // building castle 3
            s.Filled[0] = s.Filled[1] = true;
            s.Inventory["wall"] = 4;
            int score = s.Score, built = s.Built, tickets = s.Tickets;

            Assert.IsFalse(_game.CanReplay(s, 2), "the castle being built is not built yet");
            Assert.IsFalse(_game.CanReplay(s, 3), "locked");
            Assert.IsTrue(_game.StartReplay(s, 0));
            Assert.IsTrue(s.Replaying);
            Assert.AreEqual(0, s.CastleIndex);
            Assert.AreEqual(2, s.Frontier);
            Assert.IsTrue(s.Filled.All(f => !f), "built again from empty");
            Assert.AreEqual(4, s.Count("wall"), "storage stays");

            // Finish it: points for the parts only; then «Далее» goes home, nothing burns, no tickets.
            var slots = _game.CastleOf(s).Slots;
            foreach (var id in slots) s.Add(id, 1);
            for (int k = 0; k < slots.Length; k++) Assert.IsTrue(_game.Place(s, k));
            int partsOnly = s.Score - score;
            Assert.IsTrue(_game.IsComplete(s));
            Assert.IsFalse(_game.IsLastCastle(s));
            _game.NextCastle(s);
            Assert.IsFalse(s.Replaying);
            Assert.AreEqual(2, s.CastleIndex);
            Assert.IsTrue(s.Filled[0] && s.Filled[1] && !s.Filled[2], "the waiting castle as it was left");
            Assert.AreEqual(4, s.Count("wall"));
            Assert.AreEqual(tickets, s.Tickets);
            Assert.AreEqual(built, s.Built);
            Assert.AreEqual(slots.Sum(id => Scoring.Place(_cat.TierOf(id))), partsOnly, "points for the parts, no building bonus");
        }

        [Test]
        public void ReplayCanSwitchToAnotherBuiltCastleOrGoHome()
        {
            var s = _game.NewGame();
            _game.NextCastle(s); _game.NextCastle(s);
            s.Filled[0] = true;
            Assert.IsTrue(_game.StartReplay(s, 1));
            Assert.IsFalse(_game.CanReplay(s, 1), "already being built again");
            Assert.IsTrue(_game.StartReplay(s, 0));
            Assert.AreEqual(2, s.HomeIndex);
            _game.EndReplay(s);
            Assert.AreEqual(2, s.CastleIndex);
            Assert.IsTrue(s.Filled[0]);
        }
    }
}
