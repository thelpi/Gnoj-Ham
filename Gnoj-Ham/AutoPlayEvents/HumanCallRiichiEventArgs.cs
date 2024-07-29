using System;

namespace Gnoj_Ham.AutoPlayEvents
{
    public class HumanCallRiichiEventArgs : EventArgs
    {
        public bool ChooseToRiichi { get; }

        public HumanCallRiichiEventArgs(bool chooseToRiichi)
        {
            ChooseToRiichi = chooseToRiichi;
        }
    }
}
