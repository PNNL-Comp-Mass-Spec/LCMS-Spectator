using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.DialogServices;
using LcmsSpectator.TaskServices;
using LcmsSpectatorModels.Config;
using LcmsSpectatorModels.Models;
using LcmsSpectatorModels.Utils;

namespace LcmsSpectator.ViewModels
{
    public class IonListViewModel: ViewModelBase
    {
        public Task<List<LabeledIonViewModel>> FragmentLabelUpdate { get; private set; }
        public Task<List<LabeledIonViewModel>> PrecursorLabelUpdate { get; private set; }

        public ObservableCollection<PrecursorViewMode> PrecursorViewModes { get; private set; }
        
        public IonListViewModel(IMainDialogService dialogService, ITaskService taskService, Messenger messenger)
        {
            MessengerInstance = messenger;
            _dialogService = dialogService;
            ITaskService taskService1 = taskService;
            _fragmentTaskService = TaskServiceFactory.GetTaskServiceLike(taskService1);
            _precursorTaskService = TaskServiceFactory.GetTaskServiceLike(taskService1);

            _fragmentLabelCache = new Dictionary<Tuple<string, bool>, LabeledIonViewModel>();
            _precursorLabelCache = new Dictionary<Tuple<string, bool>, LabeledIonViewModel>();

            _showHeavyCount = 0;

            _currentSequence = new Sequence(new List<AminoAcid>());

            PrecursorViewModes = new ObservableCollection<PrecursorViewMode>
            {
                PrecursorViewMode.Isotopes,
                PrecursorViewMode.Charges
            };
            _precursorViewMode = PrecursorViewMode.Isotopes;

            MessengerInstance.Register<PropertyChangedMessage<bool>>(this, ShowHeavyChanged);
            MessengerInstance.Register<PropertyChangedMessage<List<IonType>>>(this, SelectedIonTypesChanged);
            MessengerInstance.Register<PropertyChangedMessage<int>>(this, SelectedChargeChanged);
            MessengerInstance.Register<PropertyChangedMessage<Sequence>>(this, SelectedSequenceChanged);
            Messenger.Default.Register<SettingsChangedNotification>(this, SettingsChanged);

            FragmentLabelUpdate = Task.Run(() => GenerateFragmentLabels(_currentSequence, false));
            PrecursorLabelUpdate = Task.Run(() => GenerateIsotopePrecursorLabels(_currentSequence));
        }

        #region Ion label properties

        public PrecursorViewMode PrecursorViewMode
        {
            get { return _precursorViewMode; }
            set
            {
                _precursorViewMode = value;
                UpdateFragmentLabels();
                UpdatePrecursorLabels();
                if (_showHeavyCount > 0)
                {
                    UpdateLightFragmentLabels();
                    UpdateHeavyFragmentLabels();
                    UpdateLightPrecursorLabels();
                    UpdateHeavyPrecursorLabels();
                }
                RaisePropertyChanged();
            }
        }

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

        public List<LabeledIonViewModel> LightFragmentLabels
        {
            get { return _lightFragmentLabels; }
            set
            {
                var oldValue = _lightFragmentLabels;
                _lightFragmentLabels = value;
                RaisePropertyChanged("LightFragmentLabels", oldValue, _lightFragmentLabels, true);
            }
        }

        public List<LabeledIonViewModel> HeavyFragmentLabels
        {
            get { return _heavyFragmentLabels; }
            set
            {
                var oldValue = _heavyFragmentLabels;
                _heavyFragmentLabels = value;
                RaisePropertyChanged("HeavyFragmentLabels", oldValue, _heavyFragmentLabels, true);
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

        public List<LabeledIonViewModel> LightPrecursorLabels
        {
            get { return _lightPrecursorLabels; }
            set
            {
                var oldValue = _lightPrecursorLabels;
                _lightPrecursorLabels = value;
                RaisePropertyChanged("LightPrecursorLabels", oldValue, _lightPrecursorLabels, true);
            }
        }

        public List<LabeledIonViewModel> HeavyPrecursorLabels
        {
            get { return _heavyPrecursorLabels; }
            set
            {
                var oldValue = _heavyPrecursorLabels;
                _heavyPrecursorLabels = value;
                RaisePropertyChanged("HeavyPrecursorLabels", oldValue, _heavyPrecursorLabels, true);
            }
        }
        #endregion

        private void UpdateFragmentLabels()
        {
            _fragmentTaskService.Enqueue(() => FragmentLabels = GenerateFragmentLabels(_currentSequence, false));
        }

        private void UpdateLightFragmentLabels()
        {
            _fragmentTaskService.Enqueue(() => LightFragmentLabels = GenerateFragmentLabels(IonUtils.GetHeavySequence(_currentSequence, IcParameters.Instance.LightModifications), true, false));
        }

        private void UpdateHeavyFragmentLabels()
        {
            _fragmentTaskService.Enqueue(() => HeavyFragmentLabels = GenerateFragmentLabels(IonUtils.GetHeavySequence(_currentSequence, IcParameters.Instance.HeavyModifications), true, false));
        }

        private List<LabeledIonViewModel> GenerateFragmentLabels(Sequence sequence, bool heavy, bool useCache = true)
        {
            var fragmentLabels = new List<LabeledIonViewModel>();
            if (sequence.Count < 1) return fragmentLabels;
            var precursorIon = IonUtils.GetPrecursorIon(sequence, _currentCharge);
            foreach (var ionType in _ionTypes)
            {
                var ionFragments = new List<LabeledIonViewModel>();
                for (int i = 1; i < sequence.Count; i++)
                {
                    LabeledIonViewModel label;
                    var key = new Tuple<string, bool>(ionType.GetName(i), heavy);
                    if (!(useCache && _fragmentLabelCache.TryGetValue(key, out label)))
                    {
                        var composition = ionType.IsPrefixIon
                            ? sequence.GetComposition(0, i)
                            : sequence.GetComposition(i, sequence.Count);
                        var labelIndex = ionType.IsPrefixIon ? i : (sequence.Count - i);
                        label = new LabeledIonViewModel(new LabeledIon(composition, labelIndex, ionType, true, precursorIon));
                        if (useCache) _fragmentLabelCache.Add(key, label);
                    }
                    ionFragments.Add(label);
                }
                if (!ionType.IsPrefixIon) ionFragments.Reverse();
                fragmentLabels.AddRange(ionFragments);
            }
            return fragmentLabels;
        }

        private void UpdatePrecursorLabels()
        {
            _precursorTaskService.Enqueue(() =>
            {
                PrecursorLabels = (_precursorViewMode == PrecursorViewMode.Charges)
                    ? GenerateChargePrecursorLabels(_currentSequence)
                    : GenerateIsotopePrecursorLabels(_currentSequence);
            });
        }

        private void UpdateLightPrecursorLabels()
        {
            _precursorTaskService.Enqueue(() =>
            {
                var lightSequence = IonUtils.GetHeavySequence(_currentSequence, IcParameters.Instance.LightModifications);
                LightPrecursorLabels = (_precursorViewMode == PrecursorViewMode.Charges)
                    ? GenerateChargePrecursorLabels(lightSequence)
                    : GenerateIsotopePrecursorLabels(lightSequence);
            });
        }

        private void UpdateHeavyPrecursorLabels()
        {
            _precursorTaskService.Enqueue(() =>
            {
                var heavySequence = IonUtils.GetHeavySequence(_currentSequence, IcParameters.Instance.HeavyModifications);
                HeavyPrecursorLabels = (_precursorViewMode == PrecursorViewMode.Charges)
                    ? GenerateChargePrecursorLabels(heavySequence)
                    : GenerateIsotopePrecursorLabels(heavySequence);
            });
        }

        private List<LabeledIonViewModel> GenerateIsotopePrecursorLabels(Sequence sequence)
        {
            var ions = new List<LabeledIonViewModel>();
            if (sequence.Count == 0) return ions;
            var precursorIonType = new IonType("Precursor", Composition.H2O, _currentCharge, false);
            var composition = sequence.Aggregate(Composition.Zero, (current, aa) => current + aa.Composition);
            var relativeIntensities = composition.GetIsotopomerEnvelope();
            var indices = new List<int> { -1 };
            for (int i = 0; i < relativeIntensities.Envolope.Length; i++)
            {
                if (relativeIntensities.Envolope[i] >= IcParameters.Instance.PrecursorRelativeIntensityThreshold || i == 0)
                    indices.Add(i);
            }
            ions.AddRange(indices.Select(index => new LabeledIon(composition, index, precursorIonType, false)).Select(ionLabel => new LabeledIonViewModel(ionLabel, MessengerInstance)));
            return ions;
        }

        private List<LabeledIonViewModel> GenerateChargePrecursorLabels(Sequence sequence)
        {
            var ions = new List<LabeledIonViewModel>();
            var charge = _currentCharge;
            var numChargeStates = IonUtils.GetNumNeighboringChargeStates(charge);
            if (sequence.Count == 0) return ions;
            var composition = sequence.Aggregate(Composition.Zero, (current, aa) => current + aa.Composition);
            var minCharge = Math.Max(1, charge - numChargeStates);
            var maxCharge = charge + numChargeStates;

            for (int i = minCharge; i <= maxCharge; i++)
            {
                var index = i - minCharge;
                if (index == 0) index = charge - minCharge;
                if (i == charge) index = 0;         // guarantee that actual charge is index 0
                var precursorIonType = new IonType("Precursor", Composition.H2O, i, false);
                var labeledIon = new LabeledIon(composition, index, precursorIonType, false, null, true);
                ions.Add(new LabeledIonViewModel(labeledIon, MessengerInstance));
            }
            return ions;
        }

        private void SelectedIonTypesChanged(PropertyChangedMessage<List<IonType>> message)
        {
            _ionTypes = message.NewValue;
            UpdateFragmentLabels();
            if (_showHeavyCount > 0)
            {
                UpdateLightFragmentLabels();
                UpdateHeavyFragmentLabels();
            }
        }

        private void SelectedChargeChanged(PropertyChangedMessage<int> message)
        {
            if (message.PropertyName != "Charge") return;
            _currentCharge = message.NewValue;
            UpdatePrecursorLabels();
            if (_showHeavyCount > 0)
            {
                UpdateLightPrecursorLabels();
                UpdateHeavyPrecursorLabels();
            }
        }

        private void SelectedSequenceChanged(PropertyChangedMessage<Sequence> message)
        {
            if (message.PropertyName != "Sequence") return;
            _currentSequence = message.NewValue;
            _fragmentLabelCache.Clear();
            _precursorLabelCache.Clear();
        }

        private void ShowHeavyChanged(PropertyChangedMessage<bool> message)
        {
            if (message.PropertyName != "ShowHeavy") return;
            if (message.NewValue)
            {
                if (_showHeavyCount == 0)
                {
                    UpdateLightFragmentLabels();
                    UpdateHeavyFragmentLabels();
                    UpdateLightPrecursorLabels();
                    UpdateHeavyPrecursorLabels();
                }
                _showHeavyCount++;
            }
            else if (_showHeavyCount > 0)
            {
                _showHeavyCount--;
            }
        }

        private void SettingsChanged(SettingsChangedNotification notification)
        {
            UpdateFragmentLabels();
            UpdatePrecursorLabels();
            if (_showHeavyCount > 0)
            {
                UpdateLightFragmentLabels();
                UpdateHeavyFragmentLabels();
                UpdateLightPrecursorLabels();
                UpdateHeavyPrecursorLabels();
            }
        }

        private IMainDialogService _dialogService;
        private readonly ITaskService _precursorTaskService;
        private readonly ITaskService _fragmentTaskService;

        private int _currentCharge;
        private Sequence _currentSequence;

        private int _showHeavyCount;

        private List<IonType> _ionTypes;
        private readonly Dictionary<Tuple<string, bool>, LabeledIonViewModel> _fragmentLabelCache;
        private readonly Dictionary<Tuple<string, bool>, LabeledIonViewModel> _precursorLabelCache;
        private List<LabeledIonViewModel> _fragmentLabels;
        private List<LabeledIonViewModel> _precursorLabels;
        private List<LabeledIonViewModel> _lightFragmentLabels;
        private List<LabeledIonViewModel> _heavyFragmentLabels;
        private List<LabeledIonViewModel> _lightPrecursorLabels;
        private List<LabeledIonViewModel> _heavyPrecursorLabels;
        private PrecursorViewMode _precursorViewMode;
    }
}
