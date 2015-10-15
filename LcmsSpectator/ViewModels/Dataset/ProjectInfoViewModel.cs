// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectInfoViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for editing a project.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels.Dataset
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;
    using LcmsSpectator.DialogServices;
    using LcmsSpectator.Models.Dataset;
    using LcmsSpectator.Utils;
    using LcmsSpectator.ViewModels.Dms;
    using ReactiveUI;
    
    /// <summary>
    /// View model for editing a project.
    /// </summary>
    public class ProjectInfoViewModel : ReactiveObject
    {
        /// <summary>
        /// Dialog service for opening dialogs from view model.
        /// </summary>
        private readonly IMainDialogService dialogService;

        /// <summary>
        /// The dataset selected for editing.
        /// </summary>
        private DatasetInfoViewModel selectedDataset;

        /// <summary>
        /// The path to the project file.
        /// </summary>
        private string projectFilePath;

        /// <summary>
        /// The output directory path.
        /// </summary>
        private string outputDirectory;

        /// <summary>
        /// The path to the layout file.
        /// </summary>
        private string layoutFilePath;

        /// <summary>
        /// A value that indicates whether the OpenFromDms button should be displayed.
        /// </summary>
        private bool showOpenFromDms;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectInfoViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        public ProjectInfoViewModel(IMainDialogService dialogService = null)
        {
            this.dialogService = dialogService ?? new MainDialogService();

            this.LoadFolderCommand = ReactiveCommand.Create();
            this.LoadFolderCommand.Subscribe(_ => this.LoadFolderImplementation());

            this.SelectFilesCommand = ReactiveCommand.Create();
            this.SelectFilesCommand.Subscribe(_ => this.SelectFilesImplementation());

            this.OpenFromDmsCommand = ReactiveCommand.Create(this.WhenAnyValue(x => x.ShowOpenFromDms).Select(x => x));
            this.OpenFromDmsCommand.Subscribe(_ => this.OpenFromDmsImplementation());

            this.BrowseOutputDirectoriesCommand = ReactiveCommand.Create();
            this.BrowseOutputDirectoriesCommand.Subscribe(
                _ =>
                    {
                        var folder = this.dialogService.OpenFolder();
                        if (!string.IsNullOrEmpty(folder))
                        {
                            this.OutputDirectory = folder;
                        }
                    });

            this.BrowseProjectFilesCommand = ReactiveCommand.Create();
            this.BrowseProjectFilesCommand.Subscribe(
                _ =>
                    {
                        var file = this.dialogService.SaveFile(".xml", "Project Files (*.xml)|*.xml;");
                        if (!string.IsNullOrEmpty(file))
                        {
                            this.ProjectFilePath = file;
                        }
                    });

            this.OkCommand = ReactiveCommand.Create(
                                                    this.WhenAnyValue(x => x.ProjectFilePath, x => x.OutputDirectory)
                                                        .Select(x => !string.IsNullOrEmpty(x.Item1) &&
                                                                     !string.IsNullOrEmpty(x.Item2)));

            // When OkCommand is executed, set the status to true and request the window to be closed.
            this.OkCommand.Subscribe(
                _ =>
                    {
                        this.Status = true;
                        this.ReadyToClose?.Invoke(this, EventArgs.Empty);
                    });

            this.CancelCommand = ReactiveCommand.Create();

            // When CancelCommand is executed, set the status to false and request the window to be closed.
            this.CancelCommand.Subscribe(
                _ =>
                    {
                        this.Status = false;
                        this.ReadyToClose?.Invoke(this, EventArgs.Empty);
                    });

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

            this.LayoutFilePath = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectInfoViewModel"/> class.
        /// </summary>
        /// <param name="projectInfo">Model to populate this view model from.</param>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        public ProjectInfoViewModel(ProjectInfo projectInfo, IMainDialogService dialogService = null) : this(dialogService)
        {
            this.OutputDirectory = projectInfo.OutputDirectory;
            this.ProjectFilePath = projectInfo.ProjectFilePath;
            this.LayoutFilePath = projectInfo.LayoutFilePath;
            this.Datasets.AddRange(projectInfo.Datasets.Select(ds => new DatasetInfoViewModel(ds, this.dialogService)));
        }

        /// <summary>
        /// An event that is triggered when the window is ready to be closed.
        /// </summary>
        public event EventHandler ReadyToClose; 

        /// <summary>
        /// Gets a command for opening files from a folder.
        /// </summary>
        public ReactiveCommand<object> LoadFolderCommand { get; }

        /// <summary>
        /// Gets a command for selecting files.
        /// </summary>
        public ReactiveCommand<object> SelectFilesCommand { get; }

        /// <summary>
        /// Gets a command for opening files from DMS.
        /// </summary>
        public ReactiveCommand<object> OpenFromDmsCommand { get; }

        /// <summary>
        /// Gets a command that is triggered when the OK button is clicked.
        /// </summary>
        public ReactiveCommand<object> OkCommand { get; }

        /// <summary>
        /// Gets a command that is triggered the cancel button is clicked.
        /// </summary>
        public ReactiveCommand<object> CancelCommand { get; }

        /// <summary>
        /// Gets a command that opens a browser for output directory.
        /// </summary>
        public ReactiveCommand<object> BrowseOutputDirectoriesCommand { get; }

        /// <summary>
        /// Gets a command that opens a browser for project file.
        /// </summary>
        public ReactiveCommand<object> BrowseProjectFilesCommand { get; }

        /// <summary>
        /// Gets a value indicating whether a project was selected.
        /// </summary>
        public bool Status { get; private set; }

        /// <summary>
        /// Gets or sets the path to the project file.
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
        /// Gets or sets the path to the layout file.
        /// </summary>
        public string LayoutFilePath
        {
            get { return this.layoutFilePath; }
            set { this.RaiseAndSetIfChanged(ref this.layoutFilePath, value); }
        }

        /// <summary>
        /// Gets the view models for the datasets in this project.
        /// </summary>
        public ReactiveList<DatasetInfoViewModel> Datasets { get; }

        /// <summary>
        /// Gets or sets the dataset selected for editing.
        /// </summary>
        public DatasetInfoViewModel SelectedDataset
        {
            get { return this.selectedDataset; }
            set { this.RaiseAndSetIfChanged(ref this.selectedDataset, value); }
        } 

        /// <summary>
        /// Gets or sets a value that indicates whether the OpenFromDms button should be displayed.
        /// </summary>
        public bool ShowOpenFromDms
        {
            get { return this.showOpenFromDms; }
            set { this.RaiseAndSetIfChanged(ref this.showOpenFromDms, value); }
        }

        /// <summary>
        /// Gets the project info model for this view model.
        /// </summary>
        public ProjectInfo ProjectInfo
        {
            get
            {
                var name = Path.GetFileNameWithoutExtension(this.ProjectFilePath);
                return new ProjectInfo
                {
                    Name = name,
                    Datasets = this.Datasets.Select(dsvm => dsvm.DatasetInfo).ToList(),
                    LayoutFilePath = this.LayoutFilePath ?? Path.Combine(this.OutputDirectory, $"{name}_Layout.xml"),
                    OutputDirectory = this.OutputDirectory,
                    ProjectFilePath = this.ProjectFilePath
                };
            }
        }

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
            var files = this.dialogService.MultiSelectOpenFile(".mzml", FileConstants.RawFileFormatString);
            if (files != null && files.Any())
            {
                var datasetViewModels =
                    DatasetInfo.GetDatasetsFromInputFilePaths(files).Select(
                        ds => new DatasetInfoViewModel(ds));
                this.Datasets.AddRange(datasetViewModels);
                this.SelectedDataset = datasetViewModels.FirstOrDefault() ?? this.SelectedDataset;
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
            var datasetInfo = dmsVm.GetDatasetInfo();
            if (datasetInfo != null)
            {
                this.Datasets.AddRange(datasetInfo.Select(ds => new DatasetInfoViewModel(ds)));
            }
        }
    }
}
