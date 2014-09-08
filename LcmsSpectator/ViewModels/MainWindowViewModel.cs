using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.DialogServices;
using LcmsSpectator.PlotModels;
using LcmsSpectator.Utils;
using LcmsSpectatorModels.Config;
using LcmsSpectatorModels.Models;
using LcmsSpectatorModels.Readers;
using LcmsSpectatorModels.Utils;

namespace LcmsSpectator.ViewModels
{
    public class MainWindowViewModel: ViewModelBase
    {
        public IdentificationTree Ids { get; private set; }
        public IdentificationTree FilteredIds { get; private set; }
        public List<ProteinId> ProteinIds { get; private set; }
        public List<PrSm> PrSms { get; private set; } 
        public List<IonType> IonTypes { get; set; }

        public List<LabeledIon> FragmentLabels { get; set; }
        public List<LabeledIon> LightFragmentLabels { get; set; } 
        public List<LabeledIon> HeavyFragmentLabels { get; set; } 
        public List<LabeledIon> PrecursorLabels { get; set; }
        public List<LabeledIon> LightPrecursorLabels { get; set; } 
        public List<LabeledIon> HeavyPrecursorLabels { get; set; } 

        public DelegateCommand OpenRawFileCommand { get; private set; }
        public DelegateCommand OpenTsvFileCommand { get; private set; }
        public DelegateCommand OpenFromDmsCommand { get; private set; }
        public DelegateCommand OpenSettingsCommand { get; private set; }
        public DelegateCommand OpenAboutBoxCommand { get; private set; }

        public CreateSequenceViewModel CreateSequenceViewModel { get; private set; }
        public IonTypeSelectorViewModel IonTypeSelectorViewModel { get; private set; }
        public SpectrumViewModel Ms2SpectrumViewModel { get; private set; }
        public ObservableCollection<XicViewModel> XicViewModels { get; private set; }

        public MainWindowViewModel(IMainDialogService dialogService)
        {
            _xicChanged = false;
            _dialogService = dialogService;
            IcParameters.Instance.IcParametersUpdated += SettingsChanged;
            Ids = new IdentificationTree();
            FilteredIds = new IdentificationTree();
            ProteinIds = Ids.ProteinIds.ToList();
            PrSms = new List<PrSm>();
            _colors = new ColorDictionary(2);
            IonTypeSelectorViewModel = new IonTypeSelectorViewModel(_dialogService);
            IonTypeSelectorViewModel.IonTypesUpdated += SetIonTypes;
            Ms2SpectrumViewModel = new SpectrumViewModel(_dialogService, _colors);
            XicViewModels = new ObservableCollection<XicViewModel>();
            CreateSequenceViewModel = new CreateSequenceViewModel(XicViewModels, _dialogService);
            CreateSequenceViewModel.SequenceCreated += UpdatePrSm;

            _spectrumChanged = false;

            OpenRawFileCommand = new DelegateCommand(OpenRawFile);
            OpenTsvFileCommand = new DelegateCommand(OpenIdFile);
            OpenFromDmsCommand = new DelegateCommand(OpenFromDms, ShowOpenFromDms);
            OpenSettingsCommand = new DelegateCommand(OpenSettings);
            OpenAboutBoxCommand = new DelegateCommand(OpenAboutBox);

            FragmentLabels = new List<LabeledIon>();
            SelectedFragmentLabels = new List<LabeledIon>();
            PrecursorLabels = new List<LabeledIon>();
            SelectedPrecursorLabels = new List<LabeledIon>();

            IsLoading = false;
            FileOpen = false;

            SelectedPrSm = null;

            _idTreeMutex = new Mutex();
        }

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
                    OnPropertyChanged("TreeViewSelectedItem");
                }
            }
        }

        public bool ShowUnidentifiedScans
        {
            get { return _showUnidentifiedScans; }
            set
            {
                _showUnidentifiedScans = value;
                _idTreeMutex.WaitOne();
                PrSms = _showUnidentifiedScans ? FilteredIds.AllPrSms : FilteredIds.IdentifiedPrSms;
                _idTreeMutex.ReleaseMutex();
                OnPropertyChanged("PrSms");
                OnPropertyChanged("ShowUnidentifiedScans");
            }
        }


        public bool ShowOpenFromDms
        {
            get { return System.Net.Dns.GetHostEntry("").HostName.Contains("pnl.gov"); }
        }

        /// <summary>
        /// Currently selected sequence and spectrum
        /// </summary>
        public PrSm SelectedPrSm
        {
            get { return _selectedPrSm; }
            set
            {
                if (value == null) return;
                CreateSequenceViewModel.SelectedScan = value.Scan;
                _spectrumChanged = true;
                if (_selectedPrSm == null ||
                    _selectedPrSm.SequenceText != value.SequenceText ||
                    _selectedPrSm.Charge != value.Charge || _xicChanged)
                {
                    _selectedPrSm = value;
                    var absoluteMaxCharge = Math.Min(Math.Max(value.Charge - 1, 2), 15);
                    _colors.BuildColorDictionary(absoluteMaxCharge);
                    IonTypeSelectorViewModel.AbsoluteMaxCharge = absoluteMaxCharge;
                    IonTypeSelectorViewModel.MinCharge = 1;
                    foreach (var xicVm in XicViewModels) xicVm.ZoomToRt(value.RetentionTime);
                    SetFragmentLabels();
                    SetPrecursorLabels();
                    _xicChanged = false;
                }
                _selectedPrSm = value;
                UpdateSpectrum();
                foreach (var xicVm in XicViewModels)
                    xicVm.HighlightRetentionTime(SelectedPrSm.RetentionTime, xicVm.RawFileName == SelectedPrSm.RawFileName, SelectedPrSm.Heavy);
                OnPropertyChanged("SelectedPrSm");
            }
        }

        public IList SelectedFragmentLabels
        {
            get { return _selectedFragmentLabels; }
            set
            {
                _selectedFragmentLabels = value;
                if (_fragmentXicChanged)
                {
                    var fragmentLabelList = _selectedFragmentLabels.Cast<LabeledIon>().ToList();
                    foreach (var xicVm in XicViewModels)
                    {
                        xicVm.SelectedFragments = fragmentLabelList;
                        xicVm.SelectedLightFragments = IonUtils.ReduceLabels(LightFragmentLabels, fragmentLabelList);
                        xicVm.SelectedHeavyFragments = IonUtils.ReduceLabels(HeavyFragmentLabels, fragmentLabelList);
                    }
                }
                OnPropertyChanged("SelectedFragmentLabels");
            }
        }

        public IList SelectedPrecursorLabels
        {
            get { return _selectedPrecursorLabels; }
            set
            {
                _selectedPrecursorLabels = value;
                if (_precursorXicChanged)
                {
                    var precursorLabelList = _selectedPrecursorLabels.Cast<LabeledIon>().ToList();
                    foreach (var xicVm in XicViewModels)
                    {
                        xicVm.SelectedPrecursors = precursorLabelList;
                        xicVm.SelectedLightPrecursors = IonUtils.ReduceLabels(LightPrecursorLabels, precursorLabelList);
                        xicVm.SelectedHeavyPrecursors = IonUtils.ReduceLabels(HeavyPrecursorLabels, precursorLabelList);
                    }
                }
                OnPropertyChanged("SelectedPrecursorLabels");
            }
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
                OnPropertyChanged("IsLoading");
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
                OnPropertyChanged("FileOpen");
            }
        }

        /// <summary>
        /// Redraws the spectrum if SelectedPrSm or fragment ion selections have changed
        /// </summary>
        public void UpdateSpectrum()
        {
            if (_spectrumChanged)
            {
                if (SelectedPrSm == null || SelectedPrSm.Ms2Spectrum == null || SelectedPrSm.NextMs1 == null ||
                    SelectedPrSm.PreviousMs1 == null)
                {
                    Ms2SpectrumViewModel.ClearPlots();
                    return;
                }
                var fragmentLabels = SelectedPrSm.Heavy ? HeavyFragmentLabels : FragmentLabels;
                var precursorIon = SelectedPrSm.Heavy
                                 ? IonUtils.GetLabeledPrecursorIon(SelectedPrSm.HeavySequence, SelectedPrSm.Charge)
                                 : IonUtils.GetLabeledPrecursorIon(SelectedPrSm.Sequence, SelectedPrSm.Charge);
                Ms2SpectrumViewModel.RawFileName = SelectedPrSm.RawFileName;
                Ms2SpectrumViewModel.UpdatePlots(SelectedPrSm.Ms2Spectrum, fragmentLabels, SelectedPrSm.PreviousMs1, SelectedPrSm.NextMs1, precursorIon, SelectedPrSm.Heavy);
                _spectrumChanged = false;
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
                Task.Factory.StartNew(() =>
                {
                    ReadRawFile(name);
                    if (XicViewModels.Count > 0) ShowUnidentifiedScans = true;
                });
            }
        }

        /// <summary>
        /// Open identification file. Checks to ensure that there is a raw file open
        /// corresponding to this ID file.
        /// </summary>
        public void OpenIdFile()
        {
            const string formatStr = @"TSV Files (*.txt; *tsv)|*.txt;*.tsv|MzId Files (*.mzId)|*.mzId|MzId GZip Files (*.mzId.gz)|*.mzId.gz";
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
            if (!String.IsNullOrEmpty(rawFileName)) Task.Factory.StartNew(() =>
            {   // Name of raw file was found
                if (xicVm == null) xicVm = ReadRawFile(rawFileName);    // raw file isn't open yet
                ReadIdFile(tsvFileName, rawFileName, xicVm);    // finally read the TSV file
            });
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
                ids = reader.Read(xicVm.Lcms, xicVm.RawFileName);
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
            SelectedPrSm = null;
            _xicChanged = true;
            ShowUnidentifiedScans = false;
            if (Ids.Proteins.Count > 0) SelectedPrSm = Ids.GetHighestScoringPrSm();
            FileOpen = true;
            IsLoading = false;
        }

        /// <summary>
        /// Open raw file
        /// </summary>
        /// <param name="rawFilePath">Path to raw file to open</param>
        public XicViewModel ReadRawFile(string rawFilePath)
        {
            var xicVm = new XicViewModel(_colors); // create xic view model
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
                    MatchedFragments = Double.NaN,
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
            xicVm.SelectedScanNumberChanged += UpdatePrSm;
            xicVm.XicClosing += CloseXic;
            xicVm.ZoomToRt(0);
            GuiInvoker.Invoke(SetFragmentLabels);
            GuiInvoker.Invoke(SetPrecursorLabels);
            GuiInvoker.Invoke(() => { CreateSequenceViewModel.SelectedXicViewModel = XicViewModels[0]; });
            GuiInvoker.Invoke(() => { CreateSequenceViewModel.CreatePrSmCommand.Executable = true; });
            FileOpen = true;
            return xicVm;
        }

        /// <summary>
        /// Open data set (raw file and ID files) from PNNL DMS system
        /// </summary>
        public void OpenFromDms()
        {
            var data = _dialogService.OpenDmsLookup(new DmsLookupViewModel(_dialogService));
            if (data == null) return;
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
                return;
            }
            foreach (var rawFilePath in rawFileNames)
            {
                var raw = rawFilePath;
                var filePath = idFilePath;
                Task.Factory.StartNew(() =>
                {
                    var xicVm = ReadRawFile(raw);
                    if (!String.IsNullOrEmpty(filePath)) ReadIdFile(filePath, xicVm.RawFileName, xicVm);
                });
            }
        }

        /// <summary>
        /// Open settings window
        /// </summary>
        public void OpenSettings()
        {
            _dialogService.OpenSettings(new SettingsViewModel(_dialogService));
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
            SetFragmentLabels();
            SetPrecursorLabels();
            foreach (var xicVm in XicViewModels) xicVm.UpdatePlots();
            _spectrumChanged = true;
            UpdateSpectrum();
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
            OnPropertyChanged("ProteinIds");
            OnPropertyChanged("PrSms");
        }

        /// <summary>
        /// Set fragment ion labels for currently selected sequence
        /// </summary>
        private void SetFragmentLabels()
        {
            if (SelectedPrSm == null) return;
            var fragmentLabels = IonUtils.GetFragmentIonLabels(SelectedPrSm.Sequence, SelectedPrSm.Charge, IonTypeSelectorViewModel.IonTypes);
            var lightFragmentLabels = IonUtils.GetFragmentIonLabels(SelectedPrSm.LightSequence, SelectedPrSm.Charge, IonTypeSelectorViewModel.IonTypes);
            var heavyFragmentLabels = IonUtils.GetFragmentIonLabels(SelectedPrSm.HeavySequence, SelectedPrSm.Charge, IonTypeSelectorViewModel.IonTypes);
            FragmentLabels = fragmentLabels;
            LightFragmentLabels = lightFragmentLabels;
            HeavyFragmentLabels = heavyFragmentLabels;
            _fragmentXicChanged = false;
            OnPropertyChanged("FragmentLabels");
            OnPropertyChanged("LightFragmentLabels");
            OnPropertyChanged("HeavyFragmentLabels");
            _fragmentXicChanged = true;
            SelectedFragmentLabels = fragmentLabels;
        }

        /// <summary>
        /// Set precursor ion labels for currently selected sequence
        /// </summary>
        private void SetPrecursorLabels()
        {
            if (SelectedPrSm == null) return;
            var precursorLabels = IonUtils.GetPrecursorIonLabels(SelectedPrSm.Sequence, SelectedPrSm.Charge, -1, 2);
            var lightPrecursorLabels = IonUtils.GetPrecursorIonLabels(SelectedPrSm.LightSequence, SelectedPrSm.Charge, -1, 2);
            var heavyPrecursorLabels = IonUtils.GetPrecursorIonLabels(SelectedPrSm.HeavySequence, SelectedPrSm.Charge, -1, 2);
            PrecursorLabels = precursorLabels;
            LightPrecursorLabels = lightPrecursorLabels;
            HeavyPrecursorLabels = heavyPrecursorLabels;
            _precursorXicChanged = false;
            OnPropertyChanged("PrecursorLabels");
            OnPropertyChanged("LightPrecursorLabels");
            OnPropertyChanged("HeavyPrecursorLabels");
            _precursorXicChanged = true;
            SelectedPrecursorLabels = precursorLabels;
        }

        /// <summary>
        /// Event handler for ion types changing in IonTypeSelectorViewModel
        /// </summary>
        /// <param name="sender">The IonTypeSelectorViewModel</param>
        /// <param name="e"></param>
        private void SetIonTypes(object sender, EventArgs e)
        {
            if (SelectedPrSm == null) return;
            _spectrumChanged = true;
            SetFragmentLabels();
            UpdateSpectrum();
        }

        /// <summary>
        /// Event handler for RequestClose in XicViewModel
        /// Closes the raw file and cleans up IDs pointing to that raw file
        /// </summary>
        /// <param name="sender">The XicViewModel</param>
        /// <param name="e"></param>
        private void CloseXic(object sender, EventArgs e)
        {
            var xicVm = sender as XicViewModel;
            if (xicVm != null)
            {
                var rawFileName = xicVm.RawFileName;
                Ids.RemovePrSmsFromRawFile(rawFileName);
                FilterIds();
                XicViewModels.Remove(xicVm);
                if (XicViewModels.Count == 0) CreateSequenceViewModel.CreatePrSmCommand.Executable = false;
                if (SelectedPrSm == null || SelectedPrSm.RawFileName == rawFileName)
                {
                    if (XicViewModels.Count > 0) CreateSequenceViewModel.SelectedXicViewModel = XicViewModels[0];
                    if (PrSms.Count > 0) SelectedPrSm = Ids.GetHighestScoringPrSm();
                    else
                    {
                        _selectedPrSm = null;
                        OnPropertyChanged("SelectedPrSm");
                        FragmentLabels = new List<LabeledIon>(); OnPropertyChanged("FragmentLabels");
                        HeavyFragmentLabels = new List<LabeledIon>(); OnPropertyChanged("HeavyFragmentLabels");
                        PrecursorLabels = new List<LabeledIon>(); OnPropertyChanged("PrecursorLabels");
                        HeavyPrecursorLabels = new List<LabeledIon>(); OnPropertyChanged("HeavyPrecursorLabels");
                        _fragmentXicChanged = true;
                        SelectedFragmentLabels = FragmentLabels;
                        _precursorXicChanged = true;
                        SelectedPrecursorLabels = PrecursorLabels;
                        _spectrumChanged = true;
                        UpdateSpectrum();
                    }
                }
            }
        }

        /// <summary>
        /// Event handler to set PrSm when a new scan is selected in XicViewModel or
        /// CreatePrSmViewModel
        /// </summary>
        /// <param name="sender">The XicViewModel or PrSmViewModel</param>
        /// <param name="e"></param>
        private void UpdatePrSm(object sender, EventArgs e)
        {
            var prsmChangedEventArgs = e as PrSmChangedEventArgs;

            if (prsmChangedEventArgs != null)
            {
                var prsm = prsmChangedEventArgs.PrSm;
                if (prsm.Sequence.Count == 0)
                {
                    prsm.MatchedFragments = -1.0;
                    prsm.Sequence = SelectedPrSm.Sequence;
                    prsm.SequenceText = SelectedPrSm.SequenceText;
                    prsm.Charge = SelectedPrSm.Charge;
                    prsm.ProteinName = SelectedPrSm.ProteinName;
                    prsm.ProteinNameDesc = SelectedPrSm.ProteinNameDesc;
                }
                SelectedPrSm = prsm;
            }
        }

        private readonly IMainDialogService _dialogService;
        private readonly ColorDictionary _colors;
        private readonly Mutex _idTreeMutex;

        private bool _isLoading;
        private bool _fileOpen;

        private PrSm _selectedPrSm;
        private IList _selectedFragmentLabels;
        private IList _selectedPrecursorLabels;

        private bool _xicChanged;
        private bool _spectrumChanged;
        private bool _fragmentXicChanged;
        private bool _precursorXicChanged;
        private object _treeViewSelectedItem;
        private bool _showUnidentifiedScans;
    }
}

