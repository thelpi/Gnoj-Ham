namespace Gnoj_Ham
{
    /// <summary>
    /// Enumeration of draw types.
    /// </summary>
    public enum DrawTypePivot
    {
        /// <summary>
        /// From wall.
        /// </summary>
        Wall,
        /// <summary>
        /// From compensation tile after calling a kan.
        /// </summary>
        Compensation,
        /// <summary>
        /// From opponent's discard.
        /// </summary>
        OpponentDiscard,
        /// <summary>
        /// From opponent's fourth tile while calling kan.
        /// </summary>
        OpponentKanCall
    }
}
