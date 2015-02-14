using System;
using LcmsSpectator.Readers;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
    public class DmsDatasetViewModel: ReactiveObject
    {

        public DmsDatasetViewModel(DmsLookupUtility.UdtDatasetInfo datasetInfo)
        {
            DatasetId = datasetInfo.DatasetId;
            Dataset = datasetInfo.Dataset;
            Experiment = datasetInfo.Experiment;
            Organism = datasetInfo.Organism;
            Instrument = datasetInfo.Instrument;
            Created = datasetInfo.Created;
            DatasetFolderPath = datasetInfo.DatasetFolderPath;
        }

        public DmsDatasetViewModel()
        {
            
        }

        public DmsLookupUtility.UdtDatasetInfo UdtDatasetInfo
        {
            get
            {
                DmsLookupUtility.UdtDatasetInfo udtDatasetInfo;
                udtDatasetInfo.DatasetId = DatasetId;
                udtDatasetInfo.Dataset = Dataset;
                udtDatasetInfo.Experiment = Experiment;
                udtDatasetInfo.Organism = Organism;
                udtDatasetInfo.Instrument = Instrument;
                udtDatasetInfo.Created = Created;
                udtDatasetInfo.DatasetFolderPath = DatasetFolderPath;
                return udtDatasetInfo;
            }
            set
            {
                DmsLookupUtility.UdtDatasetInfo datasetInfo = value;
                DatasetId = datasetInfo.DatasetId;
                Dataset = datasetInfo.Dataset;
                Experiment = datasetInfo.Experiment;
                Organism = datasetInfo.Organism;
                Instrument = datasetInfo.Instrument;
                Created = datasetInfo.Created;
                DatasetFolderPath = datasetInfo.DatasetFolderPath;
                this.RaisePropertyChanged();
            }
        }

        private int _datasetId;
        public int DatasetId
        {
            get { return _datasetId; }
            set { this.RaiseAndSetIfChanged(ref _datasetId, value); }
        }

        private string _dataset;
        public string Dataset
        {
            get { return _dataset; }
            set { this.RaiseAndSetIfChanged(ref _dataset, value); }
        }

        private string _experiment;
        public string Experiment
        {
            get { return _experiment; }
            set { this.RaiseAndSetIfChanged(ref _experiment, value); }
        }

        private string _organism;
        public string Organism
        {
            get { return _organism; }
            set { this.RaiseAndSetIfChanged(ref _organism, value); }
        }

        private string _instrument;
        public string Instrument
        {
            get { return _instrument; }
            set { this.RaiseAndSetIfChanged(ref _instrument, value); }
        }

        private DateTime _created;
        public DateTime Created
        {
            get { return _created; }
            set { this.RaiseAndSetIfChanged(ref _created, value); }
        }

        private string _datasetFolderPath;
        public string DatasetFolderPath
        {
            get { return _datasetFolderPath; }
            set { this.RaiseAndSetIfChanged(ref _datasetFolderPath, value); }
        }
    }
}
