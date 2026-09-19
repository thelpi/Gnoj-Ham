using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;
using Gnoj_Ham_ViewModel;
using Gnoj_Ham_ViewModel.Services;
using Gnoj_Ham_ViewModel_Tests.Fakes;

namespace Gnoj_Ham_ViewModel_Tests;

public class TableViewModel_Tests
{
    private static GamePivot NewGame(int seed)
        => new(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(seed));

    private static TableViewModel NewTable(GamePivot game, bool revealAllHands = false)
    {
        var table = new TableViewModel(game, PlayerIndices.Zero, revealAllHands, new UserSettings(), new FakeHumanActions());
        table.RefreshRound();
        return table;
    }

    // Plays the first round of a game to its end, keeping the game (and so the round's final state).
    private static GamePivot PlayFirstRound(int seed)
    {
        var game = NewGame(seed);
        game.Round.RunAutoPlay(new CancellationToken());
        return game;
    }

    [Fact]
    public void RefreshRound_ShowsThePlayersAndTheirPoints()
    {
        var game = NewGame(1);

        var table = NewTable(game);

        Assert.Equal(4, table.Seats.Count);
        foreach (var seat in table.Seats)
        {
            var player = game.Players[(int)seat.Index];
            Assert.Equal(player.Name, seat.Name);
            Assert.Equal($"{player.CurrentGamePoints / 1000}k", seat.Points);
        }
    }

    [Fact]
    public void RefreshRound_NamesTheSeatsAfterTheHumanPlayerOrTheirCpuNumber()
    {
        var game = NewGame(1);

        var table = NewTable(game);

        Assert.Equal(game.Players[0].Name, table.Seats[0].SideName);
        Assert.Equal("CPU1", table.Seats[1].SideName);
        Assert.Equal("CPU2", table.Seats[2].SideName);
        Assert.Equal("CPU3", table.Seats[3].SideName);
    }

    [Fact]
    public void RefreshRound_ShowsEachPlayersWindWithTheEastSeatFirst()
    {
        var game = NewGame(1);

        var table = NewTable(game);

        foreach (var seat in table.Seats)
        {
            var wind = game.GetPlayerCurrentWind(seat.Index);
            Assert.Equal(wind.ToWindDisplay(), seat.WindText);
            Assert.Equal(wind.DisplayName(), seat.WindToolTip);
        }
        Assert.Single(table.Seats, s => s.WindText == "東");
    }

    [Fact]
    public void RefreshRound_ShowsTheCentreCounters()
    {
        var game = NewGame(1);

        var table = NewTable(game);

        Assert.Equal("東", table.DominantWindText);
        Assert.Equal("Vent dominant : Est", table.DominantWindToolTip);
        Assert.Equal("1", table.EastTurnCountText);
        Assert.Equal("N° de tour en Est", table.EastTurnCountToolTip);
        Assert.Equal("0", table.HonbaText);
        Assert.Equal("0", table.PendingRiichiText);
        Assert.Equal(game.Round.WallTiles.Count, table.WallTilesLeft);
        Assert.False(table.IsWallAlmostEmpty);
    }

    [Fact]
    public void RefreshRound_ShowsOnlyTheFirstDoraIndicatorFaceUp()
    {
        var game = NewGame(1);

        var table = NewTable(game);

        Assert.Equal(5, table.DoraTiles.Count);
        // Laid out from the last indicator to the first: the revealed one ends up rightmost.
        Assert.False(table.DoraTiles[4].IsConcealed);
        Assert.Equal(game.Round.DoraIndicatorTiles[0], table.DoraTiles[4].Tile);
        Assert.All(table.DoraTiles.Take(4), t => Assert.True(t.IsConcealed));
    }

    [Fact]
    public void RefreshRound_MarksExactlyTheCurrentPlayer()
    {
        var game = NewGame(1);

        var table = NewTable(game);

        Assert.Single(table.Seats, s => s.IsCurrent);
        Assert.Equal(game.Round.CurrentPlayerIndex, table.Seats.Single(s => s.IsCurrent).Index);
    }

    [Fact]
    public void RefreshTurn_MovesTheMarkToTheCurrentPlayer()
    {
        var game = PlayFirstRound(1);
        var table = NewTable(game);

        table.RefreshTurn();

        Assert.Equal(game.Round.CurrentPlayerIndex, table.Seats.Single(s => s.IsCurrent).Index);
    }

    [Fact]
    public void RefreshRound_ShowsNoDiscardCombinationOrRiichiStickAtTheStart()
    {
        var table = NewTable(NewGame(1));

        foreach (var seat in table.Seats)
        {
            Assert.Empty(seat.Combinations);
            Assert.All(seat.DiscardRows, row => Assert.Empty(row));
            Assert.False(seat.HasRiichiStick);
        }
    }

    [Fact]
    public void RefreshRound_ShowsTheOpponentsHandsFaceDownAndTheHumanPlayersFaceUp()
    {
        var game = NewGame(1);

        var table = NewTable(game);

        foreach (var seat in table.Seats)
        {
            var concealedTiles = game.Round.GetHand(seat.Index).ConcealedTiles;
            Assert.Equal(concealedTiles.Count, seat.HandTiles.Count);
            Assert.Empty(seat.PickTiles);
            Assert.All(seat.HandTiles, t => Assert.Equal(seat.Index != PlayerIndices.Zero, t.IsConcealed));
        }
    }

    [Fact]
    public void RefreshRound_WithRevealedHands_ShowsEveryHandFaceUp()
    {
        var table = NewTable(NewGame(1), revealAllHands: true);

        Assert.All(table.Seats, seat => Assert.All(seat.HandTiles, t => Assert.False(t.IsConcealed)));
    }

    [Fact]
    public void RefreshHand_SetsTheTilePickedApartFromTheHand()
    {
        var game = NewGame(1);
        var table = NewTable(game);
        var seat = table.Seats[2];
        var picked = game.Round.GetHand(PlayerIndices.Two).ConcealedTiles[3];

        seat.RefreshHand(picked);

        var pickTile = Assert.Single(seat.PickTiles);
        Assert.Same(picked, pickTile.Tile);
        Assert.Equal(game.Round.GetHand(PlayerIndices.Two).ConcealedTiles.Count - 1, seat.HandTiles.Count);
        Assert.DoesNotContain(seat.HandTiles, t => ReferenceEquals(t.Tile, picked));
    }

    [Fact]
    public void RefreshHand_TurnsTilesTowardsTheirSeat()
    {
        var table = NewTable(NewGame(1));

        foreach (var seat in table.Seats)
        {
            Assert.All(seat.HandTiles, t => Assert.Equal((AnglePivot)seat.Index, t.Angle));
        }
    }

    [Fact]
    public void RefreshWalls_ShowsAsManyFaceDownTilesAsTheWallsHold()
    {
        var game = NewGame(1);

        var table = NewTable(game);

        Assert.Equal(4, table.Walls.Count);
        // Two tiles are stacked, so each displayed tile stands for two.
        Assert.Equal(
            (game.Round.WallTiles.Count + game.Round.AllTreasureTiles.Count) / 2,
            table.Walls.Sum(w => w.Count));
        Assert.All(table.Walls, wall => Assert.True(wall.Count <= game.Round.FullTilesList.Count / 8));
        Assert.All(table.Walls.SelectMany(w => w), t =>
        {
            Assert.True(t.IsConcealed);
            Assert.Null(t.Tile);
        });
    }

    [Fact]
    public void RefreshWalls_LaysTheTopAndBottomWallsHorizontallyAndTheSidesVertically()
    {
        var table = NewTable(NewGame(1));

        Assert.All(table.Walls[0], t => Assert.Equal(AnglePivot.A0, t.Angle));
        Assert.All(table.Walls[2], t => Assert.Equal(AnglePivot.A0, t.Angle));
        Assert.All(table.Walls[1], t => Assert.Equal(AnglePivot.A90, t.Angle));
        Assert.All(table.Walls[3], t => Assert.Equal(AnglePivot.A90, t.Angle));
    }

    [Fact]
    public void RefreshWalls_ConsumesTheWallsBackwardsRoundTheTableFromTheOneWhereTheWallIsOpened()
    {
        // Whatever the seat which opens the wall, and however far the round has gone.
        var openings = new HashSet<PlayerIndices>();
        for (var seed = 1; seed <= 40; seed++)
        {
            foreach (var game in new[] { NewGame(seed), PlayFirstRound(seed) })
            {
                var table = NewTable(game);
                var round = game.Round;
                openings.Add(round.WallOpeningIndex);
                var stacks = (round.WallTiles.Count + round.AllTreasureTiles.Count) / 2;
                var perWall = round.FullTilesList.Count / 8;

                // The wall opened is the first to be consumed, then the one before it at the table, and so on.
                var consumption = new List<PlayerIndices> { round.WallOpeningIndex };
                for (var step = 1; step < 4; step++)
                {
                    consumption.Add(consumption[^1].RelativePlayerIndex(-1));
                }

                for (var step = 0; step < consumption.Count; step++)
                {
                    var wallsAfter = 3 - step;
                    var expected = Math.Clamp(stacks - (perWall * wallsAfter), 0, perWall);
                    Assert.Equal(expected, table.Walls[(int)consumption[step]].Count);
                }
            }
        }

        Assert.Equal(Enum.GetValues<PlayerIndices>().Length, openings.Count);
    }

    [Fact]
    public void RefreshWalls_ShrinksAsTheRoundGoesOn()
    {
        var game = NewGame(1);
        var table = NewTable(game);
        var before = table.Walls.Sum(w => w.Count);

        game.Round.RunAutoPlay(new CancellationToken());
        table.RefreshWalls();

        Assert.True(table.Walls.Sum(w => w.Count) < before);
    }

    [Fact]
    public void RefreshWallTilesLeft_FollowsTheWallAndFlagsTheLastTiles()
    {
        // A round played until the wall is (nearly) exhausted.
        GamePivot? game = null;
        for (var seed = 1; seed < 300 && game == null; seed++)
        {
            var candidate = PlayFirstRound(seed);
            if (candidate.Round.WallTiles.Count <= 4)
            {
                game = candidate;
            }
        }
        Assert.NotNull(game);
        var table = NewTable(NewGame(1));
        Assert.False(table.IsWallAlmostEmpty);

        var lateTable = NewTable(game);
        lateTable.RefreshWallTilesLeft();

        Assert.Equal(game.Round.WallTiles.Count, lateTable.WallTilesLeft);
        Assert.True(lateTable.IsWallAlmostEmpty);
    }

    [Fact]
    public void RefreshDoras_RevealsMoreIndicatorsOnceTheyAreVisible()
    {
        // A round where at least one kan revealed another indicator.
        GamePivot? game = null;
        for (var seed = 1; seed < 500 && game == null; seed++)
        {
            var candidate = PlayFirstRound(seed);
            if (candidate.Round.VisibleDorasCount > 1)
            {
                game = candidate;
            }
        }
        Assert.NotNull(game);
        var table = NewTable(game);

        table.RefreshDoras();

        Assert.Equal(5 - game.Round.VisibleDorasCount, table.DoraTiles.Count(t => t.IsConcealed));
    }
}
