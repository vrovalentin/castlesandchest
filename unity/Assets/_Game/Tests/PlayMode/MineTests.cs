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
                Assert.IsNotNull(SpriteFactory.BlockArt(c), $"Art/Resources/Board art for colour {c}");
        }
    }
}
