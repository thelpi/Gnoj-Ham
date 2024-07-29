using System;

namespace Gnoj_Ham.AutoPlayEvents
{
    public class AfterPonEventArgs : EventArgs
    {
        public int PlayerIndex { get; }
        public int PreviousPlayerIndex { get; }

        public AfterPonEventArgs(int playerIndex, int previousPlayerIndex)
        {
            PlayerIndex = playerIndex;
            PreviousPlayerIndex = previousPlayerIndex;
        }
    }
}
