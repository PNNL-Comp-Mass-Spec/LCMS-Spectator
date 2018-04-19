// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IIdWriter.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   The IdWriter interface.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Writers
{
    using System.Collections.Generic;
    using Models;

    /// <summary>
    /// The IdWriter interface.
    /// </summary>
    public interface IIdWriter
    {
        /// <summary>Write IDs to file.</summary>
        /// <param name="ids">The IDs to write to a file.</param>
        void Write(IEnumerable<PrSm> ids);
    }
}
