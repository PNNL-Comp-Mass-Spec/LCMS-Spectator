// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IIdFileReader.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interface for database search results readers.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Readers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using LcmsSpectator.Models;
    
    /// <summary>
    /// Interface for database search results readers.
    /// </summary>
    public interface IIdFileReader
    {
        /// <summary>
        /// Read a search results file.
        /// </summary>
        /// <param name="modIgnoreList">Ignores modifications contained in this list.</param>
        /// <returns>The Protein-Spectrum-Match identifications.</returns>
        IEnumerable<PrSm> Read(IEnumerable<string> modIgnoreList = null);

        /// <summary>
        /// Read a search results file asynchronously.
        /// </summary>
        /// <param name="modIgnoreList">Ignores modifications contained in this list.</param>
        /// <returns>The Protein-Spectrum-Match identifications.</returns>
        Task<IEnumerable<PrSm>> ReadAsync(IEnumerable<string> modIgnoreList = null);
    }
}
