using System.Reflection;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class DoubleRon_Tests
{
    private static RoundPivot NewRound()
        => new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(1)).Round;

    // Builds a winning hand for a Ron context out of a yaku's Example (a 14-tile shape whose last
    // tile is the winning one): the example's other 13 tiles become the concealed hand, and the
    // winning tile is supplied separately via the context - SetYakus adds it back internally for any
    // non-self-draw DrawType. Passing all 14 tiles as concealed AND the winning tile via the context
    // would double-count it (15 tiles) and silently fail to complete.
    private static void SetWinningHand(RoundPivot round, PlayerIndices playerIndex, string yakuName)
    {
        var yaku = YakuPivot.Yakus.First(y => y.Name == yakuName);
        var example = yaku.Example!;
        var winningTile = example[^1];

        var hand = new HandPivot(example.Take(example.Count - 1).ToList());
        var context = new WinContextPivot(
            latestTile: winningTile,
            drawType: DrawTypes.OpponentDiscard,
            dominantWind: round.Game.DominantWind,
            playerWind: round.Game.GetPlayerCurrentWind(playerIndex));
        hand.SetYakus(context);

        var hands = (List<HandPivot>)typeof(RoundPivot)
            .GetField("_hands", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(round)!;
        hands[(int)playerIndex] = hand;
    }

    private static void AddDiscard(RoundPivot round, PlayerIndices playerIndex, TilePivot tile)
    {
        var discardHistory = typeof(RoundPivot)
            .GetField("_discardHistory", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(round)!;
        var discards = (List<List<TilePivot>>)typeof(DiscardHistoryPivot)
            .GetField("_discards", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(discardHistory)!;
        discards[(int)playerIndex].Add(tile);
    }

    [Fact]
    public void EndOfRound_TwoSimultaneousRonWinners_OnlyClosestToDiscarderCollectsTheRiichiSticksAndHonba()
    {
        // Discarder is Zero. Winners are One (relative +1 from Zero: the closest) and Two (relative
        // +2: farther). Two pending riichi sticks (2000 points) and one honba (300 points) are on the
        // table: only the closest winner (One) should collect both - the total credited across every
        // player must exactly balance out, not silently vanish (the bug this guards: the previous
        // "who's closest" check zeroed the bonus for every winner whenever there was more than one).
        var round = NewRound();
        SetWinningHand(round, PlayerIndices.One, "Tanyao");
        SetWinningHand(round, PlayerIndices.Two, "Iipeikou");

        round.Game.AddPendingRiichi(PlayerIndices.Three);
        round.Game.AddPendingRiichi(PlayerIndices.Zero);

        typeof(GamePivot).GetProperty("HonbaCount", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(round.Game, 2);

        AddDiscard(round, PlayerIndices.Zero, TilePivot.GetCompleteSet(false)[0]);

        var info = round.EndOfRound(PlayerIndices.Zero);

        var closestWinnerInfo = info.PlayersInfo.Single(pi => pi.Index == PlayerIndices.One);
        var fartherWinnerInfo = info.PlayersInfo.Single(pi => pi.Index == PlayerIndices.Two);

        // The farther winner gets exactly their hand value: no riichi sticks, no honba.
        Assert.Equal(fartherWinnerInfo.HandPointsGain, fartherWinnerInfo.PointsGain);

        // The closest winner gets their hand value plus both bonuses (2000 riichi + 300 honba).
        Assert.Equal(closestWinnerInfo.HandPointsGain + 2000 + 300, closestWinnerInfo.PointsGain);
    }

}
