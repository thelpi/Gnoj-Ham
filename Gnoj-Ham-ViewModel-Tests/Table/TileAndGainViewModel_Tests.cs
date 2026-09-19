using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;
using Gnoj_Ham_ViewModel;

namespace Gnoj_Ham_ViewModel_Tests;

public class TileAndGainViewModel_Tests
{
    // The engine's tile factories are internal: the full deck of a round is the public way to get tiles.
    private static readonly IReadOnlyList<TilePivot> Deck =
        new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(1)).Round.FullTilesList;

    private static TilePivot Tile(Families family, int number)
        => Deck.First(t => t.Family == family && t.Number == number);

    [Fact]
    public void FaceUpTile_ExposesItsImageAndTooltip()
    {
        var tile = Tile(Families.Circle, 5);

        var viewModel = new TileViewModel(tile);

        Assert.Equal(tile.ToResourceName(), viewModel.ImageResourceName);
        Assert.Equal(tile.TileDisplay(), viewModel.ToolTip);
    }

    [Fact]
    public void FaceDownTile_HidesItsFaceAndTooltip()
    {
        var viewModel = new TileViewModel(Tile(Families.Circle, 5), isConcealed: true);

        Assert.Equal(TileViewModel.ConcealedImageResourceName, viewModel.ImageResourceName);
        Assert.Null(viewModel.ToolTip);
    }

    [Theory]
    [InlineData(AnglePivot.A0, 0, false)]
    [InlineData(AnglePivot.A90, 90, true)]
    [InlineData(AnglePivot.A180, 180, false)]
    [InlineData(AnglePivot.A270, 270, true)]
    public void Angle_GivesDegreesAndWhetherTheTileLiesOnItsSide(AnglePivot angle, int degrees, bool sideways)
    {
        var viewModel = new TileViewModel(Tile(Families.Bamboo, 1), angle);

        Assert.Equal(degrees, viewModel.AngleDegrees);
        Assert.Equal(sideways, viewModel.IsSideways);
    }

    [Fact]
    public void TileDisplay_NamesDragonsWindsAndNumberedTiles()
    {
        Assert.Equal("Dragon\r\nRouge", Deck.First(t => t.Dragon == Dragons.Red).TileDisplay());
        Assert.Equal("Vent\r\nSud", Deck.First(t => t.Wind == Winds.South).TileDisplay());
        Assert.Equal("Bambou\r\n3", Tile(Families.Bamboo, 3).TileDisplay());
    }

    [Theory]
    [InlineData(1200, "+1200", GainKind.Gain)]
    [InlineData(-300, "-300", GainKind.Loss)]
    [InlineData(0, "0", GainKind.None)]
    public void Gain_HasAnExplicitPlusOnlyForAGain(int value, string text, GainKind kind)
    {
        var gain = new GainViewModel(value);

        Assert.Equal(text, gain.Text);
        Assert.Equal(kind, gain.Kind);
    }
}
