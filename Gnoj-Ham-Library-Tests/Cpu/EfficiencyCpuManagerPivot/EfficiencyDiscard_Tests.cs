using System.Reflection;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class EfficiencyDiscard_Tests
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

    private static TilePivot Decide(RoundPivot round, List<TilePivot> hand)
    {
        SetHand(round, round.CurrentPlayerIndex, hand);
        SetWaitForDiscard(round, true);

        return new EfficiencyCpuManagerPivot(round).DiscardDecision();
    }

    [Fact]
    public void DiscardDecision_KeepsTheShapeAndThrowsTheTilesThatBelongToNone()
    {
        // Two melds (234m 567p), two partial groups (45s, 68s), a pair (99p), and two lone honors: the
        // hand is one tile from tenpai, and the only discards that keep it so are the lone honors.
        var hand = HandNotation.Tiles("234m567p45s68s99p1z5z");

        var discard = Decide(NewRound(1), hand);

        Assert.True(discard.IsHonor, $"Discarded {discard.Family} {discard.Number}");
    }

    [Fact]
    public void DiscardDecision_AmongLoneTiles_KeepsTheOnesWithTheMostTilesToDraw()
    {
        // Two melds (123m 456s), a partial group (78p), a pair (99s), and four lone tiles: 9m, 1z, 7z, and
        // 9m, 1z, 7z, and 4p. Any of them can go without changing how far the hand is from tenpai, but a
        // lone honor can only ever become a pair (3 tiles to draw), a lone 9m a pair or a group with 7m
        // or 8m, and a lone 4p a group with 2p 3p 5p 6p, or a pair: an honor is what goes first.
        var hand = HandNotation.Tiles("123m456s78p99s9m1z7z4p");

        var discard = Decide(NewRound(1), hand);

        Assert.True(discard.IsHonor, $"Discarded {discard.Family} {discard.Number}");
    }

    [Fact]
    public void DiscardDecision_LeavesTheBestShantenThenTheMostTilesToDraw_OnRealDeals()
    {
        var checkedCount = 0;

        for (var seed = 1; seed <= 300; seed++)
        {
            var round = NewRound(seed);
            var player = round.CurrentPlayerIndex;

            // The dealt hand, and one more tile from the wall, as a player about to discard has.
            var hand = round.GetHand(player).ConcealedTiles.ToList();
            hand.Add(round.WallTiles[0]);
            hand.Sort();

            var discard = Decide(round, hand);

            // The special cases are none of the business of this discard: tenpai, and the two special hands.
            var pairsCount = hand.GroupBy(t => t).Count(g => g.Count() >= 2);
            var orphanKindsCount = hand.Where(t => t.IsHonorOrTerminal).Distinct().Count();
            if (round.ExtractDiscardChoicesFromTenpai(player).Count > 0 || pairsCount >= 5 || orphanKindsCount >= 11)
            {
                continue;
            }

            Assert.Contains(discard, BestChoices(round, hand));
            checkedCount++;
        }

        Assert.True(checkedCount > 250, $"Only {checkedCount} deals checked");
    }

    // Every discard which leaves the fewest shanten and, among them, the most live tiles to draw.
    private static List<TilePivot> BestChoices(RoundPivot round, List<TilePivot> hand)
    {
        var dead = round.DeadTilesFromIndexPointOfView(round.CurrentPlayerIndex);
        var live = Enumerable.Range(0, TilePivot.KindsCount)
            .Select(kind => TilePivot.CopiesCount - dead.Count(t => t.KindIndex == kind))
            .ToArray();
        var everyKind = TilePivot.GetCompleteSet(false).DistinctBy(t => t.KindIndex).OrderBy(t => t.KindIndex).ToList();

        var options = hand.Distinct().Select(discard =>
        {
            var rest = new List<TilePivot>(hand);
            rest.Remove(discard);
            var shanten = ShantenCalculatorPivot.Compute(rest, 0);

            var ukeire = everyKind
                .Where(drawn => live[drawn.KindIndex] > 0 && ShantenCalculatorPivot.Compute(new List<TilePivot>(rest) { drawn }, 0) < shanten)
                .Sum(drawn => live[drawn.KindIndex]);

            return (discard, shanten, ukeire);
        }).ToList();

        var best = options.OrderBy(o => o.shanten).ThenByDescending(o => o.ukeire).First();
        return options.Where(o => o.shanten == best.shanten && o.ukeire == best.ukeire).Select(o => o.discard).ToList();
    }

    // One seat, or every seat, plays with the efficiency; the others with the basic logic.
    private static Func<RoundPivot, CpuManagerBasePivot> Factory(bool efficient)
    {
        if (efficient)
        {
            return round => new EfficiencyCpuManagerPivot(round);
        }

        return round => new BasicCpuManagerPivot(round);
    }

    [Fact]
    public void WholeGames_PlayedByEfficiencyCpus_GoToTheEnd()
    {
        for (var seed = 1; seed <= 4; seed++)
        {
            // Called hands, riichi, and abortive draws included.
            var everySeat = seed % 2 == 0;
            var factories = Enum.GetValues<PlayerIndices>().ToDictionary(i => i, i => Factory(everySeat || i == PlayerIndices.Zero));
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
    public void TheCatalog_ListsTheEfficiencyCpu()
    {
        Assert.Contains(CpuManagerCatalog.Implementations, o => o.Type == typeof(EfficiencyCpuManagerPivot));
    }
}
