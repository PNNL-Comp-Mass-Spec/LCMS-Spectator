using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace LcmsSpectator.PlotModels
{
    public class AutoAdjustedYPlotModel: PlotModel
    {
        public AutoAdjustedYPlotModel(Axis xAxis, double multiplier)
        {
            Multiplier = multiplier;
            Axes.Add(xAxis);
            XAxis = xAxis;
            YAxis = new LinearAxis();
            xAxis.AxisChanged += XAxisChanged;
        }

        public virtual void GenerateYAxis(string title, string format)
        {
            var maxY = GetMaxYInRange(XAxis.ActualMinimum, XAxis.ActualMaximum);
            var absoluteMaxY = GetMaxYInRange(0, XAxis.AbsoluteMaximum);
            YAxis = new LinearAxis(AxisPosition.Left, title)
            {
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

        public virtual void AdjustForZoom()
        {
            var minX = XAxis.ActualMinimum;
            var maxX = XAxis.ActualMaximum;
            SetBounds(minX, maxX);
        }

        public virtual void SetBounds(double minX, double maxX)
        {
            var maxY = GetMaxYInRange(minX, maxX);
            var yaxis = DefaultYAxis ?? YAxis;
            yaxis.Maximum = maxY * Multiplier;
            InvalidatePlot(false);
        }

        protected double GetMaxYInRange(double minX, double maxX)
        {
            var maxY = 0.0;
            foreach (var series in Series)
            {
                var lSeries = series as DataPointSeries;
                if (lSeries == null) continue;
                foreach (var point in lSeries.Points)
                {
                    if (point.Y >= maxY && point.X >= minX && point.X <= maxX) maxY = point.Y;
                }
            }
            return maxY;
        }
        protected Axis YAxis;
        protected Axis XAxis;
        protected double Multiplier;

        private void XAxisChanged(object sender, AxisChangedEventArgs e)
        {
            AdjustForZoom();
        }

    }
}
