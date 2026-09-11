using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class DrivenDrawPivot_Tests
{
    private static GamePivot NewGame()
        => new(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(1));

    private static int RedDragonCount(RoundPivot round, PlayerIndices playerIndex)
        => round.GetHand(playerIndex).ConcealedTiles.Count(t => t.Family == Families.Dragon && t.Dragon == Dragons.Red);

    [Fact]
    public void GuaranteeInitialKan_RigsStartingHandWithFourRedDragons()
    {
        var round = new RoundPivot(NewGame(), PlayerIndices.Zero, new Random(42),
            fullTilesList => DrivenDrawPivot.GuaranteeInitialKan(fullTilesList, PlayerIndices.Zero));

        Assert.Equal(4, RedDragonCount(round, PlayerIndices.Zero));
    }

    [Fact]
    public void GuaranteeInitialKan_TargetsTheRequestedPlayerOnly()
    {
        var round = new RoundPivot(NewGame(), PlayerIndices.Zero, new Random(42),
            fullTilesList => DrivenDrawPivot.GuaranteeInitialKan(fullTilesList, PlayerIndices.One));

        Assert.Equal(4, RedDragonCount(round, PlayerIndices.One));
        Assert.Equal(0, RedDragonCount(round, PlayerIndices.Zero));
    }

    [Fact]
    public void GuaranteeInitialKan_ReordersWithoutLosingOrDuplicatingTiles()
    {
        var game = NewGame();

        var baseline = new RoundPivot(game, PlayerIndices.Zero, new Random(42));
        var rigged = new RoundPivot(game, PlayerIndices.Zero, new Random(42),
            fullTilesList => DrivenDrawPivot.GuaranteeInitialKan(fullTilesList, PlayerIndices.One));

        Assert.Equal(
            baseline.FullTilesList.OrderBy(t => t).ToList(),
            rigged.FullTilesList.OrderBy(t => t).ToList());
    }
}
