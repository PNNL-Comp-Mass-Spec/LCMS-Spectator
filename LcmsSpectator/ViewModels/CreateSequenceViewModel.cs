using System;
using System.Collections.ObjectModel;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.DialogServices;
using LcmsSpectator.Utils;
using LcmsSpectatorModels.Models;

namespace LcmsSpectator.ViewModels
{
    public class CreateSequenceViewModel: ViewModelBase
    {
        public ObservableCollection<XicViewModel> XicViewModels { get; private set; }
        public string SequenceText { get; set; }
        public int SelectedCharge { get; set; }
        public int SelectedScan { get; set; }
        public DelegateCommand CreatePrSmCommand { get; private set; }
        public event EventHandler SequenceCreated;
        public CreateSequenceViewModel(ObservableCollection<XicViewModel> xicViewModels, IDialogService dialogService)
        {
            XicViewModels = xicViewModels;
            _dialogService = dialogService;
            CreatePrSmCommand = new DelegateCommand(CreatePrSm, false);
            SelectedCharge = 2;
            SelectedScan = 0;
            if (XicViewModels.Count > 0) SelectedXicViewModel = XicViewModels[0];
        }

        public XicViewModel SelectedXicViewModel
        {
            get { return _selectedXicViewModel; }
            set
            {
                if (_selectedXicViewModel == value) return;
                _selectedXicViewModel = value;
                OnPropertyChanged("SelectedXicViewModel");
            }
        }

        private void CreatePrSm()
        {
            var sequence = Sequence.GetSequenceFromMsGfPlusPeptideStr(SequenceText);
            LcMsRun lcms = null;
            if (sequence == null)
            {
                _dialogService.MessageBox("Invalid sequence.");
                return;
            }
            if (SelectedCharge < 1)
            {
                _dialogService.MessageBox("Invalid Charge.");
                return;
            }
            if (SelectedScan < 0)
            {
                _dialogService.MessageBox("Invalid Scan.");
                return;
            }
            string rawFileName = "";
            if (SelectedXicViewModel == null || SelectedXicViewModel.Lcms == null) SelectedScan = 0;
            else
            {
                rawFileName = SelectedXicViewModel.RawFileName;
                lcms = SelectedXicViewModel.Lcms;
            }
            var prsm = new PrSm
            {
                Heavy = false,
                RawFileName = rawFileName,
                ProteinName = "",
                ProteinNameDesc = "",
                ProteinDesc = "",
                Scan = SelectedScan,
                Lcms = lcms,
                Sequence = sequence,
                SequenceText = SequenceText,
                Charge = SelectedCharge
            };
            var ms2Prod = prsm.Ms2Spectrum as ProductSpectrum;
            if (ms2Prod == null)
            {
                prsm.Scan = 0;
                prsm.Lcms = null;
            }
            if (SequenceCreated != null) SequenceCreated(this, new PrSmChangedEventArgs(prsm));
        }

        private readonly IDialogService _dialogService;
        private XicViewModel _selectedXicViewModel;
    }
}
