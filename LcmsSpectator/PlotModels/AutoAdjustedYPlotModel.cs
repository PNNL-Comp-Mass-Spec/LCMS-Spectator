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
    using OxyPlot;
    using OxyPlot.Axes;

    /// <summary>
    /// Plot model that automatically adjusts the visible range of the Y Axis based on the
    /// tallest point in the range visible on the X Axis.
    /// </summary>
    public class AutoAdjustedYPlotModel : PlotModel
    {
        /// <summary>
        /// A lock for thread-safe access to Series on the plot.
        /// </summary>
        private readonly object seriesLock;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoAdjustedYPlotModel"/> class.
        /// </summary>
        /// <param name="xaxis">The X Axis of the plot</param>
        /// <param name="multiplier">Multiplier that determines how much space to leave about tallest point.</param>
        public AutoAdjustedYPlotModel(Axis xaxis, double multiplier)
        {
            seriesLock = new object();
            Multiplier = multiplier;
            Axes.Add(xaxis);
            XAxis = xaxis;
            YAxis = new LinearAxis
            {
                IsZoomEnabled = false,
                IsPanEnabled = false,
                Position = AxisPosition.Left,
                Minimum = 0,
                AbsoluteMinimum = 0
            };
            AutoAdjustYAxis = true;
            Axes.Add(YAxis);
            if (xaxis != null)
            {
                xaxis.AxisChanged += XAxisChanged;
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
        /// Gets or sets a value indicating whether this plot should automatically
        /// adjust the Y Axis depending on the range selected on the X axis.
        /// </summary>
        public bool AutoAdjustYAxis { get; set; }

        /// <summary>
        /// Gets a multiplier that determines how much space to leave about tallest point.
        /// </summary>
        protected double Multiplier { get; }

        /// <summary>
        /// Update Y axis for current x axis.
        /// </summary>
        public void AdjustForZoom()
        {
            var minX = XAxis.ActualMinimum;
            var maxX = XAxis.ActualMaximum;
            SetBounds(minX, maxX);
        }

        /// <summary>
        /// Clear all series on plot (thread-safe)
        /// </summary>
        public void ClearSeries()
        {
            lock (seriesLock)
            {
                ClearAllSeries();
            }
        }

        /// <summary>
        /// Set min visible x and y bounds and update y axis max by highest
        /// IDataPointSeries point in that range.
        /// </summary>
        /// <param name="minX">Min visible x</param>
        /// <param name="maxX">Max visible x</param>
        protected virtual void SetBounds(double minX, double maxX)
        {
            var maxY = GetMaxYInRange(minX, maxX);
            var yaxis = DefaultYAxis ?? YAxis;
            yaxis.Maximum = maxY * Multiplier;
            InvalidatePlot(false);
        }

        /// <summary>
        /// Overridable clear all series on plot.
        /// </summary>
        protected virtual void ClearAllSeries()
        {
            Series.Clear();
        }

        /// <summary>
        /// Get maximum y IDataPointSeries point in a given x range.
        /// </summary>
        /// <param name="minX">Min x of range</param>
        /// <param name="maxX">Max x of range</param>
        /// <returns>The value of the tallest point in the range.</returns>
        protected virtual double GetMaxYInRange(double minX, double maxX)
        {
            lock (seriesLock)
            {
                var maxY = 0.0;
                foreach (var series in Series)
                {
                    if (series is IDataPointSeries dataPointSeries)
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
            if (AutoAdjustYAxis)
            {   // Y axis is zoomed based on the the maximum visible point in the range selected by the X axis.
                AdjustForZoom();
            }
        }
    }
}
