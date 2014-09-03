using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using LcmsSpectator.DialogServices;
using LcmsSpectatorModels.Readers;

namespace LcmsSpectator.ViewModels
{
    public class DmsLookupViewModel: ViewModelBase
    {
        public ObservableCollection<DmsDatasetViewModel> Datasets { get; private set; } 
        public ObservableCollection<DmsJobViewModel> Jobs { get; private set; } 

        public int NumberOfWeeks { get; set; }
        public string DatasetFilter { get; set; }

        public DelegateCommand LookupCommand { get; private set; }
        public DelegateCommand OpenCommand { get; private set; }
        public DelegateCommand CloseCommand { get; private set; }

        public bool Status { get; private set; }
        public event EventHandler ReadyToClose;

        public DmsLookupViewModel(IDialogService dialogService)
        {
            Status = false;
            _dialogService = dialogService;
            NumberOfWeeks = 2;
            Datasets = new ObservableCollection<DmsDatasetViewModel>();
            Jobs = new ObservableCollection<DmsJobViewModel>();
            _dmsLookupUtility = new DmsLookupUtility();
            LookupCommand = new DelegateCommand(Lookup);
            OpenCommand = new DelegateCommand(Open);
            CloseCommand = new DelegateCommand(Close);
            SelectedDataset = new DmsDatasetViewModel();
            SelectedJob = new DmsJobViewModel();
        }

        public DmsDatasetViewModel SelectedDataset
        {
            get { return _selectedDataset; }
            set
            {
                _selectedDataset = value;
                Jobs.Clear();
                var jobMap = _dmsLookupUtility.GetJobsByDataset(new List<DmsLookupUtility.UdtDatasetInfo>{ _selectedDataset.UdtDatasetInfo});
                List<DmsLookupUtility.UdtJobInfo> jobs;
                if (jobMap.TryGetValue(_selectedDataset.DatasetId, out jobs))
                {
                    foreach (var job in jobs)
                    {
                        Jobs.Add(new DmsJobViewModel(job));
                    }
                }
                if (Jobs.Count > 0) SelectedJob = Jobs[0];
                OnPropertyChanged("SelectedDataset");
            }
        }

        public DmsJobViewModel SelectedJob
        {
            get { return _selectedJob; }
            set
            {
                _selectedJob = value;
                OnPropertyChanged("SelectedJob");
            }
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
        private DmsDatasetViewModel _selectedDataset;
        private DmsJobViewModel _selectedJob;
    }
}
