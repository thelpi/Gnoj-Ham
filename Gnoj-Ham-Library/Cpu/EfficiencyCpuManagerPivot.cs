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

    protected override TilePivot DevelopmentDiscardDecision(
        IReadOnlyList<TilePivot> concealedTiles,
        List<TilePivot> discardableTiles,
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

        var candidates = discardableTiles
            .Select(tile => (tile, choice: EvaluateDiscard(tile, kindCounts, declaredCombinationsCount, liveCopies)))
            .ToList();

        var best = candidates
            .OrderBy(c => c.choice.Shanten)
            .ThenByDescending(c => c.choice.Ukeire)
            .First()
            .choice;

        return base.DevelopmentDiscardDecision(
            concealedTiles,
            candidates.Where(c => c.choice.Shanten == best.Shanten && c.choice.Ukeire == best.Ukeire).Select(c => c.tile).ToList(),
            deadTiles);
    }

    // What the hand is worth once the tile is thrown: how far it is from tenpai, and how many of the
    // tiles still to draw bring it closer.
    private static (int Shanten, int Ukeire) EvaluateDiscard(TilePivot tile, int[] kindCounts, int declaredCombinationsCount, int[] liveCopies)
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
        return (shanten, ukeire);
    }
}
