using System.Reflection;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class SuufonRenda_Tests
{
    private static RoundPivot NewRound(bool useSuufonRenda = true)
        => new GamePivot(
            new RulePivot
            {
                InitialPointsRule = InitialPointsRules.K25,
                EndOfGameRule = EndOfGameRules.EnchousenAndTobi,
                UseRedDoras = true,
                UseNagashiMangan = true,
                UseSuufonRenda = useSuufonRenda
            },
            PlayerPivot.BuildPlayers(null), new Random(1)).Round;

    private static DiscardHistoryPivot DiscardHistory(RoundPivot round)
        => (DiscardHistoryPivot)typeof(RoundPivot)
            .GetField("_discardHistory", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(round)!;

    private static List<List<TilePivot>> DiscardsField(RoundPivot round)
        => (List<List<TilePivot>>)typeof(DiscardHistoryPivot)
            .GetField("_discards", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(DiscardHistory(round))!;

    private static List<PlayerIndices> HistoryField(RoundPivot round)
        => (List<PlayerIndices>)typeof(DiscardHistoryPivot)
            .GetField("_playerIndexHistory", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(DiscardHistory(round))!;

    // Simulates all four players discarding "tile" as their first, uninterrupted discard.
    private static void SetFirstDiscards(RoundPivot round, TilePivot tile)
    {
        var discards = DiscardsField(round);
        var history = HistoryField(round);

        foreach (var playerIndex in Enum.GetValues<PlayerIndices>())
        {
            discards[(int)playerIndex].Add(tile);
            history.Insert(0, playerIndex);
        }
    }

    [Fact]
    public void IsSuufonRenda_DisabledByRuleset_IsFalseEvenIfConditionsMatch()
    {
        var round = NewRound(useSuufonRenda: false);
        var tilesSet = TilePivot.GetCompleteSet(false);
        SetFirstDiscards(round, TilePivot.GetTile(tilesSet, Families.Wind, wind: Winds.East));

        Assert.False(round.IsSuufonRenda);
    }

    [Fact]
    public void IsSuufonRenda_FourIdenticalWindDiscardsOnFirstTurn_IsTrue()
    {
        var round = NewRound();
        var tilesSet = TilePivot.GetCompleteSet(false);
        SetFirstDiscards(round, TilePivot.GetTile(tilesSet, Families.Wind, wind: Winds.East));

        Assert.True(round.IsSuufonRenda);
    }

    [Fact]
    public void IsSuufonRenda_NotAllTheSameWind_IsFalse()
    {
        var round = NewRound();
        var tilesSet = TilePivot.GetCompleteSet(false);
        var discards = DiscardsField(round);
        var history = HistoryField(round);

        discards[0].Add(TilePivot.GetTile(tilesSet, Families.Wind, wind: Winds.East));
        discards[1].Add(TilePivot.GetTile(tilesSet, Families.Wind, wind: Winds.South));
        discards[2].Add(TilePivot.GetTile(tilesSet, Families.Wind, wind: Winds.East));
        discards[3].Add(TilePivot.GetTile(tilesSet, Families.Wind, wind: Winds.East));
        foreach (var playerIndex in Enum.GetValues<PlayerIndices>())
        {
            history.Insert(0, playerIndex);
        }

        Assert.False(round.IsSuufonRenda);
    }

    [Fact]
    public void IsSuufonRenda_InterruptedByACall_IsFalse()
    {
        // Same four identical wind discards, but a call happened in between (playerIndexHistory only
        // has 3 entries instead of 4, as a real call would have cleared and partially rebuilt it).
        var round = NewRound();
        var tilesSet = TilePivot.GetCompleteSet(false);
        var windEast = TilePivot.GetTile(tilesSet, Families.Wind, wind: Winds.East);
        var discards = DiscardsField(round);
        var history = HistoryField(round);

        foreach (var playerIndex in Enum.GetValues<PlayerIndices>())
        {
            discards[(int)playerIndex].Add(windEast);
        }
        history.AddRange(new[] { PlayerIndices.Three, PlayerIndices.Two, PlayerIndices.One });

        Assert.False(round.IsSuufonRenda);
    }

    [Fact]
    public void EndOfRound_SuufonRenda_IsAbortiveDrawWithNoTenpaiPaymentAndDealerRepeats()
    {
        var round = NewRound();
        var tilesSet = TilePivot.GetCompleteSet(false);
        SetFirstDiscards(round, TilePivot.GetTile(tilesSet, Families.Wind, wind: Winds.East));

        var info = round.EndOfRound(null);

        Assert.True(info.Ryuukyoku);
        Assert.False(info.ToNextEast);
        Assert.Empty(info.PlayersInfo);
    }

    [Fact]
    public void RunAutoPlay_SuufonRenda_EndsTheRoundAsAbortiveDraw()
    {
        var round = NewRound();
        var tilesSet = TilePivot.GetCompleteSet(false);
        SetFirstDiscards(round, TilePivot.GetTile(tilesSet, Families.Wind, wind: Winds.East));

        var result = round.RunAutoPlay(new CancellationToken(), false, false, false, false, null, 0);

        Assert.True(result.EndOfRound);
        Assert.Null(result.RonPlayerId);
    }
}
