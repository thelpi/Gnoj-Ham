using System;
using System.Collections.Generic;

namespace Gnoj_Ham.AutoPlayEvents
{
    /// <summary>
    /// This event is triggered when a riichi is callable by the human player.
    /// </summary>
    public class RiichiChoicesEventArgs : EventArgs
    {
        /// <summary>
        /// Tiles available to discard.
        /// </summary>
        public IReadOnlyList<TilePivot> Tiles { get; internal set; }
    }
}
