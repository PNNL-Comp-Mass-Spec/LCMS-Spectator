using LcmsSpectator.Utils;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace LcmsSpectator.PlotModels
{
    public class SelectablePlotModel: AutoAdjustedYPlotModel
    {
        public DataPoint SelectedDataPoint { get; set; }
        public bool UniqueHighlight { get; set; }
        public SelectablePlotModel(Axis xAxis, double multiplier) : base(xAxis, multiplier)
        {
            MouseDown += SelectablePlotModel_MouseDown;
        }

        public void SetUniquePointMarker(double x)
        {
            UniqueHighlight = true;
            SetPointMarker(x, GetMarkerColor());
        }

        public void SetOrdinaryPointMarker(double x)
        {
            UniqueHighlight = false;
            SetPointMarker(x, GetMarkerColor());
        }

        public void SetPointMarker(double x)
        {
            SetPointMarker(x, GetMarkerColor());
        }
        
        public void SetPointMarker(double x, OxyColor color)
        {
            if (color == null) color = OxyColors.Black;
            var y = YAxis.Maximum;
            if (_pointMarkers != null) GuiInvoker.Invoke(() => Series.Remove(_pointMarkers));
            if (x.Equals(0)) return;
            _pointMarkers = new StemSeries(color, 3)
            {
                LineStyle = LineStyle.Dash
            };
            GuiInvoker.Invoke(() => _pointMarkers.Points.Add(new DataPoint(x, y)));
            GuiInvoker.Invoke(Series.Add, _pointMarkers);
            GuiInvoker.Invoke(InvalidatePlot, true);
        }

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

        void SelectablePlotModel_MouseDown(object sender, OxyMouseEventArgs args)
        {
            switch (args.ChangedButton)
            {
                case OxyMouseButton.Left:
                    var series = GetSeriesFromPoint(args.Position, 10);
                    if (series != null)
                    {
                        var result = series.GetNearestPoint(args.Position, false);
                        if (result != null && result.DataPoint != null) SelectedDataPoint = (DataPoint)result.DataPoint;
                    }
                    break;
            }
        }

        private OxyColor GetMarkerColor()
        {
            return UniqueHighlight ? OxyColors.Black : OxyColors.LightGray;
        }

        private LineSeries _pointMarkers;
    }
}
