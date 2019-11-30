using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Gnoj_Ham;

namespace Gnoj_HamView
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string WINDOW_TITLE = "Gnoj-Ham";

        private readonly GamePivot _game;
        private bool _pauseAutoplay;
        private System.Media.SoundPlayer _tickSound;
        private System.Timers.Timer _timer;
        private System.Timers.ElapsedEventHandler _currentTimerHandler;
        private BackgroundWorker _autoPlay;
        private Storyboard _overlayStoryboard;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="playerName">Human player name.</param>
        /// <param name="pointRule">Indicates the initial points count for every players.</param>
        /// <param name="endOfGameRule">The ruel to end a game.</param>
        /// <param name="useRedDoras">Indicates if red doras should be used.</param>
        /// <param name="sortedDraw"><c>True</c> to not randomize the tile draw.</param>
        /// <param name="useNagashiMangan"><c>True</c> to use the yaku 'Nagashi Mangan'.</param>
        /// <param name="useRenhou"><c>True</c> to use the yakuman 'Renhou'.</param>
        public MainWindow(string playerName, InitialPointsRulePivot pointRule, EndOfGameRulePivot endOfGameRule,
            bool useRedDoras, bool sortedDraw, bool useNagashiMangan, bool useRenhou)
        {
            InitializeComponent();

            _game = new GamePivot(playerName, pointRule, endOfGameRule, useRedDoras, sortedDraw, useNagashiMangan, useRenhou);
            _tickSound = new System.Media.SoundPlayer(Properties.Resources.tick);

            _overlayStoryboard = FindResource("StbHideOverlay") as Storyboard;
            Storyboard.SetTarget(_overlayStoryboard, GrdOverlayCall);

            ApplyConfigurationToOverlayStoryboard();

            SetChronoTime();

            FixWindowDimensions();

            NewRoundRefresh();

            InitializeAutoPlayWorker();

            ContentRendered += delegate (object sender, EventArgs evt)
            {
                RunAutoPlay();
            };
        }

        #region Window events

        private void BtnConfiguration_Click(object sender, RoutedEventArgs e)
        {
            bool timerWasRunning = _timer?.Enabled == true;
            _timer?.Stop();
            _pauseAutoplay = true;

            new IntroWindow(_game).ShowDialog();
            ApplyConfigurationToOverlayStoryboard();

            _pauseAutoplay = false;
            if (timerWasRunning && _timer != null)
            {
                _timer.Start();
            }
        }

        private void BtnDiscard_Click(object sender, RoutedEventArgs e)
        {
            if (IsCurrentlyClickable(e))
            {
                Discard((sender as Button).Tag as TilePivot);
            }
        }

        private void BtnChiiChoice_Click(object sender, RoutedEventArgs e)
        {
            if (IsCurrentlyClickable(e))
            {
                Tuple<TilePivot, bool> tag = (Tuple<TilePivot, bool>)((sender as Button).Tag);
                ChiiCall(tag);
            }
        }

        private void BtnKanChoice_Click(object sender, RoutedEventArgs e)
        {
            if (IsCurrentlyClickable(e))
            {
                HumanKanCallProcess((sender as Button).Tag as TilePivot, null);
            }
        }

        private void BtnPon_Click(object sender, RoutedEventArgs e)
        {
            if (IsCurrentlyClickable(e))
            {
                PonCall(GamePivot.HUMAN_INDEX);
            }
        }

        private void BtnChii_Click(object sender, RoutedEventArgs e)
        {
            if (IsCurrentlyClickable(e))
            {
                Dictionary<TilePivot, bool> tileChoices = _game.Round.CanCallChii();

                if (tileChoices.Keys.Count > 0)
                {
                    Tuple<string, int> restrictResult = RestrictDiscardWithTilesSelection(tileChoices, BtnChiiChoice_Click);
                    RaiseButtonClickEvent(restrictResult.Item1, restrictResult.Item2);
                }
            }
        }

        private void BtnKan_Click(object sender, RoutedEventArgs e)
        {
            if (IsCurrentlyClickable(e))
            {
                List<TilePivot> kanTiles = _game.Round.CanCallKan(GamePivot.HUMAN_INDEX);
                if (kanTiles.Count > 0)
                {
                    if (_game.Round.IsHumanPlayer)
                    {
                        Tuple<string, int> restrictResult = RestrictDiscardWithTilesSelection(kanTiles.ToDictionary(t => t, t => false), BtnKanChoice_Click);
                        RaiseButtonClickEvent(restrictResult.Item1, restrictResult.Item2);
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
            if (IsCurrentlyClickable(e))
            {
                CallRiichi((sender as Button).Tag as TilePivot);
            }
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_autoPlay.IsBusy)
            {
                return;
            }

            _timer?.Stop();

            if (BtnPon.Visibility == Visibility.Visible
                || BtnChii.Visibility == Visibility.Visible
                || BtnKan.Visibility == Visibility.Visible)
            {
                RunAutoPlay(true);
            }
            else if (StpPickP0.Children.Count > 0)
            {
                RaiseButtonClickEvent("StpPickP0", 0);
            }
        }

        #endregion Window events

        #region Human decisions

        // Manages riichi call opportunities.
        private Tuple<bool, string, int> HumanCallRiichi(List<TilePivot> tiles)
        {
            if (tiles.Count > 0 && MessageBox.Show("Declare riichi ?", WINDOW_TITLE, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                InvokeOverlay("Riichi", GamePivot.HUMAN_INDEX);
                Tuple<string, int> restrictResult =  RestrictDiscardWithTilesSelection(tiles.ToDictionary(t => t, t => false), BtnRiichiChoice_Click);
                return new Tuple<bool, string, int>(true, restrictResult.Item1, restrictResult.Item2);
            }
            return new Tuple<bool, string, int>(false, null, -1);
        }

        // Checks a tsumo call for the human player.
        private bool HumanCallTsumo(bool isKanCompensation)
        {
            if (_game.Round.CanCallTsumo(isKanCompensation))
            {
                if (Properties.Settings.Default.AutoCallMahjong
                    || MessageBox.Show("Call tsumo ?", WINDOW_TITLE, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    InvokeOverlay("Tsumo", GamePivot.HUMAN_INDEX);
                    return true;
                }
            }
            return false;
        }

        // Checks a ron call for the human player.
        private bool HumanCallRon()
        {
            if (_game.Round.CanCallRon(GamePivot.HUMAN_INDEX))
            {
                if (Properties.Settings.Default.AutoCallMahjong
                    || MessageBox.Show("Call ron ?", WINDOW_TITLE, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    InvokeOverlay("Ron", GamePivot.HUMAN_INDEX);
                    return true;
                }
            }
            return false;
        }

        // Inner process kan call.
        private void HumanKanCallProcess(TilePivot tile, int? previousPlayerIndex)
        {
            TilePivot compensationTile = _game.Round.CallKan(GamePivot.HUMAN_INDEX, tile);
            InvokeOverlay("Kan", GamePivot.HUMAN_INDEX);
            if (CheckCallRonForEveryone())
            {
                _game.Round.UndoPickCompensationTile();
                NewRound(_game.Round.CurrentPlayerIndex);
            }
            else
            {
                CommonCallKan(previousPlayerIndex, compensationTile);
                if (HumanCallTsumo(true))
                {
                    NewRound(null);
                }
                else
                {
                    Tuple<bool, string, int> riichiResult = HumanCallRiichi(_game.Round.CanCallRiichi());
                    if (!riichiResult.Item1)
                    {
                        ActivateTimer(GetFirstAvailableDiscardButton());
                    }
                }
            }
        }

        #endregion Human decisions

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
                bool skipCurrentAction = (bool)evt.Argument;
                Tuple<int, TilePivot, int?> kanInProgress = null;
                AutoPlayResult result = new AutoPlayResult
                {
                    ChildrenIndex = -1,
                    EndOfRound = false,
                    PanelName = null,
                    RonPlayerId = null
                };
                while (true)
                {
                    while (_pauseAutoplay)
                    {
                        // Do nothing until the autoplay is restarted.
                    }

                    if (CheckCallRonForEveryone())
                    {
                        result.EndOfRound = true;
                        result.RonPlayerId = kanInProgress != null ? kanInProgress.Item1 : _game.Round.PreviousPlayerIndex;
                        if (kanInProgress != null)
                        {
                            _game.Round.UndoPickCompensationTile();
                        }
                        break;
                    }

                    if (kanInProgress != null)
                    {
                        CommonCallKan(kanInProgress.Item3, kanInProgress.Item2);
                    }

                    if (!skipCurrentAction && _game.Round.CanCallPonOrKan(GamePivot.HUMAN_INDEX))
                    {
                        break;
                    }

                    Tuple<int, TilePivot> opponentWithKanTilePick = _game.Round.IaManager.KanDecision(false);
                    if (opponentWithKanTilePick != null)
                    {
                        int previousPlayerIndex = _game.Round.PreviousPlayerIndex;
                        TilePivot compensationTile = OpponentBeginCallKan(opponentWithKanTilePick.Item1, opponentWithKanTilePick.Item2, false, false);
                        kanInProgress = new Tuple<int, TilePivot, int?>(opponentWithKanTilePick.Item1, compensationTile, previousPlayerIndex);
                        continue;
                    }

                    int opponentPlayerId = _game.Round.IaManager.PonDecision();
                    if (opponentPlayerId > -1)
                    {
                        PonCall(opponentPlayerId);
                        continue;
                    }

                    if (!skipCurrentAction && _game.Round.IsHumanPlayer && _game.Round.CanCallChii().Count > 0)
                    {
                        break;
                    }

                    Tuple<TilePivot, bool> chiiTilePick = _game.Round.IaManager.ChiiDecision();
                    if (chiiTilePick != null)
                    {
                        ChiiCall(chiiTilePick);
                        continue;
                    }

                    if (kanInProgress != null)
                    {
                        if (OpponentAfterPick(ref kanInProgress))
                        {
                            break;
                        }
                        continue;
                    }

                    if (_game.Round.IsWallExhaustion)
                    {
                        result.EndOfRound = true;
                        break;
                    }

                    if (_game.Round.IsHumanPlayer)
                    {
                        HumanAutoPlay(result);
                        break;
                    }
                    else
                    {
                        Pick();
                        if (OpponentAfterPick(ref kanInProgress))
                        {
                            result.EndOfRound = true;
                            break;
                        }
                    }
                }

                evt.Result = result;
            };
            _autoPlay.RunWorkerCompleted += delegate (object sender, RunWorkerCompletedEventArgs evt)
            {
                AutoPlayResult autoPlayResult = evt.Result as AutoPlayResult;
                if (autoPlayResult.EndOfRound)
                {
                    NewRound(autoPlayResult.RonPlayerId);
                }
                else
                {
                    RaiseButtonClickEvent(autoPlayResult.PanelName, autoPlayResult.ChildrenIndex);
                }
            };
        }

        // Proceeds to new round.
        private void NewRound(int? ronPlayerIndex)
        {
            EndOfRoundInformationsPivot endOfRoundInfos = _game.NextRound(ronPlayerIndex);
            new ScoreWindow(_game.Players.ToList(), endOfRoundInfos).ShowDialog();
            if (endOfRoundInfos.EndOfGame)
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
        private void RunAutoPlay(bool skipCurrentAction = false)
        {
            if (!_autoPlay.IsBusy)
            {
                _autoPlay.RunWorkerAsync(skipCurrentAction);
            }
        }

        // Checks ron call for every players.
        private bool CheckCallRonForEveryone()
        {
            bool humanCallRon = false;
            if (HumanCallRon())
            {
                humanCallRon = true;
            }

            List<int> opponentsCallRon = _game.Round.IaManager.RonDecision(humanCallRon);
            foreach (int opponentPlayerIndex in opponentsCallRon)
            {
                InvokeOverlay("Ron", opponentPlayerIndex);
            }

            return humanCallRon || opponentsCallRon.Count > 0;
        }

        // Proceeds to autoplay for human player.
        private void HumanAutoPlay(AutoPlayResult result)
        {
            Pick();

            if (HumanCallTsumo(false))
            {
                result.EndOfRound = true;
                return;
            }

            List<TilePivot> tilesRiichi = _game.Round.CanCallRiichi();
            if (tilesRiichi.Count > 0)
            {
                Dispatcher.Invoke(() =>
                {
                    Tuple<bool, string, int> riichiResult = HumanCallRiichi(tilesRiichi);
                    if (!riichiResult.Item1)
                    {
                        ActivateTimer(StpPickP0.Children[0] as Button);
                    }
                    else if (!string.IsNullOrWhiteSpace(riichiResult.Item2))
                    {
                        result.PanelName = riichiResult.Item2;
                        result.ChildrenIndex = riichiResult.Item3;
                    }
                });
            }
            else if (Properties.Settings.Default.AutoDiscardAfterRiichi && _game.Round.HumanCanAutoDiscard())
            {
                Thread.Sleep(((CpuSpeedPivot)Properties.Settings.Default.CpuSpeed).ParseSpeed());
                result.PanelName = "StpPickP0";
                result.ChildrenIndex = 0;
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    ActivateTimer(StpPickP0.Children[0] as Button);
                });
            }
        }

        // Restrict possible discards on the specified selection of tiles.
        private Tuple<string, int> RestrictDiscardWithTilesSelection(IDictionary<TilePivot, bool> tileChoices, RoutedEventHandler handler)
        {
            Tuple<string, int> result = new Tuple<string, int>(null, -1);

            SetActionButtonsVisibility();

            List<Button> buttons = StpHandP0.Children.OfType<Button>().ToList();
            if (StpPickP0.Children.Count > 0)
            {
                buttons.Add(StpPickP0.Children[0] as Button);
            }

            var clickableButtons = new List<Button>();
            foreach (TilePivot tileKey in tileChoices.Keys)
            {
                // Changes the event of every buttons concerned by the call...
                Button buttonClickable = buttons.First(b => b.Tag as TilePivot == tileKey);
                buttonClickable.Click += handler;
                buttonClickable.Click -= BtnDiscard_Click;
                if (handler == BtnChiiChoice_Click)
                {
                    buttonClickable.Tag = new Tuple<TilePivot, bool>(tileKey, tileChoices[tileKey]);
                }
                clickableButtons.Add(buttonClickable);
            }
            // ...and disables every buttons not concerned.
            buttons.Where(b => !clickableButtons.Contains(b)).All(b => { b.IsEnabled = false; return true; });

            if (clickableButtons.Count == 1)
            {
                // Only one possibility : initiates the auto-discard.
                int buttonIndexInHandPanel = StpHandP0.Children.IndexOf(clickableButtons[0]);
                if (buttonIndexInHandPanel >= 0)
                {
                    result = new Tuple<string, int>(nameof(StpHandP0), buttonIndexInHandPanel);
                }
                else
                {
                    result = new Tuple<string, int>(nameof(StpPickP0), StpPickP0.Children.IndexOf(clickableButtons[0]));
                }
            }
            else
            {
                ActivateTimer(clickableButtons[0]);
            }

            return result;
        }

        // Discard action (human or CPU).
        private void Discard(TilePivot tile)
        {
            if (_game.Round.Discard(tile))
            {
                if (!_game.Round.PreviousIsHumanPlayer)
                {
                    Thread.Sleep(((CpuSpeedPivot)Properties.Settings.Default.CpuSpeed).ParseSpeed());
                }

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
            if (_game.Round.CallChii(chiiTilePick.Item2 ? chiiTilePick.Item1.Number - 1 : chiiTilePick.Item1.Number))
            {
                InvokeOverlay("Chii", _game.Round.CurrentPlayerIndex);
                if (!_game.Round.IsHumanPlayer)
                {
                    Thread.Sleep(((CpuSpeedPivot)Properties.Settings.Default.CpuSpeed).ParseSpeed());
                }

                Dispatcher.Invoke(() =>
                {
                    PlayTickSound();
                    FillHandPanel(_game.Round.CurrentPlayerIndex);
                    FillCombinationStack(_game.Round.CurrentPlayerIndex);
                    FillDiscardPanel(_game.Round.PreviousPlayerIndex);
                    SetActionButtonsVisibility(cpuPlay: !_game.Round.IsHumanPlayer);
                    if (_game.Round.IsHumanPlayer)
                    {
                        ActivateTimer(GetFirstAvailableDiscardButton());
                    }
                    else
                    {
                        SetPlayersLed();
                    }
                });

                if (!_game.Round.IsHumanPlayer)
                {
                    Discard(_game.Round.IaManager.DiscardDecision());
                }
            }
        }

        // Pon call action (human or CPU).
        private void PonCall(int playerIndex)
        {
            // Note : this value is stored here because the call to "CallPon" makes it change.
            int previousPlayerIndex = _game.Round.PreviousPlayerIndex;
            bool isCpu = playerIndex != GamePivot.HUMAN_INDEX;

            if (_game.Round.CallPon(playerIndex))
            {
                InvokeOverlay("Pon", playerIndex);
                if (isCpu)
                {
                    Thread.Sleep(((CpuSpeedPivot)Properties.Settings.Default.CpuSpeed).ParseSpeed());
                }

                Dispatcher.Invoke(() =>
                {
                    if (isCpu)
                    {
                        SetPlayersLed();
                    }
                    PlayTickSound();
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
                    Discard(_game.Round.IaManager.DiscardDecision());
                }
            }
        }

        // Pick action (human or CPU).
        private void Pick()
        {
            TilePivot pick = _game.Round.Pick();
            Dispatcher.Invoke(() =>
            {
                SetPlayersLed();
                PlayTickSound();
                (FindName($"StpPickP{_game.Round.CurrentPlayerIndex}") as StackPanel).Children.Add(
                    pick.GenerateTileButton(
                        _game.Round.IsHumanPlayer ? BtnDiscard_Click : (RoutedEventHandler)null,
                        (AnglePivot)_game.Round.CurrentPlayerIndex,
                        !_game.Round.IsHumanPlayer && !Properties.Settings.Default.DebugMode
                    )
                );
                if (_game.Round.IsHumanPlayer)
                {
                    SetActionButtonsVisibility(preDiscard: true);
                }
            });
        }

        // Riichi call action (human or CPU).
        private void CallRiichi(TilePivot tile)
        {
            if (_game.Round.CallRiichi(tile))
            {
                if (!_game.Round.PreviousIsHumanPlayer)
                {
                    InvokeOverlay("Riichi", _game.Round.PreviousPlayerIndex);
                    Thread.Sleep(((CpuSpeedPivot)Properties.Settings.Default.CpuSpeed).ParseSpeed());
                }

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

        // Proceeds to call a kan for an opponent.
        private TilePivot OpponentBeginCallKan(int playerId, TilePivot kanTilePick, bool concealedKan, bool fromPreviousKan)
        {
            TilePivot compensationTile = _game.Round.CallKan(playerId, concealedKan ? kanTilePick : null);
            if (compensationTile != null)
            {
                InvokeOverlay("Kan", playerId);
                Thread.Sleep(((CpuSpeedPivot)Properties.Settings.Default.CpuSpeed).ParseSpeed());
                Dispatcher.Invoke(() =>
                {
                    if (!concealedKan)
                    {
                        SetPlayersLed();
                    }
                });
            }
            return compensationTile;
        }

        // Manages every possible moves for the current opponent after his pick.
        private bool OpponentAfterPick(ref Tuple<int, TilePivot, int?> kanInProgress)
        {
            if (_game.Round.IaManager.TsumoDecision(kanInProgress != null))
            {
                InvokeOverlay("Tsumo", _game.Round.CurrentPlayerIndex);
                return true;
            }

            Tuple<int, TilePivot> opponentWithKanTilePick = _game.Round.IaManager.KanDecision(true);
            if (opponentWithKanTilePick != null)
            {
                TilePivot compensationTile = OpponentBeginCallKan(_game.Round.CurrentPlayerIndex, opponentWithKanTilePick.Item2, true, kanInProgress != null);
                kanInProgress = new Tuple<int, TilePivot, int?>(_game.Round.CurrentPlayerIndex, compensationTile, null);
                return false;
            }

            kanInProgress = null;

            TilePivot riichiTile = _game.Round.IaManager.RiichiDecision();
            if (riichiTile != null)
            {
                CallRiichi(riichiTile);
                return false;
            }

            Discard(_game.Round.IaManager.DiscardDecision());
            return false;
        }

        #endregion General orchestration

        #region Graphic tools

        private void CommonCallKan(int? previousPlayerIndex, TilePivot compensationTile)
        {
            Dispatcher.Invoke(() =>
            {
                PlayTickSound();
                FillHandPanel(_game.Round.CurrentPlayerIndex, compensationTile);
                (FindName($"StpPickP{_game.Round.CurrentPlayerIndex}") as StackPanel).Children.Add(
                    compensationTile.GenerateTileButton(
                        _game.Round.IsHumanPlayer ? BtnDiscard_Click : (RoutedEventHandler)null,
                        (AnglePivot)_game.Round.CurrentPlayerIndex,
                        !_game.Round.IsHumanPlayer && !Properties.Settings.Default.DebugMode
                    )
                );
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
        private void OnNotifyWallCount(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                LblWallTilesLeft.Content = _game.Round.WallTiles.Count;
                if (_game.Round.WallTiles.Count <= 4)
                {
                    LblWallTilesLeft.Foreground = System.Windows.Media.Brushes.Red;
                }
            });
        }

        // Gets the first button for a discardable tile.
        private Button GetFirstAvailableDiscardButton()
        {
            return (FindName($"StpHandP{GamePivot.HUMAN_INDEX}") as StackPanel)
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
            if (playerIndex != GamePivot.HUMAN_INDEX)
            {
                Thread.Sleep(((CpuSpeedPivot)Properties.Settings.Default.CpuSpeed).ParseSpeed());
            }
        }

        // Fix dimensions of the window and every panels (when it's required).
        private void FixWindowDimensions()
        {
            Title = WINDOW_TITLE;

            Cod0.Width = new GridLength(GraphicTools.TILE_HEIGHT + GraphicTools.DEFAULT_TILE_MARGIN);
            Cod1.Width = new GridLength(GraphicTools.TILE_HEIGHT + GraphicTools.DEFAULT_TILE_MARGIN);
            Cod2.Width = new GridLength((GraphicTools.TILE_HEIGHT * 3) + (GraphicTools.DEFAULT_TILE_MARGIN * 2));
            Cod4.Width = new GridLength((GraphicTools.TILE_HEIGHT * 3) + (GraphicTools.DEFAULT_TILE_MARGIN * 2));
            Cod5.Width = new GridLength(GraphicTools.TILE_HEIGHT + GraphicTools.DEFAULT_TILE_MARGIN);
            Cod6.Width = new GridLength(GraphicTools.TILE_HEIGHT + GraphicTools.DEFAULT_TILE_MARGIN);

            Rod0.Height = new GridLength(GraphicTools.TILE_HEIGHT + GraphicTools.DEFAULT_TILE_MARGIN);
            Rod1.Height = new GridLength(GraphicTools.TILE_HEIGHT + GraphicTools.DEFAULT_TILE_MARGIN);
            Rod2.Height = new GridLength((GraphicTools.TILE_HEIGHT * 3) + (GraphicTools.DEFAULT_TILE_MARGIN * 2));
            Rod4.Height = new GridLength((GraphicTools.TILE_HEIGHT * 3) + (GraphicTools.DEFAULT_TILE_MARGIN * 2));
            Rod5.Height = new GridLength(GraphicTools.TILE_HEIGHT + GraphicTools.DEFAULT_TILE_MARGIN);
            Rod6.Height = new GridLength(GraphicTools.TILE_HEIGHT + GraphicTools.DEFAULT_TILE_MARGIN);

            for (int i = 0; i < _game.Players.Count; i++)
            {
                for (int j = 1; j <= 3; j++)
                {
                    StackPanel panel = FindName($"StpP{i}Discard{j}") as StackPanel;
                    if (i % 2 == 0)
                    {
                        panel.Height = GraphicTools.TILE_HEIGHT + (0.5 * GraphicTools.DEFAULT_TILE_MARGIN);
                    }
                    else
                    {
                        panel.Width = GraphicTools.TILE_HEIGHT + (0.5 * GraphicTools.DEFAULT_TILE_MARGIN);
                    }
                }
            }
        }

        // Clears and refills the hand panel of the specified player index.
        private void FillHandPanel(int pIndex, TilePivot excludedTile = null)
        {
            bool isHuman = pIndex == GamePivot.HUMAN_INDEX;

            StackPanel panel = FindName($"StpHandP{pIndex}") as StackPanel;

            (FindName($"StpPickP{pIndex}") as StackPanel).Children.Clear();

            panel.Children.Clear();
            foreach (TilePivot tile in _game.Round.Hands.ElementAt(pIndex).ConcealedTiles)
            {
                if (excludedTile == null || !ReferenceEquals(excludedTile, tile))
                {
                    panel.Children.Add(tile.GenerateTileButton(isHuman && !_game.Round.IsRiichi(pIndex) ?
                        BtnDiscard_Click : (RoutedEventHandler)null, (AnglePivot)pIndex, !isHuman && !Properties.Settings.Default.DebugMode));
                }
            }
        }

        // Resets and refills every panels at a new round.
        private void NewRoundRefresh()
        {
            _game.Round.NotifyWallCount += OnNotifyWallCount;
            OnNotifyWallCount(null, null);

            StpDoras.SetDorasPanel(_game.Round.DoraIndicatorTiles, _game.Round.VisibleDorasCount);
            LblDominantWind.Content = _game.DominantWind.ToWindDisplay();
            LblEastTurnCount.Content = _game.EastRank;
            for (int pIndex = 0; pIndex < _game.Players.Count; pIndex++)
            {
                (FindName($"StpCombosP{pIndex}") as StackPanel).Children.Clear();
                FillHandPanel(pIndex);
                FillDiscardPanel(pIndex);
                (FindName($"LblWindP{pIndex}") as Label).Content = _game.GetPlayerCurrentWind(pIndex).ToString();
                (FindName($"LblNameP{pIndex}") as Label).Content = _game.Players.ElementAt(pIndex).Name;
                (FindName($"LblPointsP{pIndex}") as Label).Content = $"{_game.Players.ElementAt(pIndex).Points / 1000}k";
            }
            SetPlayersLed();
            SetActionButtonsVisibility(preDiscard: true);
        }

        // Resets the LED associated to each player.
        private void SetPlayersLed()
        {
            for (int pIndex = 0; pIndex < _game.Players.Count; pIndex++)
            {
                (FindName($"ImgLedP{pIndex}") as Image).Source =
                    (pIndex == _game.Round.CurrentPlayerIndex ? Properties.Resources.ledgreen : Properties.Resources.ledred).ToBitmapImage();
            }
        }

        // Rebuilds the discard panel of the specified player.
        private void FillDiscardPanel(int pIndex)
        {
            for (int r = 1; r <= 3; r++)
            {
                (FindName($"StpP{pIndex}Discard{r}") as StackPanel).Children.Clear();
            }

            bool reversed = pIndex == 1 || pIndex == 2;

            int i = 0;
            foreach (TilePivot tile in _game.Round.Discards.ElementAt(pIndex))
            {
                StackPanel panel = FindName($"StpP{pIndex}Discard{(i < 6 ? 1 : (i < 12 ? 2 : 3))}") as StackPanel;
                AnglePivot angle = (AnglePivot)pIndex;
                if (_game.Round.IsRiichiRank(pIndex, i))
                {
                    angle = (AnglePivot)pIndex.RelativePlayerIndex(1);
                }
                if (reversed)
                {
                    panel.Children.Insert(0, tile.GenerateTileButton(angle: angle));
                }
                else
                {
                    panel.Children.Add(tile.GenerateTileButton(angle: angle));
                }
                i++;
            }
        }

        // Adds to the player stack its last combination.
        private void FillCombinationStack(int pIndex)
        {
            StackPanel panel = FindName($"StpCombosP{pIndex}") as StackPanel;

            panel.Children.Clear();
            foreach (TileComboPivot combo in _game.Round.Hands.ElementAt(pIndex).DeclaredCombinations)
            {
                panel.Children.Add(CreateCombinationPanel(pIndex, combo));
            }
        }

        // Creates a panel for the specified combination.
        private StackPanel CreateCombinationPanel(int pIndex, TileComboPivot combo)
        {
            StackPanel panel = new StackPanel
            {
                Orientation = (pIndex == 0 || pIndex == 2 ? Orientation.Horizontal : Orientation.Vertical)
            };

            WindPivot pWind = _game.GetPlayerCurrentWind(pIndex);

            int i = 0;
            List<Tuple<TilePivot, bool>> tileTuples = combo.GetSortedTilesForDisplay(pWind);
            if (pIndex > 0 && pIndex < 3)
            {
                tileTuples.Reverse();
            }

            foreach (Tuple<TilePivot, bool> tileTuple in tileTuples)
            {
                panel.Children.Add(tileTuple.Item1.GenerateTileButton(null,
                    (AnglePivot)(tileTuple.Item2 ? pIndex.RelativePlayerIndex(1) : pIndex),
                    combo.IsConcealedDisplay(i)));
                i++;
            }

            return panel;
        }

        // Sets the Visibility property of every action buttons
        private void SetActionButtonsVisibility(bool preDiscard = false, bool cpuPlay = false)
        {
            // Default behavior.
            BtnChii.Visibility = Visibility.Collapsed;
            BtnPon.Visibility = Visibility.Collapsed;
            BtnKan.Visibility = Visibility.Collapsed;

            if (preDiscard)
            {
                // When the player has 14 tiles and need to discard
                // A kan call might be possible
                BtnChii.Visibility = Visibility.Collapsed;
                BtnPon.Visibility = Visibility.Collapsed;
                BtnKan.Visibility = _game.Round.CanCallKan(GamePivot.HUMAN_INDEX).Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (cpuPlay)
            {
                // When the CPU is playing
                // Or it's player's turn but he has not pick yet
                BtnChii.Visibility = _game.Round.IsHumanPlayer && _game.Round.CanCallChii().Keys.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                BtnPon.Visibility = _game.Round.CanCallPon(GamePivot.HUMAN_INDEX) ? Visibility.Visible : Visibility.Collapsed;
                BtnKan.Visibility = _game.Round.CanCallKan(GamePivot.HUMAN_INDEX).Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            if (BtnChii.Visibility == Visibility.Visible
                || BtnPon.Visibility == Visibility.Visible
                || BtnKan.Visibility == Visibility.Visible)
            {
                ActivateTimer(null);
            }
        }

        #endregion Graphic tools

        #region Other methods

        // Raises the button click event, from the panel specified at the index (of children) specified.
        private void RaiseButtonClickEvent(string panelName, int childrenIndex)
        {
            if (!string.IsNullOrWhiteSpace(panelName))
            {
                ((FindName(panelName) as StackPanel).Children[childrenIndex] as Button).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
        }

        // Checks if the button clicked was ready.
        private bool IsCurrentlyClickable(RoutedEventArgs e)
        {
            bool isCurrentlyClickable = !_autoPlay.IsBusy;

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
                            Window_MouseDoubleClick(null, null);
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
            ChronoPivot chronoValue = (ChronoPivot)Properties.Settings.Default.ChronoSpeed;
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

        // Plays the tick sound if sounds activated.
        private void PlayTickSound()
        {
            if (Properties.Settings.Default.PlaySounds)
            {
                _tickSound.Play();
            }
        }

        // Apply the CPU speed stored in configuration to the storyboard managing the overlay visibility.
        private void ApplyConfigurationToOverlayStoryboard()
        {
            (_overlayStoryboard.Children.Last() as ObjectAnimationUsingKeyFrames).KeyFrames[1].KeyTime =
                KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0, 0, ((CpuSpeedPivot)Properties.Settings.Default.CpuSpeed).ParseSpeed()));
        }

        #endregion Other methods
    }
}
