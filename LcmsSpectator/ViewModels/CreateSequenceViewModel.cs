using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
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
        public string SequenceText { get; set; }
        public int SequencePosition { get; set; }
        public int SelectedCharge { get; set; }
        public int SelectedScan { get; set; }
        public DelegateCommand OpenTargetListCommand { get; private set; }
        public DelegateCommand CreatePrSmCommand { get; private set; }
        public DelegateCommand InsertModificationCommand { get; private set; }
        public DelegateCommand InsertStaticModificationsCommand { get; private set; }
        public ObservableCollection<Modification> Modifications { get; private set; }
        public Modification SelectedModification { get; set; }
        public event EventHandler SequenceCreated;
        public CreateSequenceViewModel(ObservableCollection<XicViewModel> xicViewModels, IDialogService dialogService)
        {
            XicViewModels = xicViewModels;
            Targets = new ObservableCollection<Target>();
            _dialogService = dialogService;
            SequenceText = "";
            OpenTargetListCommand = new DelegateCommand(OpenTargetList);
            CreatePrSmCommand = new DelegateCommand(CreatePrSm, false);
            InsertModificationCommand = new DelegateCommand(InsertModification);
            InsertStaticModificationsCommand = new DelegateCommand(InsertStaticModifications);
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

        public Target SelectedTarget
        {
            get { return _selectedTarget; }
            set
            {
                _selectedTarget = value;
                SequenceText = _selectedTarget.SequenceText;
                InsertStaticModifications();
                OnPropertyChanged("SequenceText");
                OnPropertyChanged("SelectedTarget");
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

        private void InsertStaticModifications()
        {
            if (SequenceText == "" || IcParameters.Instance.SearchModifications.Count == 0) return;
            if (SequenceText.Contains("+")) InsertStaticMsgfPlusModifications();
            else InsertStaticLcmsSpectatorModifications();
            OnPropertyChanged("SequenceText");
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
                    if (searchModification.IsFixedModification && searchModification.TargetResidue == residue)
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
                    if (searchModification.IsFixedModification && searchModification.TargetResidue == residue)
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

        private readonly IDialogService _dialogService;
        private XicViewModel _selectedXicViewModel;
        private Target _selectedTarget;
    }
}
