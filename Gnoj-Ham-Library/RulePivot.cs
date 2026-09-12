using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

/// <summary>
/// Represents a set of rules.
/// </summary>
public class RulePivot : IEquatable<RulePivot>
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
    /// Not used in European competition rules, hence off by default.
    /// </summary>
    public bool UseSuufonRenda { get; init; }

    #endregion Embedded properties

    #region Public methods

    /// <summary>
    /// Checks if the instance is default ruleset.
    /// </summary>
    /// <returns><c>True</c> if default ruleset.</returns>
    internal bool AreDefaultRules() => Equals(Default);

    #endregion Public methods

    #region IEquatable implementation

    /// <inheritdoc />
    public bool Equals(RulePivot? other)
    {
        return other != null
            && other.UseNagashiMangan == UseNagashiMangan
            && other.InitialPointsRule == InitialPointsRule
            && other.EndOfGameRule == EndOfGameRule
            && other.UseRedDoras == UseRedDoras
            && other.DebugMode == DebugMode
            && other.DiscardTip == DiscardTip
            && other.UseMultipleYakumans == UseMultipleYakumans
            && other.UseKazoeYakuman == UseKazoeYakuman
            && other.UseSuufonRenda == UseSuufonRenda;
    }

    public override bool Equals(object? obj)
    {
        return obj != null && Equals(obj as RulePivot);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(UseNagashiMangan);
        hash.Add(InitialPointsRule);
        hash.Add(EndOfGameRule);
        hash.Add(UseRedDoras);
        hash.Add(DebugMode);
        hash.Add(DiscardTip);
        hash.Add(UseMultipleYakumans);
        hash.Add(UseKazoeYakuman);
        hash.Add(UseSuufonRenda);
        return hash.ToHashCode();
    }

    #endregion IEquatable implementation
}
