using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using LcmsSpectator.DialogServices;
using LcmsSpectator.Readers;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
    public class DmsLookupViewModel: ReactiveObject
    {
        public ReactiveList<DmsDatasetViewModel> Datasets { get; private set; } 
        public ReactiveList<DmsJobViewModel> Jobs { get; private set; } 

        public int NumberOfWeeks { get; set; }
        public string DatasetFilter { get; set; }

        public IReactiveCommand LookupCommand { get; private set; }
        public IReactiveCommand OpenCommand { get; private set; }
        public IReactiveCommand CloseCommand { get; private set; }

        public bool Status { get; private set; }
        public event EventHandler ReadyToClose;

        public DmsLookupViewModel(IDialogService dialogService)
        {
            IsFirstSearch = true;
            Status = false;
            _dialogService = dialogService;
            NumberOfWeeks = 2;
            Datasets = new ReactiveList<DmsDatasetViewModel>();
            Jobs = new ReactiveList<DmsJobViewModel>();
            _dmsLookupUtility = new DmsLookupUtility();

            var lookUpCommand = ReactiveCommand.Create();
            lookUpCommand.Subscribe(_ => Lookup());
            LookupCommand = lookUpCommand;

            var openCommand = ReactiveCommand.Create(this.WhenAnyValue(x => x.SelectedDataset, x => x.SelectedJob)
                                                     .Select(x => x.Item1 != null
                                                               && !String.IsNullOrEmpty(x.Item1.DatasetFolderPath) 
                                                               && x.Item2 != null
                                                               && !String.IsNullOrEmpty(x.Item2.JobFolderPath)));
            openCommand.Subscribe(_ => Open());
            OpenCommand = openCommand;

            var closeCommand = ReactiveCommand.Create();
            closeCommand.Subscribe(_ => Close());
            CloseCommand = closeCommand;

            SelectedDataset = new DmsDatasetViewModel();
            SelectedJob = new DmsJobViewModel();

            this.WhenAnyValue(x => x.SelectedDataset)
                .Subscribe(x =>
                {
                    Jobs.Clear();
                    if (_selectedDataset == null) return;
                    var jobMap = _dmsLookupUtility.GetJobsByDataset(new List<DmsLookupUtility.UdtDatasetInfo> { _selectedDataset.UdtDatasetInfo });
                    List<DmsLookupUtility.UdtJobInfo> jobs;
                    if (jobMap.TryGetValue(_selectedDataset.DatasetId, out jobs))
                    {
                        foreach (var job in jobs)
                        {
                            Jobs.Add(new DmsJobViewModel(job));
                        }
                    }
                    if (Jobs.Count > 0) SelectedJob = Jobs[0];
                });

            this.WhenAnyValue(x => x.SelectedDataset, x => x.IsFirstSearch)
                .Select(x => (x.Item1 == null || String.IsNullOrEmpty(x.Item1.DatasetFolderPath) && !x.Item2))
                .ToProperty(this, x => x.IsNoResultsShown, out _isNoResultsShown);
        }

        private DmsDatasetViewModel _selectedDataset;
        public DmsDatasetViewModel SelectedDataset
        {
            get { return _selectedDataset; }
            set { this.RaiseAndSetIfChanged(ref _selectedDataset, value); }
        }

        private DmsJobViewModel _selectedJob;
        public DmsJobViewModel SelectedJob
        {
            get { return _selectedJob; }
            set { this.RaiseAndSetIfChanged(ref _selectedJob, value); }
        }

        private readonly ObservableAsPropertyHelper<bool> _isNoResultsShown;
        public bool IsNoResultsShown
        {
            get { return _isNoResultsShown.Value; }
        }

        private bool _isFirstSearch;
        private bool IsFirstSearch
        {
            get { return _isFirstSearch; }
            set { this.RaiseAndSetIfChanged(ref _isFirstSearch, value); }
        }

        /// <summary>
        /// Lookup datasets and jobs for the past NumberOfWeeks with a filter given by DatasetFilter
        /// </summary>
        private void Lookup()
        {
            if (NumberOfWeeks < 1)
            {
                _dialogService.MessageBox("Number of weeks must be greater than 0.");
                return;
            }

            if (string.IsNullOrWhiteSpace(DatasetFilter))
            {
                _dialogService.MessageBox("Please enter a dataset filter.");
                return;
            }

            IsFirstSearch = false;

            Datasets.Clear();
            var dataSets =_dmsLookupUtility.GetDatasets(NumberOfWeeks, DatasetFilter);
            foreach (var dataset in dataSets) Datasets.Add(new DmsDatasetViewModel(dataset.Value));
            if (Datasets.Count > 0) SelectedDataset = Datasets[0];
        }

        /// <summary>
        /// Selected data set and close window
        /// </summary>
        private void Open()
        {
            Status = true;
            if (ReadyToClose != null) ReadyToClose(this, EventArgs.Empty);
        }

        /// <summary>
        /// Close window without opening anything
        /// </summary>
        private void Close()
        {
            Status = false;
            if (ReadyToClose != null) ReadyToClose(this, EventArgs.Empty);
        }

        private readonly DmsLookupUtility _dmsLookupUtility;
        private readonly IDialogService _dialogService;
    }
}
