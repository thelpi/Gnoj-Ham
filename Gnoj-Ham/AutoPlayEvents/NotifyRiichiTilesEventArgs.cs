using System;
using System.Collections.Generic;

namespace Gnoj_Ham.AutoPlayEvents
{
    public delegate void NotifyRiichiTilesEventHandler(NotifyRiichiTilesEventArgs evt);

    public class NotifyRiichiTilesEventArgs : EventArgs
    {
        public IReadOnlyList<TilePivot> Tiles { get; }

        public NotifyRiichiTilesEventArgs(IReadOnlyList<TilePivot> tiles)
        {
            Tiles = tiles;
        }
    }
}
