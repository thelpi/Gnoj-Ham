using Gnoj_Ham_Library;

namespace Gnoj_Ham_Library_Tests;

public class AutoPlay_Tests
{
    private readonly Dictionary<int, (string pName, int points)[]> _expected = new()
    {
        { 1000, new[] { ("CPU_0", 35900), ("CPU_2", 30800), ("CPU_3", 25300), ("CPU_1", 8000) } },
        { 666, new[] { ("CPU_0", 45500), ("CPU_1", 24000), ("CPU_3", 17500), ("CPU_2", 13000) } },
        { 999999, new[] { ("CPU_2", 47600), ("CPU_1", 40800), ("CPU_3", 12600), ("CPU_0", -1000) } },
        { 123456, new[] { ("CPU_1", 37600), ("CPU_0", 25400), ("CPU_2", 21900), ("CPU_3", 15100) } },
        { 789456, new[] { ("CPU_1", 35800), ("CPU_0", 27200), ("CPU_3", 20700), ("CPU_2", 16300) } },
        { 187543, new[] { ("CPU_3", 60000), ("CPU_1", 34000), ("CPU_0", 16600), ("CPU_2", -10600) } },
        // seed=5: natural abortive draw (ryuukyoku) somewhere in the game
        { 5, new[] { ("CPU_2", 33300), ("CPU_0", 29900), ("CPU_1", 27400), ("CPU_3", 9400) } },
        // seed=57: natural simultaneous ron from multiple winners on the same discard
        { 57, new[] { ("CPU_0", 38300), ("CPU_3", 31400), ("CPU_2", 22900), ("CPU_1", 7400) } },
        // seed=415: natural chain of 2 kans by PlayerIndices.Zero alone within a single round (round 5)
        // - covers "the human seat does several kans in a row" in a natural, non-rigged game
        { 415, new[] { ("CPU_3", 39800), ("CPU_0", 33300), ("CPU_1", 13500), ("CPU_2", 13400) } },
        // seed=345: natural yakuman win (PlayerIndices.Two)
        { 345, new[] { ("CPU_2", 90000), ("CPU_1", 21400), ("CPU_3", 300), ("CPU_0", -11700) } },
        // seed=638: natural chain of 2 kans by a single non-zero player (PlayerIndices.Two, round 5)
        // within a single round - "a CPU does 2 kans in a row" (as opposed to seed=415's PlayerIndices.Zero)
        { 638, new[] { ("CPU_2", 46400), ("CPU_1", 29400), ("CPU_3", 23500), ("CPU_0", 700) } }
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
