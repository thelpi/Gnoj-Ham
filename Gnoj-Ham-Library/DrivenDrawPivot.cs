using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

/// <summary>
/// Hand-built manipulations of the shuffled wall, to force specific scenarios for manual testing
/// (through the real UI) without hunting for a lucky <see cref="Random"/> seed.
/// </summary>
/// <remarks>Debug/testing tool only; never used during normal play.</remarks>
public static class DrivenDrawPivot
{
    /// <summary>
    /// Resolves a <see cref="DrivenDrawScenarios"/> value into the actual wall-rigging action to pass
    /// to <see cref="GamePivot"/>, or <c>Null</c> for a normal, fully random draw.
    /// </summary>
    /// <param name="scenario">The scenario to resolve.</param>
    /// <param name="humanPlayerIndex">The human player index (every scenario targets the human).</param>
    /// <returns>The action to rig the wall; <c>Null</c> if <paramref name="scenario"/> is <see cref="DrivenDrawScenarios.None"/>.</returns>
    public static Action<List<TilePivot>>? Resolve(DrivenDrawScenarios scenario, PlayerIndices humanPlayerIndex)
    {
        return scenario switch
        {
            DrivenDrawScenarios.HumanInitialKan => fullTilesList => GuaranteeInitialKan(fullTilesList, humanPlayerIndex),
            DrivenDrawScenarios.HumanTwoInitialKans => fullTilesList => GuaranteeTwoInitialKans(fullTilesList, humanPlayerIndex),
            DrivenDrawScenarios.HumanOpenKanChance => fullTilesList => GuaranteeOpenKanChance(fullTilesList, humanPlayerIndex),
            _ => null
        };
    }

    /// <summary>
    /// Rigs the wall so the specified player's starting hand already contains all four "red dragon"
    /// tiles, guaranteeing a closed kan is declarable on their very first turn (regardless of what
    /// they draw).
    /// </summary>
    /// <param name="fullTilesList">The full shuffled wall (136 tiles), before it's dealt into hands.</param>
    /// <param name="playerIndex">The player who should get the kan opportunity.</param>
    internal static void GuaranteeInitialKan(List<TilePivot> fullTilesList, PlayerIndices playerIndex)
    {
        var startIndex = WallLayout.HandStart(playerIndex);

        var redDragons = ExtractQuad(fullTilesList, Dragons.Red);

        fullTilesList.InsertRange(startIndex, redDragons);
    }

    /// <summary>
    /// Rigs the wall so the specified player's starting hand already contains all four "red dragon"
    /// AND all four "green dragon" tiles, guaranteeing two closed kans are declarable back to back
    /// (the second right after the first's rinshan draw, before ever discarding) on their very first
    /// turn - a manual way to exercise "two kans in a row" without hunting for a lucky seed.
    /// </summary>
    /// <param name="fullTilesList">The full shuffled wall (136 tiles), before it's dealt into hands.</param>
    /// <param name="playerIndex">The player who should get both kan opportunities.</param>
    internal static void GuaranteeTwoInitialKans(List<TilePivot> fullTilesList, PlayerIndices playerIndex)
    {
        var startIndex = WallLayout.HandStart(playerIndex);

        var redDragons = ExtractQuad(fullTilesList, Dragons.Red);
        var greenDragons = ExtractQuad(fullTilesList, Dragons.Green);

        fullTilesList.InsertRange(startIndex, redDragons.Concat(greenDragons));
    }

    // The human player starts with three red dragons, and the player right before them (whose discard
    // they can call) with the fourth: the CPU has no use for a lone dragon, so it is likely to be
    // discarded early, offering the human an open kan.
    internal static void GuaranteeOpenKanChance(List<TilePivot> fullTilesList, PlayerIndices playerIndex)
    {
        var redDragons = ExtractQuad(fullTilesList, Dragons.Red);

        // Both insertions keep the wall the same size, and land in the two hands dealt.
        fullTilesList.InsertRange(WallLayout.HandStart(playerIndex), redDragons.Take(3));
        fullTilesList.Insert(WallLayout.HandStart(playerIndex.RelativePlayerIndex(-1)), redDragons[3]);
    }

    // Pulls every copy of the specified dragon out of the wall (order-preserving from the end, like
    // the original single-kan rigging), leaving fullTilesList with exactly 4 fewer tiles.
    private static List<TilePivot> ExtractQuad(List<TilePivot> fullTilesList, Dragons dragon)
    {
        var quad = new List<TilePivot>(4);
        for (var i = fullTilesList.Count - 1; i >= 0; i--)
        {
            if (fullTilesList[i].Family == Families.Dragon && fullTilesList[i].Dragon == dragon)
            {
                quad.Add(fullTilesList[i]);
                fullTilesList.RemoveAt(i);
            }
        }

        return quad;
    }
}
