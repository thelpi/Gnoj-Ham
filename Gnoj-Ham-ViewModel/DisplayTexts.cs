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
