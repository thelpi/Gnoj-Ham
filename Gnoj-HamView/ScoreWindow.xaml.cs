using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Gnoj_Ham;

namespace Gnoj_HamView
{
    /// <summary>
    /// Interaction logic for ScoreWindow.xaml.
    /// </summary>
    public partial class ScoreWindow : Window
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="game">Current game.</param>
        /// <param name="endOfRoundInformations">Informations about end of round.</param>
        public ScoreWindow(GamePivot game, EndOfRoundInformationsPivot endOfRoundInformations)
        {
            InitializeComponent();

            GrdDorasAndInfos.RowDefinitions[0].Height = new GridLength(GraphicTools.TILE_HEIGHT + GraphicTools.DEFAULT_TILE_MARGIN);
            GrdDorasAndInfos.RowDefinitions[1].Height = new GridLength(GraphicTools.TILE_HEIGHT + GraphicTools.DEFAULT_TILE_MARGIN);
            GrdDorasAndInfos.ColumnDefinitions[1].Width = new GridLength((GraphicTools.TILE_WIDTH * 5) + GraphicTools.DEFAULT_TILE_MARGIN);

            LblHonba.Content = endOfRoundInformations.HonbaCount;
            LblPendingRiichi.Content = endOfRoundInformations.PendingRiichiCount;
            StpDoraTiles.SetDorasPanel(game.Round.DoraIndicatorTiles, game.Round.VisibleDorasCount);
            StpUraDoraTiles.SetDorasPanel(game.Round.UraDoraIndicatorTiles, endOfRoundInformations.DisplayUraDora ? game.Round.VisibleDorasCount : 0);

            foreach (EndOfRoundInformationsPivot.PlayerInformationsPivot p in endOfRoundInformations.PlayersInfo)
            {
                if (p.Yakus.Count > 0)
                {
                    StpYakus.Children.Add(new GroupBox
                    {
                        Header = game.Players.ElementAt(p.Index).Name,
                        Content = p.GenerateYakusInfosPanel()
                    });
                }
            }

            for (int i = 0; i < 4; i++)
            {
                (FindName($"LblPlayer{i}") as Label).Content = game.Players.ElementAt(i).Name;
                (FindName($"LblScore{i}") as Label).Content = game.Players.ElementAt(i).Points;

                int gain = endOfRoundInformations.PlayersInfo.FirstOrDefault(p => p.Index == i)?.PointsGain ?? 0;

                Label lblGain = (FindName($"LblGain{i}") as Label);

                lblGain.Content = gain;
                if (gain > 0)
                {
                    lblGain.Content = $"+{gain}";
                    lblGain.Foreground = Brushes.ForestGreen;
                }
                else if (gain < 0)
                {
                    lblGain.Foreground = Brushes.Red;
                }
            }
        }
    }
}
