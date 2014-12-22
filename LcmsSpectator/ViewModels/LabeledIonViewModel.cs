using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using LcmsSpectatorModels.Models;

namespace LcmsSpectator.ViewModels
{
    public class LabeledIonViewModel : ViewModelBase
    {
        public LabeledIon LabeledIon { get; private set; }
        public LabeledIonViewModel(LabeledIon labeledIon, IMessenger messenger=null)
        {
            if (messenger != null) MessengerInstance = messenger;
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
