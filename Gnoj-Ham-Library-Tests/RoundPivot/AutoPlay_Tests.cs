using Gnoj_Ham_Library;

namespace Gnoj_Ham_Library_Tests;

public class AutoPlay_Tests
{
    private readonly Dictionary<int, (string pName, int points)[]> _expected = new()
    {
        { 1000, new[] { ("CPU_1", 37300), ("CPU_0", 37000), ("CPU_3", 35200), ("CPU_2", -9500) } },
        { 666, new[] { ("CPU_0", 53600), ("CPU_1", 22300), ("CPU_3", 14400), ("CPU_2", 9700) } },
        { 999999, new[] { ("CPU_3", 46900), ("CPU_0", 30100), ("CPU_1", 15500), ("CPU_2", 7500) } },
        { 123456, new[] { ("CPU_1", 35600), ("CPU_3", 27500), ("CPU_0", 20100), ("CPU_2", 16800) } },
        { 789456, new[] { ("CPU_1", 37400), ("CPU_0", 27200), ("CPU_3", 20100), ("CPU_2", 15300) } },
        { 187543, new[] { ("CPU_3", 58700), ("CPU_1", 28800), ("CPU_0", 21800), ("CPU_2", -9300) } },
        // seed=5: natural abortive draw (ryuukyoku) somewhere in the game
        { 5, new[] { ("CPU_2", 46600), ("CPU_3", 22400), ("CPU_1", 16600), ("CPU_0", 14400) } },
        // seed=57: natural simultaneous ron from multiple winners on the same discard (3 winners)
        { 57, new[] { ("CPU_2", 34200), ("CPU_1", 27400), ("CPU_3", 19600), ("CPU_0", 18800) } },
        // seed=415: natural chain of 3 kans within a single round, all 3 by PlayerIndices.Zero alone
        // (round 5) - also covers "the human seat does several kans in a row" in a natural, non-rigged game
        { 415, new[] { ("CPU_3", 43200), ("CPU_2", 31000), ("CPU_1", 30300), ("CPU_0", -4500) } },
        // seed=345: natural yakuman win (PlayerIndices.Two)
        { 345, new[] { ("CPU_2", 64700), ("CPU_0", 23800), ("CPU_1", 13100), ("CPU_3", -1600) } },
        // seed=638: natural chain of 2 kans by a single non-zero player (PlayerIndices.Two, round 5)
        // within a single round - "a CPU does 2 kans in a row" (as opposed to seed=415's PlayerIndices.Zero)
        { 638, new[] { ("CPU_3", 49700), ("CPU_2", 40200), ("CPU_0", 19700), ("CPU_1", -9600) } }
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
    [InlineData(345)]
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
