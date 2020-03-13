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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.Config;
using LcmsSpectator.DialogServices;
using LcmsSpectator.Models;
using LcmsSpectator.Readers;
using LcmsSpectator.Utils;
using LcmsSpectator.ViewModels.Data;
using LcmsSpectator.ViewModels.Dms;
using LcmsSpectator.ViewModels.FileSelectors;
using LcmsSpectator.ViewModels.Modifications;
using LcmsSpectator.ViewModels.StableIsotopeViewer;
using LcmsSpectator.Writers;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
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
        /// The load progress for the id file.
        /// </summary>
        private double idFileLoadProgress;

        /// <summary>
        /// A value indicating whether an ID file is loading.
        /// </summary>
        private bool idFileLoading;

        /// <summary>
        /// Default constructor to support WPF design-time use
        /// </summary>
        [Obsolete("For WPF Design-time use only.", true)]
        public MainWindowViewModel()
        {
        }

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
            DataSets = new ReactiveList<DataSetViewModel> { ChangeTrackingEnabled = true };
            CreateSequenceViewModel = new CreateSequenceViewModel(this.dialogService);
            ScanViewModel = new ScanViewModel(this.dialogService, new List<PrSm>());

            // Remove filter by unidentified scans from ScanViewModel filters
            ScanViewModel.Filters.Remove(ScanViewModel.Filters.FirstOrDefault(f => f.Name == "Hide Unidentified Scans"));

            // Create commands for file operations
            OpenDataSetCommand = ReactiveCommand.CreateFromTask(async _ => await OpenDataSetImplementation());
            OpenRawFileCommand = ReactiveCommand.CreateFromTask(async _ => await OpenRawFileImplementation());
            OpenTsvFileCommand = ReactiveCommand.CreateFromTask(async _ => await OpenIdFileImplementation());
            OpenFeatureFileCommand = ReactiveCommand.CreateFromTask(async _ => await OpenFeatureFileImplementation());
            OpenFromDmsCommand = ReactiveCommand.CreateFromTask(async _ => await OpenFromDmsImplementation());

            // Create command to open settings window
            OpenSettingsCommand = ReactiveCommand.Create(() => this.dialogService.OpenSettings(new SettingsViewModel(this.dialogService)));

            // Create command to open isotopic profile viewer
            OpenIsotopicProfileViewerCommand = ReactiveCommand.Create(OpenIsotopicProfileViewer);

            //this.OpenIsotopicProfileViewer(new object());

            // Create command to open about box
            OpenAboutBoxCommand = ReactiveCommand.Create(() => this.dialogService.OpenAboutBox());

            // Create command to open new modification management window
            OpenManageModificationsCommand = ReactiveCommand.Create(ManageModificationsImplementation);

            // Create MSPathFinder search command
            RunMsPathFinderSearchCommand = ReactiveCommand.Create(RunMsPathFinderSearchImplementation);

            // Create export command
            ExportResultsCommand = ReactiveCommand.Create(ExportResultsImplementation, DataSets.WhenAnyValue(x => x.Count).Select(count => count > 0));

            // Create export command
            QuitProgramCommand = ReactiveCommand.Create(() => this.dialogService.QuitProgram());

            ShowSplash = true;

            // When a data set sets its ReadyToClose property to true, remove it from dataset list
            DataSets.ItemChanged.Where(x => x.PropertyName == "ReadyToClose")
                .Select(x => x.Sender).Where(sender => sender.ReadyToClose)
                .Subscribe(dataSet =>
                {
                    ScanViewModel.RemovePrSmsFromRawFile(dataSet.Title);
                    DataSets.Remove(dataSet);
                });

            // If all datasets are closed, show splash screen
            DataSets.BeforeItemsRemoved.Subscribe(x => ShowSplash = DataSets.Count == 1);

            // If a dataset is opened, show splash screen
            DataSets.BeforeItemsAdded.Subscribe(x => ShowSplash = false);

            // When the data reader is reading an ID file, show the loading screen
            this.dataReader.WhenAnyValue(x => x.ReadingIdFiles)
                .Subscribe(readingIdFiles => IdFileLoading = readingIdFiles);

            // When a PrSm is selected in the Protein Tree, make all data sets show the PrSm
            ScanViewModel.WhenAnyValue(x => x.SelectedPrSm)
                .Where(selectedPrSm => selectedPrSm != null)
                .Subscribe(selectedPrSm =>
                    {
                        foreach (var dataSet in DataSets)
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
        public ReactiveCommand<Unit, Unit> OpenDataSetCommand { get; }

        /// <summary>
        /// Gets command that prompts the user for raw file(s) to open.
        /// </summary>
        public ReactiveCommand<Unit, Unit> OpenRawFileCommand { get; }

        /// <summary>
        /// Gets command that prompts the user for id file to open.
        /// </summary>
        public ReactiveCommand<Unit, Unit> OpenTsvFileCommand { get; }

        /// <summary>
        /// Gets command that prompts user for feature file to open.
        /// </summary>
        public ReactiveCommand<Unit, Unit> OpenFeatureFileCommand { get; }

        /// <summary>
        /// Gets command that opens the DMS search dialog.
        /// </summary>
        public ReactiveCommand<Unit, Unit> OpenFromDmsCommand { get; }

        /// <summary>
        /// Gets command that opens settings window.
        /// </summary>
        public ReactiveCommand<Unit, bool> OpenSettingsCommand { get; }

        /// <summary>
        /// Gets a command that opens the isotopic profile viewer.
        /// </summary>
        public ReactiveCommand<Unit, Unit> OpenIsotopicProfileViewerCommand { get; }

        /// <summary>
        /// Gets command that opens about box.
        /// </summary>
        public ReactiveCommand<Unit, Unit> OpenAboutBoxCommand { get; }

        /// <summary>
        /// Gets command that opens a window for managing registered modifications.
        /// </summary>
        public ReactiveCommand<Unit, Unit> OpenManageModificationsCommand { get; }

        /// <summary>
        /// Gets command that runs an MSPathFinder database search.
        /// </summary>
        public ReactiveCommand<Unit, Unit> RunMsPathFinderSearchCommand { get; }

        /// <summary>
        /// Gets a command that exports results of a data set to a file.
        /// </summary>
        public ReactiveCommand<Unit, Unit> ExportResultsCommand { get; }

        /// <summary>
        /// Gets a command that exits the program
        /// </summary>
        public ReactiveCommand<Unit, Unit> QuitProgramCommand { get; }

        /// <summary>
        /// Gets view model for list of scans and identifications.
        /// </summary>
        public ScanViewModel ScanViewModel { get; }

        /// <summary>
        /// Gets view model for editing sequence, charge, and scan number.
        /// </summary>
        public CreateSequenceViewModel CreateSequenceViewModel { get; }

        /// <summary>
        /// Gets list of open data sets.
        /// </summary>
        public ReactiveList<DataSetViewModel> DataSets { get; }

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
            get => showSplash;
            private set => this.RaiseAndSetIfChanged(ref showSplash, value);
        }

        /// <summary>
        /// Gets the load progress for the id file.
        /// </summary>
        public double IdFileLoadProgress
        {
            get => idFileLoadProgress;
            private set => this.RaiseAndSetIfChanged(ref idFileLoadProgress, value);
        }

        /// <summary>
        /// Gets a value indicating whether an ID file is loading.
        /// </summary>
        public bool IdFileLoading
        {
            get => idFileLoading;
            private set => this.RaiseAndSetIfChanged(ref idFileLoading, value);
        }

        /// <summary>
        /// Implementation for <see cref="OpenDataSetCommand" />.
        /// Prompts the user for a raw file path, id file path, and feature file path and opens them.
        /// </summary>
        /// <returns>Task that creates and opens a data set view model.</returns>
        private async Task OpenDataSetImplementation()
        {
            var openDataViewModel = new OpenDataWindowViewModel(dialogService);
            if (dialogService.OpenDataWindow(openDataViewModel))
            {
                var dataSetViewModel = await ReadRawFile(openDataViewModel.RawFilePath);
                dataSetViewModel.FastaDbFilePath = openDataViewModel.FastaFilePath;

                if (!string.IsNullOrWhiteSpace(openDataViewModel.FastaFilePath) && File.Exists(openDataViewModel.FastaFilePath))
                {
                    await ScanViewModel.IdTree.AddFastaEntriesAsync(await dataReader.ReadFastaFile(openDataViewModel.FastaFilePath));
                }

                await ReadIdFile(openDataViewModel.IdFilePath, dataSetViewModel);
                await dataReader.OpenDataSet(dataSetViewModel, openDataViewModel.RawFilePath, featureFilePath: openDataViewModel.FeatureFilePath);
            }
        }

        /// <summary>
        /// Implementation for <see cref="OpenRawFileCommand" />.
        /// Prompts the user for raw files and opens them.
        /// </summary>
        /// <returns>
        /// Task that adds a data set view model to data set list for each raw file selected by the user
        /// </returns>
        private async Task OpenRawFileImplementation()
        {
            var rawFilePaths = dialogService.MultiSelectOpenFile(FileConstants.RawFileExtensions[0], MassSpecDataReaderFactory.MassSpecDataTypeFilterString);
            if (rawFilePaths == null)
            {
                return; // The user did not select any raw files.
            }

            foreach (var rawFilePath in rawFilePaths)
            {
                await ReadRawFile(rawFilePath);
            }
        }

        /// <summary>
        /// Implementation of <see cref="OpenTsvFileCommand" />.
        /// Prompts the user for a path for an ID file and opens it.
        /// </summary>
        /// <returns>Task that opens an ID file and associates it with a raw file.</returns>
        private async Task OpenIdFileImplementation()
        {
            var idFilePath = dialogService.OpenFile(FileConstants.IdFileExtensions[0], FileConstants.IdFileFormatString);
            if (string.IsNullOrEmpty(idFilePath))
            {
                return;
            }

            if (!(await OpenResultFile(idFilePath)))
            {
                dialogService.MessageBox(string.Format("Cannot read ID file: {0}", idFilePath));
            }
        }

        /// <summary>
        /// Implementation for <see cref="OpenFeatureFileCommand" />.
        /// Prompts the user for a path for a feature list file and opens it.
        /// </summary>
        /// <returns>Task that opens a feature file and associates it with a data set</returns>
        private async Task OpenFeatureFileImplementation()
        {
            var featureFilePath = dialogService.OpenFile(FileConstants.FeatureFileExtensions[0], FileConstants.FeatureFileFormatString);
            if (string.IsNullOrEmpty(featureFilePath))
            {
                return;
            }

            if (!(await OpenResultFile(featureFilePath)))
            {
                dialogService.MessageBox(string.Format("Cannot read feature file: {0}", featureFilePath));
            }
        }

        /// <summary>
        /// Implementation for <see cref="OpenFromDmsCommand" />.
        /// Prompts the user for a dataset and job name from the PNNL DMS system.
        /// </summary>
        /// <returns>Task that opens a data set from DMS and adds it to the data set list.</returns>
        private async Task OpenFromDmsImplementation()
        {
            var dmsLookUp = new DmsLookupViewModel(dialogService);
            dialogService.OpenDmsLookup(dmsLookUp);
            if (!dmsLookUp.ValidateDataSet() || !dmsLookUp.Status)
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
                    var dataSetViewModel = await ReadRawFile(file);

                    if (dataSetViewModel.MsPfParameters == null)
                    {
                        dataSetViewModel.SetMsPfParameters(idFilePath);
                    }

                    // Set the dataset's FASTA file path
                    if (dataSetViewModel.MsPfParameters != null)
                    {
                        dataSetViewModel.FastaDbFilePath = string.Format(
                            "{0}\\{1}",
                            DmsLookupViewModel.FastaFilePath,
                            dataSetViewModel.MsPfParameters.DatabaseFile);
                    }

                    IdFileLoadProgress = 100;
                    IdFileLoading = true;
                    if (!string.IsNullOrWhiteSpace(dataSetViewModel.FastaDbFilePath) && File.Exists(dataSetViewModel.FastaDbFilePath))
                    {
                        await ScanViewModel.IdTree.AddFastaEntriesAsync(await dataReader.ReadFastaFile(dataSetViewModel.FastaDbFilePath));
                    }

                    await ReadIdFile(idFilePath, dataSetViewModel);
                    await dataReader.OpenDataSet(dataSetViewModel, file, featureFilePath: featureFilePath);
                    IdFileLoadProgress = 0;
                    IdFileLoading = false;
                }
            }
            catch (Exception e)
            {
                dialogService.ExceptionAlert(e);
            }
        }

        /// <summary>
        /// Implementation for <see cref="OpenManageModificationsCommand" />.
        /// Opens a window for editing the registered modifications.
        /// </summary>
        private void ManageModificationsImplementation()
        {
            var manageModificationsViewModel = new ManageModificationsViewModel(dialogService);
            manageModificationsViewModel.Modifications.AddRange(IcParameters.Instance.RegisteredModifications);
            dialogService.OpenManageModifications(manageModificationsViewModel);

            // Update all sequences with new modifications.
            foreach (var prsm in ScanViewModel.Data)
            {
                prsm.UpdateModifications();
            }
        }

        /// <summary>
        /// Implementation for <see cref="RunMsPathFinderSearchCommand"/>.
        /// Runs an MSPathFinder database search.
        /// </summary>
        private void RunMsPathFinderSearchImplementation()
        {
            var searchSettings = new SearchSettingsViewModel(dialogService);

            // TODO: change this so it doesn't use an event and isn't void async
            searchSettings.ReadyToClose += async (o, e) =>
            {
                if (searchSettings.Status)
                {
                    var dataSetViewModel = await ReadRawFile(searchSettings.SpectrumFilePath);
                    dataSetViewModel.FastaDbFilePath = searchSettings.FastaDbFilePath;
                    if (!string.IsNullOrWhiteSpace(dataSetViewModel.FastaDbFilePath) && File.Exists(dataSetViewModel.FastaDbFilePath))
                    {
                        await ScanViewModel.IdTree.AddFastaEntriesAsync(await dataReader.ReadFastaFile(dataSetViewModel.FastaDbFilePath));
                    }

                    string featureFilePath;
                    var defaultFeatureFilePath = searchSettings.GetFeatureFilePath();

                    if (!string.IsNullOrEmpty(defaultFeatureFilePath) && File.Exists(defaultFeatureFilePath))
                    {
                        featureFilePath = defaultFeatureFilePath;
                    }
                    else
                    {
                        featureFilePath = "";
                    }

                    await ReadIdFile(searchSettings.GetIdFilePath(), dataSetViewModel);
                    await dataReader.OpenDataSet(
                            dataSetViewModel,
                            searchSettings.SpectrumFilePath,
                            featureFilePath: featureFilePath);
                }
            };

            dialogService.SearchSettingsWindow(searchSettings);
        }

        /// <summary>
        /// Implementation for <see cref="ExportResultsCommand" />.
        /// Exports results of a data set to a file.
        /// </summary>
        private void ExportResultsImplementation()
        {
            var exportDatasetViewModel = new ExportDatasetViewModel(dialogService, DataSets);

            if (dialogService.ExportDatasetWindow(exportDatasetViewModel))
            {
                var writer = new IcFileWriter(exportDatasetViewModel.OutputFilePath);
                writer.Write(exportDatasetViewModel.SelectedDataset.ScanViewModel.Data.Where(prsm => prsm.Sequence.Count > 0));
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
            if (string.Equals(resultFileExtension, ".gz", StringComparison.OrdinalIgnoreCase))
            {
                // This is a gzip file, e.g. .mzid.gz
                // Determine the extension before .gz
                resultFileExtension = Path.GetExtension(Path.GetFileNameWithoutExtension(resultFilePath)) + ".gz";
            }

            var dataSetViewModel = DataSets.FirstOrDefault(ds => ds.Title == resultFileName);
            string rawFilePath;
            if (dataSetViewModel == null)
            {
                rawFilePath = dataReader.GetRawFilesByDataSetName(
                                    Path.GetDirectoryName(resultFilePath),
                                    resultFileName).FirstOrDefault();
                if (string.IsNullOrEmpty(rawFilePath))
                {
                    var selectDataVm = new SelectDataSetViewModel(dialogService, DataSets);
                    if (dialogService.OpenSelectDataWindow(selectDataVm))
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
                            dataSetViewModel = await ReadRawFile(rawFilePath);
                        }
                    }
                }
                else
                {
                    dataSetViewModel = await ReadRawFile(rawFilePath);
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
                await ReadIdFile(resultFilePath, dataSetViewModel);
                return true;
            }

            if (!string.IsNullOrEmpty(rawFilePath) && dataSetViewModel != null)
            {   // Valid raw file path, DataSetViewModel, and feature file path
                await dataReader.OpenDataSet(dataSetViewModel, rawFilePath, featureFilePath: resultFilePath);
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
            var attemptToReadFile = true;
            var modIgnoreList = new List<string>();
            do
            {
                try
                {
                    await
                        dataReader.OpenDataSet(
                            dataSetViewModel,
                            dataSetViewModel.Title,
                            idFilePath,
                            modIgnoreList: modIgnoreList);
                    attemptToReadFile = false;
                }
                catch (BaseTsvReader.InvalidModificationNameException e)
                {
                    // file contains an unknown modification
                    var result =
                        dialogService.ConfirmationBox(
                            string.Format(
                                "{0}\nWould you like to add this modification?\nIf not, all sequences containing this modification will be ignored.",
                                e.Message),
                            "Unknown Modification");
                    if (!result || !RegisterNewModification(e.ModificationName, true))
                    {
                        modIgnoreList.Add(e.ModificationName);
                    }
                }
                catch (KeyNotFoundException e)
                {
                    // file does not have correct headers
                    dialogService.ExceptionAlert(e);
                    return;
                }
                catch (IOException e)
                {
                    // unable to read or open file.
                    dialogService.ExceptionAlert(e);
                    return;
                }
                catch (Exception e)
                {   // Most likely trying to open a synopsis file while missing some files.
                    dialogService.ExceptionAlert(e);
                    return;
                }
            }
            while (attemptToReadFile);

            var identifications = dataSetViewModel.ScanViewModel.Data.Where(p => p.Sequence.Count > 0).ToList();

            RegisterUnknownModifications(dataReader.Modifications);

            ScanViewModel.Data.AddRange(identifications);

            foreach (var id in identifications)
            {
                if (!ScanViewModel.IdTree.Proteins.ContainsKey(id.ProteinName))
                {
                    ScanViewModel.IdTree.AddFastaEntry(new FastaEntry()
                    {
                        ProteinName = id.ProteinName,
                        ProteinDescription = id.ProteinDesc,
                        ProteinSequence = id.Sequence,
                        ProteinSequenceText = id.SequenceText
                    });
                }
            }
        }

        /// <summary>
        /// Open raw file
        /// </summary>
        /// <param name="rawFilePath">Path to raw file to open</param>
        /// <returns>Task that returns a DataSetViewModel for the data set.</returns>
        private async Task<DataSetViewModel> ReadRawFile(string rawFilePath)
        {
            var dataSetViewModel = new DataSetViewModel(dialogService); // create data set view model
            DataSets.Add(dataSetViewModel); // add data set view model. Can only add to ObservableCollection in thread that created it (gui thread)
            CreateSequenceViewModel.SelectedDataSetViewModel = DataSets[0];
            try
            {
                await dataReader.OpenDataSet(dataSetViewModel, rawFilePath);
            }
            catch (Exception ex)
            {
                dialogService.ExceptionAlert(new Exception(string.Format("Cannot read {0}: {1}", Path.GetFileNameWithoutExtension(rawFilePath), ex.Message)));
                if (DataSets.Count > 0 && DataSets.Contains(dataSetViewModel))
                {
                    DataSets.Remove(dataSetViewModel);
                }
            }

            return dataSetViewModel;
        }

        private void RegisterUnknownModifications(IEnumerable<Modification> modifications)
        {
            foreach (var modification in modifications)
            {
                if (modification.Composition is CompositionWithDeltaMass)
                {
                    IcParameters.Instance.RegisterModification(modification.Name, modification.Mass);
                }
                else
                {
                    IcParameters.Instance.RegisterModification(modification.Name, modification.Composition);
                }
            }
        }

        /// <summary>
        /// Prompt user for modification mass or formula and register it with the application
        /// </summary>
        /// <param name="modificationName">Name of the modification to register</param>
        /// <param name="modificationNameEditable">Should the modification name be editable by the user?</param>
        /// <returns>Whether or not a modification was successfully registered.</returns>
        private bool RegisterNewModification(string modificationName, bool modificationNameEditable)
        {
            var customModVm = new CustomModificationViewModel(modificationName, modificationNameEditable, dialogService);
            dialogService.OpenCustomModification(customModVm);
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

        public void OpenIsotopicProfileViewer()
        {
            var ds = new MainDialogService();
            ds.OpenStableIsotopeViewer(new StableIsotopeViewModel());
        }
    }
}