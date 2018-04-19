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

namespace LcmsSpectator.ViewModels
{
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
            FragmentationSequence = new FragmentationSequence(
                    new Sequence(new List<AminoAcid>()),
                    1,
                    null,
                    ActivationMethod.HCD);
            HeavyModifications = new SearchModification[0];
            PrecursorViewMode = PrecursorViewMode.Isotopes;
            LabeledIonViewModels = new LabeledIonViewModel[0];

            this.WhenAnyValue(x => x.ChargeViewMode).Subscribe(chargeViewMode => IsotopeViewMode = !chargeViewMode);
            this.WhenAnyValue(x => x.IsotopeViewMode).Subscribe(isotopeViewMode => ChargeViewMode = !isotopeViewMode);
            this.WhenAnyValue(x => x.IsotopeViewMode).Subscribe(isotopeViewMode =>
                    PrecursorViewMode = isotopeViewMode ? PrecursorViewMode.Isotopes : PrecursorViewMode.Charges);
            this.WhenAnyValue(x => x.PrecursorViewMode).Subscribe(
                viewMode =>
                {
                    IsotopeViewMode = viewMode == PrecursorViewMode.Isotopes;
                    ChargeViewMode = viewMode == PrecursorViewMode.Charges;
                });

            this.WhenAnyValue(x => x.PrecursorViewMode, x => x.RelativeIntensityThreshold, x => x.HeavyModifications, x => x.FragmentationSequence)
                .SelectMany(async _ => await GetLabeledIonViewModels())
                .Subscribe(livms => LabeledIonViewModels = livms);
        }

        /// <summary>
        /// Gets or sets the underlying fragmentation sequence.
        /// </summary>
        public FragmentationSequence FragmentationSequence
        {
            get => fragmentationSequence;
            set => this.RaiseAndSetIfChanged(ref fragmentationSequence, value);
        }

        /// <summary>
        /// Gets or sets the type of precursor ions to generate (neighboring isotopes or charges).
        /// </summary>
        public PrecursorViewMode PrecursorViewMode
        {
            get => precursorViewMode;
            set => this.RaiseAndSetIfChanged(ref precursorViewMode, value);
        }

        /// <summary>
        /// Gets or sets the relative intensity threshold for isotopes.
        /// </summary>
        public double RelativeIntensityThreshold
        {
            get => relativeIntensityThreshold;
            set => this.RaiseAndSetIfChanged(ref relativeIntensityThreshold, value);
        }

        /// <summary>
        /// Gets or sets the heavy peptide modifications.
        /// </summary>
        public SearchModification[] HeavyModifications
        {
            get => heavyModifications;
            set => this.RaiseAndSetIfChanged(ref heavyModifications, value);
        }

        /// <summary>
        /// Gets the LabeledIonViewModels for this sequence.
        /// </summary>
        public LabeledIonViewModel[] LabeledIonViewModels
        {
            get => labeledIonViewModels;
            private set => this.RaiseAndSetIfChanged(ref labeledIonViewModels, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether isotope view mode is selected.
        /// </summary>
        public bool IsotopeViewMode
        {
            get => isotopeViewMode;
            set => this.RaiseAndSetIfChanged(ref isotopeViewMode, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether charge view mode is selected.
        /// </summary>
        public bool ChargeViewMode
        {
            get => chargeViewMode;
            set => this.RaiseAndSetIfChanged(ref chargeViewMode, value);
        }

        /// <summary>
        /// Gets the LabeledIonViewModels for this sequence.
        /// </summary>
        /// <returns>Task that creates LabeledIonViewModels.</returns>
        private async Task<LabeledIonViewModel[]> GetLabeledIonViewModels()
        {
            return (PrecursorViewMode == PrecursorViewMode.Charges
                       ? await fragmentationSequence.GetChargePrecursorLabelsAsync(HeavyModifications)
                       : await fragmentationSequence.GetIsotopePrecursorLabelsAsync(
                           RelativeIntensityThreshold,
                           HeavyModifications)).ToArray();
        }

        Task<LabeledIonViewModel[]> IFragmentationSequenceViewModel.GetLabeledIonViewModels()
        {
            return GetLabeledIonViewModels();
        }
    }
}
