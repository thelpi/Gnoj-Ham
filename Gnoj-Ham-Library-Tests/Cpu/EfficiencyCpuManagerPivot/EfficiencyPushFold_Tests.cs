using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class EfficiencyPushFold_Tests
{
    // The current player has to discard, with this hand, against an opponent in riichi who has already
    // thrown the given tiles: the ones of the hand which are certainly safe.
    private static RoundPivot SetUpAgainstRiichi(string hand, params string[] safeTiles)
    {
        var round = RoundSetup.NewRound(1);
        var player = round.CurrentPlayerIndex;
        var opponent = player.RelativePlayerIndex(1);

        RoundSetup.SetHand(round, player, HandNotation.Tiles(hand));
        RoundSetup.SetWaitForDiscard(round, true);
        RoundSetup.SetRiichi(round, opponent);
        foreach (var safeTile in safeTiles)
        {
            RoundSetup.AddToDiscard(round, opponent, HandNotation.Tiles(safeTile)[0]);
        }

        return round;
    }

    [Fact]
    public void DiscardDecision_AgainstRiichi_WithAHandOneTileFromTenpai_KeepsItInsteadOfFolding()
    {
        // Two melds (234m 567p), two partial groups (45s, 79s), a pair (99p) and two lone honors: one tile
        // from tenpai, and the pair is the only tile which is certainly safe.
        var round = SetUpAgainstRiichi("234m567p45s79s99p1z5z", "9p");

        var folding = new EfficiencyCpuManagerPivot(round).DiscardDecision();
        var pushing = new EfficiencyPushFoldCpuManagerPivot(round).DiscardDecision();

        Assert.Equal(HandNotation.Tiles("9p")[0], folding);
        Assert.True(pushing.IsHonor, $"Discarded {pushing.Family} {pushing.Number}");
    }

    [Fact]
    public void DiscardDecision_AgainstRiichi_WithAHandFarFromTenpai_FoldsLikeTheOthers()
    {
        var round = SetUpAgainstRiichi("1m4m7m1p4p7p1s4s7s1z2z3z4z5z", "7m");

        var folding = new EfficiencyCpuManagerPivot(round).DiscardDecision();
        var pushing = new EfficiencyPushFoldCpuManagerPivot(round).DiscardDecision();

        Assert.Equal(HandNotation.Tiles("7m")[0], folding);
        Assert.Equal(folding, pushing);
    }

    [Fact]
    public void DiscardDecision_AgainstRiichi_AmongTheDiscardsThatKeepTheHandClose_ChoosesTheSafest()
    {
        // The discards that keep the hand one tile from tenpai are the two lone honors: the one the
        // opponent already threw (5z) goes, though 1z comes first - and not the pair, also safe, which is
        // worth more to the hand.
        var round = SetUpAgainstRiichi("234m567p45s79s99p1z5z", "9p", "5z");

        var pushing = new EfficiencyPushFoldCpuManagerPivot(round).DiscardDecision();

        Assert.Equal(HandNotation.Tiles("5z")[0], pushing);
    }

    [Fact]
    public void DiscardDecision_WithNoOneDangerous_IsTheSameAsTheEfficiencyCpu()
    {
        var round = RoundSetup.NewRound(1);
        RoundSetup.SetHand(round, round.CurrentPlayerIndex, HandNotation.Tiles("234m567p45s79s99p1z5z"));
        RoundSetup.SetWaitForDiscard(round, true);

        var efficiency = new EfficiencyCpuManagerPivot(round).DiscardDecision();
        var pushFold = new EfficiencyPushFoldCpuManagerPivot(round).DiscardDecision();

        Assert.Equal(efficiency, pushFold);
    }

    [Fact]
    public void WholeGames_PlayedByPushFoldCpus_GoToTheEnd()
    {
        for (var seed = 1; seed <= 3; seed++)
        {
            var factories = Enum.GetValues<PlayerIndices>().ToDictionary(
                i => i,
                i => (Func<RoundPivot, CpuManagerBasePivot>)(round => i == PlayerIndices.Zero || seed == 3
                    ? new EfficiencyPushFoldCpuManagerPivot(round)
                    : new EfficiencyCpuManagerPivot(round)));
            var game = new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(seed), factories);

            EndOfRoundInformationsPivot end;
            do
            {
                var result = game.Round.RunAutoPlay(CancellationToken.None);
                end = game.NextRound(result.RonPlayerId);
            } while (!end.EndOfGame);

            Assert.True(end.EndOfGame);
        }
    }

    [Fact]
    public void TheCatalog_ListsThePushFoldCpu()
    {
        Assert.Contains(CpuManagerCatalog.Implementations, o => o.Type == typeof(EfficiencyPushFoldCpuManagerPivot));
    }
}
