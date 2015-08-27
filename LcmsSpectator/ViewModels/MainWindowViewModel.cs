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

using LcmsSpectator.Models.DTO;
using LcmsSpectator.ViewModels.Dataset;
using LcmsSpectator.ViewModels.Settings;

namespace LcmsSpectator.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using System.Reactive;
    using LcmsSpectator.DialogServices;
    using LcmsSpectator.Models;
    using LcmsSpectator.Models.Dataset;
    using LcmsSpectator.Readers;
    using LcmsSpectator.ViewModels.Data;
    using LcmsSpectator.ViewModels.Dms;
    using LcmsSpectator.ViewModels.FileSelectors;
    using LcmsSpectator.ViewModels.Modifications;
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

            // Remove filter by unidentified scans from ScanViewModel filters
            this.ScanViewModel.Filters.Remove(this.ScanViewModel.Filters.FirstOrDefault(f => f.Name == "Hide Unidentified Scans"));

            // Create commands for file operations
            this.OpenDataSetCommand = ReactiveCommand.CreateAsyncTask(async _ =>
            {
                var openDataWindowVm = new OpenDataWindowViewModel(this.dialogService);
                this.dialogService.OpenDataWindow(openDataWindowVm);
                return await this.LoadDataset(openDataWindowVm);
            });
            this.OpenDataSetCommand.Where(dsvm => dsvm != null).Subscribe(dsvm => this.ProjectManager.Datasets.Add(dsvm));

            this.OpenRawFileCommand = ReactiveCommand.CreateAsyncTask(async _ => await Task.WhenAll(this.dialogService.OpenRawFiles().Select(async ds => await this.LoadDataset(ds))));
            this.OpenRawFileCommand.Where(dsvms => dsvms != null).Where(dsvms => dsvms.Any())
                                   .Subscribe(datasets => this.ProjectManager.Datasets.AddRange(datasets));

            ////this.OpenTsvFileCommand = ReactiveCommand.CreateAsyncTask(async _ => await this.OpenIdFileImplementation());
            ////this.OpenFeatureFileCommand = ReactiveCommand.CreateAsyncTask(async _ => await this.OpenFeatureFileImplementation());

            this.OpenFromDmsCommand = ReactiveCommand.CreateAsyncTask(async _ =>
            {
                var dmslvm = new DmsLookupViewModel(this.dialogService);
                this.dialogService.OpenDmsLookup(dmslvm);
                return await this.LoadDataset(dmslvm);
            });
            this.OpenFromDmsCommand.Where(dsvm => dsvm != null).Subscribe(ds => this.ProjectManager.Datasets.Add(ds));

            // Create command to open settings window
            this.OpenSettingsCommand = ReactiveCommand.Create();
            this.OpenSettingsCommand.Subscribe(
                _ => this.dialogService.OpenSettings(new SettingsViewModel(SingletonProjectManager.Instance.ProjectInfo, this.dialogService)));

            // Create command to open about box
            var openAboutBoxCommand = ReactiveCommand.Create();
            openAboutBoxCommand.Subscribe(_ => this.dialogService.OpenAboutBox());
            this.OpenAboutBoxCommand = openAboutBoxCommand;

            // Create command to open new modification management window
            var openManageModificationsCommand = ReactiveCommand.Create();
            openManageModificationsCommand.Subscribe(_ => this.ManageModificationsImplementation());
            this.OpenManageModificationsCommand = openManageModificationsCommand;

            // Create MSPathFinder search command
            this.RunMsPathFinderSearchCommand = ReactiveCommand.Create();
            this.RunMsPathFinderSearchCommand.SelectMany(async _ => await this.RunMsPathFinderSearchImplementation())
                                             .Where(dsvm => dsvm != null)
                                             .Subscribe(dsvm => this.ProjectManager.Datasets.Add(dsvm));

            // Create export command
            var exportResultsCommand = ReactiveCommand.Create(this.ProjectManager.Datasets.WhenAnyValue(x => x.Count).Select(count => count > 0));
            exportResultsCommand.Subscribe(_ => this.ExportResultsImplementation());
            this.ExportResultsCommand = exportResultsCommand;

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
        /// The project loader for the project open in this application.
        /// </summary>
        public SingletonProjectManager ProjectManager { get; private set; }

        /// <summary>
        /// Gets command that opens a dialog box prompting the user for a raw file path,
        /// feature file path, and ID file path, then opens the files.
        /// </summary>
        public ReactiveCommand<DatasetViewModel> OpenDataSetCommand { get; private set; }

        /// <summary>
        /// Gets command that prompts the user for raw file(s) to open.
        /// </summary>
        public ReactiveCommand<DatasetViewModel[]> OpenRawFileCommand { get; private set; }
        
        /// <summary>
        /// Gets command that prompts the user for id file to open.
        /// </summary>
        public ReactiveCommand<Unit> OpenTsvFileCommand { get; private set; }
        
        /// <summary>
        /// Gets command that prompts user for feature file to open.
        /// </summary>
        public ReactiveCommand<Unit> OpenFeatureFileCommand { get; private set; }
        
        /// <summary>
        /// Gets command that opens the DMS search dialog.
        /// </summary>
        public ReactiveCommand<DatasetViewModel> OpenFromDmsCommand { get; private set; }
        
        /// <summary>
        /// Gets command that opens settings window.
        /// </summary>
        public ReactiveCommand<object> OpenSettingsCommand { get; private set; }
        
        /// <summary>
        /// Gets command that opens about box.
        /// </summary>
        public ReactiveCommand<object> OpenAboutBoxCommand { get; private set; }
        
        /// <summary>
        /// Gets command that opens a window for managing registered modifications.
        /// </summary>
        public IReactiveCommand<object> OpenManageModificationsCommand { get; private set; }

        /// <summary>
        /// Gets command that runs an MSPathFinder database search.
        /// </summary>
        public ReactiveCommand<object> RunMsPathFinderSearchCommand { get; private set; }

        /// <summary>
        /// Gets a command that exports results of a data set to a file.
        /// </summary>
        public ReactiveCommand<object> ExportResultsCommand { get; private set; }

        /// <summary>
        /// Gets view model for list of scans and identifications.
        /// </summary>
        public ScanViewModel ScanViewModel { get; private set; }
      
        /// <summary>
        /// Gets a value indicating whether or not "Open From DMS" should be shown on the menu based on whether
        /// or not the user is on the PNNL network or not.
        /// </summary>
        public bool ShowOpenFromDms
        {
            get { return System.Net.Dns.GetHostEntry(string.Empty).HostName.Contains("pnl.gov"); }
        }

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
            manageModificationsViewModel.Modifications.AddRange(this.ProjectManager.ProjectInfo.ModificationSettings.RegisteredModifications);
            this.dialogService.OpenManageModifications(manageModificationsViewModel);

            // Update all sequences with new modifications.
            foreach (var prsm in this.ScanViewModel.Data)
            {
                prsm.UpdateModifications();
            }
        }

        /// <summary>
        /// Implementation for <see cref="RunMsPathFinderSearchCommand"/>.
        /// Runs an MSPathFinder database search.
        /// </summary>
        private IObservable<DatasetViewModel> RunMsPathFinderSearchImplementation()
        {
            var searchSettings = new SearchSettingsViewModel(this.dialogService);
            this.dialogService.SearchSettingsWindow(searchSettings);

            return searchSettings.RunCommand.WhenAnyValue(x => x)
                                 .Merge(searchSettings.CancelCommand)
                                 .SelectMany(async _ => await this.LoadDataset(searchSettings));
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
        private async Task<DatasetViewModel> LoadDataset(IDatasetInfoProvider provider)
        {
            DatasetViewModel datasetVm = null;
            try
            {
                var dsInfo = provider.GetDatasetInfo();
                datasetVm = await this.LoadDataset(dsInfo);
            }
            catch (FileNotFoundException e)
            {
                this.dialogService.ExceptionAlert(e);
            }

            return datasetVm;
        }

        /// <summary>
        /// Load dataset from dataset information and add it to dataset list.
        /// </summary>
        /// <param name="datasetInfo">Path to raw file to open</param>
        /// <returns>Task that returns a <see cref="DatasetViewModel" /> for the loaded data set.</returns>
        private async Task<DatasetViewModel> LoadDataset(DatasetInfo datasetInfo)
        {
            var dataSetViewModel = new DatasetViewModel(datasetInfo, this.dialogService); // create data set view model
            try
            {
                // Load dataset.
                await dataSetViewModel.InitializeAsync();

                // Load IDs and FASTAs into ProteinTree.
                this.ScanViewModel.Data.AddRange(dataSetViewModel.ScanViewModel.Data.Where(p => p.Sequence.Count > 0));
                var fastas = dataSetViewModel.DatasetInfo.GetFastaFilePaths();
                foreach (var fasta in fastas)
                {
                    await
                        this.ScanViewModel.IdTree.AddFastaEntriesAsync(
                            await Task.Run(() => FastaReaderWriter.ReadFastaFile(fasta)));
                }
            }
            catch (Exception)
            {
                this.dialogService.ExceptionAlert(new Exception(string.Format("Cannot read {0}.", Path.GetFileNameWithoutExtension(dataSetViewModel.DatasetInfo.Name))));
                if (this.ProjectManager.Datasets.Count > 0 && this.ProjectManager.Datasets.Contains(dataSetViewModel))
                {
                    this.ProjectManager.Datasets.Remove(dataSetViewModel);
                }
            }

            return dataSetViewModel;
        }
    }
}