using System;
using System.Collections.Generic;

namespace Gnoj_Ham.AutoPlayEvents
{
    /// <summary>
    /// Event triggered when a riichi can be called by the human player.
    /// </summary>
    public class RiichiChoicesNotifierEventArgs : EventArgs
    {
        /// <summary>
        /// Tiles available to discard to valide the riichi call.
        /// </summary>
        public IReadOnlyList<TilePivot> Tiles { get; internal set; }
    }
}
