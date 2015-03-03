using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using LcmsSpectator.DialogServices;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
    public class SelectDataSetViewModel: ReactiveObject
    {
        public ReactiveList<DataSetViewModel> DataSets { get; private set; }

        public IReactiveCommand BrowseRawFilesCommand { get; private set; }
        public IReactiveCommand ClearRawFilesCommand { get; private set; }
        public IReactiveCommand OkCommand { get; private set; }
        public IReactiveCommand CancelCommand { get; private set; }

        public bool Status { get; private set; }
        public event EventHandler ReadyToClose;
        private string _rawFilePath;

        public SelectDataSetViewModel(IDialogService dialogService, IEnumerable<DataSetViewModel> dataSets)
        {
            _dialogService = dialogService;
            DataSets = new ReactiveList<DataSetViewModel>(dataSets);

            var browseRawFilesCommand = ReactiveCommand.Create();
            browseRawFilesCommand.Subscribe(_ => BrowseRawFiles());
            BrowseRawFilesCommand = browseRawFilesCommand;

            var clearRawFilesCommand = ReactiveCommand.Create();
            clearRawFilesCommand.Subscribe(_ => ClearRawFiles());
            ClearRawFilesCommand = clearRawFilesCommand;
            
            // Ok command is only available if RawFilePath isn't empty or SelectedDataSet isn't null
            var okCommand = ReactiveCommand.Create(
                        this.WhenAnyValue(x => x.RawFilePath, x => x.SelectedDataSet, x => x.RawPathSelected, x => x.DatasetSelected)
                            .Select(p => ((p.Item3 && !String.IsNullOrEmpty(p.Item1)) || (p.Item4 && p.Item2 != null))));
            okCommand.Subscribe(_ => Ok());
            OkCommand = okCommand;

            var cancelCommand = ReactiveCommand.Create();
            cancelCommand.Subscribe(_ => Cancel());
            CancelCommand = cancelCommand;

            RawPathSelected = true;
            DatasetSelected = false;

            Status = false;
        }

        public DataSetViewModel SelectedDataSet
        {
            get { return _selectedDataSet; }
            set { this.RaiseAndSetIfChanged(ref _selectedDataSet, value); }
        }

        public string RawFilePath
        {
            get { return _rawFilePath; }
            set { this.RaiseAndSetIfChanged(ref _rawFilePath, value); }
        }

        private bool _datasetSelected;
        public bool DatasetSelected
        {
            get { return _datasetSelected; }
            set { this.RaiseAndSetIfChanged(ref _datasetSelected, value); }
        }

        private bool _rawPathSelected;
        public bool RawPathSelected
        {
            get { return _rawPathSelected; }
            set { this.RaiseAndSetIfChanged(ref _rawPathSelected, value); }
        }

        private void BrowseRawFiles()
        {
            var rawFilePath = _dialogService.OpenFile(".raw", @"Raw Files (*.raw)|*.raw|MzMl Files (*.mzMl)|*.mzMl");
            if (!String.IsNullOrEmpty(rawFilePath)) RawFilePath = rawFilePath;
        }

        private void ClearRawFiles()
        {
            RawFilePath = "";
        }

        public void Ok()
        {
            if (String.IsNullOrEmpty(RawFilePath) && SelectedDataSet == null)
            {
                _dialogService.MessageBox("Please select data set or new raw/mzml file to open.");
                return;
            }

            Status = true;
            if (ReadyToClose != null) ReadyToClose(this, EventArgs.Empty);
        }

        public void Cancel()
        {
            Status = false;
            if (ReadyToClose != null) ReadyToClose(this, EventArgs.Empty);
        }

        private readonly IDialogService _dialogService;
        private DataSetViewModel _selectedDataSet;
    }
}
