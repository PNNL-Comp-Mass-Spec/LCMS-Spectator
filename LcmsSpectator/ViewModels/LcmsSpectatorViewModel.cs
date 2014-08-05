using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.DialogServices;
using LcmsSpectator.PlotModels;
using LcmsSpectator.Utils;
using LcmsSpectatorModels.Config;
using LcmsSpectatorModels.Models;
using LcmsSpectatorModels.Utils;

namespace LcmsSpectator.ViewModels
{
    public class LcmsSpectatorViewModel: ViewModelBase
    {
        public IdentificationTree Ids { get; set; }
        public List<ProteinId> ProteinIds { get; set; }
        public List<PrSm> PrSms { get; set; } 
        public List<IonType> IonTypes { get; set; }

        public List<LabeledIon> FragmentLabels { get; set; }
        public List<LabeledIon> PrecursorLabels { get; set; }

        public string SequenceText { get; set; }
        public int SelectedCharge { get; set; }
        public int SelectedScan { get; set; }
        public XicViewModel SelectedXicViewModel { get; set; }

        public DelegateCommand CreatePrSmCommand { get; set; }
        public DelegateCommand OpenRawFileCommand { get; set; }
        public DelegateCommand OpenTsvFileCommand { get; set; }
        public DelegateCommand SaveCommand { get; set; }
        public DelegateCommand OpenSettingsCommand { get; set; }

        public event EventHandler UpdateSelections;

        public IonTypeSelectorViewModel IonTypeSelectorViewModel { get; private set; }
        public SpectrumViewModel Ms2SpectrumViewModel { get; private set; }
        public ObservableCollection<XicViewModel> XicViewModels { get; private set; }

        public LcmsSpectatorViewModel(IMainDialogService dialogService)
        {
            _dialogService = dialogService;
            Ids = new IdentificationTree();
            ProteinIds = Ids.Proteins.Values.ToList();
            PrSms = new List<PrSm>();
            _colors = new ColorDictionary(2);
            IonTypeSelectorViewModel = new IonTypeSelectorViewModel(_dialogService);
            IonTypeSelectorViewModel.IonTypesUpdated += SetIonCharges;
            Ms2SpectrumViewModel = new SpectrumViewModel(_colors);
            XicViewModels = new ObservableCollection<XicViewModel>();

            _spectrumChanged = false;

            CreatePrSmCommand = new DelegateCommand(CreatePrSm, false);
            OpenRawFileCommand = new DelegateCommand(OpenRawFile);
            OpenTsvFileCommand = new DelegateCommand(OpenTsvFile);
            OpenSettingsCommand = new DelegateCommand(OpenSettings);

            _guiThread = Dispatcher.CurrentDispatcher;

            FragmentLabels = new List<LabeledIon>();
            SelectedFragmentLabels = new List<LabeledIon>();
            PrecursorLabels = new List<LabeledIon>();
            SelectedPrecursorLabels = new List<LabeledIon>();

            IsLoading = false;
            FileOpen = false;

            SelectedPrSm = null;
        }

        /// <summary>
        /// Currently selected protein in ID Tree and Sequence Graph
        /// </summary>
        public ProteinId SelectedProtein
        {
            get { return _selectedProtein; }
            set
            {
                _selectedProtein = value;
                OnPropertyChanged("SelectedProtein");
                if (_selectedProtein == null) return;
                var chargeState = Ids.GetChargeState(_selectedPrSm);
                if (chargeState != null && chargeState.Sequence.Equals(_selectedPrSm.Sequence)) SelectedChargeState = chargeState;
                else
                {
                    var newChargeState = new ChargeStateId(_selectedPrSm.Charge, _selectedPrSm.Sequence, _selectedPrSm.SequenceText, _selectedProtein.ProteinNameDesc, _selectedPrSm.PrecursorMz);
                    if (chargeState != null) newChargeState.PrSms = chargeState.PrSms;
                    SelectedChargeState = newChargeState;
                }
            }
        }

        /// <summary>
        /// Currently selected charge state and XICs
        /// </summary>
        public ChargeStateId SelectedChargeState
        {
            get { return _selectedChargeState; }
            set
            {
                if (value == null) return;
                foreach (var xicVm in XicViewModels) xicVm.SelectedScanNumber = _selectedPrSm.Scan;
                if (_selectedChargeState == null || 
                    _selectedChargeState.SequenceText != value.SequenceText || 
                    _selectedChargeState.Charge != value.Charge)
                {
                    var absoluteMaxCharge = Math.Min(Math.Max(value.Charge - 1, 2), 15);
                    _colors.BuildColorDictionary(absoluteMaxCharge);
                    IonTypeSelectorViewModel.AbsoluteMaxCharge = absoluteMaxCharge;
                    IonTypeSelectorViewModel.MinCharge = 1;
                    _selectedChargeState = value;
                    foreach (var xicVm in XicViewModels) xicVm.Reset();
                    SetFragmentLabels();
                    SetPrecursorLabels();
                    foreach (var xicVm in XicViewModels) xicVm.ZoomToScan(SelectedPrSm.Scan);
                }

                UpdateSpectrum();
                OnPropertyChanged("SelectedChargeState");
            }
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
                _selectedPrSm = value;
                OnPropertyChanged("SelectedPrSm");
                _spectrumChanged = true;
                ProteinId protein = Ids.GetProtein(_selectedPrSm);
                if (protein != null) SelectedProtein = protein;
                else
                {
                    var newChargeState = new ChargeStateId(_selectedPrSm.Charge, _selectedPrSm.Sequence, 
                                                           _selectedPrSm.SequenceText, _selectedPrSm.ProteinNameDesc, 
                                                           _selectedPrSm.PrecursorMz);
                    newChargeState.Add(_selectedPrSm);
                    SelectedChargeState = newChargeState;
                }
            }
        }

        public List<LabeledIon> SelectedFragmentLabels
        {
            get { return _selectedFragmentLabels; }
            set
            {
                _selectedFragmentLabels = value;
                foreach (var xicVm in XicViewModels) xicVm.SelectedFragments = _selectedFragmentLabels;
                OnPropertyChanged("SelectedFragmentLabels");
            }
        }

        public List<LabeledIon> SelectedPrecursorLabels
        {
            get { return _selectedPrecursorLabels; }
            set
            {
                _selectedPrecursorLabels = value;
                foreach (var xicVm in XicViewModels) xicVm.SelectedPrecursors = _selectedPrecursorLabels;
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
                if (SelectedPrSm == null || SelectedPrSm.Ms2Spectrum == null) return;
                Ms2SpectrumViewModel.UpdatePlots(SelectedPrSm.Ms2Spectrum, FragmentLabels, SelectedPrSm.PreviousMs1, SelectedPrSm.NextMs1, 
                                                 IonUtils.GetLabeledPrecursorIon(SelectedPrSm.Sequence, SelectedPrSm.Charge));
                _spectrumChanged = false;
            }
        }

        public void OpenRawFile()
        {
            var rawFileName = _dialogService.OpenFile(".raw", @"Raw Files (*.raw)|*.raw");
            if (rawFileName == "") return;
            IsLoading = true;
            RawFileOpener(rawFileName);
        }

        public void OpenTsvFile()
        {
            var tsvFileName = _dialogService.OpenFile(".txt", @"IC ID Files (*.tsv)|*.tsv");
            if (tsvFileName == "") return;
            var fileName = Path.GetFileNameWithoutExtension(tsvFileName);
            var ext = Path.GetExtension(tsvFileName);
            string path = ext != null ? tsvFileName.Remove(tsvFileName.IndexOf(ext, StringComparison.Ordinal)) : tsvFileName;
            string rawFileName = "";
            XicViewModel xicVm = null;
            foreach (var xic in XicViewModels)      // Raw file already open?
            {
                if (xic.RawFileName == fileName) xicVm = xic;
            }
            if (xicVm == null)
            {
                var directoryName = Path.GetDirectoryName(tsvFileName);
                if (directoryName != null)
                {
                    var directory = Directory.GetFiles(directoryName);
                    foreach (var file in directory)         // Raw file in same directory as tsv file?
                        if (file == path + ".raw") rawFileName = path + ".raw";
                    if (rawFileName == "")
                    {
                        _dialogService.MessageBox("Please select raw file.");
                        rawFileName = _dialogService.OpenFile(".raw", @"Raw Files (*.raw)|*.raw"); // manually find raw file
                    }
                }
            }
            Task.Factory.StartNew(() =>
            {
                if (rawFileName != "")
                {
                    RawFileOpener(rawFileName);
                    var rawFile = Path.GetFileNameWithoutExtension(rawFileName);
                    foreach (var xic in XicViewModels)
                    {
                        if (xic.RawFileName == rawFile) xicVm = xic;
                    }
                    if (xicVm == null)
                    {
                        _dialogService.MessageBox("Cannot open id file.");
                        return;
                    }
                }
                else return;
                IsLoading = true;
                var reader = IdFileReaderFactory.CreateReader(tsvFileName);
                var ids = reader.Read(xicVm.Lcms, xicVm.RawFileName);
                Ids.Add(ids);
                FilterIds();
                SelectedPrSm = null;
                if (Ids.Proteins.Count > 0) SelectedPrSm = Ids.GetHighestScoringPrSm();
                SelectedCharge = 2;
                OnPropertyChanged("SelectedCharge");
                FileOpen = true;
                IsLoading = false;
            });
        }

        public void RawFileOpener(string rawFileName)
        {
            IsLoading = true;
            var fileNameWithoutPath = Path.GetFileNameWithoutExtension(rawFileName);
            var lcms = LcMsRun.GetLcMsRun(rawFileName, MassSpecDataType.XCaliburRun, 0, 0);
            var xicVm = new XicViewModel(fileNameWithoutPath, _colors)
            {
                Lcms = lcms
            };
            xicVm.SelectedScanNumberChanged += XicScanNumberChanged;
            xicVm.ZoomToScan(0);
            var addXicAction = new Action<XicViewModel>(XicViewModels.Add);
            GuiInvoker.Invoke(addXicAction, xicVm);
            GuiInvoker.Invoke(SetFragmentLabels);
            GuiInvoker.Invoke(SetPrecursorLabels);
            GuiInvoker.Invoke(() => { CreatePrSmCommand.Executable = true; });
            FileOpen = true;
            IsLoading = false;
        }

        public void OpenSettings()
        {
            var qValue = IcParameters.Instance.QValueThreshold;
            var saved = _dialogService.OpenSettings();
            var newQValue = IcParameters.Instance.QValueThreshold;
            if (saved && !qValue.Equals(newQValue))
            {
                FilterIds();
            }
        }

        private void FilterIds()
        {
            var qValue = IcParameters.Instance.QValueThreshold;
            var ids = Ids.GetTreeFilteredByQValue(qValue);
            ProteinIds = ids.Proteins.Values.ToList();
            var prsms = ids.AllPrSms;
            prsms.Sort();
            PrSms = prsms;
            OnPropertyChanged("ProteinIds");
            OnPropertyChanged("PrSms");
        }

        private void CreatePrSm()
        {
            var sequence = Sequence.GetSequenceFromMsGfPlusPeptideStr(SequenceText);
            LcMsRun lcms = null;
            if (sequence == null)
            {
                _dialogService.MessageBox("Invalid sequence.");
                return;
            }
            if (SelectedCharge < 1)
            {
                _dialogService.MessageBox("Invalid Charge.");
                return;
            }
            if (SelectedScan < 0)
            {
                _dialogService.MessageBox("Invalid Scan.");
                return;
            }
            if (SelectedXicViewModel == null || SelectedXicViewModel.Lcms == null) SelectedScan = 0;
            else lcms = SelectedXicViewModel.Lcms;
            var prsm = new PrSm
            {
                ProteinName = "",
                ProteinNameDesc = "",
                ProteinDesc = "",
                Scan = SelectedScan,
                Lcms = lcms,
                Sequence = sequence,
                SequenceText = SequenceText,
                Charge = SelectedCharge
            };
            SelectedPrSm = prsm;
        }

        private void SetFragmentLabels()
        {
            if (SelectedChargeState == null) return;
            var fragmentLabels = IonUtils.GetFragmentIonLabels(SelectedChargeState.Sequence, SelectedChargeState.Charge, IonTypeSelectorViewModel.IonTypes);
            FragmentLabels = fragmentLabels;
            OnPropertyChanged("FragmentLabels");
            SelectedFragmentLabels = fragmentLabels;
            _guiThread.Invoke(UpdateSelections, this, null);
        }

        private void SetPrecursorLabels()
        {
            if (SelectedChargeState == null) return;
            var precursorLabels = IonUtils.GetPrecursorIonLabels(SelectedChargeState.Sequence, SelectedChargeState.Charge, -1, 2);
            PrecursorLabels = precursorLabels;
            OnPropertyChanged("PrecursorLabels");
            SelectedPrecursorLabels = precursorLabels;
            _guiThread.Invoke(UpdateSelections, this, null);
        }

        private void SetIonCharges(object sender, EventArgs e)
        {
            if (SelectedPrSm == null || SelectedChargeState == null) return;
            _spectrumChanged = true;
            UpdateSpectrum();
            SetFragmentLabels();
            SetPrecursorLabels();
            Ms2SpectrumViewModel.ProductIons = FragmentLabels;
        }

        private void XicScanNumberChanged(object sender, EventArgs e)
        {
            var prsmChangedEventArgs = e as PrSmChangedEventArgs;
            if (prsmChangedEventArgs != null)
            {
                var prsm = prsmChangedEventArgs.PrSm;
                prsm.Sequence = SelectedPrSm.Sequence;
                prsm.SequenceText = SelectedPrSm.SequenceText;
                prsm.Charge = SelectedPrSm.Charge;
                prsm.ProteinName = SelectedPrSm.ProteinName;
                prsm.ProteinNameDesc = SelectedPrSm.ProteinNameDesc;
                SelectedPrSm = prsm;
            }
        }

        private readonly IMainDialogService _dialogService;
        private PrSm _selectedPrSm;
        private readonly ColorDictionary _colors;

        private bool _isLoading;

        private bool _fileOpen;
        private ProteinId _selectedProtein;

        private readonly Dispatcher _guiThread;

        private ChargeStateId _selectedChargeState;

        private bool _spectrumChanged;
        private List<LabeledIon> _selectedFragmentLabels;
        private List<LabeledIon> _selectedPrecursorLabels;
    }
}

