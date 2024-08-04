using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Gnoj_Ham_Library;

namespace Gnoj_Ham_View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string WINDOW_TITLE = "Gnoj-Ham";

        private readonly GamePivot _game;
        private readonly System.Media.SoundPlayer _tickSound;
        private System.Timers.Timer _timer;
        private System.Timers.ElapsedEventHandler _currentTimerHandler;
        private BackgroundWorker _autoPlay;
        private readonly Storyboard _overlayStoryboard;
        private bool _waitForDecision;
        private IReadOnlyList<TilePivot> _riichiTiles;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationToken _cancellationToken;

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
            LblPlayerP0.Content = playerName;

            _game = new GamePivot(playerName, ruleset, save, new Random());
            _tickSound = new System.Media.SoundPlayer(Properties.Resources.tick);

            _overlayStoryboard = FindResource("StbHideOverlay") as Storyboard;
            Storyboard.SetTarget(_overlayStoryboard, GrdOverlayCall);

            ApplyConfigurationToOverlayStoryboard();

            SetChronoTime();

            FixWindowDimensions();

            NewRoundRefresh();

            InitializeAutoPlayWorker();

            BindConfiguration();

            ContentRendered += delegate (object sender, EventArgs evt)
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
                Discard((sender as Button).Tag as TilePivot);
            }
        }

        private void BtnChiiChoice_Click(object sender, RoutedEventArgs e)
        {
            if (IsCurrentlyClickable())
            {
                _waitForDecision = false;
                var tag = (Tuple<TilePivot, bool>)(sender as Button).Tag;
                ChiiCall(tag);
            }
        }

        private void BtnKanChoice_Click(object sender, RoutedEventArgs e)
        {
            if (IsCurrentlyClickable())
            {
                _waitForDecision = false;
                HumanKanCallProcess((sender as Button).Tag as TilePivot, null);
            }
        }

        private void BtnPon_Click(object sender, RoutedEventArgs e)
        {
            if (IsCurrentlyClickable())
            {
                PonCall(GamePivot.HUMAN_INDEX);
                SuggestDiscard();
            }
        }

        private void BtnChii_Click(object sender, RoutedEventArgs e)
        {
            if (IsCurrentlyClickable())
            {
                var tileChoices = _game.Round.CanCallChii();

                if (tileChoices.Keys.Count > 0)
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
                var kanTiles = _game.Round.CanCallKan(GamePivot.HUMAN_INDEX);
                if (kanTiles.Count > 0)
                {
                    if (_game.Round.IsHumanPlayer)
                    {
                        RaiseButtonClickEvent(RestrictDiscardWithTilesSelection(kanTiles.ToDictionary(t => t, t => false), BtnKanChoice_Click));
                    }
                    else
                    {
                        HumanKanCallProcess(null, _game.Round.PreviousPlayerIndex);
                    }
                    SuggestDiscard();
                }
            }
        }

        private void BtnRiichiChoice_Click(object sender, RoutedEventArgs e)
        {
            if (IsCurrentlyClickable())
            {
                _waitForDecision = false;
                CallRiichi((sender as Button).Tag as TilePivot);
            }
        }

        private void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
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
                CancelDiscardHighlight();
                if (BtnKan.Visibility == Visibility.Visible && _game.Round.IsHumanPlayer)
                {
                    RefreshPlayerTurnStyle();
                    SetActionButtonsVisibility(preDiscard: true, skippedInnerKan: true);
                    if (_game.Round.HumanCanAutoDiscard())
                    {
                        // Not a real CPU sleep: the auto-discard by human player is considered as such
                        Thread.Sleep(((CpuSpeedPivot)Properties.Settings.Default.CpuSpeed).ParseSpeed());
                        RaiseButtonClickEvent(new PanelButton("StpPickP", 0));
                    }
                    else
                    {
                        ActivateTimer(StpPickP0.Children[0] as Button);
                    }
                }
                else
                {
                    RunAutoPlay(skipCurrentAction: true);
                }
            }
            else if (StpPickP0.Children.Count > 0)
            {
                if (BtnRiichi.Visibility == Visibility.Visible)
                {
                    SetActionButtonsVisibility();
                    SuggestDiscard();
                    ActivateTimer(StpPickP0.Children[0] as Button);
                }
                else
                {
                    RaiseButtonClickEvent(new PanelButton("StpPickP", 0));
                }
            }
        }

        private void BtnRon_Click(object sender, RoutedEventArgs e)
        {
            if (IsCurrentlyClickable())
            {
                _overlayStoryboard.Completed += TriggerHumanRonAfterOverlayStoryboard;
                InvokeOverlay("Ron", GamePivot.HUMAN_INDEX);
            }
        }

        private void BtnTsumo_Click(object sender, RoutedEventArgs e)
        {
            if (IsCurrentlyClickable())
            {
                _overlayStoryboard.Completed += TriggerNewRoundAfterOverlayStoryboard;
                InvokeOverlay("Tsumo", GamePivot.HUMAN_INDEX);
            }
        }

        private void BtnRiichi_Click(object sender, RoutedEventArgs e)
        {
            if (IsCurrentlyClickable())
            {
                _overlayStoryboard.Completed += TriggerRiichiChoiceAfterOverlayStoryboard;
                InvokeOverlay("Riichi", GamePivot.HUMAN_INDEX);
            }
        }

        private void BtnSkipCall_Click(object sender, RoutedEventArgs e)
        {
            Grid_MouseDoubleClick(null, null);
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

        // Initializes a background worker which orchestrates the CPU actions.
        private void InitializeAutoPlayWorker()
        {
            _autoPlay = new BackgroundWorker
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false
            };
            _autoPlay.DoWork += delegate (object sender, DoWorkEventArgs evt)
            {
                var argumentsList = evt.Argument as object[];

                var autoPlay = new AutoPlayPivot(_game);

                autoPlay.ReadyToCallNotifier += e =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        switch (e.Call)
                        {
                            case CallTypePivot.Chii:
                                FillHandPanel(_game.Round.CurrentPlayerIndex);
                                FillCombinationStack(_game.Round.CurrentPlayerIndex);
                                FillDiscardPanel(_game.Round.PreviousPlayerIndex);
                                SetActionButtonsVisibility(cpuPlay: !_game.Round.IsHumanPlayer);
                                if (_game.Round.IsHumanPlayer)
                                {
                                    ActivateTimer(GetFirstAvailableDiscardButton());
                                }
                                break;
                            case CallTypePivot.Pon:
                                var isCpu = e.PlayerIndex != GamePivot.HUMAN_INDEX;
                                FillHandPanel(e.PlayerIndex);
                                FillCombinationStack(e.PlayerIndex);
                                FillDiscardPanel(e.PreviousPlayerIndex);
                                SetActionButtonsVisibility(cpuPlay: isCpu);
                                if (!isCpu)
                                {
                                    ActivateTimer(GetFirstAvailableDiscardButton());
                                }
                                break;
                            case CallTypePivot.Riichi:
                                FillHandPanel(_game.Round.PreviousPlayerIndex);
                                FillDiscardPanel(_game.Round.PreviousPlayerIndex);
                                SetActionButtonsVisibility(cpuPlay: !_game.Round.PreviousIsHumanPlayer);
                                this.FindName<Image>("RiichiStickP", _game.Round.PreviousPlayerIndex).Visibility = Visibility.Visible;
                                break;
                            case CallTypePivot.NoCall:
                                FillHandPanel(_game.Round.PreviousPlayerIndex);
                                FillDiscardPanel(_game.Round.PreviousPlayerIndex);
                                SetActionButtonsVisibility(cpuPlay: !_game.Round.PreviousIsHumanPlayer);
                                break;
                            case CallTypePivot.Kan:
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
                autoPlay.PickNotifier += e =>
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
                autoPlay.DiscardTileNotifier += e =>
                {
                    Dispatcher.Invoke(() => HighlightPreviousPlayerDiscard());
                };
                autoPlay.HumanCallNotifier += e =>
                {
                    Button autoButtonOnTimer = null;
                    Dispatcher.Invoke(() =>
                    {
                        if (e.Call == CallTypePivot.NoCall)
                        {
                            autoButtonOnTimer = StpPickP0.Children[0] as Button;
                        }
                        else
                        {
                            GrdOverlayCanCall.Visibility = Visibility.Visible;
                            BtnSkipCall.Visibility = Visibility.Visible;
                            switch (e.Call)
                            {
                                case CallTypePivot.Riichi:
                                    BtnRiichi.Visibility = Visibility.Visible;
                                    if (e.RiichiAdvised)
                                        BtnRiichi.Foreground = Brushes.DarkMagenta;
                                    else
                                        BtnSkipCall.Foreground = Brushes.DarkMagenta;
                                    break;
                                case CallTypePivot.Ron:
                                    BtnRon.Visibility = Visibility.Visible;
                                    break;
                                case CallTypePivot.Tsumo:
                                    BtnTsumo.Visibility = Visibility.Visible;
                                    break;
                            }
                        }
                    });
                    ActivateTimer(autoButtonOnTimer);
                };
                autoPlay.CallNotifier += e =>
                {
                    InvokeOverlay($"{e.Action}", e.PlayerIndex);
                };
                autoPlay.RiichiChoicesNotifier += e =>
                {
                    _riichiTiles = e.Tiles;
                };
                autoPlay.TurnChangeNotifier += e =>
                {
                    RefreshPlayerTurnStyle();
                };

                evt.Result = autoPlay.RunAutoPlay(
                    _cancellationToken,
                    (bool)argumentsList[0],
                    (bool)argumentsList[1],
                    Properties.Settings.Default.AutoCallMahjong,
                    ((CpuSpeedPivot)Properties.Settings.Default.CpuSpeed).ParseSpeed());
            };
            _autoPlay.RunWorkerCompleted += delegate (object sender, RunWorkerCompletedEventArgs evt)
            {
                if (!_cancellationToken.IsCancellationRequested)
                {
                    var (endOfRound, ronPlayerId, humanAction) =  ((bool, int ?, CallTypePivot ?))evt.Result;
                    if (endOfRound)
                    {
                        NewRound(ronPlayerId);
                    }
                    else
                    {
                        var button = humanAction.HasValue
                            ? (humanAction == CallTypePivot.NoCall
                                ? new PanelButton("StpPickP", 0)
                                : new PanelButton($"Btn{humanAction}", -1))
                            : null;
                        RaiseButtonClickEvent(button);
                    }
                }
            };
        }

        // Proceeds to new round.
        private void NewRound(int? ronPlayerIndex)
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
        private void RunAutoPlay(bool skipCurrentAction = false, bool humanRonPending = false)
        {
            if (!_autoPlay.IsBusy)
            {
                _autoPlay.RunWorkerAsync(new object[] { skipCurrentAction, humanRonPending });
            }
        }

        // Checks ron call for every players.
        private bool CheckOpponensRonCall(bool humanRonPending)
        {
            var opponentsCallRon = _game.Round.IaManager.RonDecision(humanRonPending);
            foreach (var opponentPlayerIndex in opponentsCallRon)
            {
                InvokeOverlay("Ron", opponentPlayerIndex);
            }

            return humanRonPending || opponentsCallRon.Count > 0;
        }

        // Restrict possible discards on the specified selection of tiles.
        private PanelButton RestrictDiscardWithTilesSelection(IDictionary<TilePivot, bool> tileChoices, RoutedEventHandler handler)
        {
            PanelButton result = null;

            SetActionButtonsVisibility();

            var buttons = StpHandP0.Children.OfType<Button>().ToList();
            if (StpPickP0.Children.Count > 0)
            {
                buttons.Add(StpPickP0.Children[0] as Button);
            }

            var clickableButtons = new List<Button>(tileChoices.Keys.Count);
            foreach (var tileKey in tileChoices.Keys)
            {
                // Changes the event of every buttons concerned by the call...
                var buttonClickable = buttons
                    .Where(b => (b.Tag as TilePivot) == tileKey)
                    .OrderBy(b => (b.Tag as TilePivot).IsRedDora) // in case of autoplay, we don't want the red dora discarded where there's a not-red tile
                    .First();
                buttonClickable.Click += handler;
                buttonClickable.Click -= BtnDiscard_Click;
                if (handler == BtnChiiChoice_Click)
                {
                    buttonClickable.Tag = new Tuple<TilePivot, bool>(tileKey, tileChoices[tileKey]);
                }
                SetHighlight(buttonClickable);
                clickableButtons.Add(buttonClickable);
            }

            // ...and disables every buttons not concerned.
            buttons.Where(b => !clickableButtons.Contains(b)).All(b => { b.IsEnabled = false; return true; });

            if (clickableButtons.Count == 1)
            {
                // Only one possibility : initiates the auto-discard.
                var buttonIndexInHandPanel = StpHandP0.Children.IndexOf(clickableButtons[0]);
                result = buttonIndexInHandPanel >= 0 ? new PanelButton("StpHandP", buttonIndexInHandPanel) : new PanelButton("StpPickP", 0);
            }
            else
            {
                _waitForDecision = true;
                ActivateTimer(clickableButtons[0]);
            }

            return result;
        }

        // Discard action (human or CPU).
        private void Discard(TilePivot tile)
        {
            if (!_game.Round.IsHumanPlayer)
            {
                Thread.Sleep(((CpuSpeedPivot)Properties.Settings.Default.CpuSpeed).ParseSpeed());
            }

            if (_game.Round.Discard(tile))
            {
                Dispatcher.Invoke(() =>
                {
                    FillHandPanel(_game.Round.PreviousPlayerIndex);
                    FillDiscardPanel(_game.Round.PreviousPlayerIndex);
                    SetActionButtonsVisibility(cpuPlay: !_game.Round.PreviousIsHumanPlayer);
                });

                if (_game.Round.PreviousIsHumanPlayer)
                {
                    RunAutoPlay();
                }
            }
        }

        // Chii call action (human or CPU).
        private void ChiiCall(Tuple<TilePivot, bool> chiiTilePick)
        {
            RefreshPlayerTurnStyle();
            if (_game.Round.CallChii(chiiTilePick.Item2 ? chiiTilePick.Item1.Number - 1 : chiiTilePick.Item1.Number))
            {
                InvokeOverlay("Chii", _game.Round.CurrentPlayerIndex);

                Dispatcher.Invoke(() =>
                {
                    FillHandPanel(_game.Round.CurrentPlayerIndex);
                    FillCombinationStack(_game.Round.CurrentPlayerIndex);
                    FillDiscardPanel(_game.Round.PreviousPlayerIndex);
                    SetActionButtonsVisibility(cpuPlay: !_game.Round.IsHumanPlayer);
                    if (_game.Round.IsHumanPlayer)
                    {
                        ActivateTimer(GetFirstAvailableDiscardButton());
                    }
                });

                if (!_game.Round.IsHumanPlayer)
                {
                    Discard(_game.Round.IaManager.DiscardDecision(new List<TilePivot>()));
                }
            }
        }

        // Pon call action (human or CPU).
        private void PonCall(int playerIndex)
        {
            RefreshPlayerTurnStyle();

            // Note : this value is stored here because the call to "CallPon" makes it change.
            var previousPlayerIndex = _game.Round.PreviousPlayerIndex;
            var isCpu = playerIndex != GamePivot.HUMAN_INDEX;

            if (_game.Round.CallPon(playerIndex))
            {
                InvokeOverlay("Pon", playerIndex);

                Dispatcher.Invoke(() =>
                {
                    FillHandPanel(playerIndex);
                    FillCombinationStack(playerIndex);
                    FillDiscardPanel(previousPlayerIndex);
                    SetActionButtonsVisibility(cpuPlay: isCpu);
                    if (!isCpu)
                    {
                        ActivateTimer(GetFirstAvailableDiscardButton());
                    }
                });

                if (isCpu)
                {
                    Discard(_game.Round.IaManager.DiscardDecision(new List<TilePivot>()));
                }
            }
        }

        // Riichi call action (human or CPU).
        private void CallRiichi(TilePivot tile)
        {
            if (!_game.Round.IsHumanPlayer)
            {
                InvokeOverlay("Riichi", _game.Round.CurrentPlayerIndex);
                Thread.Sleep(((CpuSpeedPivot)Properties.Settings.Default.CpuSpeed).ParseSpeed());
            }

            if (_game.Round.CallRiichi(tile))
            {
                Dispatcher.Invoke(() =>
                {
                    FillHandPanel(_game.Round.PreviousPlayerIndex);
                    FillDiscardPanel(_game.Round.PreviousPlayerIndex);
                    SetActionButtonsVisibility(cpuPlay: !_game.Round.PreviousIsHumanPlayer);
                    this.FindName<Image>("RiichiStickP", _game.Round.PreviousPlayerIndex).Visibility = Visibility.Visible;
                });

                if (_game.Round.PreviousIsHumanPlayer)
                {
                    RunAutoPlay();
                }
            }
        }

        // Inner process kan call.
        private void HumanKanCallProcess(TilePivot tile, int? previousPlayerIndex)
        {
            RefreshPlayerTurnStyle();

            _game.Round.CallKan(GamePivot.HUMAN_INDEX, tile);
            InvokeOverlay("Kan", GamePivot.HUMAN_INDEX);
            if (CheckOpponensRonCall(false))
            {
                _game.Round.UndoPickCompensationTile();
                NewRound(_game.Round.CurrentPlayerIndex);
            }
            else
            {
                CommonCallKan(previousPlayerIndex);

                if (_game.Round.CanCallTsumo(true))
                {
                    GrdOverlayCanCall.Visibility = Visibility.Visible;
                    BtnTsumo.Visibility = Visibility.Visible;
                    BtnSkipCall.Visibility = Visibility.Visible;
                    if (Properties.Settings.Default.AutoCallMahjong)
                    {
                        BtnTsumo.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    }
                    else
                    {
                        ActivateTimer(null);
                    }
                }
                else
                {
                    _riichiTiles = _game.Round.CanCallRiichi();
                    if (_riichiTiles.Count > 0)
                    {
                        var riichiDecision = _game.Ruleset.DiscardTip && _game.Round.IaManager.RiichiDecision().choice != null;

                        BtnRiichi.Visibility = Visibility.Visible;
                        BtnSkipCall.Visibility = Visibility.Visible;
                        GrdOverlayCanCall.Visibility = Visibility.Visible;

                        if (riichiDecision)
                            BtnRiichi.Foreground = Brushes.DarkMagenta;
                        else
                            BtnSkipCall.Foreground = Brushes.DarkMagenta;

                        ActivateTimer(null);
                    }
                    else if (_game.Round.HumanCanAutoDiscard())
                    {
                        // Auto discard if riichi and the compensation tile is not interesting
                        // Never tested!
                        RaiseButtonClickEvent(new PanelButton("StpPickP", 0));
                    }
                }
            }
        }

        #endregion General orchestration

        #region Graphic tools

        // Common trunk of the kan call process.
        private void CommonCallKan(int? previousPlayerIndex)
        {
            Dispatcher.Invoke(() =>
            {
                if (previousPlayerIndex.HasValue)
                {
                    FillDiscardPanel(previousPlayerIndex.Value);
                }
                FillCombinationStack(_game.Round.CurrentPlayerIndex);
                SetActionButtonsVisibility(cpuPlay: !_game.Round.IsHumanPlayer, preDiscard: _game.Round.IsHumanPlayer);
                StpDoras.SetDorasPanel(_game.Round.DoraIndicatorTiles, _game.Round.VisibleDorasCount);
            });
        }

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
        private Button GetFirstAvailableDiscardButton()
        {
            return this.FindPanel("StpHandP", GamePivot.HUMAN_INDEX)
                .Children
                .OfType<Button>()
                .First(b => _game.Round.CanDiscard(b.Tag as TilePivot));
        }

        // Displays the call overlay.
        private void InvokeOverlay(string callName, int playerIndex)
        {
            Dispatcher.Invoke(() =>
            {
                BtnOpponentCall.Content = $"{callName} !";
                BtnOpponentCall.HorizontalAlignment = playerIndex == 1 ? HorizontalAlignment.Right : (playerIndex == 3 ? HorizontalAlignment.Left : HorizontalAlignment.Center);
                BtnOpponentCall.VerticalAlignment = playerIndex == 0 ? VerticalAlignment.Bottom : (playerIndex == 2 ? VerticalAlignment.Top : VerticalAlignment.Center);
                BtnOpponentCall.Margin = new Thickness(playerIndex == 3 ? 20 : 0, playerIndex == 2 ? 20 : 0, playerIndex == 1 ? 20 : 0, playerIndex == 0 ? 20 : 0);
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

            double dim1 = GraphicTools.TILE_HEIGHT + GraphicTools.DEFAULT_TILE_MARGIN;
            double dim2 = (GraphicTools.TILE_HEIGHT * 3) + (GraphicTools.DEFAULT_TILE_MARGIN * 2);
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

            for (var i = 0; i < _game.Players.Count; i++)
            {
                for (var j = 1; j <= 3; j++)
                {
                    var panel = this.FindPanel($"StpDiscard{j}P", i);
                    if (i % 2 == 0)
                    {
                        panel.Height = GraphicTools.TILE_HEIGHT;
                    }
                    else
                    {
                        panel.Width = GraphicTools.TILE_HEIGHT;
                    }
                }
            }
        }

        // Clears and refills the hand panel of the specified player index.
        private void FillHandPanel(int pIndex, TilePivot pickTile = null)
        {
            var isHuman = pIndex == GamePivot.HUMAN_INDEX;

            var panel = this.FindPanel("StpHandP", pIndex);

            this.FindPanel("StpPickP", pIndex).Children.Clear();

            panel.Children.Clear();
            foreach (var tile in _game.Round.GetHand(pIndex).ConcealedTiles)
            {
                if (pickTile == null || !ReferenceEquals(pickTile, tile))
                {
                    panel.Children.Add(tile.GenerateTileButton(isHuman && !_game.Round.IsRiichi(pIndex) ?
                        BtnDiscard_Click : (RoutedEventHandler)null, (AnglePivot)pIndex, !isHuman && !_game.Ruleset.DebugMode));
                }
            }

            if (pickTile != null)
            {
                this.FindPanel("StpPickP", pIndex).Children.Add(
                    pickTile.GenerateTileButton(
                        _game.Round.IsHumanPlayer ? BtnDiscard_Click : (RoutedEventHandler)null,
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
            _game.Round.NotifyPick += delegate (TileEventArgs e)
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
            LblDominantWind.Content = _game.DominantWind.ToWindDisplay();
            LblDominantWind.ToolTip = $"Vent dominant : {_game.DominantWind.DisplayName()}";
            LblEastTurnCount.Content = $"{_game.EastRank}";
            LblEastTurnCount.ToolTip = $"N° de tour en {_game.DominantWind.DisplayName()}";
            TxtHonba.Text = _game.HonbaCount.ToString();
            TxtPendingRiichi.Text = _game.PendingRiichiCount.ToString();

            for (var pIndex = 0; pIndex < _game.Players.Count; pIndex++)
            {
                this.FindPanel("StpCombosP", pIndex).Children.Clear();
                FillHandPanel(pIndex);
                FillDiscardPanel(pIndex);
                this.FindName<Panel>("StpPlayerP", pIndex).ToolTip = _game.GetPlayerCurrentWind(pIndex).DisplayName();
                this.FindControl("LblWindP", pIndex).Content = _game.GetPlayerCurrentWind(pIndex).ToWindDisplay();
                this.FindControl("LblNameP", pIndex).Content = _game.Players.ElementAt(pIndex).Name;
                this.FindControl("LblPointsP", pIndex).Content = $"{_game.Players.ElementAt(pIndex).Points / 1000}k";
                this.FindName<Image>("RiichiStickP", pIndex).Visibility = Visibility.Hidden;
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
                for (var pIndex = 0; pIndex < _game.Players.Count; pIndex++)
                {
                    this.FindName<Label>("LblPlayerP", pIndex).Foreground = pIndex == _game.Round.CurrentPlayerIndex ? Brushes.OrangeRed : Brushes.White;
                    this.FindName<Label>("LblWindP", pIndex).Foreground = pIndex == _game.Round.CurrentPlayerIndex ? Brushes.OrangeRed : Brushes.White;
                }
            });
        }

        // Rebuilds the discard panel of the specified player.
        private Button FillDiscardPanel(int pIndex)
        {
            for (var r = 1; r <= 3; r++)
            {
                this.FindPanel($"StpDiscard{r}P", pIndex).Children.Clear();
            }

            var reversed = pIndex == 1 || pIndex == 2;

            Button lastButton = null;
            var i = 0;
            foreach (var tile in _game.Round.GetDiscard(pIndex))
            {
                var r = i < 6 ? 1 : (i < 12 ? 2 : 3);
                var panel = this.FindPanel($"StpDiscard{r}P", pIndex);
                var angle = (AnglePivot)pIndex;
                if (_game.Round.IsRiichiRank(pIndex, i))
                {
                    angle = (AnglePivot)pIndex.RelativePlayerIndex(1);
                }
                if (reversed)
                {
                    lastButton = tile.GenerateTileButton(angle: angle);
                    panel.Children.Insert(0, lastButton);
                }
                else
                {
                    lastButton = tile.GenerateTileButton(angle: angle);
                    panel.Children.Add(lastButton);
                }
                i++;
            }

            return lastButton;
        }

        // Highlights the last tile of the previous player discard (to show it's available for a call)
        private void HighlightPreviousPlayerDiscard()
        {
            var highlightButton = FillDiscardPanel(_game.Round.PreviousPlayerIndex);
            if (highlightButton != null)
            {
                SetHighlight(highlightButton);
            }
        }

        // Cancels the Highlighting of the previous player discard
        private void CancelDiscardHighlight()
        {
            // TODO: lazy
            FillDiscardPanel(_game.Round.PreviousPlayerIndex);
        }

        // Adds to the player stack its last combination.
        private void FillCombinationStack(int pIndex)
        {
            var panel = this.FindPanel("StpCombosP", pIndex);

            panel.Children.Clear();
            foreach (var combo in _game.Round.GetHand(pIndex).DeclaredCombinations)
            {
                panel.Children.Add(CreateCombinationPanel(pIndex, combo));
            }
        }

        // Creates a panel for the specified combination.
        private StackPanel CreateCombinationPanel(int pIndex, TileComboPivot combo)
        {
            var panel = new StackPanel
            {
                Orientation = pIndex == 0 || pIndex == 2 ? Orientation.Horizontal : Orientation.Vertical
            };

            var pWind = _game.GetPlayerCurrentWind(pIndex);

            var i = 0;
            var tileTuples = combo.GetSortedTilesForDisplay(pWind);
            if (pIndex > 0 && pIndex < 3)
            {
                tileTuples.Reverse();
            }

            foreach (var tileTuple in tileTuples)
            {
                panel.Children.Add(tileTuple.Item1.GenerateTileButton(null,
                    (AnglePivot)(tileTuple.Item2 ? pIndex.RelativePlayerIndex(1) : pIndex),
                    combo.IsConcealedDisplay(i)));
                i++;
            }

            return panel;
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
                    var kanPossibilities = _game.Round.CanCallKan(GamePivot.HUMAN_INDEX);
                    if (kanPossibilities.Count > 0)
                    {
                        BtnKan.Visibility = Visibility.Visible;
                        if (_game.Ruleset.DiscardTip)
                        {
                            needAdvice = true;
                            if (_game.Round.IaManager.KanDecisionAdvice(kanPossibilities, true))
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
                    var chiiPossibilities = _game.Round.CanCallChii();
                    if (chiiPossibilities.Count > 0)
                    {
                        BtnChii.Visibility = Visibility.Visible;
                        if (_game.Ruleset.DiscardTip)
                        {
                            needAdvice = true;
                            if (_game.Round.IaManager.ChiiDecisionAdvice(chiiPossibilities))
                            {
                                BtnChii.Foreground = Brushes.DarkMagenta;
                                advised = true;
                            }
                        }
                    }
                }

                if (_game.Round.CanCallPon(GamePivot.HUMAN_INDEX))
                {
                    BtnPon.Visibility = Visibility.Visible;
                    if (_game.Ruleset.DiscardTip)
                    {
                        needAdvice = true;
                        if (_game.Round.IaManager.PonDecisionAdvice())
                        {
                            BtnPon.Foreground = Brushes.DarkMagenta;
                            advised = true;
                        }
                    }
                }

                var kanPossibilities = _game.Round.CanCallKan(GamePivot.HUMAN_INDEX);
                if (kanPossibilities.Count > 0)
                {
                    BtnKan.Visibility = Visibility.Visible;
                    if (_game.Ruleset.DiscardTip)
                    {
                        needAdvice = true;
                        if (_game.Round.IaManager.KanDecisionAdvice(kanPossibilities, false))
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
            buttonClickable.Style = FindResource("StyleHighlightTile") as Style;
            (buttonClickable.Content as Image).Opacity = 0.8;
        }

        // Comptes the length of walls
        private void SetWallsLength()
        {
            // the order of consumption of walls; the index follow the player index
            var wallIndexes = new[] { 0, 3, 2, 1 };
            for (var i = 1; i <= _game.Round.WallOpeningIndex; i++)
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
                var tilesCountForThisWall = Math.Max(0, Math.Min(GamePivot.WALL_TILES_COUNT, wallTiles - (GamePivot.WALL_TILES_COUNT * tilesExpectedCoeff)));

                var wallPanel = this.FindName<StackPanel>("PnlWall", iWall);

                // TODO: lazy, walls are always rebuilt
                wallPanel.Children.Clear();

                for (var oneTile = 1; oneTile <= tilesCountForThisWall; oneTile++)
                {
                    wallPanel.Children.Add(GraphicTools.GenerateTileButton(null, null, iWall % 2 == 0 ? AnglePivot.A0 : AnglePivot.A90, true, WallTileSizeRate));
                }

                tilesExpectedCoeff--;
            }
        }

        #endregion Graphic tools

        #region Other methods

        // Raises the button click event, from the panel specified at the index (of children) specified.
        private void RaiseButtonClickEvent(PanelButton pButton)
        {
            if (pButton != null)
            {
                var btn = (pButton.ChildrenButtonIndex < 0 ? FindName(pButton.PanelBaseName) :
                    this.FindPanel(pButton.PanelBaseName, GamePivot.HUMAN_INDEX).Children[pButton.ChildrenButtonIndex]) as Button;
                btn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
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
        private void ActivateTimer(Button buttonToClick)
        {
            if (_timer != null)
            {
                if (_currentTimerHandler != null)
                {
                    _timer.Elapsed -= _currentTimerHandler;
                }
                _currentTimerHandler = delegate (object sender, System.Timers.ElapsedEventArgs e)
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (buttonToClick == null)
                        {
                            Grid_MouseDoubleClick(null, null);
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
            (_overlayStoryboard.Children.Last() as ObjectAnimationUsingKeyFrames).KeyFrames[1].KeyTime =
                KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0, 0, CpuSpeedPivot.S500.ParseSpeed()));
        }

        // Handler to trigger a new round at the end of the overlay storyboard animation.
        private void TriggerNewRoundAfterOverlayStoryboard(object sender, EventArgs e)
        {
            _overlayStoryboard.Completed -= TriggerNewRoundAfterOverlayStoryboard;
            NewRound(null);
        }

        // Handler to trigger a post-riichi "RestrictDiscardWithTilesSelection" at the end of the overlay storyboard animation.
        private void TriggerRiichiChoiceAfterOverlayStoryboard(object sender, EventArgs e)
        {
            _overlayStoryboard.Completed -= TriggerRiichiChoiceAfterOverlayStoryboard;
            RaiseButtonClickEvent(RestrictDiscardWithTilesSelection(_riichiTiles.ToDictionary(t => t, t => false), BtnRiichiChoice_Click));
        }

        // Handler to trigger a human ron at the end of the overlay storyboard animation.
        private void TriggerHumanRonAfterOverlayStoryboard(object sender, EventArgs e)
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

            if (_game.Round.IsHumanPlayer && _game.Round.GetHand(GamePivot.HUMAN_INDEX).IsFullHand)
            {
                var discardChoice = _game.Round.IaManager.DiscardDecision(null);
                var button = StpHandP0.Children.OfType<Button>()
                    .Concat(StpPickP0.Children.OfType<Button>())
                    .FirstOrDefault(x => x.Tag == discardChoice);
                if (button != null)
                {
                    SetHighlight(button);
                }
            }
        }

        #endregion Other methods
    }
}
