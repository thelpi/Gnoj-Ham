namespace Gnoj_Ham_Library;

/// <summary>
/// Same as <see cref="BasicCpuManagerPivot"/>, except for how it develops a hand that is neither
/// tenpai nor given up on: it discards the tile that leaves the hand the closest to tenpai (the lowest
/// shanten number, see <see cref="ShantenCalculatorPivot"/>) and, among those, the one that leaves the
/// most tiles still to draw that bring it closer (the "ukeire"). Only what's still tied is left to the
/// general ranking of <see cref="BasicCpuManagerPivot"/>. Meant to be benchmarked against three
/// <see cref="BasicCpuManagerPivot"/> opponents to measure how much win rate the tile efficiency is worth.
/// </summary>
public class EfficiencyCpuManagerPivot : BasicCpuManagerPivot
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="round">The <see cref="CpuManagerBasePivot.Round"/> value.</param>
    public EfficiencyCpuManagerPivot(RoundPivot round)
        : base(round)
    { }

    /// <summary>
    /// What a hand is worth once a tile is discarded.
    /// </summary>
    /// <param name="Tile">The tile discarded.</param>
    /// <param name="Shanten">How far the hand is from tenpai (see <see cref="ShantenCalculatorPivot"/>).</param>
    /// <param name="Ukeire">How many of the tiles still to draw bring the hand closer to tenpai.</param>
    protected readonly record struct DiscardEvaluation(TilePivot Tile, int Shanten, int Ukeire);

    protected override TilePivot DevelopmentDiscardDecision(
        IReadOnlyList<TilePivot> concealedTiles,
        List<TilePivot> discardableTiles,
        IReadOnlyList<TilePivot> deadTiles)
    {
        var evaluations = EvaluateDiscards(concealedTiles, discardableTiles, deadTiles);

        var best = evaluations
            .OrderBy(e => e.Shanten)
            .ThenByDescending(e => e.Ukeire)
            .First();

        return base.DevelopmentDiscardDecision(
            concealedTiles,
            evaluations.Where(e => e.Shanten == best.Shanten && e.Ukeire == best.Ukeire).Select(e => e.Tile).ToList(),
            deadTiles);
    }

    /// <summary>
    /// Evaluates each possible discard of the current player.
    /// </summary>
    /// <param name="concealedTiles">The concealed tiles of the current player.</param>
    /// <param name="discardableTiles">The tiles that can be discarded, of distinct kinds.</param>
    /// <param name="deadTiles">The tiles in sight of the current player (see <see cref="RoundPivot.DeadTilesFromIndexPointOfView"/>).</param>
    /// <returns>One evaluation per tile.</returns>
    protected List<DiscardEvaluation> EvaluateDiscards(
        IReadOnlyList<TilePivot> concealedTiles,
        IReadOnlyList<TilePivot> discardableTiles,
        IReadOnlyList<TilePivot> deadTiles)
    {
        var declaredCombinationsCount = Round.GetHand(Round.CurrentPlayerIndex).DeclaredCombinations.Count;

        // Every tile in sight is out of the draw: what's left of a kind is what could still come to us.
        var liveCopies = new int[TilePivot.KindsCount];
        Array.Fill(liveCopies, TilePivot.CopiesCount);
        foreach (var deadTile in deadTiles)
        {
            liveCopies[deadTile.KindIndex]--;
        }

        var kindCounts = new int[TilePivot.KindsCount];
        foreach (var tile in concealedTiles)
        {
            kindCounts[tile.KindIndex]++;
        }

        return discardableTiles
            .Select(tile => EvaluateDiscard(tile, kindCounts, declaredCombinationsCount, liveCopies))
            .ToList();
    }

    private static DiscardEvaluation EvaluateDiscard(TilePivot tile, int[] kindCounts, int declaredCombinationsCount, int[] liveCopies)
    {
        kindCounts[tile.KindIndex]--;

        var shanten = ShantenCalculatorPivot.Compute(kindCounts, declaredCombinationsCount);

        var ukeire = 0;
        for (var kind = 0; kind < kindCounts.Length; kind++)
        {
            if (liveCopies[kind] <= 0)
            {
                continue;
            }

            kindCounts[kind]++;
            if (ShantenCalculatorPivot.Compute(kindCounts, declaredCombinationsCount) < shanten)
            {
                ukeire += liveCopies[kind];
            }
            kindCounts[kind]--;
        }

        kindCounts[tile.KindIndex]++;
        return new DiscardEvaluation(tile, shanten, ukeire);
    }
}
