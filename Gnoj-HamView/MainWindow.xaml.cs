using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

        // TODO : customize.
        private const CpuSpeed CPU_SPEED = CpuSpeed.S200;

        private readonly GamePivot _game;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // TODO : first screen to personalize informations.
            _game = new GamePivot("Human", InitialPointsRulePivot.K25, true);

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
                BtnChii.Visibility = Visibility.Collapsed;
                BtnPon.Visibility = Visibility.Collapsed;
                BtnKan.Visibility = Visibility.Collapsed;

                if (!IsEndOfRoundByWallExhaustion())
                {
                    AutoSkip();
                }
            }
        }

        private void BtnChiiChoice_Click(object sender, RoutedEventArgs e)
        {
            KeyValuePair<TilePivot, bool> tag = (KeyValuePair<TilePivot, bool>)((sender as Button).Tag);

            if (_game.Round.CallChii(tag.Value ? tag.Key.Number - 1 : tag.Key.Number))
            {
                FillHandPanel(_game.Round.CurrentPlayerIndex);
                AddLatestCombinationToStack(GamePivot.HUMAN_INDEX);
                FillDiscardPanel(_game.Round.PreviousPlayerIndex);
                BtnChii.Visibility = Visibility.Collapsed;
                BtnPon.Visibility = Visibility.Collapsed;
                // TODO : is it possible to call kan after a first call ?
                BtnKan.Visibility = Visibility.Collapsed;
            }
            else
            {
                throw new NotImplementedException();
            }
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
                    BtnChii.Visibility = Visibility.Collapsed;
                    BtnPon.Visibility = Visibility.Collapsed;
                    // TODO : is it possible to call kan after a first call ?
                    BtnKan.Visibility = Visibility.Collapsed;
                }
            }
            else if (!IsEndOfRoundByWallExhaustion())
            {
                throw new NotImplementedException();
            }
        }

        private void BtnChii_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<TilePivot, bool> tileChoices = _game.Round.CanCallChii();

            if (tileChoices.Keys.Count > 0)
            {
                BtnChii.Visibility = Visibility.Collapsed;
                BtnPon.Visibility = Visibility.Collapsed;
                BtnKan.Visibility = Visibility.Collapsed;

                List<Button> buttons = StpHandP0.Children.OfType<Button>().ToList();

                var clickableButtons = new List<Button>();
                foreach (var tile in tileChoices.Keys)
                {
                    // Changes the event (and tag) of every buttons concerned by the chii call...
                    Button buttonClickable = buttons.First(b => b.Tag as TilePivot == tile);
                    buttonClickable.Click += BtnChiiChoice_Click;
                    buttonClickable.Tag = new KeyValuePair<TilePivot, bool>(tile, tileChoices[tile]);
                    clickableButtons.Add(buttonClickable);
                }
                // ...and disables every buttons not concerned.
                buttons.Where(b => !clickableButtons.Contains(b)).All(b => { b.IsEnabled = false; return true; });

                if (clickableButtons.Count == 1)
                {
                    // Only one possibility : proceeds to make the chii call.
                    clickableButtons[0].RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                }
                else
                {
                    // Otherwise, waits for the user choice.
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void BtnKan_Click(object sender, RoutedEventArgs e)
        {
            int canCount = _game.Round.CanCallKan(GamePivot.HUMAN_INDEX);
            if (canCount > 0)
            {
                if (_game.Round.IsHumanPlayer)
                {
                    if (canCount > 1)
                    {
                        // TODO : hardmode !
                    }
                    else
                    {
                        TilePivot compensationTile = _game.Round.CallKan(GamePivot.HUMAN_INDEX);
                        if (compensationTile == null)
                        {
                            throw new NotImplementedException();
                        }
                        else
                        {
                            FillHandPanel(GamePivot.HUMAN_INDEX, compensationTile);
                            StpPickP0.Children.Add(GenerateTileButton(compensationTile, BtnDiscard_Click));
                            AddLatestCombinationToStack(GamePivot.HUMAN_INDEX);
                            BtnChii.Visibility = Visibility.Collapsed;
                            BtnPon.Visibility = Visibility.Collapsed;
                            BtnKan.Visibility = _game.Round.CanCallKan(GamePivot.HUMAN_INDEX) > 0 ? Visibility.Visible : Visibility.Collapsed;
                        }
                    }
                }
                else
                {
                    // Note : this value is stored here because the call to "CallKan" makes it change.
                    int previousPlayerIndex = _game.Round.PreviousPlayerIndex;
                    TilePivot compensationTile = _game.Round.CallKan(GamePivot.HUMAN_INDEX);
                    if (compensationTile == null)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        FillHandPanel(GamePivot.HUMAN_INDEX, compensationTile);
                        StpPickP0.Children.Add(GenerateTileButton(compensationTile, BtnDiscard_Click));
                        AddLatestCombinationToStack(GamePivot.HUMAN_INDEX);
                        FillDiscardPanel(previousPlayerIndex);
                        BtnChii.Visibility = Visibility.Collapsed;
                        BtnPon.Visibility = Visibility.Collapsed;
                        BtnKan.Visibility = _game.Round.CanCallKan(GamePivot.HUMAN_INDEX) > 0 ? Visibility.Visible : Visibility.Collapsed;
                    }
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

        #endregion Window events

        #region Private methods

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
            for (int pIndex = 0; pIndex < _game.Players.Count; pIndex++)
            {
                (FindName($"StpCombosP{pIndex}") as StackPanel).Children.Clear();
                FillHandPanel(pIndex);
                FillDiscardPanel(pIndex);
            }

            BtnChii.Visibility = BtnChiiVisibility();
            BtnPon.Visibility = Visibility.Collapsed;
            BtnKan.Visibility = _game.Round.CanCallKan(GamePivot.HUMAN_INDEX) > 0 ? Visibility.Visible : Visibility.Collapsed;
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

        // Checks if a round is over by wall exhaustion.
        private bool IsEndOfRoundByWallExhaustion()
        {
            if (_game.Round.IsWallExhaustion)
            {
                NewRound();
                return true;
            }
            return false;
        }

        // Proceeds to new round.
        private void NewRound()
        {
            MessageBox.Show("End of round");
            _game.NewRound();
            NewRoundRefresh();
            AutoSkip();
        }

        // Generates a button which represents a tile.
        private Button GenerateTileButton(TilePivot tile, RoutedEventHandler handler = null, Angle angle = Angle.A0, bool concealed = false)
        {
            string rscName = concealed ? CONCEALED_TILE_RSC_NAME : tile.ToString();

            Bitmap tileBitmap = Properties.Resources.ResourceManager.GetObject(rscName) as Bitmap;

            var button = new Button
            {
                Height = angle == Angle.A0 || angle == Angle.A180 ? TILE_HEIGHT : TILE_WIDTH,
                Width = angle == Angle.A0 || angle == Angle.A180 ? TILE_WIDTH : TILE_HEIGHT,
                Content = new System.Windows.Controls.Image
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

        // Recomputes the visibility of "chii" call button.
        private Visibility BtnChiiVisibility()
        {
            return _game.Round.IsHumanPlayer && _game.Round.CanCallChii().Keys.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
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
                    (Angle)(tileKvp.Value ? (pIndex == 3 ? 0 : pIndex + 1) : pIndex),
                    combo.IsSquare && combo.IsConcealed && i > 0 && i < 3));
                i++;
            }

            return panel;
        }

        // Proceeds to skip CPU moves while human can't interact.
        private void AutoSkip()
        {
            Task.Run(() =>
            {
                // TODO : add "Ron" action when ready.
                while (!_game.Round.IsHumanPlayer
                    && _game.Round.CanCallKan(GamePivot.HUMAN_INDEX) == 0
                    && !_game.Round.CanCallPon(GamePivot.HUMAN_INDEX)
                    && (!_game.Round.IsHumanPlayer || _game.Round.CanCallChii().Keys.Count == 0))
                {
                    Thread.Sleep(Convert.ToInt32(CPU_SPEED.ToString().Replace("S", string.Empty)));
                    if (_game.Round.AutoPickAndDiscard())
                    {
                        Dispatcher.Invoke(() =>
                        {
                            FillDiscardPanel(_game.Round.PreviousPlayerIndex);
                            FillHandPanel(_game.Round.PreviousPlayerIndex);
                            BtnChii.Visibility = BtnChiiVisibility();
                            BtnPon.Visibility = _game.Round.CanCallPon(GamePivot.HUMAN_INDEX) ? Visibility.Visible : Visibility.Collapsed;
                            BtnKan.Visibility = _game.Round.CanCallKan(GamePivot.HUMAN_INDEX) > 0 ? Visibility.Visible : Visibility.Collapsed;
                        });
                    }
                    else if (_game.Round.IsWallExhaustion)
                    {
                        Dispatcher.Invoke(NewRound);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                if (_game.Round.IsHumanPlayer)
                {
                    TilePivot pick = _game.Round.Pick();
                    if (pick == null)
                    {
                        if (_game.Round.IsWallExhaustion)
                        {
                            Dispatcher.Invoke(NewRound);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            StpPickP0.Children.Add(GenerateTileButton(pick, BtnDiscard_Click));
                            BtnChii.Visibility = Visibility.Collapsed;
                            BtnPon.Visibility = Visibility.Collapsed;
                            BtnKan.Visibility = _game.Round.CanCallKan(GamePivot.HUMAN_INDEX) > 0 ? Visibility.Visible : Visibility.Collapsed;
                        });
                    }
                }
            });
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

        // Represents speed of play for CPU. 
        private enum CpuSpeed
        {
            S2000,
            S1000,
            S500,
            S200,
            S0,
        }
    }
}
