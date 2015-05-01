// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DmsDatasetViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class is a view model for editing a DMS data set.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

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
    }
}
