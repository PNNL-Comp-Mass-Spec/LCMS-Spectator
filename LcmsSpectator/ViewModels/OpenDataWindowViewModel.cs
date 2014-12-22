using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FeatureMap.DialogServices;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace FeatureMap.ViewModels
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
            OkCommand = new RelayCommand(Ok);
            CancelCommand = new RelayCommand(Cancel);
        }

        public string RawFilePath
        {
            get { return _rawFilePath; }
            set
            {
                _rawFilePath = value;
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

        private void BrowseRawFiles()
        {
            var rawFilePath = _dialogService.OpenFile(".raw", @"Raw Files (*.raw)|*.raw");
            if (!String.IsNullOrEmpty(rawFilePath)) RawFilePath = rawFilePath;
        }

        private void BrowseFeatureFiles()
        {
            const string formatStr = @"TSV Files (*.txt; *tsv)|*.txt;*.tsv";
            var featureFilePath = _dialogService.OpenFile(".txt", formatStr);
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
    }
}
