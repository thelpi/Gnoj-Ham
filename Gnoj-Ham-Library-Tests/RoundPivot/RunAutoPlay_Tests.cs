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
}
