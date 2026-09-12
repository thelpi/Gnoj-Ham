using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class ScoreTools_Tests
{
    // Regular (non-double) yakuman only: double yakumans (ConcealedFanCount == 26) have their own
    // dedicated tests below and would break the "13" assumptions used throughout this file.
    private static YakuPivot ArbitraryYakuman()
        => YakuPivot.Yakus.First(y => y.IsYakuman && y.ConcealedFanCount == 13);

    private static YakuPivot AnotherArbitraryYakuman(YakuPivot other)
        => YakuPivot.Yakus.First(y => y.IsYakuman && y.ConcealedFanCount == 13 && y.Name != other.Name);

    private static YakuPivot ArbitraryDoubleYakuman()
        => YakuPivot.Yakus.First(y => y.ConcealedFanCount == 26);

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

        Assert.Equal(13, ScoreTools.GetFanCount(yakus, concealed: true, allowMultipleYakumans: true, allowKazoeYakuman: true, allowDoubleYakuman: true));
        Assert.Equal(13, ScoreTools.GetFanCount(yakus, concealed: true, allowMultipleYakumans: false, allowKazoeYakuman: true, allowDoubleYakuman: true));
    }

    [Fact]
    public void GetFanCount_TwoDistinctYakumans_StackedOnlyWhenAllowed()
    {
        var first = ArbitraryYakuman();
        var second = AnotherArbitraryYakuman(first);
        var yakus = new List<YakuPivot> { first, second };

        Assert.Equal(26, ScoreTools.GetFanCount(yakus, concealed: true, allowMultipleYakumans: true, allowKazoeYakuman: true, allowDoubleYakuman: true));
        Assert.Equal(13, ScoreTools.GetFanCount(yakus, concealed: true, allowMultipleYakumans: false, allowKazoeYakuman: true, allowDoubleYakuman: true));
    }

    [Fact]
    public void GetFanCount_Yakuman_IgnoresDoras()
    {
        // Doras never add value on top of a yakuman hand, with or without stacking.
        var yakus = new List<YakuPivot> { ArbitraryYakuman() };

        var fanCount = ScoreTools.GetFanCount(yakus, concealed: true, allowMultipleYakumans: true, allowKazoeYakuman: true, allowDoubleYakuman: true,
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

        Assert.Equal(13, ScoreTools.GetFanCount(yakus, concealed: true, allowMultipleYakumans: true, allowKazoeYakuman: true, allowDoubleYakuman: true));
        Assert.Equal(12, ScoreTools.GetFanCount(yakus, concealed: true, allowMultipleYakumans: true, allowKazoeYakuman: false, allowDoubleYakuman: true));
    }

    [Fact]
    public void GetFanCount_BelowThirteenFans_UnaffectedByEitherSetting()
    {
        var baseYaku = YakuPivot.Yakus.First(y => !y.IsYakuman && y.ConcealedFanCount > 0);
        var yakus = new List<YakuPivot> { baseYaku };

        var expected = baseYaku.ConcealedFanCount;

        Assert.Equal(expected, ScoreTools.GetFanCount(yakus, concealed: true, allowMultipleYakumans: false, allowKazoeYakuman: false, allowDoubleYakuman: true));
        Assert.Equal(expected, ScoreTools.GetFanCount(yakus, concealed: true, allowMultipleYakumans: true, allowKazoeYakuman: true, allowDoubleYakuman: true));
    }

    [Fact]
    public void GetFanCount_DoubleYakumanAlone_Returns26OnlyWhenAllowed()
    {
        var yakus = new List<YakuPivot> { ArbitraryDoubleYakuman() };

        Assert.Equal(26, ScoreTools.GetFanCount(yakus, concealed: true, allowMultipleYakumans: true, allowKazoeYakuman: true, allowDoubleYakuman: true));
        Assert.Equal(26, ScoreTools.GetFanCount(yakus, concealed: true, allowMultipleYakumans: false, allowKazoeYakuman: true, allowDoubleYakuman: true));
        Assert.Equal(13, ScoreTools.GetFanCount(yakus, concealed: true, allowMultipleYakumans: true, allowKazoeYakuman: true, allowDoubleYakuman: false));
    }

    [Fact]
    public void GetFanCount_DoubleYakumanPlusRegularYakuman_StackedOnlyWhenMultipleAllowed()
    {
        var double13 = ArbitraryDoubleYakuman();
        var regular = ArbitraryYakuman();
        var yakus = new List<YakuPivot> { double13, regular };

        // 26 + 13 when both stacking and doubling are allowed.
        Assert.Equal(39, ScoreTools.GetFanCount(yakus, concealed: true, allowMultipleYakumans: true, allowKazoeYakuman: true, allowDoubleYakuman: true));
        // Stacking disabled: only the highest single yakuman value counts.
        Assert.Equal(26, ScoreTools.GetFanCount(yakus, concealed: true, allowMultipleYakumans: false, allowKazoeYakuman: true, allowDoubleYakuman: true));
        // Doubling disabled: the double yakuman is capped to 13, so stacking gives 13 + 13.
        Assert.Equal(26, ScoreTools.GetFanCount(yakus, concealed: true, allowMultipleYakumans: true, allowKazoeYakuman: true, allowDoubleYakuman: false));
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
