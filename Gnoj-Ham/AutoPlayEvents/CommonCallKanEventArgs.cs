using System;

namespace Gnoj_Ham.AutoPlayEvents
{
    public delegate void CommonCallKanEventHandler(CommonCallKanEventArgs evt);

    public class CommonCallKanEventArgs : EventArgs
    {
        public int? PreviousPlayerIndex { get; }

        public CommonCallKanEventArgs(int? previousPlayerIndex)
        {
            PreviousPlayerIndex = previousPlayerIndex;
        }
    }
}
