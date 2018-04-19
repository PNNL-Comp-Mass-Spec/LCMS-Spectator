// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ISequenceReader.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interface for protein/peptide sequence readers.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using InformedProteomics.Backend.Data.Sequence;

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
        /// <returns>The parsed sequence.</returns>
        Sequence Read(string sequence);
    }
}
