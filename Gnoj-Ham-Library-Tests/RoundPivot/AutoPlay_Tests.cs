using Gnoj_Ham_Library;

namespace Gnoj_Ham_Library_Tests;

public class AutoPlay_Tests
{
    private readonly Dictionary<int, (string pName, int points)[]> _expected = new()
    {
        // rescored after DeadTilesFromIndexPointOfView was fixed to return a genuine multiset (it used
        // to dedupe via Enumerable.Except, capping every kind at 0 or 1 dead copies instead of 0-4)
        { 1000, new[] { ("CPU_0", 33800), ("CPU_2", 26200), ("CPU_3", 23400), ("CPU_1", 16600) } },
        { 666, new[] { ("CPU_1", 49000), ("CPU_0", 28200), ("CPU_2", 15500), ("CPU_3", 7300) } },
        { 999999, new[] { ("CPU_3", 42800), ("CPU_1", 23800), ("CPU_2", 18700), ("CPU_0", 14700) } },
        { 123456, new[] { ("CPU_0", 33800), ("CPU_2", 23900), ("CPU_3", 22700), ("CPU_1", 19600) } },
        { 789456, new[] { ("CPU_1", 37600), ("CPU_2", 36900), ("CPU_0", 15500), ("CPU_3", 10000) } },
        { 187543, new[] { ("CPU_3", 58500), ("CPU_1", 26200), ("CPU_0", 16500), ("CPU_2", -1200) } },
        // seed=5: natural abortive draw (ryuukyoku) somewhere in the game (rescored, property re-verified)
        { 5, new[] { ("CPU_2", 41600), ("CPU_0", 26400), ("CPU_1", 24500), ("CPU_3", 7500) } },
        // seed=8: natural simultaneous ron from multiple winners on the same discard (rescored, property re-verified)
        { 8, new[] { ("CPU_1", 47700), ("CPU_0", 20200), ("CPU_3", 19100), ("CPU_2", 13000) } },
        // seed=272 (replaces seed=183, whose kan chain broke down to 1): natural chain of 2 kans by
        // PlayerIndices.Zero alone within a single round - covers "the human seat does several kans in
        // a row" in a natural, non-rigged game
        { 272, new[] { ("CPU_1", 31100), ("CPU_3", 27900), ("CPU_0", 22000), ("CPU_2", 19000) } },
        // seed=742: natural yakuman win (PlayerIndices.One)
        { 742, new[] { ("CPU_1", 68000), ("CPU_0", 26000), ("CPU_2", 9000), ("CPU_3", -3000) } },
        // seed=638: natural chain of 2 kans by a single non-zero player (PlayerIndices.Two, round 5)
        // within a single round - "a CPU does 2 kans in a row" (as opposed to seed=272's PlayerIndices.Zero)
        // (rescored, property re-verified)
        { 638, new[] { ("CPU_2", 46500), ("CPU_1", 30700), ("CPU_3", 22800), ("CPU_0", 0) } }
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
    [InlineData(272)]
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
