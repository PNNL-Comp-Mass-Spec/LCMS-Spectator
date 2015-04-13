// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ISequenceReader.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interface for protein/peptide sequence readers.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Readers.SequenceReaders
{
    /// <summary>
    /// Interface for protein/peptide sequence readers.
    /// </summary>
    public interface ISequenceReader
    {
        /// <summary>
        /// Parse a protein/peptide sequence.
        /// </summary>
        /// <param name="sequence">The sequence as a string.</param>
        /// <param name="trim">A value indicating whether the n-terminal and c-terminal amino acids should be trimmed.</param>
        /// <returns>The parsed sequence.</returns>
        InformedProteomics.Backend.Data.Sequence.Sequence Read(string sequence, bool trim = false);
    }
}
