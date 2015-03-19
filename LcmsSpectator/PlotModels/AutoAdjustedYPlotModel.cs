using System;
using System.Collections.Concurrent;
using OxyPlot;
using OxyPlot.Axes;

namespace LcmsSpectator.PlotModels
{
    public class AutoAdjustedYPlotModel: PlotModel
    {
        public AutoAdjustedYPlotModel(Axis xAxis, double multiplier)
        {
            _seriesLock = new object();
            DataPoints = new ConcurrentBag<IDataPoint>();
            Multiplier = multiplier;
            Axes.Add(xAxis);
            XAxis = xAxis;
            YAxis = new LinearAxis();
            Axes.Add(YAxis);
            if (xAxis != null) xAxis.AxisChanged += XAxisChanged;
        }

        public Axis YAxis { get; protected set; }
        public Axis XAxis { get; protected set; }

        /// <summary>
        /// Generate Y axis. Set Max Y axis to highest point in current visible x range.
        /// </summary>
        /// <param name="title">Title of the axis</param>
        /// <param name="format">String format of axis</param>
        public virtual void GenerateYAxis(string title, string format)
        {
            var maxY = GetMaxYInRange(XAxis.ActualMinimum, XAxis.ActualMaximum);
            var absoluteMaxY = GetMaxYInRange(0, XAxis.AbsoluteMaximum);
            YAxis.Position = AxisPosition.Left;
            YAxis.Title = title;
            YAxis.AbsoluteMinimum = 0;
            YAxis.AbsoluteMaximum = (absoluteMaxY*Multiplier) + 1;
            YAxis.Maximum = maxY*Multiplier;
            YAxis.Minimum = 0;
            YAxis.StringFormat = format;
            YAxis.IsZoomEnabled = false;
            YAxis.IsPanEnabled = false;
        }

        /// <summary>
        /// Update Y axis for current x axis.
        /// </summary>
        public virtual void AdjustForZoom()
        {
            var minX = XAxis.ActualMinimum;
            var maxX = XAxis.ActualMaximum;
            SetBounds(minX, maxX);
        }

        public virtual void ClearSeries()
        {
            lock (_seriesLock)
            {
                Series.Clear();
            }
        }

        /// <summary>
        /// Set min visibile x and y bounds and update y axis max by highest point in that range.
        /// </summary>
        /// <param name="minX">Min visible x</param>
        /// <param name="maxX">Max visible x</param>
        public virtual void SetBounds(double minX, double maxX)
        {
            var maxY = GetMaxYInRange(minX, maxX);
            var yaxis = DefaultYAxis ?? YAxis;
            yaxis.Maximum = maxY * Multiplier;
            InvalidatePlot(false);
        }

        /// <summary>
        /// Get maximum y point in a given x range.
        /// </summary>
        /// <param name="minX">Min x of range</param>
        /// <param name="maxX">Max x of range</param>
        /// <returns></returns>
        protected double GetMaxYInRange(double minX, double maxX)
        {
            lock (_seriesLock)
            {
                double maxY = 0.0;
                foreach (var series in Series)
                {
                    var dataPointSeries = series as IDataPointSeries;
                    if (dataPointSeries != null)
                    {
                        var seriesMaxY = dataPointSeries.GetMaxYInRange(minX, maxX);
                        if (seriesMaxY >= maxY) maxY = seriesMaxY;
                    }
                }
                return maxY;
            }
        }
        protected ConcurrentBag<IDataPoint> DataPoints;
        protected double Multiplier;
        protected Object _seriesLock;

        /// <summary>
        /// Update Y axis when X axis changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XAxisChanged(object sender, AxisChangedEventArgs e)
        {
            AdjustForZoom();
        }
    }
}
