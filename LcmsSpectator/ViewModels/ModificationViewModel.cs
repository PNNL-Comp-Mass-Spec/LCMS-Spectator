using System;
using InformedProteomics.Backend.Data.Sequence;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
    public class ModificationViewModel: ReactiveObject
    {
        public ModificationViewModel(Modification modification)
        {
            Modification = modification;
        }

        private Modification _modification;
        public Modification Modification
        {
            get { return _modification; }
            set { this.RaiseAndSetIfChanged(ref _modification, value); }
        }

        private bool _selected;
        public bool Selected
        {
            get { return _selected; }
            set { this.RaiseAndSetIfChanged(ref _selected, value); }
        }
    }
}
