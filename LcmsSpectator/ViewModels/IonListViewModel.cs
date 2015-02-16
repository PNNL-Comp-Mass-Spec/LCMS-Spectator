using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using InformedProteomics.Backend.Data.Composition;
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
            _lcms = lcms;
            _fragmentCache = new MemoizingMRUCache<Tuple<Composition, int, IonType>, LabeledIonViewModel>(GetLabeledIonViewModel, 2000);
            _fragmentCacheLock = new object();
            IonTypes = new ReactiveList<IonType>();
            ShowHeavy = false;
            this.WhenAnyValue(x => x.SelectedPrSm, x => x.PrecursorViewMode, x => x.ShowHeavy)
                .Where(x => x.Item1 != null && !x.Item3)
                .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
                .Subscribe(async _ =>
                {
                    PrecursorLabels = await GeneratePrecursorLabelsAsync();
                });
            this.WhenAnyValue(x => x.SelectedPrSm, x => x.IonTypes, x => x.ShowHeavy)
                .Where(x => x.Item1 != null && !x.Item3)
                .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
                .Subscribe(async _ =>
                {
                    FragmentLabels = await GenerateFragmentLabelsAsync();
                });
            IcParameters.Instance.WhenAnyValue(x => x.PrecursorRelativeIntensityThreshold)
                        .Where(x => SelectedPrSm != null && !ShowHeavy)
                        .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
                        .Subscribe(async _ =>
                        {
                            PrecursorLabels = await GeneratePrecursorLabelsAsync();
                        });
        }

        #region Ion label properties

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

        private ReactiveList<LabeledIonViewModel> _heavyPrecursorLabels; 
        public ReactiveList<LabeledIonViewModel> HeavyPrecursorLabels
        {
            get { return _heavyPrecursorLabels; }
            private set { this.RaiseAndSetIfChanged(ref _heavyPrecursorLabels, value); }
        }
        #endregion

        #region Public Properties
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
        #endregion

        #region Private Methods
        private Task<ReactiveList<LabeledIonViewModel>> GenerateFragmentLabelsAsync()
        {
            return Task.Run(() => GenerateFragmentLabels());
        }

        private Task<ReactiveList<LabeledIonViewModel>> GeneratePrecursorLabelsAsync()
        {
            return Task.Run(() => GeneratePrecursorLabels());
        }

        private ReactiveList<LabeledIonViewModel> GeneratePrecursorLabels()
        {
            return (PrecursorViewMode == PrecursorViewMode.Isotopes) ? GenerateIsotopePrecursorLabels()
                                                                     :  GenerateChargePrecursorLabels();
        }

        private ReactiveList<LabeledIonViewModel> GenerateFragmentLabels()
        {
            var fragmentLabels = new ReactiveList<LabeledIonViewModel> { ChangeTrackingEnabled = true };
            if (SelectedPrSm.Sequence.Count < 1) return fragmentLabels;
            var precursorIon = IonUtils.GetPrecursorIon(SelectedPrSm.Sequence, SelectedPrSm.Charge);
            lock (_fragmentCacheLock)
            {
                foreach (var ionType in IonTypes)
                {
                    var ionFragments = new List<LabeledIonViewModel>();
                    for (int i = 1; i < SelectedPrSm.Sequence.Count; i++)
                    {
                        var composition = ionType.IsPrefixIon
                            ? SelectedPrSm.Sequence.GetComposition(0, i)
                            : SelectedPrSm.Sequence.GetComposition(i, SelectedPrSm.Sequence.Count);
                        var labelIndex = ionType.IsPrefixIon ? i : (SelectedPrSm.Sequence.Count - i);
                        LabeledIonViewModel label = _fragmentCache.Get(new Tuple<Composition, int, IonType>(composition, labelIndex, ionType));
                        if (label.PrecursorIon == null) label.PrecursorIon = precursorIon;
                        ionFragments.Add(label);
                    }
                    if (!ionType.IsPrefixIon) ionFragments.Reverse();
                    fragmentLabels.AddRange(ionFragments);
                }   
            }
            return fragmentLabels;
        }

        private LabeledIonViewModel GetLabeledIonViewModel(Tuple<Composition, int, IonType> key, Object ob)
        {
            return new LabeledIonViewModel(key.Item1, key.Item2, key.Item3, true, _lcms);
        }

        private ReactiveList<LabeledIonViewModel> GenerateIsotopePrecursorLabels()
        {
            var ions = new ReactiveList<LabeledIonViewModel> { ChangeTrackingEnabled = true };
            if (SelectedPrSm.Sequence.Count == 0) return ions;
            var precursorIonType = new IonType("Precursor", Composition.H2O, SelectedPrSm.Charge, false);
            var composition = SelectedPrSm.Sequence.Aggregate(Composition.Zero, (current, aa) => current + aa.Composition);
            var relativeIntensities = composition.GetIsotopomerEnvelope();
            var indices = new List<int> { -1 };
            for (int i = 0; i < relativeIntensities.Envolope.Length; i++)
            {
                if (relativeIntensities.Envolope[i] >= IcParameters.Instance.PrecursorRelativeIntensityThreshold || i == 0)
                    indices.Add(i);
            }
            ions.AddRange(indices.Select(index => new LabeledIonViewModel(composition, index, precursorIonType, false, _lcms)));
            return ions;
        }

        private ReactiveList<LabeledIonViewModel> GenerateChargePrecursorLabels()
        {
            var ions = new ReactiveList<LabeledIonViewModel> { ChangeTrackingEnabled = true };
            var numChargeStates = IonUtils.GetNumNeighboringChargeStates(SelectedPrSm.Charge);
            if (SelectedPrSm.Sequence.Count == 0) return ions;
            var composition = SelectedPrSm.Sequence.Aggregate(Composition.Zero, (current, aa) => current + aa.Composition);
            var minCharge = Math.Max(1, SelectedPrSm.Charge - numChargeStates);
            var maxCharge = SelectedPrSm.Charge + numChargeStates;

            for (int i = minCharge; i <= maxCharge; i++)
            {
                var index = i - minCharge;
                if (index == 0) index = SelectedPrSm.Charge - minCharge;
                if (i == SelectedPrSm.Charge) index = 0;         // guarantee that actual charge is index 0
                var precursorIonType = new IonType("Precursor", Composition.H2O, i, false);
                ions.Add(new LabeledIonViewModel(composition, index, precursorIonType, false, _lcms, null, true));
            }
            return ions;
        }
#endregion

        private readonly Object _fragmentCacheLock;
        private readonly MemoizingMRUCache<Tuple<Composition, int, IonType>, LabeledIonViewModel> _fragmentCache; 
        private readonly ILcMsRun _lcms;
    }
}
