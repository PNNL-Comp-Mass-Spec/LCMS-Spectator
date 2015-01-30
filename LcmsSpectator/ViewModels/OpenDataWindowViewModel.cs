using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using LcmsSpectator.DialogServices;

namespace LcmsSpectator.ViewModels
{
    public class OpenDataWindowViewModel: ViewModelBase
    {
        public RelayCommand BrowseRawFilesCommand { get; private set; }
        public RelayCommand BrowseFeatureFilesCommand { get; private set; }
        public RelayCommand BrowseIdFilesCommand { get; private set; }

        public bool Status { get; private set; }
        public event EventHandler ReadyToClose;
        public RelayCommand OkCommand { get; private set; }
        public RelayCommand CancelCommand { get; private set; }
        public OpenDataWindowViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
            BrowseRawFilesCommand = new RelayCommand(BrowseRawFiles);
            BrowseFeatureFilesCommand = new RelayCommand(BrowseFeatureFiles);
            BrowseIdFilesCommand = new RelayCommand(BrowseIdFiles);
            Status = false;
            OpenEnabled = false;
            OkCommand = new RelayCommand(Ok);
            CancelCommand = new RelayCommand(Cancel);
        }

        public string RawFilePath
        {
            get { return _rawFilePath; }
            set
            {
                _rawFilePath = value;
                if (!String.IsNullOrEmpty(_rawFilePath))
                {
                    OpenEnabled = true;
                }
                else
                {
                    OpenEnabled = false;
                }
                RaisePropertyChanged();
            }
        }

        public string FeatureFilePath
        {
            get { return _featureFilePath; }
            set
            {
                _featureFilePath = value;
                RaisePropertyChanged();
            }
        }

        public string IdFilePath
        {
            get { return _idFilePath; }
            set
            {
                _idFilePath = value;
                RaisePropertyChanged();
            }
        }

        public bool OpenEnabled
        {
            get { return _openEnabled; }
            set
            {
                _openEnabled = value;
                RaisePropertyChanged();
            }
        }

        private void BrowseRawFiles()
        {
            var rawFilePath = _dialogService.OpenFile(".raw", @"Raw/MzML Files (*.raw; *.mzML)|*.raw;*.mzML");
            if (!String.IsNullOrEmpty(rawFilePath)) RawFilePath = rawFilePath;
        }

        private void BrowseFeatureFiles()
        {
            const string formatStr = @"Ms1FT Files (*.ms1ft)|*.ms1ft";
            var featureFilePath = _dialogService.OpenFile(".ms1ft", formatStr);
            if (!String.IsNullOrEmpty(featureFilePath)) FeatureFilePath = featureFilePath;
        }

        private void BrowseIdFiles()
        {
            const string formatStr = @"TSV Files (*.txt; *tsv)|*.txt;*.tsv|MzId Files (*.mzId[.gz])|*.mzId;*.mzId.gz|MTDB Files (*.mtdb)|*.mtdb";
            var idFilePath = _dialogService.OpenFile(".txt", formatStr);
            if (!String.IsNullOrEmpty(idFilePath)) IdFilePath = idFilePath;
        }

        private void Ok()
        {
            Status = true;
            if (ReadyToClose != null) ReadyToClose(this, EventArgs.Empty);
        }

        private void Cancel()
        {
            Status = false;
            if (ReadyToClose != null) ReadyToClose(this, EventArgs.Empty);
        }

        private readonly IDialogService _dialogService;
        private string _rawFilePath;
        private string _featureFilePath;
        private string _idFilePath;
        private bool _openEnabled;
    }
}
