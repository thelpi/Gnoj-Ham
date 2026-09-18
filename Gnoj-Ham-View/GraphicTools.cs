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
    /// Transforms the enumeration <see cref="DrivenDrawScenarios"/> into a list of <see cref="string"/> (with matching index).
    /// </summary>
    /// <returns>List of strings ready for display.</returns>
    internal static IReadOnlyList<string> GetDrivenDrawScenarioDisplayValue()
    {
        var results = new List<string>();

        foreach (var scenario in Enum.GetValues<DrivenDrawScenarios>())
        {
            switch (scenario)
            {
                case DrivenDrawScenarios.None:
                    results.Add("Aucun");
                    break;
                case DrivenDrawScenarios.HumanInitialKan:
                    results.Add("Kan possible au 1er tour");
                    break;
                case DrivenDrawScenarios.HumanTwoInitialKans:
                    results.Add("2 Kans possibles au 1er tour");
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
    /// Transforms the enumeration <see cref="UmaRules"/> into a list of <see cref="string"/> (with matching index).
    /// </summary>
    /// <returns>List of strings ready for display.</returns>
    internal static IReadOnlyList<string> GetUmaRuleDisplayValue()
    {
        var results = new List<string>();

        foreach (var rule in Enum.GetValues<UmaRules>())
        {
            switch (rule)
            {
                case UmaRules.FiveTen:
                    results.Add("5 / 10");
                    break;
                case UmaRules.TenTwenty:
                    results.Add("10 / 20");
                    break;
                case UmaRules.Ema:
                    results.Add("EMA (15 / 5)");
                    break;
            }
        }

        return results;
    }

    #endregion Enum converters
}
