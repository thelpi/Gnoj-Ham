﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_View;

/// <summary>
/// Graphic tools.
/// </summary>
internal static class GraphicTools
{
    /// <summary>
    /// The table size (width and height).
    /// </summary>
    internal const int EXPECTED_TABLE_SIZE = 920;

    /// <summary>
    /// Extension; resets a panel filled with dora tiles.
    /// </summary>
    /// <param name="panel">The panel.</param>
    /// <param name="tiles">List of dora tiles.</param>
    /// <param name="visibleCount">Number of tiles not concealed.</param>
    internal static void SetDorasPanel(this StackPanel panel, IReadOnlyList<TilePivot> tiles, int visibleCount)
    {
        panel.Children.Clear();

        var concealedCount = 5 - visibleCount;
        for (var i = 4; i >= 0; i--)
        {
            panel.Children.Add(new TileButton(tiles[i], concealed: 5 - concealedCount <= i, rate: 0.8));
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
    /// Gets a japanese caracter which represents the specified wind.
    /// </summary>
    /// <param name="wind">The wind to display.</param>
    /// <returns>The associated japanese caracter.</returns>
    /// <exception cref="NotImplementedException">The wind is not implemented.</exception>
    internal static string ToWindDisplay(this Winds wind)
    {
        return wind switch
        {
            Winds.East => "東",
            Winds.South => "南",
            Winds.West => "西",
            Winds.North => "北",
            _ => throw new NotImplementedException(),
        };
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
        return chrono switch
        {
            ChronoPivot.Long => 20,
            ChronoPivot.Short => 5,
            _ => 0,
        };
    }

    /// <summary>
    /// Transforms a <see cref="ChronoPivot"/> value into its french representation.
    /// </summary>
    /// <param name="chrono">The chrono value.</param>
    /// <returns>French representation.</returns>
    internal static string DisplayName(this ChronoPivot chrono)
    {
        return chrono switch
        {
            ChronoPivot.Long => "Long",
            ChronoPivot.Short => "Court",
            _ => "Aucun",
        };
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
    internal static T FindName<T>(this Window window, string nameWithoutIndex, PlayerIndices playerIndex) where T : FrameworkElement
    {
        return (window.FindName(string.Concat(nameWithoutIndex, (int)playerIndex)) as T)!;
    }

    /// <summary>
    /// Extension; retrieves a <see cref="ContentControl"/> from a <see cref="Window"/> by its name and the player index.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="nameWithoutIndex">The control name without the player index.</param>
    /// <param name="playerIndex">The player index.</param>
    /// <returns>The control.</returns>
    internal static ContentControl FindControl(this Window window, string nameWithoutIndex, PlayerIndices playerIndex)
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
    internal static Panel FindPanel(this Window window, string nameWithoutIndex, PlayerIndices playerIndex)
    {
        return window.FindName<Panel>(nameWithoutIndex, playerIndex);
    }

    /// <summary>
    /// Overriden; provides a textual representation of the instance.
    /// </summary>
    /// <returns>Textual representation of the instance.</returns>
    internal static string TileDisplay(this TilePivot tile)
    {
        return tile.Family switch
        {
            Families.Dragon => $"{tile.Family.DisplayName()}\r\n{tile.Dragon!.Value.DisplayName()}",
            Families.Wind => $"{tile.Family.DisplayName()}\r\n{tile.Wind!.Value.DisplayName()}",
            _ => $"{tile.Family.DisplayName()}\r\n{tile.Number}" + (tile.IsRedDora ? "\r\nRouge" : string.Empty),
        };
    }

    /// <summary>
    /// Creates a panel for a <see cref="TileComboPivot"/>
    /// </summary>
    /// <param name="combo"></param>
    /// <param name="pIndex"></param>
    /// <param name="playerWind"></param>
    /// <returns></returns>
    internal static StackPanel CreateCombinationPanel(this TileComboPivot combo, PlayerIndices pIndex, Winds playerWind)
    {
        var panel = new StackPanel
        {
            Orientation = pIndex == PlayerIndices.Zero || pIndex == PlayerIndices.Two
                ? Orientation.Horizontal
                : Orientation.Vertical
        };

        var i = 0;
        var tileTuples = combo.GetSortedTilesForDisplay(playerWind).AsEnumerable();
        if (pIndex > PlayerIndices.Zero && pIndex < PlayerIndices.Three)
        {
            tileTuples = tileTuples.Reverse();
        }

        foreach (var (tile, stolen) in tileTuples)
        {
            panel.Children.Add(new TileButton(tile, null,
                (AnglePivot)(stolen ? pIndex.RelativePlayerIndex(1) : pIndex),
                combo.IsConcealedDisplay(i)));
            i++;
        }

        return panel;
    }

    #region Enum converters

    /// <summary>
    /// Transforms the enumeration <see cref="CpuSpeedPivot"/> into a list of <see cref="string"/> (with matching index).
    /// </summary>
    /// <returns>List of strings ready for display.</returns>
    internal static IReadOnlyList<string> GetCpuSpeedDisplayValues()
    {
        return Enum.GetValues<CpuSpeedPivot>().Select(v =>
        {
            var intParsedValue = v.ParseSpeed();

            return intParsedValue >= 1000 ? $"{intParsedValue / 1000} sec" : $"{intParsedValue} ms";
        }).ToList();
    }

    /// <summary>
    /// Transforms the enumeration <see cref="ChronoPivot"/> into a list of <see cref="string"/> (with matching index).
    /// </summary>
    /// <returns>List of strings ready for display.</returns>
    internal static IReadOnlyList<string> GetChronoDisplayValues()
    {
        var results = new List<string>();

        foreach (var ch in Enum.GetValues<ChronoPivot>())
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
    /// Transforms the enumeration <see cref="EndOfGameRules"/> into a list of <see cref="string"/> (with matching index).
    /// </summary>
    /// <returns>List of strings ready for display.</returns>
    internal static IReadOnlyList<string> GetEndOfGameRuleDisplayValue()
    {
        var results = new List<string>();

        foreach (var rule in Enum.GetValues<EndOfGameRules>())
        {
            switch (rule)
            {
                case EndOfGameRules.Enchousen:
                    results.Add("Enchousen");
                    break;
                case EndOfGameRules.EnchousenAndTobi:
                    results.Add("Enchousen + Tobi");
                    break;
                case EndOfGameRules.Oorasu:
                    results.Add("Oorasu");
                    break;
                case EndOfGameRules.Tobi:
                    results.Add("Tobi");
                    break;
            }
        }

        return results;
    }

    /// <summary>
    /// Transforms the enumeration <see cref="InitialPointsRules"/> into a list of <see cref="string"/> (with matching index).
    /// </summary>
    /// <returns>List of strings ready for display.</returns>
    internal static IReadOnlyList<string> GetInitialPointsRuleDisplayValue()
    {
        return Enum.GetValues<InitialPointsRules>()
                .Select(v => $"{Convert.ToInt32(v.ToString().Replace("K", string.Empty))} 000")
                .ToList();
    }

    /// <summary>
    /// Computes the (french) name to display for the family.
    /// </summary>
    /// <param name="family">Family.</param>
    /// <returns>French display name.</returns>
    internal static string DisplayName(this Families family)
    {
        return family switch
        {
            Families.Bamboo => "Bambou",
            Families.Dragon => "Dragon",
            Families.Circle => "Cercle",
            Families.Caracter => "Caractère",
            _ => "Vent",
        };
    }

    /// <summary>
    /// Computes the (french) name to display for the dragon.
    /// </summary>
    /// <param name="dragon">Dragon.</param>
    /// <returns>French display name.</returns>
    internal static string DisplayName(this Dragons dragon)
    {
        return dragon switch
        {
            Dragons.Red => "Rouge",
            Dragons.White => "Blanc",
            _ => "Vert",
        };
    }

    /// <summary>
    /// Computes the (french) name to display for the wind.
    /// </summary>
    /// <param name="wind">Wind.</param>
    /// <returns>French display name.</returns>
    internal static string DisplayName(this Winds wind)
    {
        return wind switch
        {
            Winds.East => "Est",
            Winds.South => "Sud",
            Winds.West => "Ouest",
            _ => "Nord",
        };
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

        var fuLbl = FuLabelForScoreDisplay(p);
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

    private static Line? SeparatorForScoreDisplay(EndOfRoundInformationsPivot.PlayerInformationsPivot p)
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

    private static Label? FuLabelForScoreDisplay(EndOfRoundInformationsPivot.PlayerInformationsPivot p)
    {
        if (p.Yakus == null || p.Yakus.Count == 0)
        {
            return null;
        }

        var fuLbl = new Label
        {
            VerticalAlignment = VerticalAlignment.Center,
            Content = $"{p.FuCount} fu"
        };
        fuLbl.SetValue(Grid.RowProperty, 0);
        fuLbl.SetValue(Grid.ColumnProperty, 1);
        return fuLbl;
    }

    private static Label? FanlabelForScoreDisplay(EndOfRoundInformationsPivot.PlayerInformationsPivot p)
    {
        if (p.Yakus == null || p.Yakus.Count == 0)
        {
            return null;
        }

        var fanLbl = new Label
        {
            VerticalAlignment = VerticalAlignment.Center,
            Content = $"{p.FanCount} fan"
        };
        fanLbl.SetValue(Grid.RowProperty, 0);
        fanLbl.SetValue(Grid.ColumnProperty, 0);
        return fanLbl;
    }

    private static Grid? YakusGridForScoreDisplay(EndOfRoundInformationsPivot.PlayerInformationsPivot p)
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
            Height = TileButton.TILE_HEIGHT + (0.5 * TileButton.DEFAULT_TILE_MARGIN),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        foreach (var (tile, leaned, apart) in p.GetFullHandForDisplay())
        {
            var b = new TileButton(tile, null, leaned ? AnglePivot.A90 : AnglePivot.A0, false);
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
