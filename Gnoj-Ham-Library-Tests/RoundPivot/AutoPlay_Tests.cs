using Gnoj_Ham_Library;

namespace Gnoj_Ham_Library_Tests;

public class AutoPlay_Tests
{
    private readonly Dictionary<int, (string pName, int points)[]> _expected = new()
    {
        { 1000, new[] { ("CPU_1", 47700), ("CPU_0", 28000), ("CPU_3", 20000), ("CPU_2", 4300) } },
        { 666, new[] { ("CPU_0", 49000), ("CPU_1", 28500), ("CPU_3", 13000), ("CPU_2", 9500) } },
        { 999999, new[] { ("CPU_1", 55700), ("CPU_3", 26500), ("CPU_2", 19000), ("CPU_0", -1200) } },
        { 123456, new[] { ("CPU_1", 56700), ("CPU_0", 23700), ("CPU_2", 20100), ("CPU_3", -500) } },
        { 789456, new[] { ("CPU_3", 38400), ("CPU_0", 22900), ("CPU_1", 20800), ("CPU_2", 17900) } },
        { 187543, new[] { ("CPU_1", 35900), ("CPU_3", 23500), ("CPU_0", 23300), ("CPU_2", 17300) } },
        // seed=5: natural abortive draw (ryuukyoku) somewhere in the game
        { 5, new[] { ("CPU_0", 51300), ("CPU_3", 22200), ("CPU_2", 20700), ("CPU_1", 5800) } },
        // seed=57: natural simultaneous ron from two winners on the same discard
        { 57, new[] { ("CPU_0", 52100), ("CPU_3", 26300), ("CPU_1", 16400), ("CPU_2", 5200) } },
        // seed=140: natural chain of 3 kans within a single round
        { 140, new[] { ("CPU_0", 39600), ("CPU_2", 24200), ("CPU_3", 21400), ("CPU_1", 14800) } },
        // seed=189: natural yakuman win
        { 189, new[] { ("CPU_2", 74400), ("CPU_0", 13000), ("CPU_3", 12900), ("CPU_1", -300) } }
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
    [InlineData(140)]
    [InlineData(189)]
    public void AutoPlay_GeneratesExpectedRound(int seed)
    {
        var random = new Random(seed);

        var game = new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), random);

        IReadOnlyList<PlayerScorePivot>? scores;
        while (true)
        {
            var result = game.Round.RunAutoPlay(new CancellationToken());
            var (endOfRoundInfo, _) = game.NextRound(result.RonPlayerId);

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
