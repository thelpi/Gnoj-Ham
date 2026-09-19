using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class WallLayout_Tests
{
    private static List<TilePivot> Shuffled(int seed)
    {
        var random = new Random(seed);
        return TilePivot.GetCompleteSet(true).OrderBy(_ => random.NextDouble()).ToList();
    }

    [Fact]
    public void TheDeadWall_HoldsATileForEveryKanAndBothKindsOfIndicators()
    {
        Assert.Equal(14, WallLayout.DeadWallSize);
        Assert.Equal(5, WallLayout.IndicatorsCount);
    }

    [Fact]
    public void Deal_GivesEachPlayerAHandThenTheLiveWallThenTheDeadWallInThatOrder()
    {
        var tiles = Shuffled(1);

        var wall = new WallLayout(tiles);

        Assert.Equal(GamePivot.PlayersCount, wall.Hands.Count);
        Assert.All(wall.Hands, hand => Assert.Equal(13, hand.Count));
        Assert.Equal(70, wall.LiveWall.Count);
        Assert.Equal(4, wall.CompensationTiles.Count);
        Assert.Equal(5, wall.DoraIndicators.Count);
        Assert.Equal(5, wall.UraDoraIndicators.Count);

        // Every tile is dealt once, in the order of the shuffle.
        var dealt = wall.Hands.SelectMany(h => h)
            .Concat(wall.LiveWall)
            .Concat(wall.CompensationTiles)
            .Concat(wall.DoraIndicators)
            .Concat(wall.UraDoraIndicators);
        Assert.Equal(tiles, dealt);
    }

    [Theory]
    [InlineData(PlayerIndices.Zero, 0)]
    [InlineData(PlayerIndices.One, 13)]
    [InlineData(PlayerIndices.Two, 26)]
    [InlineData(PlayerIndices.Three, 39)]
    public void HandStart_IsWhereTheHandOfThePlayerBeginsInTheTilesDealt(PlayerIndices player, int start)
    {
        var tiles = Shuffled(2);
        var wall = new WallLayout(tiles);

        Assert.Equal(start, WallLayout.HandStart(player));
        Assert.Equal(tiles.GetRange(start, 13), wall.Hands[(int)player]);
    }

    [Fact]
    public void Deal_HandsOverCopies_SoTheRoundCanChangeThemWithoutTouchingTheTilesDealt()
    {
        var tiles = Shuffled(3);
        var first = tiles[0];

        var wall = new WallLayout(tiles);
        wall.Hands[0].Clear();
        wall.LiveWall.Clear();

        Assert.Equal(136, tiles.Count);
        Assert.Same(first, tiles[0]);
    }

    [Fact]
    public void Deal_WithTooFewTiles_Throws()
    {
        var tiles = Shuffled(4).Take(GamePivot.PlayersCount * 13).ToList();

        Assert.Throws<ArgumentException>(() => new WallLayout(tiles));
    }
}
