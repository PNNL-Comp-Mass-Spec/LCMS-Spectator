using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LcmsSpectator.ViewModels.SequenceViewer
{
    using LcmsSpectator.ViewModels.Data;

    using ReactiveUI;
    public class FragmentViewModel : ReactiveObject
    {
        public FragmentIonViewModel PrefixIon { get; set; }

        public char Residue { get; set; }

        public string Modification { get; set; }

        public FragmentIonViewModel SuffixIon { get; set; }
    }
}
