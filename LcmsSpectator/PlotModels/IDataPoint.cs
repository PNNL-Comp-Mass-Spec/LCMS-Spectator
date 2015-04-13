// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IDataPoint.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interface for data point for plot model series.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.PlotModels
{
    /// <summary>
    /// Interface for data point for plot model series.
    /// </summary>
    public interface IDataPoint
    {
        /// <summary>
        /// Gets or sets the X value of the data point.
        /// </summary>
        double X { get; set; }

        /// <summary>
        /// Gets or sets the Y value of the data point.
        /// </summary>
        double Y { get; set; }
    }
}
