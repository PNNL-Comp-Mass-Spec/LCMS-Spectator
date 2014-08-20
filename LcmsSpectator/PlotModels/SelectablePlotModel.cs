using System;
using LcmsSpectator.Utils;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace LcmsSpectator.PlotModels
{
    public class SelectablePlotModel: AutoAdjustedYPlotModel
    {
        public IDataPoint SelectedDataPoint { get; set; }
        public bool UniqueHighlight { get; set; }

        public OxyColor UniqueColor { get; set; }
        public OxyColor OrdinaryColor { get; set; }

        public SelectablePlotModel(Axis xAxis, double multiplier) : base(xAxis, multiplier)
        {
            MouseDown += SelectablePlotModel_MouseDown;
            UniqueColor = OxyColors.Black;
            OrdinaryColor = OxyColors.LightGray;
        }

        /// <summary>
        /// Set point marker highlighted with UniqueColor.
        /// </summary>
        /// <param name="x">x value to set marker at</param>
        public void SetUniquePointMarker(double x)
        {
            UniqueHighlight = true;
            SetPointMarker(x, GetMarkerColor());
        }

        /// <summary>
        /// Set point marker highlighted with OrdinaryColor.
        /// </summary>
        /// <param name="x">x value to set marker at</param>
        public void SetOrdinaryPointMarker(double x)
        {
            UniqueHighlight = false;
            SetPointMarker(x, GetMarkerColor());
        }

        /// <summary>
        /// Set point marker highlighted with current color.
        /// </summary>
        /// <param name="x">x value to set marker at</param>
        public void SetPointMarker(double x)
        {
            SetPointMarker(x, GetMarkerColor());
        }
        
        /// <summary>
        /// Set point marker highlighted with a particular color.
        /// </summary>
        /// <param name="x">x value to set marker at</param>
        /// <param name="color">color of marker</param>
        public void SetPointMarker(double x, OxyColor color)
        {
            if (color == null) color = OxyColors.Black;
            var y = YAxis.Maximum;
            if (_pointMarkers != null) GuiInvoker.Invoke(() => Series.Remove(_pointMarkers));
            if (x.Equals(0)) return;
            _pointMarkers = new StemSeries(color, 3)
            {
                LineStyle = LineStyle.Dash,
                TrackerFormatString = 
                        "{0}" + Environment.NewLine +
                        "{1}: {2}" + Environment.NewLine
            };
            GuiInvoker.Invoke(() => _pointMarkers.Points.Add(new DataPoint(x, y)));
            GuiInvoker.Invoke(Series.Add, _pointMarkers);
            GuiInvoker.Invoke(InvalidatePlot, true);
        }

        /// <summary>
        /// Override SetBounds to height of marker isn't included in maxY calculation.
        /// </summary>
        /// <param name="minX">minimum x value</param>
        /// <param name="maxX">maximum x value</param>
        public override void SetBounds(double minX, double maxX)
        {
            double xPoint = 0;
            if (_pointMarkers != null && _pointMarkers.Points.Count != 0)
            {
                var point = _pointMarkers.Points[0];
                xPoint = point.X;
                GuiInvoker.Invoke(() => Series.Remove(_pointMarkers));
            }
            base.SetBounds(minX, maxX);
            SetPointMarker(xPoint, GetMarkerColor());
        }

        /// <summary>
        /// Event handler for mouse click event to set SelectedDataPoint
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void SelectablePlotModel_MouseDown(object sender, OxyMouseEventArgs args)
        {
            switch (args.ChangedButton)
            {
                case OxyMouseButton.Left:
                    var series = GetSeriesFromPoint(args.Position, 10);
                    if (series != null)
                    {
                        var result = series.GetNearestPoint(args.Position, false);
                        if (result != null && result.DataPoint != null) SelectedDataPoint = result.DataPoint;
                    }
                    break;
            }
        }

        private OxyColor GetMarkerColor()
        {
            return UniqueHighlight ? UniqueColor : OrdinaryColor;
        }

        private LineSeries _pointMarkers;
    }
}
