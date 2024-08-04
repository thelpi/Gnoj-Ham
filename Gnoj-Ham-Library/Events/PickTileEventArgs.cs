namespace Gnoj_Ham_Library.Events;

/// <summary>
/// Event triggered when a tile is involved in an action; it can be a pick, a discard or a call.
/// </summary>
/// <seealso cref="EventArgs"/>
public class PickTileEventArgs : EventArgs
{
    /// <summary>
    /// Player index.
    /// </summary>
    public int PlayerIndex { get; }

    /// <summary>
    /// The tile involved.
    /// </summary>
    public TilePivot Tile { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="playerIndex">The <see cref="PlayerIndex"/> value.</param>
    /// <param name="tile">The <see cref="Tile"/> value.</param>
    internal PickTileEventArgs(int playerIndex, TilePivot tile)
    {
        PlayerIndex = playerIndex;
        Tile = tile;
    }
}
