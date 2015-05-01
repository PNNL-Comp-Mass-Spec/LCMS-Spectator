// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OpenDataWindowViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for selecting raw file path, feature file path, and ID file path.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels.FileSelectors
{
    using System;
    using System.Reactive.Linq;

    using LcmsSpectator.DialogServices;
    using LcmsSpectator.Utils;

    using ReactiveUI;

    /// <summary>
    /// View model for selecting raw file path, feature file path, and ID file path.
    /// </summary>
    public class OpenDataWindowViewModel : ReactiveObject
    {
        /// <summary>
        /// Dialog service for opening dialogs from view model.
        /// </summary>
        private readonly IDialogService dialogService;

        /// <summary>
        /// The selected raw file path.
        /// </summary>
        private string rawFilePath;

        /// <summary>
        /// The selected feature file path.
        /// </summary>
        private string featureFilePath;

        /// <summary>
        /// The selected ID file path.
        /// </summary>
        private string idFilePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenDataWindowViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        public OpenDataWindowViewModel(IDialogService dialogService)
        {
            this.dialogService = dialogService;
            var browseRawFilesCommand = ReactiveCommand.Create();
            browseRawFilesCommand.Subscribe(_ => this.BrowseRawFilesImplementation());
            this.BrowseRawFilesCommand = browseRawFilesCommand;

            var browseFeatureFilesCommand = ReactiveCommand.Create();
            browseFeatureFilesCommand.Subscribe(_ => this.BrowseFeatureFilesImplementation());
            this.BrowseFeatureFilesCommand = browseFeatureFilesCommand;

            var browseIdFilesCommand = ReactiveCommand.Create();
            browseIdFilesCommand.Subscribe(_ => this.BrowseIdFilesImplementation());
            this.BrowseIdFilesCommand = browseIdFilesCommand;

            // Ok button should be enabled if RawFilePath isn't null or empty
            var okCommand =
            ReactiveCommand.Create(
                    this.WhenAnyValue(x => x.RawFilePath).Select(rawFilePath => !string.IsNullOrEmpty(rawFilePath)));
            okCommand.Subscribe(_ => this.OkImplementation());
            this.OkCommand = okCommand;

            var cancelCommand = ReactiveCommand.Create();
            cancelCommand.Subscribe(_ => this.CancelImplementation());
            this.CancelCommand = cancelCommand;

            this.Status = false;
        }

        /// <summary>
        /// Event that is triggered when the window is ready to be closed.
        /// </summary>
        public event EventHandler ReadyToClose;

        /// <summary>
        /// Gets a command that prompts the user for a raw file path.
        /// </summary>
        public IReactiveCommand BrowseRawFilesCommand { get; private set; }

        /// <summary>
        /// Gets a command that prompts the user for a feature file path.
        /// </summary>
        public IReactiveCommand BrowseFeatureFilesCommand { get; private set; }

        /// <summary>
        /// Gets a command that prompts the user for an ID file path.
        /// </summary>
        public IReactiveCommand BrowseIdFilesCommand { get; private set; }

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
        /// Gets or sets the selected raw file path.
        /// </summary>
        public string RawFilePath
        {
            get { return this.rawFilePath; }
            set { this.RaiseAndSetIfChanged(ref this.rawFilePath, value); }
        }

        /// <summary>
        /// Gets or sets the selected feature file path.
        /// </summary>
        public string FeatureFilePath
        {
            get { return this.featureFilePath; }
            set { this.RaiseAndSetIfChanged(ref this.featureFilePath, value); }
        }

        /// <summary>
        /// Gets or sets the selected ID file path.
        /// </summary>
        public string IdFilePath
        {
            get { return this.idFilePath; }
            set { this.RaiseAndSetIfChanged(ref this.idFilePath, value); }
        }

        /// <summary>
        /// Implementation for BrowseRawFilesCommand.
        /// Prompts the user for a raw file path.
        /// </summary>
        private void BrowseRawFilesImplementation()
        {
            var path = this.dialogService.OpenFile(".raw", FileConstants.RawFileFormatString);
            if (!string.IsNullOrEmpty(path))
            {
                this.RawFilePath = path;
            }
        }

        /// <summary>
        /// Implementation for BrowseFeatureFilesCommand.
        /// Prompts the user for a feature file path.
        /// </summary>
        private void BrowseFeatureFilesImplementation()
        {
            var path = this.dialogService.OpenFile(".ms1ft", FileConstants.FeatureFileFormatString);
            if (!string.IsNullOrEmpty(path))
            {
                this.FeatureFilePath = path;
            }
        }

        /// <summary>
        /// Implementation for BrowseIdFilesCommand.
        /// Prompts the user for an ID file path.
        /// </summary>
        private void BrowseIdFilesImplementation()
        {
            var path = this.dialogService.OpenFile(".txt", FileConstants.IdFileFormatString);
            if (!string.IsNullOrEmpty(path))
            {
                this.IdFilePath = path;
            }
        }

        /// <summary>
        /// Implementation for OkCommand.
        /// Validates the selected raw file path and trigger ReadyToClose.
        /// </summary>
        private void OkImplementation()
        {
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
