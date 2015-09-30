namespace LcmsSpectator.ViewModels.Data
{
    using InformedProteomics.Backend.Data.Sequence;

    using LcmsSpectator.Config;
    using LcmsSpectator.Models;

    public interface IFragmentationSequenceViewModel
    {
        /// <summary>
        /// Gets or sets the underlying sequence.
        /// </summary>
        FragmentationSequence FragmentationSequence { get; set; }

        /// <summary>
        /// Gets or sets the heavy peptide modifications.
        /// </summary>
        SearchModification[] HeavyModifications { get; set; }

        /// <summary>
        /// Gets the LabeledIonViewModels for this sequence.
        /// </summary>
        LabeledIonViewModel[] LabeledIonViewModels { get; }

        /// <summary>
        /// Gets or sets the tolerances used for creating ions in this spectrum.
        /// </summary>
        ToleranceSettings ToleranceSettings { get; set; }
    }
}
