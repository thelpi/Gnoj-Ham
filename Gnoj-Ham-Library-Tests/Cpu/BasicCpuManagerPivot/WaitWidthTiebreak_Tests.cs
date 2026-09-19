using System.Reflection;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class WaitWidthTiebreak_Tests
{
    private static RoundPivot NewRound(int seed)
        => new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(seed)).Round;

    private static void SetHand(RoundPivot round, PlayerIndices playerIndex, List<TilePivot> concealedTiles)
    {
        var hands = (List<HandPivot>)typeof(RoundPivot)
            .GetField("_hands", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(round)!;
        hands[(int)playerIndex] = new HandPivot(concealedTiles);
    }

    private static void SetWaitForDiscard(RoundPivot round, bool value)
    {
        typeof(RoundPivot)
            .GetField("_waitForDiscard", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(round, value);
    }

    // Builds a hand with exactly two tenpai-preserving discards, leaving two differently-shaped waits:
    // 123m 456m 789m (three complete runs) + 11p (pair) + 3s4s6s (three floating sou tiles) is 14
    // tiles. Discarding 3s leaves 4s6s, a kanchan waiting on 5s only (one kind). Discarding 6s leaves
    // 3s4s, a ryanmen waiting on 2s and 5s (two kinds, wider). Discarding 4s breaks tenpai entirely
    // (3s/6s aren't a valid taatsu), so exactly these two choices remain.
    private static List<TilePivot> BuildKanchanVsRyanmenHand(IReadOnlyList<TilePivot> tilesSet) => new()
    {
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 1),
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 2),
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 3),
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 4),
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 5),
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 6),
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 7),
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 8),
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 9),
        TilePivot.GetTile(tilesSet, Families.Circle, number: 1),
        TilePivot.GetTile(tilesSet, Families.Circle, number: 1),
        TilePivot.GetTile(tilesSet, Families.Bamboo, number: 3),
        TilePivot.GetTile(tilesSet, Families.Bamboo, number: 4),
        TilePivot.GetTile(tilesSet, Families.Bamboo, number: 6),
    };

    // Policy (applies to every current and future BasicCpuManagerPivot subclass, same as furiten
    // avoidance): a behavior added to the base class is inherited by every variant automatically,
    // unless that variant deliberately opts out (BasicNoWaitWidthCpuManagerPivot, for A/B measurement
    // only).
    [Theory]
    [InlineData(typeof(BasicCpuManagerPivot))]
    [InlineData(typeof(NoDefenseCpuManagerPivot))]
    [InlineData(typeof(FullDefenseCpuManagerPivot))]
    public void WaitWidthAwareVariants_PreferTheWiderLiveWaitOverTheNarrowerOne(Type cpuManagerType)
    {
        var round = NewRound(1);
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand = BuildKanchanVsRyanmenHand(tilesSet);
        var bamboo3 = TilePivot.GetTile(tilesSet, Families.Bamboo, number: 3);
        var bamboo6 = TilePivot.GetTile(tilesSet, Families.Bamboo, number: 6);

        var currentPlayer = round.CurrentPlayerIndex;
        SetHand(round, currentPlayer, hand);
        SetWaitForDiscard(round, true);

        var tenpaiChoices = round.ExtractDiscardChoicesFromTenpai(currentPlayer);
        Assert.Equal(new[] { bamboo3, bamboo6 }, tenpaiChoices.OrderBy(t => t.Number));

        var cpuManager = (Gnoj_Ham_Library.BasicCpuManagerPivot)Activator.CreateInstance(cpuManagerType, round)!;
        var choice = cpuManager.DiscardDecision();

        // Discarding 6s keeps 3s4s (ryanmen on 2s/5s, two live kinds) - wider than discarding 3s, which
        // keeps 4s6s (kanchan on 5s only, one kind).
        Assert.Equal(bamboo6, choice);
    }

    [Fact]
    public void BasicNoWaitWidthCpuManagerPivot_ReproducesTheOriginalWaitWidthBlindChoice()
    {
        var round = NewRound(1);
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand = BuildKanchanVsRyanmenHand(tilesSet);
        var bamboo3 = TilePivot.GetTile(tilesSet, Families.Bamboo, number: 3);

        var currentPlayer = round.CurrentPlayerIndex;
        SetHand(round, currentPlayer, hand);
        SetWaitForDiscard(round, true);

        // Same scenario as above, but the original heuristic (no wait-width awareness) orders tied
        // candidates (safety, dora) by descending distance-to-middle only: 3s (distance 2) beats 6s
        // (distance 1) - it just happens to be the one that leaves the narrower, single-kind wait.
        var noWaitWidthChoice = new BasicNoWaitWidthCpuManagerPivot(round).DiscardDecision();

        Assert.Equal(bamboo3, noWaitWidthChoice);
    }
}
