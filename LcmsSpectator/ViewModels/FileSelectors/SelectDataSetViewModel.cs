// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SelectDataSetViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for selecting an existing dataset or a new raw file to open.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using LcmsSpectator.Models.Dataset;
using LcmsSpectator.ViewModels.Dataset;

namespace LcmsSpectator.ViewModels.FileSelectors
{
    using System;
    using System.Reactive.Linq;

    using LcmsSpectator.DialogServices;
    using LcmsSpectator.ViewModels.Data;

    using ReactiveUI;

    /// <summary>
    /// View model for selecting an existing dataset or a new raw file to open.
    /// </summary>
    public class SelectDataSetViewModel : ReactiveObject, IDatasetInfoProvider
    {
        /// <summary>
        /// Dialog service for opening dialogs from view model.
        /// </summary>
        private readonly IDialogService dialogService;
        
        /// <summary>
        /// The selected dataset.
        /// </summary>
        private DatasetViewModel selectedDataSet;

        /// <summary>
        /// The path to the raw file to open a new data set for.
        /// </summary>
        private string rawFilePath;

        /// <summary>
        /// A value indicating whether a dataset has been selected.
        /// </summary>
        private bool datasetSelected;

        /// <summary>
        /// A value indicating whether a raw file path has been selected.
        /// </summary>
        private bool rawPathSelected;

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectDataSetViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        /// <param name="dataSets">List of datasets for selection by user.</param>
        public SelectDataSetViewModel(IDialogService dialogService, ReactiveList<DatasetViewModel> dataSets)
        {
            this.dialogService = dialogService;
            this.DataSets = dataSets;

            var browseRawFilesCommand = ReactiveCommand.Create();
            browseRawFilesCommand.Subscribe(_ => this.BrowseRawFilesImplementation());
            this.BrowseRawFilesCommand = browseRawFilesCommand;

            var clearRawFilesCommand = ReactiveCommand.Create();
            clearRawFilesCommand.Subscribe(_ => this.ClearRawFilesImplementation());
            this.ClearRawFilesCommand = clearRawFilesCommand;
            
            // Ok command is only available if RawFilePath isn't empty or SelectedDataSet isn't null
            var okCommand = ReactiveCommand.Create(
                        this.WhenAnyValue(x => x.RawFilePath, x => x.SelectedDataSet, x => x.RawPathSelected, x => x.DatasetSelected)
                            .Select(p => ((p.Item3 && !string.IsNullOrEmpty(p.Item1)) || (p.Item4 && p.Item2 != null))));
            okCommand.Subscribe(_ => this.OkImplementation());
            this.OkCommand = okCommand;

            var cancelCommand = ReactiveCommand.Create();
            cancelCommand.Subscribe(_ => this.CancelImplementation());
            this.CancelCommand = cancelCommand;

            this.RawPathSelected = true;
            this.DatasetSelected = false;

            this.Status = false;
        }

        /// <summary>
        /// Event that is triggered when the window is ready to be closed.
        /// </summary>
        public event EventHandler ReadyToClose;

        /// <summary>
        /// Gets a list of all currently open datasets.
        /// </summary>
        public ReactiveList<DatasetViewModel> DataSets { get; private set; }

        /// <summary>
        /// Gets a command that prompts the user for a raw file path.
        /// </summary>
        public IReactiveCommand BrowseRawFilesCommand { get; private set; }

        /// <summary>
        /// Gets a command that clears the currently selected raw file path.
        /// </summary>
        public IReactiveCommand ClearRawFilesCommand { get; private set; }

        /// <summary>
        /// Gets a command that validates the selected raw file path and trigger ReadyToClose.
        /// </summary>
        public IReactiveCommand OkCommand { get; private set; }

        /// <summary>
        /// Gets a command that triggers ReadyToClose
        /// </summary>
        public IReactiveCommand CancelCommand { get; private set; }

        /// <summary>
        /// Gets a value indicating whether a valid dataset or raw file path was selected.
        /// </summary>
        public bool Status { get; private set; }

        /// <summary>
        /// Gets or sets the selected dataset.
        /// </summary>
        public DatasetViewModel SelectedDataSet
        {
            get { return this.selectedDataSet; }
            set { this.RaiseAndSetIfChanged(ref this.selectedDataSet, value); }
        }

        /// <summary>
        /// Gets or sets the path to the raw file to open a new data set for.
        /// </summary>
        public string RawFilePath
        {
            get { return this.rawFilePath; }
            set { this.RaiseAndSetIfChanged(ref this.rawFilePath, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether a dataset has been selected.
        /// </summary>
        public bool DatasetSelected
        {
            get { return this.datasetSelected; }
            set { this.RaiseAndSetIfChanged(ref this.datasetSelected, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether a raw file path has been selected.
        /// </summary>
        public bool RawPathSelected
        {
            get { return this.rawPathSelected; }
            set { this.RaiseAndSetIfChanged(ref this.rawPathSelected, value); }
        }

        /// <summary>
        /// Gets the dataset info for the chosen dataset.
        /// </summary>
        /// <returns>The <see cref="DatasetInfo" />.</returns>
        public DatasetInfo GetDatasetInfo()
        {
            if (this.RawPathSelected)
            {
                var files = new List<string> { this.RawFilePath };
                return new DatasetInfo(files);
            }
            else
            {
                return this.SelectedDataSet.DatasetInfo;
            }
        }

        /// <summary>
        /// Implementation for BrowseRawFilesCommand.
        /// Prompts the user for a raw file path.
        /// </summary>
        private void BrowseRawFilesImplementation()
        {
            var path = this.dialogService.OpenFile(
                ".raw",
                @"Supported Files|*.raw;*.mzML;*.mzML.gz|Raw Files (*.raw)|*.raw|MzMl Files (*.mzMl[.gz])|*.mzMl;*.mzML.gz");
            if (!string.IsNullOrEmpty(path))
            {
                this.RawFilePath = path;
            }
        }

        /// <summary>
        /// Implementation for ClearRawFilesCommand.
        /// Clears the currently selected raw file path.
        /// </summary>
        private void ClearRawFilesImplementation()
        {
            this.RawFilePath = string.Empty;
        }

        /// <summary>
        /// Implementation for OkCommand.
        /// Validates the selected raw file path and trigger ReadyToClose.
        /// </summary>
        private void OkImplementation()
        {
            if (string.IsNullOrEmpty(this.RawFilePath) && this.SelectedDataSet == null)
            {
                this.dialogService.MessageBox("Please select data set or new raw/mzml file to open.");
                return;
            }

            this.Status = true;
            if (this.ReadyToClose != null)
            {
                this.ReadyToClose(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Implementation for CancelCommand.
        /// Triggers ReadyToClose
        /// </summary>
        private void CancelImplementation()
        {
            this.Status = false;
            if (this.ReadyToClose != null)
            {
                this.ReadyToClose(this, EventArgs.Empty);
            }
        }
    }
}
