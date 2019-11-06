namespace Gnoj_Ham
{
    /// <summary>
    /// Represents the context when a "ron" or a "tsumo" is called.
    /// </summary>
    public class WinContextPivot
    {
        /// <summary>
        /// The latest tile (from self-draw or not).
        /// </summary>
        public TilePivot LatestTile { get; set; }
        /// <summary>
        /// <c>True</c> if <see cref="LatestTile"/> is not actually in the hand (simulation).
        /// </summary>
        public bool IsSimulatedLatestPick { get; set; }
        /// <summary>
        /// <c>True</c> if <see cref="LatestTile"/> was the last tile of the round (from wall or opponent discard).
        /// </summary>
        public bool IsLastTile { get; set; }
        /// <summary>
        /// <c>True</c> if the player has called riichi.
        /// </summary>
        public bool IsRiichi { get; set; }
        /// <summary>
        /// <c>True</c> if the player has called riichi on the first turn.
        /// </summary>
        public bool IsFirstTurnRiichi { get; set; }
        /// <summary>
        /// <c>True</c> if it's the first turn after calling riichi.
        /// </summary>
        public bool IsIppatsu { get; set; }
        /// <summary>
        /// The current dominant wind.
        /// </summary>
        public WindPivot DominantWind { get; set; }
        /// <summary>
        /// The current player wind.
        /// </summary>
        public WindPivot PlayerWind { get; set; }
        /// <summary>
        /// <c>True</c> if it's first turn draw (without call made).
        /// </summary>
        public bool IsFirstTurnDraw { get; set; }
        /// <summary>
        /// Draw type for <see cref="LatestTile"/>.
        /// </summary>
        public DrawTypePivot DrawType { get; set; }
    }
}
