// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ToolType.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Supported database search tools.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Models.DTO
{
    /// <summary>
    /// Supported database search tools.
    /// </summary>
    public enum ToolType
    {
        /// <summary>
        /// MS-GF+ database search tool.
        /// </summary>
        MsgfPlus,

        /// <summary>
        /// MS-PathFinder database search tool.
        /// </summary>
        MsPathFinder,

        /// <summary>
        /// Other database search tool.
        /// </summary>
        Other
    }
}
