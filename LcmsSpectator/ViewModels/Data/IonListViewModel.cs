// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IonListViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class maintains the lists of the precursor and fragment ions for a particular
//   sequence and set of ion types.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels.Data
{
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

    /// <summary>
    /// This class maintains the lists of the precursor and fragment ions for a particular
    /// sequence and set of ion types.
    /// </summary>
    public class IonListViewModel : ReactiveObject
    {
        /// <summary>
        /// The LCMSRun for the data set.
        /// </summary>
        private readonly ILcMsRun lcms;

        /// <summary>
        /// Lock for thread-safe access to caches.
        /// </summary>
        private readonly object cacheLock;

        /// <summary>
        /// Cache for previously calculated fragment ions for a particular composition and ion type.
        /// </summary>
        private readonly MemoizingMRUCache<Tuple<Composition, IonType>, LabeledIonViewModel> fragmentCache;

        /// <summary>
        /// Cache for for previously calculated prefix compositions for a given sequence.
        /// </summary>
        private readonly MemoizingMRUCache<Sequence, Composition[]> prefixCompositionCache;

        /// <summary>
        /// Cache for previously calculated suffix compositions for a given sequence.
        /// </summary>
        private readonly MemoizingMRUCache<Sequence, Composition[]> suffixCompositionCache;

        /// <summary>
        /// The fragment ion labels.
        /// </summary>
        private ReactiveList<LabeledIonViewModel> fragmentLabels;

        /// <summary>
        /// The fragment ion labels for heavy-labeled peptides.
        /// </summary>
        private ReactiveList<LabeledIonViewModel> heavyFragmentLabels;

        /// <summary>
        /// The precursor ion labels.
        /// </summary>
        private ReactiveList<LabeledIonViewModel> precursorLabels;

        /// <summary>
        /// The labels for the precursor ion and its isotopes.
        /// </summary>
        private ReactiveList<LabeledIonViewModel> isotopePrecursorLabels;

        /// <summary>
        /// The labels for the precursor ion and its neighboring charge states.
        /// </summary>
        private ReactiveList<LabeledIonViewModel> chargePrecursorLabels;

        /// <summary>
        /// The precursor ion labels for heavy-labeled peptides.
        /// </summary>
        private ReactiveList<LabeledIonViewModel> heavyPrecursorLabels;

        /// <summary>
        /// A value indicating whether heavy labeled peptides are being displayed.
        /// </summary>
        private bool showHeavy;

        /// <summary>
        /// The selected Protein-Spectrum-Match.
        /// </summary>
        private PrSm selectedPrSm;

        /// <summary>
        /// The ion types to calculation ions for.
        /// </summary>
        private ReactiveList<IonType> ionTypes;

        /// <summary>
        /// The selected Precursor View Mode (isotopes/neighboring charges)
        /// </summary>
        private PrecursorViewMode precursorViewMode;

        /// <summary>
        /// A value indicating whether the fragment ion DataGrid should use row virtualization.
        /// </summary>
        private bool enableFragmentRowVirtualization;

        /// <summary>
        /// A value indicating whether the precursor ion DataGrid should use row virtualization.
        /// </summary>
        private bool enablePrecursorRowVirtualization;

        /// <summary>
        /// Initializes a new instance of the <see cref="IonListViewModel"/> class.
        /// </summary>
        /// <param name="lcms">The LCMSRun for the data set.</param>
        public IonListViewModel(ILcMsRun lcms)
        {
            this.prefixCompositionCache = new MemoizingMRUCache<Sequence, Composition[]>(this.GetPrefixCompositions, 20);
            this.suffixCompositionCache = new MemoizingMRUCache<Sequence, Composition[]>(this.GetSuffixCompositions, 20);
            this.lcms = lcms;
            this.fragmentCache = new MemoizingMRUCache<Tuple<Composition, IonType>, LabeledIonViewModel>(this.GetLabeledIonViewModel, 1000);
            this.cacheLock = new object();
            this.IonTypes = new ReactiveList<IonType>();
            this.ShowHeavy = false;

            this.FragmentLabels = new ReactiveList<LabeledIonViewModel>();
            this.HeavyFragmentLabels = new ReactiveList<LabeledIonViewModel>();
            this.PrecursorLabels = new ReactiveList<LabeledIonViewModel>();
            this.HeavyPrecursorLabels = new ReactiveList<LabeledIonViewModel>();
            this.ChargePrecursorLabels = new ReactiveList<LabeledIonViewModel>();
            this.IsotopePrecursorLabels = new ReactiveList<LabeledIonViewModel>();

            var precursorObservable = this.WhenAnyValue(x => x.SelectedPrSm, x => x.ShowHeavy).Where(x => x.Item1 != null);
            precursorObservable.Select(x => x.Item2 ? IcParameters.Instance.LightModifications : null)
                               .SelectMany(async mods => await this.GenerateChargeLabelsAsync(mods))
                               .Subscribe(labels => this.ChargePrecursorLabels = labels);
            precursorObservable.Select(x => x.Item2 ? IcParameters.Instance.LightModifications : null)
                               .SelectMany(async mods => await this.GenerateIsotopeLabelsAsync(mods))
                               .Subscribe(labels => { this.IsotopePrecursorLabels.ChangeTrackingEnabled = false; this.IsotopePrecursorLabels = labels; });
            precursorObservable.Where(x => x.Item2)
                .SelectMany(async x => this.PrecursorViewMode == PrecursorViewMode.Isotopes ? await this.GenerateIsotopeLabelsAsync(IcParameters.Instance.HeavyModifications)
                                                                             : await this.GenerateChargeLabelsAsync(IcParameters.Instance.HeavyModifications))
                .Subscribe(labels => { this.HeavyPrecursorLabels.ChangeTrackingEnabled = false; this.HeavyPrecursorLabels = labels; });
            IcParameters.Instance.WhenAnyValue(x => x.PrecursorRelativeIntensityThreshold, x => x.LightModifications)
                .Where(_ => this.SelectedPrSm != null).Select(x => this.ShowHeavy ? x.Item2 : null)
                .SelectMany(async mods => await this.GenerateChargeLabelsAsync(mods))
                .Subscribe(labels => { this.ChargePrecursorLabels.ChangeTrackingEnabled = false; this.ChargePrecursorLabels = labels; });
            IcParameters.Instance.WhenAnyValue(x => x.PrecursorRelativeIntensityThreshold, x => x.LightModifications)
                        .Where(_ => this.SelectedPrSm != null).Select(x => this.ShowHeavy ? x.Item2 : null)
                        .SelectMany(async mods => await this.GenerateIsotopeLabelsAsync(mods))
                        .Subscribe(labels => { this.IsotopePrecursorLabels.ChangeTrackingEnabled = false; this.IsotopePrecursorLabels = labels; });
            IcParameters.Instance.WhenAnyValue(x => x.PrecursorRelativeIntensityThreshold, x => x.HeavyModifications)
                        .Where(_ => this.SelectedPrSm != null && this.ShowHeavy)
                        .SelectMany(async x => this.PrecursorViewMode == PrecursorViewMode.Isotopes ? await this.GenerateIsotopeLabelsAsync(x.Item2)
                                                                                               : await this.GenerateChargeLabelsAsync(x.Item2))
                        .Subscribe(labels => { this.HeavyPrecursorLabels.ChangeTrackingEnabled = false; this.HeavyPrecursorLabels = labels; });
            this.WhenAnyValue(x => x.IsotopePrecursorLabels, x => x.ChargePrecursorLabels, x => x.PrecursorViewMode)
                .Select(x => x.Item3 == PrecursorViewMode.Isotopes ? x.Item1 : x.Item2)
                .Subscribe(labels => this.PrecursorLabels = labels);

            var fragmentObservable = this.WhenAnyValue(x => x.SelectedPrSm, x => x.IonTypes, x => x.ShowHeavy)
                .Where(x => x.Item1 != null)
                .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler);
            fragmentObservable.Select(x => x.Item3 ? IcParameters.Instance.LightModifications : null)
                .SelectMany(async mods => await this.GenerateFragmentLabelsAsync(mods))
                .Subscribe(labels => this.FragmentLabels = labels);
            fragmentObservable.Where(x => x.Item3)
                .SelectMany(async mods => await this.GenerateFragmentLabelsAsync(IcParameters.Instance.HeavyModifications))
                .Subscribe(labels => { this.HeavyFragmentLabels.ChangeTrackingEnabled = false; this.HeavyFragmentLabels = labels; });
            IcParameters.Instance.WhenAnyValue(x => x.LightModifications)
                .Where(_ => this.SelectedPrSm != null).Select(mods => this.ShowHeavy ? mods : null)
                .SelectMany(async mods => await this.GenerateFragmentLabelsAsync(mods))
                .Subscribe(labels => this.FragmentLabels = labels);
            IcParameters.Instance.WhenAnyValue(x => x.HeavyModifications)
                .Where(_ => this.SelectedPrSm != null && this.ShowHeavy)
                .SelectMany(async mods => await this.GenerateFragmentLabelsAsync(mods))
                .Subscribe(labels => { this.HeavyFragmentLabels.ChangeTrackingEnabled = false; this.HeavyFragmentLabels = labels; });

            this.EnableFragmentRowVirtualization = true;
            this.EnablePrecursorRowVirtualization = false;
        }

        /// <summary>
        /// Gets the fragment ion labels.
        /// </summary>
        public ReactiveList<LabeledIonViewModel> FragmentLabels
        {
            get { return this.fragmentLabels; }
            private set { this.RaiseAndSetIfChanged(ref this.fragmentLabels, value); }
        }

        /// <summary>
        /// Gets the fragment ion labels for heavy-labeled peptides.
        /// </summary>
        public ReactiveList<LabeledIonViewModel> HeavyFragmentLabels
        {
            get { return this.heavyFragmentLabels; }
            private set { this.RaiseAndSetIfChanged(ref this.heavyFragmentLabels, value); }
        }

        /// <summary>
        /// Gets the precursor ion labels.
        /// </summary>
        public ReactiveList<LabeledIonViewModel> PrecursorLabels
        {
            get { return this.precursorLabels; }
            private set { this.RaiseAndSetIfChanged(ref this.precursorLabels, value); }
        }

        /// <summary>
        /// Gets the labels for the precursor ion and its isotopes.
        /// </summary>
        public ReactiveList<LabeledIonViewModel> IsotopePrecursorLabels
        {
            get { return this.isotopePrecursorLabels; }
            private set { this.RaiseAndSetIfChanged(ref this.isotopePrecursorLabels, value); }
        }

        /// <summary>
        /// Gets the labels for the precursor ion and its neighboring charge states.
        /// </summary>
        public ReactiveList<LabeledIonViewModel> ChargePrecursorLabels
        {
            get { return this.chargePrecursorLabels; }
            private set { this.RaiseAndSetIfChanged(ref this.chargePrecursorLabels, value); }
        }

        /// <summary>
        /// Gets the precursor ion labels for heavy-labeled peptides.
        /// </summary>
        public ReactiveList<LabeledIonViewModel> HeavyPrecursorLabels
        {
            get { return this.heavyPrecursorLabels; }
            private set { this.RaiseAndSetIfChanged(ref this.heavyPrecursorLabels, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether heavy labeled peptides are being displayed.
        /// </summary>
        public bool ShowHeavy
        {
            get { return this.showHeavy; }
            set { this.RaiseAndSetIfChanged(ref this.showHeavy, value); }
        }

        /// <summary>
        /// Gets or sets the selected Protein-Spectrum-Match.
        /// </summary>
        public PrSm SelectedPrSm
        {
            get { return this.selectedPrSm; }
            set { this.RaiseAndSetIfChanged(ref this.selectedPrSm, value); }
        }

        /// <summary>
        /// Gets or sets the ion types to calculation ions for.
        /// </summary>
        public ReactiveList<IonType> IonTypes
        {
            get { return this.ionTypes; }
            set { this.RaiseAndSetIfChanged(ref this.ionTypes, value); }
        }

        /// <summary>
        /// Gets or sets the selected Precursor View Mode (isotopes/neighboring charges)
        /// </summary>
        public PrecursorViewMode PrecursorViewMode
        {
            get { return this.precursorViewMode; }
            set { this.RaiseAndSetIfChanged(ref this.precursorViewMode, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the fragment ion DataGrid should use row virtualization.
        /// </summary>
        public bool EnableFragmentRowVirtualization
        {
            get { return this.enableFragmentRowVirtualization; }
            set { this.RaiseAndSetIfChanged(ref this.enableFragmentRowVirtualization, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the precursor ion DataGrid should use row virtualization.
        /// </summary>
        public bool EnablePrecursorRowVirtualization
        {
            get { return this.enablePrecursorRowVirtualization; }
            set { this.RaiseAndSetIfChanged(ref this.enablePrecursorRowVirtualization, value); }
        }

        /// <summary>
        /// Calculate fragment ion labels asynchronously.
        /// </summary>
        /// <param name="labelModifications">The heavy/light labels.</param>
        /// <returns>A task that returns a list of fragment labeled ions on completion.</returns>
        private Task<ReactiveList<LabeledIonViewModel>> GenerateFragmentLabelsAsync(ReactiveList<SearchModification> labelModifications = null)
        {
            return Task.Run(() => this.GenerateFragmentLabels(labelModifications));
        }

        /// <summary>
        /// Calculate isotope ion labels for precursor asynchronously.
        /// </summary>
        /// <param name="labelModifications">The heavy/light labels.</param>
        /// <returns>A task that returns a list of precursor labeled ions on completion.</returns>
        private Task<ReactiveList<LabeledIonViewModel>> GenerateIsotopeLabelsAsync(
            ReactiveList<SearchModification> labelModifications = null)
        {
            return Task.Run(() => this.GenerateIsotopePrecursorLabels(labelModifications));
        }

        /// <summary>
        /// Calculate neighboring charge state ion labels for precursor asynchronously.
        /// </summary>
        /// <param name="labelModifications">The heavy/light labels.</param>
        /// <returns>A task that returns a list of precursor labeled ions on completion.</returns>
        private Task<ReactiveList<LabeledIonViewModel>> GenerateChargeLabelsAsync(
            ReactiveList<SearchModification> labelModifications = null)
        {
            return Task.Run(() => this.GenerateChargePrecursorLabels(labelModifications));
        }

        /// <summary>
        /// Calculate fragment ion labels.
        /// </summary>
        /// <param name="labelModifications">The heavy/light labels.</param>
        /// <returns>A list of fragment labeled ions.</returns>
        private ReactiveList<LabeledIonViewModel> GenerateFragmentLabels(ReactiveList<SearchModification> labelModifications = null)
        {
            var fragmentLabelList = new ReactiveList<LabeledIonViewModel> { ChangeTrackingEnabled = true };
            if (this.SelectedPrSm.Sequence.Count < 1)
            {
                return fragmentLabelList;
            }

            var sequence = this.SelectedPrSm.Sequence;
            if (labelModifications != null)
            {
                sequence = IonUtils.GetHeavySequence(sequence, labelModifications.ToArray());
            }

            var precursorIon = IonUtils.GetPrecursorIon(sequence, this.SelectedPrSm.Charge);
            lock (this.cacheLock)
            {
                var prefixCompositions = this.prefixCompositionCache.Get(sequence);
                var suffixCompositions = this.suffixCompositionCache.Get(sequence);
                foreach (var ionType in this.IonTypes)
                {
                    var ionFragments = new List<LabeledIonViewModel>();
                    for (int i = 0; i < prefixCompositions.Length; i++)
                    {
                        var composition = ionType.IsPrefixIon
                            ? prefixCompositions[i]
                            : suffixCompositions[i];
                        LabeledIonViewModel label =
                             this.fragmentCache.Get(new Tuple<Composition, IonType>(composition, ionType));
                        label.Index = ionType.IsPrefixIon ? i + 1 : (sequence.Count - (i + 1));
                        if (label.PrecursorIon == null)
                        {
                            label.PrecursorIon = precursorIon;
                        }

                        ionFragments.Add(label);
                    }

                    if (!ionType.IsPrefixIon)
                    {
                        ionFragments.Reverse();
                    }

                    fragmentLabelList.AddRange(ionFragments);
                }   
            }

            return fragmentLabelList;
        }

        /// <summary>
        /// Calculate a fragment ion label.
        /// </summary>
        /// <param name="key">The key consisting of empirical formula (composition) and ion type.</param>
        /// <param name="ob">The ob required for cache access.</param>
        /// <returns>A fragment labeled ion.</returns>
        private LabeledIonViewModel GetLabeledIonViewModel(Tuple<Composition, IonType> key, object ob)
        {
            return new LabeledIonViewModel(key.Item1, key.Item2, true, this.lcms);
        }

        /// <summary>
        /// Calculate isotope ion labels for precursor.
        /// </summary>
        /// <param name="labelModifications">The heavy/light labels.</param>
        /// <returns>A list of precursor labeled ions.</returns>
        private ReactiveList<LabeledIonViewModel> GenerateIsotopePrecursorLabels(ReactiveList<SearchModification> labelModifications = null)
        {
            var ions = new ReactiveList<LabeledIonViewModel> { ChangeTrackingEnabled = true };
            if (this.SelectedPrSm.Sequence.Count == 0)
            {
                return ions;
            }

            var sequence = this.SelectedPrSm.Sequence;
            if (labelModifications != null)
            {
                sequence = IonUtils.GetHeavySequence(sequence, labelModifications.ToArray());
            }

            #pragma warning disable 0618
            var precursorIonType = new IonType("Precursor", Composition.H2O, this.SelectedPrSm.Charge, false);
            #pragma warning restore 0618
            var composition = sequence.Aggregate(Composition.Zero, (current, aa) => current + aa.Composition);
            var relativeIntensities = composition.GetIsotopomerEnvelope();
            var indices = new List<int> { -1 };
            for (int i = 0; i < relativeIntensities.Envolope.Length; i++)
            {
                if (relativeIntensities.Envolope[i] >= IcParameters.Instance.PrecursorRelativeIntensityThreshold
                    || i == 0)
                {
                    indices.Add(i);
                }
            }

            ions.AddRange(indices.Select(index => new LabeledIonViewModel(composition, precursorIonType, false, this.lcms, null, false, index)));
            return ions;
        }

        /// <summary>
        /// Calculate neighboring charge state ion labels for precursor.
        /// </summary>
        /// <param name="labelModifications">The heavy/light labels.</param>
        /// <returns>A list of neighboring charge state labeled ions.</returns>
        private ReactiveList<LabeledIonViewModel> GenerateChargePrecursorLabels(ReactiveList<SearchModification> labelModifications = null)
        {
            var ions = new ReactiveList<LabeledIonViewModel> { ChangeTrackingEnabled = true };
            var numChargeStates = IonUtils.GetNumNeighboringChargeStates(this.SelectedPrSm.Charge);
            if (this.SelectedPrSm.Sequence.Count == 0)
            {
                return ions;
            }

            var sequence = this.SelectedPrSm.Sequence;
            if (labelModifications != null)
            {
                sequence = IonUtils.GetHeavySequence(sequence, labelModifications.ToArray());
            }

            var composition = sequence.Aggregate(Composition.Zero, (current, aa) => current + aa.Composition);
            var minCharge = Math.Max(1, this.SelectedPrSm.Charge - numChargeStates);
            var maxCharge = this.SelectedPrSm.Charge + numChargeStates;

            for (int i = minCharge; i <= maxCharge; i++)
            {
                var index = i - minCharge;
                if (index == 0)
                {
                    index = this.SelectedPrSm.Charge - minCharge;
                }

                if (i == this.SelectedPrSm.Charge) 
                {
                    index = 0;         // guarantee that actual charge is index 0
                }

                #pragma warning disable 0618
                var precursorIonType = new IonType("Precursor", Composition.H2O, i, false);
                #pragma warning restore 0618
                ions.Add(new LabeledIonViewModel(composition, precursorIonType, false, this.lcms, null, true, index));
            }

            return ions;
        }

        /// <summary>
        /// Calculate the compositions for the prefixes of a protein/peptide sequence.
        /// </summary>
        /// <param name="sequence">The sequence.</param>
        /// <param name="ob">Object required for cache access.</param>
        /// <returns>Array of prefix compositions.</returns>
        private Composition[] GetPrefixCompositions(Sequence sequence, object ob)
        {
            var compositions = new Composition[sequence.Count - 1];
            for (int i = 1; i < sequence.Count; i++)
            {
                compositions[i - 1] = sequence.GetComposition(0, i);
            }

            return compositions;
        }

        /// <summary>
        /// Calculate the compositions for the suffixes of a protein/peptide sequence.
        /// </summary>
        /// <param name="sequence">The sequence.</param>
        /// <param name="ob">Object required for cache access.</param>
        /// <returns>Array of prefix compositions.</returns>
        private Composition[] GetSuffixCompositions(Sequence sequence, object ob)
        {
            var compositions = new Composition[sequence.Count - 1];
            for (int i = 1; i < sequence.Count; i++)
            {
                compositions[i - 1] = sequence.GetComposition(i, sequence.Count);
            }

            return compositions;
        }
    }
}
