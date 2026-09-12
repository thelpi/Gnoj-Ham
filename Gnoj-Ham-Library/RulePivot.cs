using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

/// <summary>
/// Represents a set of rules.
/// </summary>
public record RulePivot
{
    #region Static properties

    // do not use directly
    private static RulePivot? _default = null;

    /// <summary>
    /// Default rules.
    /// </summary>
    public static RulePivot Default
    {
        get
        {
            _default ??= new RulePivot
            {
                InitialPointsRule = InitialPointsRules.K25,
                EndOfGameRule = EndOfGameRules.EnchousenAndTobi,
                UseRedDoras = true,
                UseNagashiMangan = true
            };

            return _default;
        }
    }

    #endregion Static properties

    #region Embedded properties

    /// <summary>
    /// Initial points rule.
    /// </summary>
    public required InitialPointsRules InitialPointsRule { get; init; }

    /// <summary>
    /// End of game rule.
    /// </summary>
    public required EndOfGameRules EndOfGameRule { get; init; }

    /// <summary>
    /// Use of red doras.
    /// </summary>
    public bool UseRedDoras { get; init; }

    /// <summary>
    /// Use of <see cref="YakuPivot.NagashiMangan"/>.
    /// </summary>
    public bool UseNagashiMangan { get; init; }

    /// <summary>
    /// Use debug mode.
    /// </summary>
    public bool DebugMode { get; init; }

    /// <summary>
    /// Use discard tip.
    /// </summary>
    public bool DiscardTip { get; init; }

    /// <summary>
    /// Allows stacking the value of several distinct yakumans made in the same hand (otherwise, only one counts).
    /// </summary>
    public bool UseMultipleYakumans { get; init; } = true;

    /// <summary>
    /// Allows "kazoe yakuman": a hand reaching 13+ fans without an actual yakuman is scored as one (otherwise, it's capped at "sanbaiman").
    /// </summary>
    public bool UseKazoeYakuman { get; init; } = true;

    /// <summary>
    /// Allows "suufon renda" (four identical wind discards on the first, uninterrupted turn): abortive draw.
    /// Not used in European competition rules.
    /// </summary>
    public bool UseSuufonRenda { get; init; } = true;

    /// <summary>
    /// Allows "double yakuman" (Suuankou tanki, Kokushi musou on a 13-sided wait, Chuuren poutou pure):
    /// worth twice a regular yakuman. Not used in European competition rules.
    /// </summary>
    public bool UseDoubleYakuman { get; init; } = true;

    /// <summary>
    /// The "uma" (rank bonus/malus) rule applied to the final score.
    /// </summary>
    public UmaRules UmaRule { get; init; } = UmaRules.FiveTen;

    #endregion Embedded properties

    #region Public methods

    /// <summary>
    /// Checks if the instance is default ruleset.
    /// </summary>
    /// <returns><c>True</c> if default ruleset.</returns>
    internal bool AreDefaultRules() => Equals(Default);

    #endregion Public methods
}
