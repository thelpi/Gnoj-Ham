using Gnoj_Ham_Library;

namespace Gnoj_Ham_Library_Tests;

public class AutoPlay_Tests
{
    private readonly Dictionary<int, (string pName, int points)[]> _expected = new()
    {
        { 1000, new[] { ("CPU_0", 42900), ("CPU_1", 26300), ("CPU_3", 18100), ("CPU_2", 12700) } },
        { 666, new[] { ("CPU_0", 63900), ("CPU_1", 15400), ("CPU_3", 12300), ("CPU_2", 8400) } },
        { 999999, new[] { ("CPU_1", 32300), ("CPU_0", 25400), ("CPU_2", 23900), ("CPU_3", 18400) } }
    };

    [Theory]
    [InlineData(1000)]
    [InlineData(666)]
    [InlineData(999999)]
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
