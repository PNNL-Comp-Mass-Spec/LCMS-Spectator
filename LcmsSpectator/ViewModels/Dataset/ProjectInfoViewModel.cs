using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using LcmsSpectator.DialogServices;
using LcmsSpectator.Models.Dataset;
using LcmsSpectator.Utils;
using LcmsSpectator.ViewModels.Dms;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.Dataset
{
    public class ProjectInfoViewModel : ReactiveObject
    {
        /// <summary>
        /// Dialog service for opening dialogs from view model.
        /// </summary>
        private readonly IMainDialogService dialogService;

        /// <summary>
        /// The path to the project file.
        /// </summary>
        private string projectFilePath;

        /// <summary>
        /// The output directory path.
        /// </summary>
        private string outputDirectory;

        public ProjectInfoViewModel(IMainDialogService dialogService = null)
        {
            this.dialogService = dialogService ?? new MainDialogService();

            this.LoadFolderCommand = ReactiveCommand.Create();
            this.LoadFolderCommand.Subscribe(_ => this.LoadFolderImplementation());

            this.SelectFilesCommand = ReactiveCommand.Create();
            this.SelectFilesCommand.Subscribe(_ => this.SelectFilesImplementation());

            this.OpenFromDmsCommand = ReactiveCommand.Create();
            this.OpenFromDmsCommand.Subscribe(_ => this.OpenFromDmsImplementation());

            this.Datasets = new ReactiveList<DatasetInfoViewModel> { ChangeTrackingEnabled = true };
            this.Datasets.ItemChanged.Where(x => x.PropertyName == "ReadyToClose")
                         .Where(x => x.Sender.ReadyToClose)
                         .Subscribe(x => this.Datasets.Remove(x.Sender));

            // If OutputDirectory hasn't been set yet, set it to the directory of
            // the project file when it is set.
            this.WhenAnyValue(x => x.ProjectFilePath)
                .Where(path => !string.IsNullOrEmpty(path))
                .Where(_ => string.IsNullOrEmpty(this.OutputDirectory))
                .Select(Path.GetDirectoryName)
                .Subscribe(path => this.OutputDirectory = path);
        }

        /// <summary>
        /// Gets a command for opening files from a folder.
        /// </summary>
        public ReactiveCommand<object> LoadFolderCommand { get; private set; }

        /// <summary>
        /// Gets a command for selecting files.
        /// </summary>
        public ReactiveCommand<object> SelectFilesCommand { get; private set; }

        /// <summary>
        /// Gets a command for opening files from DMS.
        /// </summary>
        public ReactiveCommand<object> OpenFromDmsCommand { get; private set; }

        /// <summary>
        /// The path to the project file.
        /// </summary>
        public string ProjectFilePath
        {
            get { return this.projectFilePath; }
            set { this.RaiseAndSetIfChanged(ref this.projectFilePath, value); }
        }

        /// <summary>
        /// Gets or sets the output directory path.
        /// </summary>
        public string OutputDirectory
        {
            get { return this.outputDirectory; }
            set { this.RaiseAndSetIfChanged(ref this.outputDirectory, value); }
        }

        /// <summary>
        /// The selected datasets.
        /// </summary>
        public ReactiveList<DatasetInfoViewModel> Datasets { get; private set; }

        /// <summary>
        /// Implementation for <see cref="LoadFolderCommand" />.
        /// Opens files from a folder.
        /// </summary>
        private void LoadFolderImplementation()
        {
            var folder = this.dialogService.OpenFolder();
            if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            {
                var files = Directory.GetFiles(folder);
                this.Datasets.AddRange(DatasetInfo.GetDatasetsFromInputFilePaths(files).Select(ds => new DatasetInfoViewModel(ds)));
            }
        }

        /// <summary>
        /// Implementation for <see cref="SelectFilesCommand" />.
        /// Selects files.
        /// </summary>
        private void SelectFilesImplementation()
        {
            var files = this.dialogService.MultiSelectOpenFile(".mzml", FileConstants.RawFileFormatString).ToArray();
            if (files.Length > 0)
            {
                this.Datasets.AddRange(DatasetInfo.GetDatasetsFromInputFilePaths(files).Select(ds => new DatasetInfoViewModel(ds)));  
            }
        }

        /// <summary>
        /// Implementation for <see cref="OpenFromDmsCommand" />.
        /// Opens files from DMS.
        /// </summary>
        private void OpenFromDmsImplementation()
        {
            var dmsVm = new DmsLookupViewModel(this.dialogService);
            this.dialogService.OpenDmsLookup(dmsVm);
            var datasets = dmsVm.GetDatasetInfo();
            this.Datasets.AddRange(datasets.Select(ds => new DatasetInfoViewModel(ds)));
        }
    }
}
