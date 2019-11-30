namespace Gnoj_HamView
{
    /// <summary>
    /// Object returned by the auto-play.
    /// </summary>
    internal class AutoPlayResult
    {
        #region Embedded properties

        /// <summary>
        /// <c>True</c> to end the round; <c>False</c> otherwise.
        /// </summary>
        internal bool EndOfRound { get; set; }

        /// <summary>
        /// Identifier of ron player if any; otherwise <c>Null</c>.
        /// </summary>
        internal int? RonPlayerId { get; set; }

        /// <summary>
        /// The panel button to action.
        /// </summary>
        internal PanelButton PanelButton { get; set; }

        #endregion Embedded properties
    }
}
