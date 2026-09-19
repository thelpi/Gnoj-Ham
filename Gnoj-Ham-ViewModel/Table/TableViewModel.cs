using CommunityToolkit.Mvvm.ComponentModel;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;
using Gnoj_Ham_ViewModel.Services;

namespace Gnoj_Ham_ViewModel;

/// <summary>
/// The game table: the four seats, the doras, the walls, and the counters at the centre. Reads the
/// game and is told when to; it never decides by itself when something changed.
/// </summary>
public sealed partial class TableViewModel : ObservableObject
{
    // The wall is fully displayed until the count of tiles left goes down to one draw for each player.
    private static readonly int AlmostEmptyWallTilesCount = GamePivot.PlayersCount;

    // Tiles of the walls are stacked two by two.
    private const int TilesPerStack = 2;

    private static readonly IReadOnlyList<TileViewModel> NoTiles = Array.Empty<TileViewModel>();

    private readonly GamePivot _game;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="game">The game.</param>
    /// <param name="humanPlayerIndex">The human player's seat.</param>
    /// <param name="revealAllHands"><c>True</c> to show the tiles of every seat face up.</param>
    /// <param name="settings">The user's settings.</param>
    /// <param name="humanActions">Carries out what the human player chooses.</param>
    public TableViewModel(GamePivot game, PlayerIndices humanPlayerIndex, bool revealAllHands, UserSettings settings, IHumanActions humanActions)
    {
        _game = game;

        Seats = Enum.GetValues<PlayerIndices>()
            .Select(i => new SeatViewModel(game, i, i == humanPlayerIndex, revealAllHands))
            .ToList();
        Human = new HumanControlsViewModel(game, humanPlayerIndex, Seats[(int)humanPlayerIndex], settings, humanActions);
        _walls = Enum.GetValues<PlayerIndices>().Select(_ => NoTiles).ToList();
    }

    /// <summary>
    /// The four seats, in player index order.
    /// </summary>
    public IReadOnlyList<SeatViewModel> Seats { get; }

    /// <summary>
    /// Inferred; the seat at the bottom of the table (the first, <see cref="PlayerIndices.Zero"/>).
    /// </summary>
    public SeatViewModel BottomSeat => Seats[(int)PlayerIndices.Zero];

    /// <summary>
    /// Inferred; the seat on the right of the table (the second, <see cref="PlayerIndices.One"/>).
    /// </summary>
    public SeatViewModel RightSeat => Seats[(int)PlayerIndices.One];

    /// <summary>
    /// Inferred; the seat at the top of the table (the third, <see cref="PlayerIndices.Two"/>).
    /// </summary>
    public SeatViewModel TopSeat => Seats[(int)PlayerIndices.Two];

    /// <summary>
    /// Inferred; the seat on the left of the table (the fourth, <see cref="PlayerIndices.Three"/>).
    /// </summary>
    public SeatViewModel LeftSeat => Seats[(int)PlayerIndices.Three];

    /// <summary>
    /// What the human player can do.
    /// </summary>
    public HumanControlsViewModel Human { get; }

    /// <summary>
    /// The dora indicators, in display order; those not yet revealed are face down.
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<TileViewModel> _doraTiles = NoTiles;

    /// <summary>
    /// The four walls, in player index order; each is a run of face-down tiles.
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<IReadOnlyList<TileViewModel>> _walls;

    /// <summary>
    /// The dominant wind, as a character.
    /// </summary>
    [ObservableProperty]
    private string _dominantWindText = string.Empty;

    /// <summary>
    /// The dominant wind, as a tooltip.
    /// </summary>
    [ObservableProperty]
    private string _dominantWindToolTip = string.Empty;

    /// <summary>
    /// The number of turns played as east in the dominant wind.
    /// </summary>
    [ObservableProperty]
    private string _eastTurnCountText = string.Empty;

    /// <summary>
    /// The number of turns played as east in the dominant wind, as a tooltip.
    /// </summary>
    [ObservableProperty]
    private string _eastTurnCountToolTip = string.Empty;

    /// <summary>
    /// The honba count.
    /// </summary>
    [ObservableProperty]
    private string _honbaText = string.Empty;

    /// <summary>
    /// The number of riichi sticks still on the table.
    /// </summary>
    [ObservableProperty]
    private string _pendingRiichiText = string.Empty;

    /// <summary>
    /// The number of tiles left in the wall.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWallAlmostEmpty))]
    private int _wallTilesLeft;

    /// <summary>
    /// Inferred; indicates that the last tiles of the wall are about to be drawn.
    /// </summary>
    public bool IsWallAlmostEmpty => WallTilesLeft <= AlmostEmptyWallTilesCount;

    /// <summary>
    /// Sets the whole table up for a new round: every counter, the doras, the seats and the walls
    /// are read again.
    /// </summary>
    public void RefreshRound()
    {
        RefreshWallTilesLeft();
        RefreshDoras();

        DominantWindText = _game.DominantWind.ToWindDisplay();
        DominantWindToolTip = _game.DominantWind.DominantWindToolTip();
        EastTurnCountText = $"{_game.EastRank}";
        EastTurnCountToolTip = _game.DominantWind.EastTurnCountToolTip();

        HonbaText = _game.HonbaCount.ToString();
        PendingRiichiText = _game.PendingRiichiCount.ToString();

        foreach (var seat in Seats)
        {
            seat.RefreshRound();
        }

        RefreshTurn();
        RefreshWalls();
    }

    /// <summary>
    /// Reads the number of tiles left in the wall again.
    /// </summary>
    public void RefreshWallTilesLeft()
    {
        WallTilesLeft = _game.Round.WallTiles.Count;
    }

    /// <summary>
    /// Reads the dora indicators again.
    /// </summary>
    public void RefreshDoras()
    {
        DoraTiles = DoraIndicatorTiles.Build(_game.Round.DoraIndicatorTiles, _game.Round.VisibleDorasCount);
    }

    /// <summary>
    /// Says whose turn it is.
    /// </summary>
    public void RefreshTurn()
    {
        foreach (var seat in Seats)
        {
            seat.RefreshTurn(_game.Round.CurrentPlayerIndex);
        }
    }

    /// <summary>
    /// Works out how many tiles each of the four walls still holds.
    /// </summary>
    public void RefreshWalls()
    {
        // The order in which the walls are consumed: from the one of the player who opens the wall, going
        // backwards round the table; a wall has the index of the player it is in front of.
        var opening = _game.Round.WallOpeningIndex;
        var consumptionOrder = GamePivot.PerPlayer(step => opening.RelativePlayerIndex(-(int)step));

        // Every tile to display in the walls, and in each of them.
        var wallTiles = (_game.Round.WallTiles.Count + _game.Round.AllTreasureTiles.Count) / TilesPerStack;
        var tilesPerWall = _game.Round.FullTilesList.Count / (GamePivot.PlayersCount * TilesPerStack);

        var walls = new IReadOnlyList<TileViewModel>[consumptionOrder.Count];
        for (var step = 0; step < consumptionOrder.Count; step++)
        {
            // The tiles are consumed from the first wall: the walls after this one are full for as long as it goes.
            var wallsAfter = consumptionOrder.Count - 1 - step;
            var wall = (int)consumptionOrder[step];

            var tilesCountForThisWall = Math.Max(0, Math.Min(tilesPerWall, wallTiles - (tilesPerWall * wallsAfter)));
            var angle = wall % 2 == 0 ? AnglePivot.A0 : AnglePivot.A90;

            walls[wall] = Enumerable.Range(0, tilesCountForThisWall).Select(_ => TileViewModel.FaceDown(angle)).ToList();
        }

        Walls = walls;
    }
}
