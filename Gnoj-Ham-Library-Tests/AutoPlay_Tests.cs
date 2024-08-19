using Gnoj_Ham_Library;

namespace Gnoj_Ham_Library_Tests;

public class AutoPlay_Tests
{
    private readonly Dictionary<int, (string pName, int points)[]> _expected = new()
    {
        { 1000, new[] { ("CPU_1", 47700), ("CPU_0", 28000), ("CPU_3", 20000), ("CPU_2", 4300) } },
        { 666, new[] { ("CPU_0", 49000), ("CPU_1", 28500), ("CPU_3", 13000), ("CPU_2", 9500) } },
        { 999999, new[] { ("CPU_1", 51700), ("CPU_3", 24900), ("CPU_2", 24100), ("CPU_0", -700) } },
        { 123456, new[] { ("CPU_1", 54500), ("CPU_0", 28700), ("CPU_2", 16900), ("CPU_3", -100) } },
        { 789456, new[] { ("CPU_3", 38400), ("CPU_0", 22900), ("CPU_1", 20800), ("CPU_2", 17900) } },
        { 187543, new[] { ("CPU_1", 35900), ("CPU_3", 23500), ("CPU_0", 23300), ("CPU_2", 17300) } }
    };

    [Theory]
    [InlineData(1000)]
    [InlineData(666)]
    [InlineData(999999)]
    [InlineData(123456)]
    [InlineData(789456)]
    [InlineData(187543)]
    public void AutoPlay_GeneratesExpectedRound(int seed)
    {
        var random = new Random(seed);

        var permanentPlayers = new List<PermanentPlayerPivot>
        {
            new(),
            new(),
            new(),
            new()
        };

        var game = new GamePivot(RulePivot.Default, permanentPlayers, random);

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
            Assert.Equal(_expected[seed][i].points, scores[i].Player.Points);
        }
    }
}
