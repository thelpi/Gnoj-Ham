using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;
using Gnoj_Ham_Library.Events;
using Gnoj_Ham_ViewModel;
using Gnoj_Ham_ViewModel.Services;

namespace Gnoj_Ham_View;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const string WINDOW_TITLE = "Gnoj-Ham";
    public const string OverlayStoryboardResourceName = "StbHideOverlay";
    public const string CallActionButtonStyleResourceName = "StyleCallActionButton";

    private readonly IDialogService _dialogs;
    private readonly GamePivot _game;
    private readonly TableViewModel _table;
    private readonly System.Media.SoundPlayer _tickSound;
    private System.Timers.Timer? _timer;
    private System.Timers.ElapsedEventHandler? _currentTimerHandler;
    private bool _autoPlayRunning;
    private readonly Storyboard _overlayStoryboard;
    private bool _waitForDecision;
    private IReadOnlyList<TilePivot>? _riichiTiles;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly CancellationToken _cancellationToken;

    private const PlayerIndices _humanPlayerIndex = PlayerIndices.Zero;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="dialogs">Opens the secondary windows (rules, statistics, score...).</param>
    /// <param name="settings">The user's settings.</param>
    /// <param name="playerName">Human player name.</param>
    /// <param name="ruleset">The ruleset.</param>
    /// <param name="stats">Player statistics.</param>
    /// <param name="drivenDraw">Optional; see <see cref="DrivenDrawPivot.Resolve(DrivenDrawScenarios, PlayerIndices)"/>. <c>Null</c> (default) for a normal, fully random draw.</param>
    /// <param name="debugMode">Optional; reveals every hand instead of just the human player's. <c>False</c> (default).</param>
    public MainWindow(IDialogService dialogs, IUserSettings settings, string playerName, RulePivot ruleset, PlayerStatisticsPivot stats, Action<List<TilePivot>>? drivenDraw = null, bool debugMode = false)
    {
        InitializeComponent();

        _dialogs = dialogs;

        _cancellationToken = _cancellationTokenSource.Token;

        _game = new GamePivot(playerName, ruleset, stats, new Random(), drivenDraw);
        _table = new TableViewModel(_game, _humanPlayerIndex, debugMode, settings);
        DataContext = _table;
        Human.CallRequested += OnCallRequested;
        Human.SkipRequested += CancelCallProcess;
        Human.DiscardRequested += Discard;
        _tickSound = new System.Media.SoundPlayer(Properties.Resources.tick);

        _overlayStoryboard = (FindResource(OverlayStoryboardResourceName) as Storyboard)!;
        Storyboard.SetTarget(_overlayStoryboard, GrdOverlayCall);

        ApplyConfigurationToOverlayStoryboard();

        SetChronoTime();

        FixWindowDimensions();

        NewRoundRefresh();

        BindConfiguration();

        ContentRendered += delegate (object? sender, EventArgs evt)
        {
            RunAutoPlay();
        };
    }

    // What the human player can do.
    private HumanControlsViewModel Human => _table.Human;

    #region Window events

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        _cancellationTokenSource.Cancel();
    }

    private void Grid_MouseDoubleClick(object? sender, MouseButtonEventArgs? e)
    {
        CancelCallProcess();
    }

    private void BtnNewGame_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void HlkYakus_Click(object sender, RoutedEventArgs e)
    {
        _dialogs.ShowDialog(new RulesViewModel());
    }

    private void HlkAbout_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Bientôt !", "Gnoj-Ham - Information");
    }

    #region Configuration

    private void CbbCpuSpeed_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded && CbbCpuSpeed.SelectedIndex >= 0)
        {
            Properties.Settings.Default.CpuSpeed = CbbCpuSpeed.SelectedIndex;
            Properties.Settings.Default.Save();
            ApplyConfigurationToOverlayStoryboard();
        }
    }

    private void CbbChrono_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded && CbbChrono.SelectedIndex >= 0)
        {
            Properties.Settings.Default.ChronoSpeed = CbbChrono.SelectedIndex;
            Properties.Settings.Default.Save();
            SetChronoTime();
        }
    }

    private void ChkSounds_Click(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.PlaySounds = ChkSounds.IsChecked == true;
        Properties.Settings.Default.Save();
    }

    private void ChkAutoTsumoRon_Click(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.AutoCallMahjong = ChkAutoTsumoRon.IsChecked == true;
        Properties.Settings.Default.Save();
    }

    private void HlkPlayerStats_Click(object sender, RoutedEventArgs e)
    {
        var (stats, error) = PlayerSaveStorage.Load();

        if (!string.IsNullOrWhiteSpace(error))
        {
            MessageBox.Show($"Une erreur est survenue pendant le chargement du fichier de statistiques du joueur ; les statistiques seront vides.\n\nDétails de l'erreur :\n{error}", "Gnoj-Ham - Avertissement");
        }

        _dialogs.ShowDialog(new PlayerSaveStatsViewModel(stats));
    }

    #endregion Configuration

    #endregion Window events

    #region Human actions

    // The human player asks for a call, by pressing its button or through the game making it on their behalf.
    private void OnCallRequested(CallTypes call)
    {
        switch (call)
        {
            case CallTypes.Chii:
                CallChii();
                break;
            case CallTypes.Pon:
                CallPon();
                break;
            case CallTypes.Kan:
                CallKan();
                break;
            case CallTypes.Ron:
                CallRon();
                break;
            case CallTypes.Tsumo:
                CallTsumo();
                break;
            case CallTypes.Riichi:
                CallRiichi();
                break;
            case CallTypes.KyuushuKyuuhai:
                CallKyuushuKyuuhai();
                break;
        }
    }

    private void Discard(TilePivot tile)
    {
        if (IsCurrentlyClickable())
        {
            if (_game.Round.Discard(tile))
            {
                Dispatcher.Invoke(() =>
                {
                    RefreshHand(_game.Round.PreviousPlayerIndex);
                    Seat(_game.Round.PreviousPlayerIndex).RefreshDiscards();
                    SetActionButtonsVisibility();
                });
                RunAutoPlay();
            }
        }
    }

    private void ChooseChii(TilePivot chiiTilePick)
    {
        if (IsCurrentlyClickable())
        {
            _waitForDecision = false;

            RefreshPlayerTurnStyle();
            if (_game.Round.CallChii(chiiTilePick))
            {
                InvokeOverlay(CallTypes.Chii, _game.Round.CurrentPlayerIndex);

                Dispatcher.Invoke(() =>
                {
                    RefreshHand(_game.Round.CurrentPlayerIndex);
                    Seat(_game.Round.CurrentPlayerIndex).RefreshCombinations();
                    Seat(_game.Round.PreviousPlayerIndex).RefreshDiscards();
                    SetActionButtonsVisibility();
                    ActivateTimer(Human.FirstDiscardableTile());
                });
            }
        }
    }

    private void ChooseKan(TilePivot kanTile)
    {
        if (IsCurrentlyClickable())
        {
            _waitForDecision = false;
            HumanKanCallProcess(kanTile, null);
        }
    }

    private void CallPon()
    {
        if (IsCurrentlyClickable())
        {
            RefreshPlayerTurnStyle();

            // Note : this value is stored here because the call to "CallPon" makes it change.
            var previousPlayerIndex = _game.Round.PreviousPlayerIndex;
            if (_game.Round.CallPon(_humanPlayerIndex))
            {
                InvokeOverlay(CallTypes.Pon, _humanPlayerIndex);

                Dispatcher.Invoke(() =>
                {
                    RefreshHand(_humanPlayerIndex);
                    Seat(_humanPlayerIndex).RefreshCombinations();
                    Seat(previousPlayerIndex).RefreshDiscards();
                    SetActionButtonsVisibility();
                    ActivateTimer(Human.FirstDiscardableTile());
                });
            }

            Human.SuggestDiscard();
        }
    }

    private void CallChii()
    {
        if (IsCurrentlyClickable())
        {
            var tileChoices = _game.Round.CanCallChii();

            if (tileChoices.Count > 0)
            {
                ClickTile(RestrictDiscardWithTilesSelection(tileChoices, ChooseChii));
                Human.SuggestDiscard();
            }
        }
    }

    private void CallKan()
    {
        if (IsCurrentlyClickable())
        {
            var kanTiles = _game.Round.CanCallKan(_humanPlayerIndex);
            if (kanTiles.Count > 0)
            {
                if (_game.Round.IsHumanPlayer)
                {
                    ClickTile(RestrictDiscardWithTilesSelection(kanTiles, ChooseKan));
                }
                else
                {
                    HumanKanCallProcess(null, _game.Round.PreviousPlayerIndex);
                }
            }
        }
    }

    private void ChooseRiichi(TilePivot tile)
    {
        if (IsCurrentlyClickable())
        {
            _waitForDecision = false;

            if (_game.Round.CallRiichi(tile))
            {
                Dispatcher.Invoke(() =>
                {
                    RefreshHand(_game.Round.PreviousPlayerIndex);
                    Seat(_game.Round.PreviousPlayerIndex).RefreshDiscards();
                    SetActionButtonsVisibility(cpuPlay: !_game.Round.PreviousIsHumanPlayer);
                    Seat(_game.Round.PreviousPlayerIndex).ShowRiichiStick();
                });

                if (_game.Round.PreviousIsHumanPlayer)
                {
                    RunAutoPlay();
                }
            }
        }
    }

    private void CallRon()
    {
        if (IsCurrentlyClickable())
        {
            Human.HidePanel();
            _overlayStoryboard.Completed += TriggerHumanRonAfterOverlayStoryboard;
            InvokeOverlay(CallTypes.Ron, _humanPlayerIndex);
        }
    }

    private void CallTsumo()
    {
        if (IsCurrentlyClickable())
        {
            Human.HidePanel();
            _overlayStoryboard.Completed += TriggerNewRoundAfterOverlayStoryboard;
            InvokeOverlay(CallTypes.Tsumo, _humanPlayerIndex);
        }
    }

    private void CallRiichi()
    {
        if (IsCurrentlyClickable())
        {
            Human.HidePanel();
            _overlayStoryboard.Completed += TriggerRiichiChoiceAfterOverlayStoryboard;
            InvokeOverlay(CallTypes.Riichi, _humanPlayerIndex);
        }
    }

    private void CallKyuushuKyuuhai()
    {
        if (IsCurrentlyClickable() && _game.Round.CallKyuushuKyuuhai())
        {
            Human.HidePanel();
            _overlayStoryboard.Completed += TriggerNewRoundAfterOverlayStoryboard;
            InvokeOverlay(CallTypes.KyuushuKyuuhai, _humanPlayerIndex);
        }
    }

    #endregion Human actions

    #region General orchestration

    // call buttons has been proposed and rejected
    private void CancelCallProcess()
    {
        if (_autoPlayRunning || _waitForDecision)
        {
            return;
        }

        _timer?.Stop();

        if (Human.IsCallOffered)
        {
            // Cancels the Highlighting of the previous player discard
            Seat(_game.Round.PreviousPlayerIndex).RefreshDiscards();

            // A kan offered before the discard has its own way out; a kan on another player's discard
            // is turned down like any other call, even when the turn is already the human player's.
            if (Human.Kan.IsAvailable && _game.Round.IsHumanPlayer && _game.Round.GetHand(_humanPlayerIndex).IsFullHand)
            {
                RefreshPlayerTurnStyle();
                SetActionButtonsVisibility(preDiscard: true, skippedInnerKan: true);
                if (_game.Round.HumanCanAutoDiscard())
                {
                    // Not a real CPU sleep: the auto-discard by human player is considered as such.
                    // Runs on a timer instead of Thread.Sleep so the UI thread isn't blocked/frozen for the delay.
                    var autoDiscardDelay = new System.Timers.Timer(((CpuSpeedPivot)Properties.Settings.Default.CpuSpeed).ParseSpeed())
                    {
                        AutoReset = false
                    };
                    autoDiscardDelay.Elapsed += delegate (object? sender, System.Timers.ElapsedEventArgs e)
                    {
                        autoDiscardDelay.Dispose();
                        Dispatcher.Invoke(() => ClickTile(Human.PickTile));
                    };
                    autoDiscardDelay.Start();
                }
                else
                {
                    ActivateTimer(Human.PickTile);
                }
            }
            else
            {
                RunAutoPlay(skipCurrentAction: true);
            }
        }
        else if (Human.PickTile != null)
        {
            if (Human.Riichi.IsAvailable)
            {
                SetActionButtonsVisibility();
                Human.SuggestDiscard();
                ActivateTimer(Human.PickTile);
            }
            else
            {
                ClickTile(Human.PickTile);
            }
        }
    }

    // Proceeds to new round.
    private void NewRound(PlayerIndices? ronPlayerIndex)
    {
        var endOfRoundInfo = _game.NextRound(ronPlayerIndex);

        if (_game.Stats != null)
        {
            var error = PlayerSaveStorage.Save(_game.Stats);
            if (!string.IsNullOrWhiteSpace(error))
            {
                MessageBox.Show($"Une erreur est survenue pendant la sauvegarde du fichier de statistiques du joueur.\n\nDétails de l'erreur :\n{error}", "Gnoj-Ham - Avertissement");
            }
        }

        _dialogs.ShowDialog(new ScoreViewModel(_game.Players, endOfRoundInfo));

        if (endOfRoundInfo.EndOfGame)
        {
            _dialogs.ShowDialog(new EndOfGameViewModel(_game.ComputeCurrentRanking()));
            Close();
        }
        else
        {
            NewRoundRefresh();
            RunAutoPlay();
        }
    }

    // Runs the CPU auto-play off the UI thread, then applies its result back on the UI thread.
    private async void RunAutoPlay(bool skipCurrentAction = false, bool humanRonPending = false, (TilePivot compensationTile, PlayerIndices? previousPlayerIndex)? humanKanCompensation = null)
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
                _cancellationToken,
                skipCurrentAction,
                humanRonPending,
                Properties.Settings.Default.AutoCallMahjong,
                Properties.Settings.Default.DiscardTip,
                humanKanCompensation,
                ((CpuSpeedPivot)Properties.Settings.Default.CpuSpeed).ParseSpeed()));
        }
        finally
        {
            _autoPlayRunning = false;
        }

        if (_cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (result.EndOfRound)
        {
            NewRound(result.RonPlayerId);
        }
        else if (result.HumanCall is { } humanCall)
        {
            // The game decides for the human player: a plain discard, or a call.
            if (humanCall.call == CallTypes.NoCall)
            {
                ClickTile(Human.PickTile);
            }
            else
            {
                Human.RequestCall(humanCall.call);
            }
        }
        else
        {
            Human.SuggestDiscard();
        }
    }

    // Restricts the possible discards to the specified selection of tiles.
    // Returns the tile to click at once when there is no choice to make.
    private TileViewModel? RestrictDiscardWithTilesSelection(
        IReadOnlyList<TilePivot> tileChoices,
        Action<TilePivot> choose)
    {
        SetActionButtonsVisibility();

        var clickableTiles = Human.RestrictTo(tileChoices, choose);

        if (clickableTiles.Count == 1)
        {
            // Only one possibility : initiates the auto-discard.
            return clickableTiles[0];
        }

        _waitForDecision = true;
        ActivateTimer(clickableTiles[0]);
        return null;
    }

    // Inner process kan call.
    private void HumanKanCallProcess(TilePivot? tile, PlayerIndices? previousPlayerIndex)
    {
        RefreshPlayerTurnStyle();

        var compensationTile = _game.Round.CallKan(_humanPlayerIndex, tile);
        if (compensationTile == null)
        {
            throw new InvalidOperationException("CallKan returned null: the kan call was not actually possible.");
        }

        InvokeOverlay(CallTypes.Kan, _humanPlayerIndex);

        RunAutoPlay(humanKanCompensation: (compensationTile, previousPlayerIndex));
    }

    #endregion General orchestration

    #region Graphic tools

    // Triggered when the tiles count in the wall is updated.
    private void OnNotifyWallCount()
    {
        Dispatcher.Invoke(() => _table.RefreshWallTilesLeft());
    }

    // The seat of the table for a player.
    private SeatViewModel Seat(PlayerIndices playerIndex) => _table.Seats[(int)playerIndex];

    // Displays the call overlay.
    private void InvokeOverlay(CallTypes call, PlayerIndices playerIndex)
    {
        Dispatcher.Invoke(() =>
        {
            BtnOpponentCall.Content = $"{call} !";
            BtnOpponentCall.HorizontalAlignment = playerIndex == PlayerIndices.One ? HorizontalAlignment.Right : (playerIndex == PlayerIndices.Three ? HorizontalAlignment.Left : HorizontalAlignment.Center);
            BtnOpponentCall.VerticalAlignment = playerIndex == PlayerIndices.Zero ? VerticalAlignment.Bottom : (playerIndex == PlayerIndices.Two ? VerticalAlignment.Top : VerticalAlignment.Center);
            BtnOpponentCall.Margin = new Thickness(playerIndex == PlayerIndices.Three ? 20 : 0, playerIndex == PlayerIndices.Two ? 20 : 0, playerIndex == PlayerIndices.One ? 20 : 0, playerIndex == PlayerIndices.Zero ? 20 : 0);
            GrdOverlayCall.Visibility = Visibility.Visible;
            _overlayStoryboard.Begin();
        });
    }

    // Runs the given action once any in-progress call announcement overlay has fully finished
    // playing (or immediately if none is playing), so the player decision overlay never appears
    // stacked on top of it - e.g. a kan chain, where the second kan's decision could otherwise pop
    // up before the first kan's announcement has finished disappearing.
    // Uses its own timer (matching the announcement's fixed duration) rather than the storyboard's
    // own Completed event, which is already relied upon elsewhere (Ron/Tsumo/Riichi/Kyuushu Kyuuhai)
    // for a one-shot, self-unsubscribing purpose - piling another consumer onto that same shared
    // event risks subtle ordering/interruption issues between unrelated features.
    private void RunAfterCallAnnouncement(Action action)
    {
        if (GrdOverlayCall.Visibility == Visibility.Visible)
        {
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(CpuSpeedPivot.S500.ParseSpeed())
            };
            timer.Tick += (sender, e) =>
            {
                timer.Stop();
                action();
            };
            timer.Start();
        }
        else
        {
            action();
        }
    }

    // Fix dimensions of the window and every panels (when it's required).
    private void FixWindowDimensions()
    {
        Title = WINDOW_TITLE;

        GrdMain.Width = GraphicTools.EXPECTED_TABLE_SIZE;
        GrdMain.Height = GraphicTools.EXPECTED_TABLE_SIZE;
        Height = GraphicTools.EXPECTED_TABLE_SIZE + 50; // Ugly !

        double dim1 = TileButton.TILE_HEIGHT + TileButton.DEFAULT_TILE_MARGIN;
        double dim2 = (TileButton.TILE_HEIGHT * 3) + (TileButton.DEFAULT_TILE_MARGIN * 2);
        var dim3 = GraphicTools.EXPECTED_TABLE_SIZE - ((dim1 * 4) + (dim2 * 2));

        Cod0.Width = new GridLength(dim1);
        Cod1.Width = new GridLength(dim1);
        Cod2.Width = new GridLength(dim2);
        Cod3.Width = new GridLength(dim3);
        Cod4.Width = new GridLength(dim2);
        Cod5.Width = new GridLength(dim1);
        Cod6.Width = new GridLength(dim1);

        Rod0.Height = new GridLength(dim1);
        Rod1.Height = new GridLength(dim1);
        Rod2.Height = new GridLength(dim2);
        Rod3.Height = new GridLength(dim3);
        Rod4.Height = new GridLength(dim2);
        Rod5.Height = new GridLength(dim1);
        Rod6.Height = new GridLength(dim1);
    }

    // Refills the hand of the specified player.
    private void RefreshHand(PlayerIndices pIndex, TilePivot? pickTile = null)
    {
        Seat(pIndex).RefreshHand(pickTile);
    }

    // Resets and refills the table at a new round.
    private void NewRoundRefresh()
    {
        _game.Round.NotifyWallCount += OnNotifyWallCount;
        _game.Round.NotifyPick += delegate (PickTileEventArgs e)
        {
            if (Properties.Settings.Default.PlaySounds)
            {
                _tickSound.Play();
            }
            if (e != null)
            {
                Dispatcher.Invoke(() =>
                {
                    RefreshHand(e.PlayerIndex, e.Tile);
                });
            }
        };
        _game.Round.ReadyToCallNotifier += e =>
        {
            Dispatcher.Invoke(() =>
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
                            ActivateTimer(Human.FirstDiscardableTile());
                        }
                        break;
                    case CallTypes.Pon:
                        var isCpu = e.PlayerIndex != _humanPlayerIndex;
                        RefreshHand(e.PlayerIndex);
                        Seat(e.PlayerIndex).RefreshCombinations();
                        Seat(e.PreviousPlayerIndex).RefreshDiscards();
                        SetActionButtonsVisibility(cpuPlay: isCpu);
                        if (!isCpu)
                        {
                            ActivateTimer(Human.FirstDiscardableTile());
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
                        _table.RefreshDoras();
                        break;
                }
            });
        };
        _game.Round.PickNotifier += e =>
        {
            Dispatcher.Invoke(() =>
            {
                if (_game.Round.IsHumanPlayer)
                {
                    SetActionButtonsVisibility(preDiscard: true);
                }
                _table.RefreshWalls();
            });
        };
        _game.Round.DiscardTileNotifier += e =>
        {
            Dispatcher.Invoke(() =>
            {
                var seat = Seat(_game.Round.PreviousPlayerIndex);
                seat.RefreshDiscards();
                seat.HighlightLastDiscard();
            });
        };
        _game.Round.HumanCallNotifier += e =>
        {
            Dispatcher.Invoke(() =>
            {
                if (e.Call == CallTypes.NoCall)
                {
                    var pickTile = Human.PickTile;
                    if (pickTile == null)
                    {
                        MessageBox.Show("Le panel de réception de la pioche est vide !", "Gnoj-Ham - Warning", MessageBoxButton.OK);
                    }
                    ActivateTimer(pickTile);
                }
                else
                {
                    RunAfterCallAnnouncement(() =>
                    {
                        Human.ShowDecision(e.Call, e.RiichiAdvised);
                        ActivateTimer(null);
                    });
                }
            });
        };
        _game.Round.CallNotifier += e =>
        {
            InvokeOverlay(e.Action, e.PlayerIndex);
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
        _table.RefreshRound();

        SetActionButtonsVisibility(preDiscard: true);
    }

    // Refresh the style of players when turn changes.
    private void RefreshPlayerTurnStyle()
    {
        Dispatcher.Invoke(() => _table.RefreshTurn());
    }

    // Offers the human player the calls they can make; those on another player's discard (or a kan)
    // are given the time to be decided.
    private void SetActionButtonsVisibility(bool preDiscard = false, bool cpuPlay = false, bool skippedInnerKan = false)
    {
        if (Human.ShowActions(preDiscard, cpuPlay, skippedInnerKan))
        {
            RunAfterCallAnnouncement(() =>
            {
                Human.ShowPanel();
                ActivateTimer(null);
            });
        }
    }

    #endregion Graphic tools

    #region Other methods

    // Clicks a tile of the human player's hand; when there is none, suggests a discard instead.
    private void ClickTile(TileViewModel? tile)
    {
        if (tile != null)
        {
            Human.SelectTile(tile);
        }
        else
        {
            Human.SuggestDiscard();
        }
    }

    // Checks if the button clicked was ready.
    private bool IsCurrentlyClickable()
    {
        var isCurrentlyClickable = !_autoPlayRunning;

        if (isCurrentlyClickable)
        {
            _timer?.Stop();
        }

        return isCurrentlyClickable;
    }

    // Activates the human decision timer and binds its event to a tile click
    // (or, when there is no tile, to giving up on the calls offered).
    private void ActivateTimer(TileViewModel? tileToClick)
    {
        if (_timer != null)
        {
            if (_currentTimerHandler != null)
            {
                _timer.Elapsed -= _currentTimerHandler;
            }
            _currentTimerHandler = delegate (object? sender, System.Timers.ElapsedEventArgs e)
            {
                Dispatcher.Invoke(() =>
                {
                    if (tileToClick == null)
                    {
                        CancelCallProcess();
                    }
                    else
                    {
                        Human.SelectTile(tileToClick);
                    }
                });
            };
            _timer.Elapsed += _currentTimerHandler;
            _timer.Start();
        }
    }

    // Affects a value to the human decision timer.
    private void SetChronoTime()
    {
        var chronoValue = (ChronoPivot)Properties.Settings.Default.ChronoSpeed;
        if (chronoValue == ChronoPivot.None)
        {
            _timer = null;
        }
        else if (_timer != null)
        {
            _timer.Interval = chronoValue.GetDelay() * 1000;
        }
        else
        {
            _timer = new System.Timers.Timer(chronoValue.GetDelay() * 1000);
        }
    }

    // Apply the CPU speed stored in configuration to the storyboard managing the overlay visibility.
    private void ApplyConfigurationToOverlayStoryboard()
    {
        (_overlayStoryboard.Children[^1] as ObjectAnimationUsingKeyFrames)!.KeyFrames[1].KeyTime =
            KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0, 0, CpuSpeedPivot.S500.ParseSpeed()));
    }

    // Handler to trigger a new round at the end of the overlay storyboard animation.
    private void TriggerNewRoundAfterOverlayStoryboard(object? sender, EventArgs e)
    {
        _overlayStoryboard.Completed -= TriggerNewRoundAfterOverlayStoryboard;
        NewRound(null);
    }

    // Handler to trigger a post-riichi "RestrictDiscardWithTilesSelection" at the end of the overlay storyboard animation.
    private void TriggerRiichiChoiceAfterOverlayStoryboard(object? sender, EventArgs e)
    {
        _overlayStoryboard.Completed -= TriggerRiichiChoiceAfterOverlayStoryboard;
        ClickTile(RestrictDiscardWithTilesSelection(_riichiTiles!, ChooseRiichi));
    }

    // Handler to trigger a human ron at the end of the overlay storyboard animation.
    private void TriggerHumanRonAfterOverlayStoryboard(object? sender, EventArgs e)
    {
        _overlayStoryboard.Completed -= TriggerHumanRonAfterOverlayStoryboard;
        RunAutoPlay(humanRonPending: true);
    }

    // Binds graphic elements with current configuration.
    private void BindConfiguration()
    {
        CbbChrono.ItemsSource = DisplayTexts.GetChronoDisplayValues();
        CbbChrono.SelectedIndex = Properties.Settings.Default.ChronoSpeed;

        CbbCpuSpeed.ItemsSource = DisplayTexts.GetCpuSpeedDisplayValues();
        CbbCpuSpeed.SelectedIndex = Properties.Settings.Default.CpuSpeed;

        ChkSounds.IsChecked = Properties.Settings.Default.PlaySounds;
        ChkAutoTsumoRon.IsChecked = Properties.Settings.Default.AutoCallMahjong;
    }

    #endregion Other methods
}
