using CommunityToolkit.Mvvm.ComponentModel;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_ViewModel;

/// <summary>
/// One seat of the table: who sits there, the state of their hand, discards and combinations.
/// Reads the game and is told when to; it never decides by itself when something changed.
/// </summary>
public sealed partial class SeatViewModel : ObservableObject
{
    // A discard row holds this many tiles (the last one takes any remainder).
    private const int DiscardRowLength = 6;
    private const int DiscardRowsCount = 3;

    private static readonly IReadOnlyList<TileViewModel> NoTiles = Array.Empty<TileViewModel>();

    private readonly GamePivot _game;
    private readonly bool _isHuman;
    private readonly bool _revealHand;
    private TileViewModel? _lastDiscard;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="game">The game.</param>
    /// <param name="index">The seat.</param>
    /// <param name="isHuman"><c>True</c> for the human player's seat, whose tiles are always face up.</param>
    /// <param name="revealHand"><c>True</c> to show the tiles of every seat face up.</param>
    public SeatViewModel(GamePivot game, PlayerIndices index, bool isHuman, bool revealHand)
    {
        _game = game;
        _isHuman = isHuman;
        _revealHand = revealHand;

        Index = index;
        SideName = isHuman ? game.Players[(int)index].Name : $"CPU{(int)index}";
        _discardRows = Enumerable.Range(0, DiscardRowsCount).Select(_ => NoTiles).ToList();
    }

    /// <summary>
    /// The seat.
    /// </summary>
    public PlayerIndices Index { get; }

    /// <summary>
    /// The name shown next to the seat.
    /// </summary>
    public string SideName { get; }

    /// <summary>
    /// The player name.
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// The player's points, ready to display.
    /// </summary>
    [ObservableProperty]
    private string _points = string.Empty;

    /// <summary>
    /// The player's wind, as a character.
    /// </summary>
    [ObservableProperty]
    private string _windText = string.Empty;

    /// <summary>
    /// The player's wind, as a name.
    /// </summary>
    [ObservableProperty]
    private string _windToolTip = string.Empty;

    /// <summary>
    /// Indicates if it is this player's turn.
    /// </summary>
    [ObservableProperty]
    private bool _isCurrent;

    /// <summary>
    /// Indicates if the player has declared riichi, and so puts a stick forward.
    /// </summary>
    [ObservableProperty]
    private bool _hasRiichiStick;

    /// <summary>
    /// The concealed tiles of the hand, without the one just picked.
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<TileViewModel> _handTiles = NoTiles;

    /// <summary>
    /// The tile just picked, if any (a list of zero or one tile).
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<TileViewModel> _pickTiles = NoTiles;

    /// <summary>
    /// The combinations declared so far.
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<CombinationViewModel> _combinations = Array.Empty<CombinationViewModel>();

    /// <summary>
    /// The discards, split into rows in the order the seat displays them.
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<IReadOnlyList<TileViewModel>> _discardRows;

    /// <summary>
    /// Sets the seat up for a new round: nothing declared, and everything else read again.
    /// </summary>
    public void RefreshRound()
    {
        Combinations = Array.Empty<CombinationViewModel>();
        RefreshHand(null);
        RefreshDiscards();
        RefreshInfo();
    }

    /// <summary>
    /// Reads the player's name, points and wind again; the riichi stick goes back.
    /// </summary>
    public void RefreshInfo()
    {
        var player = _game.Players[(int)Index];
        var wind = _game.GetPlayerCurrentWind(Index);

        Name = player.Name;
        Points = $"{player.CurrentGamePoints / 1000}k";
        WindText = wind.ToWindDisplay();
        WindToolTip = wind.DisplayName();
        HasRiichiStick = false;
    }

    /// <summary>
    /// Reads the hand again.
    /// </summary>
    /// <param name="pickTile">The tile just picked, shown apart from the rest of the hand; <c>Null</c> if none.</param>
    public void RefreshHand(TilePivot? pickTile)
    {
        var concealed = !_isHuman && !_revealHand;
        var angle = (AnglePivot)Index;

        HandTiles = _game.Round.GetHand(Index).ConcealedTiles
            .Where(t => pickTile == null || !ReferenceEquals(pickTile, t))
            .Select(t => new TileViewModel(t, angle, concealed))
            .ToList();

        PickTiles = pickTile == null
            ? NoTiles
            : new[] { new TileViewModel(pickTile, angle, concealed) };
    }

    /// <summary>
    /// Reads the discards again (which also drops any highlight on them).
    /// </summary>
    public void RefreshDiscards()
    {
        // The seats on the right and at the top build their rows from the other end.
        var reversed = Index == PlayerIndices.One || Index == PlayerIndices.Two;

        var rows = Enumerable.Range(0, DiscardRowsCount).Select(_ => new List<TileViewModel>()).ToList();
        TileViewModel? last = null;
        var i = 0;
        foreach (var tile in _game.Round.GetDiscard(Index))
        {
            var rowIndex = Math.Min(i / DiscardRowLength, DiscardRowsCount - 1);

            // The tile discarded to declare riichi lies on its side.
            var angle = _game.Round.IsRiichiRank(Index, i)
                ? (AnglePivot)Index.RelativePlayerIndex(1)
                : (AnglePivot)Index;

            last = new TileViewModel(tile, angle);
            if (reversed)
            {
                rows[rowIndex].Insert(0, last);
            }
            else
            {
                rows[rowIndex].Add(last);
            }
            i++;
        }

        _lastDiscard = last;
        DiscardRows = rows;
    }

    /// <summary>
    /// Highlights the last discard - the tile the other players can call.
    /// </summary>
    public void HighlightLastDiscard()
    {
        if (_lastDiscard != null)
        {
            _lastDiscard.IsHighlighted = true;
        }
    }

    /// <summary>
    /// Reads the declared combinations again.
    /// </summary>
    public void RefreshCombinations()
    {
        var wind = _game.GetPlayerCurrentWind(Index);

        Combinations = _game.Round.GetHand(Index).DeclaredCombinations
            .Select(c => BuildCombination(c, wind))
            .ToList();
    }

    /// <summary>
    /// Puts the riichi stick forward.
    /// </summary>
    public void ShowRiichiStick()
    {
        HasRiichiStick = true;
    }

    /// <summary>
    /// Says whether it is this player's turn.
    /// </summary>
    /// <param name="currentPlayerIndex">The player whose turn it is.</param>
    public void RefreshTurn(PlayerIndices currentPlayerIndex)
    {
        IsCurrent = currentPlayerIndex == Index;
    }

    private CombinationViewModel BuildCombination(TileComboPivot combo, Winds playerWind)
    {
        var tileTuples = combo.GetSortedTilesForDisplay(playerWind).AsEnumerable();
        if (Index > PlayerIndices.Zero && Index < PlayerIndices.Three)
        {
            tileTuples = tileTuples.Reverse();
        }

        var tiles = new List<TileViewModel>();
        var i = 0;
        foreach (var (tile, stolen) in tileTuples)
        {
            // A tile taken from another player lies turned towards them.
            var angle = (AnglePivot)(stolen ? Index.RelativePlayerIndex(1) : Index);
            tiles.Add(new TileViewModel(tile, angle, combo.IsConcealedDisplay(i)));
            i++;
        }

        return new CombinationViewModel(tiles);
    }
}
