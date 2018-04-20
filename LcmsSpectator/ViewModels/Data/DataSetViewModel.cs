// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataSetViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Class representing a data set, containing a LCMSRun, identifications, and features.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using InformedProteomics.Backend.Database;
using InformedProteomics.Backend.MassSpecData;
using InformedProteomics.Backend.Utils;
using LcmsSpectator.Config;
using LcmsSpectator.DialogServices;
using LcmsSpectator.Models;
using LcmsSpectator.Readers;
using LcmsSpectator.Utils;
using LcmsSpectator.ViewModels.Plots;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.Data
{
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
        /// A value indicating whether this dataset is loading.
        /// </summary>
        private bool isLoading;

        /// <summary>
        /// The progress of the loading.
        /// </summary>
        private double loadProgressPercent;

        /// <summary>
        /// The status message for the loading.
        /// </summary>
        private string loadProgressStatus;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSetViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">A dialog service for opening dialogs from the view model</param>
        public DataSetViewModel(IMainDialogService dialogService)
        {
            this.dialogService = dialogService;
            ReadyToClose = false;
            IdFileOpen = false;
            SelectedPrSm = new PrSm();
            ScanViewModel = new ScanViewModel(dialogService, new List<PrSm>());
            CreateSequenceViewModel = new CreateSequenceViewModel(dialogService)
            {
                SelectedDataSetViewModel = this
            };

            CreateSequenceViewModel.CreateAndAddPrSmCommand.Subscribe(
                _ => ScanViewModel.Data.Add(CreateSequenceViewModel.SelectedPrSm));

            // Remove filter by raw file name from ScanViewModel filters
            ScanViewModel.Filters.Remove(ScanViewModel.Filters.FirstOrDefault(f => f.Name == "Raw File Name"));

            // When a PrSm is selected from the ScanViewModel, update the SelectedPrSm for this data set
            ScanViewModel.WhenAnyValue(x => x.SelectedPrSm).Where(prsm => prsm != null).Subscribe(x => SelectedPrSm = x);

            // When the scan number in the selected prsm changes, the selected scan in the xic plots should update
            this.WhenAnyValue(x => x.SelectedPrSm)
            .Where(_ => SelectedPrSm != null && SpectrumViewModel != null && XicViewModel != null)
            .Subscribe(prsm =>
            {
                SpectrumViewModel.UpdateSpectra(prsm.Scan, SelectedPrSm.PrecursorMz);
                XicViewModel.SetSelectedScan(prsm.Scan);
                XicViewModel.ZoomToScan(prsm.Scan);
            });

            var prsmObservable = this.WhenAnyValue(x => x.SelectedPrSm).Where(prsm => prsm != null);

            prsmObservable
                .Select(prsm => prsm.GetFragmentationSequence()).Where(fragSeq => fragSeq != null)
                .Subscribe(fragSeq =>
                    {
                        SpectrumViewModel.FragmentationSequence = fragSeq;
                        XicViewModel.FragmentationSequence = fragSeq;
                    });

            // When the prsm changes, update the Scan View Model.
            prsmObservable.Subscribe(prsm => ScanViewModel.SelectedPrSm = prsm);

            // When the prsm updates, update the prsm in the sequence creator
            prsmObservable.Subscribe(prsm => CreateSequenceViewModel.SelectedPrSm = prsm);

            // When the prsm updates, update the feature map
            prsmObservable.Where(_ => FeatureMapViewModel != null).Subscribe(prsm => FeatureMapViewModel.FeatureMapViewModel.SelectedPrSm = prsm);

            // When prsm updates, subscribe to scan updates
            prsmObservable.Subscribe(prsm =>
            {
                prsm.WhenAnyValue(x => x.Scan, x => x.PrecursorMz)
                    .Where(x => x.Item1 > 0 && x.Item2 > 0 && SpectrumViewModel != null)
                    .Subscribe(x => SpectrumViewModel.UpdateSpectra(x.Item1, x.Item2));
                prsm.WhenAnyValue(x => x.Scan).Where(scan => scan > 0 && XicViewModel != null)
                    .Subscribe(scan => XicViewModel.SetSelectedScan(scan));
            });

            // When a new prsm is created by CreateSequenceViewModel, update SelectedPrSm
            CreateSequenceViewModel.WhenAnyValue(x => x.SelectedPrSm).Subscribe(prsm => SelectedPrSm = prsm);

            // When IDs are filtered in the ScanViewModel, update feature map with new IDs
            ScanViewModel.WhenAnyValue(x => x.FilteredData).Where(_ => FeatureMapViewModel != null).Subscribe(data => FeatureMapViewModel.UpdateIds(data));

            // Toggle instrument data when ShowInstrumentData setting is changed.
            IcParameters.Instance.WhenAnyValue(x => x.ShowInstrumentData).Select(async x => await ScanViewModel.ToggleShowInstrumentDataAsync(x, (PbfLcMsRun)LcMs)).Subscribe();

            // When product ion tolerance or ion correlation threshold change, update scorer factory
            IcParameters.Instance.WhenAnyValue(x => x.ProductIonTolerancePpm, x => x.IonCorrelationThreshold)
                .Subscribe(
                x =>
                {
                    var scorer = new ScorerFactory(x.Item1, Constants.MinCharge, Constants.MaxCharge, x.Item2);
                    CreateSequenceViewModel.ScorerFactory = scorer;
                    ScanViewModel.ScorerFactory = scorer;
                });

            // When an ID file has been opened, turn on the unidentified scan filter
            this.WhenAnyValue(x => x.IdFileOpen)
                .Where(idFileOpen => idFileOpen)
                .Select(_ => ScanViewModel.Filters.FirstOrDefault(f => f.Name == "Hide Unidentified Scans"))
                .Where(f => f != null)
                .Subscribe(f => f.Selected = true);

            // Start MsPf Search Command
            StartMsPfSearchCommand = ReactiveCommand.Create(StartMsPfSearchImplementation);

            // Close command verifies that the user wants to close the dataset, then sets ReadyToClose to true if they are
            CloseCommand = ReactiveCommand.Create(() =>
            {
                ReadyToClose =
                    dialogService.ConfirmationBox(
                        string.Format("Are you sure you would like to close {0}?", Title), string.Empty);
            });
        }

        /// <summary>
        /// Gets ScanViewModel that contains a list of identifications for this data set.
        /// </summary>
        public ScanViewModel ScanViewModel { get; }

        /// <summary>
        /// Gets the create sequence view model.
        /// </summary>
        public CreateSequenceViewModel CreateSequenceViewModel { get; }

        /// <summary>
        /// Gets a command that starts an MSPathFinder with this data set.
        /// </summary>
        public ReactiveCommand<Unit, Unit> StartMsPfSearchCommand { get; }

        /// <summary>
        /// Gets a command that is activated when the close button is clicked on a dataset.
        /// Initiates a close request for the main view model
        /// </summary>
        public ReactiveCommand<Unit, Unit> CloseCommand { get; }

        /// <summary>
        /// Gets the LCMSRun representing the raw file for this dataset.
        /// </summary>
        public ILcMsRun LcMs { get; private set; }

        /// <summary>
        /// Gets or sets parsed MSPathFinder parameters (configuration for database search)
        /// </summary>
        public MsPfParameters MsPfParameters
        {
            get => mspfParameters;
            set => this.RaiseAndSetIfChanged(ref mspfParameters, value);
        }

        /// <summary>
        /// Gets or sets the path to the FASTA database file associated with this data set.
        /// </summary>
        public string FastaDbFilePath { get; set; }

        /// <summary>
        /// Gets or sets the path for the layout file for this dataset.
        /// </summary>
        public string LayoutFilePath { get; set; }

        /// <summary>
        /// Gets view model for extracted ion chromatogram
        /// </summary>
        public XicViewModel XicViewModel
        {
            get => xicViewModel;
            private set => this.RaiseAndSetIfChanged(ref xicViewModel, value);
        }

        /// <summary>
        /// Gets view model for spectrum plots (MS/MS, previous MS1, next MS1)
        /// </summary>
        public SpectrumViewModel SpectrumViewModel
        {
            get => spectrumViewModel;
            private set => this.RaiseAndSetIfChanged(ref spectrumViewModel, value);
        }

        /// <summary>
        /// Gets view model for mass spec feature map plot
        /// </summary>
        public FeatureViewerViewModel FeatureMapViewModel
        {
            get => featureMapViewModel;
            private set => this.RaiseAndSetIfChanged(ref featureMapViewModel, value);
        }

        /// <summary>
        /// Gets the title of this data set
        /// </summary>
        public string Title
        {
            get => title;
            private set => this.RaiseAndSetIfChanged(ref title, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not this data set is ready to be closed?
        /// </summary>
        public bool ReadyToClose
        {
            get => readyToClose;
            set => this.RaiseAndSetIfChanged(ref readyToClose, value);
        }

        /// <summary>
        /// Gets or sets the selected Protein-Spectrum-Match Identification
        /// </summary>
        public PrSm SelectedPrSm
        {
            get => selectedPrSm;
            set => this.RaiseAndSetIfChanged(ref selectedPrSm, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether an ID file has been opened for this data set.
        /// </summary>
        public bool IdFileOpen
        {
            get => idFileOpen;
            set => this.RaiseAndSetIfChanged(ref idFileOpen, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this dataset is loading.
        /// </summary>
        public bool IsLoading
        {
            get => isLoading;
            set => this.RaiseAndSetIfChanged(ref isLoading, value);
        }

        /// <summary>
        /// Gets or sets the progress of the loading.
        /// </summary>
        public double LoadProgressPercent
        {
            get => loadProgressPercent;
            set => this.RaiseAndSetIfChanged(ref loadProgressPercent, value);
        }

        /// <summary>
        /// Gets or sets the status message for the loading.
        /// </summary>
        public string LoadProgressStatus
        {
            get => loadProgressStatus;
            set => this.RaiseAndSetIfChanged(ref loadProgressStatus, value);
        }

        /// <summary>
        /// Set the MSPathFinder parameters given a path to a ID file.
        /// </summary>
        /// <param name="idFilePath">The id file path.</param>
        public void SetMsPfParameters(string idFilePath)
        {
            try
            {
                MsPfParameters = MsPfParameters.ReadFromFile(idFilePath);
            }
            catch (FormatException)
            {
                dialogService.MessageBox("MsPathFinder Parameters are not properly formatted.");
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
            filePath = MassSpecDataReaderFactory.NormalizeDatasetPath(filePath);
            IsLoading = true; // Show animated loading screen
            Title = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(filePath));
            rawFilePath = filePath;

            LoadProgressPercent = 0.0;
            LoadProgressStatus = "Loading...";
            var progress = new Progress<ProgressData>(progressData =>
            {
                progressData.UpdateFrequencySeconds = 2;
                if (progressData.ShouldUpdate())
                {
                    LoadProgressPercent = progressData.Percent;
                    LoadProgressStatus = progressData.Status;
                }
            });

            // load raw file
            LcMs = await Task.Run(() => PbfLcMsRun.GetLcMsRun(filePath, 0, 0, progress));

            // Now that we have an LcMsRun, initialize viewmodels that require it
            XicViewModel = new XicViewModel(dialogService, LcMs);
            SpectrumViewModel = new SpectrumViewModel(dialogService, LcMs);
            FeatureMapViewModel = new FeatureViewerViewModel((LcMsRun)LcMs, dialogService);

            // When the selected scan changes in the xic plots, the selected scan for the prsm should update
            XicViewModel.SelectedScanUpdated().Subscribe(scan => SelectedPrSm.Scan = scan);

            // When an ID is selected on FeatureMap, update selectedPrSm
            FeatureMapViewModel.FeatureMapViewModel.WhenAnyValue(x => x.SelectedPrSm).Where(prsm => prsm != null).Subscribe(prsm => SelectedPrSm = prsm);

            // Create prsms for scan numbers (unidentified)
            await LoadScans();
            ////await this.ScanViewModel.ToggleShowInstrumentDataAsync(IcParameters.Instance.ShowInstrumentData, (PbfLcMsRun)this.LcMs);
            SelectedPrSm.LcMs = LcMs; // For the selected PrSm, we should always use the LcMsRun for this dataset.

            IsLoading = false; // Hide animated loading screen
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
                var scans = LcMs.GetScanNumbers(1).ToList();
                scans.AddRange(LcMs.GetScanNumbers(2));
                scans.Sort();
                var prsmScans = scans.Select(scan => new PrSm
                {
                    Scan = scan,
                    RawFileName = Title,
                    LcMs = LcMs,
                    QValue = -1.0,
                    Score = double.NaN,
                });
                ScanViewModel.Data.AddRange(prsmScans);
            });
        }

        /// <summary>
        /// Implementation for <see cref="StartMsPfSearchCommand" />.
        /// Gets a command that starts an MSPathFinder with this data set.
        /// </summary>
        private void StartMsPfSearchImplementation()
        {
            var searchSettings = new SearchSettingsViewModel(dialogService, MsPfParameters)
            {
                SpectrumFilePath = rawFilePath,
                SelectedSearchMode = InternalCleavageType.SingleInternalCleavage,
                FastaDbFilePath = FastaDbFilePath,
                OutputFilePath = string.Format("{0}\\{1}", Directory.GetCurrentDirectory(), Title),
                SelectedSequence = SelectedPrSm.Sequence.Aggregate(string.Empty, (current, aa) => current + aa.Residue)
            };

            // Set feature file path.
            if (FeatureMapViewModel != null)
            {
                searchSettings.FeatureFilePath = FeatureMapViewModel.FeatureFilePath;
            }

            // Select the correct protein
            if (searchSettings.FastaEntries.Count > 0)
            {
                foreach (var entry in searchSettings.FastaEntries)
                {
                    entry.Selected = entry.ProteinName == SelectedPrSm.ProteinName;
                }
            }

            // Set scan number of selected spectrum
            var scanNum = 0;
            if (SpectrumViewModel.Ms2SpectrumViewModel.Spectrum != null)
            {
                scanNum = SpectrumViewModel.Ms2SpectrumViewModel.Spectrum.ScanNum;
                searchSettings.MinScanNumber = scanNum;
                searchSettings.MaxScanNumber = scanNum;
            }

            // TODO: change this so it doesn't use an event and isn't void async
            searchSettings.ReadyToClose += async (o, e) =>
            {
                if (searchSettings.Status)
                {
                    var idFileReader = IdFileReaderFactory.CreateReader(searchSettings.GetIdFilePath());
                    var prsms = await idFileReader.ReadAsync();
                    var prsmList = prsms.ToList();
                    foreach (var prsm in prsmList)
                    {
                        prsm.LcMs = LcMs;
                    }

                    prsmList.Sort(new PrSm.PrSmScoreComparer());
                    ScanViewModel.Data.AddRange(prsmList);

                    var scanPrsms = prsmList.Where(prsm => prsm.Scan == scanNum).ToList();
                    if (scanNum > 0 && scanPrsms.Count > 0)
                    {
                        SelectedPrSm = scanPrsms[0];
                    }
                    else if (prsmList.Count > 0)
                    {
                        SelectedPrSm = prsmList[0];
                    }
                }
            };

            dialogService.SearchSettingsWindow(searchSettings);
        }
    }
}
