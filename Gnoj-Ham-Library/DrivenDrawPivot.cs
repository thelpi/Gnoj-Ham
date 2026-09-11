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
        var startIndex = (int)playerIndex * 13;

        var redDragons = new List<TilePivot>(4);
        for (var i = fullTilesList.Count - 1; i >= 0; i--)
        {
            if (fullTilesList[i].Family == Families.Dragon && fullTilesList[i].Dragon == Dragons.Red)
            {
                redDragons.Add(fullTilesList[i]);
                fullTilesList.RemoveAt(i);
            }
        }

        fullTilesList.InsertRange(startIndex, redDragons);
    }
}
