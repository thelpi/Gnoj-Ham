using Gnoj_Ham_Library;
using Gnoj_Ham_ViewModel;

namespace Gnoj_Ham_ViewModel_Tests;

public class YakuRuleViewModel_Tests
{
    [Fact]
    public void ACommonYaku_ShowsItsWorthAndHasNoNote()
    {
        var yaku = YakuPivot.Yakus.First(y => y.FanCount > 0 && y.ConcealedBonusFanCount == 0);

        var viewModel = new YakuRuleViewModel(yaku);

        Assert.Equal(yaku.Name, viewModel.Name);
        Assert.Equal(yaku.Description, viewModel.Description);
        Assert.Equal(yaku.ConcealedFanCount.ToString(), viewModel.FansText);
        Assert.Equal(string.Empty, viewModel.ToolTip);
        Assert.False(viewModel.IsConcealedOnly);
    }

    [Fact]
    public void AYakuWorthMoreWhenConcealed_ShowsBothWorthsAndNotesTheBonus()
    {
        var yaku = YakuPivot.Yakus.First(y => y.FanCount > 0 && y.ConcealedBonusFanCount > 0);

        var viewModel = new YakuRuleViewModel(yaku);

        Assert.Equal($"{yaku.FanCount} (+{yaku.ConcealedBonusFanCount})", viewModel.FansText);
        Assert.Equal("Bonus si main fermée.", viewModel.ToolTip);
        Assert.False(viewModel.IsConcealedOnly);
    }

    [Fact]
    public void AYakuOfConcealedHandsOnly_SaysSoAndIsMarkedAsSuch()
    {
        var yaku = YakuPivot.Yakus.First(y => y.FanCount == 0 && y.ConcealedFanCount != 13);

        var viewModel = new YakuRuleViewModel(yaku);

        Assert.Equal(yaku.ConcealedFanCount.ToString(), viewModel.FansText);
        Assert.Equal("Main fermée uniquement.", viewModel.ToolTip);
        Assert.True(viewModel.IsConcealedOnly);
    }

    [Fact]
    public void AYakumanOfConcealedHandsOnly_SaysItIsAYakuman()
    {
        var yaku = YakuPivot.Yakus.First(y => y.FanCount == 0 && y.ConcealedFanCount == 13);

        var viewModel = new YakuRuleViewModel(yaku);

        Assert.Equal("13", viewModel.FansText);
        Assert.Equal("Yakuman. Main fermée uniquement.", viewModel.ToolTip);
        Assert.True(viewModel.IsConcealedOnly);
    }

    [Fact]
    public void TheExample_IsShownAsTheTilesOfTheHand()
    {
        var yaku = YakuPivot.Yakus.First(y => y.Example != null);

        var viewModel = new YakuRuleViewModel(yaku);

        Assert.Equal(yaku.Example!.Count, viewModel.ExampleTiles.Count);
        for (var i = 0; i < yaku.Example.Count; i++)
        {
            Assert.Same(yaku.Example[i], viewModel.ExampleTiles[i].Tile);
            Assert.False(viewModel.ExampleTiles[i].IsConcealed);
        }
    }

    [Fact]
    public void AYakuWithoutExample_HasNoTiles()
    {
        var yaku = YakuPivot.Yakus.FirstOrDefault(y => y.Example == null);
        Assert.NotNull(yaku);

        var viewModel = new YakuRuleViewModel(yaku);

        Assert.Empty(viewModel.ExampleTiles);
    }
}
