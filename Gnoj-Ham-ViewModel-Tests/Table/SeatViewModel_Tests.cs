using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;
using Gnoj_Ham_ViewModel;

namespace Gnoj_Ham_ViewModel_Tests;

// The seats are exercised on the final state of real rounds played by the CPUs from a fixed seed, to
// get genuine discards, combinations and riichi declarations.
public class SeatViewModel_Tests
{
    private static GamePivot PlayFirstRound(int seed)
    {
        var game = new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(seed));
        game.Round.RunAutoPlay(new CancellationToken());
        return game;
    }

    private static GamePivot FindRound(Func<GamePivot, bool> predicate)
    {
        for (var seed = 1; seed < 600; seed++)
        {
            var game = PlayFirstRound(seed);
            if (predicate(game))
            {
                return game;
            }
        }

        throw new InvalidOperationException("No round matching the predicate in the first seeds.");
    }

    private static SeatViewModel NewSeat(GamePivot game, PlayerIndices index)
    {
        var seat = new SeatViewModel(game, index, isHuman: index == PlayerIndices.Zero, revealHand: false);
        seat.RefreshRound();
        return seat;
    }

    private static bool IsReversed(PlayerIndices index) => index == PlayerIndices.One || index == PlayerIndices.Two;

    // The tile view-model showing the discard at a rank, wherever the seat lays it out.
    private static TileViewModel TileForDiscard(SeatViewModel seat, int rank)
    {
        var rowIndex = Math.Min(rank / 6, 2);
        var row = seat.DiscardRows[rowIndex];
        var offset = rank - (rowIndex * 6);
        return row[IsReversed(seat.Index) ? row.Count - 1 - offset : offset];
    }

    [Fact]
    public void RefreshDiscards_ShowsEveryDiscardInItsRow_SixToARowThenTheRest()
    {
        var game = FindRound(g => Enum.GetValues<PlayerIndices>().Any(p => g.Round.GetDiscard(p).Count >= 14));
        var index = Enum.GetValues<PlayerIndices>().First(p => game.Round.GetDiscard(p).Count >= 14);
        var discards = game.Round.GetDiscard(index);
        var seat = NewSeat(game, index);

        Assert.Equal(3, seat.DiscardRows.Count);
        Assert.Equal(6, seat.DiscardRows[0].Count);
        Assert.Equal(6, seat.DiscardRows[1].Count);
        Assert.Equal(discards.Count - 12, seat.DiscardRows[2].Count);
        for (var i = 0; i < discards.Count; i++)
        {
            Assert.Same(discards[i], TileForDiscard(seat, i).Tile);
        }
    }

    [Theory]
    [InlineData(PlayerIndices.One)]
    [InlineData(PlayerIndices.Two)]
    public void RefreshDiscards_BuildsTheRowsOfTheRightAndTopSeatsFromTheOtherEnd(PlayerIndices index)
    {
        var game = FindRound(g => g.Round.GetDiscard(index).Count >= 8);
        var discards = game.Round.GetDiscard(index);
        var seat = NewSeat(game, index);

        // The most recent discard of a row comes first.
        Assert.Same(discards[5], seat.DiscardRows[0][0].Tile);
        Assert.Same(discards[0], seat.DiscardRows[0][5].Tile);
        Assert.Same(discards[discards.Count > 11 ? 11 : discards.Count - 1], seat.DiscardRows[1][0].Tile);
    }

    [Theory]
    [InlineData(PlayerIndices.Zero)]
    [InlineData(PlayerIndices.Three)]
    public void RefreshDiscards_BuildsTheRowsOfTheBottomAndLeftSeatsInDiscardOrder(PlayerIndices index)
    {
        var game = FindRound(g => g.Round.GetDiscard(index).Count >= 8);
        var discards = game.Round.GetDiscard(index);
        var seat = NewSeat(game, index);

        Assert.Same(discards[0], seat.DiscardRows[0][0].Tile);
        Assert.Same(discards[5], seat.DiscardRows[0][5].Tile);
        Assert.Same(discards[6], seat.DiscardRows[1][0].Tile);
    }

    [Fact]
    public void RefreshDiscards_LaysTheRiichiDiscardOnItsSide()
    {
        var game = FindRound(g => Enum.GetValues<PlayerIndices>().Any(p =>
            Enumerable.Range(0, g.Round.GetDiscard(p).Count).Any(i => g.Round.IsRiichiRank(p, i))));
        var index = Enum.GetValues<PlayerIndices>().First(p =>
            Enumerable.Range(0, game.Round.GetDiscard(p).Count).Any(i => game.Round.IsRiichiRank(p, i)));
        var seat = NewSeat(game, index);

        for (var i = 0; i < game.Round.GetDiscard(index).Count; i++)
        {
            var expected = game.Round.IsRiichiRank(index, i)
                ? (AnglePivot)index.RelativePlayerIndex(1)
                : (AnglePivot)index;
            Assert.Equal(expected, TileForDiscard(seat, i).Angle);
        }
    }

    [Fact]
    public void HighlightLastDiscard_HighlightsOnlyTheMostRecentDiscard()
    {
        var game = FindRound(g => g.Round.GetDiscard(PlayerIndices.One).Count >= 3);
        var seat = NewSeat(game, PlayerIndices.One);
        var count = game.Round.GetDiscard(PlayerIndices.One).Count;

        seat.HighlightLastDiscard();

        for (var i = 0; i < count; i++)
        {
            Assert.Equal(i == count - 1, TileForDiscard(seat, i).IsHighlighted);
        }
    }

    [Fact]
    public void RefreshDiscards_DropsAnyHighlight()
    {
        var game = FindRound(g => g.Round.GetDiscard(PlayerIndices.Three).Count >= 3);
        var seat = NewSeat(game, PlayerIndices.Three);
        seat.HighlightLastDiscard();

        seat.RefreshDiscards();

        Assert.All(seat.DiscardRows.SelectMany(r => r), t => Assert.False(t.IsHighlighted));
    }

    [Fact]
    public void HighlightLastDiscard_WithoutAnyDiscard_DoesNothing()
    {
        var game = new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(1));
        var seat = NewSeat(game, PlayerIndices.Two);

        seat.HighlightLastDiscard();

        Assert.All(seat.DiscardRows, row => Assert.Empty(row));
    }

    [Theory]
    [InlineData(PlayerIndices.Zero)]
    [InlineData(PlayerIndices.One)]
    [InlineData(PlayerIndices.Two)]
    [InlineData(PlayerIndices.Three)]
    public void RefreshCombinations_ShowsEveryDeclaredCombinationInDisplayOrder(PlayerIndices index)
    {
        var game = FindRound(g => g.Round.GetHand(index).DeclaredCombinations.Count > 0);
        var seat = NewSeat(game, index);
        seat.RefreshCombinations();
        var combos = game.Round.GetHand(index).DeclaredCombinations;
        var wind = game.GetPlayerCurrentWind(index);

        Assert.Equal(combos.Count, seat.Combinations.Count);
        for (var c = 0; c < combos.Count; c++)
        {
            var expected = combos[c].GetSortedTilesForDisplay(wind).AsEnumerable();
            if (IsReversed(index))
            {
                expected = expected.Reverse();
            }
            var expectedList = expected.ToList();
            var shown = seat.Combinations[c].Tiles;

            Assert.Equal(expectedList.Count, shown.Count);
            for (var i = 0; i < expectedList.Count; i++)
            {
                Assert.Same(expectedList[i].tile, shown[i].Tile);
                Assert.Equal(combos[c].IsConcealedDisplay(i), shown[i].IsConcealed);
                var expectedAngle = (AnglePivot)(expectedList[i].stolen ? index.RelativePlayerIndex(1) : index);
                Assert.Equal(expectedAngle, shown[i].Angle);
            }
        }
    }

    [Fact]
    public void RefreshRound_ClearsTheCombinations()
    {
        var game = FindRound(g => g.Round.GetHand(PlayerIndices.Two).DeclaredCombinations.Count > 0);
        var seat = NewSeat(game, PlayerIndices.Two);
        seat.RefreshCombinations();
        Assert.NotEmpty(seat.Combinations);

        seat.RefreshRound();

        Assert.Empty(seat.Combinations);
    }

    [Fact]
    public void RiichiStick_ComesForwardOnDeclarationAndGoesBackWithTheNextRound()
    {
        var game = PlayFirstRound(1);
        var seat = NewSeat(game, PlayerIndices.Three);
        Assert.False(seat.HasRiichiStick);

        seat.ShowRiichiStick();
        Assert.True(seat.HasRiichiStick);

        seat.RefreshRound();
        Assert.False(seat.HasRiichiStick);
    }

    [Fact]
    public void RefreshTurn_MarksTheSeatOnlyWhileItIsThePlayersTurn()
    {
        var game = PlayFirstRound(1);
        var seat = NewSeat(game, PlayerIndices.Two);

        seat.RefreshTurn(PlayerIndices.Two);
        Assert.True(seat.IsCurrent);

        seat.RefreshTurn(PlayerIndices.One);
        Assert.False(seat.IsCurrent);
    }

    [Fact]
    public void Properties_RaisePropertyChangedSoTheViewFollows()
    {
        var game = PlayFirstRound(1);
        var seat = NewSeat(game, PlayerIndices.One);
        var raised = new List<string?>();
        seat.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        seat.RefreshDiscards();
        seat.ShowRiichiStick();
        seat.RefreshTurn(PlayerIndices.One);

        Assert.Contains(nameof(SeatViewModel.DiscardRows), raised);
        Assert.Contains(nameof(SeatViewModel.HasRiichiStick), raised);
        Assert.Contains(nameof(SeatViewModel.IsCurrent), raised);
    }

    [Fact]
    public void TileHighlight_RaisesPropertyChangedSoTheViewFollows()
    {
        var game = FindRound(g => g.Round.GetDiscard(PlayerIndices.Zero).Count >= 1);
        var seat = NewSeat(game, PlayerIndices.Zero);
        var last = TileForDiscard(seat, game.Round.GetDiscard(PlayerIndices.Zero).Count - 1);
        var raised = new List<string?>();
        last.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        seat.HighlightLastDiscard();

        Assert.Equal(new[] { nameof(TileViewModel.IsHighlighted) }, raised);
    }
}
