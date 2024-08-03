using System;
using System.Collections.Generic;
using System.Linq;

namespace Gnoj_Ham
{
    /// <summary>
    /// Represents informations relative to a riichi call.
    /// </summary>
    internal class RiichiPivot
    {
        #region Embedded properties

        private readonly Dictionary<int, int> _opponentsVirtualDiscardRank;

        /// <summary>
        /// Position in the player discard.
        /// </summary>
        internal int DiscardRank { get; private set; }

        /// <summary>
        /// <c>True</c> if the riichi is "daburu" (at first turn without interruption).
        /// </summary>
        internal bool IsDaburu { get; private set; }

        /// <summary>
        /// The tile discarded when the call has been made.
        /// </summary>
        internal TilePivot Tile { get; private set; }

        /// <summary>
        /// The rank, in the virtual discard of each opponent, when the riichi call has been made.
        /// </summary>
        /// <remarks>Key is the opponent index, value is the rank (<c>-1</c> if none).</remarks>
        internal IReadOnlyDictionary<int, int> OpponentsVirtualDiscardRank => _opponentsVirtualDiscardRank;

        #endregion Embedded properties

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="discardRank">The <see cref="DiscardRank"/> value.</param>
        /// <param name="isDaburu">The <see cref="IsDaburu"/> value.</param>
        /// <param name="tile">The <see cref="Tile"/> value.</param>
        /// <param name="opponentsVirtualDiscardRank">The <see cref="_opponentsVirtualDiscardRank"/> value.</param>
        /// <exception cref="ArgumentNullException"><paramref name="tile"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="opponentsVirtualDiscardRank"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="discardRank"/> is out of range.</exception>
        /// <exception cref="ArgumentException"><see cref="Messages.InvalidDiscardRank"/></exception>
        /// <exception cref="ArgumentException"><see cref="Messages.InvalidOpponentsVirtualDiscardRank"/></exception>
        internal RiichiPivot(int discardRank, bool isDaburu, TilePivot tile, IDictionary<int, int> opponentsVirtualDiscardRank)
        {
            if (opponentsVirtualDiscardRank == null)
            {
                throw new ArgumentNullException(nameof(opponentsVirtualDiscardRank));
            }

            if (discardRank < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(discardRank));
            }

            if (isDaburu && discardRank > 0)
            {
                throw new ArgumentException(Messages.InvalidDiscardRank, nameof(discardRank));
            }

            if (opponentsVirtualDiscardRank.Keys.Count != 3 || opponentsVirtualDiscardRank.Values.Any(v => v < 0))
            {
                throw new ArgumentException(Messages.InvalidOpponentsVirtualDiscardRank, nameof(opponentsVirtualDiscardRank));
            }

            DiscardRank = discardRank;
            IsDaburu = isDaburu;
            Tile = tile ?? throw new ArgumentNullException(nameof(tile));
            _opponentsVirtualDiscardRank = new Dictionary<int, int>(opponentsVirtualDiscardRank);
        }

        #endregion Constructors
    }
}
