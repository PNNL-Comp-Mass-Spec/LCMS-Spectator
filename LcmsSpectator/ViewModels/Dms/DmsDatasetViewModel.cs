// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DmsDatasetViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class is a view model for editing a DMS data set.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using LcmsSpectator.Models.Dataset;

namespace LcmsSpectator.ViewModels.Dms
{
    using System;

    using LcmsSpectator.Readers;

    using ReactiveUI;

    /// <summary>
    /// This class is a view model for editing a DMS data set.
    /// </summary>
    public class DmsDatasetViewModel : ReactiveObject
    {
        /// <summary>
        /// The id of the DMS data set.
        /// </summary>
        private int datasetId;

        /// <summary>
        /// The name of the DMS data set.
        /// </summary>
        private string dataset;

        /// <summary>
        /// The name of the type of experiment.
        /// </summary>
        private string experiment;

        /// <summary>
        /// The name of the organism.
        /// </summary>
        private string organism;

        /// <summary>
        /// The type of instrument used.
        /// </summary>
        private string instrument;

        /// <summary>
        /// The date and time that this data set was created.
        /// </summary>
        private DateTime created;

        /// <summary>
        /// The path for the folder that this data set is in.
        /// </summary>
        private string datasetFolderPath;

        /// <summary>
        /// A value indicating whether this dataset has been selected.
        /// </summary>
        private bool selected;

        /// <summary>
        /// Initializes a new instance of the <see cref="DmsDatasetViewModel"/> class. 
        /// </summary>
        /// <param name="datasetInfo">
        /// Existing data set to edit.
        /// </param>
        public DmsDatasetViewModel(DmsLookupUtility.UdtDatasetInfo? datasetInfo = null)
        {
            if (datasetInfo != null)
            {
                this.UdtDatasetInfo = datasetInfo.Value;   
            }
        }

        /// <summary>
        /// Gets or sets the the DMS data set info to edit.
        /// </summary>
        public DmsLookupUtility.UdtDatasetInfo UdtDatasetInfo
        {
            get
            {
                DmsLookupUtility.UdtDatasetInfo udtDatasetInfo;
                udtDatasetInfo.DatasetId = this.DatasetId;
                udtDatasetInfo.Dataset = this.Dataset;
                udtDatasetInfo.Experiment = this.Experiment;
                udtDatasetInfo.Organism = this.Organism;
                udtDatasetInfo.Instrument = this.Instrument;
                udtDatasetInfo.Created = this.Created;
                udtDatasetInfo.DatasetFolderPath = this.DatasetFolderPath;
                return udtDatasetInfo;
            }

            set
            {
                DmsLookupUtility.UdtDatasetInfo datasetInfo = value;
                this.DatasetId = datasetInfo.DatasetId;
                this.Dataset = datasetInfo.Dataset;
                this.Experiment = datasetInfo.Experiment;
                this.Organism = datasetInfo.Organism;
                this.Instrument = datasetInfo.Instrument;
                this.Created = datasetInfo.Created;
                this.DatasetFolderPath = datasetInfo.DatasetFolderPath;
                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the id of the DMS data set.
        /// </summary>
        public int DatasetId
        {
            get { return this.datasetId; }
            set { this.RaiseAndSetIfChanged(ref this.datasetId, value); }
        }

        /// <summary>
        /// Gets or sets the name of the DMS data set.
        /// </summary>
        public string Dataset
        {
            get { return this.dataset; }
            set { this.RaiseAndSetIfChanged(ref this.dataset, value); }
        }

        /// <summary>
        /// Gets or sets the name of the type of experiment.
        /// </summary>
        public string Experiment
        {
            get { return this.experiment; }
            set { this.RaiseAndSetIfChanged(ref this.experiment, value); }
        }

        /// <summary>
        /// Gets or sets the name of the organism.
        /// </summary>
        public string Organism
        {
            get { return this.organism; }
            set { this.RaiseAndSetIfChanged(ref this.organism, value); }
        }

        /// <summary>
        /// Gets or sets the type of instrument used.
        /// </summary>
        public string Instrument
        {
            get { return this.instrument; }
            set { this.RaiseAndSetIfChanged(ref this.instrument, value); }
        }

        /// <summary>
        /// Gets or sets the date and time that this data set was created.
        /// </summary>
        public DateTime Created
        {
            get { return this.created; }
            set { this.RaiseAndSetIfChanged(ref this.created, value); }
        }

        /// <summary>
        /// Gets or sets the path for the folder that this data set is in.
        /// </summary>
        public string DatasetFolderPath
        {
            get { return this.datasetFolderPath; }
            set { this.RaiseAndSetIfChanged(ref this.datasetFolderPath, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this dataset has been selected.
        /// </summary>
        public bool Selected
        {
            get { return this.selected; }
            set { this.RaiseAndSetIfChanged(ref this.selected, value); }
        }

        /// <summary>
        /// Get a list of all raw files associated with the selected data set.
        /// </summary>
        /// <returns>List containing full paths associated with the selected data set.</returns>
        public List<string> GetRawFileNames()
        {
            if (!this.ValidateDataSet())
            {
                return new List<string>();
            }

            var dataSetDirFiles = Directory.GetFiles(this.DatasetFolderPath);
            var rawFileNames = (from filePath in dataSetDirFiles
                                let ext = Path.GetExtension(filePath)
                                where !string.IsNullOrEmpty(ext)
                                let extL = ext.ToLower()
                                where (extL == ".raw" || extL == ".mzml" || extL == ".gz")
                                select filePath).ToList();
            for (int i = 0; i < rawFileNames.Count; i++)
            {
                var pbfFile = this.GetPbfFileName(rawFileNames[i]);
                if (!string.IsNullOrEmpty(pbfFile))
                {
                    rawFileNames[i] = pbfFile;
                }
            }

            return rawFileNames;
        }

        /// <summary>
        /// Get the PBF file (if it exists) for a certain raw file associated with this data set.
        /// </summary>
        /// <param name="rawFilePath">The path of the raw file to find associated PBF files.</param>
        /// <returns>The full path to the PBF file.</returns>
        private string GetPbfFileName(string rawFilePath)
        {
            string pbfFilePath = null;
            if (!this.ValidateDataSet())
            {
                return null;
            }

            var dataSetDirDirectories = Directory.GetDirectories(this.DatasetFolderPath);
            var pbfFolderPath = (from folderPath in dataSetDirDirectories
                                 let folderName = Path.GetFileNameWithoutExtension(folderPath)
                                 where folderName.StartsWith("PBF_Gen")
                                 select folderPath).FirstOrDefault();
            if (!string.IsNullOrEmpty(pbfFolderPath))
            {
                var pbfIndirectionPath = string.Format(@"{0}\{1}.pbf_CacheInfo.txt", pbfFolderPath, Path.GetFileNameWithoutExtension(rawFilePath));
                if (!string.IsNullOrEmpty(pbfIndirectionPath) && File.Exists(pbfIndirectionPath))
                {
                    var lines = File.ReadAllLines(pbfIndirectionPath);
                    if (lines.Length > 0)
                    {
                        pbfFilePath = lines[0];
                    }
                }
            }

            return pbfFilePath;
        }

        /// <summary>
        /// Checks to see if the data set selected is a valid data set.
        /// </summary>
        /// <returns>A value indicating whether the data set selected is valid.</returns>
        public bool ValidateDataSet()
        {
            return !string.IsNullOrEmpty(this.DatasetFolderPath)
                    && Directory.Exists(this.DatasetFolderPath);
        }
    }
}
