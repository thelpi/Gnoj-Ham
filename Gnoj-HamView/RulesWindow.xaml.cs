using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;
using Gnoj_Ham;

namespace Gnoj_HamView
{
    /// <summary>
    /// Logique d'interaction pour YakusWindow.xaml
    /// </summary>
    public partial class RulesWindow : Window
    {
        /// <summary>
        /// Ctor
        /// </summary>
        public RulesWindow()
        {
            InitializeComponent();

            var yakusSorted = YakuPivot.Yakus
                .Except(new[] { YakuPivot.NagashiMangan })
                .OrderBy(x => x.ConcealedFanCount)
                .ThenBy(x => x.FanCount)
                .ThenBy(x => x.Name);

            var i = 1;
            foreach (var yaku in yakusSorted)
            {
                GrdYakus.RowDefinitions.Add(new RowDefinition());

                var rowBrush = i % 2 == 0 ? Brushes.Azure : Brushes.White;
                var fontStyle = yaku.FanCount == 0 ? FontStyles.Italic : FontStyles.Normal;

                var nameLabel = new Label
                {
                    Content = yaku.Name,
                    Background = rowBrush,
                    FontWeight = FontWeights.Bold,
                    FontStyle = fontStyle
                };
                nameLabel.SetValue(Grid.RowProperty, i);
                nameLabel.SetValue(Grid.ColumnProperty, 0);

                var fansText = yaku.ConcealedFanCount.ToString();
                var toolTip = "Concealed only.";
                if (yaku.FanCount > 0)
                {
                    toolTip = null;
                    if (yaku.ConcealedBonusFanCount > 0)
                    {
                        fansText = $"{yaku.FanCount} (+{yaku.ConcealedBonusFanCount})";
                        toolTip = "Bonus if concealed.";
                    }
                }

                var fansLabel = new Label
                {
                    Content = fansText,
                    Background = rowBrush,
                    FontWeight = FontWeights.Bold,
                    FontStyle = fontStyle,
                    ToolTip = toolTip
                };
                fansLabel.SetValue(Grid.RowProperty, i);
                fansLabel.SetValue(Grid.ColumnProperty, 1);

                var descriptionLabel = new Label
                {
                    Content = yaku.Description,
                    Background = rowBrush,
                    FontStyle = fontStyle
                };
                descriptionLabel.SetValue(Grid.RowProperty, i);
                descriptionLabel.SetValue(Grid.ColumnProperty, 2);

                i++;

                GrdYakus.Children.Add(nameLabel);
                GrdYakus.Children.Add(fansLabel);
                GrdYakus.Children.Add(descriptionLabel);
            }

            GrdYakus.RowDefinitions.Add(new RowDefinition());

            var nameLabelNagashiMangan = new Label
            {
                Content = YakuPivot.NagashiMangan.Name,
                FontWeight = FontWeights.Bold,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = Brushes.Cornsilk
            };
            nameLabelNagashiMangan.SetValue(Grid.RowProperty, i);
            nameLabelNagashiMangan.SetValue(Grid.ColumnProperty, 0);

            var fansLabelNagashiMangan = new Label
            {
                Content = YakuPivot.NagashiMangan.ConcealedFanCount,
                FontWeight = FontWeights.Bold,
                VerticalContentAlignment = VerticalAlignment.Center,
                Background = Brushes.Cornsilk
            };
            fansLabelNagashiMangan.SetValue(Grid.RowProperty, i);
            fansLabelNagashiMangan.SetValue(Grid.ColumnProperty, 1);

            var descriptionLabelNagashiMangan = new Label
            {
                Content = YakuPivot.NagashiMangan.Description.Replace("\n", Environment.NewLine),
                Background = Brushes.Cornsilk
            };
            descriptionLabelNagashiMangan.SetValue(Grid.RowProperty, i);
            descriptionLabelNagashiMangan.SetValue(Grid.ColumnProperty, 2);

            GrdYakus.Children.Add(nameLabelNagashiMangan);
            GrdYakus.Children.Add(fansLabelNagashiMangan);
            GrdYakus.Children.Add(descriptionLabelNagashiMangan);
        }
    }
}
