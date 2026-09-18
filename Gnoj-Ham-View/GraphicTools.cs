using System.Windows;
using System.Windows.Controls;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;
using Gnoj_Ham_ViewModel;

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
}
