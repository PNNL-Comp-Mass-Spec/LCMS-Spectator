// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SelectablePlotModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class is an AutoAdjustedYPlotModel that shows a stem marker at a given DataPoint X value.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.PlotModels
{
    using System;
    using OxyPlot;
    using OxyPlot.Axes;
    using OxyPlot.Series;
    
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
        /// <param name="xAxis">The X axis.</param>
        /// <param name="multiplier">Multiplier that determines how much space to leave about tallest point.</param>
        public SelectablePlotModel(Axis xAxis, double multiplier) : base(xAxis, multiplier)
        {
            this.MouseDown += this.SelectablePlotModelMouseDown;
            this.PrimaryColor = OxyColors.Black;
            this.SecondaryColor = OxyColors.LightGray;
            this.pointMarkers = this.pointMarkers = new StemSeries
            {
                Color = this.SecondaryColor,
                StrokeThickness = 3,
                LineStyle = LineStyle.Dash,
                TrackerFormatString =
                        "{0}" + Environment.NewLine +
                        "{1}: {2}" + Environment.NewLine
            };
            Series.Add(this.pointMarkers);
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
            this.primaryHighlight = true;
            this.SetPointMarker(x, this.GetMarkerColor());
        }

        /// <summary>
        /// Set point marker highlighted with SecondaryColor.
        /// </summary>
        /// <param name="x">x value to set marker at</param>
        public void SetSecondaryPointMarker(double x)
        {
            this.primaryHighlight = false;
            this.SetPointMarker(x, this.GetMarkerColor());
        }

        /// <summary>
        /// Get point marker position.
        /// </summary>
        /// <returns>The <see cref="DataPoint"/> containing marker position.</returns>
        public DataPoint GetPointMarker()
        {
            var point = new DataPoint();
            if (this.pointMarkers != null && this.pointMarkers.Points.Count > 0)
            {
                point = this.pointMarkers.Points[0];   
            }

            return point;
        }
        
        /// <summary>
        /// Clear all series from the plot without removing marker, thread-safe.
        /// </summary>
        protected override void ClearAllSeries()
        {
            while (Series.Count > 1)
            {
                if (this.Series[0] != this.pointMarkers)
                {
                    this.Series.RemoveAt(0);
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
            this.pointMarkers.Points.Clear();
            if (x.Equals(0))
            {
                return;
            }

            this.pointMarkers.Color = color;
            this.pointMarkers.Points.Add(new DataPoint(x, y));
            this.InvalidatePlot(true);
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

                this.SelectedDataPoint = result.Item as IDataPoint;
            }
        }

        /// <summary>
        /// Determine which color to use for highlighting.
        /// </summary>
        /// <returns>The marker highlight color.</returns>
        private OxyColor GetMarkerColor()
        {
            return this.primaryHighlight ? this.PrimaryColor : this.SecondaryColor;
        }
    }
}
