using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using InformedProteomics.Backend.Data.Enum;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.DialogServices;
using LcmsSpectatorModels.Config;
using LcmsSpectatorModels.Models;
using LcmsSpectatorModels.Readers;
using LcmsSpectatorModels.Readers.SequenceReaders;

namespace LcmsSpectator.ViewModels
{
    public class CreateSequenceViewModel: ViewModelBase
    {
        public ObservableCollection<DataSetViewModel> DataSetViewModels { get; private set; }
        public ObservableCollection<Target> Targets { get; private set; } 
        public ObservableCollection<Modification> Modifications { get; private set; }
        public Modification SelectedModification { get; set; }

        #region Commands
        public RelayCommand OpenTargetListCommand { get; private set; }
        public RelayCommand CreatePrSmCommand { get; private set; }
        public RelayCommand InsertModificationCommand { get; private set; }
        public RelayCommand InsertStaticModificationsCommand { get; private set; }
        public RelayCommand PasteCommand { get; private set; }
        #endregion

        public CreateSequenceViewModel(ObservableCollection<DataSetViewModel> dataSetViewModels, IDialogService dialogService, IMessenger messenger)
        {
            MessengerInstance = messenger;
            DataSetViewModels = dataSetViewModels;
            Targets = new ObservableCollection<Target>();
            _dialogService = dialogService;
            SequenceText = "";
            OpenTargetListCommand = new RelayCommand(OpenTargetList);
            //CreatePrSmCommand = new RelayCommand(CreatePrSm, () => (DataSetViewModels != null && DataSetViewModels.Count > 0));
            CreatePrSmCommand = new RelayCommand(CreatePrSm);
            InsertModificationCommand = new RelayCommand(InsertModification);
            InsertStaticModificationsCommand = new RelayCommand(InsertStaticModifications);
            PasteCommand = new RelayCommand(Paste);
            SelectedCharge = 2;
            SelectedScan = 0;
            if (DataSetViewModels.Count > 0) SelectedDataSetViewModel = DataSetViewModels[0];

            MessengerInstance.Register<PropertyChangedMessage<PrSm>>(this, SelectedPrSmChanged);
            MessengerInstance.Register<XicPlotViewModel.SelectedScanChangedMessage>(this, SelectedScanChanged);
            //Messenger.Default.Register<PropertyChangedMessage<string>>(this, SelectedRawFileChanged);

            Modifications = new ObservableCollection<Modification>(Modification.CommonModifications);
        }

        #region Public Properties
        public int SequencePosition { get; set; }

        public PrSm SelectedPrSm
        {
            get { return _selectedPrSm; }
            set
            {
                var oldSelectedPrSm = _selectedPrSm;
                _selectedPrSm = value;
                RaisePropertyChanged("SelectedPrSm", oldSelectedPrSm, _selectedPrSm, true);
            }
        }

        public int SelectedScan
        {
            get { return _selectedScan; }
            set
            {
                _selectedScan = value;
                RaisePropertyChanged();
            }
        }

        public int SelectedCharge
        {
            get { return _selectedChage; }
            set
            {
                _selectedChage = value;
                RaisePropertyChanged();
            }
        }

        public string SequenceText
        {
            get { return _sequenceText; }
            set
            {
                _sequenceText = value;
                RaisePropertyChanged();
            }
        }

        public DataSetViewModel SelectedDataSetViewModel
        {
            get { return _selectedXicViewModel; }
            set
            {
                if (_selectedXicViewModel == value) return;
                _selectedXicViewModel = value;
                RaisePropertyChanged();
            }
        }

        public Target SelectedTarget
        {
            get { return _selectedTarget; }
            set
            {
                _selectedTarget = value;
                SequenceText = _selectedTarget.SequenceText;
                InsertStaticModifications();
                if (_selectedTarget.Charge != 0)
                {
                    SelectedCharge = _selectedTarget.Charge;
                }
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Public Methods
        public void OpenTargetList()
        {
            var targetFileName = _dialogService.OpenFile(".txt", @"Target Files (*.txt)|*.txt");
            if (targetFileName == "") return;
            var targetReader = new TargetFileReader(targetFileName);
            List<Target> targets;
            try
            {
                targets = targetReader.Read();
            }
            catch (Exception e)
            {
                _dialogService.ExceptionAlert(e);
                return;
            }
            foreach (var target in targets) Targets.Add(target);
        }
        #endregion

        #region Event Handlers
        private void SelectedScanChanged(XicPlotViewModel.SelectedScanChangedMessage message)
        {
            SelectedScan = message.Scan;
        }

        private void SelectedPrSmChanged(PropertyChangedMessage<PrSm> message)
        {
            if (message.PropertyName == "PrSm" && message.Sender is PrSmViewModel)
            {
                var prsm = message.NewValue;
                SelectedScan = prsm.Scan;
                SequenceText = prsm.SequenceText;
                SelectedCharge = prsm.Charge;
            }
        }
        #endregion

        #region Private methods
        private void InsertModification()
        {
            var modStr = String.Format("[{0}]", SelectedModification.Name);
            SequenceText = SequenceText.Insert(SequencePosition, modStr);
        }

        private void CreatePrSm()
        {
            var sequenceReader = new SequenceReader();
            Sequence sequence=null;
            try
            {
                sequence = sequenceReader.Read(SequenceText);
                if (sequence == null) throw new FormatException("Invalid Sequence.");
            }
            catch (FormatException e)
            {
                _dialogService.MessageBox(e.Message);
                return;
            }
            finally
            {
                if (sequence == null) sequence = new Sequence(new List<AminoAcid>()); 
            }

            if (sequence.Count == 0 && SelectedCharge == 0)
            {
                    _dialogService.MessageBox("Invalid Charge.");
                    return;
            }
            if (sequence.Count == 0 && SelectedScan < 0)
            {
                _dialogService.MessageBox("Invalid sequence and scan number.");
                return;
            }
            ILcMsRun lcms = SelectedDataSetViewModel.Lcms;
            string rawFileName = SelectedDataSetViewModel.RawFileName;
            var prsm = new PrSm
            {
                Heavy = false,
                RawFileName = rawFileName,
                ProteinName = "",
                ProteinDesc = "",
                Scan = Math.Min(Math.Max(SelectedScan, 0), lcms.MaxLcScan),
                Lcms = lcms,
                Charge = SelectedCharge,
                Sequence = sequence,
                SequenceText = SequenceText,
                Score = -1.0,
            };
            SelectedPrSm = prsm;
        }

        private void InsertStaticModifications()
        {
            if (SequenceText == "" || IcParameters.Instance.SearchModifications.Count == 0) return;
            if (SequenceText.Contains("+")) InsertStaticMsgfPlusModifications();
            else InsertStaticLcmsSpectatorModifications();
        }

        private void InsertStaticMsgfPlusModifications()
        {
            const string pattern = @"[A-Z](\+[0-9]+\.[0-9]+)*";

            var matches = Regex.Matches(SequenceText, pattern);

            var newSequence = new List<string>();

            foreach (Match match in matches)
            {
                var matchStr = match.Value;
                var residue = matchStr[0];
                foreach (var searchModification in IcParameters.Instance.SearchModifications)
                {
                    if (searchModification.IsFixedModification && 
                        searchModification.Location == SequenceLocation.Everywhere &&
                        searchModification.TargetResidue == residue)
                    {
                        var modStr = String.Format("+{0}", Math.Round(searchModification.Modification.Mass, 3));
                        if (!matchStr.Contains(modStr)) matchStr += modStr;
                    }
                }
                newSequence.Add(matchStr);
            }

            SequenceText = "";
            foreach (var aa in newSequence) SequenceText += aa;
        }

        private void InsertStaticLcmsSpectatorModifications()
        {
            const string pattern = @"[A-Z](\[[A-Z][a-z]+\])*";

            var matches = Regex.Matches(SequenceText, pattern);

            var newSequence = new List<string>();

            foreach (Match match in matches)
            {
                var matchStr = match.Value;
                var residue = matchStr[0];
                foreach (var searchModification in IcParameters.Instance.SearchModifications)
                {
                    if (searchModification.IsFixedModification &&
                        searchModification.Location == SequenceLocation.Everywhere &&
                        searchModification.TargetResidue == residue)
                    {
                        var modStr = String.Format("[{0}]", searchModification.Modification);
                        if (!matchStr.Contains(modStr)) matchStr += modStr;
                    }
                }
                newSequence.Add(matchStr);
            }

            SequenceText = "";
            foreach (var aa in newSequence) SequenceText += aa;
        }

        private void Paste()
        {
            var clipboardText = Clipboard.GetText();
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(clipboardText);
            writer.Flush();
            stream.Position = 0;

            var targetReader = new TargetFileReader(stream);
            List<Target> targets;
            try
            {
                targets = targetReader.Read();
            }
            catch (Exception e)
            {
                _dialogService.ExceptionAlert(e);
                return;
            }
            foreach (var target in targets) Targets.Add(target);
        }
        #endregion

        #region Private Members
        private readonly IDialogService _dialogService;
        private DataSetViewModel _selectedXicViewModel;
        private Target _selectedTarget;
        private int _selectedScan;
        private int _selectedChage;
        private string _sequenceText;
        private PrSm _selectedPrSm;
        #endregion
    }
}
