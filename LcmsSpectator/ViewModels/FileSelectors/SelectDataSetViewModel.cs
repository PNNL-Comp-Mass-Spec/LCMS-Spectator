// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SelectDataSetViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for selecting an existing dataset or a new raw file to open.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Reactive;
using System.Reactive.Linq;
using LcmsSpectator.DialogServices;
using LcmsSpectator.ViewModels.Data;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.FileSelectors
{
    /// <summary>
    /// View model for selecting an existing dataset or a new raw file to open.
    /// </summary>
    public class SelectDataSetViewModel : ReactiveObject
    {
        /// <summary>
        /// Dialog service for opening dialogs from view model.
        /// </summary>
        private readonly IDialogService dialogService;

        /// <summary>
        /// The selected dataset.
        /// </summary>
        private DataSetViewModel selectedDataSet;

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
        /// Default constructor to support WPF design-time use
        /// </summary>
        [Obsolete("For WPF Design-time use only.", true)]
        public SelectDataSetViewModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectDataSetViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        /// <param name="dataSets">List of datasets for selection by user.</param>
        public SelectDataSetViewModel(IDialogService dialogService, ReactiveList<DataSetViewModel> dataSets)
        {
            this.dialogService = dialogService;
            DataSets = dataSets;

            BrowseRawFilesCommand = ReactiveCommand.Create(BrowseRawFilesImplementation);
            ClearRawFilesCommand = ReactiveCommand.Create(ClearRawFilesImplementation);

            // Ok command is only available if RawFilePath isn't empty or SelectedDataSet isn't null
            OkCommand = ReactiveCommand.Create(OkImplementation,
                        this.WhenAnyValue(x => x.RawFilePath, x => x.SelectedDataSet, x => x.RawPathSelected, x => x.DatasetSelected)
                            .Select(p => ((p.Item3 && !string.IsNullOrEmpty(p.Item1)) || (p.Item4 && p.Item2 != null))));

            CancelCommand = ReactiveCommand.Create(CancelImplementation);

            RawPathSelected = true;
            DatasetSelected = false;

            Status = false;
        }

        /// <summary>
        /// Event that is triggered when the window is ready to be closed.
        /// </summary>
        public event EventHandler ReadyToClose;

        /// <summary>
        /// Gets a list of all currently open datasets.
        /// </summary>
        public ReactiveList<DataSetViewModel> DataSets { get; }

        /// <summary>
        /// Gets a command that prompts the user for a raw file path.
        /// </summary>
        public ReactiveCommand<Unit, Unit> BrowseRawFilesCommand { get; }

        /// <summary>
        /// Gets a command that clears the currently selected raw file path.
        /// </summary>
        public ReactiveCommand<Unit, Unit> ClearRawFilesCommand { get; }

        /// <summary>
        /// Gets a command that validates the selected raw file path and trigger ReadyToClose.
        /// </summary>
        public ReactiveCommand<Unit, Unit> OkCommand { get; }

        /// <summary>
        /// Gets a command that triggers ReadyToClose
        /// </summary>
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        /// <summary>
        /// Gets a value indicating whether a valid dataset or raw file path was selected.
        /// </summary>
        public bool Status { get; private set; }

        /// <summary>
        /// Gets or sets the selected dataset.
        /// </summary>
        public DataSetViewModel SelectedDataSet
        {
            get => selectedDataSet;
            set => this.RaiseAndSetIfChanged(ref selectedDataSet, value);
        }

        /// <summary>
        /// Gets or sets the path to the raw file to open a new data set for.
        /// </summary>
        public string RawFilePath
        {
            get => rawFilePath;
            set => this.RaiseAndSetIfChanged(ref rawFilePath, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether a dataset has been selected.
        /// </summary>
        public bool DatasetSelected
        {
            get => datasetSelected;
            set => this.RaiseAndSetIfChanged(ref datasetSelected, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether a raw file path has been selected.
        /// </summary>
        public bool RawPathSelected
        {
            get => rawPathSelected;
            set => this.RaiseAndSetIfChanged(ref rawPathSelected, value);
        }

        /// <summary>
        /// Implementation for BrowseRawFilesCommand.
        /// Prompts the user for a raw file path.
        /// </summary>
        private void BrowseRawFilesImplementation()
        {
            var path = dialogService.OpenFile(
                ".raw",
                @"Supported Files|*.raw;*.mzML;*.mzML.gz|Raw Files (*.raw)|*.raw|MzMl Files (*.mzMl[.gz])|*.mzMl;*.mzML.gz");
            if (!string.IsNullOrEmpty(path))
            {
                RawFilePath = path;
            }
        }

        /// <summary>
        /// Implementation for ClearRawFilesCommand.
        /// Clears the currently selected raw file path.
        /// </summary>
        private void ClearRawFilesImplementation()
        {
            RawFilePath = string.Empty;
        }

        /// <summary>
        /// Implementation for OkCommand.
        /// Validates the selected raw file path and trigger ReadyToClose.
        /// </summary>
        private void OkImplementation()
        {
            if (string.IsNullOrEmpty(RawFilePath) && SelectedDataSet == null)
            {
                dialogService.MessageBox("Please select data set or new raw/mzml file to open.");
                return;
            }

            Status = true;
            ReadyToClose?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Implementation for CancelCommand.
        /// Triggers ReadyToClose
        /// </summary>
        private void CancelImplementation()
        {
            Status = false;
            ReadyToClose?.Invoke(this, EventArgs.Empty);
        }
    }
}
