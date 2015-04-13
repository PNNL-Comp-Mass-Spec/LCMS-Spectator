// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XicPointSeries.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class is for a series of XIC data points.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.PlotModels
{
    using System.Linq;
    using System.Threading;
    using OxyPlot.Series;

    /// <summary>
    /// This class is for a series of XIC data points.
    /// </summary>
    public class XicPointSeries : LineSeries, IDataPointSeries
    {
        /// <summary>
        /// Lock for thread-safe access to this series.
        /// </summary>
        private readonly ReaderWriterLockSlim plotModelLock;

        /// <summary>
        /// Initializes a new instance of the <see cref="XicPointSeries"/> class.
        /// </summary>
        public XicPointSeries()
        {
            this.plotModelLock = new ReaderWriterLockSlim();
            this.Index = 0;
        }

        /// <summary>
        /// Gets or sets the isotope/charge index.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Get the area under of range the curve for this series.
        /// </summary>
        /// <param name="min">Minimum X value of range.</param>
        /// <param name="max">Maximum X value of range.</param>
        /// <returns>Area under the curve.</returns>
        public double GetArea(double min, double max)
        {
            if (this.ItemsSource != null)
            {
                return this.GetAreaItemsSource(min, max);
            }

            if (this.ActualPoints == null)
            {
                return 0;
            }

            this.plotModelLock.EnterReadLock();
            var area = ActualPoints.Where(point => point.X >= min && point.X <= max).Sum(point => point.Y);
            this.plotModelLock.ExitReadLock();
            return area;
        }

        /// <summary>
        /// Calculate the maximum Y value in the range of x values.
        /// </summary>
        /// <param name="minX">Minimum x value.</param>
        /// <param name="maxX">Maximum x value.</param>
        /// <returns>Maximum Y value.</returns>
        public double GetMaxYInRange(double minX, double maxX)
        {
            if (this.ItemsSource != null)
            {
                return this.GetMaxYInRangeItemsSource(minX, maxX);
            }

            double maxY = 0.0;
            var dataPoints = ActualPoints;
            this.plotModelLock.EnterReadLock();
            if (dataPoints != null && dataPoints.Count > 0 && this.IsVisible)
            {
                foreach (var point in dataPoints)
                {
                    if (point.X >= minX && point.X <= maxX && point.Y >= maxY)
                    {
                        maxY = point.Y;   
                    }
                }
            }

            this.plotModelLock.ExitReadLock();
            return maxY;
        }

        /// <summary>
        /// Update the axis maximum and minimum for this series thread-safe.
        /// </summary>
        protected override void UpdateAxisMaxMin()
        {
            this.plotModelLock.EnterWriteLock();
            base.UpdateAxisMaxMin();
            this.plotModelLock.ExitWriteLock();
        }

        /// <summary>
        /// Update the data of this series thread-safe
        /// </summary>
        protected override void UpdateData()
        {
            this.plotModelLock.EnterWriteLock();
            base.UpdateData();
            this.plotModelLock.ExitWriteLock();
        }

        /// <summary>
        /// Get the area under of range the curve for this series if data points were set with ItemsSource.
        /// </summary>
        /// <param name="minX">Minimum X value of range.</param>
        /// <param name="maxX">Maximum X value of range.</param>
        /// <returns>Area under the curve.</returns>
        private double GetAreaItemsSource(double minX, double maxX)
        {
            return (from object item in ItemsSource 
                           select item as IDataPoint into point 
                           where point != null && point.X >= minX && point.X <= maxX 
                           select point.Y).Sum();
        }

        /// <summary>
        /// Calculate the maximum Y value in the range of x values when the data points were set with ItemsSource.
        /// </summary>
        /// <param name="minX">Minimum x value.</param>
        /// <param name="maxX">Maximum x value.</param>
        /// <returns>Maximum Y value.</returns>
        private double GetMaxYInRangeItemsSource(double minX, double maxX)
        {
            double maxY = 0.0;
            foreach (var item in this.ItemsSource)
            {
                var point = item as IDataPoint;
                if (point != null && point.X >= minX && point.X <= maxX && point.Y >= maxY)
                {
                    maxY = point.Y;
                }
            }

            return maxY;
        }
    }
}
