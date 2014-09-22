using System;
using GalaSoft.MvvmLight;
using LcmsSpectatorModels.Readers;

namespace LcmsSpectator.ViewModels
{
    public class DmsJobViewModel: ViewModelBase
    {

        public DmsJobViewModel(DmsLookupUtility.UdtJobInfo jobInfo)
        {
            Job = jobInfo.Job;
            DatasetId = jobInfo.DatasetId;
            Tool = jobInfo.Tool;
            Completed = jobInfo.Completed;
            JobFolderPath = jobInfo.JobFolderPath;
            ParameterFile = jobInfo.ParameterFile;
            SettingsFile = jobInfo.SettingsFile;
            ProteinCollection = jobInfo.ProteinCollection;
            OrganismDb = jobInfo.OrganismDb;
        }

        public DmsJobViewModel()
        {
            
        }

        public DmsLookupUtility.UdtJobInfo UdtJobInfo
        {
            get
            {
                DmsLookupUtility.UdtJobInfo jobInfo;
                jobInfo.Job = Job;
                jobInfo.DatasetId = DatasetId;
                jobInfo.Tool = Tool;
                jobInfo.Completed = Completed;
                jobInfo.JobFolderPath = JobFolderPath;
                jobInfo.ParameterFile = ParameterFile;
                jobInfo.SettingsFile = SettingsFile;
                jobInfo.ProteinCollection = ProteinCollection;
                jobInfo.OrganismDb = OrganismDb;
                return jobInfo;
            }
            set
            {
                DmsLookupUtility.UdtJobInfo jobInfo = value;
                Job = jobInfo.Job;
                DatasetId = jobInfo.DatasetId;
                Tool = jobInfo.Tool;
                Completed = jobInfo.Completed;
                JobFolderPath = jobInfo.JobFolderPath;
                ParameterFile = jobInfo.ParameterFile;
                SettingsFile = jobInfo.SettingsFile;
                ProteinCollection = jobInfo.ProteinCollection;
                OrganismDb = jobInfo.OrganismDb;
                RaisePropertyChanged();
            }
        }

        public int Job
        {
            get { return _job; }
            set
            {
                _job = value;
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

        public string Tool
        {
            get { return _tool; }
            set
            {
                _tool = value;
                RaisePropertyChanged();
            }
        }

        public DateTime Completed
        {
            get { return _completed; }
            set
            {
                _completed = value;
                RaisePropertyChanged();
            }
        }

        public string JobFolderPath
        {
            get { return _jobFolderPath; }
            set
            {
                _jobFolderPath = value;
                RaisePropertyChanged();
            }
        }

        public string ParameterFile
        {
            get { return _parameterFile; }
            set
            {
                _parameterFile = value;
                RaisePropertyChanged();
            }
        }

        public string SettingsFile
        {
            get { return _settingsFile; }
            set
            {
                _settingsFile = value;
                RaisePropertyChanged();
            }
        }

        public string ProteinCollection
        {
            get { return _proteinCollection; }
            set
            {
                _proteinCollection = value;
                RaisePropertyChanged();
            }
        }

        public string OrganismDb
        {
            get { return _organismDb; }
            set
            {
                _organismDb = value;
                RaisePropertyChanged();
            }
        }
        
        private int _job;
        private int _datasetId;
        private string _tool;
        private DateTime _completed;
        private string _jobFolderPath;
        private string _parameterFile;
        private string _settingsFile;
        private string _proteinCollection;
        private string _organismDb;
    }
}
