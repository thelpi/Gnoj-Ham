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
        /// <returns>A button representing the tile.</returns>
        internal static Button GenerateTileButton(this TilePivot tile, RoutedEventHandler handler = null, Angle angle = Angle.A0, bool concealed = false)
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

        /// <summary>
        /// Extension; resets a panel filled with dora tiles.
        /// </summary>
        /// <param name="panel">The panel.</param>
        /// <param name="tiles">List of dora tiles.</param>
        /// <param name="visibleCount">Number of tiles not concealed.</param>
        internal static void SetDorasPanel(this StackPanel panel, IEnumerable<TilePivot> tiles, int visibleCount)
        {
            panel.Children.Clear();

            int concealedCount = 5 - visibleCount;
            for (int i = 4; i >= 0; i--)
            {
                panel.Children.Add(tiles.ElementAt(i).GenerateTileButton(concealed: 5 - concealedCount <= i));
            }
        }

        /// <summary>
        /// Generates a panel which contains every information about the value of the hand of the specified player.
        /// </summary>
        /// <param name="p">The player informations for this round.</param>
        /// <returns>A panel with informations about hand value.</returns>
        internal static DockPanel GenerateYakusInfosPanel(this EndOfRoundInformationsPivot.PlayerInformationsPivot p)
        {
            var gridYakus = new Grid();
            gridYakus.ColumnDefinitions.Add(new ColumnDefinition());
            gridYakus.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            int i = 0;
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

            var fanLbl = new Label
            {
                VerticalAlignment = VerticalAlignment.Center,
                Content = p.FanCount
            };
            fanLbl.SetValue(Grid.RowProperty, 0);
            fanLbl.SetValue(Grid.ColumnProperty, 0);

            var fuLbl = new Label
            {
                VerticalAlignment = VerticalAlignment.Center,
                Content = p.FuCount
            };
            fuLbl.SetValue(Grid.RowProperty, 0);
            fuLbl.SetValue(Grid.ColumnProperty, 1);

            var gainLbl = new Label
            {
                VerticalAlignment = VerticalAlignment.Center,
                Content = p.PointsGain,
                Foreground = System.Windows.Media.Brushes.Red
            };
            gainLbl.SetValue(Grid.RowProperty, 0);
            gainLbl.SetValue(Grid.ColumnProperty, 2);

            var gridPoints = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Right
            };
            gridPoints.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            gridPoints.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            gridPoints.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            gridPoints.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });
            gridPoints.Children.Add(fanLbl);
            gridPoints.Children.Add(fuLbl);
            gridPoints.Children.Add(gainLbl);

            var separator = new Line
            {
                Fill = System.Windows.Media.Brushes.Black,
                Height = 2,
                Width = 200,
                VerticalAlignment = VerticalAlignment.Center
            };

            gridYakus.SetValue(DockPanel.DockProperty, Dock.Top);
            separator.SetValue(DockPanel.DockProperty, Dock.Bottom);
            gridPoints.SetValue(DockPanel.DockProperty, Dock.Bottom);

            var boxPanel = new DockPanel();
            boxPanel.Children.Add(separator);
            boxPanel.Children.Add(gridPoints);
            boxPanel.Children.Add(gridYakus);
            return boxPanel;
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
    }
}
