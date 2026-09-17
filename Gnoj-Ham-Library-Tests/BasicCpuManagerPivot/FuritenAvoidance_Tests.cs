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

    [Fact]
    public void BasicCpuManagerPivot_AvoidsTheDiscardThatWouldLeaveItInFuriten()
    {
        var round = NewRound(1);
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand = BuildTwoWayTankiHand(tilesSet);
        var bamboo2 = TilePivot.GetTile(tilesSet, Families.Bamboo, number: 2);
        var bamboo3 = TilePivot.GetTile(tilesSet, Families.Bamboo, number: 3);

        var currentPlayer = round.CurrentPlayerIndex;
        SetHand(round, currentPlayer, hand);
        SetWaitForDiscard(round, true);
        // Already discarded 3s earlier this round: discarding 2s now (waiting on 3s) would be furiten.
        AddToDiscard(round, currentPlayer, bamboo3);

        var tenpaiChoices = round.ExtractDiscardChoicesFromTenpai(currentPlayer);
        Assert.Equal(new[] { bamboo2, bamboo3 }, tenpaiChoices.OrderBy(t => t.Number));

        var basicChoice = new BasicCpuManagerPivot(round).DiscardDecision();

        Assert.Equal(bamboo3, basicChoice);
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
        AddToDiscard(round, currentPlayer, bamboo3);

        // Same scenario as the fixed test above, but the original heuristic (no furiten awareness)
        // picks 2s: with no dangerous opponent, it orders by dora (tied) then safety (tied) then
        // descending distance-to-middle, and 2s (distance 3) beats 3s (distance 2) on that last tiebreak
        // - it just happens to be the one that would leave this hand in furiten.
        var noFuritenChoice = new BasicNoFuritenCpuManagerPivot(round).DiscardDecision();

        Assert.Equal(bamboo2, noFuritenChoice);
    }
}
