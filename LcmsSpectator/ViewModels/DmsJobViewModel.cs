using System;
using LcmsSpectator.Readers;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
    public class DmsJobViewModel: ReactiveObject
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
                this.RaisePropertyChanged();
            }
        }

        private int _job;
        public int Job
        {
            get { return _job; }
            set { this.RaiseAndSetIfChanged(ref _job, value); }
        }

        private int _datasetId;
        public int DatasetId
        {
            get { return _datasetId; }
            set { this.RaiseAndSetIfChanged(ref _datasetId, value); }
        }

        private string _tool;
        public string Tool
        {
            get { return _tool; }
            set { this.RaiseAndSetIfChanged(ref _tool, value); }
        }

        private DateTime _completed;
        public DateTime Completed
        {
            get { return _completed; }
            set { this.RaiseAndSetIfChanged(ref _completed, value); }
        }

        private string _jobFolderPath;
        public string JobFolderPath
        {
            get { return _jobFolderPath; }
            set { this.RaiseAndSetIfChanged(ref _jobFolderPath, value); }
        }

        private string _parameterFile;
        public string ParameterFile
        {
            get { return _parameterFile; }
            set { this.RaiseAndSetIfChanged(ref _parameterFile, value); }
        }

        private string _settingsFile;
        public string SettingsFile
        {
            get { return _settingsFile; }
            set { this.RaiseAndSetIfChanged(ref _settingsFile, value); }
        }

        private string _proteinCollection;
        public string ProteinCollection
        {
            get { return _proteinCollection; }
            set { this.RaiseAndSetIfChanged(ref _proteinCollection, value); }
        }

        private string _organismDb;
        public string OrganismDb
        {
            get { return _organismDb; }
            set { this.RaiseAndSetIfChanged(ref _organismDb, value); }
        }
    }
}
