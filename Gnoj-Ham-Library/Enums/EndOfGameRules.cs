namespace Gnoj_Ham_Library.Enums;

/// <summary>
/// Enumeration of rules to end a game.
/// </summary>
public enum EndOfGameRules
{
    /// <summary>
    /// Game ends after south 4.
    /// </summary>
    Oorasu,
    /// <summary>
    /// Game ends at the first player under <c>0</c> points, or at <see cref="Oorasu"/>.
    /// </summary>
    Tobi,
    /// <summary>
    /// Game ends after south 4, except if the point rule is <see cref="InitialPointsRules.K25"/> and no player is equal or above <c>30000</c> points.
    /// In such case, the game continues (West, the North) until someone reach <c>30000</c> points.
    /// Still in such case, game cannot end on "Ryuukyoku".
    /// </summary>
    Enchousen,
    /// <summary>
    /// Combination of rules <see cref="Enchousen"/> and <see cref="Tobi"/>.
    /// </summary>
    EnchousenAndTobi
}
