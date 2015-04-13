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
        /// <returns>Identification tree of search identifications.</returns>
        IdentificationTree Read(IEnumerable<string> modIgnoreList = null);

        /// <summary>
        /// Read a search results file asynchronously.
        /// </summary>
        /// <param name="modIgnoreList">Ignores modifications contained in this list.</param>
        /// <returns>Identification tree of search identifications.</returns>
        Task<IdentificationTree> ReadAsync(IEnumerable<string> modIgnoreList = null);
    }
}
