// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IIdData.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   An interface for a single hierarchical level of an IdentificationTree.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Models
{
    using InformedProteomics.Backend.MassSpecData;

    /// <summary>
    /// An interface for a single hierarchical level of an IdentificationTree.
    /// </summary>
    public interface IIdData
    {
        /// <summary>
        /// Add a Protein-Spectrum-Match identification.
        /// </summary>
        /// <param name="id">Protein-Spectrum-Math to add</param>
        void Add(PrSm id);

        /// <summary>
        /// Remove a Protein-Spectrum-Match identification.
        /// </summary>
        /// <param name="id">Protein-Spectrum-Match to remove.</param>
        void Remove(PrSm id);

        /// <summary>
        /// Set the LCMSRun for all data.
        /// </summary>
        /// <param name="lcms">LCMSRun to set.</param>
        /// <param name="dataSetName">Name of the data this for the LCMSRun.</param>
        void SetLcmsRun(ILcMsRun lcms, string dataSetName);

        /// <summary>
        /// Determines whether the item contains a given identification.
        /// </summary>
        /// <param name="id">the ID to search for.</param>
        /// <returns>A value indicating whether the item contains the identification.</returns>
        bool Contains(PrSm id);

        /// <summary>
        /// Get the PRSM in the tree with the highest score.
        /// </summary>
        /// <returns>The PRSM with the highest score.</returns>
        PrSm GetHighestScoringPrSm();
    }
}
