// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CreateSequenceViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class is a view model for editing the sequence, charge, and scan number
//   of a protein-spectrum match.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels.Data
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Linq;
    using System.Text.RegularExpressions;

    using InformedProteomics.Backend.Data.Enum;
    using InformedProteomics.Backend.Data.Sequence;
    using InformedProteomics.Backend.Data.Spectrometry;

    using Config;
    using DialogServices;
    using Models;
    using Readers.SequenceReaders;
    using Utils;

    using ReactiveUI;

    /// <summary>
    /// This class is a view model for editing the sequence, charge, and scan number
    /// of a protein-spectrum match.
    /// </summary>
    public class CreateSequenceViewModel : ReactiveObject
    {
        /// <summary>
        /// Dialog service for opening dialogs from view model.
        /// </summary>
        private readonly IDialogService dialogService;

        /// <summary>
        /// The selected protein-spectrum match.
        /// </summary>
        private PrSm selectedPrSm;

        /// <summary>
        /// The scan number selected by the user.
        /// </summary>
        private int selectedScan;

        /// <summary>
        /// The charge selected by the users.
        /// </summary>
        private int selectedCharge;

        /// <summary>
        /// The sequence selected by the user.
        /// </summary>
        private string sequenceText;

        /// <summary>
        /// The data set selected by the user.
        /// </summary>
        private DataSetViewModel selectedDataSetViewModel;

        /// <summary>
        /// Initializes a new instance of the CreateSequenceViewModel class.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        public CreateSequenceViewModel(IDialogService dialogService)
        {
            Targets = new ReactiveList<Target>();
            this.dialogService = dialogService;
            SequenceText = string.Empty;

            var createPrSmCommand = ReactiveCommand.Create();
            createPrSmCommand.Subscribe(_ => CreatePrSmImplementation());
            CreatePrSmCommand = createPrSmCommand;

            CreateAndAddPrSmCommand = ReactiveCommand.Create();
            CreateAndAddPrSmCommand.Subscribe(_ => CreatePrSmImplementation());

            var insertStaticModificationsCommand = ReactiveCommand.Create();
            insertStaticModificationsCommand.Subscribe(_ => InsertStaticModifications());
            InsertStaticModificationsCommand = insertStaticModificationsCommand;

            SelectedCharge = 2;
            SelectedScan = 0;

            // When PrSm changes, update scan, sequence text, and charge
            this.WhenAnyValue(x => x.SelectedPrSm)
                .Where(prsm => prsm != null)
                .Subscribe(prsm =>
            {
                SelectedScan = prsm.Scan;
                SequenceText = prsm.SequenceText;
                SelectedCharge = Math.Max(prsm.Charge, 1);
            });

            Modifications = IcParameters.Instance.RegisteredModifications;
        }

        /// <summary>
        /// Gets the list of target sequences and charges.
        /// </summary>
        public ReactiveList<Target> Targets { get; }

        /// <summary>
        /// Gets a list of modifications registered with LCMSSpectator.
        /// </summary>
        public ReactiveList<Modification> Modifications { get; }

        /// <summary>
        /// Gets or sets the modification selected for insertion into the sequence.
        /// </summary>
        public Modification SelectedModification { get; set; }

        /// <summary>
        /// Gets a command that when executed creates a new protein-spectrum match from the
        /// selected sequence, charge, scan number, and data set.
        /// </summary>
        public IReactiveCommand CreatePrSmCommand { get; }

        /// <summary>
        /// Gets a command that when executed creates a new protein-spectrum match from the
        /// selected sequence, charge, scan number, and data set. It also signals the parent
        /// data set that it should be added to the Scan View.
        /// </summary>
        public ReactiveCommand<object> CreateAndAddPrSmCommand { get; }

        /// <summary>
        /// Gets a command that when executed inserts the modifications into the sequence
        /// that are marked as static for a certain residue.
        /// </summary>
        public IReactiveCommand InsertStaticModificationsCommand { get; }

        /// <summary>
        /// Gets or sets the selected protein-spectrum match.
        /// </summary>
        public PrSm SelectedPrSm
        {
            get => selectedPrSm;
            set => this.RaiseAndSetIfChanged(ref selectedPrSm, value);
        }

        /// <summary>
        /// Gets or sets the scan number selected by the user.
        /// </summary>
        public int SelectedScan
        {
            get => selectedScan;
            set => this.RaiseAndSetIfChanged(ref selectedScan, value);
        }

        /// <summary>
        /// Gets or sets the charge selected by the users.
        /// </summary>
        public int SelectedCharge
        {
            get => selectedCharge;
            set => this.RaiseAndSetIfChanged(ref selectedCharge, value);
        }

        /// <summary>
        /// Gets or sets the sequence selected by the user.
        /// </summary>
        public string SequenceText
        {
            get => sequenceText;
            set => this.RaiseAndSetIfChanged(ref sequenceText, value);
        }

        /// <summary>
        /// Gets or sets the data set selected by the user.
        /// </summary>
        public DataSetViewModel SelectedDataSetViewModel
        {
            get => selectedDataSetViewModel;
            set => this.RaiseAndSetIfChanged(ref selectedDataSetViewModel, value);
        }

        /// <summary>
        /// Gets or sets the scorer factory used to score PRSMs on-the-fly.
        /// </summary>
        public ScorerFactory ScorerFactory { get; set; }

        /// <summary>
        /// Implementation of the CreatePRSM command.
        /// Creates a new protein-spectrum match for the selected sequence,
        /// charge, and scan number.
        /// </summary>
        private void CreatePrSmImplementation()
        {
            var sequenceReader = new SequenceReader();
            Sequence sequence = null;
            try
            {
                sequence = sequenceReader.Read(SequenceText);
                if (sequence == null)
                {
                    throw new FormatException("Invalid Sequence.");
                }
            }
            catch (FormatException e)
            {
                dialogService.MessageBox(e.Message);
                return;
            }
            finally
            {
                if (sequence == null)
                {
                    sequence = new Sequence(new List<AminoAcid>());
                }
            }

            if (sequence.Count > 0 && SelectedCharge == 0)
            {
                dialogService.MessageBox("Invalid Charge state.");
                    return;
            }

            if (sequence.Count == 0 && SelectedScan < 0)
            {
                dialogService.MessageBox("Invalid scan number.");
                return;
            }

            var lcms = SelectedDataSetViewModel.LcMs;

            var score = -1.0;
            if (lcms != null && SelectedScan > 0 && ScorerFactory != null && sequence.Count > 0)
            {

                if (lcms.GetSpectrum(SelectedScan) is ProductSpectrum spectrum)
                {
                    var scorer = ScorerFactory.GetScorer(spectrum);
                    score = IonUtils.ScoreSequence(scorer, sequence);
                }
            }

            var rawFileName = SelectedDataSetViewModel.Title;
            var prsm = new PrSm
            {
                Heavy = false,
                RawFileName = rawFileName,
                ProteinName = string.Empty,
                ProteinDesc = string.Empty,
                Scan = Math.Min(Math.Max(SelectedScan, 0), lcms?.MaxLcScan ?? 1),
                LcMs = lcms,
                Charge = SelectedCharge,
                Sequence = sequence,
                SequenceText = SequenceText,
                Score = score,
            };
            SelectedPrSm = prsm;
        }

        /// <summary>
        /// Insert modifications that are registered in LCMSSpectator as being static for a certain residue.
        /// If mass shifts are present in the sequence, the modifications are inserted as mass shifts.
        /// Otherwise, the modifications are inserted by modification name.
        /// </summary>
        private void InsertStaticModifications()
        {
            if (SequenceText == string.Empty || IcParameters.Instance.SearchModifications.Count == 0)
            {
                return;
            }

            if (SequenceText.Contains("+"))
            {
                InsertStaticMsgfPlusModifications();
            }
            else
            {
                InsertStaticLcmsSpectatorModifications();
            }
        }

        /// <summary>
        /// Inserts static modifications into the sequence as mass shifts.
        /// </summary>
        private void InsertStaticMsgfPlusModifications()
        {
            const string Pattern = @"[A-Z](\+[0-9]+\.[0-9]+)*";

            var matches = Regex.Matches(SequenceText, Pattern);

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
                        var modStr = string.Format("+{0}", Math.Round(searchModification.Modification.Mass, 3));
                        if (!matchStr.Contains(modStr))
                        {
                            matchStr += modStr;
                        }
                    }
                }

                newSequence.Add(matchStr);
            }

            SequenceText = string.Empty;
            foreach (var aa in newSequence)
            {
                SequenceText += aa;
            }
        }

        /// <summary>
        /// Inserts static modifications into the sequence by modification name.
        /// </summary>
        private void InsertStaticLcmsSpectatorModifications()
        {
            const string Pattern = @"[A-Z](\[[A-Z][a-z]+\])*";

            var matches = Regex.Matches(SequenceText, Pattern);

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
                        var modStr = string.Format("[{0}]", searchModification.Modification);
                        if (!matchStr.Contains(modStr))
                        {
                            matchStr += modStr;
                        }
                    }
                }

                newSequence.Add(matchStr);
            }

            SequenceText = string.Empty;
            foreach (var aa in newSequence)
            {
                SequenceText += aa;
            }
        }
    }
}
