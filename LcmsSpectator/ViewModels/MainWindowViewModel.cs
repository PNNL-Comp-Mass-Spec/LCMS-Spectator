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
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using InformedProteomics.Backend.Data.Composition;
    using LcmsSpectator.Config;
    using LcmsSpectator.DialogServices;
    using LcmsSpectator.Models;
    using LcmsSpectator.Readers;
    using LcmsSpectator.Utils;
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
        /// Service for reading raw files, id files, and feature files
        /// </summary>
        private readonly IDataReader dataReader;

        /// <summary>
        /// A value indicating whether or not splash screen is visible.
        /// </summary>
        private bool showSplash;

        /// <summary>
        /// Initializes a new instance of the MainWindowViewModel class.
        /// </summary>
        /// <param name="dialogService">Service for view model friendly dialogs</param>
        /// <param name="dataReader">Service for reading raw files, id files, and feature files</param>
        public MainWindowViewModel(IMainDialogService dialogService, IDataReader dataReader)
        {
            this.dialogService = dialogService;
            this.dataReader = dataReader;

            // Initialize child view models
            this.DataSets = new ReactiveList<DataSetViewModel> { ChangeTrackingEnabled = true };
            CreateSequenceViewModel = new CreateSequenceViewModel(this.dialogService);
            LoadingScreenViewModel = new LoadingScreenViewModel();
            ScanViewModel = new ScanViewModel(this.dialogService, new List<PrSm>());

            // Remove filter by unidentified scans from ScanViewModel filters
            var unidentifiedScansFilter = ScanViewModel.Filters.FirstOrDefault(f => f.Name == "Hide Unidentified Scans");
            if (unidentifiedScansFilter != null)
            {
                this.ScanViewModel.Filters.Remove(unidentifiedScansFilter);
            }

            // Create commands for file operations
            this.OpenDataSetCommand = ReactiveCommand.CreateAsyncTask(async _ => await this.OpenDataSetImplementation());
            this.OpenRawFileCommand = ReactiveCommand.CreateAsyncTask(async _ => await this.OpenRawFileImplementation());
            this.OpenTsvFileCommand = ReactiveCommand.CreateAsyncTask(async _ => await this.OpenIdFileImplementation());
            this.OpenFeatureFileCommand = ReactiveCommand.CreateAsyncTask(async _ => await this.OpenFeatureFileImplementation());
            this.OpenFromDmsCommand = ReactiveCommand.CreateAsyncTask(async _ => await this.OpenFromDmsImplementation());

            // Create command to open settings window
            var openSettingsCommand = ReactiveCommand.Create();
            openSettingsCommand.Subscribe(_ => this.dialogService.OpenSettings(new SettingsViewModel(this.dialogService)));
            this.OpenSettingsCommand = openSettingsCommand;

            // Create command to open about box
            var openAboutBoxCommand = ReactiveCommand.Create();
            openAboutBoxCommand.Subscribe(_ => this.dialogService.OpenAboutBox());
            this.OpenAboutBoxCommand = openAboutBoxCommand;

            // Create command to open new modification registration box
            var openRegisterModificationCommand = ReactiveCommand.Create();
            openRegisterModificationCommand.Subscribe(_ => this.RegisterNewModification(string.Empty, false));
            this.OpenRegisterModificationCommand = openRegisterModificationCommand;

            this.ShowSplash = true;

            // When a data set sets its ReadyToClose property to true, remove it from dataset list
            this.DataSets.ItemChanged.Where(x => x.PropertyName == "ReadyToClose")
                .Select(x => x.Sender).Where(sender => sender.ReadyToClose)
                .Subscribe(dataSet =>
                {
                    ScanViewModel.RemovePrSmsFromRawFile(dataSet.Title);
                    DataSets.Remove(dataSet);
                });

            // If all datasets are closed, show splash screen
            this.DataSets.BeforeItemsRemoved.Subscribe(x => this.ShowSplash = this.DataSets.Count == 1);

            // If a dataset is opened, show splash screen
            this.DataSets.BeforeItemsAdded.Subscribe(x => this.ShowSplash = false);

            // When the data reader is reading an ID file, show the loading screen
            this.dataReader.WhenAnyValue(x => x.ReadingIdFiles)
                .Subscribe(readingIdFiles => this.LoadingScreenViewModel.IsLoading = readingIdFiles);

            // When a PrSm is selected in the Protein Tree, make all data sets show the PrSm
            ScanViewModel.WhenAnyValue(x => x.SelectedPrSm)
                .Where(selectedPrSm => selectedPrSm != null)
                .Subscribe(selectedPrSm =>
                    {
                        foreach (var dataSet in this.DataSets)
                        {
                            dataSet.SelectedPrSm = selectedPrSm;
                        }
                    });

            // Warm up InformedProteomics Averagine using arbitrary mass
            Task.Run(() => Averagine.GetIsotopomerEnvelopeFromNominalMass(50000));
        }

        /// <summary>
        /// Gets command that opens a dialog box prompting the user for a raw file path,
        /// feature file path, and ID file path, then opens the files.
        /// </summary>
        public IReactiveCommand OpenDataSetCommand { get; private set; }

        /// <summary>
        /// Gets command that prompts the user for raw file(s) to open.
        /// </summary>
        public IReactiveCommand OpenRawFileCommand { get; private set; }
        
        /// <summary>
        /// Gets command that prompts the user for id file to open.
        /// </summary>
        public IReactiveCommand OpenTsvFileCommand { get; private set; }
        
        /// <summary>
        /// Gets command that prompts user for feature file to open.
        /// </summary>
        public IReactiveCommand OpenFeatureFileCommand { get; private set; }
        
        /// <summary>
        /// Gets command that opens the DMS search dialog.
        /// </summary>
        public IReactiveCommand OpenFromDmsCommand { get; private set; }
        
        /// <summary>
        /// Gets command that opens settings window.
        /// </summary>
        public IReactiveCommand OpenSettingsCommand { get; private set; }
        
        /// <summary>
        /// Gets command that opens about box.
        /// </summary>
        public IReactiveCommand OpenAboutBoxCommand { get; private set; }
        
        /// <summary>
        /// Gets command that opens modification registration box.
        /// </summary>
        public IReactiveCommand OpenRegisterModificationCommand { get; private set; }

        /// <summary>
        /// Gets view model for list of scans and identifications.
        /// </summary>
        public ScanViewModel ScanViewModel { get; private set; }
        
        /// <summary>
        /// Gets view model for editing sequence, charge, and scan number.
        /// </summary>
        public CreateSequenceViewModel CreateSequenceViewModel { get; private set; }
        
        /// <summary>
        /// Gets list of open data sets.
        /// </summary>
        public ReactiveList<DataSetViewModel> DataSets { get; private set; }
        
        /// <summary>
        /// Gets view model for loading screen.
        /// </summary>
        public LoadingScreenViewModel LoadingScreenViewModel { get; private set; }

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
        /// Implementation for OpenDataSetCommand.
        /// Prompts the user for a raw file path, id file path, and feature file path and opens them.
        /// </summary>
        /// <returns>Task that creates and opens a data set view model.</returns>
        private async Task OpenDataSetImplementation()
        {
            var openDataViewModel = new OpenDataWindowViewModel(this.dialogService);
            if (this.dialogService.OpenDataWindow(openDataViewModel))
            {
                var dataSetViewModel = await this.ReadRawFile(openDataViewModel.RawFilePath);
                await this.ReadIdFile(openDataViewModel.IdFilePath, dataSetViewModel);
                await this.dataReader.OpenDataSet(dataSetViewModel, openDataViewModel.RawFilePath, featureFilePath: openDataViewModel.FeatureFilePath);
            }
        }

        /// <summary>
        /// Implementation for OpenRawFileCommand.
        /// Prompts the user for raw files and opens them.
        /// </summary>
        /// <returns>
        /// Task that adds a data set view model to data set list for each raw file selected by the user
        /// </returns>
        private async Task OpenRawFileImplementation()
        {
            var rawFilePaths = this.dialogService.MultiSelectOpenFile(FileConstants.RawFileExtensions[0], FileConstants.RawFileFormatString);
            if (rawFilePaths == null)
            {
                return; // The user did not select any raw files.
            }

            foreach (var rawFilePath in rawFilePaths)
            {
                await this.ReadRawFile(rawFilePath);
            }
        }

        /// <summary>
        /// Implementation of OpenIdFileCommand.
        /// Prompts the user for a path for an ID file and opens it.
        /// </summary>
        /// <returns>Task that opens an ID file and associates it with a raw file.</returns>
        private async Task OpenIdFileImplementation()
        {
            var idFilePath = this.dialogService.OpenFile(FileConstants.IdFileExtensions[0], FileConstants.IdFileFormatString);
            if (string.IsNullOrEmpty(idFilePath))
            {
                return;
            }

            if (!(await this.OpenResultFile(idFilePath)))
            {
                this.dialogService.MessageBox(string.Format("Cannot read ID file: {0}", idFilePath));
            }
        }

        /// <summary>
        /// Implementation for OpenFeatureFileCommand.
        /// Prompts the user for a path for a feature list file and opens it.
        /// </summary>
        /// <returns>Task that opens a feature file and associates it with a data set</returns>
        private async Task OpenFeatureFileImplementation()
        {
            var featureFilePath = this.dialogService.OpenFile(FileConstants.FeatureFileExtensions[0], FileConstants.FeatureFileFormatString);
            if (string.IsNullOrEmpty(featureFilePath))
            {
                return;
            }

            if (!(await this.OpenResultFile(featureFilePath)))
            {
                this.dialogService.MessageBox(string.Format("Cannot read feature file: {0}", featureFilePath));
            }
        }

        /// <summary>
        /// Implementation for OpenFromDMSCommand.
        /// Prompts the user for a dataset and job name from the PNNL DMS system.
        /// </summary>
        /// <returns>Task that opens a data set from DMS and adds it to the data set list.</returns>
        private async Task OpenFromDmsImplementation()
        {
            var dmsLookUp = new DmsLookupViewModel(this.dialogService);
            this.dialogService.OpenDmsLookup(dmsLookUp);
            if (!dmsLookUp.ValidateDataSet())
            {
                return; // data set was not chosen
            }

            try
            {
                var rawFilePaths = dmsLookUp.GetRawFileNames();
                var idFilePath = dmsLookUp.GetIdFileName();
                var featureFilePath = dmsLookUp.GetFeatureFileName();

                foreach (var file in rawFilePaths)
                {
                    var dataSetViewModel = await this.ReadRawFile(file);
                    await this.ReadIdFile(idFilePath, dataSetViewModel);
                    await this.dataReader.OpenDataSet(dataSetViewModel, file, featureFilePath: featureFilePath);
                }
            }
            catch (Exception e)
            {
                this.dialogService.ExceptionAlert(e);
            }
        }

        /// <summary>
        /// Open ID or feature file and attempt to find data set that it should be associated with.
        /// </summary>
        /// <param name="resultFilePath">Path for result file to open and read.</param>
        /// <returns>Task that results in a value indicating whether or not the file was successfully read.</returns>
        private async Task<bool> OpenResultFile(string resultFilePath)
        {
            var resultFileName = Path.GetFileNameWithoutExtension(resultFilePath);
            var resultFileExtension = Path.GetExtension(resultFilePath);
            var dataSetViewModel = this.DataSets.FirstOrDefault(ds => ds.Title == resultFileName);
            string rawFilePath;
            if (dataSetViewModel == null)
            {
                rawFilePath = this.dataReader.GetRawFilesByDataSetName(
                                    Path.GetDirectoryName(resultFilePath),
                                    resultFileName).FirstOrDefault();
                if (string.IsNullOrEmpty(rawFilePath))
                {
                    var selectDataVm = new SelectDataSetViewModel(this.dialogService, this.DataSets);
                    if (this.dialogService.OpenSelectDataWindow(selectDataVm))
                    {
                        // manually find raw file
                        rawFilePath = selectDataVm.RawFilePath ?? string.Empty;
                        if (string.IsNullOrEmpty(selectDataVm.RawFilePath))
                        {
                            rawFilePath = selectDataVm.SelectedDataSet.Title;
                            dataSetViewModel = selectDataVm.SelectedDataSet;
                        }
                        else
                        {
                            dataSetViewModel = await this.ReadRawFile(rawFilePath);
                        }
                    }
                }
            }
            else
            {
                rawFilePath = dataSetViewModel.Title;
            }

            if (!string.IsNullOrEmpty(rawFilePath) &&
                dataSetViewModel != null &&
                !string.IsNullOrEmpty(resultFileExtension) && 
                FileConstants.IdFileExtensions.Contains(resultFileExtension.ToLower()))
            {   // Valid raw file path, DataSetViewModel, and ID file path
                await this.ReadIdFile(resultFilePath, dataSetViewModel);
                return true;
            }

            if (!string.IsNullOrEmpty(rawFilePath) && dataSetViewModel != null)
            {   // Valid raw file path, DataSetViewModel, and feature file path
                await this.dataReader.OpenDataSet(dataSetViewModel, rawFilePath, featureFilePath: resultFilePath);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempt to open Ids from identification file and associate raw file with them.
        /// </summary>
        /// <param name="idFilePath">Name of id file.</param>
        /// <param name="dataSetViewModel">Data Set View model to associate with id file.</param>
        /// <returns>Task for opening identification file.</returns>
        private async Task ReadIdFile(string idFilePath, DataSetViewModel dataSetViewModel)
        {
            bool attemptToReadFile = true;
            var modIgnoreList = new List<string>();
            dataSetViewModel.SetMsPfParameters(idFilePath);
            do
            {
                try
                {
                    await this.dataReader.OpenDataSet(
                                                      dataSetViewModel,
                                                      dataSetViewModel.Title,
                                                      idFilePath,
                                                      modIgnoreList: modIgnoreList);
                    attemptToReadFile = false;
                }
                catch (IcFileReader.InvalidModificationNameException e)
                {   // file contains an unknown modification
                    var result =
                        this.dialogService.ConfirmationBox(
                            string.Format(
                                "{0}\nWould you like to add this modification?\nIf not, all sequences containing this modification will be ignored.",
                                e.Message),
                            "Unknown Modification");
                    if (!result || !this.RegisterNewModification(e.ModificationName, true))
                    {
                        modIgnoreList.Add(e.ModificationName);
                    }
                }
                catch (KeyNotFoundException e)
                {   // file does not have correct headers
                    this.dialogService.ExceptionAlert(e);
                    return;
                }
                catch (IOException e)
                {   // unable to read or open file.
                    this.dialogService.ExceptionAlert(e);
                    return;
                }
            }
            while (attemptToReadFile);

            this.ScanViewModel.Data.AddRange(dataSetViewModel.ScanViewModel.Data);
        }

        /// <summary>
        /// Open raw file
        /// </summary>
        /// <param name="rawFilePath">Path to raw file to open</param>
        /// <returns>Task that returns a DataSetViewModel for the data set.</returns>
        private async Task<DataSetViewModel> ReadRawFile(string rawFilePath)
        {
            var dataSetViewModel = new DataSetViewModel(this.dialogService); // create data set view model
            this.DataSets.Add(dataSetViewModel); // add data set view model. Can only add to ObservableCollection in thread that created it (gui thread)
            CreateSequenceViewModel.SelectedDataSetViewModel = this.DataSets[0];
            try
            {
                await this.dataReader.OpenDataSet(dataSetViewModel, rawFilePath);
            }
            catch (Exception)
            {
                this.dialogService.ExceptionAlert(new Exception(string.Format("Cannot read {0}.", Path.GetFileNameWithoutExtension(rawFilePath))));
                if (this.DataSets.Count > 0 && this.DataSets.Contains(dataSetViewModel))
                {
                    this.DataSets.Remove(dataSetViewModel);
                }
            }

            return dataSetViewModel;
        }

        /// <summary>
        /// Prompt user for modification mass or formula and register it with the application
        /// </summary>
        /// <param name="modificationName">Name of the modification to register</param>
        /// <param name="modificationNameEditable">Should the modification name be editable by the user?</param>
        /// <returns>Whether or not a modification was successfully registered.</returns>
        private bool RegisterNewModification(string modificationName, bool modificationNameEditable)
        {
            var customModVm = new CustomModificationViewModel(modificationName, modificationNameEditable, this.dialogService);
            this.dialogService.OpenCustomModification(customModVm);
            if (!customModVm.Status)
            {
                return false;
            }

            if (customModVm.FromFormulaChecked)
            {
                IcParameters.Instance.RegisterModification(customModVm.ModificationName, customModVm.Composition);
            }
            else if (customModVm.FromMassChecked)
            {
                IcParameters.Instance.RegisterModification(customModVm.ModificationName, customModVm.Mass);
            }

            return true;
        }
    }
}