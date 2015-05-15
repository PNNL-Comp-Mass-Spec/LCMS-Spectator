// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SearchSettingsViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for configuration settings for running an MSPathFinder Database search.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using InformedProteomics.Backend.Data.Sequence;
    using InformedProteomics.Backend.Data.Spectrometry;
    using InformedProteomics.TopDown.Execution;

    using LcmsSpectator.Config;
    using LcmsSpectator.DialogServices;
    using LcmsSpectator.Utils;
    using LcmsSpectator.ViewModels.Modifications;
    using ReactiveUI;

    /// <summary>
    /// View model for configuration settings for running an MSPathFinder Database search.
    /// </summary>
    public class SearchSettingsViewModel : ReactiveObject
    {
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
        /// A cancellation token for cancelling the <see cref="runSearchTask" />.
        /// </summary>
        private readonly CancellationToken runSearchCancellationToken;

        /// <summary>
        /// The path for the spectrum file.
        /// </summary>
        private string spectrumFilePath;

        /// <summary>
        /// The path for the FASTA database file.
        /// </summary>
        private string fastaDbFilePath;

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
        private int selectedSearchMode;

        /// <summary>
        /// The description for the selected search mode.
        /// </summary>
        private string searchModeDescription;

        /// <summary>
        /// The tolerance value for precursor ions.
        /// </summary>
        private int precursorIonToleranceValue;

        /// <summary>
        /// The tolerance unit for precursor ions.
        /// </summary>
        private ToleranceUnit precursorIonToleranceUnit;

        /// <summary>
        /// The tolerance value for product ions.
        /// </summary>
        private int productIonToleranceValue;

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
        /// The minimum feature probability threshold.
        /// </summary>
        private double minFeatureProbability;

        /// <summary>
        /// The maximum dynamic modifications for each sequence.
        /// </summary>
        private int maxDynamicModificationsPerSequence;

        /// <summary>
        /// A value indicating whether application modifications were updated.
        /// </summary>
        private bool modificationsUpdated;

        /// <summary>
        /// A task for running an MSPathFinder database search;
        /// </summary>
        private Task runSearchTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchSettingsViewModel"/> class. 
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        public SearchSettingsViewModel(IMainDialogService dialogService)
        {
            this.dialogService = dialogService;

            this.runSearchCancellationToken = new CancellationToken();

            this.LoadingScreenViewModel = new LoadingScreenViewModel();

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

            // Manage Modifications Command
            var manageModificationsCommand = ReactiveCommand.Create();
            manageModificationsCommand.Subscribe(_ => this.ManageModificationsImplementation());
            this.ManageModificationsCommand = manageModificationsCommand;

            // Add Modifications Command
            var addModificationCommand = ReactiveCommand.Create();
            addModificationCommand.Subscribe(_ => this.AddModificationImplementation());
            this.AddModificationCommand = addModificationCommand;

            // Run Command
            this.RunCommand = ReactiveCommand.CreateAsyncTask(async _ => await this.RunImplementation());

            // Cancel Command
            var cancelCommand = ReactiveCommand.Create();
            cancelCommand.Subscribe(_ => this.CancelImplementation());
            this.CancelCommand = cancelCommand;

            this.SearchModes = new[] { 0, 1, 2 };

            this.ToleranceUnits = new[] { ToleranceUnit.Ppm, ToleranceUnit.Th };

            // Default values
            this.SelectedSearchMode = 2;
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
            this.MinFeatureProbability = 0.15;

            // When search mode is selected, display correct search mode description
            this.WhenAnyValue(x => x.SelectedSearchMode)
                .Subscribe(searchMode => this.SearchModeDescription = this.searchModeDescriptions[searchMode]);

            this.SearchModifications = new ReactiveList<SearchModificationViewModel> { ChangeTrackingEnabled = true };

            // Remove search modification when its Remove property is set to true.
            this.SearchModifications.ItemChanged.Where(x => x.PropertyName == "Remove")
                .Select(x => x.Sender).Where(sender => sender.Remove)
                .Subscribe(searchMod => this.SearchModifications.Remove(searchMod));

            this.ModificationsUpdated = false;
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
        /// Gets a view model for displaying animated loading screen text.
        /// </summary>
        public LoadingScreenViewModel LoadingScreenViewModel { get; private set; }

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
        public int[] SearchModes { get; private set; }

        /// <summary>
        /// Gets the list of possible tolerance units.
        /// </summary>
        public ToleranceUnit[] ToleranceUnits { get; private set; }

        /// <summary>
        /// Gets or sets the search mode selected from <see cref="SearchModes" />.
        /// </summary>
        public int SelectedSearchMode
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
        public int PrecursorIonToleranceValue
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
        public int ProductIonToleranceValue
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
        /// Gets or sets the minimum feature probability threshold.
        /// </summary>
        public double MinFeatureProbability
        {
            get { return this.minFeatureProbability; }
            set { this.RaiseAndSetIfChanged(ref this.minFeatureProbability, value); }
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
        /// Gets an <see cref="IcTopDownLauncher" /> based on the selected search settings.
        /// </summary>
        public IcTopDownLauncher IcTopDownLauncher
        {
            get
            {
                var searchModifications = this.SearchModifications.Select(searchModification => searchModification.SearchModification).ToList();
                var aminoAcidSet = new AminoAcidSet(searchModifications, this.maxDynamicModificationsPerSequence);
                return new IcTopDownLauncher(
                                             this.SpectrumFilePath,
                                             this.FastaDbFilePath,
                                             this.OutputFilePath,
                                             aminoAcidSet,
                                             this.MinSequenceLength,
                                             this.MaxSequenceLength,
                                             1,
                                             0,
                                             this.MinPrecursorIonCharge,
                                             this.MinPrecursorIonCharge,
                                             this.MinProductIonCharge,
                                             this.MinProductIonCharge,
                                             this.MinSequenceMass,
                                             this.MaxSequenceMass,
                                             this.PrecursorIonToleranceValue,
                                             this.ProductIonToleranceValue,
                                             true,
                                             this.SelectedSearchMode,
                                             this.FeatureFilePath,
                                             this.minFeatureProbability);
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
            this.Status = true;
            if (this.ReadyToClose != null)
            {
                var topDownLauncher = this.IcTopDownLauncher;
                this.runSearchTask = Task.Run(() => topDownLauncher.RunSearch(), this.runSearchCancellationToken);
                await this.runSearchTask;
                this.ReadyToClose(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Implementation for <see cref="CancelCommand"/>.
        /// Gets a command that closes the window.
        /// </summary>
        private void CancelImplementation()
        {
            this.Status = false;
            if (this.ReadyToClose != null)
            {
                this.ReadyToClose(this, EventArgs.Empty);
            }
        }
    }
}
