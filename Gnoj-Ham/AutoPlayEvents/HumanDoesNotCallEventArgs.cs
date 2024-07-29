using System;

namespace Gnoj_Ham.AutoPlayEvents
{
    public delegate void HumanDoesNotCallEventHandler(HumanDoesNotCallEventArgs evt);

    public class HumanDoesNotCallEventArgs : EventArgs
    {
    }
}
