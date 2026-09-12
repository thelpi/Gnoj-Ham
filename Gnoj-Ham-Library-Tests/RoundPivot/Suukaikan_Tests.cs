using System.Reflection;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class Suukaikan_Tests
{
    private static RoundPivot NewRound()
        => new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(1)).Round;

    private static List<HandPivot> HandsField(RoundPivot round)
        => (List<HandPivot>)typeof(RoundPivot)
            .GetField("_hands", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(round)!;

    // Adds a concealed kan combo (four copies of the given tile) to a player's declared combinations,
    // bypassing the real DeclareKan flow (which would require an actual matching concealed pung/tiles).
    private static void AddKan(RoundPivot round, PlayerIndices playerIndex, TilePivot tile)
    {
        var hand = HandsField(round)[(int)playerIndex];
        var declared = (List<TileComboPivot>)typeof(HandPivot)
            .GetField("_declaredCombinations", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(hand)!;

        declared.Add(TileComboPivot.BuildSquare(tile));
    }

    [Fact]
    public void IsSuukaikan_FourKansByFourDifferentPlayers_IsTrue()
    {
        var round = NewRound();
        var tilesSet = TilePivot.GetCompleteSet(false);

        AddKan(round, PlayerIndices.Zero, TilePivot.GetTile(tilesSet, Families.Circle, number: 1));
        AddKan(round, PlayerIndices.One, TilePivot.GetTile(tilesSet, Families.Circle, number: 9));
        AddKan(round, PlayerIndices.Two, TilePivot.GetTile(tilesSet, Families.Bamboo, number: 1));
        AddKan(round, PlayerIndices.Three, TilePivot.GetTile(tilesSet, Families.Bamboo, number: 9));

        Assert.True(round.IsSuukaikan);
    }

    [Fact]
    public void IsSuukaikan_FourKansBySinglePlayer_IsFalse()
    {
        // All four kans by the same player: the round continues instead, to let them try for suukantsu.
        var round = NewRound();
        var tilesSet = TilePivot.GetCompleteSet(false);

        AddKan(round, PlayerIndices.Zero, TilePivot.GetTile(tilesSet, Families.Circle, number: 1));
        AddKan(round, PlayerIndices.Zero, TilePivot.GetTile(tilesSet, Families.Circle, number: 9));
        AddKan(round, PlayerIndices.Zero, TilePivot.GetTile(tilesSet, Families.Bamboo, number: 1));
        AddKan(round, PlayerIndices.Zero, TilePivot.GetTile(tilesSet, Families.Bamboo, number: 9));

        Assert.False(round.IsSuukaikan);
    }

    [Fact]
    public void IsSuukaikan_OnlyThreeKansTotal_IsFalse()
    {
        var round = NewRound();
        var tilesSet = TilePivot.GetCompleteSet(false);

        AddKan(round, PlayerIndices.Zero, TilePivot.GetTile(tilesSet, Families.Circle, number: 1));
        AddKan(round, PlayerIndices.One, TilePivot.GetTile(tilesSet, Families.Circle, number: 9));
        AddKan(round, PlayerIndices.Two, TilePivot.GetTile(tilesSet, Families.Bamboo, number: 1));

        Assert.False(round.IsSuukaikan);
    }

    [Fact]
    public void EndOfRound_Suukaikan_IsAbortiveDrawWithNoTenpaiPaymentAndDealerRepeats()
    {
        var round = NewRound();
        var tilesSet = TilePivot.GetCompleteSet(false);

        AddKan(round, PlayerIndices.Zero, TilePivot.GetTile(tilesSet, Families.Circle, number: 1));
        AddKan(round, PlayerIndices.One, TilePivot.GetTile(tilesSet, Families.Circle, number: 9));
        AddKan(round, PlayerIndices.Two, TilePivot.GetTile(tilesSet, Families.Bamboo, number: 1));
        AddKan(round, PlayerIndices.Three, TilePivot.GetTile(tilesSet, Families.Bamboo, number: 9));

        var info = round.EndOfRound(null);

        Assert.True(info.Ryuukyoku);
        Assert.False(info.ToNextEast);
        Assert.Empty(info.PlayersInfo);
    }

    [Fact]
    public void RunAutoPlay_Suukaikan_EndsTheRoundAsAbortiveDraw()
    {
        var round = new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(1)).Round;
        var tilesSet = TilePivot.GetCompleteSet(false);

        AddKan(round, PlayerIndices.Zero, TilePivot.GetTile(tilesSet, Families.Circle, number: 1));
        AddKan(round, PlayerIndices.One, TilePivot.GetTile(tilesSet, Families.Circle, number: 9));
        AddKan(round, PlayerIndices.Two, TilePivot.GetTile(tilesSet, Families.Bamboo, number: 1));
        AddKan(round, PlayerIndices.Three, TilePivot.GetTile(tilesSet, Families.Bamboo, number: 9));

        var result = round.RunAutoPlay(new CancellationToken(), false, false, false, false, null, 0);

        Assert.True(result.EndOfRound);
        Assert.Null(result.RonPlayerId);
    }
}
