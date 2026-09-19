using Gnoj_Ham_Library;

namespace Gnoj_Ham_Library_Tests;

public class KindIndex_Tests
{
    [Fact]
    public void EveryKindOfTile_HasItsOwnIndexBelowTheKindsCount()
    {
        var tilesSet = TilePivot.GetCompleteSet(true);

        var kinds = tilesSet.GroupBy(t => t.KindIndex).ToList();

        Assert.Equal(TilePivot.KindsCount, kinds.Count);
        Assert.Equal(Enumerable.Range(0, TilePivot.KindsCount), kinds.Select(g => g.Key).Order());
        Assert.All(kinds, g => Assert.Equal(TilePivot.CopiesCount, g.Count()));
        Assert.All(kinds, g => Assert.Single(g.Distinct()));
    }

    [Fact]
    public void TheKinds_AreNumberedSuitBySuitThenTheWindsThenTheDragons()
    {
        var tilesSet = TilePivot.GetCompleteSet(false);

        var ordered = tilesSet.DistinctBy(t => t.KindIndex).OrderBy(t => t.KindIndex).ToList();

        Assert.Equal(ordered.OrderBy(t => t).ToList(), ordered);
    }
}
