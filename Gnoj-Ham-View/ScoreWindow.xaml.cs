using System.Windows;
using System.Windows.Controls;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_View;

/// <summary>
/// Interaction logic for ScoreWindow.xaml.
/// </summary>
public partial class ScoreWindow : Window
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="players">Lsit of players.</param>
    /// <param name="endOfRoundInformations">Informations about end of round.</param>
    public ScoreWindow(IReadOnlyList<PlayerPivot> players, EndOfRoundInformationsPivot endOfRoundInformations)
    {
        InitializeComponent();

        GrdDorasAndInfos.RowDefinitions[0].Height = new GridLength(TileButton.TILE_HEIGHT + TileButton.DEFAULT_TILE_MARGIN);
        GrdDorasAndInfos.RowDefinitions[1].Height = new GridLength(TileButton.TILE_HEIGHT + TileButton.DEFAULT_TILE_MARGIN);
        GrdDorasAndInfos.ColumnDefinitions[1].Width = new GridLength((TileButton.TILE_WIDTH * 5) + TileButton.DEFAULT_TILE_MARGIN);

        LblHonba.Content = endOfRoundInformations.HonbaCount;
        LblPendingRiichi.Content = endOfRoundInformations.PendingRiichiCount;
        StpDoraTiles.SetDorasPanel(endOfRoundInformations.DoraTiles, endOfRoundInformations.DoraVisibleCount);
        StpUraDoraTiles.SetDorasPanel(endOfRoundInformations.UraDoraTiles, endOfRoundInformations.UraDoraVisibleCount);

        foreach (var p in endOfRoundInformations.PlayersInfo)
        {
            if (p.HandPointsGain > 0)
            {
                StpYakus.Children.Add(new GroupBox
                {
                    Header = players[(int)p.Index].Name,
                    Content = p.GenerateYakusInfosPanel()
                });
            }
        }

        var x = 0;
        foreach (var (p, i) in players.Select((p, i) => (p, i)).OrderByDescending(ip => ip.p.Points))
        {
            this.FindControl("LblPlayer", (PlayerIndices)x).Content = p.Name;
            this.FindControl("LblScore", (PlayerIndices)x).Content = p.Points;
            this.FindControl("LblGain", (PlayerIndices)x).ApplyGainAndLostStyle(endOfRoundInformations.GetPlayerPointsGain((PlayerIndices)i));
            x++;
        }
    }

    private void BtnGoToNext_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
