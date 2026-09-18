using System.Reflection;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class NumberKabe_Tests
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

    // Marks a player as riichi (the "dangerous opponent" signal ComputeTilesSafety reacts to) without
    // playing through the actual riichi call mechanics - same helper as FullDefenseCpuManagerPivot_Tests.
    private static void SetRiichi(RoundPivot round, PlayerIndices playerIndex)
    {
        var riichis = (List<RiichiPivot?>)typeof(RoundPivot)
            .GetField("_riichis", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(round)!;
        var ranks = Enum.GetValues<PlayerIndices>().Where(p => p != playerIndex).ToDictionary(p => p, _ => 0);
        riichis[(int)playerIndex] = new RiichiPivot(0, false, TilePivot.GetCompleteSet(false)[0], ranks);
    }

    // Makes every copy of the given tile kind dead from the current player's point of view: removes
    // it from everywhere it could still be concealed (the wall, the reserves, the unrevealed dora
    // indicators, every opponent's hand). What DeadTilesFromIndexPointOfView reports is "everything
    // not concealed", so this - unlike merely appending to the discard log - is what actually makes
    // a tile dead, whatever this seed's real deal happened to do with its 4 copies.
    private static void MakeEveryCopyDead(RoundPivot round, PlayerIndices currentPlayer, TilePivot tile)
    {
        foreach (var fieldName in new[] { "_wallTiles", "_compensationTiles", "_deadTreasureTiles", "_uraDoraIndicatorTiles", "_doraIndicatorTiles" })
        {
            var list = (List<TilePivot>)typeof(RoundPivot)
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(round)!;
            list.RemoveAll(t => t == tile);
        }

        foreach (var opponent in Enum.GetValues<PlayerIndices>().Where(p => p != currentPlayer))
        {
            SetHand(round, opponent, round.GetHand(opponent).ConcealedTiles.Where(t => t != tile).ToList());
        }
    }

    // Ground truth for a specific tile's safety score, straight from the same private method
    // BasicCpuManagerPivot.DiscardDecisionInternal itself relies on.
    private static int ComputeSafetyScore(RoundPivot round, BasicCpuManagerPivot manager, List<TilePivot> discardableTiles, TilePivot target)
    {
        var deadTiles = round.DeadTilesFromIndexPointOfView(round.CurrentPlayerIndex);
        var method = typeof(BasicCpuManagerPivot).GetMethod("ComputeTilesSafety", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = method.Invoke(manager, new object[] { discardableTiles, deadTiles })!;
        var bestToWorst = (System.Collections.IEnumerable)result.GetType().GetField("Item1")!.GetValue(result)!;
        foreach (var entry in bestToWorst)
        {
            var tile = (TilePivot)entry!.GetType().GetField("Item1")!.GetValue(entry)!;
            if (tile == target)
            {
                return (int)entry.GetType().GetField("Item2")!.GetValue(entry)!;
            }
        }
        throw new InvalidOperationException("Target tile not found among discardable tiles.");
    }

    // 5p5p5p (triplet, 3 copies) held by the current player, with no 5p left concealed anywhere else
    // (see MakeEveryCopyDead): nobody - dangerous opponent included - can be holding or drawing one.
    // Filled out to 14 tiles with an unrelated run and pair; shape doesn't matter here since
    // ComputeTilesSafety is exercised directly, not DiscardDecision.
    private static List<TilePivot> BuildHandWithFullyDeadFivePin(IReadOnlyList<TilePivot> tilesSet) => new()
    {
        TilePivot.GetTile(tilesSet, Families.Circle, number: 5),
        TilePivot.GetTile(tilesSet, Families.Circle, number: 5),
        TilePivot.GetTile(tilesSet, Families.Circle, number: 5),
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 1),
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 2),
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 3),
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 4),
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 5),
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 6),
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 7),
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 8),
        TilePivot.GetTile(tilesSet, Families.Caracter, number: 9),
        TilePivot.GetTile(tilesSet, Families.Bamboo, number: 2),
        TilePivot.GetTile(tilesSet, Families.Bamboo, number: 3),
    };

    // Policy (same as furiten avoidance and the wait-width tie-break): a behavior added to the base
    // class is inherited by every variant automatically, unless that variant deliberately opts out
    // (BasicNoNumberKabeCpuManagerPivot, for A/B measurement only).
    // NoDefenseCpuManagerPivot isn't in this list on purpose: it never treats any opponent as
    // dangerous, so the safety scoring this test inspects is inert for it whatever the kabe flag says
    // (see NoDefenseCpuManagerPivot_DoesNotOptOutOfNumberKabe below for its own check).
    [Theory]
    [InlineData(typeof(BasicCpuManagerPivot))]
    [InlineData(typeof(FullDefenseCpuManagerPivot))]
    public void KabeAwareVariants_TreatAFullyDeadNumberedTileAsSafe(Type cpuManagerType)
    {
        var round = NewRound(1);
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand = BuildHandWithFullyDeadFivePin(tilesSet);
        var fivePin = TilePivot.GetTile(tilesSet, Families.Circle, number: 5);

        var currentPlayer = round.CurrentPlayerIndex;
        var dangerousOpponent = currentPlayer.RelativePlayerIndex(1);

        SetHand(round, currentPlayer, hand);
        SetWaitForDiscard(round, true);
        SetRiichi(round, dangerousOpponent);
        // Every one of the 4 copies is accounted for (3 in our own hand, the 4th nowhere concealed),
        // while the dangerous opponent's own discard pile stays empty - so this isn't just plain
        // genbutsu, only kabe can make the tile safe here.
        MakeEveryCopyDead(round, currentPlayer, fivePin);

        Assert.Equal(4, round.DeadTilesFromIndexPointOfView(currentPlayer).Count(t => t == fivePin));

        var discardableTiles = hand.Distinct().ToList();
        var kabeAwareManager = (Gnoj_Ham_Library.BasicCpuManagerPivot)Activator.CreateInstance(cpuManagerType, round)!;
        var kabeBlindManager = new BasicNoNumberKabeCpuManagerPivot(round);

        var kabeAwareScore = ComputeSafetyScore(round, kabeAwareManager, discardableTiles, fivePin);
        var kabeBlindScore = ComputeSafetyScore(round, kabeBlindManager, discardableTiles, fivePin);

        // Against the dangerous opponent alone, kabe brings the tile from Unsafe (4) down to Safe (0)
        // - a difference of exactly 4; the two other, non-dangerous opponents score identically in
        // both cases, so the same gap survives in the aggregated totals.
        Assert.Equal(4, kabeBlindScore - kabeAwareScore);
    }

    [Fact]
    public void NoDefenseCpuManagerPivot_DoesNotOptOutOfNumberKabe()
    {
        var flag = typeof(BasicCpuManagerPivot)
            .GetProperty("ReadsKabeForNumbers", BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.True((bool)flag.GetValue(new NoDefenseCpuManagerPivot(NewRound(1)))!);
        Assert.False((bool)flag.GetValue(new BasicNoNumberKabeCpuManagerPivot(NewRound(1)))!);
    }
}
