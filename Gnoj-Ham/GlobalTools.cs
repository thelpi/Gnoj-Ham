using System;

namespace Gnoj_Ham
{
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
