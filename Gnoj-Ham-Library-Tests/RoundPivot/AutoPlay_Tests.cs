using Gnoj_Ham_Library;

namespace Gnoj_Ham_Library_Tests;

public class AutoPlay_Tests
{
    private readonly Dictionary<int, (string pName, int points)[]> _expected = new()
    {
        { 1000, new[] { ("CPU_1", 38800), ("CPU_3", 32600), ("CPU_0", 30700), ("CPU_2", -2100) } },
        { 666, new[] { ("CPU_0", 70100), ("CPU_3", 17500), ("CPU_2", 7100), ("CPU_1", 5300) } },
        { 999999, new[] { ("CPU_2", 45300), ("CPU_3", 20000), ("CPU_1", 18600), ("CPU_0", 16100) } },
        { 123456, new[] { ("CPU_1", 46000), ("CPU_0", 38300), ("CPU_3", 9600), ("CPU_2", 6100) } },
        { 789456, new[] { ("CPU_1", 37400), ("CPU_0", 27200), ("CPU_3", 20100), ("CPU_2", 15300) } },
        { 187543, new[] { ("CPU_1", 49200), ("CPU_2", 30700), ("CPU_3", 15800), ("CPU_0", 4300) } },
        // seed=5: natural abortive draw (ryuukyoku) somewhere in the game
        { 5, new[] { ("CPU_2", 48500), ("CPU_3", 22300), ("CPU_1", 18500), ("CPU_0", 10700) } },
        // seed=57: natural simultaneous ron from multiple winners on the same discard (3 winners)
        { 57, new[] { ("CPU_0", 41300), ("CPU_3", 32700), ("CPU_2", 18200), ("CPU_1", 7800) } },
        // seed=415: natural chain of 3 kans within a single round, all 3 by PlayerIndices.Zero alone
        // (round 5) - also covers "the human seat does several kans in a row" in a natural, non-rigged game
        { 415, new[] { ("CPU_1", 42600), ("CPU_3", 32900), ("CPU_2", 30000), ("CPU_0", -5500) } },
        // seed=55: natural yakuman win (PlayerIndices.Two)
        { 55, new[] { ("CPU_2", 65300), ("CPU_3", 24700), ("CPU_1", 12400), ("CPU_0", -2400) } },
        // seed=638: natural chain of 2 kans by a single non-zero player (PlayerIndices.Two, round 5)
        // within a single round - "a CPU does 2 kans in a row" (as opposed to seed=415's PlayerIndices.Zero)
        { 638, new[] { ("CPU_1", 37900), ("CPU_3", 36900), ("CPU_2", 16400), ("CPU_0", 8800) } }
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
    [InlineData(415)]
    [InlineData(55)]
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
