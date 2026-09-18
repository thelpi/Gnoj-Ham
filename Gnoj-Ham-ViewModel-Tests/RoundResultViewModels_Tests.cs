using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;
using Gnoj_Ham_ViewModel;

namespace Gnoj_Ham_ViewModel_Tests;

// The score and end-of-game view-models are exercised on real rounds played by the engine from a fixed
// seed, since an EndOfRoundInformationsPivot can't be built by hand outside the library.
public class RoundResultViewModels_Tests
{
    private static (IReadOnlyList<PlayerPivot> players, EndOfRoundInformationsPivot info) PlayFirstRound(int seed)
    {
        var game = new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(seed));
        var result = game.Round.RunAutoPlay(new CancellationToken());
        var info = game.NextRound(result.RonPlayerId);
        return (game.Players, info);
    }

    // First round, over the first seeds, satisfying the predicate.
    private static (IReadOnlyList<PlayerPivot> players, EndOfRoundInformationsPivot info) FindFirstRound(
        Func<EndOfRoundInformationsPivot, bool> predicate)
    {
        for (var seed = 1; seed < 300; seed++)
        {
            var (players, info) = PlayFirstRound(seed);
            if (predicate(info))
            {
                return (players, info);
            }
        }

        throw new InvalidOperationException("No round matching the predicate in the first seeds.");
    }

    private static bool HasWinnerWithYakus(EndOfRoundInformationsPivot info)
        => info.PlayersInfo.Any(p => p.HandPointsGain > 0 && p.Yakus is { Count: > 0 });

    [Fact]
    public void Ranking_IsSortedByPointsDescendingWithEachPlayersGain()
    {
        var (players, info) = PlayFirstRound(8);

        var viewModel = new ScoreViewModel(players, info);

        Assert.Equal(4, viewModel.Ranking.Count);
        Assert.Equal(viewModel.Ranking.Select(r => r.Points).OrderByDescending(p => p), viewModel.Ranking.Select(r => r.Points));
        foreach (var row in viewModel.Ranking)
        {
            var index = players.ToList().FindIndex(p => p.Name == row.PlayerName);
            Assert.Equal(info.GetPlayerPointsGain((PlayerIndices)index), row.Gain.Value);
            Assert.Equal(players[index].CurrentGamePoints, row.Points);
        }
    }

    [Fact]
    public void Indicators_AreLaidOutLastToFirstWithUnrevealedOnesFaceDown()
    {
        var (players, info) = PlayFirstRound(8);

        var viewModel = new ScoreViewModel(players, info);

        Assert.Equal(5, viewModel.DoraTiles.Count);
        Assert.Equal(5, viewModel.UraDoraTiles.Count);
        for (var i = 0; i < 5; i++)
        {
            var sourceIndex = 4 - i;
            Assert.Equal(info.DoraTiles[sourceIndex], viewModel.DoraTiles[i].Tile);
            Assert.Equal(sourceIndex >= info.DoraVisibleCount, viewModel.DoraTiles[i].IsConcealed);
            Assert.Equal(sourceIndex >= info.UraDoraVisibleCount, viewModel.UraDoraTiles[i].IsConcealed);
        }
    }

    [Fact]
    public void WinningRound_ListsItsWinnersWithTheirHandAndYakus()
    {
        var (players, info) = FindFirstRound(HasWinnerWithYakus);

        var viewModel = new ScoreViewModel(players, info);

        var expectedWinners = info.PlayersInfo.Where(p => p.HandPointsGain > 0).ToList();
        Assert.Equal(expectedWinners.Count, viewModel.Winners.Count);

        var winner = viewModel.Winners.First(w => w.HasYakus);
        var winnerInfo = expectedWinners.First(p => players[(int)p.Index].Name == winner.PlayerName);
        Assert.Equal(winnerInfo.HandPointsGain, winner.HandPointsGain);
        Assert.Equal($"{winnerInfo.FanCount} fan", winner.FanText);
        Assert.Equal($"{winnerInfo.FuCount} fu", winner.FuText);
        Assert.Equal(winnerInfo.GetFullHandForDisplay().Count, winner.HandTiles.Count);
        Assert.NotEmpty(winner.YakuLines);
        Assert.All(winner.YakuLines, line => Assert.False(string.IsNullOrEmpty(line.Name)));
    }

    [Fact]
    public void DoraKinds_AppearAsYakuLinesOnlyWhenPresent()
    {
        var (players, info) = FindFirstRound(i => i.PlayersInfo.Any(p => p.HandPointsGain > 0 && p.Yakus is { Count: > 0 } && p.DoraCount > 0));

        var viewModel = new ScoreViewModel(players, info);

        var winnerInfo = info.PlayersInfo.First(p => p.HandPointsGain > 0 && p.Yakus is { Count: > 0 } && p.DoraCount > 0);
        var winner = viewModel.Winners.First(w => w.PlayerName == players[(int)winnerInfo.Index].Name);
        Assert.Contains(new YakuLineViewModel(YakuPivot.Dora, winnerInfo.DoraCount), winner.YakuLines);
        Assert.Equal(winnerInfo.UraDoraCount > 0, winner.YakuLines.Any(l => l.Name == YakuPivot.UraDora));
        Assert.Equal(winnerInfo.RedDoraCount > 0, winner.YakuLines.Any(l => l.Name == YakuPivot.RedDora));
    }

    [Fact]
    public void DrawWithTenpaiPlayers_ShowsThemWithoutAYakuBreakdown()
    {
        // On an exhaustive draw the tenpai players are paid: they show up like winners, minus any yaku.
        var (players, info) = FindFirstRound(i => i.PlayersInfo.Any(p => p.HandPointsGain > 0 && p.Yakus is not { Count: > 0 }));

        var viewModel = new ScoreViewModel(players, info);

        var tenpaiWinner = viewModel.Winners.First(w => !w.HasYakus);
        Assert.Empty(tenpaiWinner.YakuLines);
        Assert.NotEmpty(tenpaiWinner.HandTiles);
    }

    [Fact]
    public void EndOfGame_ListsEveryPlayerInRankingOrder()
    {
        var game = new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(8));
        EndOfRoundInformationsPivot info;
        do
        {
            var result = game.Round.RunAutoPlay(new CancellationToken());
            info = game.NextRound(result.RonPlayerId);
        } while (!info.EndOfGame);
        var scores = game.ComputeCurrentRanking();

        var viewModel = new EndOfGameViewModel(scores);

        Assert.Equal(4, viewModel.Rows.Count);
        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(scores[i].Rank, viewModel.Rows[i].Rank);
            Assert.Equal(scores[i].Player.Name, viewModel.Rows[i].PlayerName);
            Assert.Equal(scores[i].Player.CurrentGamePoints, viewModel.Rows[i].Points);
            Assert.Equal(scores[i].Uma, viewModel.Rows[i].Uma.Value);
            Assert.Equal(scores[i].Score, viewModel.Rows[i].Score.Value);
        }
    }

    [Fact]
    public void Rules_ListsYakusWithoutNagashiManganFromLeastToMostValuable()
    {
        var viewModel = new RulesViewModel();

        Assert.DoesNotContain(YakuPivot.NagashiMangan, viewModel.Yakus);
        Assert.Equal(
            viewModel.Yakus.Select(y => y.ConcealedFanCount).OrderBy(f => f),
            viewModel.Yakus.Select(y => y.ConcealedFanCount));
    }
}
