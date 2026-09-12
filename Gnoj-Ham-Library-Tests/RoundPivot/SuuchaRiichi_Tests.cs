using System.Reflection;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class SuuchaRiichi_Tests
{
    private static RoundPivot NewRound()
        => new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(1)).Round;

    private static void SetAllPlayersRiichi(RoundPivot round)
    {
        var riichisField = typeof(RoundPivot).GetField("_riichis", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var riichis = (List<RiichiPivot?>)riichisField.GetValue(round)!;

        var riichiCtor = typeof(RiichiPivot).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new[] { typeof(int), typeof(bool), typeof(TilePivot), typeof(IDictionary<PlayerIndices, int>) },
            null)!;

        var opponentsRank = Enum.GetValues<PlayerIndices>().ToDictionary(i => i, i => -1);

        for (var i = 0; i < 4; i++)
        {
            riichis[i] = (RiichiPivot)riichiCtor.Invoke(new object[] { i, false, round.FullTilesList[0], opponentsRank });
        }
    }

    [Fact]
    public void IsSuuchaRiichi_AllFourPlayersRiichi_IsTrue()
    {
        var round = NewRound();

        Assert.False(round.IsSuuchaRiichi);

        SetAllPlayersRiichi(round);

        Assert.True(round.IsSuuchaRiichi);
    }

    [Fact]
    public void EndOfRound_SuuchaRiichi_IsAbortiveDrawWithNoTenpaiPaymentAndDealerRepeats()
    {
        // Suucha riichi (all four players riichi) is an abortive draw: unlike a regular exhaustive
        // draw, there is no tenpai/noten payment and the dealer always repeats (renchan), regardless
        // of tenpai status. Riichi sticks carry over: this is expressed by "Ryuukyoku" staying true,
        // which GamePivot.NextRound uses to decide not to reset PendingRiichiCount.
        var round = NewRound();
        SetAllPlayersRiichi(round);

        var info = round.EndOfRound(null);

        Assert.True(info.Ryuukyoku);
        Assert.False(info.ToNextEast);
        Assert.Empty(info.PlayersInfo);
        foreach (var playerIndex in Enum.GetValues<PlayerIndices>())
        {
            Assert.Equal(0, info.GetPlayerPointsGain(playerIndex));
        }
    }

    [Fact]
    public void RunAutoPlay_SuuchaRiichi_EndsTheRoundAsAbortiveDraw()
    {
        var round = new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(1)).Round;
        SetAllPlayersRiichi(round);

        var result = round.RunAutoPlay(new CancellationToken(), false, false, false, false, null, 0);

        Assert.True(result.EndOfRound);
        Assert.Null(result.RonPlayerId);
    }
}
