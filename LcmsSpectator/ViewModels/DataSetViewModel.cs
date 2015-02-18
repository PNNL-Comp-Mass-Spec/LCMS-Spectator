using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.Config;
using LcmsSpectator.DialogServices;
using LcmsSpectator.Models;
using LcmsSpectator.Readers;
using LcmsSpectator.TaskServices;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
    public class DataSetViewModel: ReactiveObject
    {
        #region Child View Models
        public FeatureViewerViewModel FeatureMapViewModel { get; private set; }
        public ScanViewModel ScanViewModel { get; private set; }
        public IonTypeSelectorViewModel IonTypeSelectorViewModel { get; private set; }
        public IonListViewModel IonListViewModel { get; private set; }
        public CreateSequenceViewModel CreateSequenceViewModel { get; private set; }
        public LoadingScreenViewModel LoadingScreenViewModel { get; private set; }
        #endregion

        #region Commands
        public IReactiveCommand OpenFeatureFileCommand { get; private set; }
        public IReactiveCommand CloseCommand { get; private set; }
        #endregion

        public DataSetViewModel(IMainDialogService dialogService, ITaskService taskService)
        {
            _dialogService = dialogService;
            ITaskService taskService1 = taskService;
            _showFeatureMapSplash = true;
            ReadyToClose = false;
            LoadingScreenViewModel = new LoadingScreenViewModel();
            SelectedPrSm = new PrSm();
            FeatureMapViewModel = new FeatureViewerViewModel();
            ScanViewModel = new ScanViewModel(_dialogService, TaskServiceFactory.GetTaskServiceLike(taskService1), new List<PrSm>());
            IonTypeSelectorViewModel = new IonTypeSelectorViewModel(_dialogService);
            CreateSequenceViewModel = new CreateSequenceViewModel(new ReactiveList<DataSetViewModel>(),
                _dialogService)
            {
                SelectedDataSetViewModel = this
            };
            var openFeatureFileCommand = ReactiveCommand.Create();
            openFeatureFileCommand.Subscribe(_ => OpenFeatureFile());
            OpenFeatureFileCommand = openFeatureFileCommand;

            this.WhenAnyValue(x => x.RawFilePath).Where(rawFilePath => !String.IsNullOrEmpty(rawFilePath))
                .Subscribe(rawFilePath =>
                {
                    LoadRawFile();
                    // When the selected scan changes in the xic plots, the selected scan for the prsm should update
                    XicViewModel.WhenAnyValue(x => x.FragmentPlotViewModel.SelectedScan)
                        .Subscribe(scan => SelectedPrSm.Scan = scan);
                    XicViewModel.WhenAnyValue(x => x.HeavyFragmentPlotViewModel.SelectedScan)
                        .Subscribe(scan => SelectedPrSm.Scan = scan);
                    XicViewModel.WhenAnyValue(x => x.PrecursorPlotViewModel.SelectedScan)
                        .Subscribe(scan => SelectedPrSm.Scan = scan);
                    XicViewModel.WhenAnyValue(x => x.HeavyPrecursorPlotViewModel.SelectedScan)
                        .Subscribe(scan => SelectedPrSm.Scan = scan);

                    // When precursor view mode is selected in XicViewModel, ion lists should be recalculated.
                    XicViewModel.WhenAnyValue(x => x.PrecursorViewMode, x => x.ShowHeavy)
                        .Subscribe(x =>
                        {
                            IonListViewModel.PrecursorViewMode = x.Item1;
                            IonListViewModel.ShowHeavy = x.Item2;
                        });

                    IonListViewModel.WhenAnyValue(x => x.FragmentLabels)
                    .Subscribe(labels =>
                    {
                        SpectrumViewModel.PrimarySpectrumViewModel.Ions = labels;
                        XicViewModel.FragmentPlotViewModel.Ions = labels;
                    });
                    IonListViewModel.WhenAnyValue(x => x.HeavyFragmentLabels)
                    .Subscribe(labels =>
                    {
                        XicViewModel.HeavyFragmentPlotViewModel.Ions = labels;
                    });
                    IonListViewModel.WhenAnyValue(x => x.PrecursorLabels)
                    .Where(labels => labels != null)
                    .Select(labels => new ReactiveList<LabeledIonViewModel>(labels.Where(l => l.Index == 0)) { ChangeTrackingEnabled = true })
                    .Subscribe(labels =>
                    {
                        SpectrumViewModel.Secondary1ViewModel.Ions = labels;
                        SpectrumViewModel.Secondary2ViewModel.Ions = labels;
                    });
                    IonListViewModel.WhenAnyValue(x => x.PrecursorLabels)
                    .Where(labels => labels != null)
                    .Subscribe(labels =>
                    {
                        XicViewModel.PrecursorPlotViewModel.Ions = labels;
                    });
                    IonListViewModel.WhenAnyValue(x => x.HeavyPrecursorLabels)
                    .Where(labels => labels != null)
                    .Subscribe(labels =>
                    {
                        XicViewModel.HeavyPrecursorPlotViewModel.Ions = labels;
                    });
                });

            ScanViewModel.WhenAnyValue(x => x.SelectedPrSm).Where(prsm => prsm != null).Subscribe(x => SelectedPrSm = x);

            // When the scan in the prsm changes, the selected scan in the xic plots should update
            this.WhenAnyValue(x => x.SelectedPrSm)
            .Where(_ => SelectedPrSm != null && SpectrumViewModel != null && XicViewModel != null)
            .Subscribe(prsm =>
            {
                SpectrumViewModel.UpdateSpectra(prsm.Scan, SelectedPrSm.PrecursorMz);
                XicViewModel.FragmentPlotViewModel.SelectedScan = prsm.Scan;
                XicViewModel.PrecursorPlotViewModel.SelectedScan = prsm.Scan;
                XicViewModel.HeavyFragmentPlotViewModel.SelectedScan = prsm.Scan;
                XicViewModel.HeavyPrecursorPlotViewModel.SelectedScan = prsm.Scan;
            });

            // When the prsm changes, the ion lists should be recalculated
            // When the prsm changes, update the sequence creator
            this.WhenAnyValue(x => x.SelectedPrSm).Subscribe(prsm =>
            {
                if (IonListViewModel != null) IonListViewModel.SelectedPrSm = prsm;
                IonTypeSelectorViewModel.ActivationMethod = prsm.ActivationMethod;
                IonTypeSelectorViewModel.SelectedCharge = prsm.Charge;
                CreateSequenceViewModel.SelectedPrSm = prsm;
                FeatureMapViewModel.SelectedPrSm = prsm;
                prsm.WhenAnyValue(x => x.Scan)
                    .Where(_ => SelectedPrSm != null && SpectrumViewModel != null && XicViewModel != null)
                    .Subscribe(scan =>
                    {
                        if (SpectrumViewModel != null) SpectrumViewModel.UpdateSpectra(scan, SelectedPrSm.PrecursorMz);
                        if (XicViewModel != null)
                        {
                            XicViewModel.FragmentPlotViewModel.SelectedScan = scan;
                            XicViewModel.PrecursorPlotViewModel.SelectedScan = scan;
                            XicViewModel.HeavyFragmentPlotViewModel.SelectedScan = scan;
                            XicViewModel.HeavyPrecursorPlotViewModel.SelectedScan = scan;
                            XicViewModel.ZoomToScan(scan);   
                        }
                    });
            });

            // When a new prsm is created by CreateSequenceViewModel, update SelectedPrSm
            CreateSequenceViewModel.WhenAnyValue(x => x.SelectedPrSm).Subscribe(prsm => SelectedPrSm = prsm);

            // When an ID is selected on FeatureMap, update selectedPrSm
            FeatureMapViewModel.WhenAnyValue(x => x.SelectedPrSm).Where(prsm => prsm != null).Subscribe(prsm => SelectedPrSm = prsm);

            // When the ion types change, the ion lists should be recalculated.
            IonTypeSelectorViewModel.WhenAnyValue(x => x.IonTypes).Where(_ => IonListViewModel != null).Subscribe(ionTypes => IonListViewModel.IonTypes = ionTypes);

            ScanViewModel.WhenAnyValue(x => x.FilteredData).Subscribe(data => FeatureMapViewModel.UpdateIds(data));

            // Toggle instrument data when ShowInstrumentData setting is changed.
            IcParameters.Instance.WhenAnyValue(x => x.ShowInstrumentData).Subscribe(async x => await ToggleShowInstrumentData(x));

            var closeCommand = ReactiveCommand.Create();
            closeCommand.Subscribe(_ =>
            {
                ReadyToClose =
                    _dialogService.ConfirmationBox(
                        String.Format("Are you sure you would like to close {0}?", RawFileName), "");
            });
            CloseCommand = closeCommand;
        }

        #region Public Properties

        public ILcMsRun Lcms { get; private set; }

        private MsPfParameters _mspfParameters;
        public MsPfParameters MsPfParameters
        {
            get { return _mspfParameters; }
            set { this.RaiseAndSetIfChanged(ref _mspfParameters, value); }
        }

        private XicViewModel _xicViewModel;
        public XicViewModel XicViewModel
        {
            get { return _xicViewModel; }
            private set { this.RaiseAndSetIfChanged(ref _xicViewModel, value); }
        }

        private SpectrumViewModel _spectrumViewModel;
        public SpectrumViewModel SpectrumViewModel
        {
            get { return _spectrumViewModel; } 
            private set { this.RaiseAndSetIfChanged(ref _spectrumViewModel, value); }
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


        private string _rawFilePath;
        /// <summary>
        /// Full path to the raw file including extension.
        /// </summary>
        public string RawFilePath
        {
            get { return _rawFilePath; }
            set { this.RaiseAndSetIfChanged(ref _rawFilePath, value); }
        }

        private bool _readyToClose;
        public bool ReadyToClose
        {
            get { return _readyToClose; }
            set { this.RaiseAndSetIfChanged(ref _readyToClose, value); }
        }

        private void LoadRawFile()
        {
            LoadingScreenViewModel.IsLoading = true;
            RawFileName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(RawFilePath));
            var extension = Path.GetExtension(RawFilePath);
            if (extension != null) extension = extension.ToLower();
            // load raw file
            var massSpecDataType = (extension == ".mzml") ? MassSpecDataType.MzMLFile : MassSpecDataType.XCaliburRun;
            Lcms = PbfLcMsRun.GetLcMsRun(_rawFilePath, massSpecDataType, 0, 0);

            XicViewModel = new XicViewModel(_dialogService, Lcms);
            SpectrumViewModel = new SpectrumViewModel(_dialogService, Lcms);
            IonListViewModel = new IonListViewModel(Lcms);

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
            ToggleShowInstrumentData(IcParameters.Instance.ShowInstrumentData);
            SelectedPrSm.Lcms = Lcms;
            LoadingScreenViewModel.IsLoading = false;
        }

        private bool _showFeatureMapSplash;
        /// <summary>
        /// Sets/Gets whether the loading screen is currently being shown.
        /// </summary>
        public bool ShowFeatureMapSplash
        {
            get { return _showFeatureMapSplash; }
            set { this.RaiseAndSetIfChanged(ref _showFeatureMapSplash, value); }
        }

        private PrSm _selectedPrSm;
        public PrSm SelectedPrSm
        {
            get { return _selectedPrSm; }
            set { this.RaiseAndSetIfChanged(ref _selectedPrSm, value); }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Add Identifications associated with data set.
        /// </summary>
        /// <param name="ids">Identifications</param>
        public void AddIds(IdentificationTree ids)
        {
            ScanViewModel.AddIds(ids.AllPrSms);
        }

        /// <summary>
        /// Read features and display feature map for this data set.
        /// </summary>
        /// <param name="filePath">Path of feature file.</param>
        public void OpenFeatureFile(string filePath)
        {
            ShowFeatureMapSplash = false;
            try
            {
                var features = FeatureReader.Read(filePath);
                FeatureMapViewModel.SetData((LcMsRun) Lcms, features.ToList(), ScanViewModel.FilteredData);
            }
            catch (InvalidCastException)
            {
                _dialogService.MessageBox("Cannot open features for this type of data set.");
            }
        }

        /// <summary>
        /// Display open file dialog to select feature file and then read and display features.
        /// </summary>
        public void OpenFeatureFile()
        {
            const string formatStr = @"TSV Files (*.txt; *tsv)|*.txt;*.tsv";
            var tsvFileName = _dialogService.OpenFile(".txt", formatStr);
            if (!String.IsNullOrEmpty(tsvFileName))
            {
                ShowFeatureMapSplash = false;
                var features = FeatureReader.Read(tsvFileName);
                FeatureMapViewModel.SetData((LcMsRun)Lcms, features.ToList(), ScanViewModel.FilteredData);
            }
        }
        #endregion

        #region Private Methods

        public Task ToggleShowInstrumentData(bool value)
        {
            return Task.Run(() =>
            {
                LoadingScreenViewModel.IsLoading = true;
                if (value)
                {
                    var pbfLcmsRun = Lcms as PbfLcMsRun;
                    if (pbfLcmsRun != null)
                    {
                        var scans = ScanViewModel.Data;
                        foreach (var scan in scans.Where(scan => scan.Sequence.Count == 0))
                        {
                            IsolationWindow isolationWindow = pbfLcmsRun.GetIsolationWindow(scan.Scan);
                            scan.PrecursorMz = isolationWindow.MonoisotopicMz == null ? Double.NaN : isolationWindow.MonoisotopicMz.Value;
                            scan.Charge = isolationWindow.Charge == null ? 0 : isolationWindow.Charge.Value;
                            scan.Mass = isolationWindow.MonoisotopicMass == null ? Double.NaN : isolationWindow.MonoisotopicMass.Value;
                        }
                    }
                }
                else
                {
                    var scans = ScanViewModel.Data;
                    foreach (var scan in scans.Where(scan => scan.Sequence.Count == 0))
                    {
                        scan.PrecursorMz = Double.NaN;
                        scan.Charge = 0;
                        scan.Mass = Double.NaN;
                    }
                }
                ScanViewModel.FilterData();
                LoadingScreenViewModel.IsLoading = false;
            });
        }
        #endregion

        #region Private Members
        private readonly IMainDialogService _dialogService;
        #endregion
    }
}
