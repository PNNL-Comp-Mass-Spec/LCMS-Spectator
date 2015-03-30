using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.Config;
using LcmsSpectator.DialogServices;
using LcmsSpectator.Models;
using LcmsSpectator.TaskServices;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
    public class DataSetViewModel: ReactiveObject
    {
        /// <summary>
        /// Create a new instance of the DataSetViewModel class.
        /// </summary>
        /// <param name="dialogService">A dialog service for opening dialogs from the view model</param>
        /// <param name="taskService">Task scheduler for managing parallel/synchronous tasks.</param>
        public DataSetViewModel(IMainDialogService dialogService, ITaskService taskService)
        {
            _dialogService = dialogService;
            ReadyToClose = false;
            LoadingScreenViewModel = new LoadingScreenViewModel();
            SelectedPrSm = new PrSm();
            ScanViewModel = new ScanViewModel(_dialogService, TaskServiceFactory.GetTaskServiceLike(taskService), new List<PrSm>());
            IonTypeSelectorViewModel = new IonTypeSelectorViewModel(_dialogService);
            CreateSequenceViewModel = new CreateSequenceViewModel(new ReactiveList<DataSetViewModel>(),
                _dialogService) { SelectedDataSetViewModel = this };

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

            // When the prsm changes, the ion lists should be recalculated
            prsmObservable.Where(_ => IonListViewModel != null)
                .Subscribe(prsm => IonListViewModel.SelectedPrSm = prsm);

            // When the prsm changes, update activation method and selected charge in the ion type selector
            prsmObservable.Select(prsm => prsm.Ms2Spectrum).Where(ms2 => ms2 != null)
                          .Subscribe(ms2 => IonTypeSelectorViewModel.ActivationMethod = ms2.ActivationMethod);
            prsmObservable.Subscribe(prsm => IonTypeSelectorViewModel.SelectedCharge = prsm.Charge);

            // When the prsm updates, update the prsm in the sequence creator
            prsmObservable.Subscribe(prsm => CreateSequenceViewModel.SelectedPrSm = prsm);

            // When the prsm updates, update the feature map
            prsmObservable.Where(_ => FeatureMapViewModel != null).Subscribe(prsm => FeatureMapViewModel.SelectedPrSm = prsm);

            // When prsm updates, update sequence in spectrum plot view model
            prsmObservable.Where(prsm => prsm.Sequence != null && SpectrumViewModel != null)
                .Select(prsm => prsm.Sequence)
                .Subscribe(sequence => SpectrumViewModel.Ms2SpectrumViewModel.Sequence = sequence);

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

            // When the ion types change, the ion lists should be recalculated.
            IonTypeSelectorViewModel.WhenAnyValue(x => x.IonTypes).Where(_ => IonListViewModel != null).Subscribe(ionTypes => IonListViewModel.IonTypes = ionTypes);

            // When IDs are filtered in the ScanViewModel, update feature map with new IDs
            ScanViewModel.WhenAnyValue(x => x.FilteredData).Where(_ => FeatureMapViewModel != null).Subscribe(data => FeatureMapViewModel.UpdateIds(data));

            // Toggle instrument data when ShowInstrumentData setting is changed.
            IcParameters.Instance.WhenAnyValue(x => x.ShowInstrumentData).Select(async x => await ScanViewModel.ToggleShowInstrumentDataAsync(x, (PbfLcMsRun)Lcms)).Subscribe();

            // Close command verifies that the user wants to close the dataset, then sets ReadyToClose to true if they are
            var closeCommand = ReactiveCommand.Create();
            closeCommand.Subscribe(_ =>
            {
                ReadyToClose =
                    _dialogService.ConfirmationBox(
                        String.Format("Are you sure you would like to close {0}?", RawFileName), "");
            });
            CloseCommand = closeCommand;
        }

        public ScanViewModel ScanViewModel { get; private set; }
        public IonTypeSelectorViewModel IonTypeSelectorViewModel { get; private set; }
        public CreateSequenceViewModel CreateSequenceViewModel { get; private set; }
        public LoadingScreenViewModel LoadingScreenViewModel { get; private set; }

        /// <summary>
        /// Command that is activated when the close button is clicked on a dataset.
        /// Initiates a close request for the main view model
        /// </summary>
        public IReactiveCommand CloseCommand { get; private set; }

        /// <summary>
        /// The LcMsRun representing the raw file for this dataset.
        /// </summary>
        public ILcMsRun Lcms { get; private set; }

        private IonListViewModel _ionListViewModel;
        /// <summary>
        /// View model for fragment and precursor ion selection.
        /// </summary>
        public IonListViewModel IonListViewModel
        {
            get { return _ionListViewModel; }
            private set { this.RaiseAndSetIfChanged(ref _ionListViewModel, value); }
        }

        private MsPfParameters _mspfParameters;
        /// <summary>
        /// Parsed MsPathFinder parameters (configuration for database search)
        /// </summary>
        public MsPfParameters MsPfParameters
        {
            get { return _mspfParameters; }
            set { this.RaiseAndSetIfChanged(ref _mspfParameters, value); }
        }

        private XicViewModel _xicViewModel;
        /// <summary>
        /// View model for extracted ion chromatogram
        /// </summary>
        public XicViewModel XicViewModel
        {
            get { return _xicViewModel; }
            private set { this.RaiseAndSetIfChanged(ref _xicViewModel, value); }
        }

        private SpectrumViewModel _spectrumViewModel;
        /// <summary>
        /// View model for spectrum plots (ms/ms, prev ms1, next ms1) 
        /// </summary>
        public SpectrumViewModel SpectrumViewModel
        {
            get { return _spectrumViewModel; } 
            private set { this.RaiseAndSetIfChanged(ref _spectrumViewModel, value); }
        }

        private FeatureViewerViewModel _featureMapViewModel;
        /// <summary>
        /// View model for ms1 feature map plot
        /// </summary>
        public FeatureViewerViewModel FeatureMapViewModel
        {
            get { return _featureMapViewModel; } 
            private set { this.RaiseAndSetIfChanged(ref _featureMapViewModel, value); }
        }

        private string _rawFileName;
        /// <summary>
        /// Raw file name without path or extension. For displaying on tab header.
        /// </summary>
        public string RawFileName
        {
            get { return _rawFileName; }
            private set { this.RaiseAndSetIfChanged(ref _rawFileName, value); }
        }

        private bool _readyToClose;
        /// <summary>
        /// Is this data set ready to be closed?
        /// </summary>
        public bool ReadyToClose
        {
            get { return _readyToClose; }
            set { this.RaiseAndSetIfChanged(ref _readyToClose, value); }
        }

        private PrSm _selectedPrSm;
        /// <summary>
        /// Currently selected Protein-Spectrum-Match Identification
        /// </summary>
        public PrSm SelectedPrSm
        {
            get { return _selectedPrSm; }
            set { this.RaiseAndSetIfChanged(ref _selectedPrSm, value); }
        }

        /// <summary>
        /// Initialize this data set by reading the raw file asynchronously and initializing child view models
        /// </summary>
        /// <returns></returns>
        public async Task InitializeAsync(string rawFilePath)
        {
            LoadingScreenViewModel.IsLoading = true; //  Show animated loading screen
            RawFileName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(rawFilePath));
            var extension = Path.GetExtension(rawFilePath);
            if (extension != null) extension = extension.ToLower();
            if (extension == ".gz")
            {
                extension = Path.GetExtension(Path.GetFileNameWithoutExtension(rawFilePath));
                if (extension != null) extension = extension.ToLower();
            }
            // load raw file
            var massSpecDataType = (extension == ".mzml") ? MassSpecDataType.MzMLFile : MassSpecDataType.XCaliburRun;
            Lcms = await Task.Run(() => PbfLcMsRun.GetLcMsRun(rawFilePath, massSpecDataType, 0, 0));

            // Now that we have an LcMsRun, initialize viewmodels that require it
            XicViewModel = new XicViewModel(_dialogService, Lcms);
            SpectrumViewModel = new SpectrumViewModel(_dialogService, Lcms);
            IonListViewModel = new IonListViewModel(Lcms);
            FeatureMapViewModel = new FeatureViewerViewModel(_dialogService, (LcMsRun)Lcms);
            InitWirings(); // Initialize the wirings for updating these view models

            // Create prsms for scan numbers (unidentified)
            var scans = Lcms.GetScanNumbers(2);
            var prsmScans = scans.Select(scan => new PrSm
            {
                Scan = scan,
                RawFileName = RawFileName,
                Lcms = Lcms,
                QValue = 1.0,
                Score = Double.NaN,
            });
            ScanViewModel.AddIds(prsmScans);
            await ScanViewModel.ToggleShowInstrumentDataAsync(IcParameters.Instance.ShowInstrumentData, (PbfLcMsRun) Lcms);
            SelectedPrSm.Lcms = Lcms; // For the selected PrSm, we should always use the LcMsRun for this dataset.
            LoadingScreenViewModel.IsLoading = false; // Hide animated loading screen
        }

        private void InitWirings()
        {
            // When the selected scan changes in the xic plots, the selected scan for the prsm should update
            XicViewModel.SelectedScanUpdated().Subscribe(scan => SelectedPrSm.Scan = scan);

            // When precursor view mode is selected in XicViewModel, ion lists should be recalculated.
            XicViewModel.WhenAnyValue(x => x.PrecursorViewMode, x => x.ShowHeavy)
                .Subscribe(x =>
                {
                    IonListViewModel.PrecursorViewMode = x.Item1;
                    IonListViewModel.ShowHeavy = x.Item2;
                });

            // When FragmentLabels or ChargePrecursorLabels change, update spectra
            IonListViewModel.WhenAnyValue(x => x.FragmentLabels, x => x.ChargePrecursorLabels)
            .Where(x => x.Item1 != null && x.Item2 != null && SelectedPrSm != null)
            .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
            .Subscribe(x =>
            {
                var preclabels = x.Item2.Where(l => l.IonType.Charge <= SelectedPrSm.Charge);
                var labels = new ReactiveList<LabeledIonViewModel>(x.Item1) { ChangeTrackingEnabled = true };
                labels.AddRange(preclabels);
                if (SpectrumViewModel.Ms2SpectrumViewModel.Ions != null)
                    SpectrumViewModel.Ms2SpectrumViewModel.Ions.ChangeTrackingEnabled = false;
                SpectrumViewModel.Ms2SpectrumViewModel.Ions = labels;
                SpectrumViewModel.PrevMs1SpectrumViewModel.Ions = labels;
                SpectrumViewModel.NextMs1SpectrumViewModel.Ions = labels;
            });

            // When FragmentLabels or PrecursorLabels change, update XICs
            IonListViewModel.WhenAnyValue(x => x.FragmentLabels).Subscribe(labels => XicViewModel.FragmentPlotViewModel.Ions = labels);
            IonListViewModel.WhenAnyValue(x => x.HeavyFragmentLabels).Subscribe(labels => XicViewModel.HeavyFragmentPlotViewModel.Ions = labels);
            IonListViewModel.WhenAnyValue(x => x.PrecursorLabels).Where(labels => labels != null)
            .Subscribe(labels => XicViewModel.PrecursorPlotViewModel.Ions = labels);
            IonListViewModel.WhenAnyValue(x => x.HeavyPrecursorLabels).Where(labels => labels != null)
            .Subscribe(labels => XicViewModel.HeavyPrecursorPlotViewModel.Ions = labels);

            // When an ID is selected on FeatureMap, update selectedPrSm
            FeatureMapViewModel.WhenAnyValue(x => x.SelectedPrSm).Where(prsm => prsm != null).Subscribe(prsm => SelectedPrSm = prsm);
        }

        private readonly IMainDialogService _dialogService;
    }
}
