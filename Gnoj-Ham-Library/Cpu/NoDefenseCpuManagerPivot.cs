using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

/// <summary>
/// Same heuristic as <see cref="BasicCpuManagerPivot"/>, except it never treats any opponent as a
/// threat: always pushes its own hand toward completion, regardless of riichi declarations or how
/// many combinations an opponent has exposed. A deliberately reckless baseline, meant to be
/// benchmarked against three <see cref="BasicCpuManagerPivot"/> opponents to measure how much win
/// rate defensive play is actually worth.
/// </summary>
public class NoDefenseCpuManagerPivot : BasicCpuManagerPivot
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="round">The <see cref="CpuManagerBasePivot.Round"/> value.</param>
    public NoDefenseCpuManagerPivot(RoundPivot round)
        : base(round)
    { }

    // Every defensive reflex in BasicCpuManagerPivot - discarding the safest tile instead of
    // developing the hand, declining a pon/kan/chii near a dangerous opponent - is conditioned on
    // this one method. Returning "nobody's dangerous" unconditionally switches all of it off at once.
    protected override List<PlayerIndices> GetTenpaiOpponentIndexes(PlayerIndices playerIndex) => new();
}
