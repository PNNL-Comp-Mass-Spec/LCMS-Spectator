using System;
using System.Reactive.Linq;
using LcmsSpectator.DialogServices;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
    public class OpenDataWindowViewModel: ReactiveObject
    {
        public IReactiveCommand BrowseRawFilesCommand { get; private set; }
        public IReactiveCommand BrowseFeatureFilesCommand { get; private set; }
        public IReactiveCommand BrowseIdFilesCommand { get; private set; }
        public IReactiveCommand OkCommand { get; private set; }
        public IReactiveCommand CancelCommand { get; private set; }

        public bool Status { get; private set; }
        public event EventHandler ReadyToClose;
        public OpenDataWindowViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
            var browseRawFilesCommand = ReactiveCommand.Create();
            browseRawFilesCommand.Subscribe(_ => BrowseRawFiles());
            BrowseRawFilesCommand = browseRawFilesCommand;

            var browseFeatureFilesCommand = ReactiveCommand.Create();
            browseFeatureFilesCommand.Subscribe(_ => BrowseFeatureFiles());
            BrowseFeatureFilesCommand = browseFeatureFilesCommand;

            var browseIdFilesCommand = ReactiveCommand.Create();
            browseIdFilesCommand.Subscribe(_ => BrowseIdFiles());
            BrowseIdFilesCommand = browseIdFilesCommand;

            // Ok button should be enabled if RawFilePath isn't null or empty
            var okCommand =
            ReactiveCommand.Create(
                    this.WhenAnyValue(x => x.RawFilePath).Select(rawFilePath => !String.IsNullOrEmpty(rawFilePath)));
            okCommand.Subscribe(_ => Ok());
            OkCommand = okCommand;

            var cancelCommand = ReactiveCommand.Create();
            cancelCommand.Subscribe(_ => Cancel());
            CancelCommand = cancelCommand;

            Status = false;
        }

        public string RawFilePath
        {
            get { return _rawFilePath; }
            set { this.RaiseAndSetIfChanged(ref _rawFilePath, value); }
        }

        public string FeatureFilePath
        {
            get { return _featureFilePath; }
            set { this.RaiseAndSetIfChanged(ref _featureFilePath, value); }
        }

        public string IdFilePath
        {
            get { return _idFilePath; }
            set { this.RaiseAndSetIfChanged(ref _idFilePath, value); }
        }

        private void BrowseRawFiles()
        {
            var rawFilePath = _dialogService.OpenFile(".raw", @"Raw/MzML Files (*.raw; *.mzML)|*.raw;*.mzML;*.mzML.gz");
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
            const string formatStr = @"Supported Files|*.txt;*.tsv;*.mzId;*.mzId.gz;*.mtdb|TSV Files (*.txt; *tsv)|*.txt;*.tsv|MzId Files (*.mzId[.gz])|*.mzId;*.mzId.gz|MTDB Files (*.mtdb)|*.mtdb";
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
