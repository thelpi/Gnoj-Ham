using System;

namespace Gnoj_Ham.AutoPlayEvents
{
    public delegate void HumanCanCallRonEventHandler(HumanCanCallRonEventArgs evt);

    public class HumanCanCallRonEventArgs : EventArgs
    {
    }
}
