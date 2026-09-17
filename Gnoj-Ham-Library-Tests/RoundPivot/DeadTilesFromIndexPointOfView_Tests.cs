using System.Reflection;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class DeadTilesFromIndexPointOfView_Tests
{
    private static RoundPivot NewRound(int seed)
        => new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(seed)).Round;

    // Mutates the existing list in place (Clear + AddRange) rather than reassigning the field, since
    // every one of RoundPivot's tile-tracking fields is "readonly": the field itself can't be swapped
    // for a new list via reflection without also bypassing the CLR's initonly check, but nothing stops
    // mutating the list instance it already points to.
    private static void SetListField(RoundPivot round, string fieldName, IEnumerable<TilePivot> content)
    {
        var list = (List<TilePivot>)typeof(RoundPivot)
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(round)!;
        list.Clear();
        list.AddRange(content);
    }

    private static void SetHand(RoundPivot round, PlayerIndices playerIndex, List<TilePivot> concealedTiles)
    {
        var hands = (List<HandPivot>)typeof(RoundPivot)
            .GetField("_hands", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(round)!;
        hands[(int)playerIndex] = new HandPivot(concealedTiles);
    }

    [Fact]
    public void ReturnsEveryDeadCopyOfAKind_NotJustOneDeduplicatedEntry()
    {
        // The actual bug: DeadTilesFromIndexPointOfView used to be built on Enumerable.Except, which
        // dedupes both operands - so no matter how many of a tile's copies were actually dead, at most
        // one ever showed up in the result. Every caller (CloseToKokushi, the chiitoitsu discard
        // tie-break, the kabe safety reading, WaitLiveTileCount, ...) treats this list as a genuine
        // multiset (0 to 4 occurrences per kind), so that dedup silently broke all of them.
        var round = NewRound(1);
        var tilesSet = TilePivot.GetCompleteSet(false);
        var tileA = TilePivot.GetTile(tilesSet, Families.Bamboo, number: 7);
        var filler = TilePivot.GetTile(tilesSet, Families.Dragon, dragon: Dragons.Red);

        var currentPlayer = round.CurrentPlayerIndex;

        // Every copy of tileA that exists at all: exactly 4, forming the whole "deck".
        SetListField(round, "_fullTilesList", Enumerable.Repeat(tileA, 4));
        // Exactly one of those 4 remains concealed (sitting in the live wall).
        SetListField(round, "_wallTiles", new[] { tileA });
        SetListField(round, "_compensationTiles", Array.Empty<TilePivot>());
        SetListField(round, "_deadTreasureTiles", Array.Empty<TilePivot>());
        SetListField(round, "_uraDoraIndicatorTiles", Array.Empty<TilePivot>());
        SetListField(round, "_doraIndicatorTiles", Array.Empty<TilePivot>());

        foreach (var opponent in Enum.GetValues<PlayerIndices>().Where(p => p != currentPlayer))
        {
            SetHand(round, opponent, new List<TilePivot> { filler });
        }

        var deadTiles = round.DeadTilesFromIndexPointOfView(currentPlayer);

        // 1 of the 4 total copies is still concealed (the wall one); the other 3 must all be reported
        // dead - not collapsed down to a single entry.
        Assert.Equal(3, deadTiles.Count(t => t == tileA));
    }
}
