using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;
using Gnoj_Ham_Library.Events;

namespace Gnoj_Ham_View;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const string WINDOW_TITLE = "Gnoj-Ham";
    public const string StyleHighlightTileResourceName = "StyleHighlightTile";
    public const string OverlayStoryboardResourceName = "StbHideOverlay";

    private string PickPanel => StpPickP0.Name[..^1];
    private string HandPanel => StpHandP0.Name[..^1];
    private string WallPanel => PnlWall0.Name[..^1];
    private string CombosPanel => StpCombosP0.Name[..^1];
    private string PlayerLabel => LblPlayerP0.Name[..^1];
    private string WindLabel => LblWindP0.Name[..^1];
    private string[] DiscardPanels => ["", StpDiscard1P0.Name[..^1], StpDiscard2P0.Name[..^1], StpDiscard3P0.Name[..^1]];
    private string PlayerPanel => StpPlayerP0.Name[..^1];
    private string NameLabel => LblNameP0.Name[..^1];
    private string PointsLabel => LblPointsP0.Name[..^1];
    private string RiichiStickImage => RiichiStickP0.Name[..^1];
    private string ActionButton => BtnChii.Name[..BtnChii.Name.IndexOf(CallTypes.Chii.ToString())];

    private readonly GamePivot _game;
    private readonly System.Media.SoundPlayer _tickSound;
    private System.Timers.Timer? _timer;
    private System.Timers.ElapsedEventHandler? _currentTimerHandler;
    private readonly BackgroundWorker _autoPlay;
    private readonly Storyboard _overlayStoryboard;
    private bool _waitForDecision;
    private IReadOnlyList<TilePivot>? _riichiTiles;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly CancellationToken _cancellationToken;

    private const PlayerIndices _humanPlayerIndex = PlayerIndices.Zero;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="playerName">Human player name.</param>
    /// <param name="ruleset">The ruleset.</param>
    /// <param name="save">Player save file.</param>
    public MainWindow(string playerName, RulePivot ruleset, PlayerSavePivot save)
    {
        InitializeComponent();

        _cancellationToken = _cancellationTokenSource.Token;
        this.FindControl(PlayerLabel, _humanPlayerIndex).Content = playerName;

        _game = new GamePivot(playerName, ruleset, save, new Random());
        _tickSound = new System.Media.SoundPlayer(Properties.Resources.tick);

        _overlayStoryboard = (FindResource(OverlayStoryboardResourceName) as Storyboard)!;
        Storyboard.SetTarget(_overlayStoryboard, GrdOverlayCall);

        ApplyConfigurationToOverlayStoryboard();

        SetChronoTime();

        FixWindowDimensions();

        NewRoundRefresh();

        _autoPlay = new BackgroundWorker
        {
            WorkerReportsProgress = false,
            WorkerSupportsCancellation = false
        };
        InitializeAutoPlayWorker();

        BindConfiguration();

        ContentRendered += delegate (object? sender, EventArgs evt)
        {
            RunAutoPlay();
        };
    }

    #region Window events

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        _cancellationTokenSource.Cancel();
    }

    private void BtnDiscard_Click(object sender, RoutedEventArgs e)
    {
        if (IsCurrentlyClickable())
        {
            var tile = (sender as TileButton)!.Tile!;

            if (_game.Round.Discard(tile))
            {
                Dispatcher.Invoke(() =>
                {
                    FillHandPanel(_game.Round.PreviousPlayerIndex);
                    FillDiscardPanel(_game.Round.PreviousPlayerIndex);
                    SetActionButtonsVisibility();
                });
                RunAutoPlay();
            }
        }
    }

    private void BtnChiiChoice_Click(object sender, RoutedEventArgs e)
    {
        if (IsCurrentlyClickable())
        {
            _waitForDecision = false;
            var chiiTilePick = (sender as TileButton)!.Tile!;

            RefreshPlayerTurnStyle();
            if (_game.Round.CallChii(chiiTilePick))
            {
                InvokeOverlay(CallTypes.Chii, _game.Round.CurrentPlayerIndex);

                Dispatcher.Invoke(() =>
                {
                    FillHandPanel(_game.Round.CurrentPlayerIndex);
                    FillCombinationStack(_game.Round.CurrentPlayerIndex);
                    FillDiscardPanel(_game.Round.PreviousPlayerIndex);
                    SetActionButtonsVisibility();
                    ActivateTimer(GetFirstAvailableDiscardButton());
                });
            }
        }
    }

    private void BtnKanChoice_Click(object sender, RoutedEventArgs e)
    {
        if (IsCurrentlyClickable())
        {
            _waitForDecision = false;
            HumanKanCallProcess((sender as TileButton)!.Tile!, null);
        }
    }

    private void BtnPon_Click(object sender, RoutedEventArgs e)
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
                    FillHandPanel(_humanPlayerIndex);
                    FillCombinationStack(_humanPlayerIndex);
                    FillDiscardPanel(previousPlayerIndex);
                    SetActionButtonsVisibility();
                    ActivateTimer(GetFirstAvailableDiscardButton());
                });
            }

            SuggestDiscard();
        }
    }

    private void BtnChii_Click(object sender, RoutedEventArgs e)
    {
        if (IsCurrentlyClickable())
        {
            var tileChoices = _game.Round.CanCallChii();

            if (tileChoices.Count > 0)
            {
                RaiseButtonClickEvent(RestrictDiscardWithTilesSelection(tileChoices, BtnChiiChoice_Click));
                SuggestDiscard();
            }
        }
    }

    private void BtnKan_Click(object sender, RoutedEventArgs e)
    {
        if (IsCurrentlyClickable())
        {
            var kanTiles = _game.Round.CanCallKan(_humanPlayerIndex);
            if (kanTiles.Count > 0)
            {
                if (_game.Round.IsHumanPlayer)
                {
                    RaiseButtonClickEvent(RestrictDiscardWithTilesSelection(kanTiles, BtnKanChoice_Click));
                }
                else
                {
                    HumanKanCallProcess(null, _game.Round.PreviousPlayerIndex);
                }
            }
        }
    }

    private void BtnRiichiChoice_Click(object sender, RoutedEventArgs e)
    {
        if (IsCurrentlyClickable())
        {
            _waitForDecision = false;
            var tile = (sender as TileButton)!.Tile!;

            if (_game.Round.CallRiichi(tile))
            {
                Dispatcher.Invoke(() =>
                {
                    FillHandPanel(_game.Round.PreviousPlayerIndex);
                    FillDiscardPanel(_game.Round.PreviousPlayerIndex);
                    SetActionButtonsVisibility(cpuPlay: !_game.Round.PreviousIsHumanPlayer);
                    this.FindName<Image>(RiichiStickImage, _game.Round.PreviousPlayerIndex).Visibility = Visibility.Visible;
                });

                if (_game.Round.PreviousIsHumanPlayer)
                {
                    RunAutoPlay();
                }
            }
        }
    }

    private void Grid_MouseDoubleClick(object? sender, MouseButtonEventArgs? e)
    {
        CancelCallProcess();
    }

    private void BtnRon_Click(object sender, RoutedEventArgs e)
    {
        if (IsCurrentlyClickable())
        {
            _overlayStoryboard.Completed += TriggerHumanRonAfterOverlayStoryboard;
            InvokeOverlay(CallTypes.Ron, _humanPlayerIndex);
        }
    }

    private void BtnTsumo_Click(object sender, RoutedEventArgs e)
    {
        if (IsCurrentlyClickable())
        {
            _overlayStoryboard.Completed += TriggerNewRoundAfterOverlayStoryboard;
            InvokeOverlay(CallTypes.Tsumo, _humanPlayerIndex);
        }
    }

    private void BtnRiichi_Click(object sender, RoutedEventArgs e)
    {
        if (IsCurrentlyClickable())
        {
            _overlayStoryboard.Completed += TriggerRiichiChoiceAfterOverlayStoryboard;
            InvokeOverlay(CallTypes.Riichi, _humanPlayerIndex);
        }
    }

    private void BtnSkipCall_Click(object sender, RoutedEventArgs e)
    {
        CancelCallProcess();
    }

    private void BtnNewGame_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void HlkYakus_Click(object sender, RoutedEventArgs e)
    {
        new RulesWindow().ShowDialog();
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
        var (save, error) = PlayerSavePivot.GetOrCreateSave();

        if (!string.IsNullOrWhiteSpace(error))
        {
            MessageBox.Show($"Une erreur est survenue pendant le chargement du fichier de statistiques du joueur ; les statistiques seront vides.\n\nDétails de l'erreur :\n{error}", "Gnoj-Ham - Avertissement");
        }

        new PlayerSaveStatsWindow(save).ShowDialog();
    }

    #endregion Configuration

    #endregion Window events

    #region General orchestration

    // call buttons has been proposed and rejected
    private void CancelCallProcess()
    {
        if (_autoPlay.IsBusy || _waitForDecision)
        {
            return;
        }

        _timer?.Stop();

        if (BtnPon.Visibility == Visibility.Visible
            || BtnChii.Visibility == Visibility.Visible
            || BtnKan.Visibility == Visibility.Visible
            || BtnRon.Visibility == Visibility.Visible)
        {
            // TODO: lazy
            // Cancels the Highlighting of the previous player discard
            FillDiscardPanel(_game.Round.PreviousPlayerIndex);

            if (BtnKan.Visibility == Visibility.Visible && _game.Round.IsHumanPlayer)
            {
                RefreshPlayerTurnStyle();
                SetActionButtonsVisibility(preDiscard: true, skippedInnerKan: true);
                if (_game.Round.HumanCanAutoDiscard())
                {
                    // Not a real CPU sleep: the auto-discard by human player is considered as such
                    Thread.Sleep(((CpuSpeedPivot)Properties.Settings.Default.CpuSpeed).ParseSpeed());
                    RaiseButtonClickEvent(new PanelButton(PickPanel, 0));
                }
                else
                {
                    ActivateTimer(this.FindPanel(PickPanel, _humanPlayerIndex).Children[0] as Button);
                }
            }
            else
            {
                RunAutoPlay(skipCurrentAction: true);
            }
        }
        else if (this.FindPanel(PickPanel, _humanPlayerIndex).Children.Count > 0)
        {
            if (BtnRiichi.Visibility == Visibility.Visible)
            {
                SetActionButtonsVisibility();
                SuggestDiscard();
                ActivateTimer(this.FindPanel(PickPanel, _humanPlayerIndex).Children[0] as Button);
            }
            else
            {
                RaiseButtonClickEvent(new PanelButton(PickPanel, 0));
            }
        }
    }

    // Initializes a background worker which orchestrates the CPU actions.
    private void InitializeAutoPlayWorker()
    {
        _autoPlay.DoWork += delegate (object? sender, DoWorkEventArgs evt)
        {
            var argumentsList = (evt.Argument as object?[])!;

            _game.Round.ReadyToCallNotifier += e =>
            {
                Dispatcher.Invoke(() =>
                {
                    switch (e.Call)
                    {
                        case CallTypes.Chii:
                            FillHandPanel(_game.Round.CurrentPlayerIndex);
                            FillCombinationStack(_game.Round.CurrentPlayerIndex);
                            FillDiscardPanel(_game.Round.PreviousPlayerIndex);
                            SetActionButtonsVisibility(cpuPlay: !_game.Round.IsHumanPlayer);
                            if (_game.Round.IsHumanPlayer)
                            {
                                ActivateTimer(GetFirstAvailableDiscardButton());
                            }
                            break;
                        case CallTypes.Pon:
                            var isCpu = e.PlayerIndex != _humanPlayerIndex;
                            FillHandPanel(e.PlayerIndex);
                            FillCombinationStack(e.PlayerIndex);
                            FillDiscardPanel(e.PreviousPlayerIndex);
                            SetActionButtonsVisibility(cpuPlay: isCpu);
                            if (!isCpu)
                            {
                                ActivateTimer(GetFirstAvailableDiscardButton());
                            }
                            break;
                        case CallTypes.Riichi:
                            FillHandPanel(_game.Round.PreviousPlayerIndex);
                            FillDiscardPanel(_game.Round.PreviousPlayerIndex);
                            SetActionButtonsVisibility(cpuPlay: !_game.Round.PreviousIsHumanPlayer);
                            this.FindName<Image>(RiichiStickImage, _game.Round.PreviousPlayerIndex).Visibility = Visibility.Visible;
                            break;
                        case CallTypes.NoCall:
                            FillHandPanel(_game.Round.PreviousPlayerIndex);
                            FillDiscardPanel(_game.Round.PreviousPlayerIndex);
                            SetActionButtonsVisibility(cpuPlay: !_game.Round.PreviousIsHumanPlayer);
                            break;
                        case CallTypes.Kan:
                            if (e.PotentialPreviousPlayerIndex.HasValue)
                            {
                                FillDiscardPanel(e.PotentialPreviousPlayerIndex.Value);
                            }
                            FillCombinationStack(_game.Round.CurrentPlayerIndex);
                            SetActionButtonsVisibility(cpuPlay: !_game.Round.IsHumanPlayer, preDiscard: _game.Round.IsHumanPlayer);
                            StpDoras.SetDorasPanel(_game.Round.DoraIndicatorTiles, _game.Round.VisibleDorasCount);
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
                    SetWallsLength();
                });
            };
            _game.Round.DiscardTileNotifier += e =>
            {
                Dispatcher.Invoke(() =>
                {
                    var highlightButton = FillDiscardPanel(_game.Round.PreviousPlayerIndex);
                    if (highlightButton != null)
                    {
                        SetHighlight(highlightButton);
                    }
                });
            };
            _game.Round.HumanCallNotifier += e =>
            {
                Button? autoButtonOnTimer = null;
                Dispatcher.Invoke(() =>
                {
                    if (e.Call == CallTypes.NoCall)
                    {
                        autoButtonOnTimer = this.FindPanel(PickPanel, _humanPlayerIndex).Children[0] as Button;
                    }
                    else
                    {
                        GrdOverlayCanCall.Visibility = Visibility.Visible;
                        BtnSkipCall.Visibility = Visibility.Visible;
                        switch (e.Call)
                        {
                            case CallTypes.Riichi:
                                BtnRiichi.Visibility = Visibility.Visible;
                                if (e.RiichiAdvised)
                                    BtnRiichi.Foreground = Brushes.DarkMagenta;
                                else
                                    BtnSkipCall.Foreground = Brushes.DarkMagenta;
                                break;
                            case CallTypes.Ron:
                                BtnRon.Visibility = Visibility.Visible;
                                break;
                            case CallTypes.Tsumo:
                                BtnTsumo.Visibility = Visibility.Visible;
                                break;
                        }
                    }
                });
                ActivateTimer(autoButtonOnTimer);
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

            evt.Result = _game.Round.RunAutoPlay(
                _cancellationToken,
                Convert.ToBoolean(argumentsList[0]),
                Convert.ToBoolean(argumentsList[1]),
                Properties.Settings.Default.AutoCallMahjong,
                ((TilePivot, PlayerIndices?)?)argumentsList[2],
                ((CpuSpeedPivot)Properties.Settings.Default.CpuSpeed).ParseSpeed());
        };
        _autoPlay.RunWorkerCompleted += delegate (object? sender, RunWorkerCompletedEventArgs evt)
        {
            if (!_cancellationToken.IsCancellationRequested)
            {
                var result = (AutoPlayResultPivot)evt.Result!;
                if (result.EndOfRound)
                {
                    NewRound(result.RonPlayerId);
                }
                else
                {
                    // TODO
                    var button = result.HumanCall.HasValue
                        ? (result.HumanCall.Value.call == CallTypes.NoCall
                            ? new PanelButton(PickPanel, 0)
                            : new PanelButton($"{ActionButton}{result.HumanCall.Value.call}", -1))
                        : null;
                    RaiseButtonClickEvent(button);
                }
            }
        };
    }

    // Proceeds to new round.
    private void NewRound(PlayerIndices? ronPlayerIndex)
    {
        var (endOfRoundInfo, error) = _game.NextRound(ronPlayerIndex);

        if (!string.IsNullOrWhiteSpace(error))
        {
            MessageBox.Show($"Une erreur est survenue pendant la sauvegarde du fichier de statistiques du joueur.\n\nDétails de l'erreur :\n{error}", "Gnoj-Ham - Avertissement");
        }

        new ScoreWindow(_game.Players.ToList(), endOfRoundInfo).ShowDialog();

        if (endOfRoundInfo.EndOfGame)
        {
            new EndOfGameWindow(_game).ShowDialog();
            Close();
        }
        else
        {
            NewRoundRefresh();
            RunAutoPlay();
        }
    }

    // Starts the background worker.
    private void RunAutoPlay(bool skipCurrentAction = false, bool humanRonPending = false, (TilePivot compensationTile, PlayerIndices? previousPlayerIndex)? humanKanCompensation = null)
    {
        if (!_autoPlay.IsBusy)
        {
            _autoPlay.RunWorkerAsync(new object?[] { skipCurrentAction, humanRonPending, humanKanCompensation });
        }
    }

    // Restrict possible discards on the specified selection of tiles.
    private PanelButton? RestrictDiscardWithTilesSelection(
        IReadOnlyList<TilePivot> tileChoices,
        RoutedEventHandler handler)
    {
        PanelButton? result = null;

        SetActionButtonsVisibility();

        var buttons = this.FindPanel(HandPanel, _humanPlayerIndex).Children.OfType<TileButton>().ToList();
        if (this.FindPanel(PickPanel, _humanPlayerIndex).Children.Count > 0)
        {
            buttons.Add((this.FindPanel(PickPanel, _humanPlayerIndex).Children[0] as TileButton)!);
        }

        var clickableButtons = new List<TileButton>(tileChoices.Count);
        foreach (var tileKey in tileChoices)
        {
            // Changes the event of every buttons concerned by the call...
            var buttonClickable = buttons
                .Where(b => b.Tile! == tileKey)
                .OrderBy(b => b.Tile!.IsRedDora) // in case of autoplay, we don't want the red dora discarded where there's a not-red tile
                .First();
            buttonClickable.Click += handler;
            buttonClickable.Click -= BtnDiscard_Click;
            SetHighlight(buttonClickable);
            clickableButtons.Add(buttonClickable);
        }

        // ...and disables every buttons not concerned.
        foreach (var b in buttons.Where(b => !clickableButtons.Contains(b)))
        {
            b.IsEnabled = false;
        }

        if (clickableButtons.Count == 1)
        {
            // Only one possibility : initiates the auto-discard.
            var buttonIndexInHandPanel = this.FindPanel(HandPanel, _humanPlayerIndex).Children.IndexOf(clickableButtons[0]);
            result = buttonIndexInHandPanel >= 0
                ? new PanelButton(HandPanel, buttonIndexInHandPanel)
                : new PanelButton(PickPanel, 0);
        }
        else
        {
            _waitForDecision = true;
            ActivateTimer(clickableButtons[0]);
        }

        return result;
    }

    // Inner process kan call.
    private void HumanKanCallProcess(TilePivot? tile, PlayerIndices? previousPlayerIndex)
    {
        RefreshPlayerTurnStyle();

        var compensationTile = _game.Round.CallKan(_humanPlayerIndex, tile);
        InvokeOverlay(CallTypes.Kan, _humanPlayerIndex);

        RunAutoPlay(humanKanCompensation: (compensationTile!, previousPlayerIndex));
    }

    #endregion General orchestration

    #region Graphic tools

    // Triggered when the tiles count in the wall is updated.
    private void OnNotifyWallCount()
    {
        Dispatcher.Invoke(() =>
        {
            LblWallTilesLeft.Content = _game.Round.WallTiles.Count;
            if (_game.Round.WallTiles.Count <= 4)
            {
                LblWallTilesLeft.Foreground = Brushes.Red;
            }
        });
    }

    // Gets the first button for a discardable tile.
    private TileButton GetFirstAvailableDiscardButton()
    {
        return this.FindPanel(HandPanel, _humanPlayerIndex)
            .Children
            .OfType<TileButton>()
            .First(b => _game.Round.CanDiscard(b.Tile!));
    }

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

        foreach (var i in Enum.GetValues<PlayerIndices>())
        {
            for (var j = 1; j <= 3; j++)
            {
                var panel = this.FindPanel(DiscardPanels[j], i);
                if (i == PlayerIndices.Zero || i == PlayerIndices.Two)
                {
                    panel.Height = TileButton.TILE_HEIGHT;
                }
                else
                {
                    panel.Width = TileButton.TILE_HEIGHT;
                }
            }
        }
    }

    // Clears and refills the hand panel of the specified player index.
    private void FillHandPanel(PlayerIndices pIndex, TilePivot? pickTile = null)
    {
        var isHuman = pIndex == _humanPlayerIndex;

        var panel = this.FindPanel(HandPanel, pIndex);

        this.FindPanel(PickPanel, pIndex).Children.Clear();

        panel.Children.Clear();
        foreach (var tile in _game.Round.GetHand(pIndex).ConcealedTiles)
        {
            if (pickTile == null || !ReferenceEquals(pickTile, tile))
            {
                RoutedEventHandler? handler = isHuman && !_game.Round.IsRiichi(pIndex)
                    ? BtnDiscard_Click
                    : null;
                panel.Children.Add(new TileButton(tile, handler, (AnglePivot)pIndex, !isHuman && !_game.Ruleset.DebugMode));
            }
        }

        if (pickTile != null)
        {
            this.FindPanel(PickPanel, pIndex).Children.Add(
                new TileButton(
                    pickTile,
                    _game.Round.IsHumanPlayer ? BtnDiscard_Click : null,
                    (AnglePivot)pIndex,
                    !_game.Round.IsHumanPlayer && !_game.Ruleset.DebugMode
                )
            );
        }
    }

    // Resets and refills every panels at a new round.
    private void NewRoundRefresh()
    {
        LblWallTilesLeft.Foreground = Brushes.Black;
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
                    FillHandPanel(e.PlayerIndex, e.Tile);
                });
            }
        };

        // event is forced because the subscription is made too late relative to first triggered event
        OnNotifyWallCount();

        StpDoras.SetDorasPanel(_game.Round.DoraIndicatorTiles, _game.Round.VisibleDorasCount);
        
        // TODO: exract tooltip content in resource
        LblDominantWind.Content = _game.DominantWind.ToWindDisplay();
        LblDominantWind.ToolTip = $"Vent dominant : {_game.DominantWind.DisplayName()}";
        LblEastTurnCount.Content = $"{_game.EastRank}";
        LblEastTurnCount.ToolTip = $"N° de tour en {_game.DominantWind.DisplayName()}";
        
        TxtHonba.Text = _game.HonbaCount.ToString();
        TxtPendingRiichi.Text = _game.PendingRiichiCount.ToString();

        foreach (var pIndex in Enum.GetValues<PlayerIndices>())
        {
            this.FindPanel(CombosPanel, pIndex).Children.Clear();
            FillHandPanel(pIndex);
            FillDiscardPanel(pIndex);
            this.FindName<Panel>(PlayerPanel, pIndex).ToolTip = _game.GetPlayerCurrentWind(pIndex).DisplayName();
            this.FindControl(WindLabel, pIndex).Content = _game.GetPlayerCurrentWind(pIndex).ToWindDisplay();
            this.FindControl(NameLabel, pIndex).Content = _game.Players[(int)pIndex].Name;
            this.FindControl(PointsLabel, pIndex).Content = $"{_game.Players[(int)pIndex].Points / 1000}k";
            this.FindName<Image>(RiichiStickImage, pIndex).Visibility = Visibility.Hidden;
        }

        RefreshPlayerTurnStyle();
        SetActionButtonsVisibility(preDiscard: true);

        SetWallsLength();
    }

    // Refresh the style of players when turn changes.
    private void RefreshPlayerTurnStyle()
    {
        Dispatcher.Invoke(() =>
        {
            foreach (var pIndex in Enum.GetValues<PlayerIndices>())
            {
                var brush = pIndex == _game.Round.CurrentPlayerIndex ? Brushes.OrangeRed : Brushes.White;
                this.FindName<Label>(PlayerLabel, pIndex).Foreground = brush;
                this.FindName<Label>(WindLabel, pIndex).Foreground = brush;
            }
        });
    }

    // Rebuilds the discard panel of the specified player.
    private TileButton? FillDiscardPanel(PlayerIndices pIndex)
    {
        for (var r = 1; r <= 3; r++)
        {
            this.FindPanel(DiscardPanels[r], pIndex).Children.Clear();
        }

        var reversed = pIndex == PlayerIndices.One || pIndex == PlayerIndices.Two;

        TileButton? lastButton = null;
        var i = 0;
        foreach (var tile in _game.Round.GetDiscard(pIndex))
        {
            var r = i < 6 ? 1 : (i < 12 ? 2 : 3);
            var panel = this.FindPanel(DiscardPanels[r], pIndex);
            var angle = (AnglePivot)pIndex;
            if (_game.Round.IsRiichiRank(pIndex, i))
            {
                angle = (AnglePivot)pIndex.RelativePlayerIndex(1);
            }
            lastButton = new TileButton(tile, angle: angle);
            if (reversed)
            {
                panel.Children.Insert(0, lastButton);
            }
            else
            {
                panel.Children.Add(lastButton);
            }
            i++;
        }

        return lastButton;
    }

    // Adds to the player stack its last combination.
    private void FillCombinationStack(PlayerIndices pIndex)
    {
        var panel = this.FindPanel(CombosPanel, pIndex);

        panel.Children.Clear();
        foreach (var combo in _game.Round.GetHand(pIndex).DeclaredCombinations)
        {
            panel.Children.Add(combo.CreateCombinationPanel(pIndex, _game.GetPlayerCurrentWind(pIndex)));
        }
    }

    // Sets the Visibility property of every action buttons
    private void SetActionButtonsVisibility(bool preDiscard = false, bool cpuPlay = false, bool skippedInnerKan = false)
    {
        // Default behavior.
        BtnChii.Visibility = Visibility.Collapsed;
        BtnPon.Visibility = Visibility.Collapsed;
        BtnKan.Visibility = Visibility.Collapsed;
        BtnTsumo.Visibility = Visibility.Collapsed;
        BtnRiichi.Visibility = Visibility.Collapsed;
        BtnRon.Visibility = Visibility.Collapsed;
        BtnSkipCall.Visibility = Visibility.Collapsed;
        GrdOverlayCanCall.Visibility = Visibility.Collapsed;

        BtnChii.Foreground = Brushes.Black;
        BtnPon.Foreground = Brushes.Black;
        BtnKan.Foreground = Brushes.Black;
        BtnRiichi.Foreground = Brushes.Black;
        BtnSkipCall.Foreground = Brushes.Black;

        var needAdvice = false;
        var advised = false;

        if (preDiscard)
        {
            // When the player has 14 tiles and need to discard
            // A kan call might be possible

            if (!skippedInnerKan)
            {
                var (canCall, decisionTile) = _game.Round.Advisor!.KanDecision(_humanPlayerIndex, true);
                if (canCall)
                {
                    BtnKan.Visibility = Visibility.Visible;
                    if (_game.Ruleset.DiscardTip)
                    {
                        needAdvice = true;
                        if (decisionTile != null)
                        {
                            BtnKan.Foreground = Brushes.DarkMagenta;
                            advised = true;
                        }
                    }
                }
            }
        }
        else if (cpuPlay)
        {
            // When the CPU is playing
            // Or it's player's turn but he has not pick yet

            if (_game.Round.IsHumanPlayer)
            {
                var (canChii, chiiChoice) = _game.Round.Advisor!.ChiiDecision();
                if (canChii)
                {
                    BtnChii.Visibility = Visibility.Visible;
                    if (_game.Ruleset.DiscardTip)
                    {
                        needAdvice = true;
                        if (chiiChoice != null)
                        {
                            BtnChii.Foreground = Brushes.DarkMagenta;
                            advised = true;
                        }
                    }
                }
            }

            if (_game.Round.CanCallPon(_humanPlayerIndex))
            {
                BtnPon.Visibility = Visibility.Visible;
                if (_game.Ruleset.DiscardTip)
                {
                    needAdvice = true;
                    if (_game.Round.Advisor!.PonDecision(_humanPlayerIndex))
                    {
                        BtnPon.Foreground = Brushes.DarkMagenta;
                        advised = true;
                    }
                }
            }

            var (canCall, decisionTile) = _game.Round.Advisor!.KanDecision(_humanPlayerIndex, false);
            if (canCall)
            {
                BtnKan.Visibility = Visibility.Visible;
                if (_game.Ruleset.DiscardTip)
                {
                    needAdvice = true;
                    if (decisionTile != null)
                    {
                        BtnKan.Foreground = Brushes.DarkMagenta;
                        advised = true;
                    }
                }
            }
        }

        if (needAdvice && !advised)
        {
            BtnSkipCall.Foreground = Brushes.DarkMagenta;
        }

        if (BtnChii.Visibility == Visibility.Visible
            || BtnPon.Visibility == Visibility.Visible
            || BtnKan.Visibility == Visibility.Visible)
        {
            BtnSkipCall.Visibility = Visibility.Visible;
            GrdOverlayCanCall.Visibility = Visibility.Visible;
            ActivateTimer(null);
        }
    }

    // Highlights a tile
    private void SetHighlight(Button buttonClickable)
    {
        buttonClickable.Style = FindResource(StyleHighlightTileResourceName) as Style;
        (buttonClickable.Content as Image)!.Opacity = 0.8;
    }

    // Comptes the length of walls
    private void SetWallsLength()
    {
        // the order of consumption of walls; the index follow the player index
        var wallIndexes = new[] { 0, 3, 2, 1 };
        for (var i = 1; i <= (int)_game.Round.WallOpeningIndex; i++)
        {
            for (var j = 0; j < wallIndexes.Length; j++)
            {
                wallIndexes[j] = wallIndexes[j] == 3 ? 0 : wallIndexes[j] + 1;
            }
        }

        // TODO: this is highly dependent on the size of container, defined directly in the view
        const double WallTileSizeRate = 0.2;

        // every tile to display in 4 walls
        // two tile stacked so we need half the count
        var wallTiles = (_game.Round.WallTiles.Count + _game.Round.AllTreasureTiles.Count) / 2;

        var tilesExpectedCoeff = 3;
        foreach (var iWall in wallIndexes)
        {
            var tilesCountForThisWall = Math.Max(0, Math.Min(_game.Round.FullTilesList.Count / 8, wallTiles - (_game.Round.FullTilesList.Count / 8 * tilesExpectedCoeff)));

            var wallPanel = this.FindName<StackPanel>(WallPanel, (PlayerIndices)iWall);

            // TODO: lazy, walls are always rebuilt
            wallPanel.Children.Clear();

            for (var oneTile = 1; oneTile <= tilesCountForThisWall; oneTile++)
            {
                wallPanel.Children.Add(new TileButton(null, null, iWall % 2 == 0 ? AnglePivot.A0 : AnglePivot.A90, true, WallTileSizeRate));
            }

            tilesExpectedCoeff--;
        }
    }

    #endregion Graphic tools

    #region Other methods

    // Raises the button click event, from the panel specified at the index (of children) specified.
    private void RaiseButtonClickEvent(PanelButton? pButton)
    {
        if (pButton != null)
        {
            var buttonObject = pButton.ChildrenButtonIndex < 0
                    ? FindName(pButton.PanelBaseName)
                    : this.FindPanel(pButton.PanelBaseName, _humanPlayerIndex).Children[pButton.ChildrenButtonIndex];
            (buttonObject as Button)!.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }
        else
        {
            SuggestDiscard();
        }
    }

    // Checks if the button clicked was ready.
    private bool IsCurrentlyClickable()
    {
        var isCurrentlyClickable = !_autoPlay.IsBusy;

        if (isCurrentlyClickable)
        {
            _timer?.Stop();
        }

        return isCurrentlyClickable;
    }

    // Activates the human decision timer and binds its event to a button click.
    private void ActivateTimer(Button? buttonToClick)
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
                    if (buttonToClick == null)
                    {
                        CancelCallProcess();
                    }
                    else
                    {
                        buttonToClick.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
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
        RaiseButtonClickEvent(RestrictDiscardWithTilesSelection(_riichiTiles!, BtnRiichiChoice_Click));
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
        CbbChrono.ItemsSource = GraphicTools.GetChronoDisplayValues();
        CbbChrono.SelectedIndex = Properties.Settings.Default.ChronoSpeed;

        CbbCpuSpeed.ItemsSource = GraphicTools.GetCpuSpeedDisplayValues();
        CbbCpuSpeed.SelectedIndex = Properties.Settings.Default.CpuSpeed;

        ChkSounds.IsChecked = Properties.Settings.Default.PlaySounds;
        ChkAutoTsumoRon.IsChecked = Properties.Settings.Default.AutoCallMahjong;
    }

    // Suggest a discard by changing the skin of a button
    private void SuggestDiscard()
    {
        if (!_game.Ruleset.DiscardTip)
        {
            return;
        }

        if (_game.Round.IsHumanPlayer && _game.Round.GetHand(_humanPlayerIndex).IsFullHand)
        {
            var discardChoice = _game.Round.Advisor!.DiscardDecision();

            var button = this.FindPanel(HandPanel, _humanPlayerIndex).Children.OfType<TileButton>()
                .Concat(this.FindPanel(PickPanel, _humanPlayerIndex).Children.OfType<TileButton>())
                .FirstOrDefault(x => x.Tile == discardChoice);
            if (button != null)
            {
                SetHighlight(button);
            }
        }
    }

    #endregion Other methods
}
