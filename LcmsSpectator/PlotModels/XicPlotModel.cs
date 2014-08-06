using System;
using System.Collections.Generic;
using System.Linq;
using LcmsSpectatorModels.Models;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace LcmsSpectator.PlotModels
{
    public class XicPlotModel: SelectablePlotModel
    {
        private readonly string _title;
        private readonly ColorDictionary _colors;
        private readonly bool _showScanMarkers;
        public XicPlotModel(string title, Axis xAxis, IEnumerable<LabeledXic> xics, ColorDictionary colors, bool showScanMarkers, bool showLegend, double mult=1.05): base(xAxis, mult)
        {
            _title = title;
            _colors = colors;
            _showScanMarkers = showScanMarkers;
            _showLegend = showLegend;
            GeneratePlot(xics);
        }

        public XicPlotModel(): base(new LinearAxis(), 0)
        {
            _title = "";
            _colors = new ColorDictionary(2);
            _showScanMarkers = false;
        }

        private void GeneratePlot(IEnumerable<LabeledXic> xics)
        {
            // add XICs
            if (xics == null) return;
            var labeledXics = xics as LabeledXic[] ?? xics.ToArray();
            foreach (var lxic in labeledXics)
            {
                var xic = lxic.Xic;
                if (xic == null) continue;
                var color = _colors.GetColor(lxic);
                var markerType = (_showScanMarkers) ? MarkerType.Circle : MarkerType.None;
                var lineStyle = (!lxic.IsFragmentIon && lxic.Index == -1) ? LineStyle.Dash : LineStyle.Solid;
                var series = new LineSeries
                {
                    StrokeThickness = 3,
                    Title = lxic.Label,
                    Color = color,
                    LineStyle = lineStyle,
                    MarkerType = markerType,
                    MarkerSize = 3,
                    MarkerStroke = color,
                    MarkerStrokeThickness = 1,
                    MarkerFill = OxyColors.White
                };
                // Add XIC points
                for (int i = 0; i < xic.Count; i++)
                {
                    var xicPoint = xic[i];
                    // remove plateau points (line will connect them anyway)
                    if (i > 1 && i < xic.Count - 1 && xic[i - 1].Intensity.Equals(xic[i].Intensity) && xic[i + 1].Intensity.Equals(xic[i].Intensity)) continue;
                    series.Points.Add(new DataPoint(xicPoint.ScanNum, xicPoint.Intensity));
                }
                Series.Add(series);
            }

            var area = labeledXics.Where(lxic => lxic.Index >= 0).Sum(lxic => lxic.Area);
            Title = String.Format("{0} (Area: {1})", _title, area);
            GenerateYAxis("Intensity", "0e0");
            IsLegendVisible = _showLegend;   
        }

        private readonly bool _showLegend;
    }
}
