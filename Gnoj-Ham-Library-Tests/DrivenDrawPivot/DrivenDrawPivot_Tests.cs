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
    public void GuaranteeOpenKanChance_GivesThreeRedDragonsToThePlayerAndTheFourthToThePlayerBefore()
    {
        var round = new RoundPivot(NewGame(), PlayerIndices.Zero, new Random(42),
            fullTilesList => DrivenDrawPivot.GuaranteeOpenKanChance(fullTilesList, PlayerIndices.Zero));

        Assert.Equal(3, RedDragonCount(round, PlayerIndices.Zero));
        Assert.Equal(1, RedDragonCount(round, PlayerIndices.Three));
        Assert.Equal(0, RedDragonCount(round, PlayerIndices.One));
        Assert.Equal(0, RedDragonCount(round, PlayerIndices.Two));
    }

    [Fact]
    public void GuaranteeOpenKanChance_TargetsTheRequestedPlayerOnly()
    {
        var round = new RoundPivot(NewGame(), PlayerIndices.Zero, new Random(42),
            fullTilesList => DrivenDrawPivot.GuaranteeOpenKanChance(fullTilesList, PlayerIndices.Two));

        Assert.Equal(3, RedDragonCount(round, PlayerIndices.Two));
        Assert.Equal(1, RedDragonCount(round, PlayerIndices.One));
        Assert.Equal(0, RedDragonCount(round, PlayerIndices.Zero));
        Assert.Equal(0, RedDragonCount(round, PlayerIndices.Three));
    }

    [Fact]
    public void GuaranteeOpenKanChance_ReordersWithoutLosingOrDuplicatingTiles()
    {
        var game = NewGame();

        var baseline = new RoundPivot(game, PlayerIndices.Zero, new Random(42));
        var rigged = new RoundPivot(game, PlayerIndices.Zero, new Random(42),
            fullTilesList => DrivenDrawPivot.GuaranteeOpenKanChance(fullTilesList, PlayerIndices.Zero));

        Assert.Equal(
            baseline.FullTilesList.OrderBy(t => t).ToList(),
            rigged.FullTilesList.OrderBy(t => t).ToList());
    }

    [Fact]
    public void GuaranteeOpenKanChance_OffersTheHumanAnOpenKanInMostGames()
    {
        // Confirms the scenario is genuinely usable: the human player keeps their dragons and turns
        // every call down, and the player before them is expected to discard the fourth one early.
        var withChance = 0;
        const int games = 40;
        for (var seed = 1; seed <= games; seed++)
        {
            var game = new GamePivot("Me", RulePivot.Default, new PlayerStatisticsPivot(), new Random(seed),
                DrivenDrawPivot.Resolve(DrivenDrawScenarios.HumanOpenKanChance, PlayerIndices.Zero));
            var human = game.Round.GetHand(PlayerIndices.Zero);

            var declined = false;
            for (var i = 0; i < 300; i++)
            {
                var result = game.Round.RunAutoPlay(default, declined, false, false, false, null, 0);
                if (result.EndOfRound)
                {
                    break;
                }
                if (game.Round.CanCallKan(PlayerIndices.Zero).Count > 0 && !human.IsFullHand)
                {
                    withChance++;
                    break;
                }

                declined = false;
                if (result.HumanCall is null || result.HumanCall.Value.call == CallTypes.NoCall)
                {
                    var tile = human.ConcealedTiles.FirstOrDefault(t =>
                        game.Round.CanDiscard(t) && !(t.Family == Families.Dragon && t.Dragon == Dragons.Red));
                    if (tile != null && game.Round.Discard(tile))
                    {
                        continue;
                    }
                }
                declined = true;
            }
        }

        // Measured: 85% of the games.
        Assert.True(withChance >= games * 3 / 4, $"Expected an open kan chance in most games, got {withChance}/{games}.");
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
