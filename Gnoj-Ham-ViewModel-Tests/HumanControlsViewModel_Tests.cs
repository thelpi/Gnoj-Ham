using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;
using Gnoj_Ham_ViewModel;
using Gnoj_Ham_ViewModel_Tests.Fakes;

namespace Gnoj_Ham_ViewModel_Tests;

// The controls are exercised on real rounds played from a fixed seed, the CPUs playing until the
// engine gives the hand back to the human player.
public class HumanControlsViewModel_Tests
{
    private const PlayerIndices Human = PlayerIndices.Zero;

    private readonly FakeUserSettings _settings = new();
    private TableViewModel? _table;

    // The tiles of the human player's hand, apart from the one just picked.
    private IReadOnlyList<TileViewModel> HandTiles => _table!.Seats[(int)Human].HandTiles;

    private static GamePivot NewGame(int seed)
        => new("Me", RulePivot.Default, new PlayerStatisticsPivot(), new Random(seed), null);

    private HumanControlsViewModel NewControls(GamePivot game, TilePivot? pickTile = null)
    {
        _table = new TableViewModel(game, Human, false, _settings);
        _table.RefreshRound();
        _table.Seats[(int)Human].RefreshHand(pickTile);
        return _table.Human;
    }

    // Plays on, the human player declining everything, until the predicate holds after the CPUs have played.
    private static GamePivot FindGame(Func<GamePivot, bool> predicate)
    {
        for (var seed = 1; seed < 300; seed++)
        {
            var game = NewGame(seed);
            var declined = false;
            for (var i = 0; i < 400; i++)
            {
                var result = game.Round.RunAutoPlay(default, declined, false, false, false, null, 0);
                if (result.EndOfRound)
                {
                    break;
                }
                if (predicate(game))
                {
                    return game;
                }

                declined = false;
                if (result.HumanCall is null || result.HumanCall.Value.call == CallTypes.NoCall)
                {
                    var tile = game.Round.GetHand(Human).ConcealedTiles.FirstOrDefault(t => game.Round.CanDiscard(t));
                    if (tile != null && game.Round.Discard(tile))
                    {
                        continue;
                    }
                }
                declined = true;
            }
        }

        throw new InvalidOperationException("No game matching the predicate in the first seeds.");
    }

    private static bool IsHumanFullHand(GamePivot game)
        => game.Round.IsHumanPlayer && game.Round.GetHand(Human).IsFullHand;

    // The human player holds all their tiles: the last one is taken as the one just picked.
    private (GamePivot game, HumanControlsViewModel controls) AtDiscardTime()
    {
        var game = FindGame(IsHumanFullHand);
        return (game, NewControls(game, game.Round.GetHand(Human).ConcealedTiles[^1]));
    }

    [Fact]
    public void InitialState_OffersNothing()
    {
        var controls = NewControls(NewGame(1));

        Assert.False(controls.IsPanelVisible);
        Assert.False(controls.IsCallOffered);
        Assert.All(AllButtons(controls), b =>
        {
            Assert.False(b.IsAvailable);
            Assert.False(b.IsAdvised);
        });
    }

    private static ActionButtonViewModel[] AllButtons(HumanControlsViewModel c)
        => new[] { c.Riichi, c.KyuushuKyuuhai, c.Tsumo, c.Ron, c.Pon, c.Chii, c.Kan, c.Skip };

    [Fact]
    public void ShowActions_OnAnotherPlayersDiscardThePlayerCanPon_OffersPon()
    {
        var game = FindGame(g => !g.Round.IsHumanPlayer && g.Round.CanCallPon(Human));
        var controls = NewControls(game);

        var offered = controls.ShowActions(cpuPlay: true);

        Assert.True(offered);
        Assert.True(controls.Pon.IsAvailable);
        Assert.True(controls.IsCallOffered);
        Assert.False(controls.IsPanelVisible);
    }

    [Fact]
    public void ShowActions_OnAnotherPlayersDiscardThePlayerCanChii_OffersChii()
    {
        var game = FindGame(g => g.Round.IsHumanPlayer && g.Round.CanCallChii().Count > 0 && !g.Round.GetHand(Human).IsFullHand);
        var controls = NewControls(game);

        var offered = controls.ShowActions(cpuPlay: true);

        Assert.True(offered);
        Assert.True(controls.Chii.IsAvailable);
    }

    [Fact]
    public void ShowActions_WhenNothingCanBeCalled_OffersNothing()
    {
        var game = FindGame(g => !g.Round.IsHumanPlayer
            && !g.Round.CanCallPon(Human)
            && g.Round.CanCallKan(Human).Count == 0);
        var controls = NewControls(game);

        var offered = controls.ShowActions(cpuPlay: true);

        Assert.False(offered);
        Assert.False(controls.Pon.IsAvailable);
        Assert.False(controls.Kan.IsAvailable);
    }

    [Fact]
    public void ShowActions_WithoutTheAdviceEnabled_AdvisesNothing()
    {
        var game = FindGame(g => !g.Round.IsHumanPlayer && g.Round.CanCallPon(Human));
        var controls = NewControls(game);
        _settings.DiscardTip = false;

        controls.ShowActions(cpuPlay: true);

        Assert.All(AllButtons(controls), b => Assert.False(b.IsAdvised));
    }

    [Fact]
    public void ShowActions_WithTheAdviceEnabled_AdvisesPonOrElseTurningItDown()
    {
        var game = FindGame(g => !g.Round.IsHumanPlayer && g.Round.CanCallPon(Human));
        var controls = NewControls(game);
        _settings.DiscardTip = true;

        controls.ShowActions(cpuPlay: true);

        var advisedPon = game.Round.Advisor!.PonDecision(Human);
        Assert.Equal(advisedPon, controls.Pon.IsAdvised);
        // Turning the calls down is the advice whenever no offered call is the advised one.
        if (!controls.Pon.IsAdvised && !controls.Chii.IsAdvised && !controls.Kan.IsAdvised)
        {
            Assert.True(controls.Skip.IsAdvised);
        }
    }

    [Fact]
    public void ShowActions_ResetsWhatWasOfferedBefore()
    {
        var game = FindGame(g => !g.Round.IsHumanPlayer && g.Round.CanCallPon(Human));
        var controls = NewControls(game);
        controls.ShowActions(cpuPlay: true);
        controls.ShowPanel();

        controls.ShowActions();

        Assert.All(AllButtons(controls), b => Assert.False(b.IsAvailable));
        Assert.False(controls.IsPanelVisible);
    }

    [Fact]
    public void ShowPanel_MakesTheOptionToTurnCallsDownAvailable()
    {
        var controls = NewControls(NewGame(1));

        controls.ShowPanel();

        Assert.True(controls.IsPanelVisible);
        Assert.True(controls.Skip.IsAvailable);
    }

    [Fact]
    public void HidePanel_LeavesWhatWasOfferedAsItIs()
    {
        var controls = NewControls(NewGame(1));
        controls.ShowDecision(CallTypes.Ron, false);

        controls.HidePanel();

        Assert.False(controls.IsPanelVisible);
        Assert.True(controls.Ron.IsAvailable);
        Assert.True(controls.IsCallOffered);
    }

    [Theory]
    [InlineData(CallTypes.Ron)]
    [InlineData(CallTypes.Tsumo)]
    [InlineData(CallTypes.Riichi)]
    [InlineData(CallTypes.KyuushuKyuuhai)]
    public void ShowDecision_OffersThatCallAndTheOptionToTurnItDown(CallTypes call)
    {
        var controls = NewControls(NewGame(1));

        controls.ShowDecision(call, false);

        Assert.True(controls.IsPanelVisible);
        Assert.True(controls.Skip.IsAvailable);
        var offered = AllButtons(controls).Where(b => b.IsAvailable).ToList();
        Assert.Equal(2, offered.Count);
        var expected = call switch
        {
            CallTypes.Ron => controls.Ron,
            CallTypes.Tsumo => controls.Tsumo,
            CallTypes.Riichi => controls.Riichi,
            _ => controls.KyuushuKyuuhai,
        };
        Assert.Contains(expected, offered);
    }

    [Fact]
    public void ShowDecision_ForAnAdvisedRiichi_AdvisesRiichi()
    {
        var controls = NewControls(NewGame(1));
        _settings.DiscardTip = true;

        controls.ShowDecision(CallTypes.Riichi, riichiAdvised: true);

        Assert.True(controls.Riichi.IsAdvised);
        Assert.False(controls.Skip.IsAdvised);
    }

    [Fact]
    public void ShowDecision_ForARiichiNotAdvised_AdvisesTurningItDown()
    {
        var controls = NewControls(NewGame(1));
        _settings.DiscardTip = true;

        controls.ShowDecision(CallTypes.Riichi, riichiAdvised: false);

        Assert.False(controls.Riichi.IsAdvised);
        Assert.True(controls.Skip.IsAdvised);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShowDecision_ForARiichiWithoutTheAdviceEnabled_AdvisesNothing(bool riichiAdvised)
    {
        var controls = NewControls(NewGame(1));
        _settings.DiscardTip = false;

        controls.ShowDecision(CallTypes.Riichi, riichiAdvised);

        Assert.True(controls.Riichi.IsAvailable);
        Assert.False(controls.Riichi.IsAdvised);
        Assert.False(controls.Skip.IsAdvised);
    }

    [Fact]
    public void ShowActions_DropsTheAbortiveDrawOfferedBefore()
    {
        var controls = NewControls(NewGame(1));
        controls.ShowDecision(CallTypes.KyuushuKyuuhai, false);

        controls.ShowActions(preDiscard: true, skippedInnerKan: true);

        Assert.False(controls.KyuushuKyuuhai.IsAvailable);
    }

    [Fact]
    public void Reset_HidesEverything()
    {
        var controls = NewControls(NewGame(1));
        controls.ShowDecision(CallTypes.Riichi, true);

        controls.Reset();

        Assert.False(controls.IsPanelVisible);
        Assert.All(AllButtons(controls), b =>
        {
            Assert.False(b.IsAvailable);
            Assert.False(b.IsAdvised);
        });
    }

    [Fact]
    public void ActionButton_CanOnlyBePressedWhileItIsAvailable()
    {
        var controls = NewControls(NewGame(1));
        var requested = new List<CallTypes>();
        controls.CallRequested += requested.Add;

        Assert.False(controls.Pon.InvokeCommand.CanExecute(null));

        controls.Pon.IsAvailable = true;
        Assert.True(controls.Pon.InvokeCommand.CanExecute(null));
        controls.Pon.InvokeCommand.Execute(null);

        Assert.Equal(new[] { CallTypes.Pon }, requested);
    }

    [Fact]
    public void ActionButton_TellsItsCommandWhenItBecomesAvailable()
    {
        var controls = NewControls(NewGame(1));
        var raised = 0;
        controls.Kan.InvokeCommand.CanExecuteChanged += (_, _) => raised++;

        controls.Kan.IsAvailable = true;

        Assert.Equal(1, raised);
    }

    [Theory]
    [InlineData(CallTypes.Riichi)]
    [InlineData(CallTypes.KyuushuKyuuhai)]
    [InlineData(CallTypes.Tsumo)]
    [InlineData(CallTypes.Ron)]
    [InlineData(CallTypes.Pon)]
    [InlineData(CallTypes.Chii)]
    [InlineData(CallTypes.Kan)]
    public void EachActionButton_RequestsItsOwnCall(CallTypes call)
    {
        var controls = NewControls(NewGame(1));
        var requested = new List<CallTypes>();
        controls.CallRequested += requested.Add;
        var button = call switch
        {
            CallTypes.Riichi => controls.Riichi,
            CallTypes.KyuushuKyuuhai => controls.KyuushuKyuuhai,
            CallTypes.Tsumo => controls.Tsumo,
            CallTypes.Ron => controls.Ron,
            CallTypes.Pon => controls.Pon,
            CallTypes.Chii => controls.Chii,
            _ => controls.Kan,
        };
        button.IsAvailable = true;

        button.InvokeCommand.Execute(null);

        Assert.Equal(new[] { call }, requested);
    }

    [Fact]
    public void SkipButton_RequestsToTurnTheCallsDown()
    {
        var controls = NewControls(NewGame(1));
        var skipped = 0;
        controls.SkipRequested += () => skipped++;
        controls.ShowPanel();

        controls.Skip.InvokeCommand.Execute(null);

        Assert.Equal(1, skipped);
    }

    [Fact]
    public void RequestCall_MakesTheCallWhetherItIsOfferedOrNot()
    {
        var controls = NewControls(NewGame(1));
        var requested = new List<CallTypes>();
        controls.CallRequested += requested.Add;

        controls.RequestCall(CallTypes.Ron);

        Assert.Equal(new[] { CallTypes.Ron }, requested);
        Assert.False(controls.Ron.IsAvailable);
    }

    [Fact]
    public void PickTile_IsTheTileJustPickedIfAny()
    {
        var game = NewGame(1);
        var pick = game.Round.GetHand(Human).ConcealedTiles[3];

        Assert.Null(NewControls(game).PickTile);
        Assert.Same(pick, NewControls(game, pick).PickTile!.Tile);
    }

    [Fact]
    public void SelectTile_OnAHandTile_RequestsItsDiscard()
    {
        var (_, controls) = AtDiscardTime();
        var discarded = new List<TilePivot>();
        controls.DiscardRequested += discarded.Add;
        var viewModel = controls.FirstDiscardableTile();

        controls.SelectTileCommand.Execute(viewModel);

        Assert.Same(viewModel.Tile, Assert.Single(discarded));
    }

    [Fact]
    public void SelectTile_OnThePickedTile_RequestsItsDiscard()
    {
        var (_, controls) = AtDiscardTime();
        var discarded = new List<TilePivot>();
        controls.DiscardRequested += discarded.Add;

        controls.SelectTile(controls.PickTile!);

        Assert.Same(controls.PickTile!.Tile, Assert.Single(discarded));
    }

    [Fact]
    public void SelectTile_OnThePickedTileOutsideThePlayersTurn_DoesNothing()
    {
        var game = FindGame(g => !g.Round.IsHumanPlayer);
        var controls = NewControls(game, game.Round.GetHand(Human).ConcealedTiles[^1]);
        var discarded = new List<TilePivot>();
        controls.DiscardRequested += discarded.Add;

        controls.SelectTile(controls.PickTile!);

        Assert.Empty(discarded);
    }

    [Fact]
    public void FirstDiscardableTile_IsTheFirstHandTileThatCanBeDiscarded()
    {
        var (game, controls) = AtDiscardTime();

        var tile = controls.FirstDiscardableTile();

        var expected = game.Round.GetHand(Human).ConcealedTiles
            .Where(t => !ReferenceEquals(t, controls.PickTile!.Tile))
            .First(t => game.Round.CanDiscard(t));
        Assert.Same(expected, tile.Tile);
    }

    [Fact]
    public void RestrictTo_HighlightsTheChoicesAndDisablesEveryOtherTile()
    {
        var (game, controls) = AtDiscardTime();
        var hand = game.Round.GetHand(Human).ConcealedTiles;
        var choice = hand[1];

        var clickable = controls.RestrictTo(new[] { choice }, _ => { });

        var single = Assert.Single(clickable);
        Assert.Equal(choice, single.Tile);
        Assert.True(single.IsHighlighted);
        Assert.True(single.IsEnabled);
        var others = new[] { controls.PickTile! }.Concat(HandTiles).Where(t => !ReferenceEquals(t, single));
        Assert.All(others, t =>
        {
            Assert.False(t.IsEnabled);
            Assert.False(t.IsHighlighted);
        });
    }


    [Fact]
    public void RestrictTo_MakesClickingAChoiceMakeTheChoiceInsteadOfADiscard()
    {
        var (game, controls) = AtDiscardTime();
        var choice = game.Round.GetHand(Human).ConcealedTiles[1];
        var chosen = new List<TilePivot>();
        var discarded = new List<TilePivot>();
        controls.DiscardRequested += discarded.Add;

        var clickable = controls.RestrictTo(new[] { choice }, chosen.Add);
        controls.SelectTile(clickable[0]);

        Assert.Equal(choice, Assert.Single(chosen));
        Assert.Empty(discarded);
    }

    [Fact]
    public void SuggestDiscard_WithoutTheAdviceEnabled_HighlightsNothing()
    {
        var (_, controls) = AtDiscardTime();
        _settings.DiscardTip = false;

        controls.SuggestDiscard();

        Assert.DoesNotContain(HandTiles.Concat(new[] { controls.PickTile! }), t => t.IsHighlighted);
    }

    [Fact]
    public void SuggestDiscard_WithTheAdviceEnabled_HighlightsTheAdvisorsDiscard()
    {
        var (game, controls) = AtDiscardTime();
        _settings.DiscardTip = true;

        controls.SuggestDiscard();

        var advised = game.Round.Advisor!.DiscardDecision();
        var highlighted = HandTiles.Concat(new[] { controls.PickTile! }).Where(t => t.IsHighlighted).ToList();
        Assert.Single(highlighted);
        Assert.Equal(advised, highlighted[0].Tile);
    }

    [Fact]
    public void SuggestDiscard_WhenItIsNotThePlayersTurn_HighlightsNothing()
    {
        var game = FindGame(g => !g.Round.IsHumanPlayer);
        var controls = NewControls(game);
        _settings.DiscardTip = true;

        controls.SuggestDiscard();

        Assert.DoesNotContain(HandTiles, t => t.IsHighlighted);
    }
}
