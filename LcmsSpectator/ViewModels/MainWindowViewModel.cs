using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.DialogServices;
using LcmsSpectator.TaskServices;
using LcmsSpectator.Utils;
using LcmsSpectatorModels.Models;
using LcmsSpectatorModels.Readers;

namespace LcmsSpectator.ViewModels
{
    public class MainWindowViewModel: ViewModelBase
    {
        // Data
        public List<IonType> IonTypes { get; set; }

        // Commands
        public RelayCommand OpenDataSetCommand { get; private set; }
        public RelayCommand OpenRawFileCommand { get; private set; }
        public RelayCommand OpenTsvFileCommand { get; private set; }
        public RelayCommand OpenFeatureFileCommand { get; private set; }
        public RelayCommand OpenFromDmsCommand { get; private set; }
        public RelayCommand OpenSettingsCommand { get; private set; }
        public RelayCommand OpenAboutBoxCommand { get; private set; }

        // Child view models
        public ScanViewModel ScanViewModel { get; private set; }
        public CreateSequenceViewModel CreateSequenceViewModel { get; private set; }
        public ObservableCollection<DataSetViewModel> DataSets { get; private set; }
        public LoadingScreenViewModel LoadingScreenViewModel { get; private set; }

        /// <summary>
        /// Constructor for creating a new, empty MainWindowViewModel
        /// </summary>
        /// <param name="dialogService">Service for MVVM-friendly dialogs</param>
        /// <param name="taskService">Service for task queueing</param>
        public MainWindowViewModel(IMainDialogService dialogService, ITaskService taskService)
        {
            // register messenger events
            Messenger.Default.Register<DataSetCloseRequest>(this, DataSetCloseRequest);

            _dialogService = dialogService;
            _taskService = taskService;
            ScanViewModel = new ScanViewModel(_dialogService, TaskServiceFactory.GetTaskServiceLike(_taskService), new List<PrSm>(), Messenger.Default);
            DataSets = new ObservableCollection<DataSetViewModel>();
            CreateSequenceViewModel = new CreateSequenceViewModel(DataSets, _dialogService, Messenger.Default);
            LoadingScreenViewModel = new LoadingScreenViewModel(TaskServiceFactory.GetTaskServiceLike(_taskService));

            OpenDataSetCommand = new RelayCommand(OpenDataSet);
            OpenRawFileCommand = new RelayCommand(OpenRawFile);
            OpenTsvFileCommand = new RelayCommand(OpenIdFile);
            OpenFeatureFileCommand = new RelayCommand(OpenFeatureFile);
            OpenFromDmsCommand = new RelayCommand(() => OpenFromDms(), () => ShowOpenFromDms);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            OpenAboutBoxCommand = new RelayCommand(OpenAboutBox);

            ShowSplash = true;
            FileOpen = false;
        }

        /// <summary>
        /// Constructor for creating MainWindowViewModel with existing IDs
        /// </summary>
        /// <param name="dialogService">Service for MVVM-friendly dialogs</param>
        /// <param name="taskService">Service for task queueing</param>
        /// <param name="idTree">Existing IDs</param>
        public MainWindowViewModel(IMainDialogService dialogService, ITaskService taskService, IdentificationTree idTree) : this(dialogService, taskService)
        {
            ScanViewModel.AddIds(idTree.AllPrSms);
        }

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
            set
            {
                _showSplash = value;
                RaisePropertyChanged();
            }
        }

        private bool _fileOpen;
        /// <summary>
        /// Tracks whether or not a file is currently open
        /// </summary>
        public bool FileOpen
        {
            get { return _fileOpen; }
            set
            {
                _fileOpen = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Open raw file and/or id file, feature file
        /// </summary>
        public void OpenDataSet()
        {
            var openDataVm = new OpenDataWindowViewModel(_dialogService);
            if (_dialogService.OpenDataWindow(openDataVm))
            {
                ShowSplash = false;
                _taskService.Enqueue(() =>
                {
                    DataSetViewModel dsVm;
                    try
                    {
                        dsVm = ReadRawFile(openDataVm.RawFilePath);
                    }
                    catch (Exception)
                    {
                        _dialogService.ExceptionAlert(new Exception("Cannot read raw file."));
                        if (DataSets.Count > 0) GuiInvoker.Invoke(() => DataSets.RemoveAt(DataSets.Count-1));
                        return;
                    }
                    if (dsVm != null)
                    {
                        if (!String.IsNullOrEmpty(openDataVm.IdFilePath))
                            try
                            {
                                ReadIdFile(openDataVm.IdFilePath, dsVm.RawFilePath, dsVm);
                            }
                            catch (Exception)
                            {
                                _dialogService.ExceptionAlert(new Exception("Cannot read ID file."));
                            }
                        if (!String.IsNullOrEmpty(openDataVm.FeatureFilePath))
                            try
                            {
                                dsVm.OpenFeatureFile(openDataVm.FeatureFilePath);
                            }
                            catch (Exception)
                            {
                                _dialogService.ExceptionAlert(new Exception("Cannot read feature file."));
                            }
                    }
                }, true);
            }
        }

        /// <summary>
        /// Prompt user for raw files and call ReadRawFile() to open file.
        /// </summary>
        public void OpenRawFile()
        {
            var rawFileNames = _dialogService.MultiSelectOpenFile(".raw", @"Raw Files (*.raw)|*.raw|MzMl Files (*.mzMl)|*.mzMl");
            if (rawFileNames == null) return;
            ShowSplash = false;
            foreach (var rawFileName in rawFileNames)
            {
                var name = rawFileName;
                string fileName = rawFileName;
                _taskService.Enqueue(() =>
                {
                    try
                    {
                        ReadRawFile(name);
                    }
                    catch (Exception)
                    {
                        _dialogService.ExceptionAlert(new Exception(String.Format("Cannot read {0}.", fileName)));
                        if (DataSets.Count > 0) GuiInvoker.Invoke(() => DataSets.RemoveAt(DataSets.Count - 1));
                    }
                    if (DataSets.Count > 0) ScanViewModel.HideUnidentifiedScans = false;
                }, true);
            }
        }

        /// <summary>
        /// Open identification file. Checks to ensure that there is a raw file open
        /// corresponding to this ID file.
        /// </summary>
        public void OpenIdFile()
        {
            const string formatStr = @"TSV Files (*.txt; *.tsv)|*.txt;*.tsv|MzId Files (*.mzId[.gz])|*.mzId;*.mzId.gz|MTDB Files (*.mtdb)|*.mtdb";
            var tsvFileName = _dialogService.OpenFile(".txt", formatStr);
            if (tsvFileName == "") return;
            var fileName = Path.GetFileNameWithoutExtension(tsvFileName);
            var ext = Path.GetExtension(tsvFileName);
            string path = ext != null ? tsvFileName.Remove(tsvFileName.IndexOf(ext, StringComparison.Ordinal)) : tsvFileName;
            string rawFileName = "";
            DataSetViewModel dsVm = null;
            foreach (var ds in DataSets)      // Raw file already open?
            {
                if (ds.RawFileName == fileName)
                {   // xicVm with correct raw file name was found. Raw file is already open
                    dsVm = ds;
                    rawFileName = dsVm.RawFileName;
                }
            }
            if (dsVm == null)  // Raw file not already open
            {
                var directoryName = Path.GetDirectoryName(tsvFileName);
                if (directoryName != null)
                {
                    var directory = Directory.GetFiles(directoryName);
                    foreach (var file in directory) // Raw file in same directory as tsv file?
                        if (file == path + ".raw") rawFileName = path + ".raw";
                    if (rawFileName == "")  // Raw file was not in the same directory.
                    {   // prompt user for raw file path
                        /*_dialogService.MessageBox("Please select raw file.");
                        rawFileName = _dialogService.OpenFile(".raw", @"Raw Files (*.raw)|*.raw");*/
                        var selectDataVm = new SelectDataSetViewModel(_dialogService, DataSets);
                        if (_dialogService.OpenSelectDataWindow(selectDataVm))
                        {
                            // manually find raw file
                            if (!String.IsNullOrEmpty(selectDataVm.RawFilePath))
                            {
                                rawFileName = selectDataVm.RawFilePath;
                            }
                            else
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
                _taskService.Enqueue(() =>
                {
                    // Name of raw file was found
                    if (dsVm == null) // raw file isn't open yet
                        try
                        {
                            dsVm = ReadRawFile(rawFileName);
                        }
                        catch (Exception)
                        {
                            _dialogService.ExceptionAlert(new Exception("Cannot read raw file."));
                            if (DataSets.Count > 0) GuiInvoker.Invoke(() => DataSets.RemoveAt(DataSets.Count - 1));
                        }
                    ReadIdFile(tsvFileName, rawFileName, dsVm); // finally read the TSV file
                }, true);
            }
            else _dialogService.MessageBox("Cannot open ID file.");
        }

        /// <summary>
        /// Open feature file. Checks to ensure that there is a raw file open
        /// corresponding to this ID file.
        /// </summary>
        public void OpenFeatureFile()
        {
            const string formatStr = @"TSV Files (*.txt; *tsv)|*.txt;*.tsv";
            var tsvFileName = _dialogService.OpenFile(".txt", formatStr);
            if (tsvFileName == "") return;
            var fileName = Path.GetFileNameWithoutExtension(tsvFileName);
            var ext = Path.GetExtension(tsvFileName);
            string path = ext != null ? tsvFileName.Remove(tsvFileName.IndexOf(ext, StringComparison.Ordinal)) : tsvFileName;
            string rawFileName = "";
            DataSetViewModel dsVm = null;
            foreach (var ds in DataSets)      // Raw file already open?
            {
                if (ds.RawFileName == fileName)
                {   // xicVm with correct raw file name was found. Raw file is already open
                    dsVm = ds;
                    rawFileName = dsVm.RawFileName;
                }
            }
            if (dsVm == null)  // Raw file not already open
            {
                var directoryName = Path.GetDirectoryName(tsvFileName);
                if (directoryName != null)
                {
                    var directory = Directory.GetFiles(directoryName);
                    foreach (var file in directory) // Raw file in same directory as tsv file?
                    {
                        var lFile = path.ToLower();
                        if (file == path + ".raw") rawFileName = path + ".raw";
                        else if (lFile == lFile + ".mzml") rawFileName = lFile + ".mzml";
                    }
                    if (rawFileName == "")  // Raw file was not in the same directory.
                    {   // prompt user for raw file path
                        /*_dialogService.MessageBox("Please select raw file.");
                        rawFileName = _dialogService.OpenFile(".raw", @"Raw Files (*.raw)|*.raw");*/
                        var selectDataVm = new SelectDataSetViewModel(_dialogService, DataSets);
                        if (_dialogService.OpenSelectDataWindow(selectDataVm))
                        {
                            // manually find raw file
                            if (!String.IsNullOrEmpty(selectDataVm.RawFilePath))
                            {
                                rawFileName = selectDataVm.RawFilePath;
                            }
                            else
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
                _taskService.Enqueue(() =>
                {
                    // Name of raw file was found
                    if (dsVm == null) // raw file isn't open yet
                        try
                        {
                            dsVm = ReadRawFile(rawFileName);
                        }
                        catch (Exception)
                        {
                            _dialogService.ExceptionAlert(new Exception("Cannot read raw file."));
                            if (DataSets.Count > 0) GuiInvoker.Invoke(() => DataSets.RemoveAt(DataSets.Count - 1));
                        }
                    if (dsVm != null)
                        try
                        {
                            dsVm.OpenFeatureFile(tsvFileName);
                        }
                        catch (Exception)
                        {
                            _dialogService.ExceptionAlert(new Exception("Cannot read feature file."));
                        }
                }, true);
            }
            else _dialogService.MessageBox("Cannot open feature file.");
        }

        /// <summary>
        /// Attempt to open Ids from identification file and associate raw file with them.
        /// </summary>
        /// <param name="idFileName">Name of id file.</param>
        /// <param name="rawFileName">Name of raw file to associate with id file.</param>
        /// <param name="dsVm">Data Set View model to associate with id file.</param>
        public void ReadIdFile(string idFileName, string rawFileName, DataSetViewModel dsVm)
        {
            LoadingScreenViewModel.IsLoading = true;
            var ids = new IdentificationTree();
            bool attemptToReadFile = true;
            var modIgnoreList = new List<string>();
            do
            {
                try
                {
                    var reader = IdFileReaderFactory.CreateReader(idFileName);
                    ids = reader.Read(modIgnoreList);
                    ids.SetLcmsRun(dsVm.Lcms, dsVm.RawFileName);
                    attemptToReadFile = false;
                }
                catch (IOException e)
                {
                    _dialogService.ExceptionAlert(e);
                    FileOpen = false;
                    LoadingScreenViewModel.IsLoading = false;
                    return;
                }
                catch (InvalidModificationNameException e)
                {
                    var result =
                        _dialogService.ConfirmationBox(
                            String.Format(
                                "{0}\nWould you like to add this modification?\nIf not, all sequences containing this modification will be ignored.",
                                e.Message),
                            "Unknown Modification");
                    if (result)
                    {
                        var customModVm = new CustomModificationViewModel(e.ModificationName, true);
                        GuiInvoker.Invoke(() => _dialogService.OpenCustomModification(customModVm));
                        if (customModVm.Status)
                        {
                            Modification.RegisterAndGetModification(customModVm.ModificationName,
                                customModVm.Composition);
                        }
                        else
                        {
                            modIgnoreList.Add(e.ModificationName);
                        }
                    }
                    else
                    {
                        modIgnoreList.Add(e.ModificationName);
                    }
                }
                catch (Exception)
                {
                    _dialogService.ExceptionAlert(new Exception("Cannot read ID file."));
                    FileOpen = false;
                    LoadingScreenViewModel.IsLoading = false;
                    return;
                }
            } while (attemptToReadFile);
            var data = ScanViewModel.Data;
            dsVm.AddIds(ids);
            data.AddRange(ids.AllPrSms);
            ScanViewModel.AddIds(data);
            ScanViewModel.HideUnidentifiedScans = true;
            FileOpen = true;
            LoadingScreenViewModel.IsLoading = false;
        }

        /// <summary>
        /// Open raw file
        /// </summary>
        /// <param name="rawFilePath">Path to raw file to open</param>
        public DataSetViewModel ReadRawFile(string rawFilePath)
        {
            var dsVm = new DataSetViewModel(_dialogService, TaskServiceFactory.GetTaskServiceLike(_taskService)); // create data set view model
            GuiInvoker.Invoke(() => DataSets.Add(dsVm)); // add data set view model. Can only add to ObservableCollection in thread that created it (gui thread)
            dsVm.RawFilePath = rawFilePath;
            GuiInvoker.Invoke(() => { CreateSequenceViewModel.SelectedDataSetViewModel = DataSets[0]; });
            FileOpen = true;
            return dsVm;
        }

        /// <summary>
        /// Open data set (raw file and ID files) from PNNL DMS system
        /// </summary>
        public Task OpenFromDms()
        {
            Task task = null;
            var data = _dialogService.OpenDmsLookup(new DmsLookupViewModel(_dialogService));
            if (data == null) return null;
            var dataSetDirName = data.Item1;
            var jobDirName = data.Item2;
            string idFilePath = "";
            List<string> rawFileNames = null;
            if (!String.IsNullOrEmpty(dataSetDirName))      // did the user actually choose a dataset?
            {
                var dataSetDir = Directory.GetFiles(dataSetDirName);
                rawFileNames = (from filePath in dataSetDir
                                let ext = Path.GetExtension(filePath)
                                where (ext == ".raw" || ext == ".mzml")
                                select filePath).ToList();
            }
            if (!String.IsNullOrEmpty(jobDirName))      // did the user actually choose a job?
            {
                var jobDir = Directory.GetFiles(jobDirName);
                idFilePath = (from idFp in jobDir
                              let ext = Path.GetExtension(idFp)
                              where ext == ".mzid" || ext == ".gz"
                                select idFp).FirstOrDefault();
            }
            if (rawFileNames == null || rawFileNames.Count == 0)
            {   // no data set chosen or no raw files found for data set
                _dialogService.MessageBox("No raw files found for that data set.");
                LoadingScreenViewModel.IsLoading = false;
                return null;
            }
            ShowSplash = false;
            foreach (var rawFilePath in rawFileNames)
            {
                var raw = rawFilePath;
                var filePath = idFilePath;
                task = Task.Factory.StartNew(() =>
                {
                    var xicVm = ReadRawFile(raw);
                    if (!String.IsNullOrEmpty(filePath)) ReadIdFile(filePath, xicVm.RawFileName, xicVm);
                });
            }
            return task;
        }

        /// <summary>
        /// Open settings window
        /// </summary>
        public void OpenSettings()
        {
            var settingsViewModel = new SettingsViewModel(_dialogService);
            _dialogService.OpenSettings(settingsViewModel);
            if (settingsViewModel.Status)
            {
                Messenger.Default.Send(new SettingsChangedNotification(this, "SettingsChanged"));
            }
        }

        /// <summary>
        /// Open about box
        /// </summary>
        private void OpenAboutBox()
        {
            _dialogService.OpenAboutBox();
        }

        /// <summary>
        /// Event handler for XicCloseRequest in XicViewModel
        /// Closes the raw file and cleans up IDs pointing to that raw file
        /// </summary>
        /// <param name="message">Message containing sender info</param>
        private void DataSetCloseRequest(DataSetCloseRequest message)
        {
            var dsVm = message.Sender as DataSetViewModel;
            if (dsVm != null)
            {
                var rawFileName = dsVm.RawFileName;
                ScanViewModel.RemovePrSmsFromRawFile(rawFileName);
                DataSets.Remove(dsVm);
            }
        }

        private readonly IMainDialogService _dialogService;
        private readonly ITaskService _taskService;
    }

    public class SettingsChangedNotification : NotificationMessage
    {
        public SettingsChangedNotification(object sender, string notification) : base(sender, notification){}
    }
}

