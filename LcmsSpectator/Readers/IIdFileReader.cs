// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IIdFileReader.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interface for database search results readers.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InformedProteomics.Backend.Data.Sequence;
using LcmsSpectator.Models;

namespace LcmsSpectator.Readers
{
    /// <summary>
    /// Interface for database search results readers.
    /// </summary>
    public interface IIdFileReader
    {
        /// <summary>
        /// Read a search results file.
        /// </summary>
        /// <param name="scanStart">Optional filter to apply when reading from the peptide ID file</param>
        /// <param name="scanEnd">Optional filter to apply when reading from the peptide ID file</param>
        /// <param name="modIgnoreList">Ignores modifications contained in this list (not all readers support this).</param>
        /// <param name="progress">The progress reporter.</param>
        /// <returns>The Protein-Spectrum-Match identifications.</returns>
        IEnumerable<PrSm> Read(
            int scanStart = 0,
            int scanEnd = 0,
            IEnumerable<string> modIgnoreList = null,
            IProgress<double> progress = null);

        /// <summary>
        /// Read a search results file asynchronously.
        /// </summary>
        /// <param name="scanStart">Optional filter to apply when reading from the peptide ID file</param>
        /// <param name="scanEnd">Optional filter to apply when reading from the peptide ID file</param>
        /// <param name="modIgnoreList">Ignores modifications contained in this list (not all readers support this).</param>
        /// <param name="progress">The progress reporter.</param>
        /// <returns>The Protein-Spectrum-Match identifications.</returns>
        Task<IEnumerable<PrSm>> ReadAsync(
            int scanStart = 0,
            int scanEnd = 0,
            IEnumerable<string> modIgnoreList = null,
            IProgress<double> progress = null);

        /// <summary>
        /// Gets a list of modifications that potentially need to be registered after reading.
        /// </summary>
        IList<Modification> Modifications { get; }
    }
}
