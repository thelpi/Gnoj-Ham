using System.Windows;
using System.Windows.Controls;
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
}
