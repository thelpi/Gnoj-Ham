namespace Gnoj_Ham.AutoPlayEvents
{
    /// <summary>
    /// Event triggered when a call is available.
    /// </summary>
    public class ReadyToCallNotifierEventArgs
    {
        /// <summary>
        /// The type of call.
        /// </summary>
        public CallTypePivot Call { get; internal set; }

        /// <summary>
        /// Player index; usage only for call 'Pon'.
        /// </summary>
        public int PlayerIndex { get; internal set; }

        /// <summary>
        /// Previous player index; usage only for call 'Pon'.
        /// </summary>
        public int PreviousPlayerIndex { get; internal set; }

        /// <summary>
        /// Previous player index; usage only for call 'Kan''.
        /// </summary>
        public int? PotentialPreviousPlayerIndex { get; internal set; }
    }
}
