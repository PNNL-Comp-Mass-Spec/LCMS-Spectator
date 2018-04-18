namespace LcmsSpectator.ViewModels.Data
{
    using System.Threading.Tasks;

    using InformedProteomics.Backend.Data.Sequence;

    using Models;

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

        Task<LabeledIonViewModel[]> GetLabeledIonViewModels();
    }
}