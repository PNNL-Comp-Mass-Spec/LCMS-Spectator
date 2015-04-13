// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AutoAdjustedYPlotModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Plot model that automatically adjusts the visible range of the Y Axis based on the
//   tallest point in the range visible on the X Axis.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.PlotModels
{
    using System;
    using System.Collections.Concurrent;
    using OxyPlot;
    using OxyPlot.Axes;
    
    /// <summary>
    /// Plot model that automatically adjusts the visible range of the Y Axis based on the
    /// tallest point in the range visible on the X Axis.
    /// </summary>
    public class AutoAdjustedYPlotModel : PlotModel
    {
        /// <summary>
        /// Thread-safe collection of all data points on the plot.
        /// </summary>
        protected ConcurrentBag<IDataPoint> DataPoints;

        /// <summary>
        /// Multiplier that determines how much space to leave about tallest point.
        /// </summary>
        protected double Multiplier;

        /// <summary>
        /// Lock for thread-safe access to Series on the plot.
        /// </summary>
        protected Object SeriesLock;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoAdjustedYPlotModel"/> class.
        /// </summary>
        /// <param name="xAxis">The X Axis of the plot</param>
        /// <param name="multiplier">Multiplier that determines how much space to leave about tallest point.</param>
        public AutoAdjustedYPlotModel(Axis xAxis, double multiplier)
        {
            this.SeriesLock = new object();
            this.DataPoints = new ConcurrentBag<IDataPoint>();
            this.Multiplier = multiplier;
            Axes.Add(xAxis);
            this.XAxis = xAxis;
            this.YAxis = new LinearAxis();
            Axes.Add(this.YAxis);
            if (xAxis != null)
            {
                xAxis.AxisChanged += this.XAxisChanged;
            }
        }

        /// <summary>
        /// Gets or sets the Y Axis for the plot model.
        /// </summary>
        public Axis YAxis { get; protected set; }

        /// <summary>
        /// Gets or sets the X Axis for the plot model.
        /// </summary>
        public Axis XAxis { get; protected set; }

        /// <summary>
        /// Generate Y axis. Set Max Y axis to highest point in current visible x range.
        /// </summary>
        /// <param name="title">Title of the axis</param>
        /// <param name="format">String format of axis</param>
        public virtual void GenerateYAxis(string title, string format)
        {
            var maxY = this.GetMaxYInRange(this.XAxis.ActualMinimum, this.XAxis.ActualMaximum);
            var absoluteMaxY = this.GetMaxYInRange(0, this.XAxis.AbsoluteMaximum);
            this.YAxis.Position = AxisPosition.Left;
            this.YAxis.Title = title;
            this.YAxis.AbsoluteMinimum = 0;
            this.YAxis.AbsoluteMaximum = (absoluteMaxY * this.Multiplier) + 1;
            this.YAxis.Maximum = maxY * this.Multiplier;
            this.YAxis.Minimum = 0;
            this.YAxis.StringFormat = format;
            this.YAxis.IsZoomEnabled = false;
            this.YAxis.IsPanEnabled = false;
        }

        /// <summary>
        /// Update Y axis for current x axis.
        /// </summary>
        public virtual void AdjustForZoom()
        {
            var minX = this.XAxis.ActualMinimum;
            var maxX = this.XAxis.ActualMaximum;
            this.SetBounds(minX, maxX);
        }

        /// <summary>
        /// Clear all series on plot (thread-safe)
        /// </summary>
        public virtual void ClearSeries()
        {
            lock (this.SeriesLock)
            {
                Series.Clear();
            }
        }

        /// <summary>
        /// Set min visible x and y bounds and update y axis max by highest point in that range.
        /// </summary>
        /// <param name="minX">Min visible x</param>
        /// <param name="maxX">Max visible x</param>
        public virtual void SetBounds(double minX, double maxX)
        {
            var maxY = this.GetMaxYInRange(minX, maxX);
            var yaxis = DefaultYAxis ?? this.YAxis;
            yaxis.Maximum = maxY * this.Multiplier;
            this.InvalidatePlot(false);
        }

        /// <summary>
        /// Get maximum y point in a given x range.
        /// </summary>
        /// <param name="minX">Min x of range</param>
        /// <param name="maxX">Max x of range</param>
        /// <returns>The value of the tallest point in the range.</returns>
        protected double GetMaxYInRange(double minX, double maxX)
        {
            lock (this.SeriesLock)
            {
                double maxY = 0.0;
                foreach (var series in this.Series)
                {
                    var dataPointSeries = series as IDataPointSeries;
                    if (dataPointSeries != null)
                    {
                        var seriesMaxY = dataPointSeries.GetMaxYInRange(minX, maxX);
                        if (seriesMaxY >= maxY)
                        {
                            maxY = seriesMaxY;
                        }
                    }
                }

                return maxY;
            }
        }

        /// <summary>
        /// Event handler for X Axis changed event.
        /// Updates Y axis when X axis changes
        /// </summary>
        /// <param name="sender">The sender LinearAxis</param>
        /// <param name="e">The event arguments</param>
        private void XAxisChanged(object sender, AxisChangedEventArgs e)
        {
            this.AdjustForZoom();
        }
    }
}
