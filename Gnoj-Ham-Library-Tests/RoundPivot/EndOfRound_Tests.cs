using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class EndOfRound_Tests
{
    private static RoundPivot NewRound()
        => new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(1)).Round;

    [Fact]
    public void EndOfRound_MultipleSimultaneousNagashiManganWinners_DoesNotThrow()
    {
        // A freshly dealt round: every hand is concealed and every discard pile is empty, so every
        // player vacuously satisfies CheckForNagashiMangan's per-player conditions. Combined with
        // UseNagashiMangan (on by default) and a ryuukyoku (ronPlayerIndex: null), this reaches the
        // "multiple winners" branch of EndOfRound with no ron player to compare against.
        var round = NewRound();

        var info = round.EndOfRound(null);

        Assert.NotNull(info);
    }

    [Fact]
    public void EndOfRound_MultipleSimultaneousNagashiManganWinners_AppliesAtamaHane()
    {
        // Atama-hane: when several players complete nagashi mangan on the same ryuukyoku, only the
        // one closest to the dealer (East -> South -> West -> North) is kept as the actual winner;
        // the three others pay their share (self-draw-style payout), same as a regular tsumo.
        var round = NewRound();
        var expectedWinner = Enum.GetValues<PlayerIndices>()
            .OrderBy(i => (int)round.Game.GetPlayerCurrentWind(i))
            .First();

        var info = round.EndOfRound(null);

        Assert.Equal(4, info.PlayersInfo.Count);

        var winnerInfo = info.PlayersInfo.Single(pi => pi.HandPointsGain > 0);
        Assert.Equal(expectedWinner, winnerInfo.Index);
        Assert.Contains(YakuPivot.NagashiMangan, winnerInfo.Yakus!);

        foreach (var loserInfo in info.PlayersInfo.Where(pi => pi.Index != expectedWinner))
        {
            Assert.Equal(0, loserInfo.HandPointsGain);
            Assert.True(loserInfo.PointsGain < 0);
        }
    }

    [Fact]
    public void EndOfRound_HumanAndCpuPlayers_SetsIsCpuCorrectly()
    {
        // Regression: PlayerInformationsPivot.IsCpu used to be assigned from Game.IsHuman(...)
        // (inverted). PlayerSavePivot.UpdateAndSave looks up "FirstOrDefault(_ => !_.IsCpu)" to find
        // the human's own hand for stats tracking - with the inversion, it silently picked a CPU
        // player's hand instead, corrupting the saved stats after every game played against CPUs.
        var ruleset = RulePivot.Default with { UseNagashiMangan = false };
        var game = new GamePivot("Me", ruleset, null, new Random(1));

        var info = game.Round.EndOfRound(null);

        Assert.Equal(4, info.PlayersInfo.Count);

        var humanInfo = info.PlayersInfo.Single(pi => pi.Index == game.HumanPlayerIndex!.Value);
        Assert.False(humanInfo.IsCpu);

        foreach (var cpuInfo in info.PlayersInfo.Where(pi => pi.Index != game.HumanPlayerIndex!.Value))
        {
            Assert.True(cpuInfo.IsCpu);
        }
    }
}
