// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DmsJobViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class is a view model for editing DMS job info.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.IO;
using System.Linq;
using LcmsSpectator.Models;
using LcmsSpectator.Models.Dataset;
using LcmsSpectator.Models.DTO;

namespace LcmsSpectator.ViewModels.Dms
{
    using System;

    using LcmsSpectator.Readers;

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
        /// A value indicating whether this job has been selected.
        /// </summary>
        private bool selected;

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
                this.UdtJobInfo = jobInfo.Value;   
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
                jobInfo.Job = this.Job;
                jobInfo.DatasetId = this.DatasetId;
                jobInfo.Tool = this.Tool;
                jobInfo.Completed = this.Completed;
                jobInfo.JobFolderPath = this.JobFolderPath;
                jobInfo.ParameterFile = this.ParameterFile;
                jobInfo.SettingsFile = this.SettingsFile;
                jobInfo.ProteinCollection = this.ProteinCollection;
                jobInfo.OrganismDb = this.OrganismDb;
                return jobInfo;
            }

            set
            {
                DmsLookupUtility.UdtJobInfo jobInfo = value;
                this.Job = jobInfo.Job;
                this.DatasetId = jobInfo.DatasetId;
                this.Tool = jobInfo.Tool;
                this.Completed = jobInfo.Completed;
                this.JobFolderPath = jobInfo.JobFolderPath;
                this.ParameterFile = jobInfo.ParameterFile;
                this.SettingsFile = jobInfo.SettingsFile;
                this.ProteinCollection = jobInfo.ProteinCollection;
                this.OrganismDb = jobInfo.OrganismDb;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the ID of the DMS job.
        /// </summary>
        public int Job
        {
            get { return this.job; }
            set { this.RaiseAndSetIfChanged(ref this.job, value); }
        }

        /// <summary>
        /// Gets or sets the ID of the data set that this job is associated with.
        /// </summary>
        public int DatasetId
        {
            get { return this.datasetId; }
            set { this.RaiseAndSetIfChanged(ref this.datasetId, value); }
        }

        /// <summary>
        /// Gets or sets the name of the tool used for this job.
        /// </summary>
        public string Tool
        {
            get { return this.tool; }
            set { this.RaiseAndSetIfChanged(ref this.tool, value); }
        }

        /// <summary>
        /// Gets or sets the date and time that this job was completed.
        /// </summary>
        public DateTime Completed
        {
            get { return this.completed; }
            set { this.RaiseAndSetIfChanged(ref this.completed, value); }
        }

        /// <summary>
        /// Gets or sets the path for the folder that this job is in.
        /// </summary>
        public string JobFolderPath
        {
            get { return this.jobFolderPath; }
            set { this.RaiseAndSetIfChanged(ref this.jobFolderPath, value); }
        }

        /// <summary>
        /// Gets or sets the path for the parameter file for this job.
        /// </summary>
        public string ParameterFile
        {
            get { return this.parameterFile; }
            set { this.RaiseAndSetIfChanged(ref this.parameterFile, value); }
        }

        /// <summary>
        /// Gets or sets the path for the settings file for this job.
        /// </summary>
        public string SettingsFile
        {
            get { return this.settingsFile; }
            set { this.RaiseAndSetIfChanged(ref this.settingsFile, value); }
        }

        /// <summary>
        /// Gets or sets the protein collection for this job.
        /// </summary>
        public string ProteinCollection
        {
            get { return this.proteinCollection; }
            set { this.RaiseAndSetIfChanged(ref this.proteinCollection, value); }
        }

        /// <summary>
        /// Gets or sets the name of the organism database used for this job.
        /// </summary>
        public string OrganismDb
        {
            get { return this.organismDb; }
            set { this.RaiseAndSetIfChanged(ref this.organismDb, value); }
        }

        /// <summary>
        /// Gets a value indicating whether this job has been selected.
        /// </summary>
        public bool Selected
        {
            get { return this.selected; }
            set { this.RaiseAndSetIfChanged(ref this.selected, value); }
        }

        /// <summary>
        /// Get the ID file associated with the selected job.
        /// </summary>
        /// <returns>Full path of the ID file associated with the selected job.</returns>
        public string GetIdFileName()
        {
            if (!this.ValidateJob())
            {
                return null;
            }

            var jobDir = Directory.GetFiles(this.JobFolderPath);
            return (from idFp in jobDir
                    let ext = Path.GetExtension(idFp)
                    where ext == ".mzid" || ext == ".gz" || ext == ".zip"
                    select idFp).FirstOrDefault();
        }

        /// <summary>
        /// Get the feature file associated with the selected job.
        /// </summary>
        /// <returns>Full path of the feature file associated with the selected job.</returns>
        public string GetFeatureFileName()
        {
            if (!this.ValidateJob())
            {
                return null;
            }

            var jobDir = Directory.GetFiles(this.JobFolderPath);
            return (from idFp in jobDir let ext = Path.GetExtension(idFp) where ext == ".ms1ft" select idFp).FirstOrDefault();
        }

        /// <summary>
        /// Checks to see if the data set selected is a valid job.
        /// </summary>
        /// <returns>A value indicating whether the job selected is valid.</returns>
        public bool ValidateJob()
        {
            return !string.IsNullOrEmpty(this.JobFolderPath) && Directory.Exists(this.JobFolderPath);
        }

        /// <summary>
        /// Get the tool type for the selected job
        /// </summary>
        /// <returns>The tool type used for the selected job.</returns>
        public ToolType? GetTool()
        {
            if (!this.ValidateJob())
            {
                return null;
            }

            ToolType toolType;
            switch (this.Tool)
            {
                case "MS-GF+":
                    toolType = ToolType.MsgfPlus;
                    break;
                case "MSPathFinder":
                    toolType = ToolType.MsPathFinder;
                    break;
                default:
                    toolType = ToolType.Other;
                    break;
            }

            return toolType;
        }
    }
}
