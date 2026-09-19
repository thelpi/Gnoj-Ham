using System.Runtime.ExceptionServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;
using Gnoj_Ham_Library.Events;
using Gnoj_Ham_ViewModel.Services;

namespace Gnoj_Ham_ViewModel;

/// <summary>
/// A game played by a human player against three CPUs: it owns the <see cref="TableViewModel"/> shown,
/// and runs the game - the CPUs playing off the UI thread, the human decision timer, the calls
/// announcements, the end of each round - reacting to what the human player chooses.
/// </summary>
public sealed partial class GameViewModel : ObservableObject, IHumanActions
{
    private const PlayerIndices HumanPlayerIndex = PlayerIndices.Zero;

    private readonly GamePivot _game;
    private readonly IDialogService _dialogs;
    private readonly IUserSettings _settings;
    private readonly IPlayerStatisticsStorage _storage;
    private readonly IUiDispatcher _dispatcher;
    private readonly IDelay _delay;
    private readonly IAnimationService _animations;
    private readonly ISoundService _sounds;
    private readonly CancellationTokenSource _cancellation = new();

    // Work started without anyone waiting for it: kept so a failure gets noticed, and tests can wait for it.
    private readonly List<Task> _backgroundOperations = new();

    private bool _autoPlayRunning;
    private bool _waitForDecision;
    private IReadOnlyList<TilePivot>? _riichiTiles;
    private CancellationTokenSource? _decisionTimer;
    private volatile Task _announcement = Task.CompletedTask;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="setup">How the game is set up.</param>
    /// <param name="dialogs">Opens the secondary windows (rules, statistics, score...).</param>
    /// <param name="settings">The user's settings.</param>
    /// <param name="storage">Where the player statistics are kept.</param>
    /// <param name="dispatcher">Brings what the engine notifies, off the UI thread, back onto it.</param>
    /// <param name="delay">Waits, for the human decision timer.</param>
    /// <param name="animations">Plays the calls announcements.</param>
    /// <param name="sounds">Plays the sounds.</param>
    public GameViewModel(
        HumanGameSetup setup,
        IDialogService dialogs,
        IUserSettings settings,
        IPlayerStatisticsStorage storage,
        IUiDispatcher dispatcher,
        IDelay delay,
        IAnimationService animations,
        ISoundService sounds)
    {
        _dialogs = dialogs;
        _settings = settings;
        _storage = storage;
        _dispatcher = dispatcher;
        _delay = delay;
        _animations = animations;
        _sounds = sounds;

        _game = new GamePivot(setup.PlayerName, setup.Ruleset, setup.Stats, setup.Random ?? new Random(), setup.DrivenDraw);
        Table = new TableViewModel(_game, HumanPlayerIndex, setup.DebugMode, settings, this);

        NewRoundRefresh();
    }

    /// <summary>
    /// Raised when the game is over, or the player leaves it: the window showing it has nothing left to show.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// The game table.
    /// </summary>
    public TableViewModel Table { get; }

    internal GamePivot Game => _game;

    private HumanControlsViewModel Human => Table.Human;

    /// <summary>
    /// Starts the game: the CPUs play until the human player is needed.
    /// </summary>
    /// <returns>A task completing once the game needs the human player.</returns>
    [RelayCommand]
    public Task StartAsync() => RunAutoPlayAsync();

    /// <summary>
    /// Leaves the game.
    /// </summary>
    [RelayCommand]
    public void NewGame() => CloseRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Shows the rules and the lexicon.
    /// </summary>
    [RelayCommand]
    public void ShowRules() => _dialogs.ShowDialog(new RulesViewModel());

    /// <summary>
    /// Shows the player statistics.
    /// </summary>
    [RelayCommand]
    public void ShowPlayerStats()
    {
        var (stats, error) = _storage.Load();

        if (!string.IsNullOrWhiteSpace(error))
        {
            _dialogs.ShowMessage($"Une erreur est survenue pendant le chargement du fichier de statistiques du joueur ; les statistiques seront vides.\n\nDétails de l'erreur :\n{error}", "Gnoj-Ham - Avertissement");
        }

        _dialogs.ShowDialog(new PlayerSaveStatsViewModel(stats));
    }

    /// <summary>
    /// Shows information about the game.
    /// </summary>
    [RelayCommand]
    public void ShowAbout() => _dialogs.ShowMessage("Bientôt !", "Gnoj-Ham - Information");

    /// <summary>
    /// Stops the game: nothing plays on, and nothing is waited for any more.
    /// </summary>
    public void Cancel()
    {
        _cancellation.Cancel();
        StopDecisionTimer();
    }

    #region Human actions

    /// <inheritdoc />
    public async Task DiscardAsync(TilePivot tile)
    {
        if (IsCurrentlyClickable())
        {
            if (_game.Round.Discard(tile))
            {
                RefreshHand(_game.Round.PreviousPlayerIndex);
                Seat(_game.Round.PreviousPlayerIndex).RefreshDiscards();
                SetActionButtonsVisibility();
                await RunAutoPlayAsync();
            }
        }
    }

    /// <inheritdoc />
    public Task CallAsync(CallTypes call)
    {
        return call switch
        {
            CallTypes.Chii => CallChiiAsync(),
            CallTypes.Pon => CallPonAsync(),
            CallTypes.Kan => CallKanAsync(),
            CallTypes.Ron => CallRonAsync(),
            CallTypes.Tsumo => CallTsumoAsync(),
            CallTypes.Riichi => CallRiichiAsync(),
            CallTypes.KyuushuKyuuhai => CallKyuushuKyuuhaiAsync(),
            _ => Task.CompletedTask,
        };
    }

    /// <inheritdoc />
    [RelayCommand(AllowConcurrentExecutions = true)]
    public async Task SkipCallsAsync()
    {
        if (_autoPlayRunning || _waitForDecision)
        {
            return;
        }

        StopDecisionTimer();

        if (Human.IsCallOffered)
        {
            // Cancels the highlighting of the previous player discard
            Seat(_game.Round.PreviousPlayerIndex).RefreshDiscards();

            // A kan offered before the discard has its own way out; a kan on another player's discard
            // is turned down like any other call, even when the turn is already the human player's.
            if (Human.Kan.IsAvailable && _game.Round.IsHumanPlayer && _game.Round.GetHand(HumanPlayerIndex).IsFullHand)
            {
                RefreshPlayerTurnStyle();
                SetActionButtonsVisibility(preDiscard: true, skippedInnerKan: true);
                if (_game.Round.HumanCanAutoDiscard())
                {
                    RunInBackground(AutoDiscardAfterCpuDelayAsync);
                }
                else
                {
                    StartDecisionTimer(Human.PickTile);
                }
            }
            else
            {
                await RunAutoPlayAsync(skipCurrentAction: true);
            }
        }
        else if (Human.PickTile != null)
        {
            if (Human.Riichi.IsAvailable)
            {
                SetActionButtonsVisibility();
                Human.SuggestDiscard();
                StartDecisionTimer(Human.PickTile);
            }
            else
            {
                await ClickTileAsync(Human.PickTile);
            }
        }
    }

    private Task ChooseChiiAsync(TilePivot chiiTilePick)
    {
        if (IsCurrentlyClickable())
        {
            _waitForDecision = false;

            RefreshPlayerTurnStyle();
            if (_game.Round.CallChii(chiiTilePick))
            {
                _ = Announce(CallTypes.Chii, _game.Round.CurrentPlayerIndex);

                RefreshHand(_game.Round.CurrentPlayerIndex);
                Seat(_game.Round.CurrentPlayerIndex).RefreshCombinations();
                Seat(_game.Round.PreviousPlayerIndex).RefreshDiscards();
                SetActionButtonsVisibility();
                StartDecisionTimer(Human.FirstDiscardableTile());
            }
        }

        return Task.CompletedTask;
    }

    private async Task ChooseKanAsync(TilePivot kanTile)
    {
        if (IsCurrentlyClickable())
        {
            _waitForDecision = false;
            await HumanKanCallProcessAsync(kanTile, null);
        }
    }

    private async Task ChooseRiichiAsync(TilePivot tile)
    {
        if (IsCurrentlyClickable())
        {
            _waitForDecision = false;

            if (_game.Round.CallRiichi(tile))
            {
                RefreshHand(_game.Round.PreviousPlayerIndex);
                Seat(_game.Round.PreviousPlayerIndex).RefreshDiscards();
                SetActionButtonsVisibility(cpuPlay: !_game.Round.PreviousIsHumanPlayer);
                Seat(_game.Round.PreviousPlayerIndex).ShowRiichiStick();

                if (_game.Round.PreviousIsHumanPlayer)
                {
                    await RunAutoPlayAsync();
                }
            }
        }
    }

    private Task CallPonAsync()
    {
        if (IsCurrentlyClickable())
        {
            RefreshPlayerTurnStyle();

            // Note : this value is stored here because the call to "CallPon" makes it change.
            var previousPlayerIndex = _game.Round.PreviousPlayerIndex;
            if (_game.Round.CallPon(HumanPlayerIndex))
            {
                _ = Announce(CallTypes.Pon, HumanPlayerIndex);

                RefreshHand(HumanPlayerIndex);
                Seat(HumanPlayerIndex).RefreshCombinations();
                Seat(previousPlayerIndex).RefreshDiscards();
                SetActionButtonsVisibility();
                StartDecisionTimer(Human.FirstDiscardableTile());
            }

            Human.SuggestDiscard();
        }

        return Task.CompletedTask;
    }

    private async Task CallChiiAsync()
    {
        if (IsCurrentlyClickable())
        {
            var tileChoices = _game.Round.CanCallChii();

            if (tileChoices.Count > 0)
            {
                await ClickTileAsync(RestrictDiscardWithTilesSelection(tileChoices, ChooseChiiAsync));
                Human.SuggestDiscard();
            }
        }
    }

    private async Task CallKanAsync()
    {
        if (IsCurrentlyClickable())
        {
            var kanTiles = _game.Round.CanCallKan(HumanPlayerIndex);
            if (kanTiles.Count > 0)
            {
                if (_game.Round.IsHumanPlayer)
                {
                    await ClickTileAsync(RestrictDiscardWithTilesSelection(kanTiles, ChooseKanAsync));
                }
                else
                {
                    await HumanKanCallProcessAsync(null, _game.Round.PreviousPlayerIndex);
                }
            }
        }
    }

    private async Task CallRonAsync()
    {
        if (IsCurrentlyClickable())
        {
            Human.HidePanel();
            await Announce(CallTypes.Ron, HumanPlayerIndex);
            await RunAutoPlayAsync(humanRonPending: true);
        }
    }

    private async Task CallTsumoAsync()
    {
        if (IsCurrentlyClickable())
        {
            Human.HidePanel();
            await Announce(CallTypes.Tsumo, HumanPlayerIndex);
            await NewRoundAsync(null);
        }
    }

    private async Task CallRiichiAsync()
    {
        if (IsCurrentlyClickable())
        {
            Human.HidePanel();
            await Announce(CallTypes.Riichi, HumanPlayerIndex);
            await ClickTileAsync(RestrictDiscardWithTilesSelection(_riichiTiles!, ChooseRiichiAsync));
        }
    }

    private async Task CallKyuushuKyuuhaiAsync()
    {
        if (IsCurrentlyClickable() && _game.Round.CallKyuushuKyuuhai())
        {
            Human.HidePanel();
            await Announce(CallTypes.KyuushuKyuuhai, HumanPlayerIndex);
            await NewRoundAsync(null);
        }
    }

    // Inner process kan call.
    private async Task HumanKanCallProcessAsync(TilePivot? tile, PlayerIndices? previousPlayerIndex)
    {
        RefreshPlayerTurnStyle();

        var compensationTile = _game.Round.CallKan(HumanPlayerIndex, tile);
        if (compensationTile == null)
        {
            throw new InvalidOperationException("CallKan returned null: the kan call was not actually possible.");
        }

        _ = Announce(CallTypes.Kan, HumanPlayerIndex);

        await RunAutoPlayAsync(humanKanCompensation: (compensationTile, previousPlayerIndex));
    }

    // Restricts the possible discards to the specified selection of tiles.
    // Returns the tile to click at once when there is no choice to make.
    private TileViewModel? RestrictDiscardWithTilesSelection(
        IReadOnlyList<TilePivot> tileChoices,
        Func<TilePivot, Task> choose)
    {
        SetActionButtonsVisibility();

        var clickableTiles = Human.RestrictTo(tileChoices, choose);

        if (clickableTiles.Count == 1)
        {
            // Only one possibility : initiates the auto-discard.
            return clickableTiles[0];
        }

        _waitForDecision = true;
        StartDecisionTimer(clickableTiles[0]);
        return null;
    }

    // Clicks a tile of the human player's hand; when there is none, suggests a discard instead.
    private async Task ClickTileAsync(TileViewModel? tile)
    {
        if (tile != null)
        {
            await Human.SelectTileAsync(tile);
        }
        else
        {
            Human.SuggestDiscard();
        }
    }

    // Checks if the human player can act right now: not while the CPUs are playing.
    private bool IsCurrentlyClickable()
    {
        var isCurrentlyClickable = !_autoPlayRunning;

        if (isCurrentlyClickable)
        {
            StopDecisionTimer();
        }

        return isCurrentlyClickable;
    }

    // The auto-discard by the human player is considered a CPU action, and paced as such.
    private async Task AutoDiscardAfterCpuDelayAsync()
    {
        try
        {
            await _delay.DelayAsync(TimeSpan.FromMilliseconds(CpuSpeed.ParseSpeed()), _cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await ClickTileAsync(Human.PickTile);
    }

    #endregion Human actions

    #region Game flow

    // Runs the CPU auto-play off the UI thread, then applies its result back on the UI thread.
    private async Task RunAutoPlayAsync(bool skipCurrentAction = false, bool humanRonPending = false, (TilePivot compensationTile, PlayerIndices? previousPlayerIndex)? humanKanCompensation = null)
    {
        if (_autoPlayRunning)
        {
            return;
        }

        _autoPlayRunning = true;
        AutoPlayResultPivot result;
        try
        {
            result = await Task.Run(() => _game.Round.RunAutoPlay(
                _cancellation.Token,
                skipCurrentAction,
                humanRonPending,
                _settings.AutoCallMahjong,
                _settings.DiscardTip,
                humanKanCompensation,
                CpuSpeed.ParseSpeed()));
        }
        finally
        {
            _autoPlayRunning = false;
        }

        if (_cancellation.IsCancellationRequested)
        {
            return;
        }

        if (result.EndOfRound)
        {
            await NewRoundAsync(result.RonPlayerId);
        }
        else if (result.HumanCall is { } humanCall)
        {
            // The game decides for the human player: a plain discard, or a call.
            if (humanCall.call == CallTypes.NoCall)
            {
                await ClickTileAsync(Human.PickTile);
            }
            else
            {
                await CallAsync(humanCall.call);
            }
        }
        else
        {
            Human.SuggestDiscard();
        }
    }

    // Proceeds to new round.
    private async Task NewRoundAsync(PlayerIndices? ronPlayerIndex)
    {
        var endOfRoundInfo = _game.NextRound(ronPlayerIndex);

        if (_game.Stats != null)
        {
            var error = _storage.Save(_game.Stats);
            if (!string.IsNullOrWhiteSpace(error))
            {
                _dialogs.ShowMessage($"Une erreur est survenue pendant la sauvegarde du fichier de statistiques du joueur.\n\nDétails de l'erreur :\n{error}", "Gnoj-Ham - Avertissement");
            }
        }

        _dialogs.ShowDialog(new ScoreViewModel(_game.Players, endOfRoundInfo));

        if (endOfRoundInfo.EndOfGame)
        {
            _dialogs.ShowDialog(new EndOfGameViewModel(_game.ComputeCurrentRanking()));
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            NewRoundRefresh();
            await RunAutoPlayAsync();
        }
    }

    // Resets and refills the table at a new round, and listens to what the engine notifies.
    private void NewRoundRefresh()
    {
        _game.Round.NotifyWallCount += OnNotifyWallCount;
        _game.Round.NotifyPick += e =>
        {
            if (_settings.PlaySounds)
            {
                _sounds.PlayTick();
            }
            if (e != null)
            {
                _dispatcher.Invoke(() =>
                {
                    RefreshHand(e.PlayerIndex, e.Tile);
                });
            }
        };
        _game.Round.ReadyToCallNotifier += e =>
        {
            _dispatcher.Invoke(() =>
            {
                switch (e.Call)
                {
                    case CallTypes.Chii:
                        RefreshHand(_game.Round.CurrentPlayerIndex);
                        Seat(_game.Round.CurrentPlayerIndex).RefreshCombinations();
                        Seat(_game.Round.PreviousPlayerIndex).RefreshDiscards();
                        SetActionButtonsVisibility(cpuPlay: !_game.Round.IsHumanPlayer);
                        if (_game.Round.IsHumanPlayer)
                        {
                            StartDecisionTimer(Human.FirstDiscardableTile());
                        }
                        break;
                    case CallTypes.Pon:
                        var isCpu = e.PlayerIndex != HumanPlayerIndex;
                        RefreshHand(e.PlayerIndex);
                        Seat(e.PlayerIndex).RefreshCombinations();
                        Seat(e.PreviousPlayerIndex).RefreshDiscards();
                        SetActionButtonsVisibility(cpuPlay: isCpu);
                        if (!isCpu)
                        {
                            StartDecisionTimer(Human.FirstDiscardableTile());
                        }
                        break;
                    case CallTypes.Riichi:
                        RefreshHand(_game.Round.PreviousPlayerIndex);
                        Seat(_game.Round.PreviousPlayerIndex).RefreshDiscards();
                        SetActionButtonsVisibility(cpuPlay: !_game.Round.PreviousIsHumanPlayer);
                        Seat(_game.Round.PreviousPlayerIndex).ShowRiichiStick();
                        break;
                    case CallTypes.NoCall:
                        RefreshHand(_game.Round.PreviousPlayerIndex);
                        Seat(_game.Round.PreviousPlayerIndex).RefreshDiscards();
                        SetActionButtonsVisibility(cpuPlay: !_game.Round.PreviousIsHumanPlayer);
                        break;
                    case CallTypes.Kan:
                        if (e.PotentialPreviousPlayerIndex.HasValue)
                        {
                            Seat(e.PotentialPreviousPlayerIndex.Value).RefreshDiscards();
                        }
                        Seat(_game.Round.CurrentPlayerIndex).RefreshCombinations();
                        SetActionButtonsVisibility(cpuPlay: !_game.Round.IsHumanPlayer, preDiscard: _game.Round.IsHumanPlayer);
                        Table.RefreshDoras();
                        break;
                }
            });
        };
        _game.Round.PickNotifier += e =>
        {
            _dispatcher.Invoke(() =>
            {
                if (_game.Round.IsHumanPlayer)
                {
                    SetActionButtonsVisibility(preDiscard: true);
                }
                Table.RefreshWalls();
            });
        };
        _game.Round.DiscardTileNotifier += e =>
        {
            _dispatcher.Invoke(() =>
            {
                var seat = Seat(_game.Round.PreviousPlayerIndex);
                seat.RefreshDiscards();
                seat.HighlightLastDiscard();
            });
        };
        _game.Round.HumanCallNotifier += e =>
        {
            _dispatcher.Invoke(() =>
            {
                if (e.Call == CallTypes.NoCall)
                {
                    var pickTile = Human.PickTile;
                    if (pickTile == null)
                    {
                        _dialogs.ShowMessage("Le panel de réception de la pioche est vide !", "Gnoj-Ham - Warning");
                    }
                    StartDecisionTimer(pickTile);
                }
                else
                {
                    AfterAnnouncement(() =>
                    {
                        Human.ShowDecision(e.Call, e.RiichiAdvised);
                        StartDecisionTimer(null);
                    });
                }
            });
        };
        _game.Round.CallNotifier += e =>
        {
            _ = Announce(e.Action, e.PlayerIndex);
        };
        _game.Round.RiichiChoicesNotifier += e =>
        {
            _riichiTiles = e.Tiles;
        };
        _game.Round.TurnChangeNotifier += e =>
        {
            RefreshPlayerTurnStyle();
        };

        // The table reads the wall count itself: the notification above is subscribed too late to
        // have been triggered for the first tiles.
        Table.RefreshRound();

        SetActionButtonsVisibility(preDiscard: true);
    }

    // Triggered when the tiles count in the wall is updated.
    private void OnNotifyWallCount()
    {
        _dispatcher.Invoke(() => Table.RefreshWallTilesLeft());
    }

    // Refresh the style of players when turn changes.
    private void RefreshPlayerTurnStyle()
    {
        _dispatcher.Invoke(() => Table.RefreshTurn());
    }

    // The seat of the table for a player.
    private SeatViewModel Seat(PlayerIndices playerIndex) => Table.Seats[(int)playerIndex];

    // Refills the hand of the specified player.
    private void RefreshHand(PlayerIndices playerIndex, TilePivot? pickTile = null)
    {
        Seat(playerIndex).RefreshHand(pickTile);
    }

    // Offers the human player the calls they can make; those on another player's discard (or a kan)
    // are given the time to be decided.
    private void SetActionButtonsVisibility(bool preDiscard = false, bool cpuPlay = false, bool skippedInnerKan = false)
    {
        if (Human.ShowActions(preDiscard, cpuPlay, skippedInnerKan))
        {
            AfterAnnouncement(() =>
            {
                Human.ShowPanel();
                StartDecisionTimer(null);
            });
        }
    }

    #endregion Game flow

    #region Announcements and timer

    // Announces a call. The game plays on, unless the task returned is waited for.
    private Task Announce(CallTypes call, PlayerIndices playerIndex)
    {
        var announcement = Task.CompletedTask;
        _dispatcher.Invoke(() => announcement = _animations.PlayCallAnnouncementAsync(call, playerIndex));
        _announcement = announcement;
        return announcement;
    }

    // Runs the given action once any in-progress call announcement has fully finished playing (or
    // immediately if none is), so the player decision panel never appears stacked on top of it - e.g. a
    // kan chain, where the second kan's decision could otherwise pop up before the first kan's
    // announcement has finished disappearing.
    private void AfterAnnouncement(Action action)
    {
        var announcement = _announcement;
        RunInBackground(async () =>
        {
            await announcement;
            action();
        });
    }

    // Starts the human decision timer: once it is over the tile is clicked or, when there is none, the
    // calls offered are turned down. Nothing happens when the timer is disabled.
    private void StartDecisionTimer(TileViewModel? tileToClick)
    {
        // The wait itself is not work: it goes on for as long as the human player takes.
        RunInBackground(() => WaitForDecisionAsync(tileToClick), isWork: false);
    }

    private async Task WaitForDecisionAsync(TileViewModel? tileToClick)
    {
        StopDecisionTimer();

        var chrono = (ChronoPivot)_settings.ChronoSpeed;
        if (chrono == ChronoPivot.None)
        {
            return;
        }

        using var timer = CancellationTokenSource.CreateLinkedTokenSource(_cancellation.Token);
        _decisionTimer = timer;
        try
        {
            await _delay.DelayAsync(TimeSpan.FromSeconds(chrono.GetDelay()), timer.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (ReferenceEquals(_decisionTimer, timer))
        {
            _decisionTimer = null;
        }

        RunInBackground(() => tileToClick == null ? SkipCallsAsync() : Human.SelectTileAsync(tileToClick));
    }

    private void StopDecisionTimer()
    {
        var timer = _decisionTimer;
        _decisionTimer = null;
        try
        {
            timer?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The timer had already gone off.
        }
    }

    // Runs work nobody waits for. A failure is not lost: it is raised on the UI thread, like any
    // unhandled exception there.
    private void RunInBackground(Func<Task> operation, bool isWork = true)
    {
        var task = operation();
        if (task.IsCompletedSuccessfully)
        {
            return;
        }

        if (isWork)
        {
            lock (_backgroundOperations)
            {
                _backgroundOperations.Add(task);
            }
        }

        _ = task.ContinueWith(
            finished =>
            {
                // A failed operation stays in the list, for whoever waits for the work to be over to notice.
                if (isWork && !finished.IsFaulted)
                {
                    lock (_backgroundOperations)
                    {
                        _backgroundOperations.Remove(finished);
                    }
                }

                if (finished.IsFaulted)
                {
                    var error = finished.Exception!.GetBaseException();
                    _ = _dispatcher.InvokeAsync(() => ExceptionDispatchInfo.Throw(error));
                }
            },
            TaskScheduler.Default);
    }

    // Waits for everything started in the background to be over (and fails if any of it failed).
    internal async Task WhenIdleAsync()
    {
        while (true)
        {
            Task[] pending;
            lock (_backgroundOperations)
            {
                pending = _backgroundOperations.ToArray();
                _backgroundOperations.RemoveAll(t => t.IsCompleted);
            }

            if (pending.Length == 0)
            {
                return;
            }

            await Task.WhenAll(pending);
        }
    }

    // The pause between CPU actions.
    private CpuSpeedPivot CpuSpeed => (CpuSpeedPivot)_settings.CpuSpeed;

    #endregion Announcements and timer
}
