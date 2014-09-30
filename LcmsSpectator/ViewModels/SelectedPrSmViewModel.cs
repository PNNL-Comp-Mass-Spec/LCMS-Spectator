using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectatorModels.Models;
using LcmsSpectatorModels.Utils;

namespace LcmsSpectator.ViewModels
{
    public class SelectedPrSmViewModel : ViewModelBase
    {
        public Task<List<LabeledIonViewModel>> FragmentLabelUpdate { get; private set; }
        public Task<List<LabeledIonViewModel>> PrecursorLabelUpdate { get; private set; }

        private SelectedPrSmViewModel()
        {
            _ionTypes = new List<IonType>();
            _fragmentLabelCache = new Dictionary<Tuple<string, bool>, LabeledIonViewModel>();
            _precursorLabelCache = new Dictionary<Tuple<string, bool>, LabeledIonViewModel>();
            Messenger.Default.Register<PropertyChangedMessage<List<IonType>>>(this, IonTypesChanged);
            Sequence = new Sequence(new List<AminoAcid>());
            FragmentLabelUpdate = Task.Run(() => GenerateFragmentLabels(Sequence, false));
            PrecursorLabelUpdate = Task.Run(() => GeneratePrecursorLabels(Sequence));
        }

        public static SelectedPrSmViewModel Instance
        {
            get { return _instance ?? (_instance = new SelectedPrSmViewModel()); }
        }

        #region PrSm Properties
        public PrSm PrSm
        {
            get
            {
                var prsm = new PrSm
                {
                    Lcms = Lcms,
                    RawFileName = RawFileName,
                    Scan = Scan,
                    Sequence = Sequence,
                    SequenceText = SequenceText,
                    Charge = Charge,
                    Heavy = Heavy,
                    Score = Score,
                    QValue = QValue,
                };
                return prsm;
            }
            set
            {
                var oldValue = PrSm;
                Lcms = value.Lcms;
                RawFileName = value.RawFileName;
                Heavy = value.Heavy;
                _charge = value.Charge;
                _noLabelPrecursorMz = value.PrecursorMz;
                _heavyPrecursorMz = value.HeavyPrecursorMz;
                PrecursorMz = Heavy ? _heavyPrecursorMz : _noLabelPrecursorMz;
                Sequence = value.Sequence;
                SequenceText = value.SequenceText;
                Scan = value.Scan;
                RaisePropertyChanged("PrSm", oldValue, PrSm, true);
                Charge = value.Charge;
            }
        }

        public ILcMsRun Lcms
        {
            get { return _lcms; }
            set
            {
                var oldValue = _lcms;
                _lcms = value;
                RaisePropertyChanged("Lcms", oldValue, _lcms, true);
            }
        }

        public string RawFileName
        {
            get { return _rawFileName; }
            set
            {
                var oldValue = _rawFileName;
                _rawFileName = value;
                RaisePropertyChanged("RawFileName", oldValue, _rawFileName, true);
            }
        }

        public int Scan
        {
            get { return _scan; }
            set
            {
                var oldValue = _scan;
                _scan = value;
                RaisePropertyChanged("Scan", oldValue, _scan, true);
            }
        }

        public Sequence Sequence
        {
            get { return _sequence; }
            set
            {
                var oldValue = _sequence;
                _sequence = value;
                _fragmentLabelCache.Clear();
                _precursorLabelCache.Clear();
                UpdatePrecursorLabels();
                RaisePropertyChanged("Sequence", oldValue, _sequence, true);
            }
        }

        public string SequenceText
        {
            get { return _sequenceText; }
            set
            {
                var oldValue = _sequenceText;
                _sequenceText = value;
                RaisePropertyChanged("SequenceText", oldValue, _sequenceText, true);
            }
        }

        public int Charge
        {
            get { return _charge; }
            set
            {
                var oldValue = _charge;
                _charge = value;
                RaisePropertyChanged("Charge", oldValue, _charge, true);
            }
        }

        public bool Heavy
        {
            get { return _heavy; }
            set
            {
                var oldValue = _heavy;
                _heavy = value;
                PrecursorMz = _heavy ? _heavyPrecursorMz : _noLabelPrecursorMz;
                RaisePropertyChanged("Heavy", oldValue, _heavy, true);
            }
        }

        public double QValue
        {
            get { return _qValue; }
            set
            {
                var oldValue = _qValue;
                _qValue = value;
                RaisePropertyChanged("QValue", oldValue, _qValue, true);
            }
        }

        public double Score
        {
            get { return _score; }
            set
            {
                var oldValue = _score;
                _score = value;
                RaisePropertyChanged("Score", oldValue, _score, true);
            }
        }

        public double PrecursorMz
        {
            get { return _precursorMz; }
            set
            {
                var oldValue = _precursorMz;
                _precursorMz = value;
                RaisePropertyChanged("PrecursorMz", oldValue, _precursorMz, true);
            }
        }

    #endregion

        public List<LabeledIonViewModel> FragmentLabels
        {
            get { return _fragmentLabels; }
            private set
            {
                var oldValue = _fragmentLabels;
                _fragmentLabels = value;
                RaisePropertyChanged("FragmentLabels", oldValue, _fragmentLabels, true);
            }
        }

        public List<LabeledIonViewModel> PrecursorLabels
        {
            get { return _precursorLabels; }
            private set
            {
                var oldValue = _precursorLabels;
                _precursorLabels = value;
                RaisePropertyChanged("PrecursorLabels", oldValue, _precursorLabels, true);
            }
        }

        public Task<List<LabeledIonViewModel>> GetLightFragmentIons()
        {
            return Task.Run(() => GenerateFragmentLabels(PrSm.LightSequence, false));
        }

        public Task<List<LabeledIonViewModel>> GetHeavyFragmentIons()
        {
            return Task.Run(() => GenerateFragmentLabels(PrSm.HeavySequence, true));
        }

        public Task<List<LabeledIonViewModel>> GetLightPrecursorIons()
        {
            return Task.Run(() => GeneratePrecursorLabels(PrSm.LightSequence));
        }

        public Task<List<LabeledIonViewModel>> GetHeavyPrecursorIons()
        {
            return Task.Run(() => GeneratePrecursorLabels(PrSm.HeavySequence));
        }

        public void Clear()
        {
        }

        private void  UpdateFragmentLabels()
        {
            /*if (FragmentLabelUpdate != null && !FragmentLabelUpdate.IsCompleted) await FragmentLabelUpdate;
            FragmentLabelUpdate = Task.Run(() => GenerateFragmentLabels(Sequence, false));
            FragmentLabels = await FragmentLabelUpdate; */
            FragmentLabels = GenerateFragmentLabels(Sequence, false);
        }

        private List<LabeledIonViewModel> GenerateFragmentLabels(Sequence sequence, bool heavy)
        {
            var fragmentLabels = new List<LabeledIonViewModel>();
            if (sequence.Count < 1) return fragmentLabels;
            foreach (var ionType in _ionTypes)
            {
                for (int i = 1; i <= sequence.Count; i++)
                {
                    LabeledIonViewModel label;
                    var key = new Tuple<string, bool>(ionType.GetName(i), heavy);
                    if (!_fragmentLabelCache.TryGetValue(key, out label))
                    {
                        var composition = ionType.IsPrefixIon
                            ? PrSm.Sequence.GetComposition(0, i)
                            : PrSm.Sequence.GetComposition(i, PrSm.Sequence.Count);
                        label = new LabeledIonViewModel(new LabeledIon(composition, i, ionType, true, IonUtils.GetPrecursorIon(sequence, PrSm.Charge)));
                        _fragmentLabelCache.Add(key, label);
                    }
                    fragmentLabels.Add(label);
                }
            }
            return fragmentLabels;
        }

        private void UpdatePrecursorLabels()
        {
            /*if (PrecursorLabelUpdate != null && !PrecursorLabelUpdate.IsCompleted) await PrecursorLabelUpdate;
            PrecursorLabelUpdate = Task.Run(() => GeneratePrecursorLabels(Sequence));
            PrecursorLabels = await PrecursorLabelUpdate; */
            PrecursorLabels = GeneratePrecursorLabels(Sequence);
        }

        private List<LabeledIonViewModel> GeneratePrecursorLabels(Sequence sequence)
        {
            var ions = new List<LabeledIonViewModel>();
            if (sequence.Count == 0) return ions;
            for (int i = Constants.MinIsotopeIndex; i <= Constants.MaxIsotopeIndex; i++)
            {
                var precursorIonType = new IonType("Precursor", Composition.H2O, Charge, false);
                var composition = sequence.Aggregate(Composition.Zero, (current, aa) => current + aa.Composition);
                var ion = new LabeledIon(composition, i, precursorIonType, false);
                ions.Add(new LabeledIonViewModel(ion));
            }
            return ions;
        }

        private void IonTypesChanged(PropertyChangedMessage<List<IonType>> message)
        {
            _ionTypes = message.NewValue;
            UpdateFragmentLabels();
        }

        private static SelectedPrSmViewModel _instance;
        private List<IonType> _ionTypes; 
        private readonly Dictionary<Tuple<string, bool>, LabeledIonViewModel> _fragmentLabelCache;
        private readonly Dictionary<Tuple<string, bool>, LabeledIonViewModel> _precursorLabelCache;
        private List<LabeledIonViewModel> _fragmentLabels;
        private List<LabeledIonViewModel> _precursorLabels;

        private Sequence _sequence;
        private string _sequenceText;
        private int _charge;
        private bool _heavy;
        private int _scan;
        private ILcMsRun _lcms;
        private string _rawFileName;
        private double _qValue;
        private double _score;
        private double _precursorMz;
        private double _noLabelPrecursorMz;
        private double _heavyPrecursorMz;
    }
}
