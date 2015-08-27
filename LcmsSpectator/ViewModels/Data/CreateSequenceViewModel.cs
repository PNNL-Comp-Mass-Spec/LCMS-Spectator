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

using LcmsSpectator.Models.DTO;
using LcmsSpectator.ViewModels.Dataset;

namespace LcmsSpectator.ViewModels.Data
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Linq;
    using System.Text.RegularExpressions;

    using InformedProteomics.Backend.Data.Enum;
    using InformedProteomics.Backend.Data.Sequence;
    using InformedProteomics.Backend.Data.Spectrometry;
    using InformedProteomics.Backend.MassSpecData;

    using LcmsSpectator.Config;
    using LcmsSpectator.DialogServices;
    using LcmsSpectator.Models;
    using LcmsSpectator.Readers.SequenceReaders;
    using LcmsSpectator.Utils;

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
        private DatasetViewModel selectedDataSetViewModel;

        /// <summary>
        /// Initializes a new instance of the CreateSequenceViewModel class. 
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        public CreateSequenceViewModel(IDialogService dialogService)
        {
            this.Targets = new ReactiveList<Target>();
            this.dialogService = dialogService;
            this.SequenceText = string.Empty;

            var createPrSmCommand = ReactiveCommand.Create();
            createPrSmCommand.Subscribe(_ => this.CreatePrSmImplementation());
            this.CreatePrSmCommand = createPrSmCommand;

            this.CreateAndAddPrSmCommand = ReactiveCommand.Create();
            this.CreateAndAddPrSmCommand.Subscribe(_ => this.CreatePrSmImplementation());

            var insertStaticModificationsCommand = ReactiveCommand.Create();
            insertStaticModificationsCommand.Subscribe(_ => this.InsertStaticModifications());
            this.InsertStaticModificationsCommand = insertStaticModificationsCommand;

            this.SelectedCharge = 2;
            this.SelectedScan = 0;

            // When PrSm changes, update scan, sequence text, and charge
            this.WhenAnyValue(x => x.SelectedPrSm)
                .Where(prsm => prsm != null)
                .Subscribe(prsm =>
            {
                this.SelectedScan = prsm.Scan;
                this.SequenceText = prsm.SequenceText;
                this.SelectedCharge = prsm.Charge;
            });

            this.Modifications = new ReactiveList<Modification>(SingletonProjectManager.Instance.ProjectInfo.ModificationSettings.RegisteredModifications);
        }

        /// <summary>
        /// Gets the list of target sequences and charges.
        /// </summary>
        public ReactiveList<Target> Targets { get; private set; }

        /// <summary>
        /// Gets a list of modifications registered with LCMSSpectator.
        /// </summary>
        public ReactiveList<Modification> Modifications { get; private set; }

        /// <summary>
        /// Gets or sets the modification selected for insertion into the sequence.
        /// </summary>
        public Modification SelectedModification { get; set; }

        /// <summary>
        /// Gets a command that when executed creates a new protein-spectrum match from the 
        /// selected sequence, charge, scan number, and data set.
        /// </summary>
        public IReactiveCommand CreatePrSmCommand { get; private set; }

        /// <summary>
        /// Gets a command that when executed creates a new protein-spectrum match from the 
        /// selected sequence, charge, scan number, and data set. It also signals the parent
        /// data set that it should be added to the Scan View.
        /// </summary>
        public ReactiveCommand<object> CreateAndAddPrSmCommand { get; private set; } 

        /// <summary>
        /// Gets a command that when executed inserts the modifications into the sequence
        /// that are marked as static for a certain residue.
        /// </summary>
        public IReactiveCommand InsertStaticModificationsCommand { get; private set; }

        /// <summary>
        /// Gets or sets the selected protein-spectrum match.
        /// </summary>
        public PrSm SelectedPrSm
        {
            get { return this.selectedPrSm; }
            set { this.RaiseAndSetIfChanged(ref this.selectedPrSm, value); }
        }

        /// <summary>
        /// Gets or sets the scan number selected by the user.
        /// </summary>
        public int SelectedScan
        {
            get { return this.selectedScan; }
            set { this.RaiseAndSetIfChanged(ref this.selectedScan, value); }
        }

        /// <summary>
        /// Gets or sets the charge selected by the users.
        /// </summary>
        public int SelectedCharge
        {
            get { return this.selectedCharge; }
            set { this.RaiseAndSetIfChanged(ref this.selectedCharge, value); }
        }

        /// <summary>
        /// Gets or sets the sequence selected by the user.
        /// </summary>
        public string SequenceText
        {
            get { return this.sequenceText; }
            set { this.RaiseAndSetIfChanged(ref this.sequenceText, value); }
        }

        /// <summary>
        /// Gets or sets the data set selected by the user.
        /// </summary>
        public DatasetViewModel SelectedDataSetViewModel
        {
            get { return this.selectedDataSetViewModel; }
            set { this.RaiseAndSetIfChanged(ref this.selectedDataSetViewModel, value); }
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
                sequence = sequenceReader.Read(this.SequenceText);
                if (sequence == null)
                {
                    throw new FormatException("Invalid Sequence.");
                }
            }
            catch (FormatException e)
            {
                this.dialogService.MessageBox(e.Message);
                return;
            }
            finally
            {
                if (sequence == null)
                {
                    sequence = new Sequence(new List<AminoAcid>());
                }
            }

            if (sequence.Count == 0 && this.SelectedCharge == 0)
            {
                this.dialogService.MessageBox("Invalid Charge.");
                    return;
            }

            if (sequence.Count == 0 && this.SelectedScan < 0)
            {
                this.dialogService.MessageBox("Invalid sequence and scan number.");
                return;
            }

            ILcMsRun lcms = this.SelectedDataSetViewModel.LcMs;
            
            double score = -1.0;
            if (lcms != null && this.SelectedScan > 0 && this.ScorerFactory != null && sequence.Count > 0)
            {
                var spectrum = lcms.GetSpectrum(this.SelectedScan) as ProductSpectrum;
                if (spectrum != null)
                {
                    var scorer = this.ScorerFactory.GetScorer(spectrum);
                    score = IonUtils.ScoreSequence(scorer, sequence);
                }
            }

            string rawFileName = this.SelectedDataSetViewModel.DatasetInfo.Name;
            var prsm = new PrSm
            {
                Heavy = false,
                RawFileName = rawFileName,
                ProteinName = string.Empty,
                ProteinDesc = string.Empty,
                Scan = Math.Min(Math.Max(this.SelectedScan, 0), lcms.MaxLcScan),
                LcMs = lcms,
                Charge = this.SelectedCharge,
                Sequence = sequence,
                SequenceText = this.SequenceText,
                Score = score,
            };
            this.SelectedPrSm = prsm;
        }

        /// <summary>
        /// Insert modifications that are registered in LCMSSpectator as being static for a certain residue.
        /// If mass shifts are present in the sequence, the modifications are inserted as mass shifts.
        /// Otherwise, the modifications are inserted by modification name.
        /// </summary>
        private void InsertStaticModifications()
        {
            var searchModifications =
                SingletonProjectManager.Instance.ProjectInfo.ModificationSettings.SearchModifications;
            if (this.SequenceText == string.Empty || searchModifications.Count == 0)
            {
                return;
            }

            if (this.SequenceText.Contains("+"))
            {
                this.InsertStaticMsgfPlusModifications();
            }
            else
            {
                this.InsertStaticLcmsSpectatorModifications();
            }
        }

        /// <summary>
        /// Inserts static modifications into the sequence as mass shifts.
        /// </summary>
        private void InsertStaticMsgfPlusModifications()
        {
            const string Pattern = @"[A-Z](\+[0-9]+\.[0-9]+)*";

            var matches = Regex.Matches(this.SequenceText, Pattern);

            var newSequence = new List<string>();

            foreach (Match match in matches)
            {
                var matchStr = match.Value;
                var residue = matchStr[0];
                var searchModifications =
                    SingletonProjectManager.Instance.ProjectInfo.ModificationSettings.SearchModifications;
                foreach (var searchModification in searchModifications)
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

            this.SequenceText = string.Empty;
            foreach (var aa in newSequence)
            {
                this.SequenceText += aa;
            }
        }

        /// <summary>
        /// Inserts static modifications into the sequence by modification name.
        /// </summary>
        private void InsertStaticLcmsSpectatorModifications()
        {
            const string Pattern = @"[A-Z](\[[A-Z][a-z]+\])*";

            var matches = Regex.Matches(this.SequenceText, Pattern);

            var newSequence = new List<string>();

            foreach (Match match in matches)
            {
                var matchStr = match.Value;
                var residue = matchStr[0];
                var searchModifications =
                    SingletonProjectManager.Instance.ProjectInfo.ModificationSettings.SearchModifications;
                foreach (var searchModification in searchModifications)
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

            this.SequenceText = string.Empty;
            foreach (var aa in newSequence)
            {
                this.SequenceText += aa;
            }
        }
    }
}
