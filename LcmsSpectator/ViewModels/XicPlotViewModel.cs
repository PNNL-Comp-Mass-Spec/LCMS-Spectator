using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LcmsSpectator.PlotModels;
using LcmsSpectator.Utils;
using LcmsSpectatorModels.Models;
using OxyPlot;
using OxyPlot.Series;

namespace LcmsSpectator.ViewModels
{
    public class XicPlotViewModel: ViewModelBase
    {
        public AutoAdjustedYPlotModel Plot { get; set; }

        public XicPlotViewModel(ColorDictionary colors, bool showLegend=true)
        {
            _colors = colors;
            _showLegend = showLegend;
        }

        public string Title
        {
            get { return _title; }
            set
            {
                if (_title == value) return;
                _title = value;
                Task.Factory.StartNew(GeneratePlot);
                OnPropertyChanged("Title");
            }
        }

        public bool ShowScanMarkers
        {
            get { return _showScanMarkers; }
            set
            {
                if (_showScanMarkers == value) return;
                _showScanMarkers = value;
                Task.Factory.StartNew(GeneratePlot);
                OnPropertyChanged("ShowScanMarkers");
            }
        }

        public List<LabeledXic> Xics
        {
            get { return _xics; }
            set
            {
                _xics = value;
                Task.Factory.StartNew(GeneratePlot);
                OnPropertyChanged("Ions");
            }
        }

        public double Area
        {
            get { return Xics.Where(lxic => lxic.Index >= 0).Sum(lxic => lxic.Area); }
        }

        private void GeneratePlot()
        {
            // add XICs
            if (Xics == null) return;
            foreach (var lxic in Xics)
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
                GuiInvoker.Invoke(Plot.Series.Add, series);
            }

            Plot.Title = String.Format("{0} (Area: {1})", _title, Area);
            Plot.GenerateYAxis("Intensity", "0e0");
            GuiInvoker.Invoke(() => { Plot.IsLegendVisible = _showLegend; });
        }

        private string _title;
        private readonly bool _showLegend;
        private List<LabeledXic> _xics;
        private readonly ColorDictionary _colors;
        private bool _showScanMarkers;
    }
}
