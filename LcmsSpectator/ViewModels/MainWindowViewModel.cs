using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
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
        public IdentificationTree Ids { get; set; }
        public List<ProteinId> ProteinIds { get; set; }
        public List<PrSm> PrSms { get; set; } 
        public List<IonType> IonTypes { get; set; }

        public List<LabeledIon> FragmentLabels { get; set; }
        public List<LabeledIon> HeavyFragmentLabels { get; set; } 
        public List<LabeledIon> PrecursorLabels { get; set; }
        public List<LabeledIon> HeavyPrecursorLabels { get; set; } 

        public DelegateCommand OpenRawFileCommand { get; set; }
        public DelegateCommand OpenTsvFileCommand { get; set; }
        public DelegateCommand SaveCommand { get; set; }
        public DelegateCommand OpenSettingsCommand { get; set; }

        public CreateSequenceViewModel CreateSequenceViewModel { get; set; }
        public IonTypeSelectorViewModel IonTypeSelectorViewModel { get; private set; }
        public SpectrumViewModel Ms2SpectrumViewModel { get; private set; }
        public ObservableCollection<XicViewModel> XicViewModels { get; private set; }

        public MainWindowViewModel(IMainDialogService dialogService)
        {
            _xicChanged = false;
            _dialogService = dialogService;
            Ids = new IdentificationTree();
            ProteinIds = Ids.Proteins.Values.ToList();
            PrSms = new List<PrSm>();
            _colors = new ColorDictionary(2);
            IonTypeSelectorViewModel = new IonTypeSelectorViewModel(_dialogService);
            IonTypeSelectorViewModel.IonTypesUpdated += SetIonCharges;
            Ms2SpectrumViewModel = new SpectrumViewModel(_colors);
            XicViewModels = new ObservableCollection<XicViewModel>();
            CreateSequenceViewModel = new CreateSequenceViewModel(XicViewModels, _dialogService);
            CreateSequenceViewModel.SequenceCreated += UpdatePrSm;

            _spectrumChanged = false;

            OpenRawFileCommand = new DelegateCommand(OpenRawFile);
            OpenTsvFileCommand = new DelegateCommand(OpenTsvFile);
            OpenSettingsCommand = new DelegateCommand(OpenSettings);

            FragmentLabels = new List<LabeledIon>();
            SelectedFragmentLabels = new List<LabeledIon>();
            PrecursorLabels = new List<LabeledIon>();
            SelectedPrecursorLabels = new List<LabeledIon>();

            IsLoading = false;
            FileOpen = false;

            SelectedPrSm = null;
        }

        public object TreeViewSelectedItem
        {
            get { return _treeViewSelectedItem; }
            set
            {
                if (value != null)
                {
                    _treeViewSelectedItem = value;
                    if ((_treeViewSelectedItem as PrSm) != null)
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
                    var newChargeState = new ChargeStateId(_selectedPrSm.Charge, _selectedPrSm.Sequence, _selectedPrSm.HeavySequence,
                                                           _selectedPrSm.SequenceText, _selectedProtein.ProteinNameDesc, _selectedPrSm.PrecursorMz);
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
                if (_selectedChargeState == null || 
                    _selectedChargeState.SequenceText != value.SequenceText || 
                    _selectedChargeState.Charge != value.Charge || _xicChanged)
                {
                    var absoluteMaxCharge = Math.Min(Math.Max(value.Charge - 1, 2), 15);
                    _colors.BuildColorDictionary(absoluteMaxCharge);
                    IonTypeSelectorViewModel.AbsoluteMaxCharge = absoluteMaxCharge;
                    IonTypeSelectorViewModel.MinCharge = 1;
                    _selectedChargeState = value;
                    foreach (var xicVm in XicViewModels) xicVm.ZoomToScan(SelectedPrSm.Scan);
                    SetFragmentLabels();
                    SetPrecursorLabels();
                    _xicChanged = false;
                }
                UpdateSpectrum();
                foreach (var xicVm in XicViewModels)
                    xicVm.HighlightScan(SelectedPrSm.Scan, xicVm.RawFileName == SelectedPrSm.RawFileName, SelectedPrSm.Heavy);
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
                if (_xicChanged || _selectedPrSm == null) _xicChanged = true;
                _selectedPrSm = value;
                OnPropertyChanged("SelectedPrSm");
                _spectrumChanged = true;
                ProteinId protein = Ids.GetProtein(_selectedPrSm);
                if (protein != null) SelectedProtein = protein;
                else
                {
                    var newChargeState = new ChargeStateId(_selectedPrSm.Charge, _selectedPrSm.Sequence, _selectedPrSm.HeavySequence,
                                                           _selectedPrSm.SequenceText, _selectedPrSm.ProteinNameDesc, 
                                                           _selectedPrSm.PrecursorMz);
                    newChargeState.Add(_selectedPrSm);
                    SelectedChargeState = newChargeState;
                }
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

        public void OpenRawFile()
        {
            var rawFileName = _dialogService.OpenFile(".raw", @"Raw Files (*.raw)|*.raw");
            if (rawFileName == "") return;
            IsLoading = true;
            Task.Factory.StartNew(() => RawFileOpener(rawFileName));
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
                    foreach (var file in directory) // Raw file in same directory as tsv file?
                        if (file == path + ".raw") rawFileName = path + ".raw";
                    if (rawFileName == "")
                    {
                        _dialogService.MessageBox("Please select raw file.");
                        rawFileName = _dialogService.OpenFile(".raw", @"Raw Files (*.raw)|*.raw");
                            // manually find raw file
                    }
                }
            }
            else rawFileName = xicVm.RawFileName;
            Task.Factory.StartNew(() =>
            {
                if (rawFileName != "" && xicVm == null)
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
                else if (rawFileName == "") return;
                if (xicVm == null) return;
                IsLoading = true;
                var reader = IdFileReaderFactory.CreateReader(tsvFileName);
                var ids = reader.Read(xicVm.Lcms, xicVm.RawFileName);
                Ids.Add(ids);
                FilterIds();
                SelectedPrSm = null;
                _xicChanged = true;
                if (Ids.Proteins.Count > 0) SelectedPrSm = Ids.GetHighestScoringPrSm();
                FileOpen = true;
                IsLoading = false;
            });
        }

        public void RawFileOpener(string rawFileName)
        {
            IsLoading = true;
            var fileNameWithoutPath = Path.GetFileNameWithoutExtension(rawFileName);
            var lcms = LcMsRun.GetLcMsRun(rawFileName, MassSpecDataType.XCaliburRun, 0, 0);
            var xicVm = new XicViewModel(fileNameWithoutPath, lcms, _colors);
            xicVm.SelectedScanNumberChanged += UpdatePrSm;
            xicVm.XicClosing += CloseXic;
            xicVm.ZoomToScan(0);
            var addXicAction = new Action<XicViewModel>(XicViewModels.Add);
            GuiInvoker.Invoke(addXicAction, xicVm);
            GuiInvoker.Invoke(SetFragmentLabels);
            GuiInvoker.Invoke(SetPrecursorLabels);
            GuiInvoker.Invoke(() => { CreateSequenceViewModel.SelectedXicViewModel = XicViewModels[0]; });
            GuiInvoker.Invoke(() => { CreateSequenceViewModel.CreatePrSmCommand.Executable = true; });
            FileOpen = true;
            IsLoading = false;
        }

        public void OpenSettings()
        {
            var qValue = IcParameters.Instance.QValueThreshold;
            var saved = _dialogService.OpenSettings(new SettingsViewModel(_dialogService));
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

        private void SetFragmentLabels()
        {
            if (SelectedChargeState == null) return;
            var fragmentLabels = IonUtils.GetFragmentIonLabels(SelectedChargeState.Sequence, SelectedChargeState.Charge, IonTypeSelectorViewModel.IonTypes);
            var heavyFragmentLabels = IonUtils.GetFragmentIonLabels(SelectedChargeState.HeavySequence, SelectedChargeState.Charge, IonTypeSelectorViewModel.IonTypes);
            FragmentLabels = fragmentLabels;
            HeavyFragmentLabels = heavyFragmentLabels;
            _fragmentXicChanged = false;
            OnPropertyChanged("FragmentLabels");
            OnPropertyChanged("HeavyFragmentLabels");
            _fragmentXicChanged = true;
            SelectedFragmentLabels = fragmentLabels;
        }

        private void SetPrecursorLabels()
        {
            if (SelectedChargeState == null) return;
            var precursorLabels = IonUtils.GetPrecursorIonLabels(SelectedChargeState.Sequence, SelectedChargeState.Charge, -1, 2);
            var heavyPrecursorLabels = IonUtils.GetPrecursorIonLabels(SelectedChargeState.HeavySequence, SelectedChargeState.Charge, -1, 2);
            PrecursorLabels = precursorLabels;
            HeavyPrecursorLabels = heavyPrecursorLabels;
            _precursorXicChanged = false;
            OnPropertyChanged("PrecursorLabels");
            OnPropertyChanged("HeavyPrecursorLabels");
            _precursorXicChanged = true;
            SelectedPrecursorLabels = precursorLabels;
        }

        private void SetIonCharges(object sender, EventArgs e)
        {
            if (SelectedPrSm == null || SelectedChargeState == null) return;
            _spectrumChanged = true;
            UpdateSpectrum();
            SetFragmentLabels();
            Ms2SpectrumViewModel.ProductIons = SelectedPrSm.Heavy ? HeavyFragmentLabels : FragmentLabels;
        }

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
                if (SelectedPrSm.RawFileName == rawFileName)
                {
                    if (PrSms.Count > 0) SelectedPrSm = Ids.GetHighestScoringPrSm();
                    else
                    {
                        _selectedPrSm = null;
                        OnPropertyChanged("SelectedPrSm");
                        _selectedChargeState = null;
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

        private void UpdatePrSm(object sender, EventArgs e)
        {
            var prsmChangedEventArgs = e as PrSmChangedEventArgs;

            if (prsmChangedEventArgs != null)
            {
                var prsm = prsmChangedEventArgs.PrSm;
                if (prsm.Sequence == null)
                {
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

        private bool _isLoading;
        private bool _fileOpen;

        private ProteinId _selectedProtein;
        private ChargeStateId _selectedChargeState;
        private PrSm _selectedPrSm;
        private IList _selectedFragmentLabels;
        private IList _selectedPrecursorLabels;

        private bool _xicChanged;
        private bool _spectrumChanged;
        private bool _fragmentXicChanged;
        private bool _precursorXicChanged;
        private object _treeViewSelectedItem;
    }
}

