using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class ScoreTools_Tests
{
    private static YakuPivot ArbitraryYakuman()
        => YakuPivot.Yakus.First(y => y.IsYakuman);

    private static YakuPivot AnotherArbitraryYakuman(YakuPivot other)
        => YakuPivot.Yakus.First(y => y.IsYakuman && y.Name != other.Name);

    // Any 13-tile hand that isn't Thirteen Orphans: 123m 456m 789m 11p 23s.
    private static List<TilePivot> BuildArbitraryHand(IReadOnlyList<TilePivot> tilesSet)
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

    [Fact]
    public void GetFanCount_SingleYakuman_Returns13RegardlessOfMultipleYakumansSetting()
    {
        var yakus = new List<YakuPivot> { ArbitraryYakuman() };

        Assert.Equal(13, ScoreTools.GetFanCount(yakus, concealed: true, allowMultipleYakumans: true, allowKazoeYakuman: true));
        Assert.Equal(13, ScoreTools.GetFanCount(yakus, concealed: true, allowMultipleYakumans: false, allowKazoeYakuman: true));
    }

    [Fact]
    public void GetFanCount_TwoDistinctYakumans_StackedOnlyWhenAllowed()
    {
        var first = ArbitraryYakuman();
        var second = AnotherArbitraryYakuman(first);
        var yakus = new List<YakuPivot> { first, second };

        Assert.Equal(26, ScoreTools.GetFanCount(yakus, concealed: true, allowMultipleYakumans: true, allowKazoeYakuman: true));
        Assert.Equal(13, ScoreTools.GetFanCount(yakus, concealed: true, allowMultipleYakumans: false, allowKazoeYakuman: true));
    }

    [Fact]
    public void GetFanCount_Yakuman_IgnoresDoras()
    {
        // Doras never add value on top of a yakuman hand, with or without stacking.
        var yakus = new List<YakuPivot> { ArbitraryYakuman() };

        var fanCount = ScoreTools.GetFanCount(yakus, concealed: true, allowMultipleYakumans: true, allowKazoeYakuman: true,
            dorasCount: 3, uraDorasCount: 2, redDorasCount: 1);

        Assert.Equal(13, fanCount);
    }

    [Fact]
    public void GetFanCount_ThirteenFansWithoutYakuman_CappedUnlessKazoeYakumanAllowed()
    {
        var baseYaku = YakuPivot.Yakus.First(y => !y.IsYakuman && y.ConcealedFanCount > 0);
        var repeatCount = (int)Math.Ceiling(13.0 / baseYaku.ConcealedFanCount);
        var yakus = Enumerable.Repeat(baseYaku, repeatCount).ToList();

        Assert.True(yakus.Sum(y => y.ConcealedFanCount) >= 13);

        Assert.Equal(13, ScoreTools.GetFanCount(yakus, concealed: true, allowMultipleYakumans: true, allowKazoeYakuman: true));
        Assert.Equal(12, ScoreTools.GetFanCount(yakus, concealed: true, allowMultipleYakumans: true, allowKazoeYakuman: false));
    }

    [Fact]
    public void GetFanCount_BelowThirteenFans_UnaffectedByEitherSetting()
    {
        var baseYaku = YakuPivot.Yakus.First(y => !y.IsYakuman && y.ConcealedFanCount > 0);
        var yakus = new List<YakuPivot> { baseYaku };

        var expected = baseYaku.ConcealedFanCount;

        Assert.Equal(expected, ScoreTools.GetFanCount(yakus, concealed: true, allowMultipleYakumans: false, allowKazoeYakuman: false));
        Assert.Equal(expected, ScoreTools.GetFanCount(yakus, concealed: true, allowMultipleYakumans: true, allowKazoeYakuman: true));
    }

    [Fact]
    public void GetFuCount_NagashiManganHand_DoesNotThrow()
    {
        // Nagashi Mangan sets YakusCombinations to null (HandPivot.SetYakus); GetFuCount must not
        // dereference it in that case, same as it already special-cases Chiitoitsu.
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand = new HandPivot(BuildArbitraryHand(tilesSet));
        hand.SetYakus(new WinContextPivot());

        Assert.Contains(YakuPivot.NagashiMangan, hand.Yakus!);
        Assert.Null(hand.YakusCombinations);

        var fu = ScoreTools.GetFuCount(hand, isTsumo: true, Winds.East, Winds.East);

        Assert.True(fu > 0);
    }
}
