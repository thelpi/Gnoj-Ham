using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Gnoj_Ham;

namespace Gnoj_HamView
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int TILE_WIDTH = 60;
        private const int TILE_HEIGHT = 80;

        private GamePivot _game;

        public MainWindow()
        {
            InitializeComponent();

            _game = new GamePivot("Human", InitialPointsRulePivot.K25, true);

            NewRoundRefresh();

            BuildPanelHand();

            StpTreasure.Children.Add(GenerateTileButton(_game.Round.DoraIndicatorTiles.First()));

            BtnCallChi.IsEnabled = _game.Round.IsHumanPlayer && _game.Round.CanCallChii().Keys.Count > 0;
            BtnPick.IsEnabled = _game.Round.IsHumanPlayer;
            BtnSkip.IsEnabled = true;
        }

        private void BuildPanelHand()
        {
            StpHandP0.Children.Clear();
            foreach (var tile in _game.Round.Hands.ElementAt(GamePivot.HUMAN_INDEX).ConcealedTiles)
            {
                StpHandP0.Children.Add(GenerateTileButton(tile, Discard));
            }
        }

        private void NewRoundRefresh()
        {
            LblCurrentEast.Content = $"Player {_game.EastIndex + 1} ({_game.EastIndexTurnCount})";
            LblDominantWind.Content = _game.DominantWind.ToString();
            LblPlayer1Points.Content = $"{_game.Players.ElementAt(0).Name} {_game.Players.ElementAt(0).Points}";
            LblPlayer2Points.Content = $"{_game.Players.ElementAt(1).Name} {_game.Players.ElementAt(1).Points}";
            LblPlayer3Points.Content = $"{_game.Players.ElementAt(2).Name} {_game.Players.ElementAt(2).Points}";
            LblPlayer4Points.Content = $"{_game.Players.ElementAt(3).Name} {_game.Players.ElementAt(3).Points}";
        }

        private void Discard(object sender, RoutedEventArgs e)
        {
            if (_game.Round.IsHumanPlayer)
            {
                bool success = _game.Round.Discard((sender as Button).Tag as TilePivot);
                if (success)
                {
                    BuildPanelHand();
                    AddToPlayerDiscard(_game.Round.PreviousPlayerIndex);
                    EndOFRound();
                    BtnCallChi.IsEnabled = _game.Round.IsHumanPlayer && _game.Round.CanCallChii().Keys.Count > 0;
                    BtnPick.IsEnabled = _game.Round.IsHumanPlayer;
                    BtnSkip.IsEnabled = true;
                }
            }
        }

        private void AddToPlayerDiscard(int pIndex)
        {
            (FindName($"StpDiscardP{pIndex}") as StackPanel).Children.Add(GenerateTileButton(_game.Round.Discards.ElementAt(pIndex).Last()));
        }

        private void EndOFRound()
        {
            if (_game.Round.WallTiles.Count == 0)
            {
                MessageBox.Show("End of round");
                Environment.Exit(0);
            }
        }

        private void EffectiveChiCall(object sender, RoutedEventArgs e)
        {
            if (_game.Round.CallChii(((sender as Button).Tag as TilePivot).Number))
            {
                BuildPanelHand();
                foreach (var tile in _game.Round.Hands.ElementAt(_game.Round.CurrentPlayerIndex).DeclaredCombinations.Last().Tiles)
                {
                    StpOpenCombinationsP0.Children.Add(GenerateTileButton(tile));
                }
                BtnCallChi.IsEnabled = false;
                BtnPick.IsEnabled = false;
                BtnSkip.IsEnabled = false;
            }
            else
            {
                EndOFRound();
            }
        }

        private void BtnCallChi_Click(object sender, RoutedEventArgs e)
        {
            var r = _game.Round.CanCallChii();

            BtnCallChi.IsEnabled = false;
            BtnPick.IsEnabled = false;
            BtnSkip.IsEnabled = false;

            // attente du clic
        }

        private void BtnSkip_Click(object sender, RoutedEventArgs e)
        {
            if (_game.Round.AutoPickAndDiscard())
            {
                AddToPlayerDiscard(_game.Round.PreviousPlayerIndex);
                if (_game.Round.PreviousPlayerIndex == GamePivot.HUMAN_INDEX)
                {
                    BuildPanelHand();
                }
                BtnCallChi.IsEnabled = _game.Round.IsHumanPlayer && _game.Round.CanCallChii().Keys.Count > 0;
                BtnPick.IsEnabled = _game.Round.IsHumanPlayer;
                BtnSkip.IsEnabled = true;
            }
            else
            {
                EndOFRound();
                // unable to skip for another reason.
            }
        }

        private Button GenerateTileButton(TilePivot tile, RoutedEventHandler handler = null)
        {
            var imgObj = Properties.Resources.ResourceManager.GetObject(tile.ToString());

            var button = new Button
            {
                Height = TILE_HEIGHT,
                Width = TILE_WIDTH,
                Content = new System.Windows.Controls.Image { Source = ToBitmapImage(imgObj as Bitmap) },
                Tag = tile
            };
            if (handler != null)
            {
                button.Click += handler;
            }
            return button;
        }

        private BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        private void BtnPick_Click(object sender, RoutedEventArgs e)
        {
            if (!_game.Round.Pick())
            {
                EndOFRound();
            }
            else
            {
                BuildPanelHand();
                BtnCallChi.IsEnabled = false;
                BtnPick.IsEnabled = false;
                BtnSkip.IsEnabled = false;
            }
        }
    }
}
