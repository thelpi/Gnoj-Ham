using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

/// <summary>
/// Same heuristic as <see cref="BasicCpuManagerPivot"/>, except it never risks anything once an
/// opponent looks dangerous (riichi, or 3+ declared combinations - see
/// <see cref="BasicCpuManagerPivot.GetTenpaiOpponentIndexes"/>, unchanged here): it folds to the
/// safest discard even if that means breaking an already-reached tenpai, and never calls pon or kan
/// in that situation either (chii is already always declined against a dangerous opponent in the base
/// class, tenpai or not). A deliberately over-cautious baseline, meant to be benchmarked against three
/// <see cref="BasicCpuManagerPivot"/> opponents the same way as <see cref="NoDefenseCpuManagerPivot"/>.
/// </summary>
public class FullDefenseCpuManagerPivot : BasicCpuManagerPivot
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="round">The <see cref="CpuManagerBasePivot.Round"/> value.</param>
    public FullDefenseCpuManagerPivot(RoundPivot round)
        : base(round)
    { }

    protected override bool AbandonsHandEvenIfTenpai => true;

    protected override bool PonDecisionInternal(PlayerIndices playerIndex)
        => GetTenpaiOpponentIndexes(playerIndex).Count == 0 && base.PonDecisionInternal(playerIndex);

    protected override TilePivot? KanDecisionInternal(PlayerIndices playerIndex, IReadOnlyList<TilePivot> kanPossibilities, bool concealed)
        => GetTenpaiOpponentIndexes(playerIndex).Count > 0 ? null : base.KanDecisionInternal(playerIndex, kanPossibilities, concealed);
}
