using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Gnoj_Ham;

namespace Gnoj_HamView
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GamePivot _game;

        public MainWindow()
        {
            InitializeComponent();

            _game = new GamePivot(InitialPointsRulePivot.K25, true);

            NewRoundRefresh();
            
            BtnSkip.IsEnabled = _game.Round.CurrentPlayerIndex != 0;
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
            if (!_game.Round.DefaultAction())
            {
                MessageBox.Show("End of round");
                Environment.Exit(0);
            }
            BtnSkip.IsEnabled = _game.Round.CurrentPlayerIndex != 0;
        }
    }
}
