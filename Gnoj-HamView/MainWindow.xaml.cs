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
        private readonly int _cpuSpeedMs;
        private readonly bool _autoTsumoRon;
        private readonly bool _riichiAutoDiscard;
        private readonly bool _debugMode;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="playerName">Human player name.</param>
        /// <param name="pointRule">Indicates the initial points count for every players.</param>
        /// <param name="useRedDoras">Indicates if red doras should be used.</param>
        /// <param name="cpuSpeed">CPU speed.</param>
        /// <param name="autoTsumoRon">Auto call for tsumo and ron.</param>
        /// <param name="riichiAutoDiscard">Auto-discard when riichi.</param>
        /// <param name="debugMode"><c>True</c> to display the opponent tiles.</param>
        /// <param name="sortedDraw"><c>True</c> to not randomize the tile draw.</param>
        public MainWindow(string playerName, InitialPointsRulePivot pointRule, bool useRedDoras, CpuSpeed cpuSpeed, bool autoTsumoRon,
            bool riichiAutoDiscard, bool debugMode, bool sortedDraw)
        {
            InitializeComponent();
            
            _game = new GamePivot(playerName, pointRule, useRedDoras, sortedDraw);
            _cpuSpeedMs = Convert.ToInt32(cpuSpeed.ToString().Replace("S", string.Empty));
            _autoTsumoRon = autoTsumoRon;
            _riichiAutoDiscard = riichiAutoDiscard;
            _debugMode = debugMode;

            FixWindowDimensions();

            NewRoundRefresh();

            ContentRendered += delegate(object sender, EventArgs evt)
            {
                AutoPlayAsync();
            };
        }

        #region Window events

        private void BtnDiscard_Click(object sender, RoutedEventArgs e)
        {
            if (_game.Round.Discard((sender as Button).Tag as TilePivot))
            {
                FillHandPanel(_game.Round.PreviousPlayerIndex);
                FillDiscardPanel(_game.Round.PreviousPlayerIndex);
                SetActionButtonsVisibility();
                if (CheckRonForEveryPlayer(_game.Round.PreviousPlayerIndex))
                {
                    NewRound(_game.Round.PreviousPlayerIndex);
                }
                else
                {
                    AutoPlayAsync();
                }
            }
        }

        private void BtnChiiChoice_Click(object sender, RoutedEventArgs e)
        {
            KeyValuePair<TilePivot, bool> tag = (KeyValuePair<TilePivot, bool>)((sender as Button).Tag);

            if (_game.Round.CallChii(GamePivot.HUMAN_INDEX, tag.Value ? tag.Key.Number - 1 : tag.Key.Number))
            {
                FillHandPanel(_game.Round.CurrentPlayerIndex);
                AddLatestCombinationToStack(GamePivot.HUMAN_INDEX);
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
                    FillHandPanel(GamePivot.HUMAN_INDEX);
                    AddLatestCombinationToStack(GamePivot.HUMAN_INDEX);
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

        #region Private methods

        // Checks ron possibilities for every players except the one specified.
        private bool CheckRonForEveryPlayer(int onPlayerId)
        {
            var callingRonPlayers = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                if (i != onPlayerId)
                {
                    if (_game.Round.CanCallRon(i))
                    {
                        callingRonPlayers.Add(i);
                    }
                }
            }
            return callingRonPlayers.Any(i => TsumoOrRonCallManagement(i, true));
        }

        // Manages riichii call opportunities.
        private void RiichiCallManagement(List<TilePivot> tiles)
        {
            if (tiles.Count > 0)
            {
                MessageBoxResult mbRes = MessageBox.Show("Declare riichi ?", WINDOW_TITLE, MessageBoxButton.YesNo);
                if (mbRes == MessageBoxResult.Yes)
                {
                    RestrictDiscardWithTilesSelection(tiles.ToDictionary(t => t, t => false), BtnRiichiChoice_Click);
                }
            }
        }

        // Manages ron and tsumo call opportunities.
        private bool TsumoOrRonCallManagement(int playerIndex, bool ron)
        {
            return _game.Round.Hands.ElementAt(playerIndex).IsComplete && (
                _autoTsumoRon
                || playerIndex != GamePivot.HUMAN_INDEX
                || MessageBox.Show($"Declare {(ron ? "ron" : "tsumo")} ?", WINDOW_TITLE, MessageBoxButton.YesNo) == MessageBoxResult.Yes
            );
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

        // Inner process kan call.
        private void InnerKanCallProcess(TilePivot tile, int? previousPlayerIndex)
        {
            TilePivot compensationTile = _game.Round.CallKan(GamePivot.HUMAN_INDEX, tile);
            if (compensationTile == null)
            {
                throw new NotImplementedException();
            }
            else if (CheckRonForEveryPlayer(_game.Round.CurrentPlayerIndex))
            {
                _game.Round.UndoPickCompensationTile();
                NewRound(_game.Round.CurrentPlayerIndex);
            }
            else
            {
                FillHandPanel(GamePivot.HUMAN_INDEX, compensationTile);
                StpPickP0.Children.Add(compensationTile.GenerateTileButton(BtnDiscard_Click));
                if (previousPlayerIndex.HasValue)
                {
                    FillDiscardPanel(previousPlayerIndex.Value);
                }
                AddLatestCombinationToStack(GamePivot.HUMAN_INDEX);
                SetActionButtonsVisibility(preDiscard: true);
                StpDoras.SetDorasPanel(_game.Round.DoraIndicatorTiles, _game.Round.VisibleDorasCount);
                _game.Round.CanCallTsumo(false);
                if (TsumoOrRonCallManagement(GamePivot.HUMAN_INDEX, false))
                {
                    NewRound(null);
                }
                else
                {
                    RiichiCallManagement(_game.Round.CanCallRiichi(GamePivot.HUMAN_INDEX));
                }
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

            if (isHuman)
            {
                StpPickP0.Children.Clear();
            }

            panel.Children.Clear();
            foreach (TilePivot tile in _game.Round.Hands.ElementAt(pIndex).ConcealedTiles)
            {
                if (excludedTile == null || !ReferenceEquals(excludedTile, tile))
                {
                    panel.Children.Add(tile.GenerateTileButton(isHuman && !_game.Round.IsRiichi(pIndex) ?
                        BtnDiscard_Click : (RoutedEventHandler)null, (Angle)pIndex, !isHuman && !_debugMode));
                }
            }
        }

        // Resets and refills every panels at a new round.
        private void NewRoundRefresh()
        {
            StpDoras.SetDorasPanel(_game.Round.DoraIndicatorTiles, _game.Round.VisibleDorasCount);
            LblDominantWind.Content = _game.DominantWind.ToString().First();
            LblEastTurnCount.Content = _game.EastRank;
            for (int pIndex = 0; pIndex < _game.Players.Count; pIndex++)
            {
                (FindName($"StpCombosP{pIndex}") as StackPanel).Children.Clear();
                FillHandPanel(pIndex);
                FillDiscardPanel(pIndex);
                (FindName($"LblWindP{pIndex}") as Label).Content = _game.GetPlayerCurrentWind(pIndex).ToString();
                (FindName($"LblNameP{pIndex}") as Label).Content = _game.Players.ElementAt(pIndex).Name;
                (FindName($"LblPointsP{pIndex}") as Label).Content = _game.Players.ElementAt(pIndex).Points;
            }
            SetPlayersLed();
            SetActionButtonsVisibility(preDiscard: true);
            _game.Round.CanCallTsumo(false);
            if (TsumoOrRonCallManagement(_game.Round.CurrentPlayerIndex, false))
            {
                NewRound(null);
            }
            else
            {
                RiichiCallManagement(_game.Round.CanCallRiichi(GamePivot.HUMAN_INDEX));
            }
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
                Angle angle = (Angle)pIndex;
                if (_game.Round.IsRiichiRank(pIndex, i))
                {
                    angle = (Angle)pIndex.RelativePlayerIndex(1);
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

        // Adds to the player stack its last combination.
        private void AddLatestCombinationToStack(int pIndex)
        {
            StackPanel panel = FindName($"StpCombosP{pIndex}") as StackPanel;

            TileComboPivot combo = _game.Round.Hands.ElementAt(pIndex).DeclaredCombinations.Last();

            panel.Children.Add(CreateCombinationPanel(pIndex, combo));
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
            foreach (KeyValuePair<TilePivot, bool> tileKvp in combo.GetSortedTilesForDisplay(pWind))
            {
                panel.Children.Add(tileKvp.Key.GenerateTileButton(null,
                    (Angle)(tileKvp.Value ? pIndex.RelativePlayerIndex(1): pIndex),
                    combo.IsConcealedDisplay(i)));
                i++;
            }

            return panel;
        }

        // Auto-play the round while there's no reason for the player to interact; asynchronous.
        private async void AutoPlayAsync(bool skipCurrentAction = false)
        {
            Tuple<bool, int?> result = await Task.Run(() =>
            {
                while (_game.Round.IsCpuSkippable(skipCurrentAction))
                {
                    Tuple<bool, int?> endAction = CpuTurnAutoPlay();
                    if (endAction.Item1)
                    {
                        return endAction;
                    }
                }
                if (_game.Round.IsHumanSkippable(skipCurrentAction))
                {
                    return HumanTurnAutoPlay();
                }
                else if (_game.Round.IsWallExhaustion)
                {
                    return new Tuple<bool, int?>(true, null);
                }

                return new Tuple<bool, int?>(false, null);
            });

            if (result.Item1)
            {
                NewRound(result.Item2);
            }
        }
        
        // Auto-play when it's CPU turn
        private Tuple<bool, int?> CpuTurnAutoPlay()
        {
            Tuple<bool, int?> endAction = new Tuple<bool, int?>(false, null);

            Dispatcher.Invoke(SetPlayersLed);
            Thread.Sleep(_cpuSpeedMs);
            TilePivot pick = _game.Round.Pick();
            if (pick != null)
            {
                if (_game.Round.CanCallTsumo(false))
                {
                    if (TsumoOrRonCallManagement(_game.Round.CurrentPlayerIndex, false))
                    {
                        endAction = new Tuple<bool, int?>(true, null);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                else
                {
                    _game.Round.RandomDiscard();
                    Dispatcher.Invoke(() =>
                    {
                        FillDiscardPanel(_game.Round.PreviousPlayerIndex);
                        FillHandPanel(_game.Round.PreviousPlayerIndex);
                        SetActionButtonsVisibility(cpuPlay: true);
                    });
                    if (CheckRonForEveryPlayer(_game.Round.PreviousPlayerIndex))
                    {
                        endAction = new Tuple<bool, int?>(true, _game.Round.PreviousPlayerIndex);
                    }
                }
            }
            else if (!_game.Round.IsWallExhaustion)
            {
                throw new NotImplementedException();
            }

            return endAction;
        }

        // Auto-play when it's human turn
        private Tuple<bool, int?> HumanTurnAutoPlay()
        {
            Tuple<bool, int?> endAction = new Tuple<bool, int?>(false, null);

            Dispatcher.Invoke(SetPlayersLed);
            TilePivot pick = _game.Round.Pick();
            if (pick == null)
            {
                if (!_game.Round.IsWallExhaustion)
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                List<TilePivot> tilesRiichi = _game.Round.CanCallRiichi(GamePivot.HUMAN_INDEX);
                _game.Round.CanCallTsumo(false);
                Dispatcher.Invoke(() =>
                {
                    StpPickP0.Children.Add(pick.GenerateTileButton(BtnDiscard_Click));
                    SetActionButtonsVisibility(preDiscard: true);
                });
                if (TsumoOrRonCallManagement(GamePivot.HUMAN_INDEX, false))
                {
                    endAction = new Tuple<bool, int?>(true, null);
                }
                else if (tilesRiichi.Count > 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        RiichiCallManagement(tilesRiichi);
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

            return endAction;
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

        #endregion Private methods
    }
}
