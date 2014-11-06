using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.DialogServices;
using LcmsSpectator.PlotModels;
using LcmsSpectator.TaskServices;
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
        public string RawFileName { get; set; }
        public ILcMsRun Lcms { get; set; }
        public RelayCommand SetScanChangedCommand { get; private set; }
        public RelayCommand SaveAsImageCommand { get; private set; }
        public bool Heavy { get; private set; }
        public event EventHandler XicPlotChanged;
        public class SelectedScanChangedMessage : NotificationMessage
        {
            public int Scan { get; private set; }
            public SelectedScanChangedMessage(object sender, int scan, string notification="SelectedScanChanged") : base(sender, notification)
            {
                Scan = scan;
            }
        }
        public XicPlotViewModel(IDialogService dialogService, ITaskService taskService, string title, LinearAxis xAxis, bool heavy, bool showLegend=true)
        {
            _taskService = taskService;
            _dialogService = dialogService;
            _title = title;
            _showLegend = showLegend;
            _xAxis = xAxis;
            Heavy = heavy;
            PlotTitle = _title;
            SetScanChangedCommand = new RelayCommand(SetSelectedRt);
            SaveAsImageCommand = new RelayCommand(SaveAsImage);
            _pointsToSmooth = IcParameters.Instance.PointsToSmooth;
            Plot = new SelectablePlotModel(xAxis, 1.05);
            _ions = new List<LabeledIonViewModel>();
            _smoothedXics = new List<LabeledXic>();
            _xAxis.AxisChanged += AxisChanged_UpdatePlotTitle;
            _xicCache = new ConcurrentDictionary<string, LabeledXic>();
            _smoothedXicLock = new Mutex();
            //_xicCacheLock = new Mutex();
            Messenger.Default.Register<PropertyChangedMessage<int>>(this, SelectedScanChanged);
            Messenger.Default.Register<PropertyChangedMessage<bool>>(this, LabeledIonSelectedChanged);
            UpdatePlot();
        }

        public SelectablePlotModel Plot
        {
            get { return _plot; }
            private set
            {
                _plot = value;
                RaisePropertyChanged();
            }
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
                RaisePropertyChanged();
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
                RaisePropertyChanged();
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
                _taskService.Enqueue(() => _smoothedXics.Clear());
                UpdatePlot();
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Ions to generate XICs from
        /// </summary>
        public List<LabeledIonViewModel> Ions
        {
            get { return _ions; }
            set
            {
                _ions = value;
                UpdatePlot();
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Retention time currently selected in the plot.
        /// Sets a ordinary point marker at this point.
        /// </summary>
        public double SelectedRt
        {
            get { return _selectedRt;  }
            private set
            {
                _selectedRt = value;
                RaisePropertyChanged();
            }
        }

        public double Area
        {
            get { return _area; }
            set
            {
                _area = value;
                PlotTitle = GetPlotTitleWithArea(_area);
                RaisePropertyChanged();
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
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Highlight Rt and call SelectedScanChanged to inform XicViewModel
        /// </summary>
        public void SetSelectedRt()
        {
            _taskService.Enqueue(() =>
            {
                var dataPoint = Plot.SelectedDataPoint as XicDataPoint;
                if (dataPoint == null) return;
                SelectedRt = Plot.SelectedDataPoint.X;
                SelectedPrSmViewModel.Instance.Lcms = Lcms;
                SelectedPrSmViewModel.Instance.RawFileName = RawFileName;
                SelectedPrSmViewModel.Instance.Heavy = Heavy;
                SelectedPrSmViewModel.Instance.Scan = dataPoint.ScanNum;
            });
        }

        public void HighlightRt(double rt)
        {
            _taskService.Enqueue(() => Plot.SetOrdinaryPointMarker(rt));
        }

        private void SelectedScanChanged(PropertyChangedMessage<int> message)
        {
            if (message.PropertyName == "Scan" && message.Sender == SelectedPrSmViewModel.Instance)
            {
                var rt = Lcms.GetElutionTime(message.NewValue);
                _selectedRt = rt;
                if (SelectedPrSmViewModel.Instance.Lcms == Lcms && SelectedPrSmViewModel.Instance.Heavy == Heavy)
                {
                    _taskService.Enqueue(() => Plot.SetUniquePointMarker(rt));
                }
                else _taskService.Enqueue(() => Plot.SetOrdinaryPointMarker(rt));
            }
        }

        private void ToggleScanMarkers(bool value)
        {
            _taskService.Enqueue(() =>
            {
                var markerType = (value) ? MarkerType.Circle : MarkerType.None;
                foreach (var series in Plot.Series)     // Turn markers of on every line series in plot
                {
                    if (series is XicSeries)
                    {
                        var lineSeries = series as XicSeries;
                        lineSeries.MarkerType = markerType;
                    }
                }
                Plot.InvalidatePlot(false); 
            });
        }

        private void ToggleLegend(bool value)
        {
            _taskService.Enqueue(() =>
            {
                Plot.IsLegendVisible = value;
                Plot.InvalidatePlot(false); 
            });
        }

        /// <summary>
        /// Update plot title when XAxis axis changed to reflect updated area of the currently
        /// visible portion of the plot.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AxisChanged_UpdatePlotTitle(object sender, AxisChangedEventArgs e)
        {
            if (Plot == null || Plot.Series.Count == 0) return;
            UpdateArea();
        }

        public void ClearCache()
        {
            _taskService.Enqueue(() =>
            {
                _xicCache.Clear();
                _smoothedXics.Clear(); 
            });
        }

        public void UpdatePlot()
        {
            _taskService.Enqueue(() =>
            { 
                Plot = GeneratePlot();
                if (XicPlotChanged != null) XicPlotChanged(this, EventArgs.Empty);
            });
        }

        public void UpdateArea()
        {
            _taskService.Enqueue(() => { Area = GetCurrentArea();  });
        }

        public Task<double> GetAreaTask()
        {
            return Task.Run(() => GetCurrentArea());
        } 

        public double GetCurrentArea(SelectablePlotModel plot=null)
        {
            if (plot == null) plot = Plot;
            var min = _xAxis.ActualMinimum;
            var max = _xAxis.ActualMaximum;
            var area = 0.0;
            foreach (var series in plot.Series)
            {
                var xicSeries = series as XicSeries;
                if (xicSeries == null || !xicSeries.IsVisible) continue;
                foreach (var point in xicSeries.Points)
                {
                    var xicPoint = point as XicDataPoint;
                    if (xicPoint == null) continue;
                    if (xicPoint.Index >= 0 && xicPoint.X >= min && xicPoint.X <= max) area += xicPoint.Y;
                }
            }
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
            if (Ions == null || Ions.Count == 0) return plot;
            var seriesstore = Ions.ToDictionary<LabeledIonViewModel, string, LineSeries>(ion => ion.LabeledIon.Label, ion => null);
            var smoother = (PointsToSmooth == 0 || PointsToSmooth == 1) ?
                null : new SavitzkyGolaySmoother(PointsToSmooth, 2);
            var colors = new ColorDictionary(Math.Min(Math.Max(SelectedPrSmViewModel.Instance.Charge - 1, 2), 15));
            Parallel.ForEach(Ions, ion =>
            {
                var lxic = GetXic(ion.LabeledIon);
                var xic = lxic.Xic;
                if (xic == null) return;
                // smooth
                if (smoother != null) xic = IonUtils.SmoothXic(smoother, xic).ToList();
                _smoothedXicLock.WaitOne();
                _smoothedXics.Add(new LabeledXic(ion.LabeledIon.Composition, ion.LabeledIon.Index, xic, ion.LabeledIon.IonType, ion.LabeledIon.IsFragmentIon));
                _smoothedXicLock.ReleaseMutex();
                var color = colors.GetColor(lxic);
                var markerType = (_showScanMarkers) ? MarkerType.Circle : MarkerType.None;
                var lineStyle = (!lxic.IsFragmentIon && lxic.Index == -1) ? LineStyle.Dash : LineStyle.Solid;
                // create line series for xic
                var series = new XicSeries
                {
                    Label = lxic.Label,
                    StrokeThickness = 1.5,
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
                            xic[i].Intensity, ion.LabeledIon.Index));
                }
                seriesstore[ion.LabeledIon.Label] = series;
            });
            foreach (var ion in seriesstore) plot.Series.Insert(0, ion.Value);
            plot.GenerateYAxis("Intensity", "0e0");
            plot.IsLegendVisible = _showLegend;
            plot.UniqueHighlight = (Plot != null) && Plot.UniqueHighlight;
            plot.SetPointMarker(SelectedRt);
            Area = GetCurrentArea(plot);
            return plot;
        }

        private LabeledXic GetXic(LabeledIon label)
        {
            LabeledXic lxic;
            //_xicCacheLock.WaitOne();
            if (_xicCache.ContainsKey(label.Label)) lxic = _xicCache[label.Label];
            else
            {
                //_xicCacheLock.ReleaseMutex();
                var ion = label.Ion;
                Xic xic;
                if (label.IsFragmentIon) xic = Lcms.GetFullProductExtractedIonChromatogram(ion.GetMostAbundantIsotopeMz(),
                                                                                            IcParameters.Instance.ProductIonTolerancePpm,
                                                                                            label.PrecursorIon.GetMostAbundantIsotopeMz());
                else if (label.IsChargeState) xic = Lcms.GetFullPrecursorIonExtractedIonChromatogram(ion.GetMostAbundantIsotopeMz(), 
                                                                                                     IcParameters.Instance.PrecursorTolerancePpm);
                else xic = Lcms.GetFullPrecursorIonExtractedIonChromatogram(ion.GetIsotopeMz(label.Index), IcParameters.Instance.PrecursorTolerancePpm);
                lxic = new LabeledXic(label.Composition, label.Index, xic, label.IonType, label.IsFragmentIon, label.PrecursorIon, label.IsChargeState);
                //_xicCacheLock.WaitOne();
                _xicCache.AddOrUpdate(label.Label, lxic, (key, oldValue) => lxic);
            }
            //_xicCacheLock.ReleaseMutex();
            return lxic;
        }

        private void LabeledIonSelectedChanged(PropertyChangedMessage<bool> message)
        {
            if (message.PropertyName == "Selected" && message.Sender is LabeledIonViewModel)
            {
                var labeledIonVm = message.Sender as LabeledIonViewModel;
                var label = labeledIonVm.LabeledIon.Label;
                foreach (var series in Plot.Series)
                {
                    var lineSeries = series as LineSeries;
                    if (lineSeries != null && lineSeries.Title == label)
                    {
                        lineSeries.IsVisible = message.NewValue;
                        Plot.AdjustForZoom();
                        _taskService.Enqueue(() => { PlotTitle = GetPlotTitleWithArea(GetCurrentArea()); });
                    }
                }
            }
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
        private readonly ITaskService _taskService;

        private readonly string _title;
        private bool _showLegend;
        private bool _showScanMarkers;
        private double _selectedRt;
        private readonly LinearAxis _xAxis;
        private string _plotTitle;
        private List<LabeledIonViewModel> _ions;
        private int _pointsToSmooth;

        private readonly Mutex _smoothedXicLock;
        private readonly List<LabeledXic> _smoothedXics;
        private readonly ConcurrentDictionary<string, LabeledXic> _xicCache;
        //private readonly Mutex _xicCacheLock;
        private SelectablePlotModel _plot;
        private double _area;
    }
}
