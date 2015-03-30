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
using LcmsSpectator.TaskServices;
using LcmsSpectator.Utils;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
    public class MainWindowViewModel: ReactiveObject
    {
        /// <summary>
        /// Constructor for creating a new, empty MainWindowViewModel
        /// </summary>
        /// <param name="dialogService">Service for MVVM-friendly dialogs</param>
        /// <param name="taskService">Service for task queueing</param>
        public MainWindowViewModel(IMainDialogService dialogService, ITaskService taskService)
        {
            _dialogService = dialogService;
            _taskService = taskService;

            // Initialize child view models
            ScanViewModel = new ScanViewModel(_dialogService, TaskServiceFactory.GetTaskServiceLike(_taskService), new List<PrSm>());
            DataSets = new ReactiveList<DataSetViewModel> {ChangeTrackingEnabled = true};
            CreateSequenceViewModel = new CreateSequenceViewModel(DataSets, _dialogService);
            LoadingScreenViewModel = new LoadingScreenViewModel();

            // Create commands for file operations
            OpenDataSetCommand = ReactiveCommand.CreateAsyncTask(async _ => await OpenDataSetImpl());
            OpenRawFileCommand = ReactiveCommand.CreateAsyncTask(async _ => await OpenRawFileImpl());
            OpenTsvFileCommand = ReactiveCommand.CreateAsyncTask(async _ => await OpenIdFileImpl());
            OpenFeatureFileCommand = ReactiveCommand.CreateAsyncTask(async _ => await OpenFeatureFileImpl());
            OpenFromDmsCommand = ReactiveCommand.CreateAsyncTask(async _ => await OpenFromDmsImpl());

            // Create command to open settings window
            var openSettingsCommand = ReactiveCommand.Create();
            openSettingsCommand.Subscribe(_ => _dialogService.OpenSettings(new SettingsViewModel(_dialogService)));
            OpenSettingsCommand = openSettingsCommand;

            // Create command to open about box
            var openAboutBoxCommand = ReactiveCommand.Create();
            openAboutBoxCommand.Subscribe(_ => _dialogService.OpenAboutBox());
            OpenAboutBoxCommand = openAboutBoxCommand;

            // Create command to open new modification registration box
            var openRegisterModificationCommand = ReactiveCommand.Create();
            openRegisterModificationCommand.Subscribe(_ => RegisterNewModification("", false));
            OpenRegisterModificationCommand = openRegisterModificationCommand;

            ShowSplash = true;

            // When a data set sets its ReadyToClose property to true, remove it from dataset list
            DataSets.ItemChanged.Where(x => x.PropertyName == "ReadyToClose")
                .Select(x => x.Sender).Where(sender => sender.ReadyToClose)
                .Subscribe(dataSet =>
                {
                    ScanViewModel.RemovePrSmsFromRawFile(dataSet.RawFileName);
                    DataSets.Remove(dataSet);
                });

            // If all datasets are closed, show splash screen
            DataSets.BeforeItemsRemoved.Subscribe(x => ShowSplash = DataSets.Count == 1);

            // When a PrSm is selected in the Protein Tree, make all data sets show the PrSm
            ScanViewModel.WhenAnyValue(x => x.SelectedPrSm)
                .Where(selectedPrSm => selectedPrSm != null)
                .Subscribe(selectedPrSm =>
                {
                    foreach (var dataSet in DataSets) dataSet.SelectedPrSm = selectedPrSm;
                });

            // Warm up Informed Proteomics
            Task.Run(() => Averagine.GetIsotopomerEnvelopeFromNominalMass(50000));
        }

        // Commands
        public IReactiveCommand OpenDataSetCommand { get; private set; }
        public IReactiveCommand OpenRawFileCommand { get; private set; }
        public IReactiveCommand OpenTsvFileCommand { get; private set; }
        public IReactiveCommand OpenFeatureFileCommand { get; private set; }
        public IReactiveCommand OpenFromDmsCommand { get; private set; }
        public IReactiveCommand OpenSettingsCommand { get; private set; }
        public IReactiveCommand OpenAboutBoxCommand { get; private set; }
        public IReactiveCommand OpenRegisterModificationCommand { get; private set; }

        // Child view models
        public ScanViewModel ScanViewModel { get; private set; }
        public CreateSequenceViewModel CreateSequenceViewModel { get; private set; }
        public ReactiveList<DataSetViewModel> DataSets { get; private set; }
        public LoadingScreenViewModel LoadingScreenViewModel { get; private set; }

        /// <summary>
        /// Determine whether or not "Open From DMS" should be shown on the menu based on whether
        /// or not the user is on the PNNL network or not.
        /// </summary>
        public bool ShowOpenFromDms
        {
            get { return System.Net.Dns.GetHostEntry("").HostName.Contains("pnl.gov"); }
        }

        private bool _showSplash;
        /// <summary>
        /// Toggles whether or not splash screen is shown.
        /// </summary>
        public bool ShowSplash
        {
            get { return _showSplash; }
            set { this.RaiseAndSetIfChanged(ref _showSplash, value); }
        }

        /// <summary>
        /// Attempt to open Ids from identification file and associate raw file with them.
        /// </summary>
        /// <param name="idFileName">Name of id file.</param>
        /// <param name="rawFileName">Name of raw file to associate with id file.</param>
        /// <param name="dsVm">Data Set View model to associate with id file.</param>
        public void ReadIdFile(string idFileName, string rawFileName, DataSetViewModel dsVm)
        {
            var ids = new IdentificationTree();
            bool attemptToReadFile = true;
            var modIgnoreList = new List<string>();
            try
            {
                dsVm.MsPfParameters = MsPfParameters.ReadFromIdFilePath(idFileName);
            }
            catch (FormatException e)
            {
                _dialogService.ExceptionAlert(!String.IsNullOrEmpty(e.Message) ? e
                    : new Exception("MsPathFinder param file is incorrectly formatted."));
            }
            do
            {
                try
                {
                    var reader = IdFileReaderFactory.CreateReader(idFileName);
                    ids = reader.Read(modIgnoreList);
                    ids.SetLcmsRun(dsVm.Lcms, dsVm.RawFileName);
                    attemptToReadFile = false;
                }
                catch (InvalidModificationNameException e)
                {
                    // file contains an invalid modification
                    var result =
                    _dialogService.ConfirmationBox(
                        String.Format(
                            "{0}\nWould you like to add this modification?\nIf not, all sequences containing this modification will be ignored.",
                            e.Message),
                        "Unknown Modification");
                    if (!result || !RegisterNewModification(e.ModificationName, true)) modIgnoreList.Add(e.ModificationName);
                }
                catch (KeyNotFoundException e)
                {
                    // file does not have correct headers
                    _dialogService.ExceptionAlert(e);
                    LoadingScreenViewModel.IsLoading = false;
                    return;
                }
                catch (IOException e)
                {
                    // unable to read or open file.
                    _dialogService.ExceptionAlert(e);
                    LoadingScreenViewModel.IsLoading = false;
                    return;
                }
            } while (attemptToReadFile);
            var data = ScanViewModel.Data;
            var prsms = ids.AllPrSms;
            dsVm.ScanViewModel.AddIds(prsms);
            data.AddRange(prsms);
            ScanViewModel.AddIds(data);
            ScanViewModel.HideUnidentifiedScans = true;
        }

        /// <summary>
        /// Open raw file
        /// </summary>
        /// <param name="rawFilePath">Path to raw file to open</param>
        public async Task<DataSetViewModel> ReadRawFile(string rawFilePath)
        {
            var dsVm = new DataSetViewModel(_dialogService, TaskServiceFactory.GetTaskServiceLike(_taskService)); // create data set view model
            DataSets.Add(dsVm); // add data set view model. Can only add to ObservableCollection in thread that created it (gui thread)
            CreateSequenceViewModel.SelectedDataSetViewModel = DataSets[0];
            await dsVm.InitializeAsync(rawFilePath);
            return dsVm;
        }

        public async Task<DataSetViewModel> OpenDataSet(string rawFilePath, string idFilePath="", string featureFilePath="", ToolType? toolType=null)
        {
            var dsVm = await ReadRawFile(rawFilePath);
            OpenDataSet(dsVm, idFilePath, featureFilePath, toolType);
            return dsVm;
        }

        /// <summary>
        /// Open data set given existing data set view model
        /// </summary>
        /// <param name="dsVm">Existing datasetview model to open data set for</param>
        /// <param name="idFilePath">Path to a MS-GF+ or MS-PathFinder results file</param>
        /// <param name="featureFilePath">Path to ProMex results file.</param>
        /// <param name="toolType">Type of the ID tool used for this data set.</param>
        private void OpenDataSet(DataSetViewModel dsVm, string idFilePath = "", string featureFilePath = "", ToolType? toolType=null)
        {
            if (toolType != null && toolType == ToolType.MsPathFinder) dsVm.XicViewModel.PrecursorViewMode = PrecursorViewMode.Charges;
            if (!String.IsNullOrEmpty(idFilePath)) ReadIdFile(idFilePath, dsVm.RawFileName, dsVm);
            if (!String.IsNullOrEmpty(featureFilePath)) dsVm.FeatureMapViewModel.OpenFeatureFile(featureFilePath);
        }

        /// <summary>
        /// Open data set (raw file and ID files) from PNNL DMS system
        /// </summary>
        private async Task OpenFromDms(DmsLookupViewModel dmsLookUp)
        {
            var idFilePath = dmsLookUp.GetIdFileName();
            var featureFilePath = dmsLookUp.GetFeatureFileName();
            var rawFileNames = dmsLookUp.GetRawFileNames();
            var toolType = dmsLookUp.GetTool();
            if (rawFileNames == null || rawFileNames.Count == 0)
            {   // no data set chosen or no raw files found for data set
                throw new Exception("No raw files found for that data set.");
            }
            foreach (var rawFilePath in rawFileNames)
            {
                await OpenDataSet(rawFilePath, idFilePath, featureFilePath, toolType);
            }
        }

        /// <summary>
        /// Get raw files in a given directory.
        /// This method is not recursive.
        /// </summary>
        /// <param name="directoryPath">Directory path to search for raw files in.</param>
        /// <returns>IEnumerable of raw file names.</returns>
        private static IEnumerable<string> GetRawFilesInDir(string directoryPath)
        {
            var rawFiles = new List<string>();
            var directory = Directory.GetFiles(directoryPath);
            foreach (var file in directory) // Raw file in same directory as tsv file?
            {
                var lPath = directoryPath.ToLower();
                var lFile = file.ToLower();
                if (lFile == lPath + ".raw") rawFiles.Add(lPath + ".raw");
                else if (lFile == lPath + ".mzml") rawFiles.Add(lPath + ".mzml");
                else if (lFile == lPath + ".mzml.gz") rawFiles.Add(lPath + ".mzml.gz");
            }
            return rawFiles;
        }

        /// <summary>
        /// Prompt user for modification mass or formula and register it with the application
        /// </summary>
        /// <param name="modificationName">Name of the modification to register</param>
        /// <param name="modificationNameEditable">Should the modification name be editable by the user?</param>
        /// <returns>Whether or not a modification was successfully registered.</returns>
        private bool RegisterNewModification(string modificationName, bool modificationNameEditable)
        {
            var customModVm = new CustomModificationViewModel(modificationName, modificationNameEditable, _dialogService);
            GuiInvoker.Invoke(() => _dialogService.OpenCustomModification(customModVm));
            if (!customModVm.Status) return false;
            if (customModVm.FromFormulaChecked)
                IcParameters.Instance.RegisterModification(customModVm.ModificationName,
                    customModVm.Composition);
            else if (customModVm.FromMassChecked)
                IcParameters.Instance.RegisterModification(customModVm.ModificationName, customModVm.Mass);
            return true;
        }
        
        /// <summary>
        /// Implementation for OpenDataSetCommand.
        /// Prompts the user for a raw/mzml file path, id file path, and feature file path and opens them.
        /// </summary>
        private async Task OpenDataSetImpl()
        {
            var openDataVm = new OpenDataWindowViewModel(_dialogService);
            await OpenDataSet(openDataVm.RawFilePath, openDataVm.IdFilePath, openDataVm.FeatureFilePath);
        }

        /// <summary>
        /// Implementation for OpenRawFileCommand.
        /// Prompts the user for raw/mzml files and opens them.
        /// </summary>
        private async Task OpenRawFileImpl()
        {
            var rawFileNames = _dialogService.MultiSelectOpenFile(".raw", FileConstants.RawFileFormatString);
            if (rawFileNames == null) return;
            ShowSplash = false;
            foreach (var rawFileName in rawFileNames)
            {
                var name = rawFileName;
                string fileName = rawFileName;
                await Task.Run(async () =>
                {
                    try
                    {
                        await ReadRawFile(name);
                    }
                    catch (Exception)
                    {
                        _dialogService.ExceptionAlert(new Exception(String.Format("Cannot read {0}.", fileName)));
                        if (DataSets.Count > 0) GuiInvoker.Invoke(() => DataSets.RemoveAt(DataSets.Count - 1));
                    }
                    if (DataSets.Count > 0) ScanViewModel.HideUnidentifiedScans = false;
                });
            }
        }

        /// <summary>
        /// Implementation of OpenIdFileCommand.
        /// Prompts the user for a path for an ID file and opens it.
        /// </summary>
        public async Task OpenIdFileImpl()
        {
            var idFilePath = _dialogService.OpenFile(".txt", FileConstants.IdFileFormatString);
            if (String.IsNullOrEmpty(idFilePath)) return;
            var fileName = Path.GetFileNameWithoutExtension(idFilePath);
            string rawFileName = "";
            DataSetViewModel dsVm = DataSets.FirstOrDefault(ds => ds.RawFileName == fileName);
            if (dsVm == null)  // Raw file not already open
            {
                var directoryName = Path.GetDirectoryName(idFilePath);
                if (directoryName != null)
                {
                    rawFileName = GetRawFilesInDir(directoryName).FirstOrDefault();
                    if (rawFileName == "")  // Raw file was not in the same directory.
                    {   // prompt user for raw file path
                        var selectDataVm = new SelectDataSetViewModel(_dialogService, DataSets);
                        if (_dialogService.OpenSelectDataWindow(selectDataVm))
                        {
                            // manually find raw file
                            rawFileName = selectDataVm.RawFilePath ?? "";
                            if (!String.IsNullOrEmpty(selectDataVm.RawFilePath))
                            {
                                rawFileName = selectDataVm.SelectedDataSet.RawFileName;
                                dsVm = selectDataVm.SelectedDataSet;
                            }
                        }
                    }
                }
            }
            if (!String.IsNullOrEmpty(rawFileName))
            {
                ShowSplash = false;
                if (dsVm == null) await OpenDataSet(rawFileName, fileName);
                else OpenDataSet(dsVm, fileName);
            }
            else _dialogService.MessageBox("Cannot open id file.");
        }

        /// <summary>
        /// Implmentation for OpenFeatureFileCommand.
        /// Prompts the user for a path for a ProMex feature file and opens it.
        /// </summary>
        private async Task OpenFeatureFileImpl()
        {
            var ftFilePath = _dialogService.OpenFile(".ms1ft", FileConstants.IdFileFormatString);
            if (ftFilePath == "") return;
            var fileName = Path.GetFileNameWithoutExtension(ftFilePath);
            string rawFileName = "";
            DataSetViewModel dsVm = DataSets.FirstOrDefault(ds => ds.RawFileName == fileName);
            if (dsVm == null)  // Raw file not already open
            {
                var directoryName = Path.GetDirectoryName(ftFilePath);
                if (directoryName != null)
                {
                    rawFileName = GetRawFilesInDir(directoryName).FirstOrDefault();
                    if (rawFileName == "")  // Raw file was not in the same directory.
                    {   // prompt user for raw file path
                        var selectDataVm = new SelectDataSetViewModel(_dialogService, DataSets);
                        if (_dialogService.OpenSelectDataWindow(selectDataVm))
                        {
                            // manually find raw file
                            rawFileName = selectDataVm.RawFilePath ?? "";
                            if (!String.IsNullOrEmpty(selectDataVm.RawFilePath))
                            {
                                rawFileName = selectDataVm.SelectedDataSet.RawFileName;
                                dsVm = selectDataVm.SelectedDataSet;
                            }
                        }
                    }
                }
            }
            if (!String.IsNullOrEmpty(rawFileName))
            {
                ShowSplash = false;
                if (dsVm == null) await OpenDataSet(rawFileName, featureFilePath: fileName);
                else OpenDataSet(dsVm, featureFilePath: fileName);
            }
            else _dialogService.MessageBox("Cannot open feature file.");
        }

        /// <summary>
        /// Implementation for OpenFromDmsCommand.
        /// Prompts the user for a dataset and job name from the PNNL DMS system.
        /// </summary>
        private async Task OpenFromDmsImpl()
        {
            var dmsLookUp = new DmsLookupViewModel(_dialogService);
            var data = _dialogService.OpenDmsLookup(dmsLookUp);
            if (data == null) return;
            try
            {
                ShowSplash = false;
                await OpenFromDms(dmsLookUp);
            }
            catch (Exception e)
            {
                ShowSplash = true;
                _dialogService.ExceptionAlert(e);
            }
        }

        private readonly IMainDialogService _dialogService;
        private readonly ITaskService _taskService;
    }
}

