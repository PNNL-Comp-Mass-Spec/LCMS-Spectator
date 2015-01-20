using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.DialogServices;
using LcmsSpectator.TaskServices;
using LcmsSpectator.Utils;
using LcmsSpectatorModels.Config;
using LcmsSpectatorModels.Models;
using LcmsSpectatorModels.Readers;

namespace LcmsSpectator.ViewModels
{
    public class DataSetViewModel: ViewModelBase
    {
        #region Child View Models
        public PrSmViewModel SelectedPrSm { get; private set; }
        public XicViewModel XicViewModel { get; private set; }
        public SpectrumViewModel SpectrumViewModel { get; private set; }
        public FeatureViewerViewModel FeatureMapViewModel { get; private set; }
        public ScanViewModel ScanViewModel { get; private set; }
        public IonTypeSelectorViewModel IonTypeSelectorViewModel { get; private set; }
        public IonListViewModel IonListViewModel { get; private set; }
        public CreateSequenceViewModel CreateSequenceViewModel { get; private set; }
        public LoadingScreenViewModel LoadingScreenViewModel { get; private set; }
        #endregion

        #region Commands
        public RelayCommand OpenFeatureFileCommand { get; private set; }
        public RelayCommand CloseCommand { get; private set; }
        #endregion

        public DataSetViewModel(IMainDialogService dialogService, ITaskService taskService)
        {
            _dialogService = dialogService;
            ITaskService taskService1 = taskService;
            var messenger = new Messenger();
            MessengerInstance = messenger;
            Messenger.Default.Register<SettingsChangedNotification>(this, SettingsChanged);
            SelectedPrSm = new PrSmViewModel(messenger);
            XicViewModel = new XicViewModel(_dialogService, TaskServiceFactory.GetTaskServiceLike(taskService1), messenger);
            SpectrumViewModel = new SpectrumViewModel(_dialogService, TaskServiceFactory.GetTaskServiceLike(taskService1), messenger);
            FeatureMapViewModel = new FeatureViewerViewModel(taskService1, messenger);
            ScanViewModel = new ScanViewModel(_dialogService, TaskServiceFactory.GetTaskServiceLike(taskService1), new List<PrSm>(), messenger);
            IonTypeSelectorViewModel = new IonTypeSelectorViewModel(_dialogService, messenger);
            IonListViewModel = new IonListViewModel(_dialogService, taskService1, messenger);
            CreateSequenceViewModel = new CreateSequenceViewModel(new ObservableCollection<DataSetViewModel>(),
                _dialogService, messenger)
            {
                SelectedDataSetViewModel = this
            };
            LoadingScreenViewModel = new LoadingScreenViewModel(TaskServiceFactory.GetTaskServiceLike(taskService));

            _showFeatureMapSplash = true;

            _showInstrumentData = IcParameters.Instance.ShowInstrumentData;

            OpenFeatureFileCommand = new RelayCommand(OpenFeatureFile);

            CloseCommand = new RelayCommand(() =>
            {
                if (_dialogService.ConfirmationBox(String.Format("Are you sure you would like to close {0}?", RawFileName), ""))
                    Messenger.Default.Send(new DataSetCloseRequest(this));
            });
        }

        #region Public Properties
        public ILcMsRun Lcms { get; private set; }

        private string _rawFileName;
        /// <summary>
        /// Raw file name without path or extension. For displaying on tab header.
        /// </summary>
        public string RawFileName
        {
            get { return _rawFileName; }
            private set
            {
                _rawFileName = value;
                RaisePropertyChanged();
            }
        }


        private string _rawFilePath;
        /// <summary>
        /// Full path to the raw file including extension.
        /// </summary>
        public string RawFilePath
        {
            get { return _rawFilePath; }
            set
            {
                LoadingScreenViewModel.IsLoading = true;
                _rawFilePath = value;
                RawFileName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(RawFilePath));
                var extension = Path.GetExtension(value);
                if (extension != null) extension = extension.ToLower();
                // load raw file
                var massSpecDataType = (extension == ".mzml") ? MassSpecDataType.MzMLFile : MassSpecDataType.XCaliburRun;
                Lcms = PbfLcMsRun.GetLcMsRun(_rawFilePath, massSpecDataType, 0, 0);
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
                ToggleShowInstrumentData(_showInstrumentData);
                XicViewModel.Lcms = Lcms;
                SpectrumViewModel.Lcms = Lcms;
                SelectedPrSm.Lcms = Lcms;
                LoadingScreenViewModel.IsLoading = false;
                RaisePropertyChanged();
            }
        }

        private bool _showFeatureMapSplash;
        /// <summary>
        /// Sets/Gets whether the loading screen is currently being shown.
        /// </summary>
        public bool ShowFeatureMapSplash
        {
            get { return _showFeatureMapSplash; }
            set
            {
                _showFeatureMapSplash = value;
                RaisePropertyChanged();
            }
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

        private async void SettingsChanged(SettingsChangedNotification message)
        {
            if (IcParameters.Instance.ShowInstrumentData != _showInstrumentData)
            {
                _showInstrumentData = IcParameters.Instance.ShowInstrumentData;
                await ToggleShowInstrumentData(IcParameters.Instance.ShowInstrumentData);   
            }
        }

        public async Task ToggleShowInstrumentData(bool value)
        {

            await Task.Run(() =>
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
        private bool _showInstrumentData;
        #endregion
    }
}
