using System;

namespace Gnoj_Ham.AutoPlayEvents
{
    public delegate void AfterPonEventHandler(AfterPonEventArgs evt);

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
