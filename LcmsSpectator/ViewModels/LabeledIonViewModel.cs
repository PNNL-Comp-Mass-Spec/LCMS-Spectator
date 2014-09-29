using GalaSoft.MvvmLight;
using LcmsSpectatorModels.Models;

namespace LcmsSpectator.ViewModels
{
    public class LabeledIonViewModel: ViewModelBase
    {
        public LabeledIon LabeledIon { get; private set; }
        public LabeledIonViewModel(LabeledIon labeledIon)
        {
            LabeledIon = labeledIon;
            _selected = true;
        }

        public bool Selected
        {
            get { return _selected; }
            set
            {
                var oldSelected = _selected;
                _selected = value;
                RaisePropertyChanged("Selected", oldSelected, _selected, true);
            }
        }

        private bool _selected;
    }
}
