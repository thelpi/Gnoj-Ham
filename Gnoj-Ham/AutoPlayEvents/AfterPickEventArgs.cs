using System;

namespace Gnoj_Ham.AutoPlayEvents
{
    public delegate void AfterPickEventHandler(AfterPickEventArgs evt);

    public class AfterPickEventArgs : EventArgs
    {
    }
}
