// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SearchSettingsViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for configuration settings for running an MSPathFinder Database search.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.Database;
using InformedProteomics.Backend.MassSpecData;
using InformedProteomics.TopDown.Execution;
using LcmsSpectator.Config;
using LcmsSpectator.DialogServices;
using LcmsSpectator.Models;
using LcmsSpectator.Readers;
using LcmsSpectator.Utils;
using LcmsSpectator.ViewModels.Modifications;
using ReactiveUI;
using MsPfParameters = LcmsSpectator.Models.MsPfParameters;

namespace LcmsSpectator.ViewModels
{
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
        /// Default constructor to support WPF design-time use
        /// </summary>
        [Obsolete("For WPF Design-time use only.", true)]
        public SearchSettingsViewModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchSettingsViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        public SearchSettingsViewModel(IMainDialogService dialogService)
        {
            this.dialogService = dialogService;
            SearchModes = new[] { InternalCleavageType.MultipleInternalCleavages, InternalCleavageType.SingleInternalCleavage, InternalCleavageType.NoInternalCleavage };
            ToleranceUnits = new[] { ToleranceUnit.Ppm, ToleranceUnit.Mz };
            SelectedSequence = string.Empty;
            FastaEntries = new ReactiveList<FastaEntry>();
            SequenceProteins = new FastaEntry[0];

            SearchRunning = false;

            FromFastaEntry = true;
            FromSequence = false;

            // Browse Spectrum Files Command
            BrowseSpectrumFilesCommand = ReactiveCommand.Create(BrowseSpectrumFilesImplementation);

            // Browse Feature Files Command
            BrowseFeatureFilesCommand = ReactiveCommand.Create(BrowseFeatureFilesImplementation);

            // Browse Fasta DB Files Command
            BrowseFastaDbFilesCommand = ReactiveCommand.Create(BrowseFastaDbFilesImplementation);

            // Browse Output Directories Command
            BrowseOutputDirectoriesCommand = ReactiveCommand.Create(BrowseOutputDirectoriesImplementation);

            // Select All Proteins Command
            SelectAllProteinsCommand = ReactiveCommand.Create(() => SelectProteinsImplementation(true), FastaEntries.WhenAnyValue(x => x.Count).Select(count => count > 0));

            // Select No Proteins Command
            SelectNoProteinsCommand = ReactiveCommand.Create(() => SelectProteinsImplementation(false), FastaEntries.WhenAnyValue(x => x.Count).Select(count => count > 0));

            // Manage Modifications Command
            ManageModificationsCommand = ReactiveCommand.Create(ManageModificationsImplementation);

            // Add Modifications Command
            AddModificationCommand = ReactiveCommand.Create(AddModificationImplementation);

            // Run Command - Disabled when there is no SpectrumFilePath, FastaDbFilePath, or OutputFilePath selected
            RunCommand = ReactiveCommand.CreateFromTask(async _ => await RunImplementation(),
                                                        this.WhenAnyValue(
                                                                          x => x.SpectrumFilePath,
                                                                          x => x.FastaDbFilePath,
                                                                          x => x.OutputFilePath)
                                                            .Select(x => !string.IsNullOrWhiteSpace(x.Item1) &&
                                                                         !string.IsNullOrWhiteSpace(x.Item2) &&
                                                                         !string.IsNullOrWhiteSpace(x.Item3))
                                                        );

            // Prev tab command
            PrevTabCommand = ReactiveCommand.Create(() => TabIndex--,
                Observable.Merge(
                        new[]
                            {
                                this.WhenAnyValue(x => x.TabIndex).Select(tabIndex => tabIndex > 0),
                                RunCommand.IsExecuting.Select(exec => !exec)
                            }));

            // Next tab command
            NextTabCommand = ReactiveCommand.Create(() =>TabIndex++,
                    Observable.Merge(
                        new[]
                            {
                                this.WhenAnyValue(x => x.TabIndex).Select(tabIndex => tabIndex < MaxTabIndex),
                                RunCommand.IsExecuting.Select(exec => !exec)
                            }));

            // Cancel Command
            CancelCommand = ReactiveCommand.Create(CancelImplementation);

            // Default values
            SelectedSearchMode = InternalCleavageType.SingleInternalCleavage;
            MinSequenceLength = 21;
            MaxSequenceLength = 300;
            MinPrecursorIonCharge = 2;
            MaxPrecursorIonCharge = 30;
            MinProductIonCharge = 1;
            MaxProductIonCharge = 15;
            MinSequenceMass = 3000.0;
            MaxSequenceMass = 50000.0;
            PrecursorIonToleranceValue = 10;
            PrecursorIonToleranceUnit = ToleranceUnit.Ppm;
            ProductIonToleranceValue = 10;
            PrecursorIonToleranceUnit = ToleranceUnit.Ppm;
            minScanNumber = 0;
            maxScanNumber = 0;
            maxDynamicModificationsPerSequence = 0;
            FixedNTerm = true;
            FixedCTerm = true;
            NumMatchesPerSpectrum = 1;

            // When search mode is selected, display correct search mode description
            this.WhenAnyValue(x => x.SelectedSearchMode)
                .Subscribe(searchMode => SearchModeDescription = searchModeDescriptions[(int)searchMode]);

            // When Spectrum file path is selected, use its directory for the output path by default if a output path
            // has not already been selected.
            this.WhenAnyValue(x => x.SpectrumFilePath)
                .Where(_ => string.IsNullOrWhiteSpace(OutputFilePath))
                .Select(Path.GetDirectoryName)
                .Subscribe(specDir => OutputFilePath = specDir);

            SearchModifications = new ReactiveList<SearchModificationViewModel> { ChangeTrackingEnabled = true };

            this.WhenAnyValue(x => x.FastaDbFilePath).Subscribe(_ => LoadFastaFile());

            this.WhenAnyValue(x => x.FastaEntries.Count, x => x.SelectedSequence)
                .Select(x => FastaEntries.Where(entry => entry.ProteinSequenceText.Contains(x.Item2)).ToArray())
                .Subscribe(entries => SequenceProteins = entries);

            // Remove search modification when its Remove property is set to true.
            SearchModifications.ItemChanged.Where(x => x.PropertyName == "Remove")
                .Select(x => x.Sender).Where(sender => sender.Remove)
                .Subscribe(searchMod => SearchModifications.Remove(searchMod));

            ModificationsUpdated = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchSettingsViewModel"/> class.
        /// Initializes from <see cref="Models.MsPfParameters" />, MSPathFinder parameters.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from view model</param>
        /// <param name="mspfParameters">The MSPathFinder parameters.</param>
        public SearchSettingsViewModel(IMainDialogService dialogService, MsPfParameters mspfParameters) : this(dialogService)
        {
            if (mspfParameters == null)
            {
                return;
            }

            SearchModifications.AddRange(
               mspfParameters.Modifications.Select(
                    searchMod => new SearchModificationViewModel(this.dialogService) { SearchModification = searchMod }));
            MinSequenceLength = mspfParameters.MinSequenceLength;
            MaxSequenceLength = mspfParameters.MaxSequenceLength;
            MinSequenceMass = mspfParameters.MinSequenceMass;
            MaxSequenceMass = mspfParameters.MaxSequenceMass;
            MinPrecursorIonCharge = mspfParameters.MinPrecursorIonCharge;
            MaxPrecursorIonCharge = mspfParameters.MaxPrecursorIonCharge;
            MinProductIonCharge = mspfParameters.MinProductIonCharge;
            MaxProductIonCharge = mspfParameters.MaxPrecursorIonCharge;
            PrecursorIonToleranceValue = mspfParameters.PrecursorTolerancePpm.GetValue();
            ProductIonToleranceValue = mspfParameters.ProductIonTolerancePpm.GetValue();
            MaxDynamicModificationsPerSequence =
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
            get => searchRunning;
            set => this.RaiseAndSetIfChanged(ref searchRunning, value);
        }

        /// <summary>
        /// Gets a command that decrements the tab index.
        /// </summary>
        public ReactiveCommand<Unit, int> PrevTabCommand { get; }

        /// <summary>
        /// Gets a command that increments the tab index.
        /// </summary>
        public ReactiveCommand<Unit, int> NextTabCommand { get; }

        /// <summary>
        /// Gets a command that prompts the user for a raw file path.
        /// </summary>
        public ReactiveCommand<Unit, Unit> BrowseSpectrumFilesCommand { get; }

        /// <summary>
        /// Gets a command that prompts the user for a feature file path.
        /// </summary>
        public ReactiveCommand<Unit, Unit> BrowseFeatureFilesCommand { get; }

        /// <summary>
        /// Gets a command that prompts the user for a FASTA file path.
        /// </summary>
        public ReactiveCommand<Unit, Unit> BrowseFastaDbFilesCommand { get; }

        /// <summary>
        /// Gets a command that prompts user for an output directory.
        /// </summary>
        public ReactiveCommand<Unit, Unit> BrowseOutputDirectoriesCommand { get; }

        /// <summary>
        /// Gets a command that selects all proteins in the <see cref="FastaEntries" /> list.
        /// </summary>
        public ReactiveCommand<Unit, Unit> SelectAllProteinsCommand { get; }

        /// <summary>
        /// Gets a command that de-selects all proteins in the <see cref="FastaEntries" /> list.
        /// </summary>
        public ReactiveCommand<Unit, Unit> SelectNoProteinsCommand { get; }

        /// <summary>
        /// Gets a command that manages the modification registered with the application.
        /// </summary>
        public ReactiveCommand<Unit, Unit> ManageModificationsCommand { get; }

        /// <summary>
        /// Gets a command that adds a search modification.
        /// </summary>
        public ReactiveCommand<Unit, Unit> AddModificationCommand { get; }

        /// <summary>
        /// Gets a command that validates search settings and closes the window.
        /// </summary>
        public ReactiveCommand<Unit, Unit> RunCommand { get; }

        /// <summary>
        /// Gets a command that closes the window.
        /// </summary>
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        /// <summary>
        /// Gets or sets the tab index for the tab control.
        /// </summary>
        public int TabIndex
        {
            get => tabIndex;
            set => this.RaiseAndSetIfChanged(ref tabIndex, value);
        }

        /// <summary>
        /// Gets or sets the path for the spectrum file.
        /// </summary>
        public string SpectrumFilePath
        {
            get => spectrumFilePath;
            set => this.RaiseAndSetIfChanged(ref spectrumFilePath, value);
        }

        /// <summary>
        /// Gets or sets the path for the FASTA database file.
        /// </summary>
        public string FastaDbFilePath
        {
            get => fastaDbFilePath;
            set => this.RaiseAndSetIfChanged(ref fastaDbFilePath, value);
        }

        /// <summary>
        /// Gets or sets the path for the PROMEX feature file.
        /// </summary>
        public string FeatureFilePath
        {
            get => featureFilePath;
            set => this.RaiseAndSetIfChanged(ref featureFilePath, value);
        }

        /// <summary>
        /// Gets or sets the path for the directory for output files.
        /// </summary>
        public string OutputFilePath
        {
            get => outputFilePath;
            set => this.RaiseAndSetIfChanged(ref outputFilePath, value);
        }

        /// <summary>
        /// Gets the list of possible MSPathFinder search modes.
        /// </summary>
        public InternalCleavageType[] SearchModes { get; }

        /// <summary>
        /// Gets the list of entries in the FASTA file.
        /// </summary>
        public ReactiveList<FastaEntry> FastaEntries { get; }

        /// <summary>
        /// Gets the list of possible tolerance units.
        /// </summary>
        public ToleranceUnit[] ToleranceUnits { get; }

        /// <summary>
        /// Gets or sets the search mode selected from <see cref="SearchModes" />.
        /// </summary>
        public InternalCleavageType SelectedSearchMode
        {
            get => selectedSearchMode;
            set => this.RaiseAndSetIfChanged(ref selectedSearchMode, value);
        }

        /// <summary>
        /// Gets the description for the selected search mode.
        /// </summary>
        public string SearchModeDescription
        {
            get => searchModeDescription;
            private set => this.RaiseAndSetIfChanged(ref searchModeDescription, value);
        }

        /// <summary>
        /// Gets or sets the tolerance value for precursor ions.
        /// </summary>
        public double PrecursorIonToleranceValue
        {
            get => precursorIonToleranceValue;
            set => this.RaiseAndSetIfChanged(ref precursorIonToleranceValue, value);
        }

        /// <summary>
        /// Gets or sets the tolerance unit for precursor ions.
        /// </summary>
        public ToleranceUnit PrecursorIonToleranceUnit
        {
            get => precursorIonToleranceUnit;
            set => this.RaiseAndSetIfChanged(ref precursorIonToleranceUnit, value);
        }

        /// <summary>
        /// Gets or sets the tolerance value for product ions.
        /// </summary>
        public double ProductIonToleranceValue
        {
            get => productIonToleranceValue;
            set => this.RaiseAndSetIfChanged(ref productIonToleranceValue, value);
        }

        /// <summary>
        /// Gets or sets the tolerance unit for product ions.
        /// </summary>
        public ToleranceUnit ProductIonToleranceUnit
        {
            get => productIonToleranceUnit;
            set => this.RaiseAndSetIfChanged(ref productIonToleranceUnit, value);
        }

        /// <summary>
        /// Gets or sets the minimum possible sequence length.
        /// </summary>
        public int MinSequenceLength
        {
            get => minSequenceLength;
            set => this.RaiseAndSetIfChanged(ref minSequenceLength, value);
        }

        /// <summary>
        /// Gets or sets the maximum possible sequence length.
        /// </summary>
        public int MaxSequenceLength
        {
            get => maxSequenceLength;
            set => this.RaiseAndSetIfChanged(ref maxSequenceLength, value);
        }

        /// <summary>
        /// Gets or sets the minimum possible precursor ion charge.
        /// </summary>
        public int MinPrecursorIonCharge
        {
            get => minPrecursorIonCharge;
            set => this.RaiseAndSetIfChanged(ref minPrecursorIonCharge, value);
        }

        /// <summary>
        /// Gets or sets the maximum possible precursor ion charge.
        /// </summary>
        public int MaxPrecursorIonCharge
        {
            get => maxPrecursorIonCharge;
            set => this.RaiseAndSetIfChanged(ref maxPrecursorIonCharge, value);
        }

        /// <summary>
        /// Gets or sets the minimum possible product ion charge.
        /// </summary>
        public int MinProductIonCharge
        {
            get => minProductIonCharge;
            set => this.RaiseAndSetIfChanged(ref minProductIonCharge, value);
        }

        /// <summary>
        /// Gets or sets the maximum possible product ion charge.
        /// </summary>
        public int MaxProductIonCharge
        {
            get => maxProductIonCharge;
            set => this.RaiseAndSetIfChanged(ref maxProductIonCharge, value);
        }

        /// <summary>
        /// Gets or sets the minimum possible sequence mass.
        /// </summary>
        public double MinSequenceMass
        {
            get => minSequenceMass;
            set => this.RaiseAndSetIfChanged(ref minSequenceMass, value);
        }

        /// <summary>
        /// Gets or sets the maximum possible sequence mass.
        /// </summary>
        public double MaxSequenceMass
        {
            get => maxSequenceMass;
            set => this.RaiseAndSetIfChanged(ref maxSequenceMass, value);
        }

        /// <summary>
        /// Gets or sets the maximum dynamic modifications for each sequence.
        /// </summary>
        public int MaxDynamicModificationsPerSequence
        {
            get => maxDynamicModificationsPerSequence;
            set => this.RaiseAndSetIfChanged(ref maxDynamicModificationsPerSequence, value);
        }

        /// <summary>
        /// Gets or sets the minimum MS/MS scan number to restrict the search to.
        /// </summary>
        public int MinScanNumber
        {
            get => minScanNumber;
            set => this.RaiseAndSetIfChanged(ref minScanNumber, value);
        }

        /// <summary>
        /// Gets or sets the maximum MS/MS scan number to restrict the search to.
        /// </summary>
        public int MaxScanNumber
        {
            get => maxScanNumber;
            set => this.RaiseAndSetIfChanged(ref maxScanNumber, value);
        }

        /// <summary>
        /// Gets the list of modifications to use in the search.
        /// </summary>
        public ReactiveList<SearchModificationViewModel> SearchModifications { get; }

        /// <summary>
        /// Gets or sets a value indicating whether application modifications were updated.
        /// </summary>
        public bool ModificationsUpdated
        {
            get => modificationsUpdated;
            set => this.RaiseAndSetIfChanged(ref modificationsUpdated, value);
        }

        /// <summary>
        /// Gets or sets the search sequence.
        /// </summary>
        public string SelectedSequence
        {
            get => selectedSequence;
            set => this.RaiseAndSetIfChanged(ref selectedSequence, value);
        }

        /// <summary>
        /// Gets a list of proteins containing the selected search sequence.
        /// </summary>
        public FastaEntry[] SequenceProteins
        {
            get => sequenceProteins;
            private set => this.RaiseAndSetIfChanged(ref sequenceProteins, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the NTerminus of this sequence should be fixed,
        /// or if the suffix from the FASTA entry should be used.
        /// </summary>
        public bool FixedNTerm
        {
            get => fixedNTerm;
            set => this.RaiseAndSetIfChanged(ref fixedNTerm, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the CTerminus of this sequence should be fixed,
        /// or if the suffix from the FASTA entry should be used.
        /// </summary>
        public bool FixedCTerm
        {
            get => fixedCTerm;
            set => this.RaiseAndSetIfChanged(ref fixedCTerm, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this search should use a FASTA entry.
        /// </summary>
        public bool FromFastaEntry
        {
            get => fromFastaEntry;
            set => this.RaiseAndSetIfChanged(ref fromFastaEntry, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this search should use a specific search sequence.
        /// </summary>
        public bool FromSequence
        {
            get => fromSequence;
            set => this.RaiseAndSetIfChanged(ref fromSequence, value);
        }

        /// <summary>
        /// Gets or sets the maximum number of Protein-Spectrum matches found per spectrum.
        /// </summary>
        public int NumMatchesPerSpectrum
        {
            get => numMatchesPerSpectrum;
            set => this.RaiseAndSetIfChanged(ref numMatchesPerSpectrum, value);
        }

        /// <summary>
        /// Gets or sets the search progress.
        /// </summary>
        public double SearchProgressPercent
        {
            get => searchProgressPercent;
            set => this.RaiseAndSetIfChanged(ref searchProgressPercent, value);
        }

        /// <summary>
        /// The status message for the search progress.
        /// </summary>
        public string SearchProgressStatus
        {
            get => searchProgressStatus;
            set => this.RaiseAndSetIfChanged(ref searchProgressStatus, value);
        }

        /// <summary>Get a launcher for TopDown MSPathFinder searches.</summary>
        /// <param name="ms2ScanNums">The MS/MS scan numbers to restrict search to.</param>
        /// <returns>The <see cref="InformedProteomics.TopDown.Execution.IcTopDownLauncher"/>.</returns>
        public IcTopDownLauncher GetTopDownLauncher(IEnumerable<int> ms2ScanNums = null)
        {
            var searchModifications = SearchModifications.Select(searchModification => searchModification.SearchModification).ToList();
            var aminoAcidSet = new AminoAcidSet(searchModifications, maxDynamicModificationsPerSequence);
            return
                new IcTopDownLauncher(
                    new InformedProteomics.TopDown.Execution.MsPfParameters
                        {
                            MinSequenceLength = MinSequenceLength,
                            MaxSequenceLength = MaxSequenceLength,
                            //MaxNumNTermCleavages = 1,
                            //MaxNumCTermCleavages = 0,
                            MinPrecursorIonCharge = MinPrecursorIonCharge,
                            MaxPrecursorIonCharge = MaxPrecursorIonCharge,
                            MinProductIonCharge = MinProductIonCharge,
                            MaxProductIonCharge = MaxProductIonCharge,
                            MinSequenceMass = MinSequenceMass,
                            MaxSequenceMass = MaxSequenceMass,
                           // PrecursorIonTolerancePpm = this.PrecursorIonToleranceValue,
                            //ProductIonTolerancePpm = this.ProductIonToleranceValue,
                            //RunTargetDecoyAnalysis = DatabaseSearchMode.Both,
                            //SearchMode = this.SelectedSearchMode,
                            //MaxNumThreads = 4,
                            //ScanNumbers = ms2ScanNums,
                            //NumMatchesPerSpectrum = this.NumMatchesPerSpectrum,
                        });
        }

        /// <summary>
        /// Get final path to feature file.
        /// </summary>
        /// <returns>Path to feature file as a string.</returns>
        public string GetFeatureFilePath()
        {
            if (string.IsNullOrWhiteSpace(SpectrumFilePath))
                return string.Empty;

            var dataSetName = Path.GetFileNameWithoutExtension(SpectrumFilePath);
            return string.IsNullOrWhiteSpace(FeatureFilePath) ?
                   string.Format("{0}\\{1}.ms1ft", OutputFilePath, dataSetName) :
                   FeatureFilePath;
        }

        /// <summary>
        /// Get final path to ID file.
        /// </summary>
        /// <returns>Path to ID file as a string.</returns>
        public string GetIdFilePath()
        {
            var dataSetName = Path.GetFileNameWithoutExtension(SpectrumFilePath);
            return string.Format("{0}\\{1}_IcTda.tsv", OutputFilePath, dataSetName);
        }

        /// <summary>
        /// Reads the proteins from the FASTA file.
        /// </summary>
        private void LoadFastaFile()
        {
            FastaEntries.Clear();

            if (!string.IsNullOrEmpty(FastaDbFilePath) && File.Exists(FastaDbFilePath))
            {
                try
                {
                    FastaEntries.AddRange(FastaReaderWriter.ReadFastaFile(FastaDbFilePath));
                }
                catch (FormatException e)
                {
                    dialogService.ExceptionAlert(e);
                    FastaEntries.Clear();
                }
            }
        }

        /// <summary>
        /// Implementation for <see cref="BrowseSpectrumFilesCommand"/>.
        /// Prompts the user for a raw file path.
        /// </summary>
        private void BrowseSpectrumFilesImplementation()
        {
            var path = dialogService.OpenFile(".raw", FileConstants.RawFileFormatString);
            if (!string.IsNullOrWhiteSpace(path))
            {
                SpectrumFilePath = path;
            }
        }

        /// <summary>
        /// Implementation for <see cref="BrowseFeatureFilesCommand"/>.
        /// Prompts the user for a feature file path.
        /// </summary>
        private void BrowseFeatureFilesImplementation()
        {
            var path = dialogService.OpenFile(".ms1ft", FileConstants.FeatureFileFormatString);
            if (!string.IsNullOrWhiteSpace(path))
            {
                FeatureFilePath = path;
            }
        }

        /// <summary>
        /// Implementation for <see cref="BrowseFastaDbFilesCommand"/>.
        /// Prompts the user for a FASTA file path.
        /// </summary>
        private void BrowseFastaDbFilesImplementation()
        {
            var path = dialogService.OpenFile(".fasta", FileConstants.FastaFileFormatString);
            if (!string.IsNullOrWhiteSpace(path))
            {
                FastaDbFilePath = path;
            }
        }

        /// <summary>
        /// Implementation for <see cref="BrowseOutputDirectoriesCommand" />.
        /// Prompts user for an output directory.
        /// </summary>
        private void BrowseOutputDirectoriesImplementation()
        {
            var path = dialogService.OpenFolder();
            if (!string.IsNullOrWhiteSpace(path))
            {
                OutputFilePath = path;
            }
        }

        /// <summary>
        /// Implementation for <see cref="AddModificationCommand"/>.
        /// Adds a search modification.
        /// </summary>
        private void AddModificationImplementation()
        {
            SearchModifications.Add(new SearchModificationViewModel(dialogService));
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
            foreach (var entry in FastaEntries)
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
            var manageModificationsViewModel = new ManageModificationsViewModel(dialogService);
            manageModificationsViewModel.Modifications.AddRange(IcParameters.Instance.RegisteredModifications);
            dialogService.OpenManageModifications(manageModificationsViewModel);

            ModificationsUpdated = true;
        }

        /// <summary>
        /// Implementation for <see cref="RunCommand"/>.
        /// Gets a command that validates search settings and closes the window.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task RunImplementation()
        {
            SearchRunning = true;

            runSearchCancellationToken = new CancellationTokenSource();

            // Read spectrum file
            var lcms = await Task.Run(() => PbfLcMsRun.GetLcMsRun(SpectrumFilePath, 0, 0), runSearchCancellationToken.Token);

            // Get MS/MS scan numbers
            IEnumerable<int> ms2Scans = null;
            if (MaxScanNumber > 0 && (MaxScanNumber - MinScanNumber) >= 0)
            {
                var allMs2Scans = lcms.GetScanNumbers(2);
                ms2Scans = allMs2Scans.Where(scan => scan >= MinScanNumber && scan <= MaxScanNumber);
            }

            // Create truncated FASTA
            truncatedFastaDbFilePath = CreateTruncatedFastaFile();

            // Progress updater
            SearchProgressPercent = 0.0;
            SearchProgressStatus = "Searching...";
            var progress = new Progress<PRISM.ProgressData>(progressData =>
            {
                SearchProgressPercent = progressData.Percent;
                SearchProgressStatus = progressData.Status;
            });

            // Run Search
            var topDownLauncher = GetTopDownLauncher(ms2Scans);
            runSearchTask = Task.Run(
                                          () => topDownLauncher.RunSearch(
                                                                          IcParameters.Instance.IonCorrelationThreshold,
                                                                          runSearchCancellationToken.Token,
                                                                          progress),
                                                runSearchCancellationToken.Token);
            await runSearchTask;
            ////topDownLauncher.RunSearch(IcParameters.Instance.IonCorrelationThreshold);
            SearchRunning = false;

            runSearchCancellationToken = null;

            // Results delivered on close
            Status = true;
            ReadyToClose?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Implementation for <see cref="CancelCommand"/>.
        /// Gets a command that closes the window.
        /// </summary>
        private void CancelImplementation()
        {
            if (SearchRunning)
            {
                if (dialogService.ConfirmationBox(
                    "Are you sure you would like to cancel the search?",
                    "Cancel Search"))
                {
                    runSearchCancellationToken?.Cancel();

                    SearchRunning = false;
                }

                return;
            }

            Status = false;
            ReadyToClose?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Create a truncated FASTA file based on selected proteins.
        /// </summary>
        /// <returns>The path to the truncated FASTA database file.</returns>
        private string CreateTruncatedFastaFile()
        {
            var fastaFileName = Path.GetFileNameWithoutExtension(FastaDbFilePath);
            var filePath = string.Format("{0}\\{1}_truncated.fasta", OutputFilePath, fastaFileName);

            IEnumerable<FastaEntry> entries = new FastaEntry[0];

            if (FromFastaEntry)
            {
                entries = FastaEntries.Where(entry => entry.Selected);
            }
            else if (FromSequence)
            {
                var selectedEntries = SequenceProteins.Where(entry => entry.Selected).ToArray();
                if (FixedNTerm && FixedCTerm)
                {   // Just use the selected sequence for every protein.
                    entries = selectedEntries.Select(
                                entry =>
                                new FastaEntry
                                    {
                                        ProteinName = entry.ProteinName,
                                        ProteinDescription = entry.ProteinDescription,
                                        ProteinSequenceText = SelectedSequence,
                                        Selected = true
                                    });
                    entries = new List<FastaEntry> { entries.FirstOrDefault() };
                }
                else if (FixedNTerm)
                {
                    entries = from entry in selectedEntries
                              let startIndex = entry.ProteinSequenceText.IndexOf(SelectedSequence, StringComparison.Ordinal)
                              where startIndex > -1
                              let sequence = entry.ProteinSequenceText.Substring(startIndex)
                              select new FastaEntry
                                  {
                                      ProteinName = entry.ProteinName,
                                      ProteinDescription = entry.ProteinDescription,
                                      ProteinSequenceText = sequence
                                  };
                }
                else if (FixedCTerm)
                {
                    entries = from entry in selectedEntries
                              let startIndex = entry.ProteinSequenceText.IndexOf(SelectedSequence, StringComparison.Ordinal)
                              where startIndex > -1
                              let sequence = entry.ProteinSequenceText.Substring(0, startIndex + SelectedSequence.Length)
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
