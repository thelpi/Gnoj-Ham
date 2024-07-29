using System;

namespace Gnoj_Ham.AutoPlayEvents
{
    public delegate void AfterDiscardEventHandler(AfterDiscardEventArgs evt);

    public class AfterDiscardEventArgs : EventArgs
    {
    }
}
