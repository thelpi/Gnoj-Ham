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
            { 1000, new[] { ("CPU_0", 42500), ("CPU_1", 23900), ("CPU_2", 19000), ("CPU_3", 15700) } },
            { 666, new[] { ("CPU_0", 62000), ("CPU_1", 21600), ("CPU_3", 9800), ("CPU_2", 8200) } },
            { 999999, new[] { ("CPU_3", 33900), ("CPU_0", 32700), ("CPU_1", 17100), ("CPU_2", 16400) } }
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
                    scores = ScoreTools.ComputeCurrentRanking(game);
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
