// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataSetViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Class representing a data set, containing a LCMSRun, identifications, and features.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels.Data
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using InformedProteomics.Backend.MassSpecData;

    using LcmsSpectator.Config;
    using LcmsSpectator.DialogServices;
    using LcmsSpectator.Models;
    using LcmsSpectator.Readers;
    using LcmsSpectator.Utils;
    using LcmsSpectator.ViewModels.Modifications;
    using LcmsSpectator.ViewModels.Plots;

    using ReactiveUI;

    /// <summary>
    /// Class representing a data set, containing a LCMSRun, identifications, and features.
    /// </summary>
    public class DataSetViewModel : ReactiveObject
    {
        /// <summary>
        /// Dialog service for opening dialogs from view model.
        /// </summary>
        private readonly IMainDialogService dialogService;

        /// <summary>
        /// Parsed MSPathFinder parameters (configuration for database search)
        /// </summary>
        private MsPfParameters mspfParameters;

        /// <summary>
        /// View model for fragment and precursor ion selection.
        /// </summary>
        private IonListViewModel ionListViewModel;

        /// <summary>
        /// View model for extracted ion chromatogram
        /// </summary>
        private XicViewModel xicViewModel;

        /// <summary>
        /// View model for spectrum plots (MS/MS, previous MS1, next MS1) 
        /// </summary>
        private SpectrumViewModel spectrumViewModel;

        /// <summary>
        /// View model for mass spec feature map plot
        /// </summary>
        private FeatureViewerViewModel featureMapViewModel;

        /// <summary>
        /// The title of this data set
        /// </summary>
        private string title;

        /// <summary>
        /// The path to the raw file for this data set
        /// </summary>
        private string rawFilePath;

        /// <summary>
        /// A value indicating whether or not this data set is ready to be closed?
        /// </summary>
        private bool readyToClose;

        /// <summary>
        /// The selected Protein-Spectrum-Match Identification
        /// </summary>
        private PrSm selectedPrSm;

        /// <summary>
        /// A value indicating whether an ID file has been opened for this data set.
        /// </summary>
        private bool idFileOpen;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSetViewModel"/> class. 
        /// </summary>
        /// <param name="dialogService">A dialog service for opening dialogs from the view model</param>
        public DataSetViewModel(IMainDialogService dialogService)
        {
            this.dialogService = dialogService;
            this.ReadyToClose = false;
            this.IdFileOpen = false;
            this.LoadingScreenViewModel = new LoadingScreenViewModel();
            this.SelectedPrSm = new PrSm();
            this.ScanViewModel = new ScanViewModel(dialogService, new List<PrSm>());
            this.IonTypeSelectorViewModel = new IonTypeSelectorViewModel(dialogService);
            this.CreateSequenceViewModel = new CreateSequenceViewModel(dialogService)
            {
                SelectedDataSetViewModel = this
            };

            this.CreateSequenceViewModel.CreateAndAddPrSmCommand.Subscribe(
                _ => this.ScanViewModel.Data.Add(this.CreateSequenceViewModel.SelectedPrSm));

            // Remove filter by raw file name from ScanViewModel filters
            this.ScanViewModel.Filters.Remove(this.ScanViewModel.Filters.FirstOrDefault(f => f.Name == "Raw File Name"));

            // When a PrSm is selected from the ScanViewModel, update the SelectedPrSm for this data set
            this.ScanViewModel.WhenAnyValue(x => x.SelectedPrSm).Where(prsm => prsm != null).Subscribe(x => this.SelectedPrSm = x);

            // When the scan number in the selected prsm changes, the selected scan in the xic plots should update
            this.WhenAnyValue(x => x.SelectedPrSm)
            .Where(_ => this.SelectedPrSm != null && this.SpectrumViewModel != null && this.XicViewModel != null)
            .Subscribe(prsm =>
            {
                this.SpectrumViewModel.UpdateSpectra(prsm.Scan, this.SelectedPrSm.PrecursorMz);
                this.XicViewModel.SetSelectedScan(prsm.Scan);
                this.XicViewModel.ZoomToScan(prsm.Scan);
            });

            var prsmObservable = this.WhenAnyValue(x => x.SelectedPrSm).Where(prsm => prsm != null);

            // When the prsm changes, the ion lists should be recalculated
            prsmObservable.Where(_ => this.IonListViewModel != null)
                .Subscribe(prsm => this.IonListViewModel.SelectedPrSm = prsm);

            // When the prsm changes, update activation method and selected charge in the ion type selector
            prsmObservable.Select(prsm => prsm.Ms2Spectrum).Where(ms2 => ms2 != null)
                          .Subscribe(ms2 => this.IonTypeSelectorViewModel.ActivationMethod = ms2.ActivationMethod);
            prsmObservable.Subscribe(prsm => this.IonTypeSelectorViewModel.SelectedCharge = prsm.Charge);

            // When the prsm changes, update the Scan View Model.
            prsmObservable.Subscribe(prsm => this.ScanViewModel.SelectedPrSm = prsm);

            // When the prsm updates, update the prsm in the sequence creator
            prsmObservable.Subscribe(prsm => this.CreateSequenceViewModel.SelectedPrSm = prsm);

            // When the prsm updates, update the feature map
            prsmObservable.Where(_ => this.FeatureMapViewModel != null).Subscribe(prsm => this.FeatureMapViewModel.SelectedPrSm = prsm);

            // When prsm updates, update sequence in spectrum plot view model
            prsmObservable.Where(prsm => prsm.Sequence != null && this.SpectrumViewModel != null)
                .Select(prsm => prsm.Sequence)
                .Subscribe(sequence => this.SpectrumViewModel.Ms2SpectrumViewModel.Sequence = sequence);

            // When prsm updates, subscribe to scan updates
            prsmObservable.Subscribe(prsm =>
            {
                prsm.WhenAnyValue(x => x.Scan, x => x.PrecursorMz)
                    .Where(x => x.Item1 > 0 && x.Item2 > 0 && this.SpectrumViewModel != null)
                    .Subscribe(x => this.SpectrumViewModel.UpdateSpectra(x.Item1, x.Item2));
                prsm.WhenAnyValue(x => x.Scan).Where(scan => scan > 0 && this.XicViewModel != null)
                    .Subscribe(scan => this.XicViewModel.SetSelectedScan(scan));
            });

            // When a new prsm is created by CreateSequenceViewModel, update SelectedPrSm
            this.CreateSequenceViewModel.WhenAnyValue(x => x.SelectedPrSm).Subscribe(prsm => this.SelectedPrSm = prsm);

            // When the ion types change, the ion lists should be recalculated.
            this.IonTypeSelectorViewModel.WhenAnyValue(x => x.IonTypes).Where(_ => this.IonListViewModel != null).Subscribe(ionTypes => this.IonListViewModel.IonTypes = ionTypes);

            // When IDs are filtered in the ScanViewModel, update feature map with new IDs
            this.ScanViewModel.WhenAnyValue(x => x.FilteredData).Where(_ => this.FeatureMapViewModel != null).Subscribe(data => this.FeatureMapViewModel.UpdateIds(data));

            // Toggle instrument data when ShowInstrumentData setting is changed.
            IcParameters.Instance.WhenAnyValue(x => x.ShowInstrumentData).Select(async x => await this.ScanViewModel.ToggleShowInstrumentDataAsync(x, (PbfLcMsRun)this.LcMs)).Subscribe();

            // When product ion tolerance or ion correlation threshold change, update scorer factory
            IcParameters.Instance.WhenAnyValue(x => x.ProductIonTolerancePpm, x => x.IonCorrelationThreshold)
                .Subscribe(
                x =>
                {
                    var scorer = new ScorerFactory(x.Item1, Constants.MinCharge, Constants.MaxCharge, x.Item2);
                    this.CreateSequenceViewModel.ScorerFactory = scorer;
                    this.ScanViewModel.ScorerFactory = scorer;
                });

            // When an ID file has been opened, turn on the unidentified scan filter
            this.WhenAnyValue(x => x.IdFileOpen)
                .Where(idFileOpen => idFileOpen)
                .Select(_ => this.ScanViewModel.Filters.FirstOrDefault(f => f.Name == "Hide Unidentified Scans"))
                .Where(f => f != null)
                .Subscribe(f => f.Selected = true);

            // Start MsPf Search Command
            var startMsPfSearchCommand = ReactiveCommand.Create();
            startMsPfSearchCommand.Subscribe(_ => this.StartMsPfSearchImplementation());
            this.StartMsPfSearchCommand = startMsPfSearchCommand;

            // Close command verifies that the user wants to close the dataset, then sets ReadyToClose to true if they are
            var closeCommand = ReactiveCommand.Create();
            closeCommand.Subscribe(_ =>
            {
                this.ReadyToClose =
                    dialogService.ConfirmationBox(
                        string.Format("Are you sure you would like to close {0}?", this.Title), string.Empty);
            });
            this.CloseCommand = closeCommand;
        }

        /// <summary>
        /// Gets ScanViewModel that contains a list of identifications for this data set.
        /// </summary>
        public ScanViewModel ScanViewModel { get; private set; }

        /// <summary>
        /// Gets the ion type selector.
        /// </summary>
        public IonTypeSelectorViewModel IonTypeSelectorViewModel { get; private set; }

        /// <summary>
        /// Gets the create sequence view model.
        /// </summary>
        public CreateSequenceViewModel CreateSequenceViewModel { get; private set; }

        /// <summary>
        /// Gets the loading screen view model.
        /// </summary>
        public LoadingScreenViewModel LoadingScreenViewModel { get; private set; }

        /// <summary>
        /// Gets a command that starts an MSPathFinder with this data set.
        /// </summary>
        public IReactiveCommand StartMsPfSearchCommand { get; private set; }

        /// <summary>
        /// Gets a command that is activated when the close button is clicked on a dataset.
        /// Initiates a close request for the main view model
        /// </summary>
        public IReactiveCommand CloseCommand { get; private set; }

        /// <summary>
        /// Gets the LCMSRun representing the raw file for this dataset.
        /// </summary>
        public ILcMsRun LcMs { get; private set; }

        /// <summary>
        /// Gets view model for fragment and precursor ion selection.
        /// </summary>
        public IonListViewModel IonListViewModel
        {
            get { return this.ionListViewModel; }
            private set { this.RaiseAndSetIfChanged(ref this.ionListViewModel, value); }
        }

        /// <summary>
        /// Gets or sets parsed MSPathFinder parameters (configuration for database search)
        /// </summary>
        public MsPfParameters MsPfParameters
        {
            get { return this.mspfParameters; }
            set { this.RaiseAndSetIfChanged(ref this.mspfParameters, value); }
        }

        /// <summary>
        /// Gets or sets the path to the FASTA database file associated with this data set. 
        /// </summary>
        public string FastaDbFilePath { get; set; }

        /// <summary>
        /// Gets view model for extracted ion chromatogram
        /// </summary>
        public XicViewModel XicViewModel
        {
            get { return this.xicViewModel; }
            private set { this.RaiseAndSetIfChanged(ref this.xicViewModel, value); }
        }

        /// <summary>
        /// Gets view model for spectrum plots (MS/MS, previous MS1, next MS1) 
        /// </summary>
        public SpectrumViewModel SpectrumViewModel
        {
            get { return this.spectrumViewModel; }
            private set { this.RaiseAndSetIfChanged(ref this.spectrumViewModel, value); }
        }

        /// <summary>
        /// Gets view model for mass spec feature map plot
        /// </summary>
        public FeatureViewerViewModel FeatureMapViewModel
        {
            get { return this.featureMapViewModel; }
            private set { this.RaiseAndSetIfChanged(ref this.featureMapViewModel, value); }
        }

        /// <summary>
        /// Gets the title of this data set
        /// </summary>
        public string Title
        {
            get { return this.title; }
            private set { this.RaiseAndSetIfChanged(ref this.title, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not this data set is ready to be closed?
        /// </summary>
        public bool ReadyToClose
        {
            get { return this.readyToClose; }
            set { this.RaiseAndSetIfChanged(ref this.readyToClose, value); }
        }

        /// <summary>
        /// Gets or sets the selected Protein-Spectrum-Match Identification
        /// </summary>
        public PrSm SelectedPrSm
        {
            get { return this.selectedPrSm; }
            set { this.RaiseAndSetIfChanged(ref this.selectedPrSm, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether an ID file has been opened for this data set.
        /// </summary>
        public bool IdFileOpen
        {
            get { return this.idFileOpen; }
            set { this.RaiseAndSetIfChanged(ref this.idFileOpen, value); }
        }

        /// <summary>
        /// Set the MSPathFinder parameters given a path to a ID file.
        /// </summary>
        /// <param name="idFilePath">The id file path.</param>
        public void SetMsPfParameters(string idFilePath)
        {
            try
            {
                this.MsPfParameters = MsPfParameters.ReadFromFile(idFilePath);
            }
            catch (FormatException)
            {
                this.dialogService.MessageBox("MsPathFinder Parameters are not properly formatted.");
            }
        }

        /// <summary>
        /// Initialize this data set by reading the raw file asynchronously and initializing child view models
        /// </summary>
        /// <param name="filePath">The raw File Path.</param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task InitializeAsync(string filePath)
        {
            this.LoadingScreenViewModel.IsLoading = true; // Show animated loading screen
            this.Title = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(filePath));
            this.rawFilePath = filePath;

            // load raw file
            var massSpecDataType = filePath.EndsWith(FileConstants.RawFileExtensions[0], true, CultureInfo.InvariantCulture) ? 
                                                        MassSpecDataType.XCaliburRun : MassSpecDataType.MzMLFile;
            this.LcMs = await Task.Run(() => PbfLcMsRun.GetLcMsRun(filePath, massSpecDataType, 0, 0));

            // Now that we have an LcMsRun, initialize viewmodels that require it
            this.XicViewModel = new XicViewModel(this.dialogService, this.LcMs);
            this.SpectrumViewModel = new SpectrumViewModel(this.dialogService, this.LcMs);
            this.IonListViewModel = new IonListViewModel(this.LcMs);
            this.FeatureMapViewModel = new FeatureViewerViewModel(this.dialogService, (LcMsRun)this.LcMs);
            this.InitializeWirings(); // Initialize the wirings for updating these view models

            // Create prsms for scan numbers (unidentified)
            await this.LoadScans();
            ////await this.ScanViewModel.ToggleShowInstrumentDataAsync(IcParameters.Instance.ShowInstrumentData, (PbfLcMsRun)this.LcMs);
            this.SelectedPrSm.LcMs = this.LcMs; // For the selected PrSm, we should always use the LcMsRun for this dataset.
            this.LoadingScreenViewModel.IsLoading = false; // Hide animated loading screen
        }

        /// <summary>
        /// Load all scans from a raw file and insert them into the scan view model.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        private Task LoadScans()
        {
            return Task.Run(() =>
            {
                var scans = this.LcMs.GetScanNumbers(1).ToList();
                scans.AddRange(this.LcMs.GetScanNumbers(2));
                scans.Sort();
                var prsmScans = scans.Select(scan => new PrSm
                {
                    Scan = scan,
                    RawFileName = this.Title,
                    LcMs = this.LcMs,
                    QValue = -1.0,
                    Score = double.NaN,
                });
                this.ScanViewModel.Data.AddRange(prsmScans);
            });
        }

        /// <summary>
        /// Initialize the connections between the view models.
        /// </summary>
        private void InitializeWirings()
        {
            // When the selected scan changes in the xic plots, the selected scan for the prsm should update
            this.XicViewModel.SelectedScanUpdated().Subscribe(scan => this.SelectedPrSm.Scan = scan);

            // When precursor view mode is selected in XicViewModel, ion lists should be recalculated.
            this.XicViewModel.WhenAnyValue(x => x.PrecursorViewMode, x => x.ShowHeavy)
                .Subscribe(x =>
                {
                    this.IonListViewModel.PrecursorViewMode = x.Item1;
                    this.IonListViewModel.ShowHeavy = x.Item2;
                });

            // When FragmentLabels or ChargePrecursorLabels change, update spectra
            this.IonListViewModel.WhenAnyValue(x => x.FragmentLabels, x => x.ChargePrecursorLabels)
            .Where(x => x.Item1 != null && x.Item2 != null && this.SelectedPrSm != null)
            .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
            .Subscribe(x =>
            {
                var preclabels = x.Item2.Where(l => l.IonType.Charge <= this.SelectedPrSm.Charge);
                var labels = new ReactiveList<LabeledIonViewModel>(x.Item1) { ChangeTrackingEnabled = true };
                labels.AddRange(preclabels);
                if (this.SpectrumViewModel.Ms2SpectrumViewModel.Ions != null)
                {
                    this.SpectrumViewModel.Ms2SpectrumViewModel.Ions.ChangeTrackingEnabled = false;   
                }

                this.SpectrumViewModel.Ms2SpectrumViewModel.Ions = labels;
                this.SpectrumViewModel.PrevMs1SpectrumViewModel.Ions = x.Item2;
                this.SpectrumViewModel.NextMs1SpectrumViewModel.Ions = x.Item2;
            });

            // When FragmentLabels or PrecursorLabels change, update XICs
            this.IonListViewModel.WhenAnyValue(x => x.FragmentLabels).Subscribe(labels => this.XicViewModel.FragmentPlotViewModel.Ions = labels);
            this.IonListViewModel.WhenAnyValue(x => x.HeavyFragmentLabels).Subscribe(labels => this.XicViewModel.HeavyFragmentPlotViewModel.Ions = labels);
            this.IonListViewModel.WhenAnyValue(x => x.PrecursorLabels).Where(labels => labels != null)
            .Subscribe(labels => this.XicViewModel.PrecursorPlotViewModel.Ions = labels);
            this.IonListViewModel.WhenAnyValue(x => x.HeavyPrecursorLabels).Where(labels => labels != null)
            .Subscribe(labels => this.XicViewModel.HeavyPrecursorPlotViewModel.Ions = labels);

            // When an ID is selected on FeatureMap, update selectedPrSm
            this.FeatureMapViewModel.WhenAnyValue(x => x.SelectedPrSm).Where(prsm => prsm != null).Subscribe(prsm => this.SelectedPrSm = prsm);
        }

        /// <summary>
        /// Implementation for <see cref="StartMsPfSearchCommand" />.
        /// Gets a command that starts an MSPathFinder with this data set.
        /// </summary>
        private void StartMsPfSearchImplementation()
        {
            var searchSettings = new SearchSettingsViewModel(this.dialogService)
            {
                SpectrumFilePath = this.rawFilePath,
                SelectedSearchMode = 1,
                FastaDbFilePath = this.FastaDbFilePath,
                OutputFilePath = string.Format("{0}\\{1}", Directory.GetCurrentDirectory(), this.Title),
                SelectedSequence = this.SelectedPrSm.Sequence.Aggregate(string.Empty, (current, aa) => current + aa.Residue)
            };

            // Set feature file path.
            if (this.FeatureMapViewModel != null)
            {
                searchSettings.FeatureFilePath = this.FeatureMapViewModel.FeatureFilePath;
            }

            // Select the correct protein
            if (searchSettings.FastaEntries.Count > 0)
            {
                foreach (var entry in searchSettings.FastaEntries)
                {
                    entry.Selected = entry.ProteinName == this.SelectedPrSm.ProteinName;
                }
            }

            // Set scan number of selected spectrum
            var scanNum = 0;
            if (this.SpectrumViewModel.Ms2SpectrumViewModel.Spectrum != null)
            {
                scanNum = this.SpectrumViewModel.Ms2SpectrumViewModel.Spectrum.ScanNum;
                searchSettings.MinScanNumber = scanNum;
                searchSettings.MaxScanNumber = scanNum;
            }

            // Set search modifications
            if (this.MsPfParameters != null)
            {
                searchSettings.SearchModifications.AddRange(
                               this.MsPfParameters.Modifications.Select(
                                    searchMod => new SearchModificationViewModel(this.dialogService) { SearchModification = searchMod }));
                searchSettings.MinSequenceLength = this.MsPfParameters.MinSequenceLength;
                searchSettings.MaxSequenceLength = this.MsPfParameters.MaxSequenceLength;
                searchSettings.MinSequenceMass = this.MsPfParameters.MinSequenceMass;
                searchSettings.MaxSequenceMass = this.MsPfParameters.MaxSequenceMass;
                searchSettings.MinPrecursorIonCharge = this.MsPfParameters.MinPrecursorIonCharge;
                searchSettings.MaxPrecursorIonCharge = this.MsPfParameters.MaxPrecursorIonCharge;
                searchSettings.MinProductIonCharge = this.MsPfParameters.MinProductIonCharge;
                searchSettings.MaxProductIonCharge = this.MsPfParameters.MaxPrecursorIonCharge;
                searchSettings.PrecursorIonToleranceValue = this.MsPfParameters.PrecursorTolerancePpm.GetValue();
                searchSettings.ProductIonToleranceValue = this.MsPfParameters.ProductIonTolerancePpm.GetValue();
                searchSettings.MinFeatureProbability = this.MsPfParameters.MinFeatureProbablility;
                searchSettings.MaxDynamicModificationsPerSequence =
                    this.MsPfParameters.MaxDynamicModificationsPerSequence;
            }

            // TODO: change this so it doesn't use an event and isn't void async
            searchSettings.ReadyToClose += async (o, e) =>
            {
                if (searchSettings.Status)
                {
                    var idFileReader = IdFileReaderFactory.CreateReader(searchSettings.GetIdFilePath());
                    var prsms = await idFileReader.ReadAsync();
                    var prsmList = prsms.ToList();
                    prsmList.Sort(new PrSm.PrSmScoreComparer());
                    this.ScanViewModel.Data.AddRange(prsmList);

                    var scanPrsms = prsmList.Where(prsm => prsm.Scan == scanNum).ToList();
                    if (scanNum > 0 && scanPrsms.Count > 0)
                    {
                        this.SelectedPrSm = scanPrsms[0];
                    }
                    else if (prsmList.Count > 0)
                    {
                        this.SelectedPrSm = prsmList[0];
                    }
                }
            };

            this.dialogService.SearchSettingsWindow(searchSettings);
        }
    }
}
