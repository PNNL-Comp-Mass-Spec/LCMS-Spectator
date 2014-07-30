using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
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
        public BaseIonType[] BaseIonTypes { get; set; }
        public NeutralLoss[] NeutralLosses { get; set; }
        public DelegateCommand SetIonChargesCommand { get; set; }

        public List<LabeledIon> FragmentLabels { get; set; }
        public List<LabeledIon> PrecursorLabels { get; set; }

        public string SequenceText { get; set; }
        public int SelectedCharge { get; set; }
        public DelegateCommand CreatePrSmCommand { get; set; }

        public event EventHandler UpdateSelections;

        public SpectrumViewModel Ms2SpectrumViewModel { get; private set; }
        public ObservableCollection<XicViewModel> XicViewModels { get; private set; }

        public LcmsSpectatorViewModel()
        {
            Ids = new IdentificationTree();
            ProteinIds = Ids.Proteins.Values.ToList();
            PrSms = new List<PrSm>();
            _colors = new ColorDictionary(2);
            Ms2SpectrumViewModel = new SpectrumViewModel(_colors);
            XicViewModels = new ObservableCollection<XicViewModel>();
//            var XicViewModel = new XicViewModel(_colors);
//            XicViewModels.Add(XicViewModel);

            _spectrumChanged = false;

            SetIonChargesCommand = new DelegateCommand(SetIonCharges);
            CreatePrSmCommand = new DelegateCommand(CreatePrSm);

            _guiThread = Dispatcher.CurrentDispatcher;

            FragmentLabels = new List<LabeledIon>();
            SelectedFragmentLabels = new List<LabeledIon>();
            PrecursorLabels = new List<LabeledIon>();
            SelectedPrecursorLabels = new List<LabeledIon>();

            _selectedBaseIonTypes = new List<BaseIonType>();
            BaseIonTypes = BaseIonType.AllBaseIonTypes.ToArray();
            SelectedBaseIonTypes.Add(BaseIonType.B);
            SelectedBaseIonTypes.Add(BaseIonType.Y);

            NeutralLosses = NeutralLoss.CommonNeutralLosses.ToArray();
            SelectedNeutralLosses = new List<NeutralLoss> {NeutralLoss.NoLoss};

            _minFragmentIonCharge = 1;
            _maxFragmentIonCharge = 15;
            _minSelectedFragmentIonCharge = _minFragmentIonCharge;
            _maxSelectedFragmentIonCharge = _maxFragmentIonCharge;
            MinCharge = _minFragmentIonCharge;
            MaxCharge = _maxFragmentIonCharge;

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
                    _maxFragmentIonCharge = Math.Min(Math.Max(value.Charge - 1, 2), 15);
                    MinCharge = 1;
                    MaxCharge = _maxFragmentIonCharge;
                    _colors.BuildColorDictionary(_maxFragmentIonCharge);
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
        /// Neutral losses selected for fragment ion display on spectrum and XIC
        /// </summary>
        public List<NeutralLoss> SelectedNeutralLosses
        {
            get { return _selectedNeutralLosses; }
            set
            {
                _selectedNeutralLosses = value;
                OnPropertyChanged("SelectedNeutralLosses");
                _spectrumChanged = true;
                UpdateSpectrum();
                SetFragmentLabels();
            }
        }

        /// <summary>
        /// Ion types selected for fragment ion display on spectrum and XIC
        /// </summary>
        public List<BaseIonType> SelectedBaseIonTypes
        {
            get { return _selectedBaseIonTypes; }
            set
            {
                _selectedBaseIonTypes = value;
                OnPropertyChanged("SelectedBaseIonTypes");
                _spectrumChanged = true;
                UpdateSpectrum();
                SetFragmentLabels();
            }
        }

        /// <summary>
        /// Minimum fragment ion charge selected
        /// </summary>
        public int MinCharge
        {
            get { return _minCharge; }
            set
            {
                _minCharge = value;
                OnPropertyChanged("MinCharge");
            }
        }

        /// <summary>
        /// Maximum fragment ion selected
        /// </summary>
        public int MaxCharge
        {
            get { return _maxCharge; }
            set
            {
                _maxCharge = value;
                OnPropertyChanged("MaxCharge");
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
                if (SelectedPrSm == null) return;
                Ms2SpectrumViewModel.UpdatePlots(SelectedPrSm, SelectedBaseIonTypes, SelectedNeutralLosses, MinCharge, MaxCharge);
                _spectrumChanged = false;
            }
        }

        public void OpenRawFile(string rawFileName)
        {
            if (rawFileName == "") return;
            IsLoading = true;
            Task.Factory.StartNew(() =>
            {
                var fileNameWithoutPath = Path.GetFileNameWithoutExtension(rawFileName);
                var lcms = LcMsRun.GetLcMsRun(rawFileName, MassSpecDataType.XCaliburRun, 0, 0);
                var xicVm = new XicViewModel(fileNameWithoutPath, _colors)
                {
                    Lcms = lcms
                };
                xicVm.SelectedScanNumberChanged += XicScanNumberChanged;
                xicVm.ZoomToScan(0);
                var addXicAction = new Action<XicViewModel>(XicViewModels.Add);
                _guiThread.Invoke(addXicAction, xicVm);
                SetFragmentLabels();
                SetPrecursorLabels();
                FileOpen = true;
                IsLoading = false;
            });
        }

        public void OpenTsvFile(string tsvFileName)
        {
            var fileName = Path.GetFileNameWithoutExtension(tsvFileName);
            XicViewModel xicVm = null;
            foreach (var xic in XicViewModels)
            {
                if (xic.RawFileName == fileName) xicVm = xic;
            }
            if (xicVm == null)
            {
                MessageBox.Show("Cannot find raw file corresponding to this ID file.");
                return;
            }
            IsLoading = true;
            Task.Factory.StartNew(() =>
            {
                var reader = new IcFileReader(tsvFileName, xicVm.Lcms);
                var ids = reader.Read();
                Ids.Add(ids);
                var prsms = Ids.AllPrSms;
                prsms.Sort();
                PrSms = prsms;
                ProteinIds = Ids.Proteins.Values.ToList();
                OnPropertyChanged("ProteinIds");
                OnPropertyChanged("PrSms");
                SelectedPrSm = null;
                if (Ids.Proteins.Count > 0) SelectedPrSm = Ids.GetHighestScoringPrSm();
                SelectedCharge = 2;
                OnPropertyChanged("SelectedCharge");
                FileOpen = true;
                IsLoading = false;
            });
        }

        /// <summary>
        /// Open a raw file, parameter file, and identification file
        /// </summary>
        /// <param name="idFile">Name of the identification list file to open.</param>
        /// <param name="rawFile">Name of the raw file to open.</param>
        public void OpenFile(string idFile, string rawFile)
        {
            if (idFile == "") return;
            IsLoading = true;
            Task.Factory.StartNew(() =>
            {
                var reader = new IcFileReader(idFile, rawFile);
                var ids = reader.Read();
                var rawFileWithoutPath = Path.GetFileNameWithoutExtension(rawFile);
                var xicViewModel = new XicViewModel(rawFileWithoutPath, _colors)
                {
                    Lcms = IcParameters.Instance.Lcms
                };
                xicViewModel.SelectedScanNumberChanged += XicScanNumberChanged;
                var addXicAction = new Action<XicViewModel>(XicViewModels.Add);
                _guiThread.Invoke(addXicAction, xicViewModel);
                _minFragmentIonCharge = 1;
                _maxFragmentIonCharge = 15;
                MinCharge = _minFragmentIonCharge;
                MaxCharge = _maxFragmentIonCharge;
                _selectedPrSm = null;
                _selectedChargeState = null;
                SetIonCharges();
                Ids = ids;
                var prsms = Ids.AllPrSms;
                PrSms = prsms;
                prsms.Sort();
                OnPropertyChanged("Ids");
                OnPropertyChanged("PrSms");
                SelectedPrSm = null;
                if (Ids.Proteins.Count > 0) SelectedPrSm = Ids.GetHighestScoringPrSm();
                SelectedCharge = 2;
                OnPropertyChanged("SelectedCharge");
                FileOpen = true;
                IsLoading = false;
            });
        }

        private void CreatePrSm()
        {
            var sequence = Sequence.GetSequenceFromMsGfPlusPeptideStr(SequenceText);
            if (sequence == null)
            {
                MessageBox.Show("Invalid sequence");
                return;
            }
            if (SelectedCharge < 1)
            {
                MessageBox.Show("Invalid Charge.");
                return;
            }
            var prsm = new PrSm
            {
                ProteinName = "",
                ProteinNameDesc = "",
                ProteinDesc = "",
                Scan = 0,
                Sequence = sequence,
                SequenceText = SequenceText,
                Charge = SelectedCharge
            };
            SelectedPrSm = prsm;
        }

        private void SetFragmentLabels()
        {
            if (SelectedChargeState == null) return;
            var ionTypes = IonUtils.GetIonTypes(IcParameters.Instance.IonTypeFactory, SelectedBaseIonTypes,
                SelectedNeutralLosses, MinCharge, MaxCharge);
            var fragmentLabels = IonUtils.GetFragmentIonLabels(SelectedChargeState.Sequence, SelectedChargeState.Charge, ionTypes);
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

        private void SetIonCharges()
        {
            try
            {
                if (MinCharge < _minFragmentIonCharge ||
                    MaxCharge > _maxFragmentIonCharge ||
                    MinCharge > MaxCharge) throw new FormatException();
                _minSelectedFragmentIonCharge = MinCharge;
                _maxSelectedFragmentIonCharge = MaxCharge;
            }
            catch (FormatException)
            {
                MessageBox.Show("Min and Max must be integers between " +
                                _minFragmentIonCharge + " and " + _maxFragmentIonCharge + ", inclusive.");
                MinCharge = _minSelectedFragmentIonCharge;
                MaxCharge = _maxSelectedFragmentIonCharge;
            }
            if (SelectedPrSm == null || SelectedChargeState == null) return;
            _spectrumChanged = true;
            UpdateSpectrum();
            SetFragmentLabels();
            SetPrecursorLabels();
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

        private PrSm _selectedPrSm;
        private List<BaseIonType> _selectedBaseIonTypes;
        private readonly ColorDictionary _colors;

        private bool _isLoading;
        private int _minSelectedFragmentIonCharge;
        private int _maxSelectedFragmentIonCharge;
        private int _minFragmentIonCharge;
        private int _maxFragmentIonCharge;
        private int _minCharge;
        private int _maxCharge;
        private bool _fileOpen;
        private ProteinId _selectedProtein;

        private readonly Dispatcher _guiThread;

        private List<NeutralLoss> _selectedNeutralLosses;
        private ChargeStateId _selectedChargeState;

        private bool _spectrumChanged;
        private List<LabeledIon> _selectedFragmentLabels;
        private List<LabeledIon> _selectedPrecursorLabels;
    }
}

