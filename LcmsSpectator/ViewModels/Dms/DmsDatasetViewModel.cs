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

    using Readers;

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
                UdtDatasetInfo = datasetInfo.Value;
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
                var datasetInfo = value;
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

        /// <summary>
        /// Gets or sets the id of the DMS data set.
        /// </summary>
        public int DatasetId
        {
            get => datasetId;
            set => this.RaiseAndSetIfChanged(ref datasetId, value);
        }

        /// <summary>
        /// Gets or sets the name of the DMS data set.
        /// </summary>
        public string Dataset
        {
            get => dataset;
            set => this.RaiseAndSetIfChanged(ref dataset, value);
        }

        /// <summary>
        /// Gets or sets the name of the type of experiment.
        /// </summary>
        public string Experiment
        {
            get => experiment;
            set => this.RaiseAndSetIfChanged(ref experiment, value);
        }

        /// <summary>
        /// Gets or sets the name of the organism.
        /// </summary>
        public string Organism
        {
            get => organism;
            set => this.RaiseAndSetIfChanged(ref organism, value);
        }

        /// <summary>
        /// Gets or sets the type of instrument used.
        /// </summary>
        public string Instrument
        {
            get => instrument;
            set => this.RaiseAndSetIfChanged(ref instrument, value);
        }

        /// <summary>
        /// Gets or sets the date and time that this data set was created.
        /// </summary>
        public DateTime Created
        {
            get => created;
            set => this.RaiseAndSetIfChanged(ref created, value);
        }

        /// <summary>
        /// Gets or sets the path for the folder that this data set is in.
        /// </summary>
        public string DatasetFolderPath
        {
            get => datasetFolderPath;
            set => this.RaiseAndSetIfChanged(ref datasetFolderPath, value);
        }

        public override string ToString()
        {
            return $"{DatasetId}: {Dataset} {Instrument}";
        }
    }
}
