using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using LcmsSpectator.PlotModels;
using LcmsSpectator.Utils;
using LcmsSpectatorModels.Models;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace LcmsSpectator.ViewModels
{
    public class XicPlotViewModel: ViewModelBase
    {
        public SelectablePlotModel Plot { get; set; }
        public DelegateCommand SetScanChangedCommand { get; set; }
        public bool Heavy { get; set; }
        public event EventHandler SelectedScanChanged;
        public XicPlotViewModel(string title, ColorDictionary colors, LinearAxis xAxis, bool heavy, bool showMarker=true, bool showLegend=true)
        {
            _title = title;
            _colors = colors;
            _showLegend = showLegend;
            _xAxis = xAxis;
            _xAxis.AxisChanged += UpdatePlotTitle;
            Heavy = heavy;
            _showMarker = showMarker;
            SetScanChangedCommand = new DelegateCommand(SetSelectedScan);
            Xics = new List<LabeledXic>();
        }

        private void UpdatePlotTitle(object sender, AxisChangedEventArgs e)
        {
            if (Plot != null && _xAxis != null)
            {
                Task.Factory.StartNew(() =>
                {
                    var min = _xAxis.ActualMinimum;
                    var max = _xAxis.ActualMaximum;
                    var areaStr = String.Format(CultureInfo.InvariantCulture, "{0:0.##E0}", GetAreaOfRange((int)min, (int)max));
                    var newTitle = String.Format("{0} (Area: {1})", _title, areaStr);
                    GuiInvoker.Invoke(() => { Plot.Title = newTitle; });
                });
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
                OnPropertyChanged("Xics");
            }
        }

        public int SelectedScan
        {
            get { return _selectedScan;  }
            set
            {
                _selectedScan = value;
                if (_showMarker) Plot.SetOrdinaryPointMarker(_selectedScan);
                Plot.AdjustForZoom();
                OnPropertyChanged("SelectedScan");
            }
        }

        public double GetAreaOfRange(int min, int max)
        {
            return (from lxic in Xics from point in lxic.Xic where point.ScanNum >= min && point.ScanNum <= max select point.Intensity).Sum();
        }

        public double Area
        {
            get
            {
                var area = Xics.Where(lxic => lxic.Index >= 0).Sum(lxic => lxic.Area);
                return Math.Round(area, 2);
            }
        }

        public void HighlightScan(int scanNum, bool unique)
        {
            _selectedScan = scanNum;
            if (unique) Plot.SetUniquePointMarker(scanNum);
        }

        public void SetSelectedScan()
        {
            _selectedScan = (int) Plot.SelectedDataPoint.X;
            if (SelectedScanChanged != null) SelectedScanChanged(this, null);
            Plot.SetUniquePointMarker(SelectedScan);
        }

        private void GeneratePlot()
        {
            // add XICs
            if (Xics == null) return;
            var plot = new SelectablePlotModel(_xAxis, 1.05);
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
                plot.Series.Add(series);
            }
            var areaStr = String.Format(CultureInfo.InvariantCulture, "{0:0.##E0}", Area);
            plot.Title = String.Format("{0} (Area: {1})", _title, areaStr);
            plot.GenerateYAxis("Intensity", "0e0");
            plot.IsLegendVisible = _showLegend;
            plot.UniqueHighlight = (Plot != null) && Plot.UniqueHighlight;
            if (_showMarker) plot.SetPointMarker(SelectedScan);
            Plot = plot;
            OnPropertyChanged("Plot");
        }

        private readonly string _title;
        private readonly bool _showLegend;
        private List<LabeledXic> _xics;
        private readonly ColorDictionary _colors;
        private bool _showScanMarkers;
        private readonly bool _showMarker;
        private int _selectedScan;
        private readonly LinearAxis _xAxis;
    }
}
