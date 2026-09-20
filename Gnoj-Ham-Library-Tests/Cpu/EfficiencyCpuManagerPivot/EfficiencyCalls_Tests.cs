using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class EfficiencyCalls_Tests
{
    // The player about to play has this hand, and the one before has just discarded the tile: the moment
    // the CPUs are asked whether to call. An open hand has a pon of 2s declared, the two tiles of it
    // being the "22s" of the hand notation (its third is the stolen one).
    private static (RoundPivot round, PlayerIndices caller) SetUpCall(string hand, string discard, bool open = false)
    {
        var round = RoundSetup.NewRound(1);
        var caller = round.CurrentPlayerIndex;

        RoundSetup.SetHand(round, caller, HandNotation.Tiles(hand));
        if (open)
        {
            round.GetHand(caller).DeclarePon(HandNotation.Tiles("222s")[2], Winds.East);
        }
        RoundSetup.AddToDiscard(round, round.PreviousPlayerIndex, HandNotation.Tiles(discard)[0]);
        RoundSetup.SetWaitForDiscard(round, false);

        return (round, caller);
    }

    [Fact]
    public void PonDecision_WhichBringsTheHandCloserToTenpai_Calls()
    {
        // Two melds, a pair of dragons (55z) and another (66z), and a partial group (79s): one tile from
        // tenpai. With the pon of 5z, and 1z discarded, it's tenpai.
        var (round, caller) = SetUpCall("123m456p55z66z79s1z", "5z");

        Assert.True(new EfficiencyCallsCpuManagerPivot(round).PonDecision(caller));
    }

    [Fact]
    public void PonDecision_WhichLeavesTheHandAsFarFromTenpai_Declines()
    {
        // Three melds, a pair of dragons (55z) and a partial group (89s): tenpai already, and after the
        // pon of 5z (and a discard of 8s or 9s) still tenpai, on a worse wait: no gain to open the hand for.
        var (round, caller) = SetUpCall("123m456p123s89s55z", "5z");
        Assert.Equal(Winds.East, round.Game.GetPlayerCurrentWind(caller));

        // The pon is worth making to the logic all the others are built on (the dealer's dragon).
        Assert.True(new EfficiencyPushFoldCpuManagerPivot(round).PonDecision(caller));
        Assert.False(new EfficiencyCallsCpuManagerPivot(round).PonDecision(caller));
    }

    [Fact]
    public void ChiiDecision_WhichBringsTheHandCloserToTenpai_Calls()
    {
        // An open hand with a pair of each of 5p, 6p and 9s, and a meld: one tile from tenpai. The chii
        // of 7p with 5p and 6p makes another meld and leaves a partial group (5p6p) and the pair 9s.
        var (round, _) = SetUpCall("22s123m55p66p8s99s", "7p", open: true);

        var (canChii, chiiTile) = new EfficiencyCallsCpuManagerPivot(round).ChiiDecision();

        Assert.True(canChii);
        Assert.Equal(HandNotation.Tiles("5p")[0], chiiTile);
    }

    [Fact]
    public void ChiiDecision_WhichLeavesTheHandAsFarFromTenpai_Declines()
    {
        // Two melds, a pair of 5p, two partial groups (6p8p, 8s9s) and a lone honor: one tile from tenpai.
        // The chii of 4p with 5p and 6p makes a third meld, but takes a tile of the pair and one of a
        // partial group: what's left (5p, 8p and 8s9s) is still one tile from tenpai.
        var (round, _) = SetUpCall("22s123m55p6p8p8s9s1z", "4p", open: true);

        var (canChiiForTheOthers, tileForTheOthers) = new EfficiencyPushFoldCpuManagerPivot(round).ChiiDecision();
        var (canChii, chiiTile) = new EfficiencyCallsCpuManagerPivot(round).ChiiDecision();

        Assert.True(canChiiForTheOthers);
        Assert.Equal(HandNotation.Tiles("5p")[0], tileForTheOthers);
        Assert.True(canChii);
        Assert.Null(chiiTile);
    }

    [Fact]
    public void WholeGames_PlayedByCallsCpus_GoToTheEnd()
    {
        for (var seed = 1; seed <= 3; seed++)
        {
            var factories = Enum.GetValues<PlayerIndices>().ToDictionary(
                i => i,
                i => (Func<RoundPivot, CpuManagerBasePivot>)(round => i == PlayerIndices.Zero || seed == 3
                    ? new EfficiencyCallsCpuManagerPivot(round)
                    : new EfficiencyPushFoldCpuManagerPivot(round)));
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
    public void TheCatalog_ListsTheCallsCpu()
    {
        Assert.Contains(CpuManagerCatalog.Implementations, o => o.Type == typeof(EfficiencyCallsCpuManagerPivot));
    }
}
