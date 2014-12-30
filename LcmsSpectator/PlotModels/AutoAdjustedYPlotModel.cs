using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace LcmsSpectator.PlotModels
{
    public class AutoAdjustedYPlotModel: PlotModel
    {
        public AutoAdjustedYPlotModel(Axis xAxis, double multiplier)
        {
            DataPoints = new ConcurrentBag<IDataPoint>();
            Multiplier = multiplier;
            Axes.Add(xAxis);
            XAxis = xAxis;
            YAxis = new LinearAxis();
            if (xAxis != null) xAxis.AxisChanged += XAxisChanged;
        }

        /// <summary>
        /// Generate Y axis. Set Max Y axis to highest point in current visible x range.
        /// </summary>
        /// <param name="title">Title of the axis</param>
        /// <param name="format">String format of axis</param>
        public virtual void GenerateYAxis(string title, string format)
        {
            var maxY = GetMaxYInRange(XAxis.ActualMinimum, XAxis.ActualMaximum);
            var absoluteMaxY = GetMaxYInRange(0, XAxis.AbsoluteMaximum);
            YAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = title,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = (absoluteMaxY * Multiplier) + 1,
                Maximum = maxY * Multiplier,
                Minimum = 0,
                StringFormat = format,
                IsZoomEnabled = false,
                IsPanEnabled = false
            };
            Axes.Add(YAxis);
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
        /// Add Series to AutoAdjustedYPlotModel. This should be used in addtion to Plot.Series.Add.
        /// In the new version of OxyPlot, the actual points of a series are no longer exposed
        /// externally. Most of the time Series.Points once the plot is rendered
        /// just returns an empty list even if a series has points.
        /// </summary>
        /// <param name="dataPoints">Points to add to plot</param>
        public virtual void AddSeries(IEnumerable<IDataPoint> dataPoints) 
        {
            foreach (var point in dataPoints) DataPoints.Add(point);
        }

        public virtual void ClearSeries()
        {
            DataPoints = new ConcurrentBag<IDataPoint>();
            Series.Clear();
        }

        public virtual void ClearAxes()
        {
            XAxis.AxisChanged -= XAxisChanged;
            Axes.Clear();
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
            double[] maxY = { 0.0 };
            var dataPoints = DataPoints.ToArray();
            if (dataPoints.Length > 0)
            {
                foreach (var point in dataPoints.Where(point => point.Y >= maxY[0] &&
                                                        point.X >= minX && point.X <= maxX))
                {
                    maxY[0] = point.Y;
                }   
            }
            else
            {
                foreach (var series in Series)
                {
                    var lSeries = series as DataPointSeries;
                    if (lSeries == null || lSeries.IsVisible == false) continue;
                    foreach (var point in lSeries.Points)
                    {
                        if (point.Y >= maxY[0] && point.X >= minX && point.X <= maxX) maxY[0] = point.Y;
                    }
                }   
            }
            return maxY[0];
        }
        protected Axis YAxis;
        protected Axis XAxis;
        protected ConcurrentBag<IDataPoint> DataPoints;
        protected double Multiplier;

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
