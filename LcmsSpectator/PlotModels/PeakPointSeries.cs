// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PeakPointSeries.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class is for a series of spectrum data points.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using OxyPlot.Series;

namespace LcmsSpectator.PlotModels
{
    /// <summary>
    /// This class is for a series of spectrum data points.
    /// </summary>
    public class PeakPointSeries : StemSeries, IDataPointSeries
    {
        /// <summary>
        /// Calculate the maximum Y value in the range of x values.
        /// </summary>
        /// <param name="minX">Minimum x value.</param>
        /// <param name="maxX">Maximum x value.</param>
        /// <returns>Maximum Y value.</returns>
        public double GetMaxYInRange(double minX, double maxX)
        {
            if (ItemsSource != null)
            {
                return GetMaxYInRangeItemsSource(minX, maxX);
            }

            var maxY = 0.0;
            var dataPoints = ActualPoints;
            if (dataPoints != null && dataPoints.Count > 0 && IsVisible)
            {
                foreach (var point in dataPoints)
                {
                    if (point.X >= minX && point.X <= maxX && point.Y >= maxY)
                    {
                        maxY = point.Y;
                    }
                }
            }

            return maxY;
        }

        /// <summary>
        /// Calculate the maximum Y value in the range of x values when the data points were set with ItemsSource.
        /// </summary>
        /// <param name="minX">Minimum x value.</param>
        /// <param name="maxX">Maximum x value.</param>
        /// <returns>Maximum Y value.</returns>
        private double GetMaxYInRangeItemsSource(double minX, double maxX)
        {
            var maxY = 0.0;
            foreach (var item in ItemsSource)
            {
                if (item is IDataPoint point && point.X >= minX && point.X <= maxX && point.Y >= maxY)
                {
                    maxY = point.Y;
                }
            }

            return maxY;
        }
    }

    /// <summary>
    /// This class is for a series of spectrum data points.
    /// </summary>
    public class ProfilePeakPointSeries : LineSeries, IDataPointSeries
    {
        /// <summary>
        /// Calculate the maximum Y value in the range of x values.
        /// </summary>
        /// <param name="minX">Minimum x value.</param>
        /// <param name="maxX">Maximum x value.</param>
        /// <returns>Maximum Y value.</returns>
        public double GetMaxYInRange(double minX, double maxX)
        {
            if (ItemsSource != null)
            {
                return GetMaxYInRangeItemsSource(minX, maxX);
            }

            var maxY = 0.0;
            var dataPoints = ActualPoints;
            if (dataPoints != null && dataPoints.Count > 0 && IsVisible)
            {
                foreach (var point in dataPoints)
                {
                    if (point.X >= minX && point.X <= maxX && point.Y >= maxY)
                    {
                        maxY = point.Y;
                    }
                }
            }

            return maxY;
        }

        /// <summary>
        /// Calculate the maximum Y value in the range of x values when the data points were set with ItemsSource.
        /// </summary>
        /// <param name="minX">Minimum x value.</param>
        /// <param name="maxX">Maximum x value.</param>
        /// <returns>Maximum Y value.</returns>
        private double GetMaxYInRangeItemsSource(double minX, double maxX)
        {
            var maxY = 0.0;
            foreach (var item in ItemsSource)
            {
                if (item is IDataPoint point && point.X >= minX && point.X <= maxX && point.Y >= maxY)
                {
                    maxY = point.Y;
                }
            }

            return maxY;
        }
    }
}
