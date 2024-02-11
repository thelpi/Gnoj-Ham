using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Gnoj_Ham;

namespace Gnoj_HamView
{
    /// <summary>
    /// Graphic tools.
    /// </summary>
    internal static class GraphicTools
    {
        /// <summary>
        /// Tile width.
        /// </summary>
        internal const int TILE_WIDTH = 45;
        /// <summary>
        /// Tile height.
        /// </summary>
        internal const int TILE_HEIGHT = 60;
        /// <summary>
        /// Default margin.
        /// </summary>
        internal const int DEFAULT_TILE_MARGIN = 10;
        /// <summary>
        /// The table size (width and height).
        /// </summary>
        internal const int EXPECTED_TABLE_SIZE = 920;
        /// <summary>
        /// Tile concealed resource name.
        /// </summary>
        internal const string CONCEALED_TILE_RSC_NAME = "concealed";

        /// <summary>
        /// Extension; generates a button which represents a tile.
        /// </summary>
        /// <param name="tile">The tile to display.</param>
        /// <param name="handler">Optionnal; event on click on the button; default value is <c>Null</c>.</param>
        /// <param name="angle">Optionnal; rotation angle; default is <c>0°</c>.</param>
        /// <param name="concealed">Optionnal; set <c>True</c> to display a concealed tile; default is <c>False</c>.</param>
        /// <param name="rate">Optionnal; applies a rate on the size of the tile; default is <c>1</c>.</param>
        /// <returns>A button representing the tile.</returns>
        internal static Button GenerateTileButton(this TilePivot tile, RoutedEventHandler handler = null, AnglePivot angle = AnglePivot.A0, bool concealed = false, double rate = 1)
        {
            var rscName = concealed ? CONCEALED_TILE_RSC_NAME : tile.ToResourceName();

            var tileBitmap = Properties.Resources.ResourceManager.GetObject(rscName) as Bitmap;

            var button = new Button
            {
                Height = angle == AnglePivot.A0 || angle == AnglePivot.A180 ? (TILE_HEIGHT * rate) : (TILE_WIDTH * rate),
                Width = angle == AnglePivot.A0 || angle == AnglePivot.A180 ? (TILE_WIDTH * rate) : (TILE_HEIGHT * rate),
                Content = new System.Windows.Controls.Image
                {
                    Source = tileBitmap.ToBitmapImage(),
                    LayoutTransform = new RotateTransform(Convert.ToDouble(angle.ToString().Replace("A", string.Empty)))
                },
                Tag = tile,
                ToolTip = concealed ? null : tile.ToString()
            };

            if (handler != null)
            {
                button.Click += handler;
            }

            return button;
        }

        /// <summary>
        /// Extension; resets a panel filled with dora tiles.
        /// </summary>
        /// <param name="panel">The panel.</param>
        /// <param name="tiles">List of dora tiles.</param>
        /// <param name="visibleCount">Number of tiles not concealed.</param>
        internal static void SetDorasPanel(this StackPanel panel, IEnumerable<TilePivot> tiles, int visibleCount)
        {
            panel.Children.Clear();

            var concealedCount = 5 - visibleCount;
            for (var i = 4; i >= 0; i--)
            {
                panel.Children.Add(tiles.ElementAt(i).GenerateTileButton(concealed: 5 - concealedCount <= i, rate: 0.8));
            }
        }

        /// <summary>
        /// Generates a panel which contains every information about the value of the hand of the specified player.
        /// </summary>
        /// <param name="p">The player informations for this round.</param>
        /// <returns>A panel with informations about hand value.</returns>
        internal static DockPanel GenerateYakusInfosPanel(this EndOfRoundInformationsPivot.PlayerInformationsPivot p)
        {
            var boxPanel = new DockPanel();

            var separator = SeparatorForScoreDisplay(p);
            if (separator != null)
            {
                boxPanel.Children.Add(separator);
            }

            boxPanel.Children.Add(PointsGridForScoreDisplay(p));
            boxPanel.Children.Add(HandPanelForScoreDisplay(p));

            var gridYakus = YakusGridForScoreDisplay(p);
            if (gridYakus != null)
            {
                boxPanel.Children.Add(gridYakus);
            }

            return boxPanel;
        }

        /// <summary>
        /// Extension; transfoms a <see cref="Bitmap"/> to a <see cref="BitmapImage"/>.
        /// </summary>
        /// <param name="bitmap">The <see cref="Bitmap"/> to transform.</param>
        /// <returns>The converted <see cref="BitmapImage"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bitmap"/> is <c>Null</c>.</exception>
        internal static BitmapImage ToBitmapImage(this Bitmap bitmap)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException(nameof(bitmap));
            }

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

        /// <summary>
        /// Gets a japanese caracter which represents the specified wind.
        /// </summary>
        /// <param name="wind">The wind to display.</param>
        /// <returns>The associated japanese caracter.</returns>
        /// <exception cref="NotImplementedException">The wind is not implemented.</exception>
        internal static string ToWindDisplay(this WindPivot wind)
        {
            switch (wind)
            {
                case WindPivot.East:
                    return "東";
                case WindPivot.South:
                    return "南";
                case WindPivot.West:
                    return "西";
                case WindPivot.North:
                    return "北";
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Extension; converts a <see cref="CpuSpeedPivot"/> to a integer value.
        /// </summary>
        /// <param name="cpuSpeed">The speed to convert.</param>
        /// <returns>The integer value.</returns>
        internal static int ParseSpeed(this CpuSpeedPivot cpuSpeed)
        {
            return Convert.ToInt32(cpuSpeed.ToString().Replace("S", string.Empty));
        }

        /// <summary>
        /// Transforms a <see cref="ChronoPivot"/> value into its delay in seconds.
        /// </summary>
        /// <param name="chrono">The chrono value.</param>
        /// <returns>Delay in seconds.</returns>
        internal static int GetDelay(this ChronoPivot chrono)
        {
            switch (chrono)
            {
                case ChronoPivot.Long:
                    return 20;
                case ChronoPivot.Short:
                    return 5;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Transforms a <see cref="ChronoPivot"/> value into its french representation.
        /// </summary>
        /// <param name="chrono">The chrono value.</param>
        /// <returns>French representation.</returns>
        internal static string DisplayName(this ChronoPivot chrono)
        {
            switch (chrono)
            {
                case ChronoPivot.Long:
                    return "Long";
                case ChronoPivot.Short:
                    return "Court";
                default:
                    return "Aucun";
            }
        }

        /// <summary>
        /// Applies a style to a label to show a gain or lost.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="gainOrLostValue">The gain or lost value.</param>
        internal static void ApplyGainAndLostStyle(this ContentControl control, int gainOrLostValue)
        {
            control.Content = gainOrLostValue;
            if (gainOrLostValue > 0)
            {
                control.Content = $"+{gainOrLostValue}";
                control.Foreground = System.Windows.Media.Brushes.ForestGreen;
            }
            else if (gainOrLostValue < 0)
            {
                control.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        /// <summary>
        /// Extension; retrieves a <see cref="FrameworkElement"/> from a <see cref="Window"/> by its name and the player index.
        /// </summary>
        /// <typeparam name="T">Subtype of <see cref="FrameworkElement"/>.</typeparam>
        /// <param name="window">The window.</param>
        /// <param name="nameWithoutIndex">The element name without the player index.</param>
        /// <param name="playerIndex">The player index.</param>
        /// <returns>The element.</returns>
        internal static T FindName<T>(this Window window, string nameWithoutIndex, int playerIndex) where T : FrameworkElement
        {
            return window.FindName(string.Concat(nameWithoutIndex, playerIndex)) as T;
        }

        /// <summary>
        /// Extension; retrieves a <see cref="ContentControl"/> from a <see cref="Window"/> by its name and the player index.
        /// </summary>
        /// <param name="window">The window.</param>
        /// <param name="nameWithoutIndex">The control name without the player index.</param>
        /// <param name="playerIndex">The player index.</param>
        /// <returns>The control.</returns>
        internal static ContentControl FindControl(this Window window, string nameWithoutIndex, int playerIndex)
        {
            return window.FindName<ContentControl>(nameWithoutIndex, playerIndex);
        }

        /// <summary>
        /// Extension; retrieves a <see cref="Panel"/> from a <see cref="Window"/> by its name and the player index.
        /// </summary>
        /// <param name="window">The window.</param>
        /// <param name="nameWithoutIndex">The panel name without the player index.</param>
        /// <param name="playerIndex">The player index.</param>
        /// <returns>The panel.</returns>
        internal static Panel FindPanel(this Window window, string nameWithoutIndex, int playerIndex)
        {
            return window.FindName<Panel>(nameWithoutIndex, playerIndex);
        }

        #region Enum converters

        /// <summary>
        /// Transforms the enumeration <see cref="CpuSpeedPivot"/> into a list of <see cref="string"/> (with matching index).
        /// </summary>
        /// <returns>List of strings ready for display.</returns>
        internal static List<string> GetCpuSpeedDisplayValues()
        {
            return Enum.GetValues(typeof(CpuSpeedPivot)).OfType<CpuSpeedPivot>().Select(v =>
            {
                var intParsedValue = v.ParseSpeed();

                return intParsedValue >= 1000 ? $"{intParsedValue / 1000} sec" : $"{intParsedValue} ms";
            }).ToList();
        }

        /// <summary>
        /// Transforms the enumeration <see cref="ChronoPivot"/> into a list of <see cref="string"/> (with matching index).
        /// </summary>
        /// <returns>List of strings ready for display.</returns>
        internal static List<string> GetChronoDisplayValues()
        {
            var results = new List<string>();

            foreach (var ch in Enum.GetValues(typeof(ChronoPivot)).OfType<ChronoPivot>())
            {
                switch (ch)
                {
                    case ChronoPivot.None:
                        results.Add("Aucun");
                        break;
                    case ChronoPivot.Short:
                    case ChronoPivot.Long:
                        results.Add($"{ch.DisplayName()} ({ch.GetDelay()} sec)");
                        break;
                }
            }

            return results;
        }

        /// <summary>
        /// Transforms the enumeration <see cref="EndOfGameRulePivot"/> into a list of <see cref="string"/> (with matching index).
        /// </summary>
        /// <returns>List of strings ready for display.</returns>
        internal static List<string> GetEndOfGameRuleDisplayValue()
        {
            var results = new List<string>();

            foreach (var rule in Enum.GetValues(typeof(EndOfGameRulePivot)).OfType<EndOfGameRulePivot>())
            {
                switch (rule)
                {
                    case EndOfGameRulePivot.Enchousen:
                        results.Add("Enchousen");
                        break;
                    case EndOfGameRulePivot.EnchousenAndTobi:
                        results.Add("Enchousen + Tobi");
                        break;
                    case EndOfGameRulePivot.Oorasu:
                        results.Add("Oorasu");
                        break;
                    case EndOfGameRulePivot.Tobi:
                        results.Add("Tobi");
                        break;
                }
            }

            return results;
        }

        /// <summary>
        /// Transforms the enumeration <see cref="InitialPointsRulePivot"/> into a list of <see cref="string"/> (with matching index).
        /// </summary>
        /// <returns>List of strings ready for display.</returns>
        internal static List<string> GetInitialPointsRuleDisplayValue()
        {
            return Enum.GetValues(typeof(InitialPointsRulePivot))
                    .OfType<InitialPointsRulePivot>()
                    .Select(v => $"{Convert.ToInt32(v.ToString().Replace("K", string.Empty))},000")
                    .ToList();
        }

        #endregion Enum converters

        #region Private methods

        private static Grid PointsGridForScoreDisplay(EndOfRoundInformationsPivot.PlayerInformationsPivot p)
        {
            var gridPoints = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Right
            };
            gridPoints.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            gridPoints.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            gridPoints.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            gridPoints.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });

            var fanLbl = FanlabelForScoreDisplay(p);
            if (fanLbl != null)
            {
                gridPoints.Children.Add(fanLbl);
            }

            var fuLbl = FuLabelForScopreDisplay(p);
            if (fuLbl != null)
            {
                gridPoints.Children.Add(fuLbl);
            }
            gridPoints.Children.Add(GainLabelForScoreDisplay(p));

            gridPoints.SetValue(DockPanel.DockProperty, Dock.Bottom);
            return gridPoints;
        }

        private static Label GainLabelForScoreDisplay(EndOfRoundInformationsPivot.PlayerInformationsPivot p)
        {
            var gainLbl = new Label
            {
                VerticalAlignment = VerticalAlignment.Center,
                Content = p.HandPointsGain,
                Foreground = System.Windows.Media.Brushes.Red
            };
            gainLbl.SetValue(Grid.RowProperty, 0);
            gainLbl.SetValue(Grid.ColumnProperty, 2);
            return gainLbl;
        }

        private static Line SeparatorForScoreDisplay(EndOfRoundInformationsPivot.PlayerInformationsPivot p)
        {
            if (p.Yakus == null || p.Yakus.Count == 0)
            {
                return null;
            }

            var separator = new Line
            {
                Fill = System.Windows.Media.Brushes.Black,
                Height = 2,
                Width = 200,
                VerticalAlignment = VerticalAlignment.Center
            };
            separator.SetValue(DockPanel.DockProperty, Dock.Bottom);
            return separator;
        }

        private static Label FuLabelForScopreDisplay(EndOfRoundInformationsPivot.PlayerInformationsPivot p)
        {
            if (p.Yakus == null || p.Yakus.Count == 0)
            {
                return null;
            }

            var fuLbl = new Label
            {
                VerticalAlignment = VerticalAlignment.Center,
                Content = p.FuCount
            };
            fuLbl.SetValue(Grid.RowProperty, 0);
            fuLbl.SetValue(Grid.ColumnProperty, 1);
            return fuLbl;
        }

        private static Label FanlabelForScoreDisplay(EndOfRoundInformationsPivot.PlayerInformationsPivot p)
        {
            if (p.Yakus == null || p.Yakus.Count == 0)
            {
                return null;
            }

            var fanLbl = new Label
            {
                VerticalAlignment = VerticalAlignment.Center,
                Content = p.FanCount
            };
            fanLbl.SetValue(Grid.RowProperty, 0);
            fanLbl.SetValue(Grid.ColumnProperty, 0);
            return fanLbl;
        }

        private static Grid YakusGridForScoreDisplay(EndOfRoundInformationsPivot.PlayerInformationsPivot p)
        {
            if (p.Yakus == null || p.Yakus.Count == 0)
            {
                return null;
            }

            var gridYakus = new Grid();
            gridYakus.ColumnDefinitions.Add(new ColumnDefinition());
            gridYakus.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            var i = 0;
            foreach (var yaku in p.Yakus.GroupBy(y => y))
            {
                gridYakus.AddGridRowYaku(i, yaku.Key.Name, (p.Concealed ? yaku.Key.ConcealedFanCount : yaku.Key.FanCount) * yaku.Count());
                i++;
            }
            if (p.DoraCount > 0)
            {
                gridYakus.AddGridRowYaku(i, YakuPivot.Dora, p.DoraCount);
                i++;
            }
            if (p.UraDoraCount > 0)
            {
                gridYakus.AddGridRowYaku(i, YakuPivot.UraDora, p.UraDoraCount);
                i++;
            }
            if (p.RedDoraCount > 0)
            {
                gridYakus.AddGridRowYaku(i, YakuPivot.RedDora, p.RedDoraCount);
                i++;
            }

            gridYakus.SetValue(DockPanel.DockProperty, Dock.Top);
            return gridYakus;
        }

        private static StackPanel HandPanelForScoreDisplay(EndOfRoundInformationsPivot.PlayerInformationsPivot p)
        {
            var handPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Height = TILE_HEIGHT + (0.5 * DEFAULT_TILE_MARGIN),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            foreach (var (tile, leaned, apart) in p.GetFullHandForDisplay())
            {
                var b = GenerateTileButton(tile, null, (leaned ? AnglePivot.A90 : AnglePivot.A0), false);
                if (apart)
                {
                    b.Margin = new Thickness(5, 0, 0, 0);
                }
                handPanel.Children.Add(b);
            }
            handPanel.SetValue(DockPanel.DockProperty, Dock.Top);
            return handPanel;
        }

        // Adds a yaku to the grid.
        private static void AddGridRowYaku(this Grid gridTop, int i, string yakuName, int yakuFanCount)
        {
            gridTop.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });

            var lblYakuName = new Label
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Content = yakuName
            };
            lblYakuName.SetValue(Grid.ColumnProperty, 0);
            lblYakuName.SetValue(Grid.RowProperty, i);

            var lblYakuFanCount = new Label
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Content = yakuFanCount,
                Foreground = System.Windows.Media.Brushes.Red
            };
            lblYakuFanCount.SetValue(Grid.ColumnProperty, 1);
            lblYakuFanCount.SetValue(Grid.RowProperty, i);

            gridTop.Children.Add(lblYakuName);
            gridTop.Children.Add(lblYakuFanCount);
        }

        #endregion Private methods
    }
}
