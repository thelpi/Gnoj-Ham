using System;

namespace Gnoj_Ham.AutoPlayEvents
{
    public delegate void TimerEventHandler(TimerEventArgs evt);

    public class TimerEventArgs : EventArgs
    {
    }
}
