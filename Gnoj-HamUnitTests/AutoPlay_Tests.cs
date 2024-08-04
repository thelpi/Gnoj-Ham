using System;
using System.Collections.Generic;
using Gnoj_Ham;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gnoj_HamUnitTests
{
    [TestClass]
    public class AutoPlay_Tests
    {
        private readonly Dictionary<int, (string pName, int points)[]> _expected = new Dictionary<int, (string pName, int points)[]>
        {
            { 1000, new[] { ("CPU_0", 42900), ("CPU_1", 26300), ("CPU_3", 18100), ("CPU_2", 12700) } },
            { 666, new[] { ("CPU_0", 63900), ("CPU_1", 15400), ("CPU_3", 12300), ("CPU_2", 8400) } },
            { 999999, new[] { ("CPU_1", 32300), ("CPU_0", 25400), ("CPU_2", 23900), ("CPU_3", 18400) } }
        };

        [TestMethod]
        public void AutoPlay_WithSeed1000_GeneratesExpectedRound()
        {
            var seed = 1000;

            InternalAutoPlayAndAssert(seed);
        }

        [TestMethod]
        public void AutoPlay_WithSeed666_GeneratesExpectedRound()
        {
            var seed = 666;

            InternalAutoPlayAndAssert(seed);
        }

        [TestMethod]
        public void AutoPlay_WithSeed999999_GeneratesExpectedRound()
        {
            var seed = 999999;

            InternalAutoPlayAndAssert(seed);
        }

        private void InternalAutoPlayAndAssert(int seed)
        {
            var random = new Random(seed);

            var permanentPlayers = new List<PermanentPlayerPivot>
            {
                new PermanentPlayerPivot(),
                new PermanentPlayerPivot(),
                new PermanentPlayerPivot(),
                new PermanentPlayerPivot()
            };

            var game = new GamePivot(RulePivot.Default, permanentPlayers, random);

            var autoPlay = new AutoPlayPivot(game);

            IReadOnlyList<PlayerScorePivot> scores = null;
            while (true)
            {
                var result = autoPlay.RunAutoPlay(new System.Threading.CancellationToken());
                var (endOfRoundInfo, _) = game.NextRound(result.ronPlayerId);

                if (endOfRoundInfo.EndOfGame)
                {
                    scores = game.ComputeCurrentRanking();
                    break;
                }
            }

            Assert.IsNotNull(scores);
            for (var i = 0; i < scores.Count; i++)
            {
                Assert.AreEqual(_expected[seed][i].pName, scores[i].Player.Name);
                Assert.AreEqual(_expected[seed][i].points, scores[i].Player.Points);
            }
        }
    }
}
