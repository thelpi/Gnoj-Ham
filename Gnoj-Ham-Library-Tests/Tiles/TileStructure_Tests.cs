using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

// What the tile constants stand for, checked against the set of tiles and the rules they come from.
public class TileStructure_Tests
{
    [Fact]
    public void ACompleteSet_HoldsEveryTileTheNumberOfCopiesItHas()
    {
        var set = TilePivot.GetCompleteSet();

        Assert.All(set.GroupBy(t => t), group => Assert.Equal(TilePivot.CopiesCount, group.Count()));
        Assert.Equal(136, set.Count);
    }

    [Fact]
    public void TheNumbersOfASuit_RunFromTheMinimumToTheMaximumAroundTheMiddle()
    {
        var numbers = TilePivot.GetCompleteSet().Where(t => !t.IsHonor).Select(t => (int)t.Number).Distinct().OrderBy(n => n).ToList();

        Assert.Equal(TilePivot.MinNumber, numbers.First());
        Assert.Equal(TilePivot.MaxNumber, numbers.Last());
        Assert.Equal(TilePivot.MiddleNumber, (TilePivot.MinNumber + TilePivot.MaxNumber) / 2);
    }

    [Fact]
    public void TheTerminals_AreTheMinimumAndTheMaximumOfASuit()
    {
        var terminals = TilePivot.GetCompleteSet().Where(t => t.IsTerminal).Select(t => (int)t.Number).Distinct().OrderBy(n => n);

        Assert.Equal(new[] { (int)TilePivot.MinNumber, TilePivot.MaxNumber }, terminals);
    }

    [Fact]
    public void OnlyTheLastCopyOfTheMiddleNumberOfASuitIsRed()
    {
        var reds = TilePivot.GetCompleteSet(withRedDoras: true).Where(t => t.IsRedDora).ToList();

        Assert.Equal(3, reds.Count);
        Assert.All(reds, t => Assert.Equal(TilePivot.MiddleNumber, t.Number));
    }

    [Fact]
    public void ACompleteHand_IsMeldsAndAPairAndADealtHandIsOneTileShort()
    {
        Assert.Equal((HandPivot.MeldsCount * TileComboPivot.MeldSize) + TileComboPivot.PairSize, HandPivot.FullSize);
        Assert.Equal(14, HandPivot.FullSize);
        Assert.Equal(HandPivot.FullSize - 1, HandPivot.DealtSize);
        Assert.Equal(HandPivot.DealtSize, WallLayout.HandSize);
    }

    [Fact]
    public void ASquare_TakesEveryCopyOfATile()
    {
        Assert.Equal(TilePivot.CopiesCount, TileComboPivot.KanSize);
        var tile = TilePivot.GetCompleteSet().First();

        Assert.True(TileComboPivot.BuildPair(tile).IsPair);
        Assert.True(TileComboPivot.BuildBrelan(tile).IsBrelan);
        Assert.True(TileComboPivot.BuildSquare(tile).IsSquare);
    }
}
