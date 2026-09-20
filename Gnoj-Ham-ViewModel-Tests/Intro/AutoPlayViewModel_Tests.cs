using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;
using Gnoj_Ham_ViewModel;
using Gnoj_Ham_ViewModel_Tests.Fakes;

namespace Gnoj_Ham_ViewModel_Tests;

public class AutoPlayViewModel_Tests
{
    private readonly FakeDialogService _dialogs = new();

    private AutoPlayViewModel NewViewModel()
        => new(RulePivot.Default, _dialogs, new FakeUiDispatcher());

    [Fact]
    public void InitialState_OffersFourDefaultSeatsAndTenGames()
    {
        var viewModel = NewViewModel();

        Assert.Equal(AutoPlayState.Idle, viewModel.State);
        Assert.Equal("10", viewModel.GamesCountText);
        Assert.Equal(4, viewModel.Seats.Count);
        Assert.All(viewModel.Seats, seat => Assert.Equal(CpuManagerCatalog.Default, seat.Selected));
        Assert.Same(CpuManagerCatalog.Implementations, viewModel.CpuOptions);
        Assert.Null(viewModel.Results);
        Assert.Equal(0, viewModel.Progress);
    }

    [Fact]
    public void InitialState_ShowsTheStartControlsAndNoProgressBeforeAnyGameIsPlayed()
    {
        var viewModel = NewViewModel();

        Assert.True(viewModel.AreSeatsEditable);
        Assert.True(viewModel.IsActionPanelVisible);
        Assert.False(viewModel.IsWaitingPanelVisible);
        Assert.False(viewModel.AreResultsVisible);
    }

    [Fact]
    public async Task Start_WithAnInvalidGamesCount_ShowsAMessageAndStaysIdle()
    {
        var viewModel = NewViewModel();
        viewModel.GamesCountText = "many";

        await viewModel.StartCommand.ExecuteAsync(null);

        var (message, _) = Assert.Single(_dialogs.ShownMessages);
        Assert.Equal("Invalid number of games!", message);
        Assert.Equal(AutoPlayState.Idle, viewModel.State);
    }

    [Fact]
    public async Task Start_PlaysTheWholeBatchAndPublishesTheResults()
    {
        var viewModel = NewViewModel();
        viewModel.GamesCountText = "3";
        var completed = 0;
        viewModel.BatchCompleted += (_, _) => completed++;

        await viewModel.StartCommand.ExecuteAsync(null);

        Assert.Equal(AutoPlayState.Finished, viewModel.State);
        Assert.Equal(1, viewModel.Progress);
        Assert.Equal(1, completed);
        Assert.NotNull(viewModel.Results);
        Assert.Equal(4, viewModel.Results.Count);
        // Exactly one player finishes first in each game.
        Assert.Equal(3, viewModel.Results.Sum(p => p.FirstPlaceCount));
        Assert.Equal(3, viewModel.Results.Sum(p => p.LastPlaceCount));
    }

    [Fact]
    public async Task Start_WhenTheBatchIsOver_ShowsTheResultsInsteadOfTheProgress()
    {
        var viewModel = NewViewModel();
        viewModel.GamesCountText = "1";

        await viewModel.StartCommand.ExecuteAsync(null);

        Assert.True(viewModel.AreSeatsEditable);
        Assert.True(viewModel.IsActionPanelVisible);
        Assert.False(viewModel.IsWaitingPanelVisible);
        Assert.True(viewModel.AreResultsVisible);
    }

    [Fact]
    public async Task Start_NamesEachPlayerAfterTheLogicPlayingItsSeat()
    {
        var viewModel = NewViewModel();
        viewModel.GamesCountText = "1";
        var noDefense = CpuManagerCatalog.Implementations.First(o => o.Type == typeof(NoDefenseCpuManagerPivot));
        viewModel.Seats[1].Selected = noDefense;

        await viewModel.StartCommand.ExecuteAsync(null);

        var names = viewModel.Results!.Select(p => p.Name).ToList();
        Assert.Single(names, n => n.Contains($"({noDefense.DisplayName})"));
        Assert.Equal(3, names.Count(n => n.Contains($"({CpuManagerCatalog.Default.DisplayName})")));
    }

    [Fact]
    public async Task Start_WhileRunning_LocksTheSeatsAndHidesTheStartControls()
    {
        var viewModel = NewViewModel();
        viewModel.GamesCountText = "200";
        var task = viewModel.StartCommand.ExecuteAsync(null);

        try
        {
            Assert.Equal(AutoPlayState.Running, viewModel.State);
            Assert.False(viewModel.AreSeatsEditable);
            Assert.False(viewModel.IsActionPanelVisible);
            Assert.True(viewModel.IsWaitingPanelVisible);
            Assert.False(viewModel.AreResultsVisible);
        }
        finally
        {
            viewModel.Cancel();
            await task;
        }
    }

    [Fact]
    public async Task Cancel_StopsTheBatchWithoutPublishingResults()
    {
        var viewModel = NewViewModel();
        viewModel.GamesCountText = "200";
        var completed = 0;
        viewModel.BatchCompleted += (_, _) => completed++;

        var task = viewModel.StartCommand.ExecuteAsync(null);
        await Task.Delay(300);
        viewModel.Cancel();
        await task;

        Assert.Null(viewModel.Results);
        Assert.Equal(0, completed);
        Assert.NotEqual(AutoPlayState.Finished, viewModel.State);
        Assert.True(viewModel.Progress < 1);
    }
}
