using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using LcmsSpectator.DialogServices;

namespace LcmsSpectator.ViewModels
{
    public class SelectDataSetViewModel: ViewModelBase
    {
        public ObservableCollection<DataSetViewModel> DataSets { get; private set; }

        public DataSetViewModel SelectedDataSet
        {
            get { return _selectedDataSet; }
            set
            {
                _selectedDataSet = value;
                if (!String.IsNullOrEmpty(_rawFilePath) || SelectedDataSet != null)
                {
                    SelectEnabled = true;
                }
                else SelectEnabled = false;
                RaisePropertyChanged();
            }
        }

        public string RawFilePath
        {
            get { return _rawFilePath; }
            set
            {
                _rawFilePath = value;
                if (!String.IsNullOrEmpty(_rawFilePath) || SelectedDataSet != null)
                {
                    SelectEnabled = true;
                }
                else SelectEnabled = false;
                RaisePropertyChanged();
            }
        }

        public bool SelectEnabled
        {
            get { return _selectEnabled; }
            set
            {
                _selectEnabled = value;
                RaisePropertyChanged();
            }
        }

        public RelayCommand BrowseRawFilesCommand { get; private set; }
        public RelayCommand ClearRawFilesCommand { get; private set; }

        public RelayCommand OkCommand { get; private set; }
        public RelayCommand CancelCommand { get; private set; }

        public bool Status { get; private set; }
        public event EventHandler ReadyToClose;
        private string _rawFilePath;

        public SelectDataSetViewModel(IDialogService dialogService, IEnumerable<DataSetViewModel> dataSets)
        {
            _dialogService = dialogService;
            DataSets = new ObservableCollection<DataSetViewModel>(dataSets);

            BrowseRawFilesCommand = new RelayCommand(BrowseRawFiles);
            ClearRawFilesCommand = new RelayCommand(ClearRawFiles);

            SelectEnabled = false;

            OkCommand = new RelayCommand(Ok);
            CancelCommand = new RelayCommand(Cancel);

            Status = false;
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
        private bool _selectEnabled;
    }
}
