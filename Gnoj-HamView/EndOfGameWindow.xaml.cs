using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Gnoj_Ham;

namespace Gnoj_HamView
{
    /// <summary>
    /// Logique d'interaction pour EndOfGameWindow.xaml
    /// </summary>
    public partial class EndOfGameWindow : Window
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="game">The game.</param>
        public EndOfGameWindow(GamePivot game)
        {
            InitializeComponent();

            List<PlayerScorePivot> playerScores = ScoreTools.ComputeCurrentRanking(game);
            for (int i = 0; i < playerScores.Count; i++)
            {
                (FindName($"LblRank{i}") as Label).Content = playerScores[i].Rank;
                (FindName($"LblPlayer{i}") as Label).Content = playerScores[i].Player.Name;
                (FindName($"LblPoints{i}") as Label).Content = playerScores[i].Player.Points;
                (FindName($"LblUma{i}") as Label).ApplyGainAndLostStyle(playerScores[i].Uma);
                (FindName($"LblScore{i}") as Label).ApplyGainAndLostStyle(playerScores[i].Score);
            }
        }
    }
}
