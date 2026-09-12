using System.Reflection;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class Furiten_Tests
{
    // Tenpai hand: 123m 456m 789m 11p + 23s, waiting on 1s or 4s (ryanmen).
    private static List<TilePivot> BuildTenpaiHand(IReadOnlyList<TilePivot> tilesSet)
    {
        var hand = new List<TilePivot>
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
            TilePivot.GetTile(tilesSet, Families.Circle, number: 1),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 2),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 3)
        };

        return hand.OrderBy(t => t).ToList();
    }

    private static (TilePivot waitLow, TilePivot waitHigh) WinningTiles(IReadOnlyList<TilePivot> tilesSet)
        => (TilePivot.GetTile(tilesSet, Families.Bamboo, number: 1), TilePivot.GetTile(tilesSet, Families.Bamboo, number: 4));

    // RoundPivot delegates all discard-tracking state to DiscardHistoryPivot; reflect into that
    // collaborator first, then into its own private fields.
    private static DiscardHistoryPivot DiscardHistory(RoundPivot round)
        => (DiscardHistoryPivot)typeof(RoundPivot)
            .GetField("_discardHistory", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(round)!;

    private static List<List<TilePivot>> VirtualDiscardsField(RoundPivot round)
        => (List<List<TilePivot>>)typeof(DiscardHistoryPivot)
            .GetField("_virtualDiscards", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(DiscardHistory(round))!;

    private static Dictionary<PlayerIndices, int> LastOwnDiscardRankField(RoundPivot round, PlayerIndices playerIndex)
        => ((List<Dictionary<PlayerIndices, int>>)typeof(DiscardHistoryPivot)
            .GetField("_lastOwnDiscardOpponentsVirtualRank", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(DiscardHistory(round))!)[(int)playerIndex];

    private static List<PlayerIndices> PlayerIndexHistoryField(RoundPivot round)
        => (List<PlayerIndices>)typeof(DiscardHistoryPivot)
            .GetField("_playerIndexHistory", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(DiscardHistory(round))!;

    private static RoundPivot NewRound()
        => new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(1)).Round;

    #region CancelYakusIfFuriten (permanent furiten)

    [Fact]
    public void PermanentFuriten_WinningTileInOwnDiscard_ReturnsTrue()
    {
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand = new HandPivot(BuildTenpaiHand(tilesSet));
        var (waitLow, _) = WinningTiles(tilesSet);

        var isFuriten = hand.CancelYakusIfFuriten(new List<TilePivot> { waitLow }, new List<TilePivot>());

        Assert.True(isFuriten);
    }

    [Fact]
    public void PermanentFuriten_WinningTileInOpponentDiscardSinceRiichi_ReturnsTrue()
    {
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand = new HandPivot(BuildTenpaiHand(tilesSet));
        var (_, waitHigh) = WinningTiles(tilesSet);

        var isFuriten = hand.CancelYakusIfFuriten(new List<TilePivot>(), new List<TilePivot> { waitHigh });

        Assert.True(isFuriten);
    }

    [Fact]
    public void PermanentFuriten_NoWinningTileDiscarded_ReturnsFalse()
    {
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand = new HandPivot(BuildTenpaiHand(tilesSet));
        var unrelatedTile = TilePivot.GetTile(tilesSet, Families.Dragon, dragon: Dragons.Red);

        var isFuriten = hand.CancelYakusIfFuriten(new List<TilePivot> { unrelatedTile }, new List<TilePivot> { unrelatedTile });

        Assert.False(isFuriten);
    }

    #endregion

    #region HandPivot.CancelYakusIfTemporaryFuriten (given the reconstructed tile list)

    [Fact]
    public void TemporaryFuriten_ListContainsWinningTile_ReturnsTrue()
    {
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand = new HandPivot(BuildTenpaiHand(tilesSet));
        var (waitLow, _) = WinningTiles(tilesSet);

        var isFuriten = hand.CancelYakusIfTemporaryFuriten(new List<TilePivot> { waitLow });

        Assert.True(isFuriten);
    }

    [Fact]
    public void TemporaryFuriten_EmptyList_ReturnsFalse()
    {
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand = new HandPivot(BuildTenpaiHand(tilesSet));

        var isFuriten = hand.CancelYakusIfTemporaryFuriten(new List<TilePivot>());

        Assert.False(isFuriten);
    }

    #endregion

    #region RoundPivot.GetTilesFromVirtualDiscardsSinceLastOwnDiscard (the actual bug: calls must not clear the temporary furiten window)

    [Fact]
    public void SinceLastOwnDiscard_IgnoresTilesDiscardedBeforeOwnLastDiscard()
    {
        var round = NewRound();
        var tilesSet = round.FullTilesList;
        var oldTile = TilePivot.GetTile(tilesSet, Families.Dragon, dragon: Dragons.Red);
        var newTile = TilePivot.GetTile(tilesSet, Families.Dragon, dragon: Dragons.White);

        // Opponent discarded 'oldTile' before P0's last discard: already accounted for at that time.
        VirtualDiscardsField(round)[(int)PlayerIndices.One].Add(oldTile);
        LastOwnDiscardRankField(round, PlayerIndices.Zero)[PlayerIndices.One] = 1;
        // Opponent discards 'newTile' afterwards: this is new, since P0's last discard.
        VirtualDiscardsField(round)[(int)PlayerIndices.One].Add(newTile);

        var result = round.GetTilesFromVirtualDiscardsSinceLastOwnDiscard(PlayerIndices.Zero, newTile);

        Assert.DoesNotContain(oldTile, result);
    }

    [Fact]
    public void SinceLastOwnDiscard_ExcludesTheLiveRonTile()
    {
        var round = NewRound();
        var tilesSet = round.FullTilesList;
        var liveTile = TilePivot.GetTile(tilesSet, Families.Dragon, dragon: Dragons.White);

        VirtualDiscardsField(round)[(int)PlayerIndices.One].Add(liveTile);

        var result = round.GetTilesFromVirtualDiscardsSinceLastOwnDiscard(PlayerIndices.Zero, liveTile);

        Assert.Empty(result);
    }

    [Fact]
    public void SinceLastOwnDiscard_SurvivesACallThatClearsPlayerIndexHistory()
    {
        // This is the actual regression test for the bug: a missed winning tile discarded by P1 must
        // still be reported even after P2's call (pon/chii/closed kan) clears "_playerIndexHistory" in
        // between - temporary furiten lasts until P0's own next discard, not until the next call.
        var round = NewRound();
        var tilesSet = round.FullTilesList;
        var missedWinningTile = TilePivot.GetTile(tilesSet, Families.Dragon, dragon: Dragons.Red);
        var liveTile = TilePivot.GetTile(tilesSet, Families.Dragon, dragon: Dragons.White);

        // P1 discards the tile P0 could have ronned on, right after P0's own last discard.
        VirtualDiscardsField(round)[(int)PlayerIndices.One].Add(missedWinningTile);

        // Some other player then calls something, which clears "_playerIndexHistory" (simulated
        // directly here, as it would happen inside Discard() when a call is in progress).
        PlayerIndexHistoryField(round).Clear();

        // P2 discards the live tile P0 is now offered a ron on.
        VirtualDiscardsField(round)[(int)PlayerIndices.Two].Add(liveTile);

        var result = round.GetTilesFromVirtualDiscardsSinceLastOwnDiscard(PlayerIndices.Zero, liveTile);

        Assert.Contains(missedWinningTile, result);
    }

    #endregion
}
