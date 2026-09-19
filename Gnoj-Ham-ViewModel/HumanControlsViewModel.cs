using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;
using Gnoj_Ham_ViewModel.Services;

namespace Gnoj_Ham_ViewModel;

/// <summary>
/// What the human player can do: which calls are offered (and which is advised), and which tiles of
/// their hand can be clicked. It decides what is offered, reading the game and its advisor; what
/// happens once the player chooses is up to the <see cref="IHumanActions"/> it is given.
/// </summary>
public sealed partial class HumanControlsViewModel : ObservableObject
{
    private readonly GamePivot _game;
    private readonly PlayerIndices _humanIndex;
    private readonly SeatViewModel _seat;
    private readonly IUserSettings _settings;
    private readonly IHumanActions _actions;

    // What clicking a tile does when it is not a plain discard (e.g. choosing which tiles make a chii).
    private readonly Dictionary<TileViewModel, Func<TilePivot, Task>> _tileChoices = new();
    private bool _isKanAdvised;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="game">The game.</param>
    /// <param name="humanIndex">The human player's seat.</param>
    /// <param name="seat">The human player's seat on the table, which holds their hand.</param>
    /// <param name="settings">The user's settings.</param>
    /// <param name="actions">Carries out what the human player chooses.</param>
    public HumanControlsViewModel(GamePivot game, PlayerIndices humanIndex, SeatViewModel seat, IUserSettings settings, IHumanActions actions)
    {
        _game = game;
        _humanIndex = humanIndex;
        _seat = seat;
        _settings = settings;
        _actions = actions;

        Riichi = NewButton(CallTypes.Riichi);
        KyuushuKyuuhai = NewButton(CallTypes.KyuushuKyuuhai);
        Tsumo = NewButton(CallTypes.Tsumo);
        Ron = NewButton(CallTypes.Ron);
        Pon = NewButton(CallTypes.Pon);
        Chii = NewButton(CallTypes.Chii);
        Kan = NewButton(CallTypes.Kan);
        Skip = new ActionButtonViewModel(actions.SkipCallsAsync);
    }

    /// <summary>
    /// The riichi button.
    /// </summary>
    public ActionButtonViewModel Riichi { get; }

    /// <summary>
    /// The button to abort the round on a hand of nine different terminals and honours.
    /// </summary>
    public ActionButtonViewModel KyuushuKyuuhai { get; }

    /// <summary>
    /// The tsumo button.
    /// </summary>
    public ActionButtonViewModel Tsumo { get; }

    /// <summary>
    /// The ron button.
    /// </summary>
    public ActionButtonViewModel Ron { get; }

    /// <summary>
    /// The pon button.
    /// </summary>
    public ActionButtonViewModel Pon { get; }

    /// <summary>
    /// The chii button.
    /// </summary>
    public ActionButtonViewModel Chii { get; }

    /// <summary>
    /// The kan button.
    /// </summary>
    public ActionButtonViewModel Kan { get; }

    /// <summary>
    /// The button to turn the calls down.
    /// </summary>
    public ActionButtonViewModel Skip { get; }

    /// <summary>
    /// Indicates if the panel holding the buttons is shown.
    /// </summary>
    [ObservableProperty]
    private bool _isPanelVisible;

    /// <summary>
    /// Inferred; indicates if a call on another player's discard (or a kan) is offered.
    /// </summary>
    public bool IsCallOffered => Pon.IsAvailable || Chii.IsAvailable || Kan.IsAvailable || Ron.IsAvailable;

    /// <summary>
    /// Inferred; the tile just picked, if any.
    /// </summary>
    public TileViewModel? PickTile => _seat.PickTiles.FirstOrDefault();

    /// <summary>
    /// Chooses the calls to offer, and marks the one advised (when the advice is enabled).
    /// The panel stays hidden: see <see cref="ShowPanel"/>.
    /// </summary>
    /// <param name="preDiscard">The player holds all their tiles and has to discard: only a concealed kan can be offered.</param>
    /// <param name="cpuPlay">Another player has just discarded (or the player has not picked yet): chii, pon and kan can be offered.</param>
    /// <param name="skippedInnerKan">The player has already turned down a concealed kan this turn.</param>
    /// <returns><c>True</c> if a call is offered, which the player must be given the time to decide.</returns>
    public bool ShowActions(bool preDiscard = false, bool cpuPlay = false, bool skippedInnerKan = false)
    {
        Reset();

        var needAdvice = false;
        var advised = false;

        if (preDiscard)
        {
            // The player has 14 tiles and needs to discard; a kan call might be possible.
            if (!skippedInnerKan)
            {
                var (canCall, decisionTile) = _game.Round.Advisor!.KanDecision(_humanIndex, true);
                if (canCall)
                {
                    Kan.IsAvailable = true;
                    if (_settings.DiscardTip)
                    {
                        needAdvice = true;
                        if (decisionTile != null)
                        {
                            Kan.IsAdvised = true;
                            _isKanAdvised = true;
                        }
                    }
                }
            }
        }
        else if (cpuPlay)
        {
            // Another player is playing, or it is the player's turn but they have not picked yet.
            if (_game.Round.IsHumanPlayer)
            {
                var (canChii, chiiChoice) = _game.Round.Advisor!.ChiiDecision();
                if (canChii)
                {
                    Chii.IsAvailable = true;
                    if (_settings.DiscardTip)
                    {
                        needAdvice = true;
                        if (chiiChoice != null)
                        {
                            Chii.IsAdvised = true;
                            advised = true;
                        }
                    }
                }
            }

            if (_game.Round.CanCallPon(_humanIndex))
            {
                Pon.IsAvailable = true;
                if (_settings.DiscardTip)
                {
                    needAdvice = true;
                    if (_game.Round.Advisor!.PonDecision(_humanIndex))
                    {
                        Pon.IsAdvised = true;
                        advised = true;
                    }
                }
            }

            var (canKan, kanTile) = _game.Round.Advisor!.KanDecision(_humanIndex, false);
            if (canKan)
            {
                Kan.IsAvailable = true;
                if (_settings.DiscardTip)
                {
                    needAdvice = true;
                    if (kanTile != null)
                    {
                        Kan.IsAdvised = true;
                        _isKanAdvised = true;
                    }
                }
            }
        }

        advised |= _isKanAdvised;

        if (needAdvice && !advised)
        {
            Skip.IsAdvised = true;
        }

        return IsCallOffered;
    }

    /// <summary>
    /// Shows the panel of buttons, with the option to turn down what it offers.
    /// </summary>
    public void ShowPanel()
    {
        Skip.IsAvailable = true;
        IsPanelVisible = true;
    }

    /// <summary>
    /// Shows the panel with the single decision the engine is waiting for (riichi, ron, tsumo or
    /// the abortive draw).
    /// </summary>
    /// <param name="call">The call to offer.</param>
    /// <param name="riichiAdvised">For a riichi, indicates if it is the advised choice.</param>
    public void ShowDecision(CallTypes call, bool riichiAdvised)
    {
        ShowPanel();

        switch (call)
        {
            case CallTypes.Riichi:
                Riichi.IsAvailable = true;
                // Without the advice enabled the engine never advises a riichi, which must not read as
                // advice against it.
                if (_settings.DiscardTip)
                {
                    if (riichiAdvised)
                    {
                        Riichi.IsAdvised = true;
                    }
                    else
                    {
                        Skip.IsAdvised = true;
                    }
                }
                break;
            case CallTypes.Ron:
                Ron.IsAvailable = true;
                break;
            case CallTypes.Tsumo:
                Tsumo.IsAvailable = true;
                break;
            case CallTypes.KyuushuKyuuhai:
                KyuushuKyuuhai.IsAvailable = true;
                break;
        }
    }

    /// <summary>
    /// Hides the panel of buttons, the offers themselves left as they are.
    /// </summary>
    public void HidePanel()
    {
        IsPanelVisible = false;
    }

    /// <summary>
    /// Offers nothing: hides the panel and every button.
    /// </summary>
    public void Reset()
    {
        foreach (var button in new[] { Riichi, KyuushuKyuuhai, Tsumo, Ron, Pon, Chii, Kan, Skip })
        {
            button.Reset();
        }

        IsPanelVisible = false;
        _isKanAdvised = false;
    }

    /// <summary>
    /// Clicks a tile of the hand: it is discarded, unless the player is choosing among tiles (see
    /// <see cref="RestrictTo"/>) in which case the choice is made instead.
    /// </summary>
    /// <param name="tile">The tile clicked.</param>
    /// <returns>A task completing once the game needs the player again.</returns>
    [RelayCommand(AllowConcurrentExecutions = true)]
    public async Task SelectTileAsync(TileViewModel tile)
    {
        if (_tileChoices.TryGetValue(tile, out var choose))
        {
            await choose(tile.Tile!);
            return;
        }

        // The tile just picked can always be discarded on the player's turn; the others no longer
        // can once riichi is declared.
        var canDiscard = _seat.PickTiles.Contains(tile)
            ? _game.Round.IsHumanPlayer
            : !_game.Round.IsRiichi(_humanIndex);
        if (canDiscard)
        {
            await _actions.DiscardAsync(tile.Tile!);
        }
    }

    /// <summary>
    /// Restricts what the player can click to some tiles: those stand out, and clicking one makes the
    /// choice instead of discarding it; every other tile is disabled. This lasts until the hand is shown again.
    /// </summary>
    /// <param name="choices">The tiles that can be chosen.</param>
    /// <param name="choose">What choosing a tile does.</param>
    /// <returns>The tiles of the hand that can be clicked.</returns>
    public IReadOnlyList<TileViewModel> RestrictTo(IReadOnlyList<TilePivot> choices, Func<TilePivot, Task> choose)
    {
        _tileChoices.Clear();

        var tiles = _seat.HandTiles.Concat(_seat.PickTiles).ToList();
        var clickable = new List<TileViewModel>(choices.Count);
        foreach (var choice in choices)
        {
            // Of two identical tiles, the red dora is the one to keep: it isn't the one to choose.
            var tile = tiles
                .Where(t => t.Tile! == choice)
                .OrderBy(t => t.Tile!.IsRedDora)
                .First();
            _tileChoices[tile] = choose;
            tile.IsHighlighted = true;
            clickable.Add(tile);
        }

        foreach (var tile in tiles.Except(clickable))
        {
            tile.IsEnabled = false;
        }

        return clickable;
    }

    /// <summary>
    /// Gets the first tile of the hand (not the one just picked) that can be discarded.
    /// </summary>
    /// <returns>The tile.</returns>
    public TileViewModel FirstDiscardableTile()
    {
        return _seat.HandTiles.First(t => _game.Round.CanDiscard(t.Tile!));
    }

    /// <summary>
    /// Makes the advisor's discard stand out, when the advice is enabled and there is a discard to make.
    /// </summary>
    public void SuggestDiscard()
    {
        if (!_settings.DiscardTip)
        {
            return;
        }

        if (_isKanAdvised)
        {
            // A kan call is already the advised action here: suggesting a discard on top of it
            // would contradict that advice (the two are mutually exclusive for this turn).
            return;
        }

        if (_game.Round.IsHumanPlayer && _game.Round.GetHand(_humanIndex).IsFullHand)
        {
            var discardChoice = _game.Round.Advisor!.DiscardDecision();

            var tile = _seat.HandTiles.Concat(_seat.PickTiles).FirstOrDefault(t => t.Tile == discardChoice);
            if (tile != null)
            {
                tile.IsHighlighted = true;
            }
        }
    }

    private ActionButtonViewModel NewButton(CallTypes call)
        => new(() => _actions.CallAsync(call));
}
