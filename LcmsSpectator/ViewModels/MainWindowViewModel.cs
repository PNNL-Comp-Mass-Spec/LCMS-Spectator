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
using LcmsSpectatorModels.Config;
using LcmsSpectatorModels.Models;
using LcmsSpectatorModels.Readers;

namespace LcmsSpectator.ViewModels
{
    public class MainWindowViewModel: ViewModelBase
    {
        public IdentificationTree Ids { get; private set; }
        public IdentificationTree FilteredIds { get; private set; }
        public List<IonType> IonTypes { get; set; }

        public RelayCommand OpenRawFileCommand { get; private set; }
        public RelayCommand OpenTsvFileCommand { get; private set; }
        public RelayCommand OpenFromDmsCommand { get; private set; }
        public RelayCommand OpenSettingsCommand { get; private set; }
        public RelayCommand OpenAboutBoxCommand { get; private set; }

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
            IcParameters.Instance.IcParametersUpdated += SettingsChanged;
            Ids = new IdentificationTree();
            FilteredIds = new IdentificationTree();
            ProteinIds = Ids.ProteinIds.ToList();
            PrSms = new List<PrSm>();
            SelectedPrSmViewModel = SelectedPrSmViewModel.Instance;
            Ms2SpectrumViewModel = new SpectrumViewModel(_dialogService, TaskServiceFactory.GetTaskServiceLike(taskService));
            IonTypeSelectorViewModel = new IonTypeSelectorViewModel(_dialogService);
            XicViewModels = new ObservableCollection<XicViewModel>();
            CreateSequenceViewModel = new CreateSequenceViewModel(XicViewModels, _dialogService);

            OpenRawFileCommand = new RelayCommand(() => OpenRawFile());
            OpenTsvFileCommand = new RelayCommand(() => OpenIdFile());
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
            Ids = idTree;
            FilterIds();
        }

        /// <summary>
        /// Object selected in Treeview. Uses weak typing because each level TreeView is a different data type.
        /// </summary>
        public object TreeViewSelectedItem
        {
            get { return _treeViewSelectedItem; }
            set
            {
                if (value != null)
                {
                    _treeViewSelectedItem = value;
                    if (_treeViewSelectedItem is PrSm)
                    {
                        var selectedPrSm = _treeViewSelectedItem as PrSm;
                        SelectedPrSm = selectedPrSm;
                    }
                    else
                    {
                        var selected = (IIdData) _treeViewSelectedItem;
                        if (selected == null) return;
                        var highest = selected.GetHighestScoringPrSm();
                        SelectedPrSm = highest;
                    }
                    RaisePropertyChanged();
                }
            }
        }

        public PrSm SelectedPrSm
        {
            get { return _selectedPrSm; }
            set
            {
                _selectedPrSm = value;
                SelectedPrSmViewModel.Instance.PrSm = _selectedPrSm;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Toggle whether or not the Scan View shows Scans that do not have a sequence ID
        /// </summary>
        public bool ShowUnidentifiedScans
        {
            get { return _showUnidentifiedScans; }
            set
            {
                _showUnidentifiedScans = value;
                _idTreeMutex.WaitOne();
                PrSms = _showUnidentifiedScans ? FilteredIds.AllPrSms : FilteredIds.IdentifiedPrSms;
                _idTreeMutex.ReleaseMutex();
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// List of all PrSms (for display in Scan View)
        /// </summary>
        public List<PrSm> PrSms
        {
            get { return _prSms; }
            private set
            {
                _prSms = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// List of all ProteinIds (for display in Protein Tree)
        /// </summary>
        public List<ProteinId> ProteinIds
        {
            get { return _proteinIds; }
            private set
            {
                _proteinIds = value;
                RaisePropertyChanged();
            }
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
        public Task OpenRawFile()
        {
            Task task = null;
            var rawFileNames = _dialogService.MultiSelectOpenFile(".raw", @"Raw Files (*.raw)|*.raw");
            if (rawFileNames == null) return null;
            foreach (var rawFileName in rawFileNames)
            {
                var name = rawFileName;
                task = Task.Factory.StartNew(() =>
                {
                    ReadRawFile(name);
                    if (XicViewModels.Count > 0) ShowUnidentifiedScans = true;
                });
            }
            return task;
        }

        /// <summary>
        /// Open identification file. Checks to ensure that there is a raw file open
        /// corresponding to this ID file.
        /// </summary>
        public Task OpenIdFile()
        {
            Task task = null;
            const string formatStr = @"TSV Files (*.txt; *tsv)|*.txt;*.tsv|MzId Files (*.mzId)|*.mzId|MzId GZip Files (*.mzId.gz)|*.mzId.gz";
            var tsvFileName = _dialogService.OpenFile(".txt", formatStr);
            if (tsvFileName == "") return null;
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
                task = Task.Factory.StartNew(() =>
                {
                    // Name of raw file was found
                    if (xicVm == null) xicVm = ReadRawFile(rawFileName); // raw file isn't open yet
                    ReadIdFile(tsvFileName, rawFileName, xicVm); // finally read the TSV file
                });
            }
            else _dialogService.MessageBox("Cannot open ID file.");
            return task;
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
            Ids.Add(ids);
            Ids.Tool = ids.Tool; // assign new tool
            FilterIds();    // filter Ids by qvalue threshold
            ShowUnidentifiedScans = false;
            if (Ids.Proteins.Count > 0) SelectedPrSmViewModel.Instance.PrSm = Ids.GetHighestScoringPrSm();
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
                Ids.Add(prsm);
                _idTreeMutex.ReleaseMutex();
            }
            _idTreeMutex.WaitOne();
            FilterIds();
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
        /// Event handler for IcParametersChanged in IcParameters
        /// </summary>
        private void SettingsChanged()
        {
            if (!FilteredIds.QValueFilter.Equals(IcParameters.Instance.QValueThreshold)) Task.Factory.StartNew(FilterIds);
        }

        /// <summary>
        /// Filter Ids by QValue threshold set in settings
        /// </summary>
        private void FilterIds()
        {
            var qValue = IcParameters.Instance.QValueThreshold;
            FilteredIds = Ids.GetTreeFilteredByQValue(qValue);
            ProteinIds = FilteredIds.ProteinIds.ToList();
            var prsms = _showUnidentifiedScans ? FilteredIds.AllPrSms : FilteredIds.IdentifiedPrSms;
            prsms.Sort();
            PrSms = prsms;
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
                Ids.RemovePrSmsFromRawFile(rawFileName);
                FilterIds();
                XicViewModels.Remove(xicVm);
                if (SelectedPrSmViewModel.Instance.RawFileName == rawFileName)
                {
                    if (XicViewModels.Count > 0) CreateSequenceViewModel.SelectedXicViewModel = XicViewModels[0];
                    if (PrSms.Count > 0) SelectedPrSmViewModel.Instance.PrSm = Ids.GetHighestScoringPrSm();
                    else
                    {
                        SelectedPrSmViewModel.Instance.Clear();
                    }
                }
            }
        }

        private readonly IMainDialogService _dialogService;
        private readonly Mutex _idTreeMutex;

        private bool _isLoading;
        private bool _fileOpen;

        private object _treeViewSelectedItem;
        private bool _showUnidentifiedScans;
        private List<PrSm> _prSms;
        private List<ProteinId> _proteinIds;
        private PrSm _selectedPrSm;
        private ITaskService _taskService;
    }

    public class SettingsChangedNotification : NotificationMessage
    {
        public SettingsChangedNotification(object sender, string notification) : base(sender, notification)
        {
        }
    }
}

