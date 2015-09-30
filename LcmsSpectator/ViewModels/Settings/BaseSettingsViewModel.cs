namespace LcmsSpectator.ViewModels.Settings
{
    using System.Collections.Generic;

    using LcmsSpectator.ViewModels.Dataset;

    using ReactiveUI;

    public class BaseSettingsViewModel : ReactiveObject
    {
        /// <summary>
        /// A value that indicates whether these settings should be applied
        /// to all datasets in the project.
        /// </summary>
        private bool applyToAllDatasets;

        /// <summary>
        /// The dataset to apply these settings to.
        /// </summary>
        private DatasetViewModel selectedDatasetViewModel;

        public BaseSettingsViewModel(IEnumerable<DatasetViewModel> datasets)
        {
            this.ApplyToAllDatasets = false;
            this.Datasets = new ReactiveList<DatasetViewModel>(datasets);
        }

        /// <summary>
        /// Gets the list of possible datasets.
        /// </summary>
        public ReactiveList<DatasetViewModel> Datasets { get; private set; } 

        /// <summary>
        /// Gets or sets a value that indicates whether these settings should be applied
        /// to all datasets in the project.
        /// </summary>
        public bool ApplyToAllDatasets
        {
            get { return this.applyToAllDatasets; }
            set { this.RaiseAndSetIfChanged(ref this.applyToAllDatasets, value); }
        }

        /// <summary>
        /// Gets or sets the dataset to apply these settings to.
        /// </summary>
        public DatasetViewModel SelectedDatasetViewModel
        {
            get { return this.selectedDatasetViewModel; }
            set { this.RaiseAndSetIfChanged(ref this.selectedDatasetViewModel, value); }
        }
    }
}
