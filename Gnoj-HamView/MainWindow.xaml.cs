using System;
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
            
            NewRoundRefresh();
        }

        #region Window events

        private void BtnDiscard_Click(object sender, RoutedEventArgs e)
        {
            if (_game.Round.Discard((sender as Button).Tag as TilePivot))
            {
                FillHandPanel(_game.Round.PreviousPlayerIndex);
                AddToPlayerDiscard(_game.Round.PreviousPlayerIndex);
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
                AddToPlayerDiscard(_game.Round.PreviousPlayerIndex);
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
            if (!_game.Round.Pick())
            {
                if (!IsEndOfRoundByWallExhaustion())
                {
                    // Failure of pick for no reason.
                    throw new NotImplementedException();
                }
            }
            else
            {
                StpPickP0.Children.Add(GenerateTileButton(_game.Round.Hands.ElementAt(GamePivot.HUMAN_INDEX).ConcealedTiles.Last(), BtnDiscard_Click));
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
            StpCombosP0.Children.Clear();
            StpCombosP1.Children.Clear();
            StpCombosP2.Children.Clear();
            StpCombosP3.Children.Clear();

            for (int pIndex = 0; pIndex < _game.Players.Count; pIndex++)
            {
                FillHandPanel(pIndex);
            }

            BtnChii.Visibility = BtnChiiVisibility();
            BtnPick.Visibility = _game.Round.IsHumanPlayer ? Visibility.Visible : Visibility.Collapsed;
            BtnSkip.Visibility = Visibility.Visible;
        }

        private void AddToPlayerDiscard(int pIndex)
        {
            /*(FindName($"StpDiscardP{pIndex}") as StackPanel).Children.Add(GenerateTileButton(_game.Round.Discards.ElementAt(pIndex).Last()));*/
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

        }

        #endregion Private methods

        // Represents tile rotation (depending on the player).
        private enum Angle
        {
            A0,
            A90,
            A180,
            A270
        }
    }
}
