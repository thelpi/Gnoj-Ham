using Gnoj_Ham_Library;

namespace Gnoj_Ham_Library_Tests;

public class AutoPlay_Tests
{
    private readonly Dictionary<int, (string pName, int points)[]> _expected = new()
    {
        { 1000, new[] { ("CPU_2", 67600), ("CPU_0", 16800), ("CPU_3", 14200), ("CPU_1", 1400) } },
        { 666, new[] { ("CPU_0", 64700), ("CPU_2", 29700), ("CPU_3", 6100), ("CPU_1", -500) } },
        { 999999, new[] { ("CPU_0", 70100), ("CPU_3", 15600), ("CPU_2", 13100), ("CPU_1", 1200) } },
        { 123456, new[] { ("CPU_2", 55800), ("CPU_0", 36700), ("CPU_1", 6000), ("CPU_3", 1500) } },
        { 789456, new[] { ("CPU_1", 31600), ("CPU_3", 29300), ("CPU_0", 20500), ("CPU_2", 18600) } },
        { 187543, new[] { ("CPU_1", 47200), ("CPU_2", 25300), ("CPU_0", 19800), ("CPU_3", 7700) } },
        // seed=5: natural abortive draw (ryuukyoku) somewhere in the game
        { 5, new[] { ("CPU_2", 46300), ("CPU_3", 22300), ("CPU_1", 16500), ("CPU_0", 14900) } },
        // seed=57: natural simultaneous ron from multiple winners on the same discard (now 3 winners)
        { 57, new[] { ("CPU_0", 63500), ("CPU_3", 23700), ("CPU_2", 9000), ("CPU_1", 3800) } },
        // seed=415: natural chain of 3 kans within a single round, all 3 by PlayerIndices.Zero alone
        // (round 5) - also covers "the human seat does several kans in a row" in a natural, non-rigged game
        { 415, new[] { ("CPU_1", 30000), ("CPU_2", 28900), ("CPU_3", 21300), ("CPU_0", 19800) } },
        // seed=231: natural yakuman win (PlayerIndices.One)
        { 231, new[] { ("CPU_1", 107500), ("CPU_2", 8300), ("CPU_3", -7300), ("CPU_0", -8500) } },
        // seed=638: natural chain of 2 kans by a single non-zero player (PlayerIndices.Two, round 5)
        // within a single round - "a CPU does 2 kans in a row" (as opposed to seed=415's PlayerIndices.Zero)
        { 638, new[] { ("CPU_1", 34600), ("CPU_3", 29500), ("CPU_0", 23500), ("CPU_2", 12400) } }
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
    [InlineData(231)]
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
