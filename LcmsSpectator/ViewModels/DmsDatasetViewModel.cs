using System;
using LcmsSpectatorModels.Readers;

namespace LcmsSpectator.ViewModels
{
    public class DmsDatasetViewModel: ViewModelBase
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
                OnPropertyChanged("UdtDatasetInfo");
            }
        }

        public int DatasetId
        {
            get { return _datasetId; }
            set
            {
                _datasetId = value;
                OnPropertyChanged("DatasetId");
            }
        }

        public string Dataset
        {
            get { return _dataset; }
            set
            {
                _dataset = value;
                OnPropertyChanged("Dataset");
            }
        }

        public string Experiment
        {
            get { return _experiment; }
            set
            {
                _experiment = value;
                OnPropertyChanged("Experiment");
            }
        }

        public string Organism
        {
            get { return _organism; }
            set
            {
                _organism = value;
                OnPropertyChanged("Organism");
            }
        }

        public string Instrument
        {
            get { return _instrument; }
            set
            {
                _instrument = value;
                OnPropertyChanged("Instrument");
            }
        }

        public DateTime Created
        {
            get { return _created; }
            set
            {
                _created = value;
                OnPropertyChanged("Created");
            }
        }

        public string DatasetFolderPath
        {
            get { return _datasetFolderPath; }
            set
            {
                _datasetFolderPath = value;
                OnPropertyChanged("DatasetFolderPath");
            }
        }
        
        private int _datasetId;
        private string _dataset;
        private string _experiment;
        private string _organism;
        private string _instrument;
        private DateTime _created;
        private string _datasetFolderPath;
    }
}
