using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.Config;
using LcmsSpectator.Models;
using LcmsSpectator.Utils;
using ReactiveUI;
using Splat;

namespace LcmsSpectator.ViewModels
{
    public class IonListViewModel: ReactiveObject
    {
        public IonListViewModel(ILcMsRun lcms)
        {
            _prefixCompositionCache = new MemoizingMRUCache<Sequence, Composition[]>(GetPrefixCompositions, 20);
            _suffixCompositionCache = new MemoizingMRUCache<Sequence, Composition[]>(GetSuffixCompositions, 20);
            _lcms = lcms;
            _fragmentCache = new MemoizingMRUCache<Tuple<Composition, IonType>, LabeledIonViewModel>(GetLabeledIonViewModel, 1000);
            _cacheLock = new object();
            IonTypes = new ReactiveList<IonType>();
            ShowHeavy = false;

            FragmentLabels = new ReactiveList<LabeledIonViewModel>();
            HeavyFragmentLabels = new ReactiveList<LabeledIonViewModel>();
            PrecursorLabels = new ReactiveList<LabeledIonViewModel>();
            HeavyPrecursorLabels = new ReactiveList<LabeledIonViewModel>();
            ChargePrecursorLabels = new ReactiveList<LabeledIonViewModel>();
            IsotopePrecursorLabels = new ReactiveList<LabeledIonViewModel>();

            var precursorObservable = this.WhenAnyValue(x => x.SelectedPrSm, x => x.ShowHeavy).Where(x => x.Item1 != null);
            precursorObservable.Select(x => x.Item2 ? IcParameters.Instance.LightModifications : null)
                               .SelectMany(async mods => await GenerateChargeLabelsAsync(mods))
                               .Subscribe(labels => ChargePrecursorLabels = labels);
            precursorObservable.Select(x => x.Item2 ? IcParameters.Instance.LightModifications : null)
                               .SelectMany(async mods => await GenerateIsotopeLabelsAsync(mods))
                               .Subscribe(labels =>{ IsotopePrecursorLabels.ChangeTrackingEnabled = false; IsotopePrecursorLabels = labels; });
            precursorObservable.Where(x => x.Item2)
                .SelectMany(async x => PrecursorViewMode == PrecursorViewMode.Isotopes ? await GenerateIsotopeLabelsAsync(IcParameters.Instance.HeavyModifications) 
                                                                             : await GenerateChargeLabelsAsync(IcParameters.Instance.HeavyModifications))
                .Subscribe(labels => {HeavyPrecursorLabels.ChangeTrackingEnabled = false; HeavyPrecursorLabels = labels;});
            IcParameters.Instance.WhenAnyValue(x => x.PrecursorRelativeIntensityThreshold, x => x.LightModifications)
                .Where(_ => SelectedPrSm != null).Select(x => ShowHeavy ? x.Item2 : null)
                .SelectMany(async mods => await GenerateChargeLabelsAsync(mods))
                .Subscribe(labels => {ChargePrecursorLabels.ChangeTrackingEnabled = false; ChargePrecursorLabels = labels;});
            IcParameters.Instance.WhenAnyValue(x => x.PrecursorRelativeIntensityThreshold, x => x.LightModifications)
                        .Where(_ => SelectedPrSm != null).Select(x => ShowHeavy ? x.Item2 : null)
                        .SelectMany(async mods => await GenerateIsotopeLabelsAsync(mods))
                        .Subscribe(labels => {IsotopePrecursorLabels.ChangeTrackingEnabled = false; IsotopePrecursorLabels = labels;});
            IcParameters.Instance.WhenAnyValue(x => x.PrecursorRelativeIntensityThreshold, x => x.HeavyModifications)
                        .Where(_ => SelectedPrSm != null && ShowHeavy)
                        .SelectMany(async x => PrecursorViewMode == PrecursorViewMode.Isotopes ? await GenerateIsotopeLabelsAsync(x.Item2)
                                                                                               : await GenerateChargeLabelsAsync(x.Item2))
                        .Subscribe(labels => { HeavyPrecursorLabels.ChangeTrackingEnabled = false; HeavyPrecursorLabels = labels; });
            this.WhenAnyValue(x => x.IsotopePrecursorLabels, x => x.ChargePrecursorLabels, x => x.PrecursorViewMode)
                .Select(x => x.Item3 == PrecursorViewMode.Isotopes ? x.Item1 : x.Item2)
                .Subscribe(labels => PrecursorLabels = labels);

            var fragmentObservable = this.WhenAnyValue(x => x.SelectedPrSm, x => x.IonTypes, x => x.ShowHeavy)
                .Where(x => x.Item1 != null)
                .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler);
            fragmentObservable.Select(x => x.Item3 ? IcParameters.Instance.LightModifications : null)
                .SelectMany(async mods => await GenerateFragmentLabelsAsync(mods))
                .Subscribe(labels => FragmentLabels = labels);
            fragmentObservable.Where(x => x.Item3)
                .SelectMany(async mods => await GenerateFragmentLabelsAsync(IcParameters.Instance.HeavyModifications))
                .Subscribe(labels =>{ HeavyFragmentLabels.ChangeTrackingEnabled = false; HeavyFragmentLabels = labels;});
            IcParameters.Instance.WhenAnyValue(x => x.LightModifications)
                .Where(_ => SelectedPrSm != null).Select(mods => ShowHeavy ? mods : null)
                .SelectMany(async mods => await GenerateFragmentLabelsAsync(mods))
                .Subscribe(labels => FragmentLabels = labels);
            IcParameters.Instance.WhenAnyValue(x => x.HeavyModifications)
                .Where(_ => SelectedPrSm != null && ShowHeavy)
                .SelectMany(async mods => await GenerateFragmentLabelsAsync(mods))
                .Subscribe(labels => {HeavyFragmentLabels.ChangeTrackingEnabled = false;HeavyFragmentLabels = labels;});

            EnableFragmentRowVirtualization = true;
            EnablePrecursorRowVirtualization = false;
        }

        private ReactiveList<LabeledIonViewModel> _fragmentLabels; 
        public ReactiveList<LabeledIonViewModel> FragmentLabels
        {
            get { return _fragmentLabels; }
            private set { this.RaiseAndSetIfChanged(ref _fragmentLabels, value); }
        }

        private ReactiveList<LabeledIonViewModel> _heavyFragmentLabels; 
        public ReactiveList<LabeledIonViewModel> HeavyFragmentLabels
        {
            get { return _heavyFragmentLabels; }
            private set { this.RaiseAndSetIfChanged(ref _heavyFragmentLabels, value); }
        }

        private ReactiveList<LabeledIonViewModel> _precursorLabels; 
        public ReactiveList<LabeledIonViewModel> PrecursorLabels
        {
            get { return _precursorLabels; }
            private set { this.RaiseAndSetIfChanged(ref _precursorLabels, value); }
        }

        private ReactiveList<LabeledIonViewModel> _isotopePrecursorLabels;

        public ReactiveList<LabeledIonViewModel> IsotopePrecursorLabels
        {
            get { return _isotopePrecursorLabels; }
            set { this.RaiseAndSetIfChanged(ref _isotopePrecursorLabels, value); }
        }

        private ReactiveList<LabeledIonViewModel> _chargePrecursorLabels;
        public ReactiveList<LabeledIonViewModel> ChargePrecursorLabels
        {
            get { return _chargePrecursorLabels; }
            set { this.RaiseAndSetIfChanged(ref _chargePrecursorLabels, value); }
        }

        private ReactiveList<LabeledIonViewModel> _heavyPrecursorLabels; 
        public ReactiveList<LabeledIonViewModel> HeavyPrecursorLabels
        {
            get { return _heavyPrecursorLabels; }
            private set { this.RaiseAndSetIfChanged(ref _heavyPrecursorLabels, value); }
        }

        private bool _showHeavy;
        public bool ShowHeavy
        {
            get { return _showHeavy; }
            set { this.RaiseAndSetIfChanged(ref _showHeavy, value); }
        }

        private PrSm _selectedPrSm;
        public PrSm SelectedPrSm
        {
            get { return _selectedPrSm; }
            set { this.RaiseAndSetIfChanged(ref _selectedPrSm, value); }
        }

        private ReactiveList<IonType> _ionTypes; 
        public ReactiveList<IonType> IonTypes
        {
            get { return _ionTypes; }
            set { this.RaiseAndSetIfChanged(ref _ionTypes, value); }
        }

        private PrecursorViewMode _precursorViewMode;
        public PrecursorViewMode PrecursorViewMode
        {
            get { return _precursorViewMode; }
            set { this.RaiseAndSetIfChanged(ref _precursorViewMode, value); }
        }

        private bool _enableFragmentRowVirtualization;
        public bool EnableFragmentRowVirtualization
        {
            get { return _enableFragmentRowVirtualization; }
            set { this.RaiseAndSetIfChanged(ref _enableFragmentRowVirtualization, value); }
        }

        private bool _enablePrecursorRowVirtualization;
        public bool EnablePrecursorRowVirtualization
        {
            get { return _enablePrecursorRowVirtualization; }
            set { this.RaiseAndSetIfChanged(ref _enablePrecursorRowVirtualization, value); }
        }

        private Task<ReactiveList<LabeledIonViewModel>> GenerateFragmentLabelsAsync(ReactiveList<ModificationViewModel> labelModifications = null)
        {
            return Task.Run(() => GenerateFragmentLabels(labelModifications));
        }

        private Task<ReactiveList<LabeledIonViewModel>> GenerateIsotopeLabelsAsync(
            ReactiveList<ModificationViewModel> labelModifications = null)
        {
            return Task.Run(() => GenerateIsotopePrecursorLabels(labelModifications));
        }

        private Task<ReactiveList<LabeledIonViewModel>> GenerateChargeLabelsAsync(
            ReactiveList<ModificationViewModel> labelModifications = null)
        {
            return Task.Run(() => GenerateChargePrecursorLabels(labelModifications));
        }

        private ReactiveList<LabeledIonViewModel> GenerateFragmentLabels(ReactiveList<ModificationViewModel> labelModifications = null)
        {
            var fragmentLabels = new ReactiveList<LabeledIonViewModel> { ChangeTrackingEnabled = true };
            if (SelectedPrSm.Sequence.Count < 1) return fragmentLabels;
            var sequence = SelectedPrSm.Sequence;
            if (labelModifications != null) sequence = IonUtils.GetHeavySequence(sequence, labelModifications);
            var precursorIon = IonUtils.GetPrecursorIon(sequence, SelectedPrSm.Charge);
            lock (_cacheLock)
            {
                var prefixCompositions = _prefixCompositionCache.Get(sequence);
                var suffixCompositions = _suffixCompositionCache.Get(sequence);
                foreach (var ionType in IonTypes)
                {
                    var ionFragments = new List<LabeledIonViewModel>();
                    for (int i = 0; i < prefixCompositions.Length; i++)
                    {
                        var composition = ionType.IsPrefixIon
                            ? prefixCompositions[i]
                            : suffixCompositions[i];
                        LabeledIonViewModel label = _fragmentCache.Get(new Tuple<Composition, IonType>(composition, ionType));
                        label.Index = ionType.IsPrefixIon ? i+1 : (sequence.Count - (i+1));
                        if (label.PrecursorIon == null) label.PrecursorIon = precursorIon;
                        ionFragments.Add(label);
                    }
                    if (!ionType.IsPrefixIon) ionFragments.Reverse();
                    fragmentLabels.AddRange(ionFragments);
                }   
            }
            return fragmentLabels;
        }

        private LabeledIonViewModel GetLabeledIonViewModel(Tuple<Composition, IonType> key, Object ob)
        {
            return new LabeledIonViewModel(key.Item1, key.Item2, true, _lcms);
        }

        private ReactiveList<LabeledIonViewModel> GenerateIsotopePrecursorLabels(ReactiveList<ModificationViewModel> labelModifications = null)
        {
            var ions = new ReactiveList<LabeledIonViewModel> { ChangeTrackingEnabled = true };
            if (SelectedPrSm.Sequence.Count == 0) return ions;
            var sequence = SelectedPrSm.Sequence;
            if (labelModifications != null) sequence = IonUtils.GetHeavySequence(sequence, labelModifications);
            #pragma warning disable 0618
            var precursorIonType = new IonType("Precursor", Composition.H2O, SelectedPrSm.Charge, false);
            #pragma warning restore 0618
            var composition = sequence.Aggregate(Composition.Zero, (current, aa) => current + aa.Composition);
            var relativeIntensities = composition.GetIsotopomerEnvelope();
            var indices = new List<int> { -1 };
            for (int i = 0; i < relativeIntensities.Envolope.Length; i++)
            {
                if (relativeIntensities.Envolope[i] >= IcParameters.Instance.PrecursorRelativeIntensityThreshold || i == 0)
                    indices.Add(i);
            }
            ions.AddRange(indices.Select(index => new LabeledIonViewModel(composition, precursorIonType, false, _lcms, null, false, index)));
            return ions;
        }

        private ReactiveList<LabeledIonViewModel> GenerateChargePrecursorLabels(ReactiveList<ModificationViewModel> labelModifications = null)
        {
            var ions = new ReactiveList<LabeledIonViewModel> { ChangeTrackingEnabled = true };
            var numChargeStates = IonUtils.GetNumNeighboringChargeStates(SelectedPrSm.Charge);
            if (SelectedPrSm.Sequence.Count == 0) return ions;
            var sequence = SelectedPrSm.Sequence;
            if (labelModifications != null) sequence = IonUtils.GetHeavySequence(sequence, labelModifications);
            var composition = sequence.Aggregate(Composition.Zero, (current, aa) => current + aa.Composition);
            var minCharge = Math.Max(1, SelectedPrSm.Charge - numChargeStates);
            var maxCharge = SelectedPrSm.Charge + numChargeStates;

            for (int i = minCharge; i <= maxCharge; i++)
            {
                var index = i - minCharge;
                if (index == 0) index = SelectedPrSm.Charge - minCharge;
                if (i == SelectedPrSm.Charge) index = 0;         // guarantee that actual charge is index 0
                #pragma warning disable 0618
                var precursorIonType = new IonType("Precursor", Composition.H2O, i, false);
                #pragma warning restore 0618
                ions.Add(new LabeledIonViewModel(composition, precursorIonType, false, _lcms, null, true, index));
            }
            return ions;
        }

        private Composition[] GetPrefixCompositions(Sequence sequence, object ob)
        {
            var compositions = new Composition[sequence.Count - 1];
            for (int i = 1; i < sequence.Count; i++)
            {
                compositions[i-1] = sequence.GetComposition(0, i);
            }
            return compositions;
        }

        private Composition[] GetSuffixCompositions(Sequence sequence, object ob)
        {
            var compositions = new Composition[sequence.Count - 1];
            for (int i = 1; i < sequence.Count; i++)
            {
                compositions[i - 1] = sequence.GetComposition(i, sequence.Count);
            }
            return compositions;
        }

        private readonly Object _cacheLock;
        private readonly MemoizingMRUCache<Tuple<Composition, IonType>, LabeledIonViewModel> _fragmentCache;
        private readonly MemoizingMRUCache<Sequence, Composition[]> _prefixCompositionCache;
        private readonly MemoizingMRUCache<Sequence, Composition[]> _suffixCompositionCache; 
        private readonly ILcMsRun _lcms;
    }
}
