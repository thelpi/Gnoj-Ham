using System;

namespace Gnoj_Ham
{
    /// <summary>
    /// Event triggered when a tile is involved in an action; it can be a pick, a discard or a call.
    /// </summary>
    /// <seealso cref="EventArgs"/>
    public class TileEventArgs : EventArgs
    {
        #region Embedded properties

        /// <summary>
        /// Player index.
        /// </summary>
        public int PlayerIndex { get; private set; }

        /// <summary>
        /// The tile involved.
        /// </summary>
        public TilePivot Tile { get; private set; }

        #endregion Embedded properties

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="playerIndex">The <see cref="PlayerIndex"/> value.</param>
        /// <param name="tile">The <see cref="Tile"/> value.</param>
        /// <exception cref="ArgumentNullException"><paramref name="tile"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="playerIndex"/> is out of range.</exception>
        internal TileEventArgs(int playerIndex, TilePivot tile)
        {
            if (playerIndex < 0 || playerIndex > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(playerIndex));
            }

            PlayerIndex = playerIndex;
            Tile = tile ?? throw new ArgumentNullException(nameof(tile));
        }

        #endregion Constructors
    }
}
