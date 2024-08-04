using System.Windows;
using System.Windows.Controls;
using Gnoj_Ham_Library;

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

        GrdDorasAndInfos.RowDefinitions[0].Height = new GridLength(GraphicTools.TILE_HEIGHT + GraphicTools.DEFAULT_TILE_MARGIN);
        GrdDorasAndInfos.RowDefinitions[1].Height = new GridLength(GraphicTools.TILE_HEIGHT + GraphicTools.DEFAULT_TILE_MARGIN);
        GrdDorasAndInfos.ColumnDefinitions[1].Width = new GridLength((GraphicTools.TILE_WIDTH * 5) + GraphicTools.DEFAULT_TILE_MARGIN);

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
                    Header = players[p.Index].Name,
                    Content = p.GenerateYakusInfosPanel()
                });
            }
        }

        var x = 0;
        foreach (var (p, i) in players.Select((p, i) => (p, i)).OrderByDescending(ip => ip.p.Points))
        {
            this.FindControl("LblPlayer", x).Content = p.Name;
            this.FindControl("LblScore", x).Content = p.Points;
            this.FindControl("LblGain", x).ApplyGainAndLostStyle(endOfRoundInformations.GetPlayerPointsGain(i));
            x++;
        }
    }

    private void BtnGoToNext_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
