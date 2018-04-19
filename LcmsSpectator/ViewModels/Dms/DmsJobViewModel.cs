// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DmsJobViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class is a view model for editing DMS job info.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels.Dms
{
    using System;

    using Readers;

    using ReactiveUI;

    /// <summary>
    /// This class is a view model for editing DMS job info.
    /// </summary>
    public class DmsJobViewModel : ReactiveObject
    {
        /// <summary>
        /// The ID of the DMS job.
        /// </summary>
        private int job;

        /// <summary>
        /// The ID of the data set that this job is associated with.
        /// </summary>
        private int datasetId;

        /// <summary>
        /// The name of the tool used for this job.
        /// </summary>
        private string tool;

        /// <summary>
        /// The date and time that this job was completed.
        /// </summary>
        private DateTime completed;

        /// <summary>
        /// The path for the folder that this job is in.
        /// </summary>
        private string jobFolderPath;

        /// <summary>
        /// The path for the parameter file for this job.
        /// </summary>
        private string parameterFile;

        /// <summary>
        /// The path for the settings file for this job.
        /// </summary>
        private string settingsFile;

        /// <summary>
        /// The protein collection for this job.
        /// </summary>
        private string proteinCollection;

        /// <summary>
        /// The name of the organism database used for this job.
        /// </summary>
        private string organismDb;

        /// <summary>
        /// Initializes a new instance of the <see cref="DmsJobViewModel"/> class.
        /// </summary>
        /// <param name="jobInfo">
        /// Existing job to edit.
        /// </param>
        public DmsJobViewModel(DmsLookupUtility.UdtJobInfo? jobInfo = null)
        {
            if (jobInfo != null)
            {
                UdtJobInfo = jobInfo.Value;
            }
        }

        /// <summary>
        /// Gets or sets the the DMS job info to edit.
        /// </summary>
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
                var jobInfo = value;
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

        /// <summary>
        /// Gets or sets the ID of the DMS job.
        /// </summary>
        public int Job
        {
            get => job;
            set => this.RaiseAndSetIfChanged(ref job, value);
        }

        /// <summary>
        /// Gets or sets the ID of the data set that this job is associated with.
        /// </summary>
        public int DatasetId
        {
            get => datasetId;
            set => this.RaiseAndSetIfChanged(ref datasetId, value);
        }

        /// <summary>
        /// Gets or sets the name of the tool used for this job.
        /// </summary>
        public string Tool
        {
            get => tool;
            set => this.RaiseAndSetIfChanged(ref tool, value);
        }

        /// <summary>
        /// Gets or sets the date and time that this job was completed.
        /// </summary>
        public DateTime Completed
        {
            get => completed;
            set => this.RaiseAndSetIfChanged(ref completed, value);
        }

        /// <summary>
        /// Gets or sets the path for the folder that this job is in.
        /// </summary>
        public string JobFolderPath
        {
            get => jobFolderPath;
            set => this.RaiseAndSetIfChanged(ref jobFolderPath, value);
        }

        /// <summary>
        /// Gets or sets the path for the parameter file for this job.
        /// </summary>
        public string ParameterFile
        {
            get => parameterFile;
            set => this.RaiseAndSetIfChanged(ref parameterFile, value);
        }

        /// <summary>
        /// Gets or sets the path for the settings file for this job.
        /// </summary>
        public string SettingsFile
        {
            get => settingsFile;
            set => this.RaiseAndSetIfChanged(ref settingsFile, value);
        }

        /// <summary>
        /// Gets or sets the protein collection for this job.
        /// </summary>
        public string ProteinCollection
        {
            get => proteinCollection;
            set => this.RaiseAndSetIfChanged(ref proteinCollection, value);
        }

        /// <summary>
        /// Gets or sets the name of the organism database used for this job.
        /// </summary>
        public string OrganismDb
        {
            get => organismDb;
            set => this.RaiseAndSetIfChanged(ref organismDb, value);
        }
    }
}
