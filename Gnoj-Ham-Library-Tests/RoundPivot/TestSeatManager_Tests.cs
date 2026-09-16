using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class TestSeatManager_Tests
{
    [Fact]
    public void OnlyTheGivenSeatsUseTheirFactory_EveryOtherSeatStaysBasic()
    {
        var factories = new Dictionary<PlayerIndices, Func<RoundPivot, CpuManagerBasePivot>>
        {
            { PlayerIndices.Two, round => new NoDefenseCpuManagerPivot(round) }
        };

        var game = new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(1), factories);

        for (var i = 0; i < 4; i++)
        {
            var manager = game.Round.CpuManager((PlayerIndices)i);
            if ((PlayerIndices)i == PlayerIndices.Two)
            {
                Assert.IsType<NoDefenseCpuManagerPivot>(manager);
            }
            else
            {
                Assert.IsType<BasicCpuManagerPivot>(manager);
            }
        }
    }

    [Fact]
    public void NoCpuManagerFactories_EverySeatStaysBasic()
    {
        var game = new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(1));

        for (var i = 0; i < 4; i++)
        {
            Assert.IsType<BasicCpuManagerPivot>(game.Round.CpuManager((PlayerIndices)i));
        }
    }

    [Fact]
    public void NextRound_KeepsTheSameCpuManagerFactories()
    {
        var factories = new Dictionary<PlayerIndices, Func<RoundPivot, CpuManagerBasePivot>>
        {
            { PlayerIndices.Three, round => new NoDefenseCpuManagerPivot(round) }
        };

        var game = new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(1), factories);

        game.NextRound(null);

        Assert.IsType<NoDefenseCpuManagerPivot>(game.Round.CpuManager(PlayerIndices.Three));
        Assert.IsType<BasicCpuManagerPivot>(game.Round.CpuManager(PlayerIndices.Zero));
    }

    [Fact]
    public void MultipleSeatsCanUseTheSameOrDifferentFactories()
    {
        var factories = new Dictionary<PlayerIndices, Func<RoundPivot, CpuManagerBasePivot>>
        {
            { PlayerIndices.Zero, round => new NoDefenseCpuManagerPivot(round) },
            { PlayerIndices.Two, round => new NoDefenseCpuManagerPivot(round) }
        };

        var game = new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(1), factories);

        Assert.IsType<NoDefenseCpuManagerPivot>(game.Round.CpuManager(PlayerIndices.Zero));
        Assert.IsType<NoDefenseCpuManagerPivot>(game.Round.CpuManager(PlayerIndices.Two));
        Assert.IsType<BasicCpuManagerPivot>(game.Round.CpuManager(PlayerIndices.One));
        Assert.IsType<BasicCpuManagerPivot>(game.Round.CpuManager(PlayerIndices.Three));
    }
}
