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
    using System.IO;
    using System.Reactive.Linq;

    using LcmsSpectator.DialogServices;
    using LcmsSpectator.Models;
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
        /// The selectedMSPF parameter file path.
        /// </summary>
        private string paramFilePath;

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
        /// The selected FASTA file path.
        /// </summary>
        private string fastaFilePath;

        /// <summary>
        /// A value indicating whether a parameter file has been selected.
        /// </summary>
        private bool paramFileSelected;

        /// <summary>
        /// A value indicating whether a dataset has been selected to be opened.
        /// </summary>
        private bool datasetSelected;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenDataWindowViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        public OpenDataWindowViewModel(IDialogService dialogService)
        {
            this.dialogService = dialogService;

            this.WhenAnyValue(x => x.ParamFilePath).Subscribe(this.ReadParamFile);

            var browseParamFilesCommand = ReactiveCommand.Create();
            browseParamFilesCommand.Subscribe(_ => this.BrowseParamFilesImplementation());
            this.BrowseParamFilesCommand = browseParamFilesCommand;

            var browseRawFilesCommand = ReactiveCommand.Create();
            browseRawFilesCommand.Subscribe(_ => this.BrowseRawFilesImplementation());
            this.BrowseRawFilesCommand = browseRawFilesCommand;

            var browseFeatureFilesCommand = ReactiveCommand.Create();
            browseFeatureFilesCommand.Subscribe(_ => this.BrowseFeatureFilesImplementation());
            this.BrowseFeatureFilesCommand = browseFeatureFilesCommand;

            var browseIdFilesCommand = ReactiveCommand.Create();
            browseIdFilesCommand.Subscribe(_ => this.BrowseIdFilesImplementation());
            this.BrowseIdFilesCommand = browseIdFilesCommand;

            var browseFastaFilesCommand = ReactiveCommand.Create();
            browseFastaFilesCommand.Subscribe(_ => this.BrowseFastaFilesImplementation());
            this.BrowseFastaFilesCommand = browseIdFilesCommand;

            this.ParamFileSelected = true;
            this.DatasetSelected = false;

            // Ok button should be enabled if RawFilePath isn't null or empty
            var okCommand =
            ReactiveCommand.Create(this.WhenAnyValue(x => x.RawFilePath).Select(x => !string.IsNullOrWhiteSpace(x)));
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
        /// Gets a command that prompts the user for a MSPF parameter file path.
        /// </summary>
        public IReactiveCommand BrowseParamFilesCommand { get; private set; }

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
        /// Gets a command that prompts the user for a FASTA file path.
        /// </summary>
        public IReactiveCommand BrowseFastaFilesCommand { get; private set; }
        
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
        /// Gets or sets the selected MSPF parameter file path.
        /// </summary>
        public string ParamFilePath
        {
            get { return this.paramFilePath; }
            set { this.RaiseAndSetIfChanged(ref this.paramFilePath, value); }
        }

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
        /// Gets or sets the selected FASTA file path.
        /// </summary>
        public string FastaFilePath
        {
            get { return this.fastaFilePath; }
            set { this.RaiseAndSetIfChanged(ref this.fastaFilePath, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether a parameter file has been selected to be opened.
        /// </summary>
        public bool ParamFileSelected
        {
            get { return this.paramFileSelected; }
            set { this.RaiseAndSetIfChanged(ref this.paramFileSelected, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether a dataset has been selected to be opened.
        /// </summary>
        public bool DatasetSelected
        {
            get { return this.datasetSelected; }
            set { this.RaiseAndSetIfChanged(ref this.datasetSelected, value); }
        }

        /// <summary>
        /// Read a MSPF parameter file and populate raw file and feature file.
        /// </summary>
        /// <param name="filePath">The path to the parameter file.</param>
        private void ReadParamFile(string filePath)
        {
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                var mspfParams = MsPfParameters.ReadFromFile(filePath);
                var dirPath = Path.GetDirectoryName(filePath);

                var raw = string.Format("{0}\\{1}", dirPath, mspfParams.PuSpecFile);
                if (!string.IsNullOrWhiteSpace(mspfParams.PuSpecFile) && File.Exists(raw))
                {
                    this.RawFilePath = raw;   
                }

                var feature = string.Format("{0}\\{1}", dirPath, mspfParams.FeatureFile);
                if (!string.IsNullOrWhiteSpace(mspfParams.FeatureFile) && File.Exists(feature))
                {
                    this.FeatureFilePath = feature;
                }

                var fasta = string.Format("{0}\\{1}", dirPath, mspfParams.DatabaseFile);
                if (!string.IsNullOrWhiteSpace(mspfParams.DatabaseFile) && File.Exists(fasta))
                {
                    this.FastaFilePath = fasta;
                }
            }
        }

        /// <summary>
        /// Implementation for <see cref="BrowseParamFilesCommand"/>.
        /// A command that prompts the user for a MSPF parameter file path.
        /// </summary>
        private void BrowseParamFilesImplementation()
        {
            var path = this.dialogService.OpenFile(".param", FileConstants.ParamFileFormatString);
            if (!string.IsNullOrWhiteSpace(path))
            {
                this.ParamFilePath = path;
            }
        }

        /// <summary>
        /// Implementation for <see cref="BrowseRawFilesCommand"/>.
        /// Prompts the user for a raw file path.
        /// </summary>
        private void BrowseRawFilesImplementation()
        {
            var path = this.dialogService.OpenFile(".raw", FileConstants.RawFileFormatString);
            if (!string.IsNullOrWhiteSpace(path))
            {
                this.RawFilePath = path;
            }
        }

        /// <summary>
        /// Implementation for <see cref="BrowseFeatureFilesCommand"/>.
        /// Prompts the user for a feature file path.
        /// </summary>
        private void BrowseFeatureFilesImplementation()
        {
            var path = this.dialogService.OpenFile(".ms1ft", FileConstants.FeatureFileFormatString);
            if (!string.IsNullOrWhiteSpace(path))
            {
                this.FeatureFilePath = path;
            }
        }

        /// <summary>
        /// Implementation for <see cref="BrowseIdFilesCommand"/>.
        /// Prompts the user for an ID file path.
        /// </summary>
        private void BrowseIdFilesImplementation()
        {
            var path = this.dialogService.OpenFile(".txt", FileConstants.IdFileFormatString);
            if (!string.IsNullOrWhiteSpace(path))
            {
                this.IdFilePath = path;
            }
        }

        /// <summary>
        /// Implementation for <see cref="BrowseFastaFilesCommand"/>.
        /// Prompts the user for a FASTA file path.
        /// </summary>
        private void BrowseFastaFilesImplementation()
        {
            var path = this.dialogService.OpenFile(".fasta", FileConstants.FastaFileFormatString);
            if (!string.IsNullOrWhiteSpace(path))
            {
                this.FastaFilePath = path;
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
