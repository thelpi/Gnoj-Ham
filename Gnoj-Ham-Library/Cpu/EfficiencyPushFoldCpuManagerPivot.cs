namespace Gnoj_Ham_Library;

/// <summary>
/// Same as <see cref="EfficiencyCpuManagerPivot"/>, except for what it does against a dangerous opponent
/// with a hand that isn't tenpai: <see cref="BasicCpuManagerPivot"/> folds outright, whereas this one keeps
/// on developing a hand that's at most <see cref="PushShanten"/> tiles from tenpai - discarding, among the
/// tiles that keep it there, the safest - and only folds a hand that's farther from it. Meant to be
/// benchmarked against three <see cref="EfficiencyCpuManagerPivot"/> opponents to measure how much win
/// rate pushing such hands is worth.
/// </summary>
public class EfficiencyPushFoldCpuManagerPivot : EfficiencyCpuManagerPivot
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="round">The <see cref="CpuManagerBasePivot.Round"/> value.</param>
    public EfficiencyPushFoldCpuManagerPivot(RoundPivot round)
        : base(round)
    { }

    // Up to how many tiles from tenpai (see ShantenCalculatorPivot) a hand is worth pushing against a
    // dangerous opponent: 1 is an iishanten hand (a tenpai one is always pushed, whatever the variant).
    protected virtual int PushShanten => 1;

    protected override TilePivot FoldDiscardDecision(
        IReadOnlyList<TilePivot> concealedTiles,
        List<TilePivot> discardableTiles,
        IReadOnlyList<(TilePivot tile, int unsafePoints)> tilesSafety,
        IReadOnlyList<TilePivot> deadTiles)
    {
        var evaluations = EvaluateDiscards(concealedTiles, discardableTiles, deadTiles);
        var bestShanten = evaluations.Min(e => e.Shanten);

        if (bestShanten > PushShanten)
        {
            return base.FoldDiscardDecision(concealedTiles, discardableTiles, tilesSafety, deadTiles);
        }

        // Among the discards that keep the hand as close to tenpai as it can be: the safest, then the one
        // that leaves the most tiles to draw.
        var unsafePointsByTile = tilesSafety.ToDictionary(s => s.tile, s => s.unsafePoints);
        return evaluations
            .Where(e => e.Shanten == bestShanten)
            .OrderBy(e => unsafePointsByTile[e.Tile])
            .ThenByDescending(e => e.Ukeire)
            .First()
            .Tile;
    }
}
