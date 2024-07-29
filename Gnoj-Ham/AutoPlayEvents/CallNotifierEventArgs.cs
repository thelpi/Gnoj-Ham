using System;

namespace Gnoj_Ham.AutoPlayEvents
{
    /// <summary>
    /// Event triggered when a call has to be notified.
    /// </summary>
    public class CallNotifierEventArgs : EventArgs
    {
        /// <summary>
        /// The player index.
        /// </summary>
        public int PlayerIndex { get; internal set; }

        /// <summary>
        /// The type of call.
        /// </summary>
        public CallTypePivot Action { get; internal set; }
    }
}
