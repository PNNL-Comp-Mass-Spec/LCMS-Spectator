using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.DialogServices;
using LcmsSpectator.PlotModels;
using LcmsSpectatorModels.Config;
using LcmsSpectatorModels.Models;
using LcmsSpectatorModels.Utils;
using MultiDimensionalPeakFinding;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Wpf;
using LinearAxis = OxyPlot.Axes.LinearAxis;
using LineSeries = OxyPlot.Series.LineSeries;

namespace LcmsSpectator.ViewModels
{
    public class XicPlotViewModel: ViewModelBase
    {
        public SelectablePlotModel Plot { get; private set; }
        public Task<SelectablePlotModel> PlotTask { get; private set; }
        public Task<double> AreaTask { get; private set; }
        public ILcMsRun Lcms { get; set; }
        public DelegateCommand SetScanChangedCommand { get; private set; }
        public DelegateCommand SaveAsImageCommand { get; private set; }
        public bool Heavy { get; private set; }
        public event EventHandler PlotChanged;
        public event EventHandler SelectedScanChanged;
        public XicPlotViewModel(IDialogService dialogService, string title, ColorDictionary colors, LinearAxis xAxis, bool heavy, bool showLegend=true)
        {
            _dialogService = dialogService;
            _title = title;
            _colors = colors;
            _showLegend = showLegend;
            _xAxis = xAxis;
            Heavy = heavy;
            PlotTitle = _title;
            SetScanChangedCommand = new DelegateCommand(SetSelectedRt);
            SaveAsImageCommand = new DelegateCommand(SaveAsImage);
            _pointsToSmooth = IcParameters.Instance.PointsToSmooth;
            Plot = new SelectablePlotModel(xAxis, 1.05);
            _ions = new List<LabeledIon>();
            _smoothedXics = new List<LabeledXic>();
            _xAxis.AxisChanged += AxisChanged_UpdatePlotTitle;
            _xicCache = new Dictionary<string, LabeledXic>();
            _xicCacheLock = new Mutex();
            _smoothedXicLock = new Mutex();
            UpdatePlot();
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
                ToggleScanMarkers(value);
                OnPropertyChanged("ShowScanMarkers");
            }
        }

        /// <summary>
        /// Shows or hides the legend for the plot.
        /// </summary>
        public bool ShowLegend
        {
            get { return _showLegend; }
            set
            {
                if (_showLegend == value) return;
                _showLegend = value;
                ToggleLegend(value);
                OnPropertyChanged("ShowLegend");
            }
        }

        /// <summary>
        /// Property for setting the smoothing window size
        /// </summary>
        public int PointsToSmooth
        {
            get { return _pointsToSmooth; }
            set
            {
                _pointsToSmooth = value;
                _smoothedXicLock.WaitOne();
                _smoothedXics.Clear();
                _smoothedXicLock.ReleaseMutex();
                UpdatePlot();
                OnPropertyChanged("PointsToSmooth");
            }
        }

        /// <summary>
        /// Ions to generate XICs from
        /// </summary>
        public List<LabeledIon> Ions
        {
            get { return _ions; }
            set
            {
                _ions = value;
                UpdatePlot();
                OnPropertyChanged("Ions");
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
                SetSelectedRt(_selectedRt);
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
        /// Highlight retention time in plot
        /// </summary>
        /// <param name="rtTime">Retention time to highlight</param>
        /// <param name="unique">Is it a unique marker (highlighted different color)</param>
        public async void HighlightRt(double rtTime, bool unique)
        {
            if (!PlotTask.IsCompleted) await PlotTask;
            if (Plot == null || Plot.Series.Count == 0) return;
            _selectedRt = rtTime;
            if (unique) Plot.SetUniquePointMarker(rtTime);
            else Plot.SetOrdinaryPointMarker(rtTime);
        }

        /// <summary>
        /// Highlight Rt and call SelectedScanChanged to inform XicViewModel
        /// </summary>
        public async void SetSelectedRt()
        {
            if (!PlotTask.IsCompleted) await PlotTask;
            if (Plot == null || Plot.Series.Count == 0) return;
            var dataPoint = Plot.SelectedDataPoint as XicDataPoint;
            _selectedRt = Plot.SelectedDataPoint.X;
            if (dataPoint != null) SelectedScan = dataPoint.ScanNum;
            if (SelectedScanChanged != null) SelectedScanChanged(this, null);
            Plot.SetUniquePointMarker(SelectedRt);
        }

        private async void SetSelectedRt(double rt)
        {
            if (!PlotTask.IsCompleted) await PlotTask;
            if (Plot == null || Plot.Series.Count == 0) return;
            Plot.SetOrdinaryPointMarker(rt);
            Plot.AdjustForZoom();
        }

        private async void ToggleScanMarkers(bool value)
        {
            if (!PlotTask.IsCompleted) await PlotTask;
            if (Plot == null || Plot.Series.Count == 0) return;
            var markerType = (value) ? MarkerType.Circle : MarkerType.None;
            foreach (var series in Plot.Series)     // Turn markers of on every line series in plot
            {
                if (series is LineSeries)
                {
                    var lineSeries = series as LineSeries;
                    lineSeries.MarkerType = markerType;
                }
            }
            Plot.InvalidatePlot(false);
        }

        private async void ToggleLegend(bool value)
        {
            if (!PlotTask.IsCompleted) await PlotTask;
            Plot.IsLegendVisible = value;
            Plot.InvalidatePlot(false);
        }

        /// <summary>
        /// Update plot title when XAxis axis changed to reflect updated area of the currently
        /// visible portion of the plot.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void AxisChanged_UpdatePlotTitle(object sender, AxisChangedEventArgs e)
        {
            if (!PlotTask.IsCompleted) await PlotTask;
            if (Plot == null || Plot.Series.Count == 0) return;
            UpdateArea();
        }

        public void ClearCache()
        {
            _xicCacheLock.WaitOne();
            _xicCache.Clear();
            _xicCacheLock.ReleaseMutex();
            _smoothedXicLock.WaitOne();
            _smoothedXics.Clear();
            _smoothedXicLock.ReleaseMutex();
        }

        public async void UpdatePlot()
        {
            PlotTask = UpdatePlotTask();
            Plot = await PlotTask;
            OnPropertyChanged("Plot");
        }

        public Task<SelectablePlotModel> UpdatePlotTask()
        {
            return Task.Run(() => GeneratePlot());
        }

        public async void UpdateArea()
        {
            AreaTask = GetAreaTask();
            PlotTitle = GetPlotTitleWithArea(await GetAreaTask());
        }

        public Task<double> GetAreaTask()
        {
            return Task.Run(() => GetCurrentArea());
        }

        public double GetCurrentArea()
        {
            var min = _xAxis.ActualMinimum;
            var max = _xAxis.ActualMaximum;
            _smoothedXicLock.WaitOne();
            var area =  (from lxic in _smoothedXics where lxic.Index >= 0
                        from point in lxic.Xic 
                        where Lcms.GetElutionTime(point.ScanNum) >= min && Lcms.GetElutionTime(point.ScanNum) <= max 
                        select point.Intensity).Sum();
            _smoothedXicLock.ReleaseMutex();
            return area;
        }

        /// <summary>
        /// Get the plot title in the format "[Title] (Area: ###)".
        /// </summary>
        /// <returns>Plot title with area as a string.</returns>
        private string GetPlotTitleWithArea(double area)
        {
            var areaStr = String.Format(CultureInfo.InvariantCulture, "{0:0.##E0}", area);
            var title = String.Format("{0} (Area: {1})", _title, areaStr);
            return title;
        }

        /// <summary>
        /// Generate plot by smoothing XICs, creating a LineSeries for each xic, and adding it to the Plot
        /// </summary>
        private SelectablePlotModel GeneratePlot()
        {
            // add XICs
            var plot = new SelectablePlotModel(_xAxis, 1.05)
            {
                TitleFontSize = 14,
                TitlePadding = 0,
            };
            var seriesstore = Ions.ToDictionary<LabeledIon, string, LineSeries>(ion => ion.Label, ion => null);
            var smoother = (PointsToSmooth == 0 || PointsToSmooth == 1) ?
                null : new SavitzkyGolaySmoother(PointsToSmooth, 2);
            Parallel.ForEach(Ions, ion =>
            {
                var lxic = GetXic(ion);
                var xic = lxic.Xic;
                if (xic == null) return;
                // smooth
                if (smoother != null) xic = IonUtils.SmoothXic(smoother, xic).ToList();
                _smoothedXicLock.WaitOne();
                _smoothedXics.Add(new LabeledXic(ion.Composition, ion.Index, xic, ion.IonType, ion.IsFragmentIon));
                _smoothedXicLock.ReleaseMutex();
                var color = _colors.GetColor(lxic);
                var markerType = (_showScanMarkers) ? MarkerType.Circle : MarkerType.None;
                var lineStyle = (!lxic.IsFragmentIon && lxic.Index == -1) ? LineStyle.Dash : LineStyle.Solid;
                // create line series for xic
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
                    if (i > 1 && i < xic.Count - 1 && xic[i - 1].Intensity.Equals(xic[i].Intensity) &&
                        xic[i + 1].Intensity.Equals(xic[i].Intensity)) continue;
                    if (xic[i] != null)
                        series.Points.Add(new XicDataPoint(Lcms.GetElutionTime(xic[i].ScanNum), xic[i].ScanNum,
                            xic[i].Intensity));
                }
                seriesstore[ion.Label] = series;
            });
            foreach (var ion in seriesstore) plot.Series.Add(ion.Value);
            plot.GenerateYAxis("Intensity", "0e0");
            plot.IsLegendVisible = _showLegend;
            plot.UniqueHighlight = (Plot != null) && Plot.UniqueHighlight;
            plot.SetPointMarker(SelectedRt);
            if (PlotChanged != null) PlotChanged(this, EventArgs.Empty);
            PlotTitle = GetPlotTitleWithArea(GetCurrentArea());
            return plot;
        }

        private LabeledXic GetXic(LabeledIon label)
        {
            _xicCacheLock.WaitOne();
            LabeledXic lxic;
            if (_xicCache.ContainsKey(label.Label)) lxic = _xicCache[label.Label];
            else
            {
                var ion = label.Ion;
                Xic xic;
                if (label.IsFragmentIon) xic = Lcms.GetFullProductExtractedIonChromatogram(ion.GetMostAbundantIsotopeMz(),
                                                                                            IcParameters.Instance.ProductIonTolerancePpm,
                                                                                            label.PrecursorIon.GetMostAbundantIsotopeMz());
                else xic = Lcms.GetFullPrecursorIonExtractedIonChromatogram(ion.GetIsotopeMz(label.Index), IcParameters.Instance.PrecursorTolerancePpm);
                lxic = new LabeledXic(label.Composition, label.Index, xic, label.IonType, label.IsFragmentIon);
                _xicCache.Add(label.Label, lxic);
            }
            _xicCacheLock.ReleaseMutex();
            return lxic;
        }

        /// <summary>
        /// Open a save file dialog and save plot as png image to user selected location.
        /// </summary>
        private void SaveAsImage()
        {
            if (Plot == null) return;
            var fileName = _dialogService.SaveFile(".png", @"Png Files (*.png)|*.png");
            try
            {
                if (fileName != "") PngExporter.Export(Plot, fileName, (int)Plot.Width, (int)Plot.Height);
            }
            catch (Exception e)
            {
                _dialogService.ExceptionAlert(e);
            }
        }

        private readonly IDialogService _dialogService;
        private readonly string _title;
        private bool _showLegend;
        private readonly ColorDictionary _colors;
        private bool _showScanMarkers;
        private double _selectedRt;
        private readonly LinearAxis _xAxis;
        private int _selectedScan;
        private string _plotTitle;
        private List<LabeledIon> _ions;
        private int _pointsToSmooth;

        private readonly Mutex _xicCacheLock;
        private readonly Mutex _smoothedXicLock;
        private readonly List<LabeledXic> _smoothedXics;
        private readonly Dictionary<string, LabeledXic> _xicCache;
    }
}
