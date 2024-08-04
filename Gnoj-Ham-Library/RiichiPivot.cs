namespace Gnoj_Ham_Library;

/// <summary>
/// Represents informations relative to a riichi call.
/// </summary>
internal class RiichiPivot
{
    /// <summary>
    /// Position in the player discard.
    /// </summary>
    internal int DiscardRank { get; }

    /// <summary>
    /// <c>True</c> if the riichi is "daburu" (at first turn without interruption).
    /// </summary>
    internal bool IsDaburu { get; }

    /// <summary>
    /// The tile discarded when the call has been made.
    /// </summary>
    internal TilePivot Tile { get; }

    /// <summary>
    /// The rank, in the virtual discard of each opponent, when the riichi call has been made.
    /// </summary>
    /// <remarks>Key is the opponent index, value is the rank (<c>-1</c> if none).</remarks>
    internal IReadOnlyDictionary<int, int> OpponentsVirtualDiscardRank { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="discardRank">The <see cref="DiscardRank"/> value.</param>
    /// <param name="isDaburu">The <see cref="IsDaburu"/> value.</param>
    /// <param name="tile">The <see cref="Tile"/> value.</param>
    /// <param name="opponentsVirtualDiscardRank">The <see cref="_opponentsVirtualDiscardRank"/> value.</param>
    internal RiichiPivot(int discardRank, bool isDaburu, TilePivot tile, IDictionary<int, int> opponentsVirtualDiscardRank)
    {
        DiscardRank = discardRank;
        IsDaburu = isDaburu;
        Tile = tile;
        OpponentsVirtualDiscardRank = new Dictionary<int, int>(opponentsVirtualDiscardRank);
    }
}
