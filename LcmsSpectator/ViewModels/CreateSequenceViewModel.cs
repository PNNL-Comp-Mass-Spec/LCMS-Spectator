using System;
using System.Collections.ObjectModel;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.DialogServices;
using LcmsSpectator.Utils;
using LcmsSpectatorModels.Models;
using LcmsSpectatorModels.SequenceReaders;

namespace LcmsSpectator.ViewModels
{
    public class CreateSequenceViewModel: ViewModelBase
    {
        public ObservableCollection<XicViewModel> XicViewModels { get; private set; }
        public string SequenceText { get; set; }
        public int SequencePosition { get; set; }
        public int SelectedCharge { get; set; }
        public int SelectedScan { get; set; }
        public DelegateCommand CreatePrSmCommand { get; private set; }
        public DelegateCommand InsertModificationCommand { get; private set; }
        public ObservableCollection<Modification> Modifications { get; private set; }
        public Modification SelectedModification { get; set; }
        public event EventHandler SequenceCreated;
        public CreateSequenceViewModel(ObservableCollection<XicViewModel> xicViewModels, IDialogService dialogService)
        {
            XicViewModels = xicViewModels;
            _dialogService = dialogService;
            SequenceText = "";
            CreatePrSmCommand = new DelegateCommand(CreatePrSm, false);
            InsertModificationCommand = new DelegateCommand(InsertModification);
            SelectedCharge = 2;
            SelectedScan = 0;
            if (XicViewModels.Count > 0) SelectedXicViewModel = XicViewModels[0];

            Modifications = new ObservableCollection<Modification>(Modification.CommonModifications);
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

        private void InsertModification()
        {
            var modStr = String.Format("[{0}]", SelectedModification.Name);
            SequenceText = SequenceText.Insert(SequencePosition, modStr);
            OnPropertyChanged("SequenceText");
        }

        private void CreatePrSm()
        {
            var sequenceReader = new SequenceReader();
            Sequence sequence;
            try
            {
                sequence = sequenceReader.Read(SequenceText);
                if (sequence == null) throw new FormatException("Invalid Sequence.");
            }
            catch (FormatException e)
            {
                _dialogService.ExceptionAlert(e);
                return;
            }
            LcMsRun lcms = null;
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
