// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SelectablePlotModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class is an AutoAdjustedYPlotModel that shows a stem marker at a given DataPoint X value.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace LcmsSpectator.PlotModels
{
    /// <summary>
    /// This class is an AutoAdjustedYPlotModel that shows a stem marker at a given DataPoint X value.
    /// </summary>
    public class SelectablePlotModel : AutoAdjustedYPlotModel
    {
        /// <summary>
        /// Series containing the marker point.
        /// </summary>
        private readonly LineSeries pointMarkers;

        /// <summary>
        /// A value indicating whether the primary marker color should be used.
        /// </summary>
        private bool primaryHighlight;

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectablePlotModel"/> class.
        /// </summary>
        /// <param name="xaxis">The X axis.</param>
        /// <param name="multiplier">Multiplier that determines how much space to leave about tallest point.</param>
        public SelectablePlotModel(Axis xaxis, double multiplier) : base(xaxis, multiplier)
        {
            MouseDown += SelectablePlotModelMouseDown;
            PrimaryColor = OxyColors.Black;
            SecondaryColor = OxyColors.LightGray;
            pointMarkers = pointMarkers = new StemSeries
            {
                Color = SecondaryColor,
                StrokeThickness = 3,
                LineStyle = LineStyle.Dash,
                TrackerFormatString =
                        "{0}" + Environment.NewLine +
                        "{1}: {2}" + Environment.NewLine
            };
            Series.Add(pointMarkers);
        }

        /// <summary>
        /// Gets or sets the data point that is marked on the plot.
        /// </summary>
        public IDataPoint SelectedDataPoint { get; set; }

        /// <summary>
        /// Gets or sets the color for unique markings.
        /// </summary>
        public OxyColor PrimaryColor { get; set; }

        /// <summary>
        /// Gets or sets the color for ordinary markings.
        /// </summary>
        public OxyColor SecondaryColor { get; set; }

        /// <summary>
        /// Set point marker highlighted with PrimaryColor.
        /// </summary>
        /// <param name="x">x value to set marker at</param>
        public void SetPrimaryPointMarker(double x)
        {
            primaryHighlight = true;
            SetPointMarker(x, GetMarkerColor());
        }

        /// <summary>
        /// Set point marker highlighted with SecondaryColor.
        /// </summary>
        /// <param name="x">x value to set marker at</param>
        public void SetSecondaryPointMarker(double x)
        {
            primaryHighlight = false;
            SetPointMarker(x, GetMarkerColor());
        }

        /// <summary>
        /// Get point marker position.
        /// </summary>
        /// <returns>The <see cref="DataPoint"/> containing marker position.</returns>
        public DataPoint GetPointMarker()
        {
            var point = new DataPoint();
            if (pointMarkers?.Points.Count > 0)
            {
                point = pointMarkers.Points[0];
            }

            return point;
        }

        /// <summary>
        /// Set min visible x and y bounds and update y axis max by highest
        /// IDataPointSeries point in that range.
        /// </summary>
        /// <param name="minX">Min visible x</param>
        /// <param name="maxX">Max visible x</param>
        protected override void SetBounds(double minX, double maxX)
        {
            var maxY = GetMaxYInRange(minX, maxX);
            var yaxis = DefaultYAxis ?? YAxis;
            yaxis.Maximum = maxY * Multiplier;

            if (pointMarkers.Points.Count > 0)
            {
                SetPointMarker(pointMarkers.Points[0].X, GetMarkerColor());
            }

            InvalidatePlot(true);
        }

        /// <summary>
        /// Clear all series from the plot without removing marker, thread-safe.
        /// </summary>
        protected override void ClearAllSeries()
        {
            while (Series.Count > 1)
            {
                if (Series[0] != pointMarkers)
                {
                    Series.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// Set point marker highlighted with a particular color.
        /// </summary>
        /// <param name="x">x value to set marker at</param>
        /// <param name="color">color of marker</param>
        private void SetPointMarker(double x, OxyColor color)
        {
            ////if (color == null) color = OxyColors.Black;
            var y = YAxis.Maximum;
            pointMarkers.Points.Clear();
            if (x.Equals(0))
            {
                return;
            }

            pointMarkers.Color = color;
            pointMarkers.Points.Add(new DataPoint(x, y));
            InvalidatePlot(true);
        }

        /// <summary>
        /// Event handler for mouse click event to set SelectedDataPoint
        /// </summary>
        /// <param name="sender">The sender PlotModel.</param>
        /// <param name="args">The event arguments.</param>
        private void SelectablePlotModelMouseDown(object sender, OxyMouseEventArgs args)
        {
            var series = GetSeriesFromPoint(args.Position, 10);
            if (series != null)
            {
                var result = series.GetNearestPoint(args.Position, false);
                if (result == null)
                {
                    return;
                }

                SelectedDataPoint = result.Item as IDataPoint;
            }
        }

        /// <summary>
        /// Determine which color to use for highlighting.
        /// </summary>
        /// <returns>The marker highlight color.</returns>
        private OxyColor GetMarkerColor()
        {
            return primaryHighlight ? PrimaryColor : SecondaryColor;
        }
    }
}
