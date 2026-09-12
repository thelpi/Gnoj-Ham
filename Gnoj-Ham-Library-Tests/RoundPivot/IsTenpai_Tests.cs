using System.Reflection;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class IsTenpai_Tests
{
    private static RoundPivot NewRound()
        => new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(1)).Round;

    private static void SetHand(RoundPivot round, PlayerIndices playerIndex, List<TilePivot> concealedTiles)
    {
        var hands = (List<HandPivot>)typeof(RoundPivot)
            .GetField("_hands", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(round)!;
        hands[(int)playerIndex] = new HandPivot(concealedTiles);
    }

    [Fact]
    public void IsTenpai_WaitingOnATileAlreadyHeldFourTimes_IsStillTrue()
    {
        // Tenpai is a pure hand-shape notion, independent of whether the wait can ever physically be
        // completed: 123m + 456p + 789s + a red dragon triplet, plus a lone fourth red dragon (tanki
        // wait on the pair). All four copies of the waiting tile are already in this same hand, so the
        // wait is mathematically dead (0% chance - no fifth copy exists anywhere) - real riichi rules
        // still count this as tenpai (it pays out on an exhaustive draw, and riichi can be declared on
        // it), and the engine doesn't special-case tile availability anywhere in its combinatorics.
        var round = NewRound();
        var tilesSet = TilePivot.GetCompleteSet(false);
        var redDragons = tilesSet.Where(t => t.Family == Families.Dragon && t.Dragon == Dragons.Red).ToList();
        Assert.Equal(4, redDragons.Count);

        var hand = new List<TilePivot>
        {
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 1),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 2),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 3),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 4),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 5),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 6),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 7),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 8),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 9),
            redDragons[0],
            redDragons[1],
            redDragons[2],
            redDragons[3]
        };

        SetHand(round, PlayerIndices.Zero, hand);

        Assert.True(round.IsTenpai(PlayerIndices.Zero, null));
    }

    [Fact]
    public void IsTenpai_NormalTankiWait_IsTrue()
    {
        var round = NewRound();
        var tilesSet = TilePivot.GetCompleteSet(false);
        var whiteDragons = tilesSet.Where(t => t.Family == Families.Dragon && t.Dragon == Dragons.White).ToList();

        var hand = new List<TilePivot>
        {
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 1),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 2),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 3),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 4),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 5),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 6),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 7),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 8),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 9),
            whiteDragons[0],
            whiteDragons[1],
            whiteDragons[2],
            TilePivot.GetTile(tilesSet, Families.Circle, number: 1) // lone tile, tanki wait, only 1 of 4 copies held
        };

        SetHand(round, PlayerIndices.Zero, hand);

        Assert.True(round.IsTenpai(PlayerIndices.Zero, null));
    }
}
