using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

public class GlobalExtensions_Tests
{
    [Theory]
    [InlineData(Winds.East, Winds.South)]
    [InlineData(Winds.South, Winds.West)]
    [InlineData(Winds.West, Winds.North)]
    [InlineData(Winds.North, Winds.East)]
    public void Right_IsTheNextWindInTheTurnOrder(Winds wind, Winds expected)
    {
        Assert.Equal(expected, wind.Right());
    }

    [Theory]
    [InlineData(Winds.East, Winds.North)]
    [InlineData(Winds.South, Winds.East)]
    [InlineData(Winds.West, Winds.South)]
    [InlineData(Winds.North, Winds.West)]
    public void Left_IsThePreviousWindInTheTurnOrder(Winds wind, Winds expected)
    {
        Assert.Equal(expected, wind.Left());
    }

    [Theory]
    [InlineData(Winds.East, Winds.West)]
    [InlineData(Winds.South, Winds.North)]
    [InlineData(Winds.West, Winds.East)]
    [InlineData(Winds.North, Winds.South)]
    public void Opposite_IsTheWindTwoSeatsAway(Winds wind, Winds expected)
    {
        Assert.Equal(expected, wind.Opposite());
    }

    [Fact]
    public void ThereAreAsManyWindsAsPlayers()
    {
        // The winds turn around the table the way the players do, sharing the same count.
        Assert.Equal(GamePivot.PlayersCount, Enum.GetValues<Winds>().Length);
        Assert.Equal(Enum.GetValues<PlayerIndices>().Length, GamePivot.PlayersCount);
    }

    [Fact]
    public void WindFrom_GivesEastToTheEastPlayerThenFollowsTheTurnOrder()
    {
        var turnOrder = new[] { Winds.East, Winds.South, Winds.West, Winds.North };

        foreach (var east in Enum.GetValues<PlayerIndices>())
        {
            for (var steps = 0; steps < turnOrder.Length; steps++)
            {
                Assert.Equal(turnOrder[steps], east.RelativePlayerIndex(steps).WindFrom(east));
            }
        }
    }

    [Fact]
    public void RelativePlayerIndex_MovesAroundTheTableFromAnyPlayerByAnyNumberOfSeats()
    {
        // Walks the seats one at a time, so the expectation does not rest on the arithmetic under test.
        var next = new Dictionary<PlayerIndices, PlayerIndices>
        {
            [PlayerIndices.Zero] = PlayerIndices.One,
            [PlayerIndices.One] = PlayerIndices.Two,
            [PlayerIndices.Two] = PlayerIndices.Three,
            [PlayerIndices.Three] = PlayerIndices.Zero
        };
        var previous = next.ToDictionary(kv => kv.Value, kv => kv.Key);

        foreach (var start in Enum.GetValues<PlayerIndices>())
        {
            foreach (var steps in Enumerable.Range(-13, 27))
            {
                var expected = start;
                for (var i = 0; i < Math.Abs(steps); i++)
                {
                    expected = steps > 0 ? next[expected] : previous[expected];
                }

                Assert.Equal(expected, start.RelativePlayerIndex(steps));
            }
        }
    }

    [Fact]
    public void IsSelfDraw_IsTrueOnlyForTilesTheWinnerDrewThemselves()
    {
        var selfDraws = Enum.GetValues<DrawTypes>().Where(d => d.IsSelfDraw()).ToList();

        Assert.Equal(new[] { DrawTypes.Wall, DrawTypes.Compensation }, selfDraws);
    }

    [Theory]
    [InlineData(EndOfGameRules.Oorasu, false, false)]
    [InlineData(EndOfGameRules.Tobi, true, false)]
    [InlineData(EndOfGameRules.Enchousen, false, true)]
    [InlineData(EndOfGameRules.EnchousenAndTobi, true, true)]
    public void EndOfGameRules_SayWhichOfTobiAndEnchousenTheyApply(EndOfGameRules rule, bool tobi, bool enchousen)
    {
        Assert.Equal(tobi, rule.TobiRuleApply());
        Assert.Equal(enchousen, rule.EnchousenRuleApply());
    }

    [Theory]
    [InlineData(InitialPointsRules.K25, 25000)]
    [InlineData(InitialPointsRules.K30, 30000)]
    public void GetInitialPointsFromRule_ReadsThePointsOfTheRule(InitialPointsRules rule, int points)
    {
        Assert.Equal(points, rule.GetInitialPointsFromRule());
    }

    [Fact]
    public void GetInitialPointsFromRule_ForARuleNotImplemented_Throws()
    {
        Assert.Throws<NotImplementedException>(() => ((InitialPointsRules)99).GetInitialPointsFromRule());
    }

    [Theory]
    [InlineData(new int[0], 5, new[] { 5 })]
    [InlineData(new[] { 1, 3, 5 }, 7, new[] { 1, 3, 5, 7 })]
    [InlineData(new[] { 1, 3, 5 }, 0, new[] { 0, 1, 3, 5 })]
    [InlineData(new[] { 1, 3, 5 }, 4, new[] { 1, 3, 4, 5 })]
    [InlineData(new[] { 1, 3, 5 }, 3, new[] { 1, 3, 3, 5 })]
    [InlineData(new[] { 1, 3, 5 }, 5, new[] { 1, 3, 5, 5 })]
    [InlineData(new[] { 1, 3, 5 }, 1, new[] { 1, 1, 3, 5 })]
    public void AddSorted_KeepsTheListInAscendingOrder(int[] items, int item, int[] expected)
    {
        var list = items.ToList();

        list.AddSorted(item);

        Assert.Equal(expected, list);
    }

    [Theory]
    [InlineData(new[] { 1, 2, 3 }, new[] { 3, 1, 2 }, true)]
    [InlineData(new[] { 1, 2, 3 }, new[] { 1, 2 }, false)]
    [InlineData(new[] { 1, 2 }, new[] { 1, 2, 3 }, false)]
    [InlineData(new[] { 1, 1, 2 }, new[] { 1, 2 }, true)]
    [InlineData(new int[0], new int[0], true)]
    public void IsBijection_ComparesTheElementsOfBothListsWhateverTheirOrder(int[] first, int[] second, bool expected)
    {
        Assert.Equal(expected, ((IReadOnlyList<int>)first).IsBijection(second));
    }
}
