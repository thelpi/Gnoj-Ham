using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class ComputeCurrentRanking_Tests
{
    [Fact]
    public void NoTargetPlayers_RecordsScoreOnTheGamesOwnPlayers()
    {
        var game = new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(1));

        var ranking = game.ComputeCurrentRanking();

        Assert.Equal(4, ranking.Count);
        Assert.All(ranking, r => Assert.Contains(r.Player, game.Players));
        Assert.All(game.Players, p => Assert.Equal(1, p.GamesCount));
    }

    [Fact]
    public void TargetPlayers_RecordsScoreOnTargets_NotOnTheGamesOwnPlayers()
    {
        var game = new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(1));
        var targetPlayers = PlayerPivot.BuildPlayers(null);

        var ranking = game.ComputeCurrentRanking(targetPlayers);

        Assert.Equal(4, ranking.Count);
        Assert.All(ranking, r => Assert.Contains(r.Player, targetPlayers));
        Assert.All(game.Players, p => Assert.Equal(0, p.GamesCount));
        Assert.All(targetPlayers, p => Assert.Equal(1, p.GamesCount));
    }

    [Fact]
    public void TargetPlayers_ScoreReflectsTheGamesOwnFinalPoints_NotTheTargetsPriorPoints()
    {
        // Baseline: same seed, scored onto the game's own players (known-correct, pre-existing path).
        var expectedRanking = new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(1)).ComputeCurrentRanking();

        // Same seed again, but scored onto DIFFERENT target players whose CurrentGamePoints (from a
        // totally different, unrelated prior game) must not leak into the computed score.
        var game = new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(1));
        var targetPlayers = PlayerPivot.BuildPlayers(null);
        PlayerPivot.SetPlayersForNewGame(targetPlayers, RulePivot.Default.InitialPointsRule, new Random(99));

        var actualRanking = game.ComputeCurrentRanking(targetPlayers);

        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(expectedRanking[i].Rank, actualRanking[i].Rank);
            Assert.Equal(expectedRanking[i].Score, actualRanking[i].Score);
        }
    }

    [Fact]
    public void MismatchedTargetPlayersCount_Throws()
    {
        var game = new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(1));
        var tooFewPlayers = PlayerPivot.BuildPlayers(null).Take(3).ToList();

        Assert.Throws<ArgumentException>(() => game.ComputeCurrentRanking(tooFewPlayers));
    }

    [Fact]
    public async Task ManyGamesInParallel_EachOnThrowawayPlayers_MergedUnderALock_MatchesAutoPlayWindowsPattern()
    {
        // Mirrors AutoPlayWindow.RunBatch: several full games played concurrently, each on its own
        // throwaway PlayerPivot set, merging their final ranking onto the same four permanent players
        // under a lock - the actual concurrency pattern this test is meant to guard.
        const int gamesCount = 20;
        var permanentPlayers = PlayerPivot.BuildPlayers(null);
        var statsLock = new object();
        var allRanks = new System.Collections.Concurrent.ConcurrentBag<int>();

        await Parallel.ForEachAsync(Enumerable.Range(0, gamesCount), async (seed, ct) =>
        {
            var gamePlayers = PlayerPivot.BuildPlayers(null);
            var game = new GamePivot(RulePivot.Default, gamePlayers, new Random(seed));

            EndOfRoundInformationsPivot info;
            do
            {
                var result = game.Round.RunAutoPlay(ct);
                info = game.NextRound(result.RonPlayerId);
            } while (!info.EndOfGame);

            IReadOnlyList<PlayerScorePivot> ranking;
            lock (statsLock)
            {
                ranking = game.ComputeCurrentRanking(permanentPlayers);
            }

            foreach (var r in ranking)
            {
                allRanks.Add(r.Rank);
            }

            await Task.CompletedTask;
        });

        Assert.All(permanentPlayers, p => Assert.Equal(gamesCount, p.GamesCount));

        // Every one of the 20 games must have assigned each rank (1-4) to exactly one seat: no lost or
        // duplicated update despite the concurrent merging.
        Assert.Equal(gamesCount * 4, allRanks.Count);
        for (var rank = 1; rank <= 4; rank++)
        {
            Assert.Equal(gamesCount, allRanks.Count(r => r == rank));
        }
    }
}
