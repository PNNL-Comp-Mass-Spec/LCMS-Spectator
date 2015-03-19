using System;
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
            _pointMarkers = _pointMarkers = new StemSeries
            {
                Color = OrdinaryColor,
                StrokeThickness = 3,
                LineStyle = LineStyle.Dash,
                TrackerFormatString =
                        "{0}" + Environment.NewLine +
                        "{1}: {2}" + Environment.NewLine
            };
            Series.Add(_pointMarkers);
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
        /// Get point marker position.
        /// </summary>
        public DataPoint GetPointMarker()
        {
            var point = new DataPoint();
            if (_pointMarkers != null && _pointMarkers.Points.Count > 0)
                point = _pointMarkers.Points[0];
            return point;
        }
        
        /// <summary>
        /// Set point marker highlighted with a particular color.
        /// </summary>
        /// <param name="x">x value to set marker at</param>
        /// <param name="color">color of marker</param>
        public void SetPointMarker(double x, OxyColor color)
        {
            //if (color == null) color = OxyColors.Black;
            var y = YAxis.Maximum;
            _pointMarkers.Points.Clear();
            if (x.Equals(0)) return;
            _pointMarkers.Color = color;
            _pointMarkers.Points.Add(new DataPoint(x, y));
            InvalidatePlot(true);
        }

        /// <summary>
        /// Override SetBounds to height of marker isn't included in maxY calculation.
        /// </summary>
        /// <param name="minX">minimum x value</param>
        /// <param name="maxX">maximum x value</param>
        public override void SetBounds(double minX, double maxX)
        {
            double xPoint = 0;
            if (_pointMarkers.Points.Count != 0) // remove marker
            {
                var point = _pointMarkers.Points[0];
                xPoint = point.X;
                _pointMarkers.Points.Clear();
            }
            base.SetBounds(minX, maxX);
            SetPointMarker(xPoint, GetMarkerColor()); // add marker
        }

        public override void ClearSeries()
        {
            lock (_seriesLock)
            {
                while (Series.Count > 1)
                {
                    if (Series[0] != _pointMarkers) Series.RemoveAt(0);
                }   
            }
        }

        /// <summary>
        /// Event handler for mouse click event to set SelectedDataPoint
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void SelectablePlotModel_MouseDown(object sender, OxyMouseEventArgs args)
        {
            var series = GetSeriesFromPoint(args.Position, 10);
            if (series != null)
            {
                var result = series.GetNearestPoint(args.Position, false);
                if (result == null) return;
                SelectedDataPoint = result.Item as IDataPoint;
            }
        }

        private OxyColor GetMarkerColor()
        {
            return UniqueHighlight ? UniqueColor : OrdinaryColor;
        }

        private readonly LineSeries _pointMarkers;
    }
}
