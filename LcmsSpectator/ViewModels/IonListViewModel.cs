using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.DialogServices;
using LcmsSpectator.TaskServices;
using LcmsSpectatorModels.Models;
using LcmsSpectatorModels.Utils;

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

            _fragmentLabelCache = new Dictionary<Tuple<string, bool>, LabeledIonViewModel>();
            _precursorLabelCache = new Dictionary<Tuple<string, bool>, LabeledIonViewModel>();

            _currentSequence = new Sequence(new List<AminoAcid>());

            Messenger.Default.Register<PropertyChangedMessage<List<IonType>>>(this, SelectedIonTypesChanged);
            Messenger.Default.Register<PropertyChangedMessage<int>>(this, SelectedChargeChanged);
            Messenger.Default.Register<PropertyChangedMessage<Sequence>>(this, SelectedSequenceChanged);

            FragmentLabelUpdate = Task.Run(() => GenerateFragmentLabels(_currentSequence, false));
            PrecursorLabelUpdate = Task.Run(() => GeneratePrecursorLabels(_currentSequence));
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
            if (FragmentLabelUpdate != null && !FragmentLabelUpdate.IsCompleted) await FragmentLabelUpdate;
            FragmentLabelUpdate = Task.Run(() => GenerateFragmentLabels(SelectedPrSmViewModel.Instance.Sequence, false));
            FragmentLabels = await FragmentLabelUpdate;
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

        private async void UpdatePrecursorLabels()
        {
            if (PrecursorLabelUpdate != null && !PrecursorLabelUpdate.IsCompleted) await PrecursorLabelUpdate;
            PrecursorLabelUpdate = Task.Run(() => GeneratePrecursorLabels(SelectedPrSmViewModel.Instance.Sequence));
            PrecursorLabels = await PrecursorLabelUpdate;
        }

        private List<LabeledIonViewModel> GeneratePrecursorLabels(Sequence sequence)
        {
            var ions = new List<LabeledIonViewModel>();
            if (sequence.Count == 0) return ions;
            for (int i = Constants.MinIsotopeIndex; i <= Constants.MaxIsotopeIndex; i++)
            {
                var precursorIonType = new IonType("Precursor", Composition.H2O, SelectedPrSmViewModel.Instance.Charge, false);
                var composition = sequence.Aggregate(Composition.Zero, (current, aa) => current + aa.Composition);
                var ion = new LabeledIon(composition, i, precursorIonType, false);
                ions.Add(new LabeledIonViewModel(ion));
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
            return Task.Run(() => GeneratePrecursorLabels(SelectedPrSmViewModel.Instance.PrSm.LightSequence));
        }

        public Task<List<LabeledIonViewModel>> GetHeavyPrecursorIons()
        {
            return Task.Run(() => GeneratePrecursorLabels(SelectedPrSmViewModel.Instance.PrSm.HeavySequence));
        }

        private IMainDialogService _dialogService;
        private ITaskService _taskService;

        private int _currentCharge;
        private Sequence _currentSequence;

        private List<IonType> _ionTypes;
        private readonly Dictionary<Tuple<string, bool>, LabeledIonViewModel> _fragmentLabelCache;
        private readonly Dictionary<Tuple<string, bool>, LabeledIonViewModel> _precursorLabelCache;
        private List<LabeledIonViewModel> _fragmentLabels;
        private List<LabeledIonViewModel> _precursorLabels;
    }
}
