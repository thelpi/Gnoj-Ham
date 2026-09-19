using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

// Covers the yaku that have no static YakuPivot.Example (context-dependent: first-turn draws, riichi
// state, last-tile, kan compensation...), complementing YakuPivot_Tests.cs which covers every yaku
// that does have one.
public class YakuPivot_ContextDependent_Tests
{
    // A plain, unremarkable complete hand (also happens to be Tanyao - not relevant to what these
    // tests check): 234m 234p 234s 678s 55m. 14 tiles, ready for a self-draw context as-is.
    private static List<TilePivot> BuildStandardHand(IReadOnlyList<TilePivot> tilesSet)
    {
        var hand = new List<TilePivot>
        {
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
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 8),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 5),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 5)
        };

        return hand.OrderBy(t => t).ToList();
    }

    // Same shape as above, but only 13 tiles (missing one of the "234s" sequence) so it can be
    // completed by ron on the 14th tile supplied separately as the win context's LatestTile.
    private static (List<TilePivot> concealed, TilePivot winningTile) BuildStandardHandMinusOne(IReadOnlyList<TilePivot> tilesSet)
    {
        var full = BuildStandardHand(tilesSet);
        var winningTile = full.First(t => t.Family == Families.Bamboo && t.Number == 4);
        full.Remove(winningTile);
        return (full, winningTile);
    }

    private static void AssertYaku(HandPivot hand, WinContextPivot context, YakuPivot expected)
    {
        hand.SetYakus(context);

        Assert.NotNull(hand.Yakus);
        Assert.Contains(expected, hand.Yakus!);
    }

    [Fact]
    public void Riichi_IsDetected()
    {
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand = new HandPivot(BuildStandardHand(tilesSet));
        var context = new WinContextPivot(hand.LatestPick, DrawTypes.Wall, Winds.East, Winds.South, isRiichi: true);

        AssertYaku(hand, context, YakuPivot.Riichi);
    }

    [Fact]
    public void DaburuRiichi_IsDetected()
    {
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand = new HandPivot(BuildStandardHand(tilesSet));
        var context = new WinContextPivot(hand.LatestPick, DrawTypes.Wall, Winds.East, Winds.South, isRiichi: null);

        AssertYaku(hand, context, YakuPivot.DaburuRiichi);
    }

    [Fact]
    public void Ippatsu_IsDetected()
    {
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand = new HandPivot(BuildStandardHand(tilesSet));
        var context = new WinContextPivot(hand.LatestPick, DrawTypes.Wall, Winds.East, Winds.South, isRiichi: true, isIppatsu: true);

        AssertYaku(hand, context, YakuPivot.Ippatsu);
    }

    [Fact]
    public void MenzenTsumo_IsDetected()
    {
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand = new HandPivot(BuildStandardHand(tilesSet));
        var context = new WinContextPivot(hand.LatestPick, DrawTypes.Wall, Winds.East, Winds.South);

        AssertYaku(hand, context, YakuPivot.MenzenTsumo);
    }

    [Fact]
    public void Haitei_IsDetectedOnLastTileOfTheRound()
    {
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand = new HandPivot(BuildStandardHand(tilesSet));
        var context = new WinContextPivot(hand.LatestPick, DrawTypes.Wall, Winds.East, Winds.South, isFirstOrLast: null);

        AssertYaku(hand, context, YakuPivot.Haitei);
    }

    [Fact]
    public void RinshanKaihou_IsDetectedOnKanCompensation()
    {
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand = new HandPivot(BuildStandardHand(tilesSet));
        var context = new WinContextPivot(hand.LatestPick, DrawTypes.Compensation, Winds.East, Winds.South);

        AssertYaku(hand, context, YakuPivot.RinshanKaihou);
    }

    [Fact]
    public void Chankan_IsDetectedWhenStealingAnOpenKanCompensation()
    {
        var tilesSet = TilePivot.GetCompleteSet(false);
        var (concealed, winningTile) = BuildStandardHandMinusOne(tilesSet);
        var hand = new HandPivot(concealed);
        var context = new WinContextPivot(winningTile, DrawTypes.OpponentKanCallOpen, Winds.East, Winds.South);

        AssertYaku(hand, context, YakuPivot.Chankan);
    }

    [Fact]
    public void Tenhou_IsDetectedForDealerFirstTurnSelfDraw()
    {
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand = new HandPivot(BuildStandardHand(tilesSet));
        var context = new WinContextPivot(hand.LatestPick, DrawTypes.Wall, Winds.East, Winds.East, isFirstOrLast: true);

        AssertYaku(hand, context, YakuPivot.Tenhou);
    }

    [Fact]
    public void Chiihou_IsDetectedForNonDealerFirstTurnSelfDraw()
    {
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand = new HandPivot(BuildStandardHand(tilesSet));
        var context = new WinContextPivot(hand.LatestPick, DrawTypes.Wall, Winds.East, Winds.South, isFirstOrLast: true);

        AssertYaku(hand, context, YakuPivot.Chiihou);
    }

    [Fact]
    public void Renhou_IsDetectedForFirstTurnRon()
    {
        var tilesSet = TilePivot.GetCompleteSet(false);
        var (concealed, winningTile) = BuildStandardHandMinusOne(tilesSet);
        var hand = new HandPivot(concealed);
        var context = new WinContextPivot(winningTile, DrawTypes.OpponentDiscard, Winds.East, Winds.South, isFirstOrLast: true);

        AssertYaku(hand, context, YakuPivot.Renhou);
    }

    [Fact]
    public void Sanankou_IsDetectedWithThreeConcealedTriplets()
    {
        // Circle 1 x3, Circle 9 x3, Bamboo 1 x3 (three concealed triplets) + Caracter 234 (sequence)
        // + Caracter 7 pair. Self-draw so every triplet counts regardless of which tile is "latest".
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand14 = new List<TilePivot>
        {
            TilePivot.GetTile(tilesSet, Families.Circle, number: 1),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 1),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 1),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 9),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 9),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 9),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 1),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 1),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 1),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 2),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 3),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 4),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 7),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 7)
        }.OrderBy(t => t).ToList();

        var hand = new HandPivot(hand14);
        var context = new WinContextPivot(hand.LatestPick, DrawTypes.Wall, Winds.East, Winds.South);

        AssertYaku(hand, context, YakuPivot.Sanankou);
    }

    [Fact]
    public void Sankantsu_IsDetectedWithThreeDeclaredKans()
    {
        // Three concealed kans of ordinary number tiles (deliberately not dragons/winds, which would
        // also read as Daisangen/Shousuushii/Daisuushii - a bigger yakuman always wins over a regular
        // yaku, so the test would then fail to see Sankantsu at all) + Bamboo 234 (sequence) + Caracter
        // 9 pair.
        var tilesSet = TilePivot.GetCompleteSet(false);
        var circle1s = tilesSet.Where(t => t.Family == Families.Circle && t.Number == 1).ToList();
        var circle9s = tilesSet.Where(t => t.Family == Families.Circle && t.Number == 9).ToList();
        var bamboo1s = tilesSet.Where(t => t.Family == Families.Bamboo && t.Number == 1).ToList();

        var hand17 = new List<TilePivot>();
        hand17.AddRange(circle1s);
        hand17.AddRange(circle9s);
        hand17.AddRange(bamboo1s);
        hand17.Add(TilePivot.GetTile(tilesSet, Families.Bamboo, number: 2));
        hand17.Add(TilePivot.GetTile(tilesSet, Families.Bamboo, number: 3));
        hand17.Add(TilePivot.GetTile(tilesSet, Families.Bamboo, number: 4));
        hand17.Add(TilePivot.GetTile(tilesSet, Families.Caracter, number: 9));
        hand17.Add(TilePivot.GetTile(tilesSet, Families.Caracter, number: 9));

        var hand = new HandPivot(hand17.OrderBy(t => t).ToList());
        hand.DeclareKan(circle1s[0]);
        hand.DeclareKan(circle9s[0]);
        hand.DeclareKan(bamboo1s[0]);

        var context = new WinContextPivot(hand.LatestPick, DrawTypes.Wall, Winds.East, Winds.South);

        AssertYaku(hand, context, YakuPivot.Sankantsu);
    }

    [Fact]
    public void Suukantsu_IsDetectedWithFourDeclaredKans()
    {
        // Four concealed kans (Dragon Red/White/Green + Wind East) + Caracter 9 pair. Yakuman.
        var tilesSet = TilePivot.GetCompleteSet(false);
        var redDragons = tilesSet.Where(t => t.Family == Families.Dragon && t.Dragon == Dragons.Red).ToList();
        var whiteDragons = tilesSet.Where(t => t.Family == Families.Dragon && t.Dragon == Dragons.White).ToList();
        var greenDragons = tilesSet.Where(t => t.Family == Families.Dragon && t.Dragon == Dragons.Green).ToList();
        var eastWinds = tilesSet.Where(t => t.Family == Families.Wind && t.Wind == Winds.East).ToList();

        var hand18 = new List<TilePivot>();
        hand18.AddRange(redDragons);
        hand18.AddRange(whiteDragons);
        hand18.AddRange(greenDragons);
        hand18.AddRange(eastWinds);
        hand18.Add(TilePivot.GetTile(tilesSet, Families.Caracter, number: 9));
        hand18.Add(TilePivot.GetTile(tilesSet, Families.Caracter, number: 9));

        var hand = new HandPivot(hand18.OrderBy(t => t).ToList());
        hand.DeclareKan(redDragons[0]);
        hand.DeclareKan(whiteDragons[0]);
        hand.DeclareKan(greenDragons[0]);
        hand.DeclareKan(eastWinds[0]);

        var context = new WinContextPivot(hand.LatestPick, DrawTypes.Wall, Winds.East, Winds.South);

        AssertYaku(hand, context, YakuPivot.Suukantsu);
    }

    // Optional rule (RulePivot.UseDoubleYakuman): Suuankou tanki, Kokushi musou juusanmen and Chuuren
    // poutou junsei. YakuPivot.GetYakus/HandPivot.SetYakus detect them unconditionally - the ruleset
    // only decides, in ScoreTools.GetFanCount, whether their fan value is honored (26) or capped (13).

    [Fact]
    public void SuuankouTanki_IsDetectedWhenTheWinningTileCompletesThePair()
    {
        // Four concealed triplets (Circle1, Circle9, Bamboo1, Caracter2) + a pair (Bamboo9), self-draw
        // on the second Bamboo9 (the pair): a tanki wait.
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand14 = new List<TilePivot>
        {
            TilePivot.GetTile(tilesSet, Families.Circle, number: 1),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 1),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 1),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 9),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 9),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 9),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 1),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 1),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 1),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 2),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 2),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 2),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 9),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 9)
        };

        var hand = new HandPivot(hand14);
        var context = new WinContextPivot(hand.LatestPick, DrawTypes.Wall, Winds.East, Winds.South);

        AssertYaku(hand, context, YakuPivot.SuuankouTanki);
        Assert.DoesNotContain(YakuPivot.Suuankou, hand.Yakus!);
    }

    [Fact]
    public void Suuankou_IsNotDoubleWhenTheWinningTileCompletesATripletInstead()
    {
        // Same shape as above, but the last (winning) tile completes a triplet instead of the pair.
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand14 = new List<TilePivot>
        {
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 9),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 9),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 9),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 9),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 9),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 1),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 1),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 1),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 2),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 2),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 2),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 1),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 1),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 1)
        };

        var hand = new HandPivot(hand14);
        var context = new WinContextPivot(hand.LatestPick, DrawTypes.Wall, Winds.East, Winds.South);

        AssertYaku(hand, context, YakuPivot.Suuankou);
        Assert.DoesNotContain(YakuPivot.SuuankouTanki, hand.Yakus!);
    }

    [Fact]
    public void KokushiMusouJuusanmen_IsDetectedOnAThirteenSidedWait()
    {
        // All 13 distinct terminal/honour types held, no duplicate yet: winning on the 14th (any of
        // the 13 types) is a genuine 13-sided wait.
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand14 = new List<TilePivot>
        {
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 1),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 9),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 1),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 9),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 1),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 9),
            TilePivot.GetTile(tilesSet, Families.Wind, wind: Winds.East),
            TilePivot.GetTile(tilesSet, Families.Wind, wind: Winds.South),
            TilePivot.GetTile(tilesSet, Families.Wind, wind: Winds.West),
            TilePivot.GetTile(tilesSet, Families.Wind, wind: Winds.North),
            TilePivot.GetTile(tilesSet, Families.Dragon, dragon: Dragons.Red),
            TilePivot.GetTile(tilesSet, Families.Dragon, dragon: Dragons.White),
            TilePivot.GetTile(tilesSet, Families.Dragon, dragon: Dragons.Green),
            TilePivot.GetTile(tilesSet, Families.Dragon, dragon: Dragons.Green)
        };

        var hand = new HandPivot(hand14);
        var context = new WinContextPivot(hand.LatestPick, DrawTypes.Wall, Winds.East, Winds.South);

        AssertYaku(hand, context, YakuPivot.KokushiMusouJuusanmen);
        Assert.DoesNotContain(YakuPivot.KokushiMusou, hand.Yakus!);
    }

    [Fact]
    public void KokushiMusou_IsNotDoubleWhenAPairIsAlreadyFormedBeforeTheWin()
    {
        // A pair (Red dragon) is already formed before the win: a narrow, single-tile wait on the
        // one missing type (Green dragon here).
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand14 = new List<TilePivot>
        {
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 1),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 9),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 1),
            TilePivot.GetTile(tilesSet, Families.Circle, number: 9),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 1),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 9),
            TilePivot.GetTile(tilesSet, Families.Wind, wind: Winds.East),
            TilePivot.GetTile(tilesSet, Families.Wind, wind: Winds.South),
            TilePivot.GetTile(tilesSet, Families.Wind, wind: Winds.West),
            TilePivot.GetTile(tilesSet, Families.Wind, wind: Winds.North),
            TilePivot.GetTile(tilesSet, Families.Dragon, dragon: Dragons.Red),
            TilePivot.GetTile(tilesSet, Families.Dragon, dragon: Dragons.Red),
            TilePivot.GetTile(tilesSet, Families.Dragon, dragon: Dragons.White),
            TilePivot.GetTile(tilesSet, Families.Dragon, dragon: Dragons.Green)
        };

        var hand = new HandPivot(hand14);
        var context = new WinContextPivot(hand.LatestPick, DrawTypes.Wall, Winds.East, Winds.South);

        AssertYaku(hand, context, YakuPivot.KokushiMusou);
        Assert.DoesNotContain(YakuPivot.KokushiMusouJuusanmen, hand.Yakus!);
    }

    [Fact]
    public void ChuurenPoutouJunsei_IsDetectedOnAPureNineSidedWait()
    {
        // The hand, before the winning tile, is exactly 1112345678999: a wait on all 9 values at once.
        var tilesSet = TilePivot.GetCompleteSet(false);
        var hand14 = new List<TilePivot>
        {
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 1),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 1),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 1),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 2),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 3),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 4),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 6),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 7),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 8),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 9),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 9),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 9),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 5),
            TilePivot.GetTile(tilesSet, Families.Caracter, number: 5)
        };

        var hand = new HandPivot(hand14);
        var context = new WinContextPivot(hand.LatestPick, DrawTypes.Wall, Winds.East, Winds.South);

        AssertYaku(hand, context, YakuPivot.ChuurenPoutouJunsei);
        Assert.DoesNotContain(YakuPivot.ChuurenPoutou, hand.Yakus!);
    }
}
