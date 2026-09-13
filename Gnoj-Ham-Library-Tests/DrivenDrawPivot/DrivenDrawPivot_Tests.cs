using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class DrivenDrawPivot_Tests
{
    private static GamePivot NewGame()
        => new(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(1));

    private static int RedDragonCount(RoundPivot round, PlayerIndices playerIndex)
        => round.GetHand(playerIndex).ConcealedTiles.Count(t => t.Family == Families.Dragon && t.Dragon == Dragons.Red);

    private static int GreenDragonCount(RoundPivot round, PlayerIndices playerIndex)
        => round.GetHand(playerIndex).ConcealedTiles.Count(t => t.Family == Families.Dragon && t.Dragon == Dragons.Green);

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

    [Fact]
    public void GuaranteeTwoInitialKans_RigsStartingHandWithBothDragonQuads()
    {
        var round = new RoundPivot(NewGame(), PlayerIndices.Zero, new Random(42),
            fullTilesList => DrivenDrawPivot.GuaranteeTwoInitialKans(fullTilesList, PlayerIndices.Zero));

        Assert.Equal(4, RedDragonCount(round, PlayerIndices.Zero));
        Assert.Equal(4, GreenDragonCount(round, PlayerIndices.Zero));
    }

    [Fact]
    public void GuaranteeTwoInitialKans_TargetsTheRequestedPlayerOnly()
    {
        var round = new RoundPivot(NewGame(), PlayerIndices.Zero, new Random(42),
            fullTilesList => DrivenDrawPivot.GuaranteeTwoInitialKans(fullTilesList, PlayerIndices.One));

        Assert.Equal(4, RedDragonCount(round, PlayerIndices.One));
        Assert.Equal(4, GreenDragonCount(round, PlayerIndices.One));
        Assert.Equal(0, RedDragonCount(round, PlayerIndices.Zero));
        Assert.Equal(0, GreenDragonCount(round, PlayerIndices.Zero));
    }

    [Fact]
    public void GuaranteeTwoInitialKans_ReordersWithoutLosingOrDuplicatingTiles()
    {
        var game = NewGame();

        var baseline = new RoundPivot(game, PlayerIndices.Zero, new Random(42));
        var rigged = new RoundPivot(game, PlayerIndices.Zero, new Random(42),
            fullTilesList => DrivenDrawPivot.GuaranteeTwoInitialKans(fullTilesList, PlayerIndices.One));

        Assert.Equal(
            baseline.FullTilesList.OrderBy(t => t).ToList(),
            rigged.FullTilesList.OrderBy(t => t).ToList());
    }

    [Fact]
    public void GuaranteeTwoInitialKans_BothKansAreActuallyDeclaredBackToBack()
    {
        // Confirms the scenario is genuinely usable, not just "4+4 tiles present": run through the
        // real auto-play loop (the CPU standing in for a human clicking through the same prompts, and
        // always taking a concealed kan when offered) and check both kans actually get declared.
        var round = new RoundPivot(NewGame(), PlayerIndices.Zero, new Random(42),
            fullTilesList => DrivenDrawPivot.GuaranteeTwoInitialKans(fullTilesList, PlayerIndices.Zero));

        var kanCount = 0;
        round.CallNotifier += e =>
        {
            if (e.Action == CallTypes.Kan && e.PlayerIndex == PlayerIndices.Zero)
            {
                kanCount++;
            }
        };

        round.RunAutoPlay(new CancellationToken());

        Assert.True(kanCount >= 2, $"Expected at least 2 kans from PlayerIndices.Zero, got {kanCount}.");
    }
}
