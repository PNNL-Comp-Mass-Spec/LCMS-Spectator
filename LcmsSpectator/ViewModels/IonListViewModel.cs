using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.DialogServices;
using LcmsSpectator.TaskServices;
using LcmsSpectatorModels.Config;
using LcmsSpectatorModels.Models;
using LcmsSpectatorModels.Utils;
using Constants = LcmsSpectatorModels.Utils.Constants;

namespace LcmsSpectator.ViewModels
{
    public class IonListViewModel: ViewModelBase
    {
        public Task<List<LabeledIonViewModel>> FragmentLabelUpdate { get; private set; }
        public Task<List<LabeledIonViewModel>> PrecursorLabelUpdate { get; private set; }

        public IonListViewModel(IMainDialogService dialogService, ITaskService taskService)
        {
            _dialogService = dialogService;
            _taskService = taskService;
            _fragmentTaskService = TaskServiceFactory.GetTaskServiceLike(_taskService);
            _precursorTaskService = TaskServiceFactory.GetTaskServiceLike(_taskService);

            _fragmentLabelCache = new Dictionary<Tuple<string, bool>, LabeledIonViewModel>();
            _precursorLabelCache = new Dictionary<Tuple<string, bool>, LabeledIonViewModel>();

            _currentSequence = new Sequence(new List<AminoAcid>());

            Messenger.Default.Register<PropertyChangedMessage<List<IonType>>>(this, SelectedIonTypesChanged);
            Messenger.Default.Register<PropertyChangedMessage<int>>(this, SelectedChargeChanged);
            Messenger.Default.Register<PropertyChangedMessage<Sequence>>(this, SelectedSequenceChanged);
            Messenger.Default.Register<SettingsChangedNotification>(this, SettingsChanged);

            FragmentLabelUpdate = Task.Run(() => GenerateFragmentLabels(_currentSequence, false));
            PrecursorLabelUpdate = Task.Run(() => GenerateIsotopePrecursorLabels(_currentSequence));
        }

        public string PrecursorIonType
        {
            get { return "Precursor Isotopes";  }
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

        private async void UpdateFragmentLabels()
        {
            _precursorTaskService.Enqueue(() => FragmentLabels = GenerateFragmentLabels(SelectedPrSmViewModel.Instance.Sequence, false));
            /*if (FragmentLabelUpdate != null && !FragmentLabelUpdate.IsCompleted) await FragmentLabelUpdate;
            FragmentLabelUpdate = Task.Run(() => GenerateFragmentLabels(SelectedPrSmViewModel.Instance.Sequence, false));
            FragmentLabels = await FragmentLabelUpdate; */
        }

        private List<LabeledIonViewModel> GenerateFragmentLabels(Sequence sequence, bool heavy, bool useCache = true)
        {
            var fragmentLabels = new List<LabeledIonViewModel>();
            if (sequence.Count < 1) return fragmentLabels;
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
                            ? SelectedPrSmViewModel.Instance.Sequence.GetComposition(0, i)
                            : SelectedPrSmViewModel.Instance.Sequence.GetComposition(i, SelectedPrSmViewModel.Instance.Sequence.Count);
                        var labelIndex = ionType.IsPrefixIon ? i : (SelectedPrSmViewModel.Instance.Sequence.Count - i);
                        label = new LabeledIonViewModel(new LabeledIon(composition, labelIndex, ionType, true, IonUtils.GetPrecursorIon(sequence, SelectedPrSmViewModel.Instance.Charge)));
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
                PrecursorLabels = (IcParameters.Instance.PrecursorViewMode == PrecursorViewMode.Charges)
                    ? GenerateChargePrecursorLabels(SelectedPrSmViewModel.Instance.Sequence)
                    : GenerateIsotopePrecursorLabels(SelectedPrSmViewModel.Instance.Sequence);
            });
            /*if (PrecursorLabelUpdate != null && !PrecursorLabelUpdate.IsCompleted) await PrecursorLabelUpdate;
            PrecursorLabelUpdate = Task.Run(() => GenerateChargePrecursorLabels(SelectedPrSmViewModel.Instance.Sequence));
            PrecursorLabels = await PrecursorLabelUpdate; */
        }

        private List<LabeledIonViewModel> GenerateIsotopePrecursorLabels(Sequence sequence)
        {
            var ions = new List<LabeledIonViewModel>();
            if (sequence.Count == 0) return ions;
            var precursorIonType = new IonType("Precursor", Composition.H2O, SelectedPrSmViewModel.Instance.Charge, false);
            var composition = sequence.Aggregate(Composition.Zero, (current, aa) => current + aa.Composition);
            var relativeIntensities = composition.GetIsotopomerEnvelope();
            var indices = new List<int> { -1 };
            for (int i = 0; i < relativeIntensities.Length; i++)
            {
                if (relativeIntensities[i] >= IcParameters.Instance.PrecursorRelativeIntensityThreshold || i == 0)
                    indices.Add(i);
            }
            ions.AddRange(indices.Select(index => new LabeledIon(composition, index, precursorIonType, false)).Select(ionLabel => new LabeledIonViewModel(ionLabel)));
            return ions;
        }

        private List<LabeledIonViewModel> GenerateChargePrecursorLabels(Sequence sequence)
        {
            var ions = new List<LabeledIonViewModel>();

            if (sequence.Count == 0) return ions;
            var composition = sequence.Aggregate(Composition.Zero, (current, aa) => current + aa.Composition);
            var minCharge = Math.Max(1, SelectedPrSmViewModel.Instance.Charge - 3);
            var maxCharge = SelectedPrSmViewModel.Instance.Charge + 3;

            for (int i = minCharge; i <= maxCharge; i++)
            {
                var precursorIonType = new IonType("Precursor", Composition.H2O, i, false);
                var labeledIon = new LabeledIon(composition, i-minCharge, precursorIonType, false, null, true);
                ions.Add(new LabeledIonViewModel(labeledIon));
            }
            return ions;
        }

        private void SelectedIonTypesChanged(PropertyChangedMessage<List<IonType>> message)
        {
            _ionTypes = message.NewValue;
            UpdateFragmentLabels();
        }

        private void SelectedChargeChanged(PropertyChangedMessage<int> message)
        {
            if (message.PropertyName != "Charge") return;
            _currentCharge = message.NewValue;
        }

        private void SelectedSequenceChanged(PropertyChangedMessage<Sequence> message)
        {
            _currentSequence = message.NewValue;
            _fragmentLabelCache.Clear();
            _precursorLabelCache.Clear();
            UpdatePrecursorLabels();
        }

        public Task<List<LabeledIonViewModel>> GetLightFragmentIons()
        {
            return Task.Run(() => GenerateFragmentLabels(SelectedPrSmViewModel.Instance.PrSm.LightSequence, false, false));
        }

        public Task<List<LabeledIonViewModel>> GetHeavyFragmentIons()
        {
            return Task.Run(() => GenerateFragmentLabels(SelectedPrSmViewModel.Instance.PrSm.HeavySequence, true, false));
        }

        public Task<List<LabeledIonViewModel>> GetLightPrecursorIons()
        {
            return Task.Run(() => GenerateIsotopePrecursorLabels(SelectedPrSmViewModel.Instance.PrSm.LightSequence));
        }

        public Task<List<LabeledIonViewModel>> GetHeavyPrecursorIons()
        {
            return Task.Run(() => GenerateIsotopePrecursorLabels(SelectedPrSmViewModel.Instance.PrSm.HeavySequence));
        }

        private void SettingsChanged(SettingsChangedNotification notification)
        {
            UpdatePrecursorLabels();
        }

        private IMainDialogService _dialogService;
        private ITaskService _taskService;
        private ITaskService _precursorTaskService;
        private ITaskService _fragmentTaskService;

        private int _currentCharge;
        private Sequence _currentSequence;

        private List<IonType> _ionTypes;
        private readonly Dictionary<Tuple<string, bool>, LabeledIonViewModel> _fragmentLabelCache;
        private readonly Dictionary<Tuple<string, bool>, LabeledIonViewModel> _precursorLabelCache;
        private List<LabeledIonViewModel> _fragmentLabels;
        private List<LabeledIonViewModel> _precursorLabels;
    }
}
