using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;
using Gnoj_Ham_ViewModel;
using Gnoj_Ham_ViewModel.Services;
using Gnoj_Ham_ViewModel_Tests.Fakes;

namespace Gnoj_Ham_ViewModel_Tests;

// The whole game is played through the view-model, on real rounds from a fixed seed: the CPUs play for
// real (at full speed), the human player is a policy pressing the same buttons and clicking the same
// tiles a person would.
public class GameViewModel_Tests
{
    private const PlayerIndices Human = PlayerIndices.Zero;

    private readonly FakeDialogService _dialogs = new();
    private readonly FakeUserSettings _settings = new() { CpuSpeed = (int)CpuSpeedPivot.S0 };
    private readonly FakePlayerStatisticsStorage _storage = new();
    private readonly FakeAnimationService _animations = new();
    private readonly FakeSoundService _sounds = new();
    private IDelay _delay = new FakeDelay();

    private GameViewModel NewGame(int seed, DrivenDrawScenarios scenario = DrivenDrawScenarios.None)
    {
        var setup = new HumanGameSetup(
            "Me",
            RulePivot.Default,
            new PlayerStatisticsPivot(),
            DrivenDrawPivot.Resolve(scenario, Human),
            false,
            new Random(seed));

        return new GameViewModel(setup, _dialogs, _settings, _storage, new FakeUiDispatcher(), _delay, _animations, _sounds);
    }

    private static HumanControlsViewModel HumanOf(GameViewModel viewModel) => viewModel.Table.Human;

    private static bool MustDiscard(GameViewModel viewModel)
        => viewModel.Game.Round.IsHumanPlayer && viewModel.Game.Round.GetHand(Human).IsFullHand;

    // The player is asked to choose among some tiles of their hand (e.g. which make a chii), the others being disabled.
    private static bool IsChoosing(GameViewModel viewModel)
    {
        var seat = viewModel.Table.Seats[(int)Human];
        return seat.HandTiles.Concat(seat.PickTiles).Any(t => !t.IsEnabled);
    }

    // Clicks what a person could: the first tile that can be discarded (or, when the player has to choose
    // among some, the first of them).
    private static async Task DiscardAsync(GameViewModel viewModel, Func<TilePivot, bool>? keep = null)
    {
        var controls = HumanOf(viewModel);
        var tiles = viewModel.Table.Seats[(int)Human].HandTiles.Concat(viewModel.Table.Seats[(int)Human].PickTiles).ToList();
        var restricted = tiles.Any(t => !t.IsEnabled);
        var tile = tiles.First(t => t.IsEnabled
            && (restricted || (viewModel.Game.Round.CanDiscard(t.Tile!) && (keep == null || !keep(t.Tile!)))));

        await controls.SelectTileCommand.ExecuteAsync(tile);
    }

    // Presses the button the policy picks among those offered; the others are turned down.
    private static async Task DecideAsync(GameViewModel viewModel, params ActionButtonViewModel[] accepted)
    {
        var controls = HumanOf(viewModel);
        var choice = accepted.FirstOrDefault(b => b.IsAvailable) ?? controls.Skip;

        await choice.InvokeCommand.ExecuteAsync(null);
    }

    // Plays until the game is over, the human player accepting every call (except the abortive draw) - or
    // only the pons (and the wins), turning down the rest.
    private static async Task<bool> PlayToTheEndAsync(GameViewModel viewModel, bool acceptCalls, int maxSteps = 4000, bool ponsOnly = false)
    {
        var closed = false;
        viewModel.CloseRequested += (_, _) => closed = true;
        var controls = HumanOf(viewModel);

        await viewModel.StartCommand.ExecuteAsync(null);

        for (var step = 0; step < maxSteps; step++)
        {
            await viewModel.WhenIdleAsync();
            if (closed)
            {
                return true;
            }

            if (controls.IsPanelVisible)
            {
                if (ponsOnly)
                {
                    await DecideAsync(viewModel, controls.Ron, controls.Tsumo, controls.Pon);
                }
                else if (acceptCalls)
                {
                    await DecideAsync(viewModel, controls.Ron, controls.Tsumo, controls.Riichi, controls.Kan, controls.Pon, controls.Chii);
                }
                else
                {
                    await DecideAsync(viewModel, controls.Ron, controls.Tsumo);
                }
            }
            else if (MustDiscard(viewModel) || IsChoosing(viewModel))
            {
                await DiscardAsync(viewModel);
            }
            else
            {
                var round = viewModel.Game.Round;
                throw new InvalidOperationException(
                    $"The game waits for something the human player cannot do (step {step}): turn of {round.CurrentPlayerIndex}, "
                    + $"{round.GetHand(Human).ConcealedTiles.Count} concealed tiles, {round.GetHand(Human).DeclaredCombinations.Count} combinations, "
                    + $"offered: {string.Join(",", new[] { controls.Riichi, controls.Ron, controls.Tsumo, controls.Pon, controls.Chii, controls.Kan, controls.Skip }.Select(b => b.IsAvailable))}, "
                    + $"riichi: {round.IsRiichi(Human)}, pick: {controls.PickTile != null}.");
            }
        }

        return false;
    }

    // Plays on until the predicate holds while the human player has to decide (returns false if the game ends first).
    private async Task<GameViewModel?> PlayUntilAsync(Func<GameViewModel, bool> predicate, DrivenDrawScenarios scenario = DrivenDrawScenarios.None,
        Func<TilePivot, bool>? keep = null, int seeds = 200)
    {
        for (var seed = 1; seed <= seeds; seed++)
        {
            var viewModel = NewGame(seed, scenario);
            var closed = false;
            viewModel.CloseRequested += (_, _) => closed = true;
            var controls = HumanOf(viewModel);

            await viewModel.StartCommand.ExecuteAsync(null);

            for (var step = 0; step < 400 && !closed; step++)
            {
                await viewModel.WhenIdleAsync();
                if (predicate(viewModel))
                {
                    return viewModel;
                }

                if (controls.IsPanelVisible)
                {
                    await DecideAsync(viewModel);
                }
                else if (MustDiscard(viewModel) || IsChoosing(viewModel))
                {
                    await DiscardAsync(viewModel, keep);
                }
                else
                {
                    break;
                }
            }
        }

        return null;
    }

    private static bool IsRedDragon(TilePivot tile) => tile.Family == Families.Dragon && tile.Dragon == Dragons.Red;

    [Fact]
    public void Constructor_ShowsTheFirstRoundOnTheTable()
    {
        var viewModel = NewGame(1);

        var human = viewModel.Table.Seats[(int)Human];
        Assert.Equal("Me", human.Name);
        Assert.Equal(viewModel.Game.Round.GetHand(Human).ConcealedTiles.Count, human.HandTiles.Count + human.PickTiles.Count);
        Assert.Equal(viewModel.Game.Round.WallTiles.Count, viewModel.Table.WallTilesLeft);
        Assert.Single(viewModel.Table.Seats, s => s.IsCurrent);
    }

    [Fact]
    public void Options_ReadAndChangeTheSettingsTheGameRunsOn()
    {
        _settings.ChronoSpeed = (int)ChronoPivot.Short;
        var viewModel = NewGame(1);

        Assert.Equal((int)ChronoPivot.Short, viewModel.Options.ChronoSpeedIndex);
        Assert.Equal((int)CpuSpeedPivot.S0, viewModel.Options.CpuSpeedIndex);

        viewModel.Options.ChronoSpeedIndex = (int)ChronoPivot.None;

        Assert.Equal((int)ChronoPivot.None, _settings.ChronoSpeed);
    }

    [Fact]
    public async Task Start_PlaysTheCpusUntilTheHumanPlayerIsNeeded()
    {
        var viewModel = NewGame(1);

        await viewModel.StartCommand.ExecuteAsync(null);
        await viewModel.WhenIdleAsync();

        Assert.True(MustDiscard(viewModel) || HumanOf(viewModel).IsPanelVisible);
    }

    [Fact]
    public async Task Discard_HandsThePlayToTheCpusThenBackToTheHumanPlayer()
    {
        var viewModel = NewGame(1);
        await viewModel.StartCommand.ExecuteAsync(null);
        await viewModel.WhenIdleAsync();
        Assert.True(MustDiscard(viewModel));
        var discards = viewModel.Game.Round.GetDiscard(Human).Count;

        await DiscardAsync(viewModel);
        await viewModel.WhenIdleAsync();

        Assert.Equal(discards + 1, viewModel.Game.Round.GetDiscard(Human).Count);
        Assert.NotEmpty(viewModel.Table.Seats[(int)Human].DiscardRows[0]);
        Assert.True(MustDiscard(viewModel) || HumanOf(viewModel).IsPanelVisible || viewModel.Game.Round.IsHumanPlayer);
    }

    [Fact]
    public async Task Discard_WhileTheCpusArePlaying_IsIgnored()
    {
        var viewModel = NewGame(1);
        await viewModel.StartCommand.ExecuteAsync(null);
        await viewModel.WhenIdleAsync();
        var controls = HumanOf(viewModel);
        var discards = viewModel.Game.Round.GetDiscard(Human).Count;

        // The same tile, discarded a second time by a double click: nothing is left of it in the hand.
        var tile = controls.FirstDiscardableTile();
        await controls.SelectTileCommand.ExecuteAsync(tile);
        await viewModel.WhenIdleAsync();
        var afterFirst = viewModel.Game.Round.GetDiscard(Human).Count;
        await controls.SelectTileCommand.ExecuteAsync(tile);

        Assert.Equal(discards + 1, afterFirst);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public async Task AWholeGame_IsPlayedToTheEnd(int seed, bool acceptCalls)
    {
        var viewModel = NewGame(seed);

        var finished = await PlayToTheEndAsync(viewModel, acceptCalls);

        Assert.True(finished);
        Assert.IsType<EndOfGameViewModel>(_dialogs.ShownViewModels[^1]);
        Assert.All(_dialogs.ShownViewModels.SkipLast(1), vm => Assert.IsType<ScoreViewModel>(vm));
        Assert.True(_dialogs.ShownViewModels.Count >= 5);
        // The statistics are saved once per round.
        Assert.Equal(_dialogs.ShownViewModels.Count - 1, _storage.SaveCount);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(8)]
    public async Task AWholeGame_TakingPonsAndTurningDownTheChii_ShowsNoWarning(int seed)
    {
        // A chii turned down once the hand is open used to be made anyway by the engine, which left the
        // human player without the tile they had to pick, and the game warning about it.
        var viewModel = NewGame(seed);

        var finished = await PlayToTheEndAsync(viewModel, acceptCalls: false, ponsOnly: true);

        Assert.True(finished);
        Assert.Empty(_dialogs.ShownMessages);
    }

    [Fact]
    public async Task AWholeGame_AnnouncesTheCallsTheHumanPlayerMakes()
    {
        var viewModel = NewGame(1);

        await PlayToTheEndAsync(viewModel, acceptCalls: true);

        var humanCalls = _animations.AnnouncedCalls.Where(a => a.playerIndex == Human).Select(a => a.call).ToList();
        Assert.Contains(CallTypes.Pon, humanCalls.Concat(_animations.AnnouncedCalls.Select(a => a.call)));
        Assert.All(humanCalls, call => Assert.NotEqual(CallTypes.NoCall, call));
        Assert.Contains(_animations.AnnouncedCalls, a => a.playerIndex != Human);
    }

    [Fact]
    public async Task ARoundEnding_ShowsTheScoresThenStartsTheNextRoundOnTheTable()
    {
        var viewModel = NewGame(1);
        var closed = false;
        viewModel.CloseRequested += (_, _) => closed = true;
        var wallBefore = viewModel.Table.WallTilesLeft;

        // Plays the first round only.
        var controls = HumanOf(viewModel);
        await viewModel.StartCommand.ExecuteAsync(null);
        while (_dialogs.ShownViewModels.Count == 0 && !closed)
        {
            await viewModel.WhenIdleAsync();
            if (controls.IsPanelVisible)
            {
                await DecideAsync(viewModel);
            }
            else
            {
                await DiscardAsync(viewModel);
            }
        }
        await viewModel.WhenIdleAsync();

        Assert.IsType<ScoreViewModel>(_dialogs.ShownViewModels[0]);
        Assert.False(closed);
        Assert.Equal(1, _storage.SaveCount);
        // A fresh wall on the table: the second round is on.
        Assert.True(viewModel.Table.WallTilesLeft >= wallBefore - 4);
        Assert.Empty(viewModel.Table.Seats[(int)PlayerIndices.One].Combinations);
    }

    [Fact]
    public async Task ARoundEnding_WhenTheStatisticsCannotBeSaved_ShowsTheError()
    {
        _storage.SaveError = "disk full";
        var viewModel = NewGame(1);

        await PlayToTheEndAsync(viewModel, acceptCalls: false);

        Assert.Contains(_dialogs.ShownMessages, m => m.message.Contains("disk full"));
    }

    [Fact]
    public async Task SoundsEnabled_TicksOnEveryPick()
    {
        _settings.PlaySounds = true;
        var viewModel = NewGame(1);

        await viewModel.StartCommand.ExecuteAsync(null);

        Assert.True(_sounds.TickCount > 0);
    }

    [Fact]
    public async Task SoundsDisabled_StaysSilent()
    {
        _settings.PlaySounds = false;
        var viewModel = NewGame(1);

        await viewModel.StartCommand.ExecuteAsync(null);

        Assert.Equal(0, _sounds.TickCount);
    }

    [Fact]
    public async Task ACallOffered_ShowsThePanelToTheHumanPlayer()
    {
        var viewModel = await PlayUntilAsync(g => HumanOf(g).Pon.IsAvailable && HumanOf(g).IsPanelVisible);

        Assert.NotNull(viewModel);
        var controls = HumanOf(viewModel);
        Assert.True(controls.Skip.IsAvailable);
        Assert.True(viewModel.Game.Round.CanCallPon(Human));
    }

    [Fact]
    public async Task Pon_TakesTheTileAndLetsTheHumanPlayerDiscard()
    {
        var viewModel = await PlayUntilAsync(g => HumanOf(g).Pon.IsAvailable && HumanOf(g).IsPanelVisible);
        Assert.NotNull(viewModel);
        var controls = HumanOf(viewModel);

        await controls.Pon.InvokeCommand.ExecuteAsync(null);
        await viewModel.WhenIdleAsync();

        Assert.Single(viewModel.Table.Seats[(int)Human].Combinations);
        Assert.Contains(_animations.AnnouncedCalls, a => a.call == CallTypes.Pon && a.playerIndex == Human);
        Assert.True(MustDiscard(viewModel));
        Assert.False(controls.IsPanelVisible);
    }

    [Fact]
    public async Task Chii_LetsTheHumanPlayerPickTheTilesThenDiscard()
    {
        var viewModel = await PlayUntilAsync(g => HumanOf(g).Chii.IsAvailable && HumanOf(g).IsPanelVisible);
        Assert.NotNull(viewModel);
        var controls = HumanOf(viewModel);

        await controls.Chii.InvokeCommand.ExecuteAsync(null);
        await viewModel.WhenIdleAsync();

        // Either the only choice went through at once, or the player is asked to choose.
        if (viewModel.Table.Seats[(int)Human].Combinations.Count == 0)
        {
            var tiles = viewModel.Table.Seats[(int)Human].HandTiles.Concat(viewModel.Table.Seats[(int)Human].PickTiles).ToList();
            Assert.Contains(tiles, t => !t.IsEnabled);
            var choice = tiles.First(t => t.IsEnabled);
            await controls.SelectTileCommand.ExecuteAsync(choice);
            await viewModel.WhenIdleAsync();
        }

        Assert.Single(viewModel.Table.Seats[(int)Human].Combinations);
        Assert.Contains(_animations.AnnouncedCalls, a => a.call == CallTypes.Chii && a.playerIndex == Human);
    }

    [Fact]
    public async Task TurningTheCallsDown_PlaysOnWithoutTheCall()
    {
        var viewModel = await PlayUntilAsync(g => HumanOf(g).Pon.IsAvailable && HumanOf(g).IsPanelVisible);
        Assert.NotNull(viewModel);
        var controls = HumanOf(viewModel);
        var discardsBefore = Enum.GetValues<PlayerIndices>().Sum(p => viewModel.Game.Round.GetDiscard(p).Count);

        await controls.Skip.InvokeCommand.ExecuteAsync(null);
        await viewModel.WhenIdleAsync();

        Assert.Empty(viewModel.Table.Seats[(int)Human].Combinations);
        Assert.DoesNotContain(_animations.AnnouncedCalls, a => a.call == CallTypes.Pon && a.playerIndex == Human);
        // The game went on: more tiles were discarded, or the human player picked theirs.
        Assert.True(Enum.GetValues<PlayerIndices>().Sum(p => viewModel.Game.Round.GetDiscard(p).Count) > discardsBefore || controls.PickTile != null);
    }

    [Fact]
    public async Task ACloseKanOfferedBeforeTheDiscard_CanBeTurnedDown()
    {
        var viewModel = await PlayUntilAsync(g => MustDiscard(g) && HumanOf(g).Kan.IsAvailable && HumanOf(g).IsPanelVisible,
            DrivenDrawScenarios.HumanInitialKan, seeds: 5);
        Assert.NotNull(viewModel);
        var controls = HumanOf(viewModel);
        var discards = viewModel.Game.Round.GetDiscard(Human).Count;

        await controls.Skip.InvokeCommand.ExecuteAsync(null);
        await viewModel.WhenIdleAsync();

        Assert.Empty(viewModel.Table.Seats[(int)Human].Combinations);
        Assert.False(controls.Kan.IsAvailable);
        // The tile just picked is discarded on the player's behalf, as when calls are turned down.
        Assert.True(MustDiscard(viewModel) || viewModel.Game.Round.GetDiscard(Human).Count > discards);
    }

    [Fact]
    public async Task ACloseKanOfferedBeforeTheDiscard_CanBeAccepted()
    {
        var viewModel = await PlayUntilAsync(g => MustDiscard(g) && HumanOf(g).Kan.IsAvailable && HumanOf(g).IsPanelVisible,
            DrivenDrawScenarios.HumanInitialKan, seeds: 5);
        Assert.NotNull(viewModel);
        var controls = HumanOf(viewModel);

        await controls.Kan.InvokeCommand.ExecuteAsync(null);
        await viewModel.WhenIdleAsync();

        Assert.Single(viewModel.Table.Seats[(int)Human].Combinations);
        Assert.Contains(_animations.AnnouncedCalls, a => a.call == CallTypes.Kan && a.playerIndex == Human);
    }

    [Fact]
    public async Task AnOpenKanOnTheDiscardOfThePlayerBefore_CanBeTurnedDown()
    {
        // The regression this guards: the turn being already the human player's, turning the kan down was
        // handled as if it had been offered before a discard, and got stuck (it used to crash).
        var viewModel = await PlayUntilAsync(
            g => HumanOf(g).Kan.IsAvailable && HumanOf(g).IsPanelVisible && !g.Game.Round.GetHand(Human).IsFullHand,
            DrivenDrawScenarios.HumanOpenKanChance,
            keep: IsRedDragon);
        Assert.NotNull(viewModel);
        var controls = HumanOf(viewModel);

        await controls.Skip.InvokeCommand.ExecuteAsync(null);
        await viewModel.WhenIdleAsync();

        Assert.Empty(viewModel.Table.Seats[(int)Human].Combinations);
        Assert.NotNull(controls.PickTile);
        Assert.True(MustDiscard(viewModel));
    }

    [Fact]
    public async Task AnOpenKanOnTheDiscardOfThePlayerBefore_CanBeAccepted()
    {
        var viewModel = await PlayUntilAsync(
            g => HumanOf(g).Kan.IsAvailable && HumanOf(g).IsPanelVisible && !g.Game.Round.GetHand(Human).IsFullHand,
            DrivenDrawScenarios.HumanOpenKanChance,
            keep: IsRedDragon);
        Assert.NotNull(viewModel);
        var controls = HumanOf(viewModel);

        await controls.Kan.InvokeCommand.ExecuteAsync(null);
        await viewModel.WhenIdleAsync();

        var combination = Assert.Single(viewModel.Table.Seats[(int)Human].Combinations);
        Assert.Equal(4, combination.Tiles.Count);
        Assert.Contains(_animations.AnnouncedCalls, a => a.call == CallTypes.Kan && a.playerIndex == Human);
        Assert.True(MustDiscard(viewModel));
    }

    [Fact]
    public async Task Riichi_WaitsForItsAnnouncementBeforeAskingWhichTileToDiscard()
    {
        var viewModel = await PlayUntilAsync(g => HumanOf(g).Riichi.IsAvailable && HumanOf(g).IsPanelVisible, seeds: 300);
        Assert.NotNull(viewModel);
        var controls = HumanOf(viewModel);
        _animations.Hold = true;

        var riichi = controls.Riichi.InvokeCommand.ExecuteAsync(null);

        Assert.False(riichi.IsCompleted);
        Assert.False(controls.IsPanelVisible);
        Assert.Contains(_animations.AnnouncedCalls, a => a.call == CallTypes.Riichi && a.playerIndex == Human);
        var tiles = viewModel.Table.Seats[(int)Human].HandTiles.Concat(viewModel.Table.Seats[(int)Human].PickTiles);
        Assert.All(tiles, t => Assert.True(t.IsEnabled));

        _animations.Release();
        await riichi;
        await viewModel.WhenIdleAsync();

        // Either the only tile went through, or the player is asked to choose among some.
        var declared = viewModel.Table.Seats[(int)Human].HasRiichiStick;
        var asked = viewModel.Table.Seats[(int)Human].HandTiles.Concat(viewModel.Table.Seats[(int)Human].PickTiles).Any(t => !t.IsEnabled);
        Assert.True(declared || asked);
    }

    [Fact]
    public async Task ACpuCall_HoldsTheGameUntilItsAnnouncementHasBeenSeen()
    {
        _animations.Hold = true;
        GameViewModel? viewModel = null;
        Task? start = null;

        // Plays until a CPU makes a call, whose announcement is held.
        for (var seed = 1; seed <= 300 && viewModel == null; seed++)
        {
            var candidate = NewGame(seed);
            start = candidate.StartCommand.ExecuteAsync(null);
            var deadline = DateTime.UtcNow.AddSeconds(2);
            while (!start.IsCompleted && _animations.AnnouncedCalls.Count == 0 && DateTime.UtcNow < deadline)
            {
                await Task.Delay(1);
            }

            if (_animations.AnnouncedCalls.Count > 0)
            {
                viewModel = candidate;
            }
        }
        Assert.NotNull(viewModel);
        Assert.NotNull(start);
        var discardsBefore = Enum.GetValues<PlayerIndices>().Sum(p => viewModel.Game.Round.GetDiscard(p).Count);

        await Task.Delay(100);

        // The call has been announced, and nothing else happens for as long as the announcement lasts.
        Assert.False(start.IsCompleted);
        Assert.Single(_animations.AnnouncedCalls);
        Assert.NotEqual(Human, _animations.AnnouncedCalls[0].playerIndex);
        Assert.Equal(discardsBefore, Enum.GetValues<PlayerIndices>().Sum(p => viewModel.Game.Round.GetDiscard(p).Count));

        _animations.Hold = false;
        _animations.Release();
        await start;
        await viewModel.WhenIdleAsync();

        Assert.True(Enum.GetValues<PlayerIndices>().Sum(p => viewModel.Game.Round.GetDiscard(p).Count) > discardsBefore);
    }

    [Fact]
    public async Task TheDecisionTimer_WhenCallsAreOffered_TurnsThemDownOnceElapsed()
    {
        var manualDelay = new ManualDelay();
        _delay = manualDelay;
        _settings.ChronoSpeed = (int)ChronoPivot.Short;
        var viewModel = await PlayUntilAsync(g => HumanOf(g).Pon.IsAvailable && HumanOf(g).IsPanelVisible);
        Assert.NotNull(viewModel);
        Assert.Contains(TimeSpan.FromSeconds(5), manualDelay.PendingDelays);
        var discardsBefore = Enum.GetValues<PlayerIndices>().Sum(p => viewModel.Game.Round.GetDiscard(p).Count);

        manualDelay.ElapseAll();
        await viewModel.WhenIdleAsync();

        // The offer was turned down, and the game went on: more tiles were discarded, or the human player picked theirs.
        Assert.Empty(viewModel.Table.Seats[(int)Human].Combinations);
        var discardsAfter = Enum.GetValues<PlayerIndices>().Sum(p => viewModel.Game.Round.GetDiscard(p).Count);
        Assert.True(discardsAfter > discardsBefore || HumanOf(viewModel).PickTile != null);
    }

    [Fact]
    public async Task TheDecisionTimer_WhenThePlayerHasToDiscard_DiscardsThePickedTileOnceElapsed()
    {
        var manualDelay = new ManualDelay();
        _delay = manualDelay;
        _settings.ChronoSpeed = (int)ChronoPivot.Short;
        var viewModel = await PlayUntilAsync(g => MustDiscard(g) && !HumanOf(g).IsPanelVisible && HumanOf(g).PickTile != null);
        Assert.NotNull(viewModel);
        Assert.Contains(TimeSpan.FromSeconds(5), manualDelay.PendingDelays);
        var picked = HumanOf(viewModel).PickTile!.Tile;
        var discards = viewModel.Game.Round.GetDiscard(Human).Count;

        manualDelay.ElapseAll();
        await viewModel.WhenIdleAsync();

        Assert.Equal(discards + 1, viewModel.Game.Round.GetDiscard(Human).Count);
        Assert.Same(picked, viewModel.Game.Round.GetDiscard(Human)[^1]);
    }

    [Fact]
    public async Task TheDecisionTimer_IsStoppedByTheHumanPlayerActing()
    {
        var manualDelay = new ManualDelay();
        _delay = manualDelay;
        _settings.ChronoSpeed = (int)ChronoPivot.Long;
        var viewModel = await PlayUntilAsync(g => HumanOf(g).Pon.IsAvailable && HumanOf(g).IsPanelVisible);
        Assert.NotNull(viewModel);
        Assert.Contains(TimeSpan.FromSeconds(20), manualDelay.PendingDelays);

        await HumanOf(viewModel).Pon.InvokeCommand.ExecuteAsync(null);
        await viewModel.WhenIdleAsync();
        var combinations = viewModel.Table.Seats[(int)Human].Combinations.Count;
        manualDelay.ElapseAll();
        await viewModel.WhenIdleAsync();

        // The timer of the call is gone: elapsing what is left does not undo or redo it.
        Assert.Equal(1, combinations);
    }

    [Fact]
    public async Task TheDecisionTimer_WhenDisabled_NeverStarts()
    {
        var manualDelay = new ManualDelay();
        _delay = manualDelay;
        _settings.ChronoSpeed = (int)ChronoPivot.None;

        var viewModel = await PlayUntilAsync(g => HumanOf(g).Pon.IsAvailable && HumanOf(g).IsPanelVisible);

        Assert.NotNull(viewModel);
        Assert.Empty(manualDelay.PendingDelays);
    }

    [Fact]
    public async Task Cancel_StopsTheDecisionTimer()
    {
        var manualDelay = new ManualDelay();
        _delay = manualDelay;
        _settings.ChronoSpeed = (int)ChronoPivot.Short;
        var viewModel = await PlayUntilAsync(g => HumanOf(g).Pon.IsAvailable && HumanOf(g).IsPanelVisible);
        Assert.NotNull(viewModel);
        Assert.NotEmpty(manualDelay.PendingDelays);

        viewModel.Cancel();

        Assert.Empty(manualDelay.PendingDelays);
    }

    [Fact]
    public async Task Cancel_StopsTheGameFromPlayingOn()
    {
        var viewModel = NewGame(1);
        viewModel.Cancel();

        await viewModel.StartCommand.ExecuteAsync(null);

        // The CPUs were cancelled before playing: the human player never got their turn.
        Assert.False(MustDiscard(viewModel) && viewModel.Game.Round.GetDiscard(PlayerIndices.One).Count > 0);
    }

    [Fact]
    public void NewGame_AsksTheWindowToClose()
    {
        var viewModel = NewGame(1);
        var closed = 0;
        viewModel.CloseRequested += (_, _) => closed++;

        viewModel.NewGameCommand.Execute(null);

        Assert.Equal(1, closed);
    }

    [Fact]
    public void ShowRules_OpensTheRules()
    {
        NewGame(1).ShowRulesCommand.Execute(null);

        Assert.IsType<RulesViewModel>(Assert.Single(_dialogs.ShownViewModels));
    }

    [Fact]
    public void ShowPlayerStats_OpensTheStatistics()
    {
        NewGame(1).ShowPlayerStatsCommand.Execute(null);

        Assert.IsType<PlayerSaveStatsViewModel>(Assert.Single(_dialogs.ShownViewModels));
        Assert.Empty(_dialogs.ShownMessages);
    }

    [Fact]
    public void ShowPlayerStats_WhenTheyCannotBeLoaded_ShowsTheErrorAndEmptyStatistics()
    {
        _storage.LoadError = "corrupted file";

        NewGame(1).ShowPlayerStatsCommand.Execute(null);

        var (message, _) = Assert.Single(_dialogs.ShownMessages);
        Assert.Contains("corrupted file", message);
        Assert.IsType<PlayerSaveStatsViewModel>(Assert.Single(_dialogs.ShownViewModels));
    }

    [Fact]
    public void ShowAbout_ShowsAMessage()
    {
        NewGame(1).ShowAboutCommand.Execute(null);

        Assert.Single(_dialogs.ShownMessages);
    }
}
