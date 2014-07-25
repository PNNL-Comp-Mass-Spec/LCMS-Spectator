using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.PlotModels;
using LcmsSpectator.Utils;
using LcmsSpectatorModels.Config;
using LcmsSpectatorModels.Models;

namespace LcmsSpectator.ViewModels
{
    public class MsPathViewerViewModel: ViewModelBase
    {
        public List<ProteinId> Ids { get; set; }
        public List<PrSm> PrSms { get; set; } 
        public List<IonType> IonTypes { get; set; }
        public BaseIonType[] BaseIonTypes { get; set; }
        public NeutralLoss[] NeutralLosses { get; set; }
        public DelegateCommand SetIonChargesCommand { get; set; }

        public List<LabeledXic> FragmentXics { get; set; }
        public List<LabeledXic> PrecursorXics { get; set; }

        public string SequenceText { get; set; }
        public int SelectedCharge { get; set; }
        public DelegateCommand CreatePrSmCommand { get; set; }

        public event EventHandler UpdateSelections;

        public SpectrumViewModel Ms2SpectrumViewModel { get; private set; }
        public XicViewModel XicViewModel { get; private set; }

        public MsPathViewerViewModel()
        {
            Ids = new List<ProteinId>();
            PrSms = new List<PrSm>();
            _colors = new ColorDictionary(2);
            Ms2SpectrumViewModel = new SpectrumViewModel(_colors);
            XicViewModel = new XicViewModel(_colors);

            _spectrumChanged = false;
            _xicChanged = false;

            SetIonChargesCommand = new DelegateCommand(SetIonCharges);
            CreatePrSmCommand = new DelegateCommand(CreatePrSm);

            _guiThread = Dispatcher.CurrentDispatcher;

            FragmentXics = new List<LabeledXic>();
            SelectedFragmentXics = new List<LabeledXic>();
            PrecursorXics = new List<LabeledXic>();
            SelectedPrecursorXics = new List<LabeledXic>();

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

            XicViewModel.SelectedScanNumberChanged += XicScanNumberChanged;
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
                ProteoformId proteoform = null;
                foreach (var prot in _selectedProtein.Proteoforms)
                {
                    if (prot.Contains(_selectedPrSm)) proteoform = prot;
                }
                if (proteoform == null) return;
                ChargeStateId chargeState = null;
                foreach (var cs in proteoform.ChargeStates)
                {
                    if (cs.Contains(_selectedPrSm)) chargeState = cs;
                }
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
                XicViewModel.UpdateSelectedScan(_selectedPrSm.Scan);
                if (_selectedChargeState == null || _selectedChargeState.SequenceText != value.SequenceText ||_selectedChargeState.Charge != value.Charge)
                {
                    _maxFragmentIonCharge = Math.Min(Math.Max(value.Charge - 1, 2), 15);
                    MinCharge = 1;
                    MaxCharge = _maxFragmentIonCharge;
                    _colors.BuildColorDictionary(_maxFragmentIonCharge);
                    _selectedChargeState = value;
                    _xicChanged = true;
                    UpdateXics();
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
                ProteinId protein = null;
                foreach (var prot in Ids)
                {
                    if (prot.Contains(_selectedPrSm)) protein = prot;
                }
                if (protein != null) SelectedProtein = protein;
                else
                {
                    var newChargeState = new ChargeStateId(_selectedPrSm.Charge, _selectedPrSm.Sequence, 
                                                           _selectedPrSm.SequenceText, _selectedPrSm.ProteinNameDesc, _selectedPrSm.PrecursorMz);
                    newChargeState.PrSms.Add(_selectedPrSm);
                    SelectedChargeState = newChargeState;
                }
            }
        }


        public List<LabeledXic> SelectedFragmentXics
        {
            get { return _selectedFragmentXics; }
            set
            {
                _selectedFragmentXics = value;
                OnPropertyChanged("SelectedFragmentXics");
                if (SelectedChargeState != null)
                {
                    SelectedChargeState.SelectedFragmentXics = value;
                }
            }
        }
        public List<LabeledXic> SelectedPrecursorXics
        {
            get { return _selectedPrecursorXics; }
            set
            {
                _selectedPrecursorXics = value;
                OnPropertyChanged("SelectedPrecursorXics");
                if (SelectedChargeState != null)
                {
                    SelectedChargeState.SelectedPrecursorXics = value;
                }
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
                _xicChanged = true;
                UpdateSpectrum();
                SetFragmentXics();
                LoadFragmentXicPlot();
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
                _xicChanged = true;
                UpdateSpectrum();
                SetFragmentXics();
                LoadFragmentXicPlot();
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

        /// <summary>
        /// Redraws the the XICs if SelectedChargeState has changed
        /// </summary>
        public void UpdateXics()
        {
            if (_xicChanged)
            {
                if (SelectedChargeState == null) return;
                SetFragmentXics();
                SetPrecursorXics();
                XicViewModel.Update(SelectedChargeState, _selectedPrSm.Scan);
                _xicChanged = false;
            }
        }

        /// <summary>
        /// Open a raw file, parameter file, and identification file
        /// </summary>
        /// <param name="paramFile">Name of the parameter file to open.</param>
        /// <param name="idFile">Name of the identification list file to open.</param>
        /// <param name="rawFile">Name of the raw file to open.</param>
        public void OpenFile(string paramFile, string idFile, string rawFile)
        {
            if (paramFile == "" || idFile == "") return;
            IsLoading = true;
            Task.Factory.StartNew(() =>
            {
                var reader = new IcFileReader(idFile, paramFile, rawFile);
                var ids = reader.Read();
                XicViewModel.Lcms = IcParameters.Instance.Lcms;
                _minFragmentIonCharge = IcParameters.Instance.MinProductIonCharge;
                _maxFragmentIonCharge = IcParameters.Instance.MaxProductIonCharge;
                MinCharge = _minFragmentIonCharge;
                MaxCharge = _maxFragmentIonCharge;
                _selectedPrSm = null;
                _selectedChargeState = null;
                SetIonCharges();
                Ids = ids;
                var prsms = new List<PrSm>();
                foreach (var id in Ids) prsms.AddRange(id.Scans);
                PrSms = prsms;
                prsms.Sort();
                OnPropertyChanged("Ids");
                OnPropertyChanged("PrSms");
                SelectedPrSm = null;
                if (Ids.Count > 0) SelectedPrSm = Ids[0].GetHighestScoringPrSm();
                SelectedCharge = 2;
                OnPropertyChanged("SelectedCharge");
                FileOpen = true;
                IsLoading = false;
            });
        }

        /// <summary>
        /// Update Precursor Ion Xic plot
        /// </summary>
        public void LoadPrecursorXicPlot()
        {
            if (_selectedPrSm == null || _selectedChargeState == null) return;
            XicViewModel.PrecursorUpdate(SelectedChargeState);
        }

        /// <summary>
        /// Update Fragment Ion Xic plot
        /// </summary>
        public void LoadFragmentXicPlot()
        {
            if (_selectedPrSm == null || _selectedChargeState == null) return;
            XicViewModel.FragmentUpdate(SelectedChargeState, _selectedPrSm.Scan);
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
                Scan = 0,
                Sequence = sequence,
                SequenceText = SequenceText,
                Charge = SelectedCharge
            };
            SelectedPrSm = prsm;
        }

        private void SetFragmentXics()
        {
            if (SelectedChargeState == null) return;
            var fragmentXics = new List<LabeledXic>();
            foreach (var neutralLoss in SelectedNeutralLosses)
            {
                foreach (var baseIonType in SelectedBaseIonTypes)
                {
                    for (var charge = MinCharge;
                        charge <= MaxCharge;
                        charge++)
                    {
                        fragmentXics.AddRange(_selectedChargeState.GetFragmentXics(baseIonType, neutralLoss, charge));
                    }
                }
            }
            FragmentXics = fragmentXics;
            OnPropertyChanged("FragmentXics");
            SelectedFragmentXics = fragmentXics;
            _guiThread.Invoke(UpdateSelections, this, null);
        }

        private void SetPrecursorXics()
        {
            if (SelectedChargeState == null) return;
            var precursorXics = new List<LabeledXic>();
            for (int i = -1; i <= 2; i++)
            {
                precursorXics.Add(SelectedChargeState.PrecursorIonXics(i));
            }
            PrecursorXics = precursorXics;
            OnPropertyChanged("PrecursorXics");
            SelectedPrecursorXics = precursorXics;
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
            _xicChanged = true;
            UpdateSpectrum();
            SetFragmentXics();
            SetPrecursorXics();
            LoadFragmentXicPlot();
            LoadPrecursorXicPlot();
        }

        private void XicScanNumberChanged(object sender, EventArgs e)
        {
            var prsmChangedEventArgs = e as PrSmChangedEventArgs;
            if (prsmChangedEventArgs != null) SelectedPrSm = prsmChangedEventArgs.PrSm;
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
        private bool _xicChanged;
        private List<LabeledXic> _selectedPrecursorXics;
        private List<LabeledXic> _selectedFragmentXics;
    }
}

