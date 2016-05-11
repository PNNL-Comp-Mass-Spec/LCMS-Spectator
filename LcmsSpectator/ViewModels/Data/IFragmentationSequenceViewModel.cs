namespace LcmsSpectator.ViewModels.Data
{
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    using InformedProteomics.Backend.Data.Sequence;
    using InformedProteomics.Backend.Data.Spectrometry;

    using LcmsSpectator.Models;
    using LcmsSpectator.PlotModels.ColorDicionaries;

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
    }
}
