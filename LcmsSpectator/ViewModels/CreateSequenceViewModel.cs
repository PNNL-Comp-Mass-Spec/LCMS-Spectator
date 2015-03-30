using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using InformedProteomics.Backend.Data.Enum;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.Config;
using LcmsSpectator.DialogServices;
using LcmsSpectator.Models;
using LcmsSpectator.Readers;
using LcmsSpectator.Readers.SequenceReaders;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
    public class CreateSequenceViewModel: ReactiveObject
    {
        public CreateSequenceViewModel(ReactiveList<DataSetViewModel> dataSetViewModels, IDialogService dialogService)
        {
            DataSetViewModels = dataSetViewModels;
            Targets = new ReactiveList<Target>();
            _dialogService = dialogService;
            SequenceText = "";
            var openTargetListCommand = ReactiveCommand.Create();
            openTargetListCommand.Subscribe(_ => OpenTargetList());
            OpenTargetListCommand = openTargetListCommand;

            var createPrSmCommand = ReactiveCommand.Create();
            createPrSmCommand.Subscribe(_ => CreatePrSm());
            CreatePrSmCommand = createPrSmCommand;

            var insertStaticModificationsCommand = ReactiveCommand.Create();
            insertStaticModificationsCommand.Subscribe(_ => InsertStaticModifications());
            InsertStaticModificationsCommand = insertStaticModificationsCommand;

            var pasteCommand = ReactiveCommand.Create();
            pasteCommand.Subscribe(_ => Paste());
            PasteCommand = pasteCommand;

            SelectedCharge = 2;
            SelectedScan = 0;
            if (DataSetViewModels.Count > 0) SelectedDataSetViewModel = DataSetViewModels[0];

            // When target changes, update sequence text, charge, insert static modifications.
            this.WhenAnyValue(x => x.SelectedTarget)
                .Where(target => (target != null && !String.IsNullOrEmpty(target.SequenceText)))
                .Subscribe(target =>
                {
                    SequenceText = _selectedTarget.SequenceText;
                    InsertStaticModifications();
                    if (_selectedTarget.Charge > 0) SelectedCharge = _selectedTarget.Charge;
                });

            // When PrSm changes, update scan, sequence text, and charge
            this.WhenAnyValue(x => x.SelectedPrSm)
                .Where(prsm => prsm != null)
                .Subscribe(prsm =>
            {
                SelectedScan = prsm.Scan;
                SequenceText = prsm.SequenceText;
                SelectedCharge = prsm.Charge;
            });

            Modifications = IcParameters.Instance.RegisteredModifications;
        }

        public ReactiveList<DataSetViewModel> DataSetViewModels { get; private set; }
        public ReactiveList<Target> Targets { get; private set; }
        public ReactiveList<Modification> Modifications { get; private set; }
        public Modification SelectedModification { get; set; }

        // Commands
        public IReactiveCommand OpenTargetListCommand { get; private set; }
        public IReactiveCommand CreatePrSmCommand { get; private set; }
        public IReactiveCommand InsertStaticModificationsCommand { get; private set; }
        public IReactiveCommand PasteCommand { get; private set; }

        public int SequencePosition { get; set; }

        private PrSm _selectedPrSm;
        public PrSm SelectedPrSm
        {
            get { return _selectedPrSm; }
            set { this.RaiseAndSetIfChanged(ref _selectedPrSm, value); }
        }

        private int _selectedScan;
        public int SelectedScan
        {
            get { return _selectedScan; }
            set { this.RaiseAndSetIfChanged(ref _selectedScan, value); }
        }

        private int _selectedCharge;
        public int SelectedCharge
        {
            get { return _selectedCharge; }
            set { this.RaiseAndSetIfChanged(ref _selectedCharge, value); }
        }

        private string _sequenceText;
        public string SequenceText
        {
            get { return _sequenceText; }
            set { this.RaiseAndSetIfChanged(ref _sequenceText, value); }
        }

        private DataSetViewModel _selectedDataSetViewModel;
        public DataSetViewModel SelectedDataSetViewModel
        {
            get { return _selectedDataSetViewModel; }
            set { this.RaiseAndSetIfChanged(ref _selectedDataSetViewModel, value); }
        }

        private Target _selectedTarget;
        public Target SelectedTarget
        {
            get { return _selectedTarget; }
            set { this.RaiseAndSetIfChanged(ref _selectedTarget, value); }
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

        private readonly IDialogService _dialogService;
    }
}
