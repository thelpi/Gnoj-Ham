using System;

namespace Gnoj_Ham.AutoPlayEvents
{
    public class InvokeOverlayEventArgs : EventArgs
    {
        public int PlayerIndex { get; }

        public CallTypePivot Action { get; }

        public InvokeOverlayEventArgs(int playerIndex, CallTypePivot action)
        {
            PlayerIndex = playerIndex;
            Action = action;
        }
    }
}
