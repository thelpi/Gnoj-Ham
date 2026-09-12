using System.Reflection;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class KyuushuKyuuhai_Tests
{
    private static RoundPivot NewRound()
        => new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(1)).Round;

    // 9 distinct terminal/honour kinds (4 winds + 3 dragons + 1m + 9m), padded to 14 tiles with
    // duplicates of already-used kinds (extra physical copies, not new kinds).
    private static List<TilePivot> BuildNineKindsHand(IReadOnlyList<TilePivot> tilesSet)
    {
        return new List<TilePivot>
        {
            TilePivot.GetTile(tilesSet, Families.Wind, wind: Winds.East),
            TilePivot.GetTile(tilesSet, Families.Wind, wind: Winds.South),
            TilePivot.GetTile(tilesSet, Families.Wind, wind: Winds.West),
            TilePivot.GetTile(tilesSet, Families.Wind, wind: Winds.North),
            TilePivot.GetTile(tilesSet, Families.Dragon, dragon: Dragons.Red),
            TilePivot.GetTile(tilesSet, Families.Dragon, dragon: Dragons.White),
            TilePivot.GetTile(tilesSet, Families.Dragon, dragon: Dragons.Green),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 1),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 9),
            TilePivot.GetTile(tilesSet, Families.Wind, wind: Winds.East),
            TilePivot.GetTile(tilesSet, Families.Wind, wind: Winds.East),
            TilePivot.GetTile(tilesSet, Families.Dragon, dragon: Dragons.Red),
            TilePivot.GetTile(tilesSet, Families.Dragon, dragon: Dragons.Red),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 1),
        };
    }

    private static void SetHandAndWaitForDiscard(RoundPivot round, PlayerIndices playerIndex, List<TilePivot> concealedTiles)
    {
        var hands = (List<HandPivot>)typeof(RoundPivot)
            .GetField("_hands", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(round)!;
        hands[(int)playerIndex] = new HandPivot(concealedTiles);

        typeof(RoundPivot)
            .GetField("_waitForDiscard", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(round, true);
    }

    [Fact]
    public void CanCallKyuushuKyuuhai_NineDistinctKindsOnFirstTurn_ReturnsTrue()
    {
        var round = NewRound();
        var tilesSet = TilePivot.GetCompleteSet(false);
        SetHandAndWaitForDiscard(round, round.CurrentPlayerIndex, BuildNineKindsHand(tilesSet));

        Assert.True(round.CanCallKyuushuKyuuhai());
    }

    [Fact]
    public void CanCallKyuushuKyuuhai_OnlyEightDistinctKinds_ReturnsFalse()
    {
        var round = NewRound();
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand = BuildNineKindsHand(tilesSet);
        // Swaps the 9th kind (9m) for a duplicate of an already-used kind: down to 8 distinct kinds.
        hand.RemoveAt(hand.FindIndex(t => t.Family == Families.Caracter && t.Number == 9));
        hand.Add(TilePivot.GetTile(tilesSet, Families.Wind, wind: Winds.East));

        SetHandAndWaitForDiscard(round, round.CurrentPlayerIndex, hand);

        Assert.False(round.CanCallKyuushuKyuuhai());
    }

    [Fact]
    public void CanCallKyuushuKyuuhai_AfterOwnFirstDiscard_ReturnsFalse()
    {
        var round = NewRound();
        var tilesSet = TilePivot.GetCompleteSet(false);
        SetHandAndWaitForDiscard(round, round.CurrentPlayerIndex, BuildNineKindsHand(tilesSet));

        var discards = (List<List<TilePivot>>)typeof(RoundPivot)
            .GetField("_discards", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(round)!;
        discards[(int)round.CurrentPlayerIndex].Add(tilesSet[0]);

        Assert.False(round.CanCallKyuushuKyuuhai());
    }

    [Fact]
    public void CallKyuushuKyuuhai_WhenEligible_EndsTheRoundAsAbortiveDrawWithNoTenpaiPaymentAndDealerRepeats()
    {
        var round = NewRound();
        var tilesSet = TilePivot.GetCompleteSet(false);
        SetHandAndWaitForDiscard(round, round.CurrentPlayerIndex, BuildNineKindsHand(tilesSet));

        var called = round.CallKyuushuKyuuhai();

        Assert.True(called);
        Assert.True(round.IsKyuushuKyuuhai);

        var info = round.EndOfRound(null);

        Assert.True(info.Ryuukyoku);
        Assert.False(info.ToNextEast);
        Assert.Empty(info.PlayersInfo);
    }

    [Fact]
    public void CallKyuushuKyuuhai_WhenNotEligible_ReturnsFalseAndLeavesTheRoundUnaffected()
    {
        var round = NewRound();

        var called = round.CallKyuushuKyuuhai();

        Assert.False(called);
        Assert.False(round.IsKyuushuKyuuhai);
    }
}
