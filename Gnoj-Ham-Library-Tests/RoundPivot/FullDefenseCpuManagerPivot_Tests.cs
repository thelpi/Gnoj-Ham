using System.Reflection;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class FullDefenseCpuManagerPivot_Tests
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

    // RoundPivot.CanDiscard requires this True (a tile was just picked, waiting to be discarded);
    // RoundPivot.CanCallPon/CanCallKan require it False (nothing pending) - each test sets whichever
    // its own scenario needs.
    private static void SetWaitForDiscard(RoundPivot round, bool value)
    {
        typeof(RoundPivot)
            .GetField("_waitForDiscard", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(round, value);
    }

    // Marks a player as riichi (the "dangerous opponent" signal both BasicCpuManagerPivot and
    // FullDefenseCpuManagerPivot react to) without playing through the actual riichi call mechanics.
    private static void SetRiichi(RoundPivot round, PlayerIndices playerIndex)
    {
        var riichis = (List<RiichiPivot?>)typeof(RoundPivot)
            .GetField("_riichis", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(round)!;
        var ranks = Enum.GetValues<PlayerIndices>().Where(p => p != playerIndex).ToDictionary(p => p, _ => 0);
        riichis[(int)playerIndex] = new RiichiPivot(0, false, TilePivot.GetCompleteSet(false)[0], ranks);
    }

    private static void AddToDiscard(RoundPivot round, PlayerIndices playerIndex, TilePivot tile)
    {
        var discardHistory = typeof(RoundPivot)
            .GetField("_discardHistory", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(round)!;
        var discards = (List<List<TilePivot>>)typeof(DiscardHistoryPivot)
            .GetField("_discards", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(discardHistory)!;
        discards[(int)playerIndex].Add(tile);
    }

    // Ground truth for "the single safest discardable tile", straight from the same private method
    // BasicCpuManagerPivot.DiscardDecisionInternal itself relies on - avoids having to hand-predict the
    // outcome of the safety scoring (which also factors in incidental things like this round's dora).
    private static TilePivot ComputeSafestTile(RoundPivot round, List<TilePivot> discardableTiles)
    {
        var manager = new BasicCpuManagerPivot(round);
        var deadTiles = round.DeadTilesFromIndexPointOfView(round.CurrentPlayerIndex);
        var method = typeof(BasicCpuManagerPivot).GetMethod("ComputeTilesSafety", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var result = method.Invoke(manager, new object[] { discardableTiles, deadTiles })!;
        var bestToWorst = (System.Collections.IEnumerable)result.GetType().GetField("Item1")!.GetValue(result)!;
        foreach (var entry in bestToWorst)
        {
            return (TilePivot)entry!.GetType().GetField("Item1")!.GetValue(entry)!;
        }
        throw new InvalidOperationException("No discardable tile.");
    }

    [Fact]
    public void FullDefense_BreaksTenpaiForASafeTile_WhereBasicWouldStayTenpai()
    {
        var round = NewRound(1);
        var tilesSet = TilePivot.GetCompleteSet(false);

        var currentPlayer = round.CurrentPlayerIndex;
        var dangerousOpponent = currentPlayer.RelativePlayerIndex(1);

        // Tenpai shape (123m 456m 789m 11p 34s, waiting 2s/5s) plus one extra junk tile (East): the
        // only discard that keeps tenpai is East. A copy of 1p sits in the dangerous opponent's
        // discard pile (guaranteed-safe), but discarding it breaks tenpai (down to two disconnected
        // singles: 1p and East).
        var hand = new List<TilePivot>
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
            TilePivot.GetTile(tilesSet, Families.Wind, wind: Winds.East),
        };
        var safeOnePin = TilePivot.GetTile(tilesSet, Families.Circle, number: 1);

        SetHand(round, currentPlayer, hand);
        SetWaitForDiscard(round, true);
        SetRiichi(round, dangerousOpponent);
        AddToDiscard(round, dangerousOpponent, safeOnePin);

        Assert.True(round.IsRiichi(dangerousOpponent));

        var tenpaiChoices = round.ExtractDiscardChoicesFromTenpai(currentPlayer);
        Assert.Contains(tenpaiChoices, t => t.Family == Families.Wind && t.Wind == Winds.East);
        Assert.DoesNotContain(tenpaiChoices, t => t.Family == Families.Circle && t.Number == 1);

        var discardableTiles = hand.Distinct().ToList();
        var safestTile = ComputeSafestTile(round, discardableTiles);
        // The scenario is only meaningful if the globally safest tile actually sits outside the
        // tenpai-preserving choices - otherwise Basic and FullDefense would coincidentally agree.
        Assert.DoesNotContain(safestTile, tenpaiChoices);

        var basicChoice = new BasicCpuManagerPivot(round).DiscardDecision();
        var fullDefenseChoice = new FullDefenseCpuManagerPivot(round).DiscardDecision();

        Assert.Contains(basicChoice, tenpaiChoices);
        Assert.Equal(safestTile, fullDefenseChoice);
        Assert.DoesNotContain(fullDefenseChoice, tenpaiChoices);
    }

    [Fact]
    public void FullDefense_DeclinesPon_WhenAnOpponentIsDangerous()
    {
        var round = NewRound(2);
        var tilesSet = TilePivot.GetCompleteSet(false);

        var currentPlayer = round.CurrentPlayerIndex;
        // The discarder must be PreviousPlayerIndex for CanCallPon to even consider the call - making
        // that same player the dangerous (riichi) one matches a common real scenario (ponning a riichi
        // player's own discard) and keeps the test setup simple.
        var dangerousOpponent = round.PreviousPlayerIndex;

        // A hand with a pair of the discarded tile's kind plus a valuable-wind angle: BasicCpuManagerPivot
        // would normally pon this, but FullDefenseCpuManagerPivot must decline outright once any
        // opponent looks dangerous.
        var whiteDragon = TilePivot.GetTile(tilesSet, Families.Dragon, dragon: Dragons.White);
        var hand = new List<TilePivot>
        {
            whiteDragon,
            whiteDragon,
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 2),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 3),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 4),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 2),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 3),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 4),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 2),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 3),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 4),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 6),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 7),
        };

        SetHand(round, currentPlayer, hand);
        SetRiichi(round, dangerousOpponent);
        AddToDiscard(round, dangerousOpponent, whiteDragon);

        Assert.True(round.CanCallPon(currentPlayer));
        Assert.False(new FullDefenseCpuManagerPivot(round).PonDecision(currentPlayer));
    }
}
