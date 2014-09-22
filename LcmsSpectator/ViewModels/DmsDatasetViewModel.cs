using System;
using GalaSoft.MvvmLight;
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
                RaisePropertyChanged();
            }
        }

        public int DatasetId
        {
            get { return _datasetId; }
            set
            {
                _datasetId = value;
                RaisePropertyChanged();
            }
        }

        public string Dataset
        {
            get { return _dataset; }
            set
            {
                _dataset = value;
                RaisePropertyChanged();
            }
        }

        public string Experiment
        {
            get { return _experiment; }
            set
            {
                _experiment = value;
                RaisePropertyChanged();
            }
        }

        public string Organism
        {
            get { return _organism; }
            set
            {
                _organism = value;
                RaisePropertyChanged();
            }
        }

        public string Instrument
        {
            get { return _instrument; }
            set
            {
                _instrument = value;
                RaisePropertyChanged();
            }
        }

        public DateTime Created
        {
            get { return _created; }
            set
            {
                _created = value;
                RaisePropertyChanged();
            }
        }

        public string DatasetFolderPath
        {
            get { return _datasetFolderPath; }
            set
            {
                _datasetFolderPath = value;
                RaisePropertyChanged();
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
