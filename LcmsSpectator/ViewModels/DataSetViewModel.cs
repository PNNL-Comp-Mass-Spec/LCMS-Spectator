using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.DialogServices;
using LcmsSpectator.TaskServices;
using LcmsSpectator.Utils;
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
            SelectedPrSm = new PrSmViewModel(messenger);
            XicViewModel = new XicViewModel(_dialogService, TaskServiceFactory.GetTaskServiceLike(taskService1), messenger);
            SpectrumViewModel = new SpectrumViewModel(_dialogService, TaskServiceFactory.GetTaskServiceLike(taskService1), messenger);
            FeatureMapViewModel = new FeatureViewerViewModel(taskService1, messenger);
            ScanViewModel = new ScanViewModel(_dialogService, TaskServiceFactory.GetTaskServiceLike(taskService1), new List<PrSm>(), messenger);
            IonTypeSelectorViewModel = new IonTypeSelectorViewModel(_dialogService, messenger);
            IonListViewModel = new IonListViewModel(_dialogService, taskService1, messenger);

            _showFeatureMapSplash = true;

            OpenFeatureFileCommand = new RelayCommand(OpenFeatureFile);

            CloseCommand = new RelayCommand(() =>
            {
                if (_dialogService.ConfirmationBox(String.Format("Are you sure you would like to close {0}?", RawFileName), ""))
                    Messenger.Default.Send(new DataSetCloseRequest(this));
            });
        }

        #region Public Properties
        public ILcMsRun Lcms { get; private set; }

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


        /// <summary>
        /// Full path to the raw file including extension.
        /// </summary>
        public string RawFilePath
        {
            get { return _rawFilePath; }
            set
            {
                _rawFilePath = value;
                RawFileName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(RawFilePath));
                IsLoading = true;
                // load raw file
                Lcms = PbfLcMsRun.GetLcMsRun(_rawFilePath, MassSpecDataType.XCaliburRun, 0, 0);
                XicViewModel.Lcms = Lcms;
                SpectrumViewModel.Lcms = Lcms;
                // set bounds for shared x axis
                //var maxRt = Math.Max(Lcms.GetElutionTime(Lcms.MaxLcScan), 1.0);
                IsLoading = false;
                RaisePropertyChanged();
            }
        }

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

        /// <summary>
        /// Sets/Gets whether the loading screen is currently being shown.
        /// </summary>
        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        public void OpenFeatureFile()
        {
            var openDataVm = new OpenDataWindowViewModel(new DialogService());
            if (_dialogService.OpenDataWindow(openDataVm))
            {
                ShowFeatureMapSplash = false;
                //ShowLoadingScreen = true;
                var features = FeatureReader.Read(openDataVm.FeatureFilePath);
                FeatureMapViewModel.SetData((LcMsRun)Lcms, features.ToList(), ScanViewModel.FilteredData);
                //ShowLoadingScreen = false;
            }
        }

        #region Private members
        private readonly IMainDialogService _dialogService;

        private string _rawFilePath;
        private string _rawFileName;
        private bool _isLoading;
        private bool _showFeatureMapSplash;
        #endregion
    }
}
