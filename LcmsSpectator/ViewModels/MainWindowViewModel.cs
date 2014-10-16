using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
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
        public RelayCommand OpenRawFileCommand { get; private set; }
        public RelayCommand OpenTsvFileCommand { get; private set; }
        public RelayCommand OpenFromDmsCommand { get; private set; }
        public RelayCommand OpenSettingsCommand { get; private set; }
        public RelayCommand OpenAboutBoxCommand { get; private set; }

        // Child view models
        public ScanViewModel ScanViewModel { get; private set; }
        public CreateSequenceViewModel CreateSequenceViewModel { get; private set; }
        public IonTypeSelectorViewModel IonTypeSelectorViewModel { get; private set; }
        public SpectrumViewModel Ms2SpectrumViewModel { get; private set; }
        public ObservableCollection<XicViewModel> XicViewModels { get; private set; }
        public SelectedPrSmViewModel SelectedPrSmViewModel { get; private set; }

        /// <summary>
        /// Constructor for creating a new, empty MainWindowViewModel
        /// </summary>
        /// <param name="dialogService">Service for MVVM-friendly dialogs</param>
        /// <param name="taskService">Service for task queueing</param>
        public MainWindowViewModel(IMainDialogService dialogService, ITaskService taskService)
        {
            // register messenger events
            Messenger.Default.Register<XicViewModel.XicCloseRequest>(this, XicCloseRequest);

            _dialogService = dialogService;
            _taskService = taskService;
            SelectedPrSmViewModel = SelectedPrSmViewModel.Instance;
            Ms2SpectrumViewModel = new SpectrumViewModel(_dialogService, TaskServiceFactory.GetTaskServiceLike(taskService));
            ScanViewModel = new ScanViewModel(_dialogService, TaskServiceFactory.GetTaskServiceLike(_taskService), new List<PrSm>());
            IonTypeSelectorViewModel = new IonTypeSelectorViewModel(_dialogService);
            XicViewModels = new ObservableCollection<XicViewModel>();
            CreateSequenceViewModel = new CreateSequenceViewModel(XicViewModels, _dialogService);

            OpenRawFileCommand = new RelayCommand(OpenRawFile);
            OpenTsvFileCommand = new RelayCommand(OpenIdFile);
            OpenFromDmsCommand = new RelayCommand(() => OpenFromDms(), () => ShowOpenFromDms);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            OpenAboutBoxCommand = new RelayCommand(OpenAboutBox);

            IsLoading = false;
            FileOpen = false;

            _idTreeMutex = new Mutex();
        }

        /// <summary>
        /// Constructor for creating MainWindowViewModel with existing IDs
        /// </summary>
        /// <param name="dialogService">Service for MVVM-friendly dialogs</param>
        /// <param name="taskService">Service for task queueing</param>
        /// <param name="idTree">Existing IDs</param>
        public MainWindowViewModel(IMainDialogService dialogService, ITaskService taskService, IdentificationTree idTree) : this(dialogService, taskService)
        {
            ScanViewModel.Data = idTree.AllPrSms;
        }

        /// <summary>
        /// Determine whether or not "Open From DMS" should be shown on the menu based on whether
        /// or not the user is on the PNNL network or not.
        /// </summary>
        public bool ShowOpenFromDms
        {
            get { return System.Net.Dns.GetHostEntry("").HostName.Contains("pnl.gov"); }
        }

        /// <summary>
        /// A file is currently being loaded
        /// </summary>
        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                RaisePropertyChanged();
            }
        }

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
        /// Prompt user for raw files and call ReadRawFile() to open file.
        /// </summary>
        public void OpenRawFile()
        {
            var rawFileNames = _dialogService.MultiSelectOpenFile(".raw", @"Raw Files (*.raw)|*.raw");
            if (rawFileNames == null) return;
            foreach (var rawFileName in rawFileNames)
            {
                var name = rawFileName;
                _taskService.Enqueue(() =>
                {
                    ReadRawFile(name);
                    if (XicViewModels.Count > 0) ScanViewModel.HideUnidentifiedScans = false;
                }, true);
            }
        }

        /// <summary>
        /// Open identification file. Checks to ensure that there is a raw file open
        /// corresponding to this ID file.
        /// </summary>
        public void OpenIdFile()
        {
            const string formatStr = @"TSV Files (*.txt; *tsv)|*.txt;*.tsv|MzId Files (*.mzId[.gz])|*.mzId;*.mzId.gz|MTDB Files (*.mtdb)|*.mtdb";
            var tsvFileName = _dialogService.OpenFile(".txt", formatStr);
            if (tsvFileName == "") return;
            var fileName = Path.GetFileNameWithoutExtension(tsvFileName);
            var ext = Path.GetExtension(tsvFileName);
            string path = ext != null ? tsvFileName.Remove(tsvFileName.IndexOf(ext, StringComparison.Ordinal)) : tsvFileName;
            string rawFileName = "";
            XicViewModel xicVm = null;
            foreach (var xic in XicViewModels)      // Raw file already open?
            {
                if (xic.RawFileName == fileName)
                {   // xicVm with correct raw file name was found. Raw file is already open
                    xicVm = xic;
                    rawFileName = xicVm.RawFileName;
                }
            }
            if (xicVm == null)  // Raw file not already open
            {
                var directoryName = Path.GetDirectoryName(tsvFileName);
                if (directoryName != null)
                {
                    var directory = Directory.GetFiles(directoryName);
                    foreach (var file in directory) // Raw file in same directory as tsv file?
                        if (file == path + ".raw") rawFileName = path + ".raw";
                    if (rawFileName == "")  // Raw file was not in the same directory.
                    {   // prompt user for raw file path
                        _dialogService.MessageBox("Please select raw file.");
                        rawFileName = _dialogService.OpenFile(".raw", @"Raw Files (*.raw)|*.raw");
                            // manually find raw file
                    }
                }
            }
            if (!String.IsNullOrEmpty(rawFileName))
            {
                _taskService.Enqueue(() =>
                {
                    // Name of raw file was found
                    if (xicVm == null) xicVm = ReadRawFile(rawFileName); // raw file isn't open yet
                    ReadIdFile(tsvFileName, rawFileName, xicVm); // finally read the TSV file
                }, true);
            }
            else _dialogService.MessageBox("Cannot open ID file.");
        }


        /// <summary>
        /// Attempt to open Ids from identification file and associate raw file with them.
        /// </summary>
        /// <param name="idFileName">Name of id file.</param>
        /// <param name="rawFileName">Name of raw file to associate with id file.</param>
        /// <param name="xicVm">Xic View model to associate with id file.</param>
        public void ReadIdFile(string idFileName, string rawFileName, XicViewModel xicVm)
        {
            IsLoading = true;
            IdentificationTree ids;
			try
			{
                var reader = IdFileReaderFactory.CreateReader(idFileName);
                ids = reader.Read();
                ids.SetLcmsRun(xicVm.Lcms, xicVm.RawFileName);
            }
			catch (IOException e)
			{
				_dialogService.ExceptionAlert(e);
				FileOpen = false;
				IsLoading = false;
				return;
			}
            var data = ScanViewModel.Data;
            data.AddRange(ids.AllPrSms);
            ScanViewModel.Data = data;
            ScanViewModel.HideUnidentifiedScans = true;
            if (ids.Proteins.Count > 0) SelectedPrSmViewModel.Instance.PrSm = ids.GetHighestScoringPrSm();
            FileOpen = true;
            IsLoading = false;
        }

        /// <summary>
        /// Open raw file
        /// </summary>
        /// <param name="rawFilePath">Path to raw file to open</param>
        public XicViewModel ReadRawFile(string rawFilePath)
        {
            var xicVm = new XicViewModel(_dialogService, TaskServiceFactory.GetTaskServiceLike(_taskService)); // create xic view model
            GuiInvoker.Invoke(() => XicViewModels.Add(xicVm)); // add xic view model to gui
            xicVm.RawFilePath = rawFilePath;
            var lcms = xicVm.Lcms;
            var scans = lcms.GetScanNumbers(2);
            var prsmScans = new List<PrSm>();
            foreach (var scan in scans)
            {
                var prsm = new PrSm
                {
                    Scan = scan,
                    RawFileName = xicVm.RawFileName,
                    Lcms = lcms,
                    QValue = 1.0,
                    Score = Double.NaN,
                    Sequence = new Sequence(new List<AminoAcid>()),
                    SequenceText = "",
                    ProteinName = "",
                    ProteinDesc = "",
                    Charge = 0
                };
                _idTreeMutex.WaitOne();
                prsmScans.Add(prsm);
                _idTreeMutex.ReleaseMutex();
            }
            _idTreeMutex.WaitOne();
            ScanViewModel.Data.AddRange(prsmScans);
            ScanViewModel.Data = ScanViewModel.Data;
            _idTreeMutex.ReleaseMutex();
            GuiInvoker.Invoke(() => { CreateSequenceViewModel.SelectedXicViewModel = XicViewModels[0]; });
            FileOpen = true;
            return xicVm;
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
                                where ext == ".raw"
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
                IsLoading = false;
                return null;
            }
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
        private void XicCloseRequest(XicViewModel.XicCloseRequest message)
        {
            var xicVm = message.Sender as XicViewModel;
            if (xicVm != null)
            {
                var rawFileName = xicVm.RawFileName;
                ScanViewModel.RemovePrSmsFromRawFile(rawFileName);
                XicViewModels.Remove(xicVm);
                if (SelectedPrSmViewModel.Instance.RawFileName == rawFileName)
                {
                    if (XicViewModels.Count > 0) CreateSequenceViewModel.SelectedXicViewModel = XicViewModels[0];
                    //if (ScanViewModel.Data.Count > 0) SelectedPrSmViewModel.Instance.PrSm = Ids.GetHighestScoringPrSm();
                    else
                    {
                        SelectedPrSmViewModel.Instance.Clear();
                    }
                }
            }
        }

        private readonly IMainDialogService _dialogService;
        private readonly ITaskService _taskService;
        private readonly Mutex _idTreeMutex;

        private bool _isLoading;
        private bool _fileOpen;
    }

    public class SettingsChangedNotification : NotificationMessage
    {
        public SettingsChangedNotification(object sender, string notification) : base(sender, notification){}
    }
}

