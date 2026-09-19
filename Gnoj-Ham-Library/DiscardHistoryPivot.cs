using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

/// <summary>
/// Tracks every player's discards, in every form <see cref="RoundPivot"/> needs to reason about: the
/// real order (which shrinks when a tile is claimed by a call), a call-immune virtual order used for
/// furiten bookkeeping, and the "since when" markers riichi and temporary furiten are measured against.
/// </summary>
internal class DiscardHistoryPivot
{
    private readonly List<List<TilePivot>> _discards;
    private readonly List<List<TilePivot>> _virtualDiscards;
    private readonly List<Dictionary<PlayerIndices, int>> _lastOwnDiscardOpponentsVirtualRank;
    private readonly List<PlayerIndices> _playerIndexHistory;

    /// <summary>
    /// Constructor.
    /// </summary>
    internal DiscardHistoryPivot()
    {
        _discards = GamePivot.PerPlayer(_ => new List<TilePivot>(20));
        _virtualDiscards = GamePivot.PerPlayer(_ => new List<TilePivot>(20));
        _playerIndexHistory = new List<PlayerIndices>(10);
        _lastOwnDiscardOpponentsVirtualRank = GamePivot.PerPlayer(player => Enum.GetValues<PlayerIndices>()
            .Where(p => p != player)
            .ToDictionary(p => p, _ => 0));
    }

    /// <summary>
    /// Real discards, per player, in chronological order. Shrinks when a tile is claimed by a call
    /// (see <see cref="TakeLastDiscard"/>).
    /// </summary>
    internal IReadOnlyList<IReadOnlyList<TilePivot>> Discards => _discards;

    /// <summary>
    /// Append-only shadow of <see cref="Discards"/>: unlike real discards, a tile claimed by a call
    /// never leaves it, so ranks recorded against it (see <see cref="GetLastOwnDiscardOpponentRank"/>
    /// and riichi's own opponent ranks) stay meaningful even after such a call.
    /// </summary>
    internal IReadOnlyList<IReadOnlyList<TilePivot>> VirtualDiscards => _virtualDiscards;

    /// <summary>
    /// The players who cleanly discarded in a row, most recent first; reset whenever a call
    /// (pon/chii/kan) interrupts the sequence. Used to check "nothing was called since X".
    /// </summary>
    internal IReadOnlyList<PlayerIndices> PlayerIndexHistory => _playerIndexHistory;

    /// <summary>
    /// Records a discard for the specified player: updates both the real and virtual history, freezes
    /// every opponent's current virtual-discard count for this player (the temporary furiten "since"
    /// marker), and extends - or, if a call just interrupted the sequence, resets - the clean
    /// turn-order history.
    /// </summary>
    /// <param name="playerIndex">The discarding player.</param>
    /// <param name="tile">The discarded tile.</param>
    /// <param name="wasInterruptedByCall"><c>True</c> if a call (pon/chii/kan) is why this player is discarding now, instead of normal turn order.</param>
    internal void RecordDiscard(PlayerIndices playerIndex, TilePivot tile, bool wasInterruptedByCall)
    {
        if (wasInterruptedByCall)
        {
            _playerIndexHistory.Clear();
        }

        _discards[(int)playerIndex].Add(tile);
        _virtualDiscards[(int)playerIndex].Add(tile);

        foreach (var opponent in Enum.GetValues<PlayerIndices>())
        {
            if (opponent != playerIndex)
            {
                _lastOwnDiscardOpponentsVirtualRank[(int)playerIndex][opponent] = _virtualDiscards[(int)opponent].Count;
            }
        }

        _playerIndexHistory.Insert(0, playerIndex);
    }

    /// <summary>
    /// Removes and returns the last (real) discard of the specified player: the tile just claimed by
    /// a chii, pon or kan call.
    /// </summary>
    internal TilePivot TakeLastDiscard(PlayerIndices playerIndex)
    {
        var tile = _discards[(int)playerIndex][^1];
        _discards[(int)playerIndex].RemoveAt(_discards[(int)playerIndex].Count - 1);
        return tile;
    }

    /// <summary>
    /// Records a virtual-only discard: used when a concealed kan is declared from a previous pon, so
    /// the tile (already removed from the real discard pile back when the pon happened) still belongs
    /// in the virtual history for furiten purposes.
    /// </summary>
    internal void RecordVirtualOnly(PlayerIndices playerIndex, TilePivot tile)
    {
        _virtualDiscards[(int)playerIndex].Add(tile);
    }

    /// <summary>
    /// The virtual-discard rank of <paramref name="opponent"/> at the moment <paramref name="playerIndex"/>
    /// last discarded for themselves: the starting point of the temporary furiten window, which -
    /// unlike <see cref="PlayerIndexHistory"/> - survives calls made by someone else in the meantime.
    /// </summary>
    internal int GetLastOwnDiscardOpponentRank(PlayerIndices playerIndex, PlayerIndices opponent)
    {
        return _lastOwnDiscardOpponentsVirtualRank[(int)playerIndex][opponent];
    }
}
