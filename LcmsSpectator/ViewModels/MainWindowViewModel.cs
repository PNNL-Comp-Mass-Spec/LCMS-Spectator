// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindowViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for main window. This is the view model for the entry point of the application.
//   Composed of all child view models.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using LcmsSpectator.DialogServices;
    using LcmsSpectator.Models;
    using LcmsSpectator.Models.Dataset;
    using LcmsSpectator.Models.DTO;
    using LcmsSpectator.ViewModels.Data;
    using LcmsSpectator.ViewModels.Dataset;
    using LcmsSpectator.ViewModels.Dms;
    using LcmsSpectator.ViewModels.FileSelectors;
    using LcmsSpectator.ViewModels.Modifications;
    using LcmsSpectator.ViewModels.Settings;
    using LcmsSpectator.Writers;

    using ReactiveUI;

    /// <summary>
    /// View model for main window. This is the view model for the entry point of the application.
    /// Composed of all child view models.
    /// </summary>
    public class MainWindowViewModel : ReactiveObject
    {
        /// <summary>
        /// Dialog service for opening dialogs from view model.
        /// </summary>
        private readonly IMainDialogService dialogService;

        /// <summary>
        /// A value indicating whether or not splash screen is visible.
        /// </summary>
        private bool showSplash;

        /// <summary>
        /// Initializes a new instance of the MainWindowViewModel class.
        /// </summary>
        /// <param name="dialogService">Service for view model friendly dialogs</param>
        public MainWindowViewModel(IMainDialogService dialogService)
        {
            this.dialogService = dialogService;
            this.ProjectManager = SingletonProjectManager.Instance;
            this.ShowSplash = true;

            // Initialize child view models
            this.ScanViewModel = new ScanViewModel(this.dialogService, new List<PrSm>());

            // New project command
            this.NewProjectCommand = ReactiveCommand.CreateAsyncTask(async _ => await this.NewProjectImplementation());

            // Configure project command is executable when a project is open.
            this.ConfigureProjectCommand = ReactiveCommand.Create(this.ProjectManager.WhenAnyValue(x => x.ProjectInfo)
                                                                      .Select(project => project != null && project.Name != "DefaultProject"));

            // Remove filter by unidentified scans from ScanViewModel filters
            this.ScanViewModel.Filters.Remove(this.ScanViewModel.Filters.FirstOrDefault(f => f.Name == "Hide Unidentified Scans"));

            // Create commands for file operations
            this.OpenDataSetCommand = ReactiveCommand.CreateAsyncTask(async _ =>
            {
                var openDataWindowVm = new OpenDataWindowViewModel(this.dialogService);
                this.dialogService.OpenDataWindow(openDataWindowVm);
                if (openDataWindowVm.Status)
                {
                    await this.LoadDatasets(openDataWindowVm);
                }
            });

            this.OpenRawFileCommand = ReactiveCommand.CreateAsyncTask(async _ => await this.LoadDatasets(this.dialogService.OpenRawFiles()));

            this.OpenFromDmsCommand = ReactiveCommand.CreateAsyncTask(async _ =>
            {
                var dmslvm = new DmsLookupViewModel(this.dialogService);
                this.dialogService.OpenDmsLookup(dmslvm);
                await this.LoadDatasets(dmslvm);
            });

            // Create command to open settings window
            this.OpenSettingsCommand = ReactiveCommand.Create();
            this.OpenSettingsCommand.Subscribe(
                _ => this.dialogService.OpenSettings(new SettingsViewModel(SingletonProjectManager.Instance.ProjectInfo, this.dialogService)));

            // Create command to open about box
            this.OpenAboutBoxCommand = ReactiveCommand.Create();
            this.OpenAboutBoxCommand.Subscribe(_ => this.dialogService.OpenAboutBox());

            // Create command to open new modification management window
            var openManageModificationsCommand = ReactiveCommand.Create();
            openManageModificationsCommand.Subscribe(_ => this.ManageModificationsImplementation());
            this.OpenManageModificationsCommand = openManageModificationsCommand;

            // Create MSPathFinder search command
            this.RunMsPathFinderSearchCommand = ReactiveCommand.Create();

            // Create export command
            this.ExportResultsCommand = ReactiveCommand.Create(this.ProjectManager.Datasets.WhenAnyValue(x => x.Count).Select(count => count > 0));
            this.ExportResultsCommand.Subscribe(_ => this.ExportResultsImplementation());

            // If all datasets are closed, show splash screen
            this.ProjectManager.Datasets.BeforeItemsRemoved.Subscribe(x => this.ShowSplash = this.ProjectManager.Datasets.Count == 1);

            // If a dataset is opened, show splash screen
            this.ProjectManager.Datasets.BeforeItemsAdded.Subscribe(x => this.ShowSplash = false);

            // When a PrSm is selected in the Protein Tree, make all data sets show the PrSm
            this.ScanViewModel.WhenAnyValue(x => x.SelectedPrSm)
                .Where(selectedPrSm => selectedPrSm != null)
                .Subscribe(selectedPrSm =>
                {
                    foreach (var dataset in this.ProjectManager.Datasets)
                    {
                        dataset.SelectedPrSm = selectedPrSm;
                    }
                });
        }

        /// <summary>
        /// Gets the project loader for the project open in this application.
        /// </summary>
        public SingletonProjectManager ProjectManager { get; }

        /// <summary>
        /// Gets a command that opens a dialog for a new project and loads the project.
        /// </summary>
        public ReactiveCommand<Unit> NewProjectCommand { get; }

        /// <summary>
        /// Gets a command that opens a dialog for configuring the active project.
        /// </summary>
        public ReactiveCommand<Object> ConfigureProjectCommand { get; }

        /// <summary>
        /// Gets command that opens a dialog box prompting the user for a raw file path,
        /// feature file path, and ID file path, then opens the files.
        /// </summary>
        public ReactiveCommand<Unit> OpenDataSetCommand { get; }

        /// <summary>
        /// Gets command that prompts the user for raw file(s) to open.
        /// </summary>
        public ReactiveCommand<Unit> OpenRawFileCommand { get; }
        
        /// <summary>
        /// Gets command that opens the DMS search dialog.
        /// </summary>
        public ReactiveCommand<Unit> OpenFromDmsCommand { get; }
        
        /// <summary>
        /// Gets command that opens settings window.
        /// </summary>
        public ReactiveCommand<object> OpenSettingsCommand { get; }
        
        /// <summary>
        /// Gets command that opens about box.
        /// </summary>
        public ReactiveCommand<object> OpenAboutBoxCommand { get; }
        
        /// <summary>
        /// Gets command that opens a window for managing registered modifications.
        /// </summary>
        public IReactiveCommand<object> OpenManageModificationsCommand { get; }

        /// <summary>
        /// Gets command that runs an MSPathFinder database search.
        /// </summary>
        public ReactiveCommand<object> RunMsPathFinderSearchCommand { get; }

        /// <summary>
        /// Gets a command that exports results of a data set to a file.
        /// </summary>
        public ReactiveCommand<object> ExportResultsCommand { get; }

        /// <summary>
        /// Gets view model for list of scans and identifications.
        /// </summary>
        public ScanViewModel ScanViewModel { get; }
      
        /// <summary>
        /// Gets a value indicating whether or not "Open From DMS" should be shown on the menu based on whether
        /// or not the user is on the PNNL network or not.
        /// </summary>
        public bool ShowOpenFromDms => System.Net.Dns.GetHostEntry(string.Empty).HostName.Contains("pnl.gov");

        /// <summary>
        /// Gets a value indicating whether or not splash screen is visible.
        /// </summary>
        public bool ShowSplash
        {
            get { return this.showSplash; }
            private set { this.RaiseAndSetIfChanged(ref this.showSplash, value); }
        }

        /// <summary>
        /// Implementation for <see cref="OpenManageModificationsCommand" />.
        /// Opens a window for editing the registered modifications.
        /// </summary>
        private void ManageModificationsImplementation()
        {
            var manageModificationsViewModel = new ManageModificationsViewModel(this.dialogService);
            manageModificationsViewModel.Modifications.AddRange(this.ProjectManager.ProjectInfo.RegisteredModifications);
            this.dialogService.OpenManageModifications(manageModificationsViewModel);

            // Update all sequences with new modifications.
            foreach (var prsm in this.ScanViewModel.Data)
            {
                prsm.UpdateModifications();
            }
        }

        /// <summary>
        /// Implementation for <see cref="ExportResultsCommand" />.
        /// Exports results of a data set to a file.
        /// </summary>
        private void ExportResultsImplementation()
        {
            var exportDatasetViewModel = new ExportDatasetViewModel(this.dialogService, this.ProjectManager.Datasets);

            if (this.dialogService.ExportDatasetWindow(exportDatasetViewModel))
            {
                var writer = new IcFileWriter(exportDatasetViewModel.OutputFilePath);
                writer.Write(exportDatasetViewModel.SelectedDataset.ScanViewModel.Data.Where(prsm => prsm.Sequence.Count > 0));
            }
        }

        /// <summary>
        /// Load a dataset from a dataset info provider and add it to the dataset list.
        /// </summary>
        /// <param name="provider">The <see cref="IDatasetInfoProvider" />.</param>
        /// <returns>Task that returns a <see cref="DatasetViewModel" /> for the loaded data set.</returns>
        private async Task LoadDatasets(IDatasetInfoProvider provider)
        {
            try
            {
                var datasetInfo = provider.GetDatasetInfo();
                await this.LoadDatasets(datasetInfo);
            }
            catch (FileNotFoundException e)
            {
                this.dialogService.ExceptionAlert(e);
            }
        }

        /// <summary>
        /// Load datasets.
        /// </summary>
        /// <param name="datasets">The <see cref="IDatasetInfoProvider" />.</param>
        /// <returns>Task that returns a <see cref="DatasetViewModel" /> for the loaded data set.</returns>
        private async Task LoadDatasets(IEnumerable<DatasetInfo> datasets)
        {
            await Task.WhenAll(datasets.Select(this.LoadDataset));
        }

        /// <summary>
        /// Load dataset from dataset information and add it to dataset list.
        /// </summary>
        /// <param name="datasetInfo">Path to raw file to open</param>
        /// <returns>Task that returns a <see cref="DatasetViewModel" /> for the loaded data set.</returns>
        private async Task LoadDataset(DatasetInfo datasetInfo)
        {
            var datasetViewModel = new DatasetViewModel(datasetInfo, this.dialogService); // create data set view model
            try
            {
                this.ProjectManager.Datasets.Add(datasetViewModel);

                // Load dataset.
                await datasetViewModel.InitializeAsync();

                // Load IDs and FASTAs into ProteinTree.
                this.ScanViewModel.Data.AddRange(datasetViewModel.ScanViewModel.Data.Where(p => p.Sequence.Count > 0));
                await this.ScanViewModel.IdTree.AddFastaFilesAsync(datasetViewModel.DatasetInfo.GetFastaFilePaths());
            }
            catch (Exception)
            {
                this.dialogService.ExceptionAlert(new Exception(
                    $"Cannot read {Path.GetFileNameWithoutExtension(datasetViewModel.DatasetInfo.Name)}."));
                if (this.ProjectManager.Datasets.Count > 0 && this.ProjectManager.Datasets.Contains(datasetViewModel))
                {
                    this.ProjectManager.Datasets.Remove(datasetViewModel);
                }
            }
        }

        /// <summary>
        /// Implementation for <see cref="NewProjectCommand" />.
        /// Opens a dialog for a new project and loads the project.
        /// </summary>
        /// <returns>The <see cref="Task" />.</returns>
        private async Task NewProjectImplementation()
        {
            var projectVm = new ProjectInfoViewModel(this.dialogService) { ShowOpenFromDms = this.ShowOpenFromDms };
            if (this.dialogService.OpenProjectEditor(projectVm))
            {
                var project = projectVm.ProjectInfo;
                SingletonProjectManager.Instance.LoadProject(project);
                if (project.Datasets.Any())
                {
                    await this.LoadDatasets(projectVm.ProjectInfo.Datasets);
                }
            }
        }
    }
}