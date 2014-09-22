using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using InformedProteomics.Backend.Data.Enum;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.DialogServices;
using LcmsSpectator.Utils;
using LcmsSpectatorModels.Config;
using LcmsSpectatorModels.Models;
using LcmsSpectatorModels.Readers;
using LcmsSpectatorModels.Readers.SequenceReaders;

namespace LcmsSpectator.ViewModels
{
    public class CreateSequenceViewModel: ViewModelBase
    {
        public ObservableCollection<XicViewModel> XicViewModels { get; private set; }
        public ObservableCollection<Target> Targets { get; private set; } 
        public int SequencePosition { get; set; }
        public RelayCommand OpenTargetListCommand { get; private set; }
        public RelayCommand CreatePrSmCommand { get; private set; }
        public RelayCommand InsertModificationCommand { get; private set; }
        public RelayCommand InsertStaticModificationsCommand { get; private set; }
        public RelayCommand PasteCommand { get; private set; }
        public ObservableCollection<Modification> Modifications { get; private set; }
        public Modification SelectedModification { get; set; }
        public CreateSequenceViewModel(ObservableCollection<XicViewModel> xicViewModels, IDialogService dialogService)
        {
            XicViewModels = xicViewModels;
            Targets = new ObservableCollection<Target>();
            _dialogService = dialogService;
            _selectedPrSm = null;
            SequenceText = "";
            OpenTargetListCommand = new RelayCommand(OpenTargetList);
            CreatePrSmCommand = new RelayCommand(CreatePrSm, () => (XicViewModels != null && XicViewModels.Count > 0));
            InsertModificationCommand = new RelayCommand(InsertModification);
            InsertStaticModificationsCommand = new RelayCommand(InsertStaticModifications);
            PasteCommand = new RelayCommand(Paste);
            SelectedCharge = 2;
            SelectedScan = 0;
            if (XicViewModels.Count > 0) SelectedXicViewModel = XicViewModels[0];

            Modifications = new ObservableCollection<Modification>(Modification.CommonModifications);
        }

        public PrSm SelectedPrSm
        {
            get { return _selectedPrSm; }
            set
            {
                var oldPrSm = _selectedPrSm;
                _selectedPrSm = value;
                RaisePropertyChanged("SelectedPrSm", oldPrSm, _selectedPrSm, true);
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

        public XicViewModel SelectedXicViewModel
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

        private void InsertModification()
        {
            var modStr = String.Format("[{0}]", SelectedModification.Name);
            SequenceText = SequenceText.Insert(SequencePosition, modStr);
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
            ILcMsRun lcms = null;
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
                ProteinDesc = "",
                Scan = SelectedScan,
                Lcms = lcms,
                Sequence = sequence,
                SequenceText = SequenceText,
                Charge = SelectedCharge,
                Score = -1.0,
            };
            var ms2Prod = prsm.Ms2Spectrum as ProductSpectrum;
            if (ms2Prod == null)
            {
                prsm.Scan = 0;
                prsm.Lcms = null;
            }
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
                        var modStr = String.Format("+{0}", Math.Round(searchModification.Modification.GetMass(), 3));
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

        private readonly IDialogService _dialogService;
        private XicViewModel _selectedXicViewModel;
        private Target _selectedTarget;
        private int _selectedScan;
        private int _selectedChage;
        private string _sequenceText;
        private PrSm _selectedPrSm;
    }
}
