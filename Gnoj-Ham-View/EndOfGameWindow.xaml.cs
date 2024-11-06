using System.Windows;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_View;

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

        var playerScores = game.ComputeCurrentRanking();
        foreach (var i in Enum.GetValues<PlayerIndices>())
        {
            this.FindControl("LblRank", i).Content = playerScores[(int)i].Rank;
            this.FindControl("LblPlayer", i).Content = playerScores[(int)i].Player.Name;
            this.FindControl("LblPoints", i).Content = playerScores[(int)i].Player.CurrentGamePoints;
            this.FindControl("LblUma", i).ApplyGainAndLostStyle(playerScores[(int)i].Uma);
            this.FindControl("LblScore", i).ApplyGainAndLostStyle(playerScores[(int)i].Score);
        }
    }
}
