using System.Collections;
using System.Linq;
using Game.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.View.Tests
{
    public class MineTests
    {
        private GameObject _go;
        private MineController _mine;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _go = new GameObject("Mine");
            _mine = _go.AddComponent<MineController>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_go);
            yield return null;
        }

        private IEnumerator WaitIdle()
        {
            float t = 0;
            while (_mine.Busy && t < 5f) { t += Time.deltaTime; yield return null; }
            Assert.IsFalse(_mine.Busy, "animation did not finish");
        }

        [UnityTest]
        public IEnumerator StartsARunWithAFullBoard()
        {
            var run = _mine.State.Run;
            Assert.IsNotNull(run);
            Assert.AreEqual(30, run.Moves);
            Assert.AreEqual(14, _mine.State.Tickets, "daily gift 15, one spent on the run");
            Assert.AreEqual(100, _mine.CellViewCount);
            yield break;
        }

        [UnityTest]
        public IEnumerator TapPlaysTheMoveAndViewsMatchTheBoard()
        {
            for (int i = 0; i < 5; i++)
            {
                var b = _mine.State.Run.Board;
                var g = b.AllGroups().OrderByDescending(x => x.Count).First();
                int moves = _mine.State.Run.Moves;
                Assert.IsTrue(_mine.TryTap(g[0].R, g[0].C));
                yield return WaitIdle();
                Assert.AreEqual(moves - 1, _mine.State.Run.Moves);
                Assert.AreEqual(b.Rows * b.Cols, _mine.CellViewCount, "one view per cell after the move");
            }
        }

        [UnityTest]
        public IEnumerator RestartGivesANewBoardForATicket()
        {
            var before = _mine.State.Run.Board;
            int tickets = _mine.State.Tickets;
            _mine.AskRestart();
            Assert.IsTrue(_mine.Restart());
            yield return null;
            Assert.AreNotSame(before, _mine.State.Run.Board);
            Assert.AreEqual(tickets - 1, _mine.State.Tickets);
            Assert.AreEqual(30, _mine.State.Run.Moves);
            Assert.AreEqual(100, _mine.CellViewCount);
        }

        [UnityTest]
        public IEnumerator BoardFitsBetweenTheToolsAndTheGotPanel()
        {
            yield return null;
            var cam = Camera.main;
            var b = _mine.State.Run.Board;
            float s = Mathf.Min(Screen.width / 720f, Screen.height / 1280f);
            var area = _mine.Layout().Board;
            // Screen y runs up, GUI y runs down.
            var topLeft = cam.WorldToScreenPoint(new Vector3(-b.Cols / 2f, b.Rows / 2f, 0));
            var bottomRight = cam.WorldToScreenPoint(new Vector3(b.Cols / 2f, -b.Rows / 2f, 0));
            const float eps = 2f;
            Assert.GreaterOrEqual(topLeft.x, area.xMin * s - eps, "left");
            Assert.LessOrEqual(bottomRight.x, area.xMax * s + eps, "right");
            Assert.GreaterOrEqual(Screen.height - topLeft.y, area.yMin * s - eps, "top");
            Assert.LessOrEqual(Screen.height - bottomRight.y, area.yMax * s + eps, "bottom");
        }

        [UnityTest]
        public IEnumerator HammerSpendsAToolNotAMove()
        {
            var b = _mine.State.Run.Board;
            int r = -1, c = -1;
            for (int y = 0; y < b.Rows && r < 0; y++)
                for (int x = 0; x < b.Cols; x++)
                    if (b[y, x].IsBlock) { r = y; c = x; break; }
            _mine.Arm(Tool.Hammer);
            Assert.AreEqual(Tool.Hammer, _mine.Armed);
            Assert.IsTrue(_mine.TryTap(r, c));
            yield return WaitIdle();
            Assert.AreEqual(2, _mine.State.Hammers);
            Assert.AreEqual(30, _mine.State.Run.Moves);
            Assert.IsNull(_mine.Armed);
            Assert.AreEqual(100, _mine.CellViewCount);
        }

        [Test]
        public void EveryBlockColourHasArt()
        {
            for (int c = 0; c < Palette.Blocks.Length; c++)
            {
                Assert.IsNotNull(SpriteFactory.BlockArt(c), $"block art for colour {c}");
                Assert.IsNotNull(SpriteFactory.ChestArt(c), $"chest art for colour {c}");
                Assert.IsNotNull(GameArt.ChestOpen(c), $"open chest art for colour {c}");
            }
        }

        [Test]
        public void ScreenArtLoads()
        {
            Assert.IsNotNull(GameArt.MineBackground);
            Assert.IsNotNull(GameArt.BuildBackground);
            Assert.IsNotNull(GameArt.Sparkle);
            // The chest window's art.
            Assert.IsNotNull(GameArt.Glint);
            Assert.IsNotNull(GameArt.Rays);
            Assert.IsNotNull(GameArt.Plate);
            Assert.IsNotNull(GameArt.NightPanel);
            Assert.IsNotNull(GameArt.Ribbon);
            Assert.IsNotNull(GameArt.GoldButton);
            Assert.IsNotNull(GameArt.GoldButtonDown);
            // The browser has no system fonts: the UI font must ship with the game and cover Russian.
            Assert.IsNotNull(UiArt.TextFont);
            Assert.IsNotNull(UiArt.BoldFont);
            Assert.IsTrue(UiArt.TextFont.HasCharacter('Ж'));
            Assert.IsTrue(UiArt.BoldFont.HasCharacter('ё'));
            // Every part has an icon except the unicorn, which is still to be drawn.
            foreach (var p in DemoContent.Create().Parts)
                if (p.Id != "unicorn") Assert.IsNotNull(GameArt.Part(p.Id), p.Id);
        }
    }
}
