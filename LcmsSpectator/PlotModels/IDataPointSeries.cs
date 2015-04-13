// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IDataPointSeries.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interface for a series of (X, Y) cartesian data points.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.PlotModels
{
    /// <summary>
    /// Interface for a series of (X, Y) cartesian data points.
    /// </summary>
    public interface IDataPointSeries
    {
        /// <summary>
        /// Calculate the maximum Y value in the range of x values.
        /// </summary>
        /// <param name="min">Minimum x value.</param>
        /// <param name="max">Maximum x value.</param>
        /// <returns>Maximum Y value.</returns>
        double GetMaxYInRange(double min, double max);
    }
}
