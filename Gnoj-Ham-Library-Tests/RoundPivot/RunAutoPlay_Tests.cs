using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class RunAutoPlay_Tests
{
    [Fact]
    public void RunAutoPlay_HumanKanCompensationOnNoHumanGame_ThrowsClearException()
    {
        // A 4-CPU game (PlayerPivot.BuildPlayers(null)) has no human player at all. Supplying a human
        // kan compensation there is a caller bug - it must fail loudly and clearly, not with a bare
        // NullReferenceException from blindly trusting Game.HumanPlayerIndex to have a value.
        var round = new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(1)).Round;
        var compensationTile = round.FullTilesList[0];

        Assert.Throws<InvalidOperationException>(() =>
            round.RunAutoPlay(new CancellationToken(), false, false, false, false, (compensationTile, (PlayerIndices?)null), 0));
    }

    [Fact]
    public void RunAutoPlay_AfterTheHumanPlayerDeclinedAChii_DoesNotMakeItForThem()
    {
        // The human player's own CPU manager (their advisor) would make the chii: declining it must
        // still be final. The engine used to make it anyway, leaving the human player with a chii they
        // refused and no tile picked.
        var found = false;
        for (var seed = 1; seed <= 300 && !found; seed++)
        {
            var game = new GamePivot("Me", RulePivot.Default, new PlayerStatisticsPivot(), new Random(seed), null);
            var human = PlayerIndices.Zero;
            var declined = false;
            for (var i = 0; i < 400; i++)
            {
                var result = game.Round.RunAutoPlay(default, declined, false, false, false, null, 0);
                if (result.EndOfRound)
                {
                    break;
                }

                if (game.Round.IsHumanPlayer
                    && !game.Round.GetHand(human).IsFullHand
                    && game.Round.CanCallChii().Count > 0
                    && game.Round.Advisor!.ChiiDecision().chiiChoice != null)
                {
                    var combinations = game.Round.GetHand(human).DeclaredCombinations.Count;
                    var chiiCalls = 0;
                    game.Round.CallNotifier += e => chiiCalls += e.Action == CallTypes.Chii && e.PlayerIndex == human ? 1 : 0;

                    game.Round.RunAutoPlay(default, true, false, false, false, null, 0);

                    Assert.Equal(0, chiiCalls);
                    Assert.Equal(combinations, game.Round.GetHand(human).DeclaredCombinations.Count);
                    Assert.True(game.Round.GetHand(human).IsFullHand, "The human player should have picked a tile, and be about to discard.");
                    found = true;
                    break;
                }

                declined = false;
                if (result.HumanCall is null || result.HumanCall.Value.call == CallTypes.NoCall)
                {
                    // The human player takes every pon (which opens their hand), and turns down everything else.
                    if (!game.Round.IsHumanPlayer && game.Round.CanCallPon(human))
                    {
                        game.Round.CallPon(human);
                    }
                }

                if (result.HumanCall is null || result.HumanCall.Value.call == CallTypes.NoCall)
                {
                    var tile = game.Round.GetHand(human).ConcealedTiles.FirstOrDefault(t => game.Round.CanDiscard(t));
                    if (tile != null && game.Round.Discard(tile))
                    {
                        continue;
                    }
                }
                declined = true;
            }
        }

        Assert.True(found, "No situation where the human player's advisor would chii was found in the first seeds.");
    }
}
