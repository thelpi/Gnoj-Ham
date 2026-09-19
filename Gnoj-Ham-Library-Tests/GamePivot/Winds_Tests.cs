using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class Winds_Tests
{
    private static readonly Winds[] TurnOrder = { Winds.East, Winds.South, Winds.West, Winds.North };

    private static GamePivot NewGame(int seed)
        => new(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(seed));

    [Fact]
    public void InitialWinds_FollowTheTurnOrderFromTheEastPlayer()
    {
        // Every seat of the table gets to be east in one of the games.
        var eastPlayers = new HashSet<PlayerIndices>();
        for (var seed = 1; seed <= 60; seed++)
        {
            var game = NewGame(seed);
            var east = game.FirstEastIndex;
            eastPlayers.Add(east);

            for (var steps = 0; steps < TurnOrder.Length; steps++)
            {
                var player = east.RelativePlayerIndex(steps);
                Assert.Equal(TurnOrder[steps], game.Players[(int)player].CurrentGameInitialWind);
            }
        }

        Assert.Equal(Enum.GetValues<PlayerIndices>().Length, eastPlayers.Count);
    }

    [Fact]
    public void GetPlayerCurrentWind_FollowsTheTurnOrderFromTheCurrentEastPlayer()
    {
        var eastPlayers = new HashSet<PlayerIndices>();
        for (var seed = 1; seed <= 15; seed++)
        {
            var game = NewGame(seed);

            // Plays the rounds one after the other: the east player changes whenever it is not kept.
            var endOfGame = false;
            for (var round = 0; round < 30 && !endOfGame; round++)
            {
                eastPlayers.Add(game.EastIndex);
                for (var steps = 0; steps < TurnOrder.Length; steps++)
                {
                    var player = game.EastIndex.RelativePlayerIndex(steps);
                    Assert.Equal(TurnOrder[steps], game.GetPlayerCurrentWind(player));
                }

                var result = game.Round.RunAutoPlay(new CancellationToken());
                endOfGame = game.NextRound(result.RonPlayerId).EndOfGame;
            }
        }

        Assert.Equal(Enum.GetValues<PlayerIndices>().Length, eastPlayers.Count);
    }
}
