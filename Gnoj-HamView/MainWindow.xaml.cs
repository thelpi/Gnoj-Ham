﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Gnoj_Ham;

namespace Gnoj_HamView
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int TILE_WIDTH = 45;
        private const int TILE_HEIGHT = 60;
        private const int DEFAULT_TILE_MARGIN = 10;
        private const string WINDOW_TITLE = "Gnoj-Ham";
        private const string CONCEALED_TILE_RSC_NAME = "concealed";

        private readonly GamePivot _game;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

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

            // TODO : first screen to personalize informations.
            _game = new GamePivot("Human", InitialPointsRulePivot.K25, true);

            // Fix the size of every discard panels
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

            NewRoundRefresh();
        }

        #region Window events

        private void BtnDiscard_Click(object sender, RoutedEventArgs e)
        {
            if (_game.Round.Discard((sender as Button).Tag as TilePivot))
            {
                FillHandPanel(_game.Round.PreviousPlayerIndex);
                AddLatestDiscardToPanel(_game.Round.PreviousPlayerIndex);
                BtnChii.Visibility = BtnChiiVisibility();
                BtnPick.Visibility = _game.Round.IsHumanPlayer ? Visibility.Visible : Visibility.Collapsed;
                BtnSkip.Visibility = Visibility.Visible;

                IsEndOfRoundByWallExhaustion();
            }
        }

        private void BtnChiiChoice_Click(object sender, RoutedEventArgs e)
        {
            KeyValuePair<TilePivot, bool> tag = (KeyValuePair<TilePivot, bool>)((sender as Button).Tag);

            if (_game.Round.CallChii(tag.Value ? tag.Key.Number - 1 : tag.Key.Number))
            {
                FillHandPanel(_game.Round.CurrentPlayerIndex);
                AddLatestCombinationToStack(GamePivot.HUMAN_INDEX);
                BtnChii.Visibility = Visibility.Collapsed;
                BtnPick.Visibility = Visibility.Collapsed;
                BtnSkip.Visibility = Visibility.Collapsed;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void BtnSkip_Click(object sender, RoutedEventArgs e)
        {
            if (_game.Round.AutoPickAndDiscard())
            {
                AddLatestDiscardToPanel(_game.Round.PreviousPlayerIndex);
                FillHandPanel(_game.Round.PreviousPlayerIndex);
                BtnChii.Visibility = BtnChiiVisibility();
                BtnPick.Visibility = _game.Round.IsHumanPlayer ? Visibility.Visible : Visibility.Collapsed;
                BtnSkip.Visibility = Visibility.Visible;
            }
            else if (!IsEndOfRoundByWallExhaustion())
            {
                // Failure of auto-pick and discard for no reason.
                throw new NotImplementedException();
            }
        }

        private void BtnPick_Click(object sender, RoutedEventArgs e)
        {
            TilePivot pick = _game.Round.Pick();
            if (pick == null)
            {
                if (!IsEndOfRoundByWallExhaustion())
                {
                    // Failure of pick for no reason.
                    throw new NotImplementedException();
                }
            }
            else
            {
                StpPickP0.Children.Add(GenerateTileButton(pick, BtnDiscard_Click));
                BtnChii.Visibility = Visibility.Collapsed;
                BtnPick.Visibility = Visibility.Collapsed;
                BtnSkip.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnTsumo_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BtnRon_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BtnPon_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BtnChii_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<TilePivot, bool> tileChoices = _game.Round.CanCallChii();

            if (tileChoices.Keys.Count > 0)
            {
                BtnChii.Visibility = Visibility.Collapsed;
                BtnPick.Visibility = Visibility.Collapsed;
                BtnSkip.Visibility = Visibility.Collapsed;

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
            throw new NotImplementedException();
        }

        private void BtnRiichi_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        #endregion Window events

        #region Private methods

        // Clears and refills the hand panel of the specified player index.
        private void FillHandPanel(int pIndex)
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
                panel.Children.Add(GenerateTileButton(tile, isHuman ? BtnDiscard_Click : (RoutedEventHandler)null, (Angle)pIndex, !isHuman));
            }
        }

        // Resets and refills every panels at a new round.
        private void NewRoundRefresh()
        {
            for (int pIndex = 0; pIndex < _game.Players.Count; pIndex++)
            {
                (FindName($"StpCombosP{pIndex}") as StackPanel).Children.Clear();
                (FindName($"StpP{pIndex}Discard1") as StackPanel).Children.Clear();
                (FindName($"StpP{pIndex}Discard2") as StackPanel).Children.Clear();
                (FindName($"StpP{pIndex}Discard3") as StackPanel).Children.Clear();
                FillHandPanel(pIndex);
            }

            BtnChii.Visibility = BtnChiiVisibility();
            BtnPick.Visibility = _game.Round.IsHumanPlayer ? Visibility.Visible : Visibility.Collapsed;
            BtnSkip.Visibility = Visibility.Visible;
        }

        // Adds the last tile discarded to the discard panel of the specified player.
        private void AddLatestDiscardToPanel(int pIndex)
        {
            IReadOnlyCollection<TilePivot> discards = _game.Round.Discards.ElementAt(pIndex);

            StackPanel panel = FindName($"StpP{pIndex}Discard{(discards.Count > 12 ? 3 : (discards.Count > 6 ? 2 : 1))}") as StackPanel;

            if (pIndex == 1 || pIndex == 2)
            {
                panel.Children.Insert(0, GenerateTileButton(discards.Last(), angle: (Angle)pIndex));
            }
            else
            {
                panel.Children.Add(GenerateTileButton(discards.Last(), angle: (Angle)pIndex));
            }
        }

        // Checks if a round is over by wall exhaustion.
        private bool IsEndOfRoundByWallExhaustion()
        {
            if (_game.Round.IsWallExhaustion)
            {
                MessageBox.Show("End of round");
                _game.NewRound();
                NewRoundRefresh();
                return true;
            }
            return false;
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

            Dictionary<TilePivot, bool> tiles = combo.GetSortedTilesForDisplay(pWind);
            int i = 0;
            foreach (TilePivot tile in tiles.Keys)
            {
                panel.Children.Add(GenerateTileButton(tile,
                    null,
                    (Angle)(tiles[tile] ? (pIndex == 3 ? 0 : pIndex + 1) : pIndex),
                    combo.IsSquare && combo.IsConcealed && i > 0 && i < 3));
                i++;
            }

            return panel;
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
