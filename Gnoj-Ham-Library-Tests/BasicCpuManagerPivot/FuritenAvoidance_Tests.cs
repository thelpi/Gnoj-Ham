using System.Reflection;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class FuritenAvoidance_Tests
{
    private static RoundPivot NewRound(int seed)
        => new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(seed)).Round;

    private static void SetHand(RoundPivot round, PlayerIndices playerIndex, List<TilePivot> concealedTiles)
    {
        var hands = (List<HandPivot>)typeof(RoundPivot)
            .GetField("_hands", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(round)!;
        hands[(int)playerIndex] = new HandPivot(concealedTiles);
    }

    private static void SetWaitForDiscard(RoundPivot round, bool value)
    {
        typeof(RoundPivot)
            .GetField("_waitForDiscard", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(round, value);
    }

    private static void AddToDiscard(RoundPivot round, PlayerIndices playerIndex, TilePivot tile)
    {
        var discardHistory = typeof(RoundPivot)
            .GetField("_discardHistory", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(round)!;
        var discards = (List<List<TilePivot>>)typeof(DiscardHistoryPivot)
            .GetField("_discards", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(discardHistory)!;
        discards[(int)playerIndex].Add(tile);
    }

    // Tops up the (real, untouched) wall with extra concealed copies of the given tiles, so that -
    // regardless of how this seed's actual deal happened to distribute the real 4 copies of each kind -
    // DeadTilesFromIndexPointOfView reports 0 dead copies for them: a real discard removes a tile from
    // its owner's ConcealedTiles (shrinking what's concealed), but AddToDiscard above only appends to
    // the discard log and deliberately leaves hands/wall untouched (it exists purely to control furiten,
    // which reads the discard log directly) - so it has no effect on dead-tile counts by itself.
    private static void KeepFullyConcealed(RoundPivot round, params TilePivot[] tiles)
    {
        var wallTiles = (List<TilePivot>)typeof(RoundPivot)
            .GetField("_wallTiles", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(round)!;
        foreach (var tile in tiles)
        {
            wallTiles.AddRange(Enumerable.Repeat(tile, 4));
        }
    }

    // Builds a hand with exactly two tenpai-preserving discards, each leaving a different tanki
    // (single-tile) wait: 123m 456m 789m 123p are four complete RUNS already (a run, unlike a triplet,
    // can't be reinterpreted as "pair plus a discard" - no other tenpai reading sneaks in), so
    // discarding either 2s or 3s leaves the other as the wait.
    private static List<TilePivot> BuildTwoWayTankiHand(IReadOnlyList<TilePivot> tilesSet) => new()
    {
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 1),
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 2),
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 3),
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 4),
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 5),
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 6),
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 7),
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 8),
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 9),
        TilePivot.GetTile(tilesSet, Families.Circle, number: 1),
        TilePivot.GetTile(tilesSet, Families.Circle, number: 2),
        TilePivot.GetTile(tilesSet, Families.Circle, number: 3),
        TilePivot.GetTile(tilesSet, Families.Bamboo, number: 2),
        TilePivot.GetTile(tilesSet, Families.Bamboo, number: 3),
    };

    // Policy (applies to every current and future BasicCpuManagerPivot subclass): a behavior added to
    // the base class - like furiten avoidance - is inherited by every variant automatically, unless
    // that variant deliberately opts out (as BasicNoFuritenCpuManagerPivot does below, for A/B
    // measurement only). This theory locks that in for the two existing variants that must NOT opt out.
    [Theory]
    [InlineData(typeof(BasicCpuManagerPivot))]
    [InlineData(typeof(NoDefenseCpuManagerPivot))]
    [InlineData(typeof(FullDefenseCpuManagerPivot))]
    public void FuritenProneVariants_AvoidTheDiscardThatWouldLeaveThemInFuriten(Type cpuManagerType)
    {
        var round = NewRound(1);
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand = BuildTwoWayTankiHand(tilesSet);
        var bamboo2 = TilePivot.GetTile(tilesSet, Families.Bamboo, number: 2);
        var bamboo3 = TilePivot.GetTile(tilesSet, Families.Bamboo, number: 3);

        var currentPlayer = round.CurrentPlayerIndex;
        SetHand(round, currentPlayer, hand);
        SetWaitForDiscard(round, true);
        // Keeps both waits equally fully-live, so the wait-width tie-break stays neutral and this
        // scenario isolates furiten avoidance instead of being decided by wait-width first.
        KeepFullyConcealed(round, bamboo2, bamboo3);
        // Already discarded 3s earlier this round: discarding 2s now (waiting on 3s) would be furiten.
        AddToDiscard(round, currentPlayer, bamboo3);

        var tenpaiChoices = round.ExtractDiscardChoicesFromTenpai(currentPlayer);
        Assert.Equal(new[] { bamboo2, bamboo3 }, tenpaiChoices.OrderBy(t => t.Number));

        var cpuManager = (Gnoj_Ham_Library.BasicCpuManagerPivot)Activator.CreateInstance(cpuManagerType, round)!;
        var choice = cpuManager.DiscardDecision();

        Assert.Equal(bamboo3, choice);
    }

    [Fact]
    public void BasicNoFuritenCpuManagerPivot_ReproducesTheOriginalFuritenBlindChoice()
    {
        var round = NewRound(1);
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand = BuildTwoWayTankiHand(tilesSet);
        var bamboo2 = TilePivot.GetTile(tilesSet, Families.Bamboo, number: 2);
        var bamboo3 = TilePivot.GetTile(tilesSet, Families.Bamboo, number: 3);

        var currentPlayer = round.CurrentPlayerIndex;
        SetHand(round, currentPlayer, hand);
        SetWaitForDiscard(round, true);
        // Same as above: without this, the (correctly working) wait-width tie-break would already favor
        // 3s on its own merits (its wait on 2s is more live than the other way around), coincidentally
        // agreeing with furiten-avoidance and defeating the point of this test.
        KeepFullyConcealed(round, bamboo2, bamboo3);
        AddToDiscard(round, currentPlayer, bamboo3);

        // Same scenario as the test above, but the original heuristic (no furiten awareness) picks 2s:
        // with no dangerous opponent, it orders by dora (tied) then safety (tied) then wait-width (tied,
        // thanks to KeepFullyConcealed) then descending distance-to-middle, and 2s (distance 3) beats 3s
        // (distance 2) on that last tiebreak - it just happens to be the one that would leave this hand
        // in furiten.
        var noFuritenChoice = new BasicNoFuritenCpuManagerPivot(round).DiscardDecision();

        Assert.Equal(bamboo2, noFuritenChoice);
    }
}
