using System;

namespace Gnoj_Ham.AutoPlayEvents
{
    public class InvokeOverlayEventArgs : EventArgs
    {
        public int PlayerIndex { get; }

        public HumanActionPivot Action { get; }

        public InvokeOverlayEventArgs(int playerIndex, HumanActionPivot action)
        {
            PlayerIndex = playerIndex;
            Action = action;
        }
    }
}
