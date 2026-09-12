using System.Reflection;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

// "Sekinin barai" (liability payment): the player who feeds the third melded dragon pon/kan (Daisangen)
// or the fourth melded wind pon/kan (Daisuushii) to an opponent becomes liable for that yakuman if it's
// completed later - without losing anything at the moment they feed it. On ron, they share the payment
// equally with the actual discarder; only the discarder pays honba.
public class SekininBarai_Tests
{
    private static RoundPivot NewRound()
        => new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(1)).Round;

    private static void SetHonbaCount(RoundPivot round, int honbaCount)
    {
        typeof(GamePivot).GetProperty("HonbaCount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(round.Game, honbaCount);
    }

    private static void AddDiscard(RoundPivot round, PlayerIndices playerIndex, TilePivot tile)
    {
        var discards = (List<List<TilePivot>>)typeof(RoundPivot)
            .GetField("_discards", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(round)!;
        discards[(int)playerIndex].Add(tile);
    }

    // Builds a Daisangen hand for "winnerIndex": three dragon pons (Red, White, Green - Green called
    // last) plus Bamboo 2-3-4 plus a Caracter 9 pair, winning on the second Caracter 9 (by ron, or by
    // self-draw if "isTsumo"). The last (Green) pon is stolen from "liableWind": the sekinin barai
    // trigger.
    private static void SetDaisangenHand(RoundPivot round, PlayerIndices winnerIndex, Winds liableWind, bool isTsumo = false)
    {
        var tilesSet = TilePivot.GetCompleteSet(false);
        var redDragons = tilesSet.Where(t => t.Family == Families.Dragon && t.Dragon == Dragons.Red).ToList();
        var whiteDragons = tilesSet.Where(t => t.Family == Families.Dragon && t.Dragon == Dragons.White).ToList();
        var greenDragons = tilesSet.Where(t => t.Family == Families.Dragon && t.Dragon == Dragons.Green).ToList();
        var caracter9s = tilesSet.Where(t => t.Family == Families.Caracter && t.Number == 9).ToList();

        var initial = new List<TilePivot>
        {
            redDragons[0], redDragons[1],
            whiteDragons[0], whiteDragons[1],
            greenDragons[0], greenDragons[1],
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 2),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 3),
            TilePivot.GetTile(tilesSet, Families.Bamboo, number: 4),
            caracter9s[0]
        };

        var winningTile = caracter9s[1];
        if (isTsumo)
        {
            // Self-draw: the winning tile is part of the concealed hand from the start (must stay
            // last in this pre-sort list, since HandPivot.LatestPick is set from it before sorting).
            initial.Add(winningTile);
        }

        var hand = new HandPivot(isTsumo ? initial : initial.OrderBy(t => t).ToList());
        hand.DeclarePon(redDragons[2], Winds.North);
        hand.DeclarePon(whiteDragons[2], Winds.North);
        hand.DeclarePon(greenDragons[2], liableWind);

        var context = new WinContextPivot(
            latestTile: isTsumo ? hand.LatestPick : winningTile,
            drawType: isTsumo ? DrawTypes.Wall : DrawTypes.OpponentDiscard,
            dominantWind: round.Game.DominantWind,
            playerWind: round.Game.GetPlayerCurrentWind(winnerIndex));
        hand.SetYakus(context);

        Assert.True(hand.IsComplete);
        Assert.Contains(YakuPivot.Daisangen, hand.Yakus!);

        var hands = (List<HandPivot>)typeof(RoundPivot)
            .GetField("_hands", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(round)!;
        hands[(int)winnerIndex] = hand;
    }

    [Fact]
    public void EndOfRound_DaisangenRonWithLiablePlayer_DiscarderAndLiablePlayerEachPayHalf()
    {
        // Discarder Zero, winner One (Daisangen), liable player Two (fed the third dragon pon). No
        // honba, to isolate the 50/50 split from the honba question below.
        var round = NewRound();
        var discarder = PlayerIndices.Zero;
        var winner = PlayerIndices.One;
        var liable = PlayerIndices.Two;

        SetHonbaCount(round, 0);
        SetDaisangenHand(round, winner, round.Game.GetPlayerCurrentWind(liable));
        AddDiscard(round, discarder, TilePivot.GetCompleteSet(false)[0]);

        var info = round.EndOfRound(discarder);

        var winnerInfo = info.PlayersInfo.Single(pi => pi.Index == winner);
        var discarderInfo = info.PlayersInfo.Single(pi => pi.Index == discarder);
        var liableInfo = info.PlayersInfo.Single(pi => pi.Index == liable);

        var handValue = winnerInfo.PointsGain; // no riichi/honba here, so this is the full ron value
        Assert.True(handValue > 0);

        Assert.Equal(-handValue / 2, discarderInfo.PointsGain);
        Assert.Equal(-handValue / 2, liableInfo.PointsGain);

        // Zero-sum: nothing created or lost.
        Assert.Equal(0, info.PlayersInfo.Sum(pi => pi.PointsGain));
    }

    [Fact]
    public void EndOfRound_DaisangenRonWithLiablePlayer_OnlyDiscarderPaysHonba()
    {
        // Same as above, but with honba on the table: the rule is explicit that only the discarder
        // pays for counters - the liable player's share must stay untouched by honba.
        var round = NewRound();
        var discarder = PlayerIndices.Zero;
        var winner = PlayerIndices.One;
        var liable = PlayerIndices.Two;

        SetHonbaCount(round, 2); // HonbaCountBeforeScoring = 1 => 300 points
        SetDaisangenHand(round, winner, round.Game.GetPlayerCurrentWind(liable));
        AddDiscard(round, discarder, TilePivot.GetCompleteSet(false)[0]);

        var info = round.EndOfRound(discarder);

        var winnerInfo = info.PlayersInfo.Single(pi => pi.Index == winner);
        var discarderInfo = info.PlayersInfo.Single(pi => pi.Index == discarder);
        var liableInfo = info.PlayersInfo.Single(pi => pi.Index == liable);

        var handValue = winnerInfo.PointsGain - 300;
        Assert.True(handValue > 0);

        Assert.Equal(-handValue / 2, liableInfo.PointsGain);
        Assert.Equal(-handValue / 2 - 300, discarderInfo.PointsGain);

        Assert.Equal(0, info.PlayersInfo.Sum(pi => pi.PointsGain));
    }

    [Fact]
    public void EndOfRound_DaisangenRonWhereTheLiablePlayerIsAlsoTheDiscarder_NoSplitFullPaymentByDiscarder()
    {
        // "No consequence as ron player and liable player are the same": the discarder who also fed
        // the third dragon pon simply pays the full hand value, like an ordinary ron with no liability
        // involved at all.
        var round = NewRound();
        var discarder = PlayerIndices.Zero;
        var winner = PlayerIndices.One;

        SetHonbaCount(round, 0);
        SetDaisangenHand(round, winner, round.Game.GetPlayerCurrentWind(discarder));
        AddDiscard(round, discarder, TilePivot.GetCompleteSet(false)[0]);

        var info = round.EndOfRound(discarder);

        var winnerInfo = info.PlayersInfo.Single(pi => pi.Index == winner);
        var discarderInfo = info.PlayersInfo.Single(pi => pi.Index == discarder);

        Assert.Equal(2, info.PlayersInfo.Count);
        Assert.Equal(-winnerInfo.PointsGain, discarderInfo.PointsGain);
    }

    [Fact]
    public void EndOfRound_DaisangenTsumo_TransformsIntoRonOnTheLiablePlayer()
    {
        // Self-draw win: the liability rule transforms it into a ron on the liable player, who then
        // pays the entire hand value (and the entire honba) alone - the other two players pay nothing.
        var round = NewRound();
        var winner = PlayerIndices.One;
        var liable = PlayerIndices.Two;
        var untouched = PlayerIndices.Three;

        SetHonbaCount(round, 2); // HonbaCountBeforeScoring = 1 => 300 points
        SetDaisangenHand(round, winner, round.Game.GetPlayerCurrentWind(liable), isTsumo: true);

        var info = round.EndOfRound(null);

        var winnerInfo = info.PlayersInfo.Single(pi => pi.Index == winner);
        var liableInfo = info.PlayersInfo.Single(pi => pi.Index == liable);

        Assert.Equal(2, info.PlayersInfo.Count);
        Assert.DoesNotContain(info.PlayersInfo, pi => pi.Index == untouched);
        Assert.Equal(-winnerInfo.PointsGain, liableInfo.PointsGain);
    }
}
