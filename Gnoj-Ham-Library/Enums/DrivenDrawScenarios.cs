namespace Gnoj_Ham_Library.Enums;

/// <summary>
/// Enumeration of hand-built wall scenarios, for manual testing through the real UI without hunting for a lucky <see cref="Random"/> seed.
/// </summary>
public enum DrivenDrawScenarios
{
    /// <summary>
    /// Normal, fully random draw.
    /// </summary>
    None,
    /// <summary>
    /// The human player's starting hand already contains four "red dragon" tiles: a closed kan is declarable on their very first turn.
    /// </summary>
    HumanInitialKan,
    /// <summary>
    /// The human player's starting hand already contains four "red dragon" AND four "green dragon" tiles: two closed kans are declarable back to back on their very first turn.
    /// </summary>
    HumanTwoInitialKans,
    /// <summary>
    /// The human player's starting hand contains three "red dragon" tiles, and the player right before them holds the fourth: if that player discards it, an open kan is callable.
    /// </summary>
    HumanOpenKanChance
}
