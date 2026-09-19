using Gnoj_Ham_Library;

namespace Gnoj_Ham_ViewModel;

/// <summary>
/// Lays out the (dora or ura-dora) indicator tiles the way every window displays them.
/// </summary>
internal static class DoraIndicatorTiles
{
    private const int IndicatorsCount = 5;

    /// <summary>
    /// Indicators are laid out from the last one down to the first: the first (the one revealed
    /// initially) ends up rightmost, and the ones past the visible count are face down.
    /// </summary>
    /// <param name="tiles">The five indicator tiles.</param>
    /// <param name="visibleCount">The number of revealed indicators.</param>
    /// <returns>The tiles, in display order.</returns>
    internal static IReadOnlyList<TileViewModel> Build(IReadOnlyList<TilePivot> tiles, int visibleCount)
    {
        var result = new List<TileViewModel>(IndicatorsCount);
        for (var i = IndicatorsCount - 1; i >= 0; i--)
        {
            result.Add(new TileViewModel(tiles[i], isConcealed: visibleCount <= i));
        }

        return result;
    }
}
