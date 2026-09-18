using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_ViewModel;

/// <summary>
/// French display texts for engine values. Plain strings, so they live here rather than in the view.
/// </summary>
public static class DisplayTexts
{
    /// <summary>
    /// Computes the (french) name to display for the family.
    /// </summary>
    /// <param name="family">Family.</param>
    /// <returns>French display name.</returns>
    public static string DisplayName(this Families family)
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
    public static string DisplayName(this Dragons dragon)
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
    public static string DisplayName(this Winds wind)
    {
        return wind switch
        {
            Winds.East => "Est",
            Winds.South => "Sud",
            Winds.West => "Ouest",
            _ => "Nord",
        };
    }

    /// <summary>
    /// Transforms a <see cref="ChronoPivot"/> value into its french representation.
    /// </summary>
    /// <param name="chrono">The chrono value.</param>
    /// <returns>French representation.</returns>
    public static string DisplayName(this ChronoPivot chrono)
    {
        return chrono switch
        {
            ChronoPivot.Long => "Long",
            ChronoPivot.Short => "Court",
            _ => "Aucun",
        };
    }

    /// <summary>
    /// Transforms the enumeration <see cref="CpuSpeedPivot"/> into a list of <see cref="string"/> (with matching index).
    /// </summary>
    /// <returns>List of strings ready for display.</returns>
    public static IReadOnlyList<string> GetCpuSpeedDisplayValues()
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
    public static IReadOnlyList<string> GetChronoDisplayValues()
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
    public static IReadOnlyList<string> GetEndOfGameRuleDisplayValue()
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
    public static IReadOnlyList<string> GetDrivenDrawScenarioDisplayValue()
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
    public static IReadOnlyList<string> GetInitialPointsRuleDisplayValue()
    {
        return Enum.GetValues<InitialPointsRules>()
                .Select(v => $"{Convert.ToInt32(v.ToString().Replace("K", string.Empty))} 000")
                .ToList();
    }

    /// <summary>
    /// Transforms the enumeration <see cref="UmaRules"/> into a list of <see cref="string"/> (with matching index).
    /// </summary>
    /// <returns>List of strings ready for display.</returns>
    public static IReadOnlyList<string> GetUmaRuleDisplayValue()
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

    /// <summary>
    /// Provides a textual representation of a tile, for a tooltip.
    /// </summary>
    /// <param name="tile">The tile.</param>
    /// <returns>Textual representation of the tile.</returns>
    public static string TileDisplay(this TilePivot tile)
    {
        return tile.Family switch
        {
            Families.Dragon => $"{tile.Family.DisplayName()}\r\n{tile.Dragon!.Value.DisplayName()}",
            Families.Wind => $"{tile.Family.DisplayName()}\r\n{tile.Wind!.Value.DisplayName()}",
            _ => $"{tile.Family.DisplayName()}\r\n{tile.Number}" + (tile.IsRedDora ? "\r\nRouge" : string.Empty),
        };
    }
}
