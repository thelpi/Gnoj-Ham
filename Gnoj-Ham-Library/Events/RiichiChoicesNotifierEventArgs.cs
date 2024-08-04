namespace Gnoj_Ham_Library.Events;

/// <summary>
/// Event triggered when a riichi can be called by the human player.
/// </summary>
public class RiichiChoicesNotifierEventArgs : EventArgs
{
    /// <summary>
    /// Tiles available to discard to valide the riichi call.
    /// </summary>
    public IReadOnlyList<TilePivot> Tiles { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="tiles"><see cref="Tiles"/></param>
    internal RiichiChoicesNotifierEventArgs(IReadOnlyList<TilePivot> tiles)
    {
        Tiles = tiles;
    }
}
