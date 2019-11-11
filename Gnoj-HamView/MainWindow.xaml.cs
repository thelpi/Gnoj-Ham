using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Gnoj_Ham;

namespace Gnoj_HamView
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int TILE_WIDTH = 45;
        private const int TILE_HEIGHT = 60;
        private const int DEFAULT_TILE_MARGIN = 10;
        private const string WINDOW_TITLE = "Gnoj-Ham";
        private const string CONCEALED_TILE_RSC_NAME = "concealed";

        private readonly GamePivot _game;
        private readonly int _cpuSpeedMs;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="playerName">Human player name.</param>
        /// <param name="pointRule">Indicates the initial points count for every players.</param>
        /// <param name="useRedDoras">Indicates if red doras should be used.</param>
        /// <param name="cpuSpeed">CPU speed.</param>
        public MainWindow(string playerName, InitialPointsRulePivot pointRule, bool useRedDoras, CpuSpeed cpuSpeed)
        {
            InitializeComponent();
            
            _game = new GamePivot(playerName, pointRule, useRedDoras);
            _cpuSpeedMs = Convert.ToInt32(cpuSpeed.ToString().Replace("S", string.Empty));

            FixWindowDimensions();

            NewRoundRefresh();

            ContentRendered += delegate(object sender, EventArgs evt)
            {
                AutoSkip();
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
                AutoSkip();
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
                InnerKanOrChiiTileSelection(tileChoices, BtnChiiChoice_Click);
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
                    InnerKanOrChiiTileSelection(kanTiles.ToDictionary(t => t, t => false), BtnKanChoice_Click);
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

        private void BtnRiichi_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BtnTsumo_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BtnRon_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (BtnPon.Visibility == Visibility.Visible
                || BtnChii.Visibility == Visibility.Visible
                || BtnKan.Visibility == Visibility.Visible)
            {
                AutoSkip(true);
            }
        }

        #endregion Window events

        #region Private methods

        private void TsumoManagement(bool isKanCompensation)
        {
            List<YakuPivot> yakus = _game.Round.CanCallTsumo(GamePivot.HUMAN_INDEX, isKanCompensation);
            if (yakus.Count > 0)
            {
                var r = MessageBox.Show("Declare tsumo ?", WINDOW_TITLE, MessageBoxButton.YesNo);
                if (r == MessageBoxResult.Yes)
                {
                    NewRound(GamePivot.HUMAN_INDEX, yakus);
                }
            }
        }

        // Inner process for kan and chii tile selection.
        private void InnerKanOrChiiTileSelection(IDictionary<TilePivot, bool> tileChoices, RoutedEventHandler handler)
        {
            SetActionButtonsVisibility();

            List<Button> buttons = StpHandP0.Children.OfType<Button>().ToList();
            if (StpPickP0.Children.Count > 0)
            {
                buttons.Add(StpPickP0.Children[0] as Button);
            }

            var clickableButtons = new List<Button>();
            foreach (TilePivot tile in tileChoices.Keys)
            {
                // Changes the event of every buttons concerned by the chii call...
                Button buttonClickable = buttons.First(b => b.Tag as TilePivot == tile);
                buttonClickable.Click += handler;
                buttonClickable.Click -= BtnDiscard_Click;
                if (handler == BtnChiiChoice_Click)
                {
                    buttonClickable.Tag = new KeyValuePair<TilePivot, bool>(tile, tileChoices[tile]);
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
            else
            {
                FillHandPanel(GamePivot.HUMAN_INDEX, compensationTile);
                StpPickP0.Children.Add(GenerateTileButton(compensationTile, BtnDiscard_Click));
                if (previousPlayerIndex.HasValue)
                {
                    FillDiscardPanel(previousPlayerIndex.Value);
                }
                AddLatestCombinationToStack(GamePivot.HUMAN_INDEX);
                SetActionButtonsVisibility(preDiscard: true);
                TsumoManagement(true);
            }
        }

        // Fix dimensions of the window and every panels (when it's required).
        private void FixWindowDimensions()
        {
            Title = WINDOW_TITLE;

            Cod0.Width = new GridLength(TILE_HEIGHT + DEFAULT_TILE_MARGIN);
            Cod1.Width = new GridLength(TILE_HEIGHT + DEFAULT_TILE_MARGIN);
            Cod2.Width = new GridLength((TILE_HEIGHT * 3) + (DEFAULT_TILE_MARGIN * 2));
            Cod4.Width = new GridLength((TILE_HEIGHT * 3) + (DEFAULT_TILE_MARGIN * 2));
            Cod5.Width = new GridLength(TILE_HEIGHT + DEFAULT_TILE_MARGIN);
            Cod6.Width = new GridLength(TILE_HEIGHT + DEFAULT_TILE_MARGIN);

            Rod0.Height = new GridLength(TILE_HEIGHT + DEFAULT_TILE_MARGIN);
            Rod1.Height = new GridLength(TILE_HEIGHT + DEFAULT_TILE_MARGIN);
            Rod2.Height = new GridLength((TILE_HEIGHT * 3) + (DEFAULT_TILE_MARGIN * 2));
            Rod4.Height = new GridLength((TILE_HEIGHT * 3) + (DEFAULT_TILE_MARGIN * 2));
            Rod5.Height = new GridLength(TILE_HEIGHT + DEFAULT_TILE_MARGIN);
            Rod6.Height = new GridLength(TILE_HEIGHT + DEFAULT_TILE_MARGIN);

            for (int i = 0; i < _game.Players.Count; i++)
            {
                for (int j = 1; j <= 3; j++)
                {
                    StackPanel panel = FindName($"StpP{i}Discard{j}") as StackPanel;
                    if (i % 2 == 0)
                    {
                        panel.Height = TILE_HEIGHT + (0.5 * DEFAULT_TILE_MARGIN);
                    }
                    else
                    {
                        panel.Width = TILE_HEIGHT + (0.5 * DEFAULT_TILE_MARGIN);
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
                    panel.Children.Add(GenerateTileButton(tile, isHuman ? BtnDiscard_Click : (RoutedEventHandler)null, (Angle)pIndex, !isHuman));
                }
            }
        }

        // Resets and refills every panels at a new round.
        private void NewRoundRefresh()
        {
            SetDorasPanel();
            LblDominantWind.Content = _game.DominantWind.ToString().First();
            LblEastTurnCount.Content = _game.EastIndexTurnCount;
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
            TsumoManagement(false);
        }

        // resets the doras panel.
        private void SetDorasPanel()
        {
            StpDoras.Children.Clear();
            
            int concealedCount = 5 - (1 + (4 - _game.Round.CompensationTiles.Count));
            for (int i = 4; i >= 0; i--)
            {
                StpDoras.Children.Add(GenerateTileButton(_game.Round.DoraIndicatorTiles.ElementAt(i), concealed: 5 - concealedCount <= i));
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
                if (reversed)
                {
                    panel.Children.Insert(0, GenerateTileButton(tile, angle: (Angle)pIndex));
                }
                else
                {
                    panel.Children.Add(GenerateTileButton(tile, angle: (Angle)pIndex));
                }
                i++;
            }
        }

        // Proceeds to new round.
        private void NewRound(int playerIndex, List<YakuPivot> yakus)
        {
            if (yakus.Count == 0)
            {
                MessageBox.Show("End of round (Ryuukyoku)", WINDOW_TITLE);
            }
            else
            {
                MessageBox.Show($"End of round ({_game.Players.ElementAt(playerIndex).Name} wins with {string.Join(" + ", yakus.Select(y => y.Name))})", WINDOW_TITLE);
            }
            _game.NewRound();
            NewRoundRefresh();
            AutoSkip();
        }

        // Generates a button which represents a tile.
        private Button GenerateTileButton(TilePivot tile, RoutedEventHandler handler = null, Angle angle = Angle.A0, bool concealed = false)
        {
            string rscName = concealed ? CONCEALED_TILE_RSC_NAME : tile.ToString();

            System.Drawing.Bitmap tileBitmap = Properties.Resources.ResourceManager.GetObject(rscName) as System.Drawing.Bitmap;

            var button = new Button
            {
                Height = angle == Angle.A0 || angle == Angle.A180 ? TILE_HEIGHT : TILE_WIDTH,
                Width = angle == Angle.A0 || angle == Angle.A180 ? TILE_WIDTH : TILE_HEIGHT,
                Content = new Image
                {
                    Source = tileBitmap.ToBitmapImage(),
                    LayoutTransform = new RotateTransform(Convert.ToDouble(angle.ToString().Replace("A", string.Empty)))
                },
                Tag = tile
            };

            if (handler != null)
            {
                button.Click += handler;
            }

            return button;
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
                panel.Children.Add(GenerateTileButton(tileKvp.Key,
                    null,
                    (Angle)(tileKvp.Value ? RoundPivot.RelativePlayerIndex(pIndex, 1): pIndex),
                    combo.IsSquare && combo.IsConcealed && i > 0 && i < 3));
                i++;
            }

            return panel;
        }

        // Proceeds to skip CPU moves while human can't interact.
        private void AutoSkip(bool skipCurrentAction = false)
        {
            Task.Run(() =>
            {
                while (_game.Round.IsCpuSkippable(skipCurrentAction))
                {
                    skipCurrentAction = false;
                    Dispatcher.Invoke(SetPlayersLed);
                    Thread.Sleep(_cpuSpeedMs);
                    if (_game.Round.AutoPickAndDiscard())
                    {
                        Dispatcher.Invoke(() =>
                        {
                            FillDiscardPanel(_game.Round.PreviousPlayerIndex);
                            FillHandPanel(_game.Round.PreviousPlayerIndex);
                            SetActionButtonsVisibility(cpuPlay: true);
                        });
                    }
                    else if (!_game.Round.IsWallExhaustion)
                    {
                        throw new NotImplementedException();
                    }
                }
                if (_game.Round.IsHumanTurnBeforePick(skipCurrentAction))
                {
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
                        Dispatcher.Invoke(() =>
                        {
                            StpPickP0.Children.Add(GenerateTileButton(pick, BtnDiscard_Click));
                            SetActionButtonsVisibility(preDiscard: true);
                            TsumoManagement(false);
                        });
                    }
                }
                else if (_game.Round.IsWallExhaustion)
                {
                    Dispatcher.Invoke(() => NewRound(-1, new List<YakuPivot>()));
                }
            })
            .ContinueWith(task =>
            {
                // Without this code, no exception is thrown by the parallel task.
                if (task?.Exception?.InnerExceptions?.FirstOrDefault() != null)
                {
                    throw task.Exception.InnerExceptions.First();
                }
            });
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

        // Represents tile rotation (depending on the player).
        private enum Angle
        {
            A0,
            A270,
            A180,
            A90
        }
    }
}
