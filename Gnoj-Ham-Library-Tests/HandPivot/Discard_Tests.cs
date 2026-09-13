using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class Discard_Tests
{
    [Fact]
    public void Discard_TileWithARedDoraTwinListedFirst_RemovesExactlyTheDiscardedInstance()
    {
        // Regression: TilePivot equality ignores IsRedDora, so when a red and a plain copy of the
        // same tile both sit in hand, the old "_concealedTiles.Remove(tile)" (value-based) could grab
        // whichever of the two came first in the list - silently discarding the red dora while the
        // plain tile (the one actually clicked/chosen) stayed behind, making the red marker vanish.
        var tilesSet = TilePivot.GetCompleteSet(true);
        var redFive = TilePivot.GetTile(tilesSet, Families.Circle, number: 5, isRedDora: true);
        var plainFive = TilePivot.GetTile(tilesSet, Families.Circle, number: 5, isRedDora: false);

        // Red tile listed first, so a value-based Remove(plainFive) would incorrectly hit it instead.
        var hand = new HandPivot(new List<TilePivot> { redFive, plainFive });

        hand.Discard(plainFive);

        var remaining = Assert.Single(hand.ConcealedTiles);
        Assert.Same(redFive, remaining);
    }
}
