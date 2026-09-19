using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

/// <summary>
/// How the shuffled tiles of a round are dealt: a hand for each player, then the live wall, then the
/// dead wall - the tiles which replace the ones taken by a kan, and the dora and ura-dora indicators.
/// This is the only place which knows that order, and the size of each part.
/// </summary>
internal sealed class WallLayout
{
    /// <summary>
    /// The tiles of a hand as it is dealt.
    /// </summary>
    internal const int HandSize = 13;

    /// <summary>
    /// The most kans a round can see; the dead wall holds a tile for each to replace the tile taken.
    /// </summary>
    internal const int MaxKans = 4;

    /// <summary>
    /// The dora indicators (and the ura-dora ones): the first, and one more for each kan.
    /// </summary>
    internal const int IndicatorsCount = 1 + MaxKans;

    /// <summary>
    /// The tiles of the dead wall: those which replace the ones taken by a kan, and both kinds of indicators.
    /// </summary>
    internal const int DeadWallSize = MaxKans + (2 * IndicatorsCount);

    /// <summary>
    /// Constructor; deals the tiles.
    /// </summary>
    /// <param name="tiles">The shuffled tiles, whatever was done to them before the deal.</param>
    /// <exception cref="ArgumentException">There are not enough tiles to deal.</exception>
    internal WallLayout(List<TilePivot> tiles)
    {
        if (tiles.Count < (GamePivot.PlayersCount * HandSize) + DeadWallSize)
        {
            throw new ArgumentException("There are not enough tiles to deal.", nameof(tiles));
        }

        var start = 0;
        List<TilePivot> Take(int count)
        {
            var part = tiles.GetRange(start, count);
            start += count;
            return part;
        }

        Hands = GamePivot.PerPlayer(_ => Take(HandSize));
        LiveWall = Take(tiles.Count - start - DeadWallSize);
        CompensationTiles = Take(MaxKans);
        DoraIndicators = Take(IndicatorsCount);
        UraDoraIndicators = Take(IndicatorsCount);
    }

    /// <summary>
    /// The hand of each player, in seat order.
    /// </summary>
    internal IReadOnlyList<List<TilePivot>> Hands { get; }

    /// <summary>
    /// The tiles the players pick from.
    /// </summary>
    internal List<TilePivot> LiveWall { get; }

    /// <summary>
    /// The tiles which replace the ones taken by a kan.
    /// </summary>
    internal List<TilePivot> CompensationTiles { get; }

    /// <summary>
    /// The dora indicators, the first of which is visible from the start.
    /// </summary>
    internal List<TilePivot> DoraIndicators { get; }

    /// <summary>
    /// The ura-dora indicators.
    /// </summary>
    internal List<TilePivot> UraDoraIndicators { get; }

    /// <summary>
    /// Gets where the hand of a player starts in the tiles dealt.
    /// </summary>
    /// <param name="playerIndex">The player index.</param>
    /// <returns>The index of the first tile of the hand.</returns>
    internal static int HandStart(PlayerIndices playerIndex)
    {
        return (int)playerIndex * HandSize;
    }
}
