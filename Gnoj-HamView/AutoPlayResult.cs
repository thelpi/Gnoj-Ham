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
        /// Stackpanel name which contains a button to click.
        /// </summary>
        internal string PanelName { get; set; }

        /// <summary>
        /// Index, in the <see cref="PanelName"/> children, of the button to click.
        /// </summary>
        internal int ChildrenIndex { get; set; }

        #endregion Embedded properties
    }
}
