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

            _game = new GamePivot(InitialPointsRulePivot.K25, true);

            NewRoundRefresh();

            BuildPanelHand();

            StpTreasure.Children.Add(GenerateTileButton(_game.Round.DoraIndicatorTiles.First()));
        }

        private void BuildPanelHand()
        {
            StpHandP0.Children.Clear();
            foreach (var tile in _game.Round.Hands.ElementAt(0).ConcealedTiles)
            {
                StpHandP0.Children.Add(GenerateTileButton(tile));
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

        private void BtnCallChi_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnCallPon_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnCallKan_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnCallTsumo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnCallRon_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnSkip_Click(object sender, RoutedEventArgs e)
        {
            var playerToIncrement = _game.Round.CurrentPlayerIndex;
            if (!_game.Round.DefaultAction())
            {
                MessageBox.Show("End of round");
                Environment.Exit(0);
            }
            (FindName($"StpDiscardP{playerToIncrement}") as StackPanel).Children.Add(GenerateTileButton(_game.Round.Discards.ElementAt(playerToIncrement).Last()));
        }

        private Button GenerateTileButton(TilePivot tile, RoutedEventHandler handler = null)
        {
            var imgObj = Properties.Resources.ResourceManager.GetObject(tile.ToString());

            var button = new Button
            {
                Height = TILE_HEIGHT,
                Width = TILE_WIDTH,
                Content = new System.Windows.Controls.Image { Source = ToBitmapImage(imgObj as Bitmap) }
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
    }
}
