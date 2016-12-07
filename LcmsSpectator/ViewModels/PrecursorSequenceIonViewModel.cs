namespace LcmsSpectator.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Linq;
    using System.Threading.Tasks;

    using InformedProteomics.Backend.Data.Sequence;
    using InformedProteomics.Backend.Data.Spectrometry;

    using LcmsSpectator.Config;
    using LcmsSpectator.Models;
    using LcmsSpectator.ViewModels.Data;

    using ReactiveUI;

    public class PrecursorSequenceIonViewModel : ReactiveObject, IFragmentationSequenceViewModel
    {
        /// <summary>
        /// The type of precursor ions to generate (neighboring isotopes or charges).
        /// </summary>
        private PrecursorViewMode precursorViewMode;

        /// <summary>
        /// The underlying fragmentation sequence.
        /// </summary>
        private FragmentationSequence fragmentationSequence;

        /// <summary>
        /// The relative intensity threshold for isotopes.
        /// </summary>
        private double relativeIntensityThreshold;

        /// <summary>
        /// The heavy peptide modifications.
        /// </summary>
        private SearchModification[] heavyModifications;

        /// <summary>
        /// The LabeledIonViewModels for this sequence.
        /// </summary>
        private LabeledIonViewModel[] labeledIonViewModels;

        /// <summary>
        /// A value indicating whether isotope view mode is selected.
        /// </summary>
        private bool isotopeViewMode;

        /// <summary>
        /// A value indicating whether charge view mode is selected.
        /// </summary>
        private bool chargeViewMode;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrecursorSequenceIonViewModel"/> class.
        /// </summary>
        public PrecursorSequenceIonViewModel()
        {
            this.FragmentationSequence = new FragmentationSequence(
                    new Sequence(new List<AminoAcid>()),
                    1,
                    null,
                    ActivationMethod.HCD);
            this.HeavyModifications = new SearchModification[0];
            this.PrecursorViewMode = PrecursorViewMode.Isotopes;
            this.LabeledIonViewModels = new LabeledIonViewModel[0];

            this.WhenAnyValue(x => x.ChargeViewMode).Subscribe(chargeViewMode => this.IsotopeViewMode = !chargeViewMode);
            this.WhenAnyValue(x => x.IsotopeViewMode).Subscribe(isotopeViewMode => this.ChargeViewMode = !isotopeViewMode);
            this.WhenAnyValue(x => x.IsotopeViewMode).Subscribe(isotopeViewMode =>
                    this.PrecursorViewMode = isotopeViewMode ? PrecursorViewMode.Isotopes : PrecursorViewMode.Charges);
            this.WhenAnyValue(x => x.PrecursorViewMode).Subscribe(
                viewMode =>
                {
                    this.IsotopeViewMode = viewMode == PrecursorViewMode.Isotopes;
                    this.ChargeViewMode = viewMode == PrecursorViewMode.Charges;
                });

            this.WhenAnyValue(x => x.PrecursorViewMode, x => x.RelativeIntensityThreshold, x => x.HeavyModifications, x => x.FragmentationSequence)
                .SelectMany(async _ => await this.GetLabeledIonViewModels())
                .Subscribe(livms => this.LabeledIonViewModels = livms);
        }

        /// <summary>
        /// Gets or sets the underlying fragmentation sequence.
        /// </summary>
        public FragmentationSequence FragmentationSequence
        {
            get { return this.fragmentationSequence; }
            set { this.RaiseAndSetIfChanged(ref this.fragmentationSequence, value); }
        }

        /// <summary>
        /// Gets or sets the type of precursor ions to generate (neighboring isotopes or charges).
        /// </summary>
        public PrecursorViewMode PrecursorViewMode
        {
            get { return this.precursorViewMode; }
            set { this.RaiseAndSetIfChanged(ref this.precursorViewMode, value); }
        }

        /// <summary>
        /// Gets or sets the relative intensity threshold for isotopes.
        /// </summary>
        public double RelativeIntensityThreshold
        {
            get { return this.relativeIntensityThreshold; }
            set { this.RaiseAndSetIfChanged(ref this.relativeIntensityThreshold, value); }
        }

        /// <summary>
        /// Gets or sets the heavy peptide modifications.
        /// </summary>
        public SearchModification[] HeavyModifications
        {
            get { return this.heavyModifications; }
            set { this.RaiseAndSetIfChanged(ref this.heavyModifications, value); }
        }

        /// <summary>
        /// Gets the LabeledIonViewModels for this sequence.
        /// </summary>
        public LabeledIonViewModel[] LabeledIonViewModels
        {
            get { return this.labeledIonViewModels; }
            private set { this.RaiseAndSetIfChanged(ref this.labeledIonViewModels, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether isotope view mode is selected.
        /// </summary>
        public bool IsotopeViewMode
        {
            get { return this.isotopeViewMode; }
            set { this.RaiseAndSetIfChanged(ref this.isotopeViewMode, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether charge view mode is selected.
        /// </summary>
        public bool ChargeViewMode
        {
            get { return this.chargeViewMode; }
            set { this.RaiseAndSetIfChanged(ref this.chargeViewMode, value); }
        }

        /// <summary>
        /// Gets the LabeledIonViewModels for this sequence.
        /// </summary>
        /// <returns>Task that creates LabeledIonViewModels.</returns>
        private async Task<LabeledIonViewModel[]> GetLabeledIonViewModels()
        {
            return (this.PrecursorViewMode == PrecursorViewMode.Charges
                       ? await this.fragmentationSequence.GetChargePrecursorLabelsAsync(this.HeavyModifications)
                       : await this.fragmentationSequence.GetIsotopePrecursorLabelsAsync(
                           this.RelativeIntensityThreshold,
                           this.HeavyModifications)).ToArray();
        }

        Task<LabeledIonViewModel[]> IFragmentationSequenceViewModel.GetLabeledIonViewModels()
        {
            return this.GetLabeledIonViewModels();
        }
    }
}
