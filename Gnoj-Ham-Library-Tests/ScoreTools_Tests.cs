using Gnoj_Ham_Library;

namespace Gnoj_Ham_Library_Tests;

public class ScoreTools_Tests
{
    private static YakuPivot ArbitraryYakuman()
        => YakuPivot.Yakus.First(y => y.IsYakuman);

    private static YakuPivot AnotherArbitraryYakuman(YakuPivot other)
        => YakuPivot.Yakus.First(y => y.IsYakuman && y.Name != other.Name);

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
}
