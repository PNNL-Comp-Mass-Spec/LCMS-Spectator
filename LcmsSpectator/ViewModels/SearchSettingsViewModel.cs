// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SearchSettingsViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for configuration settings for running an MSPathFinder Database search.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using InformedProteomics.Backend.Database;
using InformedProteomics.Backend.Utils;

namespace LcmsSpectator.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using InformedProteomics.Backend.Data.Sequence;
    using InformedProteomics.Backend.Data.Spectrometry;
    using InformedProteomics.Backend.MassSpecData;
    using InformedProteomics.TopDown.Execution;
    using LcmsSpectator.Config;
    using LcmsSpectator.DialogServices;
    using LcmsSpectator.Models;
    using LcmsSpectator.Readers;
    using LcmsSpectator.Utils;
    using LcmsSpectator.ViewModels.Modifications;
    using ReactiveUI;

    /// <summary>
    /// View model for configuration settings for running an MSPathFinder Database search.
    /// </summary>
    public class SearchSettingsViewModel : ReactiveObject
    {
        /// <summary>
        /// The maximum possible tab index for the tab control.
        /// </summary>
        private const int MaxTabIndex = 3;

        /// <summary>
        /// The descriptions for the three search modes.
        /// </summary>
        private readonly string[] searchModeDescriptions =
        {
            "Multiple internal cleavages", "Single internal cleavage",
            "No internal cleavage"
        }; 

        /// <summary>
        /// Dialog service for opening dialogs from view model.
        /// </summary>
        private readonly IMainDialogService dialogService;

        /// <summary>
        /// The tab index for the tab control.
        /// </summary>
        private int tabIndex;

        /// <summary>
        /// A value indicating whether the search is currently running.
        /// </summary>
        private bool searchRunning;

        /// <summary>
        /// The path for the spectrum file.
        /// </summary>
        private string spectrumFilePath;

        /// <summary>
        /// The path for the FASTA database file.
        /// </summary>
        private string fastaDbFilePath;

        /// <summary>
        /// The path to the truncated FASTA database file.
        /// </summary>
        private string truncatedFastaDbFilePath;

        /// <summary>
        /// The path for the PROMEX feature file.
        /// </summary>
        private string featureFilePath;

        /// <summary>
        /// The path for the directory for output files.
        /// </summary>
        private string outputFilePath;

        /// <summary>
        /// The search mode selected from <see cref="SearchModes" />.
        /// </summary>
        private InternalCleavageType selectedSearchMode;

        /// <summary>
        /// The description for the selected search mode.
        /// </summary>
        private string searchModeDescription;

        /// <summary>
        /// The tolerance value for precursor ions.
        /// </summary>
        private double precursorIonToleranceValue;

        /// <summary>
        /// The tolerance unit for precursor ions.
        /// </summary>
        private ToleranceUnit precursorIonToleranceUnit;

        /// <summary>
        /// The tolerance value for product ions.
        /// </summary>
        private double productIonToleranceValue;

        /// <summary>
        /// The tolerance unit for product ions.
        /// </summary>
        private ToleranceUnit productIonToleranceUnit;

        /// <summary>
        /// The minimum possible sequence length.
        /// </summary>
        private int minSequenceLength;

        /// <summary>
        /// The maximum possible sequence length.
        /// </summary>
        private int maxSequenceLength;

        /// <summary>
        /// The minimum possible precursor ion charge.
        /// </summary>
        private int minPrecursorIonCharge;

        /// <summary>
        /// The maximum possible precursor ion charge.
        /// </summary>
        private int maxPrecursorIonCharge;

        /// <summary>
        /// The minimum possible product ion charge.
        /// </summary>
        private int minProductIonCharge;

        /// <summary>
        /// The maximum possible product ion charge.
        /// </summary>
        private int maxProductIonCharge;

        /// <summary>
        /// The minimum possible sequence mass.
        /// </summary>
        private double minSequenceMass;

        /// <summary>
        /// The maximum possible sequence mass.
        /// </summary>
        private double maxSequenceMass;

        /// <summary>
        /// The maximum dynamic modifications for each sequence.
        /// </summary>
        private int maxDynamicModificationsPerSequence;

        /// <summary>
        /// The minimum MS/MS scan number to restrict the search to.
        /// </summary>
        private int minScanNumber;

        /// <summary>
        /// The maximum MS/MS scan number to restrict the search to.
        /// </summary>
        private int maxScanNumber;

        /// <summary>
        /// A value indicating whether application modifications were updated.
        /// </summary>
        private bool modificationsUpdated;

        /// <summary>
        /// The search sequence.
        /// </summary>
        private string selectedSequence;

        /// <summary>
        /// A list of proteins containing the selected search sequence.
        /// </summary>
        private FastaEntry[] sequenceProteins;

        /// <summary>
        /// A value indicating whether the NTerminus of this sequence should be fixed,
        /// or if the suffix from the FASTA entry should be used.
        /// </summary>
        private bool fixedNTerm;

        /// <summary>
        /// A value indicating whether the CTerminus of this sequence should be fixed,
        /// or if the suffix from the FASTA entry should be used.
        /// </summary>
        private bool fixedCTerm;

        /// <summary>
        /// A value indicating whether this search should use a FASTA entry.
        /// </summary>
        private bool fromFastaEntry;

        /// <summary>
        /// A value indicating whether this search should use a specific search sequence.
        /// </summary>
        private bool fromSequence;

        /// <summary>
        /// The maximum number of Protein-Spectrum matches found per spectrum.
        /// </summary>
        private int numMatchesPerSpectrum;

        /// <summary>
        /// The search progress.
        /// </summary>
        private double searchProgressPercent;

        /// <summary>
        /// The status message for the search progress.
        /// </summary>
        private string searchProgressStatus;

        /// <summary>
        /// A task for running an MSPathFinder database search;
        /// </summary>
        private Task runSearchTask;

        /// <summary>
        /// A cancellation token for cancelling the <see cref="runSearchTask" />.
        /// </summary>
        private CancellationTokenSource runSearchCancellationToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchSettingsViewModel"/> class. 
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        public SearchSettingsViewModel(IMainDialogService dialogService)
        {
            this.dialogService = dialogService;
            this.SearchModes = new[] { InternalCleavageType.MultipleInternalCleavages, InternalCleavageType.SingleInternalCleavage, InternalCleavageType.NoInternalCleavage };
            this.ToleranceUnits = new[] { ToleranceUnit.Ppm, ToleranceUnit.Th };
            this.SelectedSequence = string.Empty;
            this.FastaEntries = new ReactiveList<FastaEntry>();
            this.SequenceProteins = new FastaEntry[0];

            this.SearchRunning = false;

            this.FromFastaEntry = true;
            this.FromSequence = false;

            // Browse Spectrum Files Command
            var browseRawFilesCommand = ReactiveCommand.Create();
            browseRawFilesCommand.Subscribe(_ => this.BrowseSpectrumFilesImplementation());
            this.BrowseSpectrumFilesCommand = browseRawFilesCommand;

            // Browse Feature Files Command
            var browseFeatureFilesCommand = ReactiveCommand.Create();
            browseFeatureFilesCommand.Subscribe(_ => this.BrowseFeatureFilesImplementation());
            this.BrowseFeatureFilesCommand = browseFeatureFilesCommand;

            // Browse Fasta DB Files Command
            var browseFastaDbFilesCommand = ReactiveCommand.Create();
            browseFastaDbFilesCommand.Subscribe(_ => this.BrowseFastaDbFilesImplementation());
            this.BrowseFastaDbFilesCommand = browseFastaDbFilesCommand;

            // Browse Output Directories Command
            var browseOutputDirectoriesCommand = ReactiveCommand.Create();
            browseOutputDirectoriesCommand.Subscribe(_ => this.BrowseOutputDirectoriesImplementation());
            this.BrowseOutputDirectoriesCommand = browseOutputDirectoriesCommand;

            // Select All Proteins Command
            var selectAllProteinsCommand = ReactiveCommand.Create(this.FastaEntries.WhenAnyValue(x => x.Count).Select(count => count > 0));
            selectAllProteinsCommand.Subscribe(_ => this.SelectProteinsImplementation(true));
            this.SelectAllProteinsCommand = selectAllProteinsCommand;

            // Select No Proteins Command
            var selectNoProteins = ReactiveCommand.Create(this.FastaEntries.WhenAnyValue(x => x.Count).Select(count => count > 0));
            selectNoProteins.Subscribe(_ => this.SelectProteinsImplementation(false));
            this.SelectNoProteinsCommand = selectNoProteins;

            // Manage Modifications Command
            var manageModificationsCommand = ReactiveCommand.Create();
            manageModificationsCommand.Subscribe(_ => this.ManageModificationsImplementation());
            this.ManageModificationsCommand = manageModificationsCommand;

            // Add Modifications Command
            var addModificationCommand = ReactiveCommand.Create();
            addModificationCommand.Subscribe(_ => this.AddModificationImplementation());
            this.AddModificationCommand = addModificationCommand;

            // Run Command - Disabled when there is no SpectrumFilePath, FastaDbFilePath, or OutputFilePath selected
            this.RunCommand = ReactiveCommand.CreateAsyncTask(
                                                              this.WhenAnyValue(
                                                                                x => x.SpectrumFilePath,
                                                                                x => x.FastaDbFilePath,
                                                                                x => x.OutputFilePath)
                                                                  .Select(x => !string.IsNullOrWhiteSpace(x.Item1) &&
                                                                               !string.IsNullOrWhiteSpace(x.Item2) &&
                                                                               !string.IsNullOrWhiteSpace(x.Item3)),
                                                              async _ => await this.RunImplementation());

            // Prev tab command
            var prevTabCommand = ReactiveCommand.Create(
                Observable.Merge(
                        new[]
                            {
                                this.WhenAnyValue(x => x.TabIndex).Select(tabIndex => tabIndex > 0),
                                this.RunCommand.IsExecuting.Select(exec => !exec)
                            }));
            prevTabCommand.Subscribe(_ => this.TabIndex--);
            this.PrevTabCommand = prevTabCommand;

            // Next tab command
            var nextTabCommand = ReactiveCommand.Create(
                    Observable.Merge(
                        new[]
                            {
                                this.WhenAnyValue(x => x.TabIndex).Select(tabIndex => tabIndex < MaxTabIndex),
                                this.RunCommand.IsExecuting.Select(exec => !exec)
                            }));
            nextTabCommand.Subscribe(_ => this.TabIndex++);
            this.NextTabCommand = nextTabCommand;

            // Cancel Command
            var cancelCommand = ReactiveCommand.Create();
            cancelCommand.Subscribe(_ => this.CancelImplementation());
            this.CancelCommand = cancelCommand;

            // Default values
            this.SelectedSearchMode = InternalCleavageType.SingleInternalCleavage;
            this.MinSequenceLength = 21;
            this.MaxSequenceLength = 300;
            this.MinPrecursorIonCharge = 2;
            this.MaxPrecursorIonCharge = 30;
            this.MinProductIonCharge = 1;
            this.MaxProductIonCharge = 15;
            this.MinSequenceMass = 3000.0;
            this.MaxSequenceMass = 50000.0;
            this.PrecursorIonToleranceValue = 10;
            this.PrecursorIonToleranceUnit = ToleranceUnit.Ppm;
            this.ProductIonToleranceValue = 10;
            this.PrecursorIonToleranceUnit = ToleranceUnit.Ppm;
            this.minScanNumber = 0;
            this.maxScanNumber = 0;
            this.maxDynamicModificationsPerSequence = 0;
            this.FixedNTerm = true;
            this.FixedCTerm = true;
            this.NumMatchesPerSpectrum = 1;

            // When search mode is selected, display correct search mode description
            this.WhenAnyValue(x => x.SelectedSearchMode)
                .Subscribe(searchMode => this.SearchModeDescription = this.searchModeDescriptions[(int)searchMode]);

            // When Spectrum file path is selected, use its directory for the output path by default if a output path
            // has not already been selected.
            this.WhenAnyValue(x => x.SpectrumFilePath)
                .Where(_ => string.IsNullOrWhiteSpace(this.OutputFilePath))
                .Select(Path.GetDirectoryName)
                .Subscribe(specDir => this.OutputFilePath = specDir);

            this.SearchModifications = new ReactiveList<SearchModificationViewModel> { ChangeTrackingEnabled = true };

            this.WhenAnyValue(x => x.FastaDbFilePath).Subscribe(_ => this.LoadFastaFile());

            this.WhenAnyValue(x => x.FastaEntries.Count, x => x.SelectedSequence)
                .Select(x => this.FastaEntries.Where(entry => entry.ProteinSequenceText.Contains(x.Item2)).ToArray())
                .Subscribe(entries => this.SequenceProteins = entries);

            // Remove search modification when its Remove property is set to true.
            this.SearchModifications.ItemChanged.Where(x => x.PropertyName == "Remove")
                .Select(x => x.Sender).Where(sender => sender.Remove)
                .Subscribe(searchMod => this.SearchModifications.Remove(searchMod));

            this.ModificationsUpdated = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchSettingsViewModel"/> class.
        /// Initializes from <see cref="MsPfParameters" />, MSPathFinder parameters.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from view model</param>
        /// <param name="mspfParameters">The MSPathFinder parameters.</param>
        public SearchSettingsViewModel(IMainDialogService dialogService, MsPfParameters mspfParameters) : this(dialogService)
        {
            if (mspfParameters == null)
            {
                return;
            }

            this.SearchModifications.AddRange(
               mspfParameters.Modifications.Select(
                    searchMod => new SearchModificationViewModel(this.dialogService) { SearchModification = searchMod }));
            this.MinSequenceLength = mspfParameters.MinSequenceLength;
            this.MaxSequenceLength = mspfParameters.MaxSequenceLength;
            this.MinSequenceMass = mspfParameters.MinSequenceMass;
            this.MaxSequenceMass = mspfParameters.MaxSequenceMass;
            this.MinPrecursorIonCharge = mspfParameters.MinPrecursorIonCharge;
            this.MaxPrecursorIonCharge = mspfParameters.MaxPrecursorIonCharge;
            this.MinProductIonCharge = mspfParameters.MinProductIonCharge;
            this.MaxProductIonCharge = mspfParameters.MaxPrecursorIonCharge;
            this.PrecursorIonToleranceValue = mspfParameters.PrecursorTolerancePpm.GetValue();
            this.ProductIonToleranceValue = mspfParameters.ProductIonTolerancePpm.GetValue();
            this.MaxDynamicModificationsPerSequence =
                mspfParameters.MaxDynamicModificationsPerSequence;
        }

        /// <summary>
        /// Event that is triggered when save or cancel are executed.
        /// </summary>
        public event EventHandler ReadyToClose;

        /// <summary>
        /// Gets a value indicating whether a valid modification has been selected.
        /// </summary>
        public bool Status { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the search is currently running.
        /// </summary>
        public bool SearchRunning
        {
            get { return this.searchRunning; }
            set { this.RaiseAndSetIfChanged(ref this.searchRunning, value); }
        }

        /// <summary>
        /// Gets a command that decrements the tab index.
        /// </summary>
        public IReactiveCommand PrevTabCommand { get; private set; }

        /// <summary>
        /// Gets a command that increments the tab index.
        /// </summary>
        public IReactiveCommand NextTabCommand { get; private set; }

        /// <summary>
        /// Gets a command that prompts the user for a raw file path.
        /// </summary>
        public IReactiveCommand BrowseSpectrumFilesCommand { get; private set; }

        /// <summary>
        /// Gets a command that prompts the user for a feature file path.
        /// </summary>
        public IReactiveCommand BrowseFeatureFilesCommand { get; private set; }

        /// <summary>
        /// Gets a command that prompts the user for a FASTA file path.
        /// </summary>
        public IReactiveCommand BrowseFastaDbFilesCommand { get; private set; }

        /// <summary>
        /// Gets a command that prompts user for an output directory.
        /// </summary>
        public IReactiveCommand BrowseOutputDirectoriesCommand { get; private set; }

        /// <summary>
        /// Gets a command that selects all proteins in the <see cref="FastaEntries" /> list.
        /// </summary>
        public IReactiveCommand SelectAllProteinsCommand { get; private set; }
        
        /// <summary>
        /// Gets a command that de-selects all proteins in the <see cref="FastaEntries" /> list.
        /// </summary>
        public IReactiveCommand SelectNoProteinsCommand { get; private set; }

        /// <summary>
        /// Gets a command that manages the modification registered with the application.
        /// </summary>
        public IReactiveCommand ManageModificationsCommand { get; private set; }

        /// <summary>
        /// Gets a command that adds a search modification.
        /// </summary>
        public IReactiveCommand AddModificationCommand { get; private set; }

        /// <summary>
        /// Gets a command that validates search settings and closes the window.
        /// </summary>
        public IReactiveCommand RunCommand { get; private set; }

        /// <summary>
        /// Gets a command that closes the window.
        /// </summary>
        public IReactiveCommand CancelCommand { get; private set; }

        /// <summary>
        /// Gets or sets the tab index for the tab control.
        /// </summary>
        public int TabIndex
        {
            get { return this.tabIndex; }
            set { this.RaiseAndSetIfChanged(ref this.tabIndex, value); }
        }

        /// <summary>
        /// Gets or sets the path for the spectrum file.
        /// </summary>
        public string SpectrumFilePath
        {
            get { return this.spectrumFilePath; }
            set { this.RaiseAndSetIfChanged(ref this.spectrumFilePath, value); }
        }

        /// <summary>
        /// Gets or sets the path for the FASTA database file.
        /// </summary>
        public string FastaDbFilePath
        {
            get { return this.fastaDbFilePath; }
            set { this.RaiseAndSetIfChanged(ref this.fastaDbFilePath, value); }
        }

        /// <summary>
        /// Gets or sets the path for the PROMEX feature file.
        /// </summary>
        public string FeatureFilePath
        {
            get { return this.featureFilePath; }
            set { this.RaiseAndSetIfChanged(ref this.featureFilePath, value); }
        }

        /// <summary>
        /// Gets or sets the path for the directory for output files.
        /// </summary>
        public string OutputFilePath
        {
            get { return this.outputFilePath; }
            set { this.RaiseAndSetIfChanged(ref this.outputFilePath, value); }
        }

        /// <summary>
        /// Gets the list of possible MSPathFinder search modes.
        /// </summary>
        public InternalCleavageType[] SearchModes { get; private set; }

        /// <summary>
        /// Gets the list of entries in the FASTA file.
        /// </summary>
        public ReactiveList<FastaEntry> FastaEntries { get; private set; }

        /// <summary>
        /// Gets the list of possible tolerance units.
        /// </summary>
        public ToleranceUnit[] ToleranceUnits { get; private set; }

        /// <summary>
        /// Gets or sets the search mode selected from <see cref="SearchModes" />.
        /// </summary>
        public InternalCleavageType SelectedSearchMode
        {
            get { return this.selectedSearchMode; }
            set { this.RaiseAndSetIfChanged(ref this.selectedSearchMode, value); }
        }

        /// <summary>
        /// Gets the description for the selected search mode.
        /// </summary>
        public string SearchModeDescription
        {
            get { return this.searchModeDescription; }
            private set { this.RaiseAndSetIfChanged(ref this.searchModeDescription, value); }
        }

        /// <summary>
        /// Gets or sets the tolerance value for precursor ions.
        /// </summary>
        public double PrecursorIonToleranceValue
        {
            get { return this.precursorIonToleranceValue; }
            set { this.RaiseAndSetIfChanged(ref this.precursorIonToleranceValue, value); }
        }

        /// <summary>
        /// Gets or sets the tolerance unit for precursor ions.
        /// </summary>
        public ToleranceUnit PrecursorIonToleranceUnit
        {
            get { return this.precursorIonToleranceUnit; }
            set { this.RaiseAndSetIfChanged(ref this.precursorIonToleranceUnit, value); }
        }

        /// <summary>
        /// Gets or sets the tolerance value for product ions.
        /// </summary>
        public double ProductIonToleranceValue
        {
            get { return this.productIonToleranceValue; }
            set { this.RaiseAndSetIfChanged(ref this.productIonToleranceValue, value); }
        }

        /// <summary>
        /// Gets or sets the tolerance unit for product ions.
        /// </summary>
        public ToleranceUnit ProductIonToleranceUnit
        {
            get { return this.productIonToleranceUnit; }
            set { this.RaiseAndSetIfChanged(ref this.productIonToleranceUnit, value); }
        }

        /// <summary>
        /// Gets or sets the minimum possible sequence length.
        /// </summary>
        public int MinSequenceLength
        {
            get { return this.minSequenceLength; }
            set { this.RaiseAndSetIfChanged(ref this.minSequenceLength, value); }
        }

        /// <summary>
        /// Gets or sets the maximum possible sequence length.
        /// </summary>
        public int MaxSequenceLength
        {
            get { return this.maxSequenceLength; }
            set { this.RaiseAndSetIfChanged(ref this.maxSequenceLength, value); }
        }

        /// <summary>
        /// Gets or sets the minimum possible precursor ion charge.
        /// </summary>
        public int MinPrecursorIonCharge
        {
            get { return this.minPrecursorIonCharge; }
            set { this.RaiseAndSetIfChanged(ref this.minPrecursorIonCharge, value); }   
        }

        /// <summary>
        /// Gets or sets the maximum possible precursor ion charge.
        /// </summary>
        public int MaxPrecursorIonCharge
        {
            get { return this.maxPrecursorIonCharge; }
            set { this.RaiseAndSetIfChanged(ref this.maxPrecursorIonCharge, value); }
        }

        /// <summary>
        /// Gets or sets the minimum possible product ion charge.
        /// </summary>
        public int MinProductIonCharge
        {
            get { return this.minProductIonCharge; }
            set { this.RaiseAndSetIfChanged(ref this.minProductIonCharge, value); }
        }

        /// <summary>
        /// Gets or sets the maximum possible product ion charge.
        /// </summary>
        public int MaxProductIonCharge
        {
            get { return this.maxProductIonCharge; }
            set { this.RaiseAndSetIfChanged(ref this.maxProductIonCharge, value); }
        }

        /// <summary>
        /// Gets or sets the minimum possible sequence mass.
        /// </summary>
        public double MinSequenceMass
        {
            get { return this.minSequenceMass; }
            set { this.RaiseAndSetIfChanged(ref this.minSequenceMass, value); }
        }

        /// <summary>
        /// Gets or sets the maximum possible sequence mass.
        /// </summary>
        public double MaxSequenceMass
        {
            get { return this.maxSequenceMass; }
            set { this.RaiseAndSetIfChanged(ref this.maxSequenceMass, value); }
        }

        /// <summary>
        /// Gets or sets the maximum dynamic modifications for each sequence.
        /// </summary>
        public int MaxDynamicModificationsPerSequence
        {
            get { return this.maxDynamicModificationsPerSequence; }
            set { this.RaiseAndSetIfChanged(ref this.maxDynamicModificationsPerSequence, value); }
        }

        /// <summary>
        /// Gets or sets the minimum MS/MS scan number to restrict the search to.
        /// </summary>
        public int MinScanNumber
        {
            get { return this.minScanNumber; }
            set { this.RaiseAndSetIfChanged(ref this.minScanNumber, value); }
        }

        /// <summary>
        /// Gets or sets the maximum MS/MS scan number to restrict the search to.
        /// </summary>
        public int MaxScanNumber
        {
            get { return this.maxScanNumber; }
            set { this.RaiseAndSetIfChanged(ref this.maxScanNumber, value); }
        }

        /// <summary>
        /// Gets the list of modifications to use in the search.
        /// </summary>
        public ReactiveList<SearchModificationViewModel> SearchModifications { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether application modifications were updated.
        /// </summary>
        public bool ModificationsUpdated
        {
            get { return this.modificationsUpdated; }
            set { this.RaiseAndSetIfChanged(ref this.modificationsUpdated, value); }
        }

        /// <summary>
        /// Gets or sets the search sequence.
        /// </summary>
        public string SelectedSequence
        {
            get { return this.selectedSequence; }
            set { this.RaiseAndSetIfChanged(ref this.selectedSequence, value); }
        }

        /// <summary>
        /// Gets a list of proteins containing the selected search sequence.
        /// </summary>
        public FastaEntry[] SequenceProteins
        {
            get { return this.sequenceProteins; }
            private set { this.RaiseAndSetIfChanged(ref this.sequenceProteins, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the NTerminus of this sequence should be fixed,
        /// or if the suffix from the FASTA entry should be used.
        /// </summary>
        public bool FixedNTerm
        {
            get { return this.fixedNTerm; }
            set { this.RaiseAndSetIfChanged(ref this.fixedNTerm, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the CTerminus of this sequence should be fixed,
        /// or if the suffix from the FASTA entry should be used.
        /// </summary>
        public bool FixedCTerm
        {
            get { return this.fixedCTerm; }
            set { this.RaiseAndSetIfChanged(ref this.fixedCTerm, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this search should use a FASTA entry.
        /// </summary>
        public bool FromFastaEntry
        {
            get { return this.fromFastaEntry; }
            set { this.RaiseAndSetIfChanged(ref this.fromFastaEntry, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this search should use a specific search sequence.
        /// </summary>
        public bool FromSequence
        {
            get { return this.fromSequence; }
            set { this.RaiseAndSetIfChanged(ref this.fromSequence, value); }
        }

        /// <summary>
        /// Gets or sets the maximum number of Protein-Spectrum matches found per spectrum.
        /// </summary>
        public int NumMatchesPerSpectrum
        {
            get { return this.numMatchesPerSpectrum; }
            set { this.RaiseAndSetIfChanged(ref this.numMatchesPerSpectrum, value); }
        }

        /// <summary>
        /// Gets or sets the search progress.
        /// </summary>
        public double SearchProgressPercent
        {
            get { return this.searchProgressPercent; }
            set { this.RaiseAndSetIfChanged(ref this.searchProgressPercent, value); }
        }

        /// <summary>
        /// The status message for the search progress.
        /// </summary>
        public string SearchProgressStatus
        {
            get { return this.searchProgressStatus; }
            set { this.RaiseAndSetIfChanged(ref this.searchProgressStatus, value); }
        }

        /// <summary>Get a launcher for TopDown MSPathFinder searches.</summary>
        /// <param name="ms2ScanNums">The MS/MS scan numbers to restrict search to.</param>
        /// <returns>The <see cref="IcTopDownLauncher"/>.</returns>
        public IcTopDownLauncher GetTopDownLauncher(IEnumerable<int> ms2ScanNums = null)
        {
            var searchModifications = this.SearchModifications.Select(searchModification => searchModification.SearchModification).ToList();
            var aminoAcidSet = new AminoAcidSet(searchModifications, this.maxDynamicModificationsPerSequence);
            return new IcTopDownLauncher(
                this.SpectrumFilePath,
                this.truncatedFastaDbFilePath,
                this.OutputFilePath,
                aminoAcidSet,
                this.FeatureFilePath)
            {
                MinSequenceLength = this.MinSequenceLength,
                MaxSequenceLength = this.MaxSequenceLength,
                MaxNumNTermCleavages = 1,
                MaxNumCTermCleavages = 0,
                MinPrecursorIonCharge = this.MinPrecursorIonCharge,
                MaxPrecursorIonCharge = this.MaxPrecursorIonCharge,
                MinProductIonCharge = this.MinProductIonCharge,
                MaxProductIonCharge = this.MaxProductIonCharge,
                MinSequenceMass = this.MinSequenceMass,
                MaxSequenceMass = this.MaxSequenceMass,
                PrecursorIonTolerancePpm = this.PrecursorIonToleranceValue,
                ProductIonTolerancePpm = this.ProductIonToleranceValue,
                RunTargetDecoyAnalysis = DatabaseSearchMode.Both,
                SearchMode = this.SelectedSearchMode,
                MaxNumThreads = 4,
                ScanNumbers = ms2ScanNums,
                NumMatchesPerSpectrum = this.NumMatchesPerSpectrum,
            };
        }

        /// <summary>
        /// Get final path to feature file.
        /// </summary>
        /// <returns>Path to feature file as a string.</returns>
        public string GetFeatureFilePath()
        {
            var dataSetName = Path.GetFileNameWithoutExtension(this.SpectrumFilePath);
            return string.IsNullOrWhiteSpace(this.FeatureFilePath) ? 
                   string.Format("{0}\\{1}.ms1ft", this.OutputFilePath, dataSetName) : 
                   this.FeatureFilePath;
        }

        /// <summary>
        /// Get final path to ID file.
        /// </summary>
        /// <returns>Path to ID file as a string.</returns>
        public string GetIdFilePath()
        {
            var dataSetName = Path.GetFileNameWithoutExtension(this.SpectrumFilePath);
            return string.Format("{0}\\{1}_IcTda.tsv", this.OutputFilePath, dataSetName);
        }

        /// <summary>
        /// Reads the proteins from the FASTA file.
        /// </summary>
        private void LoadFastaFile()
        {
            this.FastaEntries.Clear();

            if (!string.IsNullOrEmpty(this.FastaDbFilePath) && File.Exists(this.FastaDbFilePath))
            {
                try
                {
                    this.FastaEntries.AddRange(FastaReaderWriter.ReadFastaFile(this.FastaDbFilePath));
                }
                catch (FormatException e)
                {
                    this.dialogService.ExceptionAlert(e);
                    this.FastaEntries.Clear();
                }
            }
        }

        /// <summary>
        /// Implementation for <see cref="BrowseSpectrumFilesCommand"/>.
        /// Prompts the user for a raw file path.
        /// </summary>
        private void BrowseSpectrumFilesImplementation()
        {
            var path = this.dialogService.OpenFile(".raw", FileConstants.RawFileFormatString);
            if (!string.IsNullOrWhiteSpace(path))
            {
                this.SpectrumFilePath = path;
            }
        }

        /// <summary>
        /// Implementation for <see cref="BrowseFeatureFilesCommand"/>.
        /// Prompts the user for a feature file path.
        /// </summary>
        private void BrowseFeatureFilesImplementation()
        {
            var path = this.dialogService.OpenFile(".ms1ft", FileConstants.FeatureFileFormatString);
            if (!string.IsNullOrWhiteSpace(path))
            {
                this.FeatureFilePath = path;
            }
        }

        /// <summary>
        /// Implementation for <see cref="BrowseFastaDbFilesCommand"/>.
        /// Prompts the user for a FASTA file path.
        /// </summary>
        private void BrowseFastaDbFilesImplementation()
        {
            var path = this.dialogService.OpenFile(".fasta", FileConstants.FastaFileFormatString);
            if (!string.IsNullOrWhiteSpace(path))
            {
                this.FastaDbFilePath = path;
            }
        }

        /// <summary>
        /// Implementation for <see cref="BrowseOutputDirectoriesCommand" />.
        /// Prompts user for an output directory.
        /// </summary>
        private void BrowseOutputDirectoriesImplementation()
        {
            var path = this.dialogService.OpenFolder();
            if (!string.IsNullOrWhiteSpace(path))
            {
                this.OutputFilePath = path;
            }
        }

        /// <summary>
        /// Implementation for <see cref="AddModificationCommand"/>.
        /// Adds a search modification.
        /// </summary>
        private void AddModificationImplementation()
        {
            this.SearchModifications.Add(new SearchModificationViewModel(this.dialogService));
        }

        /// <summary>
        /// Implementation for <see cref="SelectAllProteinsCommand" /> and
        /// <see cref="SelectNoProteinsCommand" />.
        /// Selects all or none of the proteins in the <see cref="FastaEntries" /> list.
        /// </summary>
        /// <param name="selected">
        /// A value indicating whether the proteins should be selected or de-selected.
        /// </param>
        private void SelectProteinsImplementation(bool selected)
        {
            foreach (var entry in this.FastaEntries)
            {
                entry.Selected = selected;
            }
        }

        /// <summary>
        /// Implementation for <see cref="ManageModificationsCommand"/>.
        /// Gets or sets a command that manages the modification registered with the application.
        /// </summary>
        private void ManageModificationsImplementation()
        {
            var manageModificationsViewModel = new ManageModificationsViewModel(this.dialogService);
            manageModificationsViewModel.Modifications.AddRange(IcParameters.Instance.RegisteredModifications);
            this.dialogService.OpenManageModifications(manageModificationsViewModel);

            this.ModificationsUpdated = true;
        }

        /// <summary>
        /// Implementation for <see cref="RunCommand"/>.
        /// Gets a command that validates search settings and closes the window.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task RunImplementation()
        {
            this.SearchRunning = true;

            this.runSearchCancellationToken = new CancellationTokenSource();

            // Read spectrum file
            var lcms = await Task.Run(() => PbfLcMsRun.GetLcMsRun(this.SpectrumFilePath, 0, 0), this.runSearchCancellationToken.Token);
            
            // Get MS/MS scan numbers
            IEnumerable<int> ms2Scans = null;
            if (this.MaxScanNumber > 0 && (this.MaxScanNumber - this.MinScanNumber) >= 0)
            {
                var allMs2Scans = lcms.GetScanNumbers(2);
                ms2Scans = allMs2Scans.Where(scan => scan >= this.MinScanNumber && scan <= this.MaxScanNumber);   
            }
            
            // Create truncated FASTA
            this.truncatedFastaDbFilePath = this.CreateTruncatedFastaFile();
            
            // Progress updater
            this.SearchProgressPercent = 0.0;
            this.SearchProgressStatus = "Searching...";
            var progress = new Progress<ProgressData>(progressData =>
            {
                this.SearchProgressPercent = progressData.Percent;
                this.SearchProgressStatus = progressData.Status;
            });

            // Run Search
            var topDownLauncher = this.GetTopDownLauncher(ms2Scans);
            this.runSearchTask = Task.Run(
                                          () => topDownLauncher.RunSearch(
                                                                          IcParameters.Instance.IonCorrelationThreshold,
                                                                          this.runSearchCancellationToken.Token,
                                                                          progress),
                                                this.runSearchCancellationToken.Token);
            await this.runSearchTask;
            ////topDownLauncher.RunSearch(IcParameters.Instance.IonCorrelationThreshold);
            this.SearchRunning = false;

            this.runSearchCancellationToken = null;

            // Results delivered on close
            this.Status = true;
            if (this.ReadyToClose != null)
            {
                this.ReadyToClose(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Implementation for <see cref="CancelCommand"/>.
        /// Gets a command that closes the window.
        /// </summary>
        private void CancelImplementation()
        {
            if (this.SearchRunning)
            {
                if (this.dialogService.ConfirmationBox(
                    "Are you sure you would like to cancel the search?",
                    "Cancel Search"))
                {
                    if (this.runSearchCancellationToken != null)
                    {
                        this.runSearchCancellationToken.Cancel();
                    }

                    this.SearchRunning = false;   
                }

                return;
            }

            this.Status = false;
            if (this.ReadyToClose != null)
            {
                this.ReadyToClose(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Create a truncated FASTA file based on selected proteins.
        /// </summary>
        /// <returns>The path to the truncated FASTA database file.</returns>
        private string CreateTruncatedFastaFile()
        {
            var fastaFileName = Path.GetFileNameWithoutExtension(this.FastaDbFilePath);
            var filePath = string.Format("{0}\\{1}_truncated.fasta", this.OutputFilePath, fastaFileName);

            IEnumerable<FastaEntry> entries = new FastaEntry[0];

            if (this.FromFastaEntry)
            {
                entries = this.FastaEntries.Where(entry => entry.Selected);
            }
            else if (this.FromSequence)
            {
                var selectedEntries = this.SequenceProteins.Where(entry => entry.Selected).ToArray();
                if (this.FixedNTerm && this.FixedCTerm)
                {   // Just use the selected sequence for every protein.
                    entries = selectedEntries.Select(
                                entry =>
                                new FastaEntry
                                    {
                                        ProteinName = entry.ProteinName,
                                        ProteinDescription = entry.ProteinDescription,
                                        ProteinSequenceText = this.SelectedSequence,
                                        Selected = true
                                    });
                    entries = new List<FastaEntry> { entries.FirstOrDefault() };
                }
                else if (this.FixedNTerm)
                {
                    entries = from entry in selectedEntries 
                              let startIndex = entry.ProteinSequenceText.IndexOf(this.SelectedSequence, StringComparison.Ordinal) 
                              where startIndex > -1 
                              let sequence = entry.ProteinSequenceText.Substring(startIndex) 
                              select new FastaEntry
                                  {
                                      ProteinName = entry.ProteinName,
                                      ProteinDescription = entry.ProteinDescription,
                                      ProteinSequenceText = sequence
                                  };
                }
                else if (this.FixedCTerm)
                {
                    entries = from entry in selectedEntries
                              let startIndex = entry.ProteinSequenceText.IndexOf(this.SelectedSequence, StringComparison.Ordinal)
                              where startIndex > -1
                              let sequence = entry.ProteinSequenceText.Substring(0, startIndex + this.SelectedSequence.Length)
                              select new FastaEntry
                              {
                                  ProteinName = entry.ProteinName,
                                  ProteinDescription = entry.ProteinDescription,
                                  ProteinSequenceText = sequence
                              };
                }
                else
                {
                    entries = selectedEntries;
                }
            }

            Console.WriteLine(@"Creating truncated fasta file at: {0}", filePath);
            FastaReaderWriter.Write(entries, filePath);
            return filePath;
        }
    }
}
