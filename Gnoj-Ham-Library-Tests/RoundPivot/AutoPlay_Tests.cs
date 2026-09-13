using Gnoj_Ham_Library;

namespace Gnoj_Ham_Library_Tests;

public class AutoPlay_Tests
{
    private readonly Dictionary<int, (string pName, int points)[]> _expected = new()
    {
        { 1000, new[] { ("CPU_0", 33800), ("CPU_2", 26200), ("CPU_3", 23400), ("CPU_1", 16600) } },
        { 666, new[] { ("CPU_0", 55400), ("CPU_1", 37200), ("CPU_3", 5800), ("CPU_2", 1600) } },
        { 999999, new[] { ("CPU_3", 40900), ("CPU_1", 27900), ("CPU_2", 24500), ("CPU_0", 6700) } },
        { 123456, new[] { ("CPU_1", 44200), ("CPU_0", 25600), ("CPU_3", 20000), ("CPU_2", 10200) } },
        { 789456, new[] { ("CPU_1", 38100), ("CPU_0", 22100), ("CPU_3", 20600), ("CPU_2", 19200) } },
        { 187543, new[] { ("CPU_3", 60600), ("CPU_1", 29600), ("CPU_0", 18800), ("CPU_2", -9000) } },
        // seed=5: natural abortive draw (ryuukyoku) somewhere in the game
        { 5, new[] { ("CPU_0", 58900), ("CPU_3", 22800), ("CPU_2", 18900), ("CPU_1", -600) } },
        // seed=57: natural simultaneous ron from multiple winners on the same discard
        { 57, new[] { ("CPU_3", 44500), ("CPU_0", 30300), ("CPU_1", 16400), ("CPU_2", 8800) } },
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
    [InlineData(57)]
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
