using System;

namespace Gnoj_Ham.AutoPlayEvents
{
    /// <summary>
    /// Event triggered when the human can make a call.
    /// </summary>
    public class HumanCallNotifierEventArgs : EventArgs
    {
        /// <summary>
        /// Call type.
        /// </summary>
        public CallTypePivot Call { get; internal set; }

        /// <summary>
        /// In case of 'Riichi' call, indicates if the call is advised (if the advice feature is eanbled).
        /// </summary>
        public bool RiichiAdvised { get; internal set; }
    }
}
