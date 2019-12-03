using System;

namespace Gnoj_Ham
{
    /// <summary>
    /// Delegate for the <see cref="TileEventArgs"/> event.
    /// </summary>
    /// <param name="evt">The event.</param>
    public delegate void TileEventHandler(TileEventArgs evt);

    /// <summary>
    /// Global tools.
    /// </summary>
    public static class GlobalTools
    {
        /// <summary>
        /// Global instance of <see cref="Random"/>.
        /// </summary>
        public static Random Randomizer { get; } = new Random(DateTime.Now.Millisecond);
    }
}
