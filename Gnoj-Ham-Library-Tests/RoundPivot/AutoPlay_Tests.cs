using Gnoj_Ham_Library;

namespace Gnoj_Ham_Library_Tests;

public class AutoPlay_Tests
{
    private readonly Dictionary<int, (string pName, int points)[]> _expected = new()
    {
        { 1000, new[] { ("CPU_0", 33800), ("CPU_2", 26200), ("CPU_3", 23400), ("CPU_1", 16600) } },
        // rescored after the wait-width tie-break was added to BasicCpuManagerPivot.GetBestDiscardFromList
        { 666, new[] { ("CPU_0", 52500), ("CPU_1", 43000), ("CPU_3", 2900), ("CPU_2", 1600) } },
        { 999999, new[] { ("CPU_3", 47300), ("CPU_2", 24500), ("CPU_1", 18300), ("CPU_0", 9900) } },
        { 123456, new[] { ("CPU_1", 34800), ("CPU_0", 28500), ("CPU_3", 21500), ("CPU_2", 15200) } },
        { 789456, new[] { ("CPU_1", 37600), ("CPU_2", 36900), ("CPU_0", 15500), ("CPU_3", 10000) } },
        { 187543, new[] { ("CPU_3", 55200), ("CPU_1", 29100), ("CPU_0", 25700), ("CPU_2", -10000) } },
        // seed=5: natural abortive draw (ryuukyoku) somewhere in the game
        { 5, new[] { ("CPU_0", 58900), ("CPU_3", 22800), ("CPU_2", 18900), ("CPU_1", -600) } },
        // seed=8: natural simultaneous ron from multiple winners on the same discard (rescored, property re-verified)
        { 8, new[] { ("CPU_0", 33600), ("CPU_1", 28700), ("CPU_2", 19300), ("CPU_3", 18400) } },
        // seed=183: natural chain of 2 kans by PlayerIndices.Zero alone within a single round (round 7)
        // - covers "the human seat does several kans in a row" in a natural, non-rigged game
        { 183, new[] { ("CPU_0", 45700), ("CPU_2", 22500), ("CPU_3", 18800), ("CPU_1", 13000) } },
        // seed=742: natural yakuman win (PlayerIndices.One)
        { 742, new[] { ("CPU_1", 68000), ("CPU_0", 26000), ("CPU_2", 9000), ("CPU_3", -3000) } },
        // seed=638: natural chain of 2 kans by a single non-zero player (PlayerIndices.Two, round 5)
        // within a single round - "a CPU does 2 kans in a row" (as opposed to seed=183's PlayerIndices.Zero)
        { 638, new[] { ("CPU_2", 47500), ("CPU_1", 29700), ("CPU_3", 22800), ("CPU_0", 0) } }
    };

    [Theory]
    [InlineData(1000)]
    [InlineData(666)]
    [InlineData(999999)]
    [InlineData(123456)]
    [InlineData(789456)]
    [InlineData(187543)]
    [InlineData(5)]
    [InlineData(8)]
    [InlineData(183)]
    [InlineData(742)]
    [InlineData(638)]
    public void AutoPlay_GeneratesExpectedRound(int seed)
    {
        var random = new Random(seed);

        var game = new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), random);

        IReadOnlyList<PlayerScorePivot>? scores;
        while (true)
        {
            var result = game.Round.RunAutoPlay(new CancellationToken());
            var endOfRoundInfo = game.NextRound(result.RonPlayerId);

            if (endOfRoundInfo.EndOfGame)
            {
                scores = game.ComputeCurrentRanking();
                break;
            }
        }

        Assert.NotNull(scores);
        for (var i = 0; i < scores.Count; i++)
        {
            Assert.Equal(_expected[seed][i].pName, scores[i].Player.Name);
            Assert.Equal(_expected[seed][i].points, scores[i].Player.CurrentGamePoints);
        }
    }
}
