using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using LcmsSpectator.PlotModels;
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
            Heavy = heavy;
            PlotTitle = _title;
            _showMarker = showMarker;
            SetScanChangedCommand = new DelegateCommand(SetSelectedRt);
            Xics = new List<LabeledXic>();
            _xAxis.AxisChanged += UpdatePlotTitle;
        }

        /// <summary>
        /// Shows and hides the point markers on the XIC plots.
        /// Regenerates the plot with or without markers.
        /// </summary>
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

        /// <summary>
        /// Xics to generate plot from.
        /// </summary>
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

        /// <summary>
        /// Scan number currently selected in the plot.
        /// </summary>
        public int SelectedScan
        {
            get { return _selectedScan; }
            private set
            {
                _selectedScan = value;
                OnPropertyChanged("SelectedScan");
            }
        }

        /// <summary>
        /// Retention time currently selected in the plot.
        /// Sets a ordinary point marker at this point.
        /// </summary>
        public double SelectedRt
        {
            get { return _selectedRt;  }
            set
            {
                _selectedRt = value;
                if (_showMarker) Plot.SetOrdinaryPointMarker(_selectedRt);
                Plot.AdjustForZoom();
                OnPropertyChanged("SelectedRt");
            }
        }

        /// <summary>
        /// Title of plot, including area
        /// </summary>
        public string PlotTitle
        {
            get { return _plotTitle; }
            set
            {
                _plotTitle = value;
                OnPropertyChanged("PlotTitle");
            }
        }

        /// <summary>
        /// Calculate area under curve of XIC in range.
        /// </summary>
        /// <param name="min">Min x limit</param>
        /// <param name="max">Max x limit</param>
        /// <returns>Area under the curve of the range</returns>
        public double GetAreaOfRange(double min, double max)
        {
            return (from lxic in Xics where lxic.Index >= 0
                    from point in lxic.Xic 
                        where point.RetentionTime >= min && point.RetentionTime <= max 
                        select point.Intensity).Sum();
        }

        /// <summary>
        /// Highlight retention time in plot
        /// </summary>
        /// <param name="rtTime">Retention time to highlight</param>
        /// <param name="unique">Is it a unique marker (highlighted different color)</param>
        public void HighlightRt(double rtTime, bool unique)
        {
            _selectedRt = rtTime;
            if (unique) Plot.SetUniquePointMarker(rtTime);
            else Plot.SetOrdinaryPointMarker(rtTime);
        }

        /// <summary>
        /// Highlight Rt and call SelectedScanChanged to inform XicViewModel
        /// </summary>
        public void SetSelectedRt()
        {
            var dataPoint = Plot.SelectedDataPoint as XicDataPoint;
            _selectedRt = Plot.SelectedDataPoint.X;
            if (dataPoint != null) SelectedScan = dataPoint.ScanNum;
            if (SelectedScanChanged != null) SelectedScanChanged(this, null);
            Plot.SetUniquePointMarker(SelectedRt);
        }

        private void UpdatePlotTitle(object sender, AxisChangedEventArgs e)
        {
            if (Plot != null && _xAxis != null)
            {
                Task.Factory.StartNew(() =>
                {
                    var title = GetPlotTitleWithArea();
                    PlotTitle = title;
                });
            }
        }

        private string GetPlotTitleWithArea()
        {
            string title;
            if (Plot != null && _xAxis != null)
            {
                var min = _xAxis.ActualMinimum;
                var max = _xAxis.ActualMaximum;
                var areaStr = String.Format(CultureInfo.InvariantCulture, "{0:0.##E0}", GetAreaOfRange(min, max));
                title = String.Format("{0} (Area: {1})", _title, areaStr);
            }
            else title = _title;
            return title;
        }

        private void GeneratePlot()
        {
            // add XICs
            if (Xics == null) return;
            var plot = new SelectablePlotModel(_xAxis, 1.05)
            {
                TitleFontSize = 14,
                TitlePadding = 0,
            };
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
                    MarkerFill = OxyColors.White,
                    TrackerFormatString = 
                            "{0}" + Environment.NewLine +
                            "{1}: {2:0.###}" + Environment.NewLine +
                            "Scan #: {ScanNum}" + Environment.NewLine +
                            "{3}: {4:0.##E0}"
                };
                // Add XIC points
                for (int i = 0; i < xic.Count; i++)
                {
                    // remove plateau points (line will connect them anyway)
                    if (i > 1 && i < xic.Count - 1 && xic[i - 1].Intensity.Equals(xic[i].Intensity) && xic[i + 1].Intensity.Equals(xic[i].Intensity)) continue;
                    if (xic[i] != null) series.Points.Add(new XicDataPoint(xic[i].RetentionTime, xic[i].ScanNum, xic[i].Intensity));
                }
                plot.Series.Add(series);
            }
            plot.GenerateYAxis("Intensity", "0e0");
            plot.IsLegendVisible = _showLegend;
            plot.UniqueHighlight = (Plot != null) && Plot.UniqueHighlight;
            if (_showMarker) plot.SetPointMarker(SelectedRt);
            Plot = plot;
            PlotTitle = GetPlotTitleWithArea();
            OnPropertyChanged("Plot");
        }

        private readonly string _title;
        private readonly bool _showLegend;
        private List<LabeledXic> _xics;
        private readonly ColorDictionary _colors;
        private bool _showScanMarkers;
        private readonly bool _showMarker;
        private double _selectedRt;
        private readonly LinearAxis _xAxis;
        private int _selectedScan;
        private string _plotTitle;
    }
}
