using System.Reflection;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class KanDoraTiming_Tests
{
    private static RoundPivot NewRound()
        => new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(1)).Round;

    // Simulates the state CallKan leaves behind, without going through a full legal kan setup.
    private static void SetLastKanWasOpen(RoundPivot round, bool isOpen)
        => typeof(RoundPivot).GetField("_lastKanWasOpen", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(round, isOpen);

    private static void SetWaitForDiscard(RoundPivot round, bool value)
        => typeof(RoundPivot).GetField("_waitForDiscard", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(round, value);

    [Fact]
    public void ResolveKanDoraReveal_Ankan_RevealsImmediately()
    {
        var round = NewRound();

        Assert.Equal(1, round.VisibleDorasCount);

        SetLastKanWasOpen(round, false);
        round.ResolveKanDoraReveal();

        Assert.Equal(2, round.VisibleDorasCount);
    }

    [Fact]
    public void ResolveKanDoraReveal_OpenKan_StaysHiddenUntilTheFollowingDiscard()
    {
        var round = NewRound();

        SetLastKanWasOpen(round, true);
        round.ResolveKanDoraReveal();

        // Not revealed yet: e.g. a rinshan kaihou win right here must never see it.
        Assert.Equal(1, round.VisibleDorasCount);

        SetWaitForDiscard(round, true);
        var tile = round.GetHand(PlayerIndices.Zero).ConcealedTiles[0];
        Assert.True(round.Discard(tile));

        Assert.Equal(2, round.VisibleDorasCount);
    }

    [Fact]
    public void ResolveKanDoraReveal_ChainOfOpenKans_AllFlushTogetherOnTheEventualDiscard()
    {
        var round = NewRound();

        SetLastKanWasOpen(round, true);
        round.ResolveKanDoraReveal();
        round.ResolveKanDoraReveal();

        Assert.Equal(1, round.VisibleDorasCount);

        SetWaitForDiscard(round, true);
        var tile = round.GetHand(PlayerIndices.Zero).ConcealedTiles[0];
        Assert.True(round.Discard(tile));

        Assert.Equal(3, round.VisibleDorasCount);
    }

    [Fact]
    public void ResolveKanDoraReveal_OpenKanNeverFollowedByADiscard_NeverReveals()
    {
        // Models a rinshan kaihou win right after an open kan: the round ends before any discard,
        // so the pending reveal must simply never apply.
        var round = NewRound();

        SetLastKanWasOpen(round, true);
        round.ResolveKanDoraReveal();

        Assert.Equal(1, round.VisibleDorasCount);
    }
}
