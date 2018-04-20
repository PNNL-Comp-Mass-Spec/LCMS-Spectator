// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportDatasetViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for selecting a dataset and export path.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using LcmsSpectator.DialogServices;
using LcmsSpectator.ViewModels.Data;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.FileSelectors
{
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
        /// Default constructor to support WPF design-time use
        /// </summary>
        [Obsolete("For WPF Design-time use only.", true)]
        public ExportDatasetViewModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportDatasetViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        /// <param name="datasets">The datasets.</param>
        public ExportDatasetViewModel(IDialogService dialogService, IEnumerable<DataSetViewModel> datasets)
        {
            this.dialogService = dialogService;
            Datasets = new ReactiveList<DataSetViewModel>(datasets);

            if (Datasets.Count > 0)
            {
                SelectedDataset = Datasets[0];
            }

            BrowseOutputFilesCommand = ReactiveCommand.Create(BrowseOutputFilesImplementation);

            ExportCommand = ReactiveCommand.Create(ExportImplementation, this.WhenAnyValue(x => x.SelectedDataset, x => x.OutputFilePath)
                                                           .Select(x => x.Item1 != null && !string.IsNullOrWhiteSpace(x.Item2)));

            CancelCommand = ReactiveCommand.Create(CancelImplementation);
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
        public ReactiveList<DataSetViewModel> Datasets { get; }

        /// <summary>
        /// Gets or sets the selected Dataset View Model.
        /// </summary>
        public DataSetViewModel SelectedDataset
        {
            get => selectedDataset;
            set => this.RaiseAndSetIfChanged(ref selectedDataset, value);
        }

        /// <summary>
        /// Gets or sets the output file path for the export.
        /// </summary>
        public string OutputFilePath
        {
            get => outputFilePath;
            set => this.RaiseAndSetIfChanged(ref outputFilePath, value);
        }

        /// <summary>
        /// Gets a command that opens a save dialog so the user can select a TSV file path.
        /// </summary>
        public ReactiveCommand<Unit, Unit> BrowseOutputFilesCommand { get; }

        /// <summary>
        /// Gets a command that closes the window and exports the data set.
        /// </summary>
        public ReactiveCommand<Unit, Unit> ExportCommand { get; }

        /// <summary>
        /// Gets a command that closes the window and does nothing.
        /// </summary>
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        /// <summary>
        /// Implementation for <see cref="BrowseOutputFilesCommand"/>.
        /// Gets a command that opens a save dialog so the user can select a TSV file path.
        /// </summary>
        private void BrowseOutputFilesImplementation()
        {
            var path = dialogService.SaveFile(".tsv", "TSV Files (*.tsv)|*.tsv");
            if (!string.IsNullOrWhiteSpace(path))
            {
                OutputFilePath = path;
            }
        }

        /// <summary>
        /// Implementation for <see cref="ExportCommand" />.
        /// Validates the selected raw file path and trigger ReadyToClose.
        /// </summary>
        private void ExportImplementation()
        {
            Status = true;
            ReadyToClose?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Implementation for <see cref="CancelCommand" />.
        /// Triggers ReadyToClose
        /// </summary>
        private void CancelImplementation()
        {
            Status = false;
            ReadyToClose?.Invoke(this, EventArgs.Empty);
        }
    }
}
