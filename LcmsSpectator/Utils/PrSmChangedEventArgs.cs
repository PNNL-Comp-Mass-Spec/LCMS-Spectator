using System;
using LcmsSpectatorModels.Models;

namespace LcmsSpectator.Utils
{
    public class PrSmChangedEventArgs: EventArgs
    {
        public PrSm PrSm { get; private set; }

        public PrSmChangedEventArgs(PrSm prsm)
        {
            PrSm = prsm;
        }
    }
}
