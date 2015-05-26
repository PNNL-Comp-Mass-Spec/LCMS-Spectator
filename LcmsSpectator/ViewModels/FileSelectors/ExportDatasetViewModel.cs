// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportDatasetViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for selecting a dataset and export path.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels.FileSelectors
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Linq;

    using LcmsSpectator.DialogServices;
    using LcmsSpectator.ViewModels.Data;
    using ReactiveUI;

    /// <summary>
    /// View model for selecting a dataset and export path.
    /// </summary>
    public class ExportDatasetViewModel : ReactiveObject
    {
        /// <summary>
        /// Dialog service for opening dialogs from view model.
        /// </summary>
        private readonly IDialogService dialogService;

        /// <summary>
        /// The selected Dataset View Model.
        /// </summary>
        private DataSetViewModel selectedDataset;

        /// <summary>
        /// The path for the output file.
        /// </summary>
        private string outputFilePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportDatasetViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        /// <param name="datasets">The datasets.</param>
        public ExportDatasetViewModel(IDialogService dialogService, IEnumerable<DataSetViewModel> datasets)
        {
            this.dialogService = dialogService;
            this.Datasets = new ReactiveList<DataSetViewModel>(datasets);

            if (this.Datasets.Count > 0)
            {
                this.SelectedDataset = this.Datasets[0];
            }

            var browseOuputFilesCommand = ReactiveCommand.Create();
            browseOuputFilesCommand.Subscribe(_ => this.BrowseOutputFilesImplementation());
            this.BrowseOutputFilesCommand = browseOuputFilesCommand;

            var exportCommand = ReactiveCommand.Create(
                                                       this.WhenAnyValue(x => x.SelectedDataset, x => x.OutputFilePath)
                                                           .Select(x => x.Item1 != null && !string.IsNullOrWhiteSpace(x.Item2)));
            exportCommand.Subscribe(_ => this.ExportImplementation());
            this.ExportCommand = exportCommand;

            var cancelCommand = ReactiveCommand.Create();
            cancelCommand.Subscribe(_ => this.CancelImplementation());
            this.CancelCommand = cancelCommand;
        }

        /// <summary>
        /// Event that is triggered when the window is ready to be closed.
        /// </summary>
        public event EventHandler ReadyToClose;

        /// <summary>
        /// Gets a value indicating whether a valid dataset or raw file path was selected.
        /// </summary>
        public bool Status { get; private set; }

        /// <summary>
        /// Gets the list of datasets.
        /// </summary>
        public ReactiveList<DataSetViewModel> Datasets { get; private set; }

        /// <summary>
        /// Gets or sets the selected Dataset View Model.
        /// </summary>
        public DataSetViewModel SelectedDataset
        {
            get { return this.selectedDataset; }
            set { this.RaiseAndSetIfChanged(ref this.selectedDataset, value); }
        }

        /// <summary>
        /// Gets or sets the output file path for the export.
        /// </summary>
        public string OutputFilePath
        {
            get { return this.outputFilePath; }
            set { this.RaiseAndSetIfChanged(ref this.outputFilePath, value); }
        }

        /// <summary>
        /// Gets a command that opens a save dialog so the user can select a TSV file path.
        /// </summary>
        public IReactiveCommand BrowseOutputFilesCommand { get; private set; }

        /// <summary>
        /// Gets a command that closes the window and exports the data set.
        /// </summary>
        public IReactiveCommand ExportCommand { get; private set; }

        /// <summary>
        /// Gets a command that closes the window and does nothing.
        /// </summary>
        public IReactiveCommand CancelCommand { get; private set; }

        /// <summary>
        /// Implementation for <see cref="BrowseOutputFilesCommand"/>.
        /// Gets a command that opens a save dialog so the user can select a TSV file path.
        /// </summary>
        private void BrowseOutputFilesImplementation()
        {
            var path = this.dialogService.SaveFile(".tsv", "TSV Files (*.tsv)|*.tsv");
            if (!string.IsNullOrWhiteSpace(path))
            {
                this.OutputFilePath = path;
            }
        }

        /// <summary>
        /// Implementation for <see cref="ExportCommand" />.
        /// Validates the selected raw file path and trigger ReadyToClose.
        /// </summary>
        private void ExportImplementation()
        {
            this.Status = true;
            if (this.ReadyToClose != null)
            {
                this.ReadyToClose(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Implementation for <see cref="CancelCommand" />.
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
