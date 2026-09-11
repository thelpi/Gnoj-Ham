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
    HumanInitialKan
}
