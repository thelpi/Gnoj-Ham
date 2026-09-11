using System.Reflection;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class YakuPivot_Tests
{
    // Moves every tile matching (family, number) from the hand's concealed tiles into a new open
    // (non-concealed) combination, using those same three physical tiles - the total stays 14 tiles.
    // DeclarePon doesn't fit here: it assumes the "stolen" tile is a separate, external tile (an
    // opponent's discard), which none of these Example hands has - they're already fully-formed
    // 14-tile winning hands.
    private static void OpenTriplet(HandPivot hand, Families family, byte number)
    {
        var concealedField = typeof(HandPivot).GetField("_concealedTiles", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var declaredField = typeof(HandPivot).GetField("_declaredCombinations", BindingFlags.NonPublic | BindingFlags.Instance)!;

        var concealed = (List<TilePivot>)concealedField.GetValue(hand)!;
        var declared = (List<TileComboPivot>)declaredField.GetValue(hand)!;

        var triplet = concealed.Where(t => t.Family == family && t.Number == number).Take(3).ToList();
        Assert.Equal(3, triplet.Count);

        foreach (var tile in triplet)
        {
            concealed.Remove(tile);
        }

        declared.Add(new TileComboPivot(triplet.Take(2), triplet[2], Winds.East));
    }


    // These three ship an Example but can't be verified through the generic theory below (see the
    // dedicated tests for why); every other yaku with an Example is purely hand-shape-dependent.
    private static readonly string[] SpecialCased = { "Toitoi", "Honroutou", "Pinfu" };

    // Context-dependent yaku (Riichi, Ippatsu, Haitei, Tenhou...) have no Example at all and would
    // need their own dedicated test too; not covered here yet.
    public static IEnumerable<object[]> YakuWithExample()
        => YakuPivot.Yakus.Where(y => y.Example != null && !SpecialCased.Contains(y.Name)).Select(y => new object[] { y.Name });

    [Theory]
    [MemberData(nameof(YakuWithExample))]
    public void SetYakus_ExampleHand_DetectsTheYakuItIllustrates(string yakuName)
    {
        var yaku = YakuPivot.Yakus.First(y => y.Name == yakuName);
        var hand = new HandPivot(yaku.Example!);
        var context = new WinContextPivot(
            latestTile: hand.LatestPick,
            drawType: DrawTypes.Wall,
            dominantWind: Winds.East,
            playerWind: Winds.South);

        hand.SetYakus(context);

        Assert.NotNull(hand.Yakus);
        Assert.Contains(yaku, hand.Yakus!);
    }

    [Fact]
    public void SetYakus_OpenToitoi_IsDetected()
    {
        // YakuPivot.Toitoi's Example (four triplets, all concealed since Example has no way to mark
        // a tile as stolen) is also a valid Suuankou hand. Suuankou is a yakuman, and GetYakus always
        // returns yakumans exclusively when any is found - so the plain example can never show Toitoi
        // on its own. Opening one triplet breaks the Suuankou reading (needs all four concealed)
        // without affecting Toitoi (any four triplets, open or concealed).
        var yaku = YakuPivot.Yakus.First(y => y.Name == "Toitoi");
        var hand = new HandPivot(yaku.Example!);
        OpenTriplet(hand, Families.Bamboo, 7);

        var context = new WinContextPivot(latestTile: hand.LatestPick, drawType: DrawTypes.Wall, dominantWind: Winds.East, playerWind: Winds.South);
        hand.SetYakus(context);

        Assert.NotNull(hand.Yakus);
        Assert.Contains(yaku, hand.Yakus!);
    }

    [Fact]
    public void SetYakus_OpenHonroutou_IsDetected()
    {
        // Same root cause as SetYakus_OpenToitoi_IsDetected: Honroutou's Example is also, incidentally,
        // a concealed Suuankou.
        var yaku = YakuPivot.Yakus.First(y => y.Name == "Honroutou");
        var hand = new HandPivot(yaku.Example!);
        OpenTriplet(hand, Families.Caracter, 9);

        var context = new WinContextPivot(latestTile: hand.LatestPick, drawType: DrawTypes.Wall, dominantWind: Winds.East, playerWind: Winds.South);
        hand.SetYakus(context);

        Assert.NotNull(hand.Yakus);
        Assert.Contains(yaku, hand.Yakus!);
    }

    [Fact]
    public void SetYakus_Pinfu_IsDetectedOnARyanmenWait()
    {
        // Pinfu additionally requires the latest tile to complete a two-sided ("ryanmen") wait.
        // HandPivot.LatestPick (used by the generic theory above) defaults to the last tile of the
        // Example array, which for this particular hand is one of the pair's tiles - not part of any
        // sequence at all, so the wait-shape check can never pass. Here the latest tile is explicitly
        // the edge of the 4-5-6 bamboo sequence (waiting on 3 or 6: a genuine ryanmen).
        var yaku = YakuPivot.Yakus.First(y => y.Name == "Pinfu");
        var hand = new HandPivot(yaku.Example!);
        var latestTile = hand.ConcealedTiles.First(t => t.Family == Families.Bamboo && t.Number == 6);

        var context = new WinContextPivot(latestTile: latestTile, drawType: DrawTypes.Wall, dominantWind: Winds.East, playerWind: Winds.South);
        hand.SetYakus(context);

        Assert.NotNull(hand.Yakus);
        Assert.Contains(yaku, hand.Yakus!);
    }
}
