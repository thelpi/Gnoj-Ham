using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
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
        private int _cpuSpeedMs;
        private bool _autoTsumoRon;
        private bool _riichiAutoDiscard;
        private bool _debugMode;
        private bool _sounds;
        private bool _pauseAutoplay;
        private System.Media.SoundPlayer _tickSound;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="playerName">Human player name.</param>
        /// <param name="pointRule">Indicates the initial points count for every players.</param>
        /// <param name="endOfGameRule">The ruel to end a game.</param>
        /// <param name="useRedDoras">Indicates if red doras should be used.</param>
        /// <param name="cpuSpeed">CPU speed.</param>
        /// <param name="autoTsumoRon">Auto call for tsumo and ron.</param>
        /// <param name="riichiAutoDiscard">Auto-discard when riichi.</param>
        /// <param name="debugMode"><c>True</c> to display the opponent tiles.</param>
        /// <param name="sortedDraw"><c>True</c> to not randomize the tile draw.</param>
        /// <param name="useNagashiMangan"><c>True</c> to use the yaku 'Nagashi Mangan'.</param>
        /// <param name="useRenhou"><c>True</c> to use the yakuman 'Renhou'.</param>
        /// <param name="sounds"><c>True</c> to activate sounds.</param>
        public MainWindow(string playerName, InitialPointsRulePivot pointRule, EndOfGameRulePivot endOfGameRule, bool useRedDoras, CpuSpeedPivot cpuSpeed,
            bool autoTsumoRon, bool riichiAutoDiscard, bool debugMode, bool sortedDraw, bool useNagashiMangan, bool useRenhou, bool sounds)
        {
            InitializeComponent();
            
            _game = new GamePivot(playerName, pointRule, endOfGameRule, useRedDoras, sortedDraw, useNagashiMangan, useRenhou);
            _cpuSpeedMs = cpuSpeed.ParseSpeed();
            _autoTsumoRon = autoTsumoRon;
            _riichiAutoDiscard = riichiAutoDiscard;
            _debugMode = debugMode;
            _sounds = sounds;
            _tickSound = new System.Media.SoundPlayer(Properties.Resources.tick);

            FixWindowDimensions();

            NewRoundRefresh();

            ContentRendered += delegate(object sender, EventArgs evt)
            {
                AutoPlayAsync();
            };
        }

        #region Window events
        
        private void BtnConfiguration_Click(object sender, RoutedEventArgs e)
        {
            _pauseAutoplay = true;
            var configurationWindow = new IntroWindow(_game);
            configurationWindow.ShowDialog();
            _cpuSpeedMs = configurationWindow.CpuSpeed.ParseSpeed();
            _autoTsumoRon = configurationWindow.AutoTsumoRon;
            _riichiAutoDiscard = configurationWindow.RiichiAutoDiscard;
            _debugMode = configurationWindow.DebugMode;
            _sounds = configurationWindow.Sounds;
            _pauseAutoplay = false;
        }
        
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

        private void BtnDiscard_Click(object sender, RoutedEventArgs e)
        {
            TilePivot discard = (sender as Button).Tag as TilePivot;
            if (_game.Round.Discard(discard))
            {
                FillHandPanel(_game.Round.PreviousPlayerIndex);
                FillDiscardPanel(_game.Round.PreviousPlayerIndex);
                SetActionButtonsVisibility();
                AutoPlayAsync();
            }
        }

        private void BtnChiiChoice_Click(object sender, RoutedEventArgs e)
        {
            KeyValuePair<TilePivot, bool> tag = (KeyValuePair<TilePivot, bool>)((sender as Button).Tag);

            if (_game.Round.CallChii(GamePivot.HUMAN_INDEX, tag.Value ? tag.Key.Number - 1 : tag.Key.Number))
            {
                PlayTickSound();
                FillHandPanel(_game.Round.CurrentPlayerIndex);
                FillCombinationStack(GamePivot.HUMAN_INDEX);
                FillDiscardPanel(_game.Round.PreviousPlayerIndex);
                SetActionButtonsVisibility();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void BtnKanChoice_Click(object sender, RoutedEventArgs e)
        {
            InnerKanCallProcess((sender as Button).Tag as TilePivot, null);
        }

        private void BtnPon_Click(object sender, RoutedEventArgs e)
        {
            if (_game.Round.CanCallPon(GamePivot.HUMAN_INDEX))
            {
                // Note : this value is stored here because the call to "CallPon" makes it change.
                int previousPlayerIndex = _game.Round.PreviousPlayerIndex;
                if (!_game.Round.CallPon(GamePivot.HUMAN_INDEX))
                {
                    throw new NotImplementedException();
                }
                else
                {
                    PlayTickSound();
                    FillHandPanel(GamePivot.HUMAN_INDEX);
                    FillCombinationStack(GamePivot.HUMAN_INDEX);
                    FillDiscardPanel(previousPlayerIndex);
                    SetActionButtonsVisibility();
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void BtnChii_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<TilePivot, bool> tileChoices = _game.Round.CanCallChii(GamePivot.HUMAN_INDEX);

            if (tileChoices.Keys.Count > 0)
            {
                RestrictDiscardWithTilesSelection(tileChoices, BtnChiiChoice_Click);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void BtnKan_Click(object sender, RoutedEventArgs e)
        {
            List<TilePivot> kanTiles = _game.Round.CanCallKan(GamePivot.HUMAN_INDEX);
            if (kanTiles.Count > 0)
            {
                if (_game.Round.IsHumanPlayer)
                {
                    RestrictDiscardWithTilesSelection(kanTiles.ToDictionary(t => t, t => false), BtnKanChoice_Click);
                }
                else
                {
                    InnerKanCallProcess(null, _game.Round.PreviousPlayerIndex);
                }
            }
            else if (_game.Round.CompensationTiles.Count > 0)
            {
                throw new NotImplementedException();
            }
        }

        private void BtnRiichiChoice_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

            if (_game.Round.CallRiichi(GamePivot.HUMAN_INDEX, button.Tag as TilePivot))
            {
                FillHandPanel(_game.Round.PreviousPlayerIndex);
                FillDiscardPanel(_game.Round.PreviousPlayerIndex);
                SetActionButtonsVisibility();
                AutoPlayAsync();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (BtnPon.Visibility == Visibility.Visible
                || BtnChii.Visibility == Visibility.Visible
                || BtnKan.Visibility == Visibility.Visible)
            {
                AutoPlayAsync(true);
            }
            else if (StpPickP0.Children.Count > 0)
            {
                (StpPickP0.Children[0] as Button).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
        }

        #endregion Window events

        #region Human decisions

        // Manages riichii call opportunities.
        private void HumanCallRiichi(List<TilePivot> tiles)
        {
            if (tiles.Count > 0 && MessageBox.Show("Declare riichi ?", WINDOW_TITLE, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                RestrictDiscardWithTilesSelection(tiles.ToDictionary(t => t, t => false), BtnRiichiChoice_Click);
            }
        }

        // Checks a tsumo call for the human player.
        private bool HumanCallTsumo(bool isKanCompensation)
        {
            if (_game.Round.CanCallTsumo(isKanCompensation))
            {
                if (_autoTsumoRon || MessageBox.Show("Call tsumo ?", WINDOW_TITLE, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
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
                if (_autoTsumoRon || MessageBox.Show("Call ron ?", WINDOW_TITLE, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    return true;
                }
            }
            return false;
        }

        // Inner process kan call.
        private void InnerKanCallProcess(TilePivot tile, int? previousPlayerIndex)
        {
            TilePivot compensationTile = _game.Round.CallKan(GamePivot.HUMAN_INDEX, tile);
            if (compensationTile == null)
            {
                throw new NotImplementedException();
            }
            else if (CheckCallRonForEveryone())
            {
                _game.Round.UndoPickCompensationTile();
                NewRound(_game.Round.CurrentPlayerIndex);
            }
            else
            {
                PlayTickSound();
                FillHandPanel(GamePivot.HUMAN_INDEX, compensationTile);
                StpPickP0.Children.Add(compensationTile.GenerateTileButton(BtnDiscard_Click));
                if (previousPlayerIndex.HasValue)
                {
                    FillDiscardPanel(previousPlayerIndex.Value);
                }
                FillCombinationStack(GamePivot.HUMAN_INDEX);
                SetActionButtonsVisibility(preDiscard: true);
                StpDoras.SetDorasPanel(_game.Round.DoraIndicatorTiles, _game.Round.VisibleDorasCount);
                if (HumanCallTsumo(true))
                {
                    NewRound(null);
                }
                else
                {
                    HumanCallRiichi(_game.Round.CanCallRiichi(GamePivot.HUMAN_INDEX));
                }
            }
        }

        #endregion Human decisions

        #region General orchestration

        // Proceeds to new round.
        private void NewRound(int? ronPlayerIndex)
        {
            EndOfRoundInformationsPivot endOfRoundInfos = _game.NextRound(ronPlayerIndex);
            new ScoreWindow(_game.Players.ToList(), endOfRoundInfos).ShowDialog();
            if (endOfRoundInfos.EndOfGame)
            {
                Close();
            }
            NewRoundRefresh();
            AutoPlayAsync();
        }

        // Auto-play the round while there's no reason for the player to interact; asynchronous.
        private async void AutoPlayAsync(bool skipCurrentAction = false)
        {
            bool endOfRound = false;
            int? ronPlayerId = null;
            Tuple<int, TilePivot, int?> kanInProgress = null;

            await Task.Run(() =>
            {
                while (true)
                {
                    while (_pauseAutoplay)
                    {
                        // Do nothing until the autoplay is restarted.
                    }

                    if (CheckCallRonForEveryone())
                    {
                        endOfRound = true;
                        ronPlayerId = kanInProgress != null ? kanInProgress.Item1 : _game.Round.PreviousPlayerIndex;
                        if (kanInProgress != null)
                        {
                            _game.Round.UndoPickCompensationTile();
                        }
                        break;
                    }

                    if (kanInProgress != null)
                    {
                        OpponentContinueCallKan(kanInProgress.Item2, kanInProgress.Item3);
                    }

                    if (!skipCurrentAction && _game.Round.CanCallPonOrKan(GamePivot.HUMAN_INDEX))
                    {
                        break;
                    }

                    Tuple<int, TilePivot> opponentWithKanTilePick = _game.Round.IaManager.KanDecision(false);
                    if (opponentWithKanTilePick != null)
                    {
                        int currentPlayerId = _game.Round.CurrentPlayerIndex;
                        TilePivot compensationTile = OpponentBeginCallKan(opponentWithKanTilePick.Item1, opponentWithKanTilePick.Item2, false, false);
                        kanInProgress = new Tuple<int, TilePivot, int?>(opponentWithKanTilePick.Item1, compensationTile, currentPlayerId);
                        continue;
                    }

                    int opponentPlayerId = _game.Round.IaManager.PonDecision();
                    if (opponentPlayerId > -1)
                    {
                        OpponentCallPonAndDiscard(opponentPlayerId);
                        continue;
                    }

                    if (!skipCurrentAction && _game.Round.CanCallChii(GamePivot.HUMAN_INDEX).Count > 0)
                    {
                        break;
                    }

                    Tuple<TilePivot, bool> chiiTilePick = _game.Round.IaManager.ChiiDecision();
                    if (chiiTilePick != null)
                    {
                        OpponentCallChiiAndDiscard(chiiTilePick);
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
                        endOfRound = true;
                        break;
                    }

                    if (_game.Round.IsHumanPlayer)
                    {
                        HumanAutoPlay(out endOfRound);
                        break;
                    }
                    else
                    {
                        OpponentPick();
                        if (OpponentAfterPick(ref kanInProgress))
                        {
                            endOfRound = true;
                            break;
                        }
                    }
                }
            });

            if (endOfRound)
            {
                NewRound(ronPlayerId);
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
        private void HumanAutoPlay(out bool newRound)
        {
            newRound = false;

            TilePivot pick = _game.Round.Pick();

            Dispatcher.Invoke(() =>
            {
                SetPlayersLed();
                PlayTickSound();
                StpPickP0.Children.Add(pick.GenerateTileButton(BtnDiscard_Click));
                SetActionButtonsVisibility(preDiscard: true);
            });

            if (HumanCallTsumo(false))
            {
                newRound = true;
                return;
            }

            List<TilePivot> tilesRiichi = _game.Round.CanCallRiichi(GamePivot.HUMAN_INDEX);
            if (tilesRiichi.Count > 0)
            {
                Dispatcher.Invoke(() =>
                {
                    HumanCallRiichi(tilesRiichi);
                });
            }
            else if (_riichiAutoDiscard && _game.Round.HumanCanAutoDiscard())
            {
                Thread.Sleep(_cpuSpeedMs);
                Dispatcher.Invoke(() =>
                {
                    (StpPickP0.Children[0] as Button).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                });
            }
        }

        // Restrict possible discards on the specified selection of tiles.
        private void RestrictDiscardWithTilesSelection(IDictionary<TilePivot, bool> tileChoices, RoutedEventHandler handler)
        {
            SetActionButtonsVisibility();

            List<Button> buttons = StpHandP0.Children.OfType<Button>().ToList();
            if (StpPickP0.Children.Count > 0)
            {
                buttons.Add(StpPickP0.Children[0] as Button);
            }

            var clickableButtons = new List<Button>();
            foreach (KeyValuePair<TilePivot, bool> tileKvp in tileChoices)
            {
                // Changes the event of every buttons concerned by the call...
                Button buttonClickable = buttons.First(b => b.Tag as TilePivot == tileKvp.Key);
                buttonClickable.Click += handler;
                buttonClickable.Click -= BtnDiscard_Click;
                if (handler == BtnChiiChoice_Click)
                {
                    buttonClickable.Tag = tileKvp;
                }
                clickableButtons.Add(buttonClickable);
            }
            // ...and disables every buttons not concerned.
            buttons.Where(b => !clickableButtons.Contains(b)).All(b => { b.IsEnabled = false; return true; });

            if (clickableButtons.Count == 1)
            {
                // Only one possibility : proceeds to make the call.
                clickableButtons[0].RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
            else
            {
                // Otherwise, waits for the user choice.
            }
        }

        #endregion General orchestration

        #region CPU actions

        // Intermediate phase when an opponent call kan.
        private void OpponentContinueCallKan(TilePivot compensationTile, int? previousPlayerIndex)
        {
            Dispatcher.Invoke(() =>
            {
                PlayTickSound();
                FillHandPanel(_game.Round.CurrentPlayerIndex, compensationTile);
                (FindName($"StpPickP{_game.Round.CurrentPlayerIndex}") as StackPanel).Children.Add(compensationTile.GenerateTileButton(null, (AnglePivot)_game.Round.CurrentPlayerIndex, !_debugMode));
                if (previousPlayerIndex.HasValue)
                {
                    FillDiscardPanel(previousPlayerIndex.Value);
                }
                FillCombinationStack(_game.Round.CurrentPlayerIndex);
                SetActionButtonsVisibility(cpuPlay: true);
                StpDoras.SetDorasPanel(_game.Round.DoraIndicatorTiles, _game.Round.VisibleDorasCount);
            });
        }

        // Proceeds to pick for the current opponent.
        private void OpponentPick()
        {
            TilePivot pick = _game.Round.Pick();
            Dispatcher.Invoke(() =>
            {
                SetPlayersLed();
                PlayTickSound();
                int i = _game.Round.CurrentPlayerIndex;
                (FindName($"StpPickP{i}") as StackPanel).Children.Add(pick.GenerateTileButton(null, (AnglePivot)i, !_debugMode));
            });
        }

        // Proceeds to call chii then discard for the current opponent.
        private void OpponentCallChiiAndDiscard(Tuple<TilePivot, bool> chiiTilePick)
        {
            InvokeOverlay("Chii", _game.Round.CurrentPlayerIndex);

            _game.Round.CallChii(_game.Round.CurrentPlayerIndex, chiiTilePick.Item2 ? chiiTilePick.Item1.Number - 1 : chiiTilePick.Item1.Number);
            Thread.Sleep(_cpuSpeedMs);
            Dispatcher.Invoke(() =>
            {
                SetPlayersLed();
                PlayTickSound();
                FillHandPanel(_game.Round.CurrentPlayerIndex);
                FillCombinationStack(_game.Round.CurrentPlayerIndex);
                FillDiscardPanel(_game.Round.PreviousPlayerIndex);
                SetActionButtonsVisibility(cpuPlay: true);
            });
            OpponentDiscard();
        }

        // Proceeds to call pon then discard for an opponent.
        private void OpponentCallPonAndDiscard(int opponentPlayerId)
        {
            InvokeOverlay("Pon", opponentPlayerId);

            // Note : this value is stored here because the call to "CallPon" makes it change.
            int previousPlayerIndex = _game.Round.PreviousPlayerIndex;
            _game.Round.CallPon(opponentPlayerId);
            Thread.Sleep(_cpuSpeedMs);
            Dispatcher.Invoke(() =>
            {
                SetPlayersLed();
                PlayTickSound();
                FillHandPanel(opponentPlayerId);
                FillCombinationStack(opponentPlayerId);
                FillDiscardPanel(previousPlayerIndex);
                SetActionButtonsVisibility(cpuPlay: true);
            });
            OpponentDiscard();
        }

        // Proceeds to call a kan for an opponent.
        private TilePivot OpponentBeginCallKan(int playerId, TilePivot kanTilePick, bool concealedKan, bool fromPreviousKan)
        {
            InvokeOverlay("Kan", playerId);

            TilePivot compensationTile = _game.Round.CallKan(playerId, kanTilePick);
            Thread.Sleep(_cpuSpeedMs);
            Dispatcher.Invoke(() =>
            {
                if (!concealedKan)
                {
                    SetPlayersLed();
                }
            });
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
                OpponentCallRiichiAndDiscard(riichiTile);
                return false;
            }

            OpponentDiscard();
            return false;
        }

        // Proceeds to discard for the current opponent.
        private void OpponentDiscard()
        {
            if (_game.Round.Discard(_game.Round.IaManager.DiscardDecision()))
            {
                Thread.Sleep(_cpuSpeedMs);
                Dispatcher.Invoke(() =>
                {
                    FillHandPanel(_game.Round.PreviousPlayerIndex);
                    FillDiscardPanel(_game.Round.PreviousPlayerIndex);
                    SetActionButtonsVisibility(cpuPlay: true);
                });
            }
        }

        // Proceeds to call riichi for the current opponent.
        private void OpponentCallRiichiAndDiscard(TilePivot riichiTile)
        {
            InvokeOverlay("Riichi", _game.Round.CurrentPlayerIndex);

            _game.Round.CallRiichi(_game.Round.CurrentPlayerIndex, riichiTile);
            Thread.Sleep(_cpuSpeedMs);
            Dispatcher.Invoke(() =>
            {
                FillHandPanel(_game.Round.PreviousPlayerIndex);
                FillDiscardPanel(_game.Round.PreviousPlayerIndex);
                SetActionButtonsVisibility(cpuPlay: true);
            });
        }

        #endregion CPU actions

        #region Graphic tools

        // Displays the call overlay.
        private void InvokeOverlay(string callName, int playerIndex)
        {
            Dispatcher.Invoke(() =>
            {
                BtnOpponentCall.Content = $"{callName} !";
                BtnOpponentCall.HorizontalAlignment = playerIndex == 1 ? HorizontalAlignment.Right : (playerIndex == 3 ? HorizontalAlignment.Left : HorizontalAlignment.Center);
                BtnOpponentCall.VerticalAlignment = playerIndex == 2 ? VerticalAlignment.Top : VerticalAlignment.Center;
                BtnOpponentCall.Margin = new Thickness(playerIndex == 3 ? 20 : 0, playerIndex == 2 ? 20 : 0, playerIndex == 1 ? 20 : 0, 0);
                GrdOverlayCall.Visibility = Visibility.Visible;
            });
            Thread.Sleep(_cpuSpeedMs);
            Dispatcher.Invoke(() =>
            {
                GrdOverlayCall.Visibility = Visibility.Collapsed;
            });
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
                        BtnDiscard_Click : (RoutedEventHandler)null, (AnglePivot)pIndex, !isHuman && !_debugMode));
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
            List<KeyValuePair<TilePivot, bool>> tilesKvp = combo.GetSortedTilesForDisplay(pWind);
            if (pIndex > 0 && pIndex < 3)
            {
                tilesKvp.Reverse();
            }

            foreach (KeyValuePair<TilePivot, bool> tileKvp in tilesKvp)
            {
                panel.Children.Add(tileKvp.Key.GenerateTileButton(null,
                    (AnglePivot)(tileKvp.Value ? pIndex.RelativePlayerIndex(1) : pIndex),
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
                BtnChii.Visibility = _game.Round.CanCallChii(GamePivot.HUMAN_INDEX).Keys.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                BtnPon.Visibility = _game.Round.CanCallPon(GamePivot.HUMAN_INDEX) ? Visibility.Visible : Visibility.Collapsed;
                BtnKan.Visibility = _game.Round.CanCallKan(GamePivot.HUMAN_INDEX).Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // Plays the tick sound if sounds activated.
        private void PlayTickSound()
        {
            if (_sounds)
            {
                _tickSound.Play();
            }
        }

        #endregion Graphic tools
    }
}
