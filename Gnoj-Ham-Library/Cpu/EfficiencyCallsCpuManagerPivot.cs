using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

/// <summary>
/// Same as <see cref="EfficiencyPushFoldCpuManagerPivot"/>, except for the pon and chii calls: it makes the
/// ones <see cref="BasicCpuManagerPivot"/> would make (the yaku and the defense they're gated on are left
/// to it), but only if they bring the hand closer to tenpai (see <see cref="ShantenCalculatorPivot"/>) -
/// once the discard the call requires is made - and, for a chii, it picks the sequence that leaves the
/// hand the closest. Meant to be benchmarked against three <see cref="EfficiencyPushFoldCpuManagerPivot"/>
/// opponents to measure how much win rate calling only when it helps is worth.
/// </summary>
public class EfficiencyCallsCpuManagerPivot : EfficiencyPushFoldCpuManagerPivot
{
    // Whatever the shanten, a sequence which doesn't get the hand closer is worth less than one which does.
    private const int ShantenWeight = 100;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="round">The <see cref="CpuManagerBasePivot.Round"/> value.</param>
    public EfficiencyCallsCpuManagerPivot(RoundPivot round)
        : base(round)
    { }

    protected override bool PonDecisionInternal(PlayerIndices playerIndex)
    {
        if (!base.PonDecisionInternal(playerIndex))
        {
            return false;
        }

        var hand = Round.GetHand(playerIndex);
        var calledKind = CalledTile().KindIndex;

        return ShantenAfterCall(hand, calledKind, calledKind, calledKind) < ShantenNow(hand);
    }

    protected override int ChiiCost(TilePivot tileKey, TilePivot companion)
    {
        var hand = Round.GetHand(Round.CurrentPlayerIndex);
        var shanten = ShantenAfterCall(hand, CalledTile().KindIndex, tileKey.KindIndex, companion.KindIndex);

        return shanten < ShantenNow(hand)
            ? (shanten * ShantenWeight) + base.ChiiCost(tileKey, companion)
            : int.MaxValue;
    }

    // The tile just discarded by the previous player, which a call would take.
    private TilePivot CalledTile()
        => Round.GetDiscard(Round.PreviousPlayerIndex)[^1];

    private static int ShantenNow(HandPivot hand)
        => ShantenCalculatorPivot.Compute(hand.ConcealedTiles, hand.DeclaredCombinations.Count);

    // How far from tenpai the hand is once it has called - spending these tiles of its own, along with the
    // one called - and discarded the best tile it can. The kind called can't be discarded (the kuikae rule);
    // neither can the one on the other side of a sequence, but the hands that hold it are the ones
    // BasicCpuManagerPivot doesn't call a chii for anyway (they'd break a run).
    private static int ShantenAfterCall(HandPivot hand, int calledKind, params int[] spentKinds)
    {
        var kindCounts = new int[TilePivot.KindsCount];
        foreach (var tile in hand.ConcealedTiles)
        {
            kindCounts[tile.KindIndex]++;
        }
        foreach (var kind in spentKinds)
        {
            kindCounts[kind]--;
        }

        var declaredCombinationsCount = hand.DeclaredCombinations.Count + 1;

        var best = int.MaxValue;
        for (var kind = 0; kind < kindCounts.Length; kind++)
        {
            if (kindCounts[kind] == 0 || kind == calledKind)
            {
                continue;
            }

            kindCounts[kind]--;
            best = Math.Min(best, ShantenCalculatorPivot.Compute(kindCounts, declaredCombinationsCount));
            kindCounts[kind]++;
        }

        return best;
    }
}
