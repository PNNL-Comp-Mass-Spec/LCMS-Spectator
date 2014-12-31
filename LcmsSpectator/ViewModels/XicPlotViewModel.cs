using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        /// <summary>
        /// Xic Plot View Model constructor
        /// </summary>
        /// <param name="dialogService">Dialog service</param>
        /// <param name="taskService">Task scheduler</param>
        /// <param name="messenger">Messenger instance to raise property changes/events to</param>
        /// <param name="title">Title of XIC plot</param>
        /// <param name="xAxis">XAxis for XIC plo.</param>
        /// <param name="heavy">Does this XIC represent a heavy peptide?</param>
        /// <param name="showLegend">Should a legend be shown on the plot?</param>
        public XicPlotViewModel(IDialogService dialogService, ITaskService taskService, Messenger messenger, string title, LinearAxis xAxis, bool heavy, bool showLegend=true)
        {
            MessengerInstance = messenger;
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
            _xAxis.AxisChanged += AxisChanged_UpdatePlotTitle;
            _smoothedXics = new ConcurrentDictionary<string, Tuple<LabeledXic, bool>>();
            _xicCache = new ConcurrentDictionary<string, LabeledXic>();
            MessengerInstance.Register<PropertyChangedMessage<int>>(this, SelectedScanChanged);
            MessengerInstance.Register<PropertyChangedMessage<int>>(this, SelectedChargeChanged);
            MessengerInstance.Register<PropertyChangedMessage<bool>>(this, LabeledIonSelectedChanged);
            UpdatePlot();
        }

        #region Public Properties

        /// <summary>
        /// Name of data sets (without file extension or folder path)
        /// </summary>
        public string RawFileName { get; set; }

        /// <summary>
        /// LcMsRun data set
        /// </summary>
        public ILcMsRun Lcms { get; set; }

        /// <summary>
        /// Command triggered when new scan is selected (double clicked) on XIC.
        /// </summary>
        public RelayCommand SetScanChangedCommand { get; private set; }

        /// <summary>
        /// Command for exporting xic plot as an image.
        /// </summary>
        public RelayCommand SaveAsImageCommand { get; private set; }

        /// <summary>
        /// Whether or not this xic plot represents a heavy peptide/protein.
        /// </summary>
        public bool Heavy { get; private set; }

        /// <summary>
        /// Event that is triggered when the XIC plot is updated.
        /// </summary>
        public event EventHandler XicPlotChanged;

        /// <summary>
        /// Plot model for XIC
        /// </summary>
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

        /// <summary>
        /// Area under curve of XIC
        /// </summary>
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
#endregion

        #region Public Methods
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
                MessengerInstance.Send(new SelectedScanChangedMessage(this, dataPoint.ScanNum));
            });
        }

        /// <summary>
        /// Highlight (place marker at) retention time.
        /// </summary>
        /// <param name="rt">Retention time to put marker at.</param>
        public void HighlightRt(double rt)
        {
            _taskService.Enqueue(() => Plot.SetOrdinaryPointMarker(rt));
        }

        /// <summary>
        /// Clear XIC cache
        /// </summary>
        public void ClearCache()
        {
            _taskService.Enqueue(() =>
            {
                _xicCache.Clear();
                _smoothedXics.Clear();
            });
        }

        /// <summary>
        /// Start update of XIC plot.
        /// </summary>
        public void UpdatePlot()
        {
            _taskService.Enqueue(() =>
            { 
                Plot.ClearAxes();
                Plot = GeneratePlot();
                Plot.InvalidatePlot(true);
                if (XicPlotChanged != null) XicPlotChanged(this, EventArgs.Empty);
            });
        }

        /// <summary>
        /// Start update of plot title with current area.
        /// </summary>
        public void UpdateArea()
        {
            _taskService.Enqueue(() => { Area = GetCurrentArea();  });
        }

        /// <summary>
        /// Get an awaitable task for XIC area.
        /// </summary>
        /// <returns></returns>
        public Task<double> GetAreaTask()
        {
            return Task.Run(() => GetCurrentArea());
        } 

        /// <summary>
        /// Get current area under curve of XIC plot
        /// </summary>
        /// <returns></returns>
        public double GetCurrentArea()
        {
            var min = _xAxis.ActualMinimum;
            var max = _xAxis.ActualMaximum;
            var area = 0.0;
            var xics = _smoothedXics.Values;
            foreach (var val in xics)
            {
                var lXic = val.Item1;
                var xicPoints = lXic.Xic;
                if (lXic.Index < 0 || !val.Item2) continue;
                area += (from xicPoint in xicPoints
                         let rt = Lcms.GetElutionTime(xicPoint.ScanNum)
                         where rt >= min && rt <= max
                         select xicPoint.Intensity).Sum();
            }
            return area;
        }
#endregion

        #region Event Handlers
        private void SelectedScanChanged(PropertyChangedMessage<int> message)
        {
            if (message.PropertyName == "Scan" && message.Sender is PrSmViewModel)
            {
                var rt = Lcms.GetElutionTime(message.NewValue);
                _selectedRt = rt;
                /*if (SelectedPrSmViewModel.Instance.Heavy == Heavy)
                {
                    _taskService.Enqueue(() => Plot.SetUniquePointMarker(rt));
                }
                else _taskService.Enqueue(() => Plot.SetOrdinaryPointMarker(rt));*/
                _taskService.Enqueue(() => Plot.SetUniquePointMarker(rt));
            }
        }

        /// <summary>
        /// Captures when the data set's selected charge has changed.
        /// </summary>
        /// <param name="message">Event message containing new charge.</param>
        private void SelectedChargeChanged(PropertyChangedMessage<int> message)
        {
            if (message.PropertyName == "Charge")
            {
                _selectedCharge = message.NewValue;
            }
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

        /// <summary>
        /// Ion label has been selected/unselected
        /// </summary>
        /// <param name="message">Event message containing whether or not sender is selected.</param>
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
                        var lXic = _smoothedXics[label].Item1;
                        var value = new Tuple<LabeledXic, bool>(lXic, message.NewValue);
                        _smoothedXics.AddOrUpdate(label, value, (key, oldValue) => value);
                        Plot.AdjustForZoom();
                        _taskService.Enqueue(() => { PlotTitle = GetPlotTitleWithArea(GetCurrentArea()); });
                    }
                }
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Begin toggling scan markers on xic plot on/off.
        /// The scan markers are the markers at each point in each series.
        /// </summary>
        /// <param name="value">Whether the scan markers are on or off</param>
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

        /// <summary>
        /// Begin toggling legend on xic plot on/off.
        /// </summary>
        /// <param name="value">Whether the legend is on or off</param>
        private void ToggleLegend(bool value)
        {
            _taskService.Enqueue(() =>
            {
                Plot.IsLegendVisible = value;
                Plot.InvalidatePlot(false);
            });
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
            Plot.Axes.Clear();
            var plot = new SelectablePlotModel(_xAxis, 1.05)
            {
                TitleFontSize = 14,
                TitlePadding = 0,
            };
            if (Ions == null || Ions.Count == 0) return plot;
            var seriesstore = Ions.ToDictionary<LabeledIonViewModel, string, Tuple<LineSeries, List<XicDataPoint>>>(ion => ion.LabeledIon.Label, ion => null);
            var smoother = (PointsToSmooth == 0 || PointsToSmooth == 1) ?
                null : new SavitzkyGolaySmoother(PointsToSmooth, 2);
            var colors = new ColorDictionary(Math.Min(Math.Max(_selectedCharge - 1, 2), 15));
            Parallel.ForEach(Ions, ion =>
            {
                var lxic = GetXic(ion.LabeledIon);
                var xic = lxic.Xic;
                if (xic == null) return;
                // smooth
                if (smoother != null) xic = IonUtils.SmoothXic(smoother, xic);
                var labeledXic = new LabeledXic(ion.LabeledIon.Composition, ion.LabeledIon.Index, xic,
                                                ion.LabeledIon.IonType, ion.LabeledIon.IsFragmentIon);
                var value = new Tuple<LabeledXic, bool>(labeledXic, ion.Selected);
                _smoothedXics.AddOrUpdate(ion.LabeledIon.Label, value, (key, oldValue) => value);   
                var color = colors.GetColor(lxic);
                var markerType = (_showScanMarkers) ? MarkerType.Circle : MarkerType.None;
                var lineStyle = (!lxic.IsFragmentIon && lxic.Index == -1) ? LineStyle.Dash : LineStyle.Solid;
                var xicPoints = new List<XicDataPoint>();
                for (int i = 0; i < xic.Length; i++)
                {
                    // remove plateau points (line will connect them anyway)
                    if (i > 1 && i < xic.Length - 1 && xic[i - 1].Intensity.Equals(xic[i].Intensity) &&
                        xic[i + 1].Intensity.Equals(xic[i].Intensity)) continue;
                    xicPoints.Add(new XicDataPoint(Lcms.GetElutionTime(xic[i].ScanNum), xic[i].ScanNum, xic[i].Intensity, ion.LabeledIon.Index));
                }
                // create line series for xic
                var series = new LineSeries
                {
                    ItemsSource = xicPoints,
                    Mapping = x => new DataPoint(((XicDataPoint)x).X, ((XicDataPoint)x).Y),
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
                seriesstore[ion.LabeledIon.Label] = new Tuple<LineSeries, List<XicDataPoint>>(series, xicPoints);
            });
            foreach (var ion in seriesstore)
            {
                if (ion.Value == null) continue;
                plot.Series.Insert(0, ion.Value.Item1);
                plot.AddSeries(ion.Value.Item2);
            }
            plot.GenerateYAxis("Intensity", "0e0");
            plot.IsLegendVisible = _showLegend;
            plot.UniqueHighlight = (Plot != null) && Plot.UniqueHighlight;
            plot.SetPointMarker(SelectedRt);
            Area = GetCurrentArea();
            return plot;
        }

        /// <summary>
        /// Calculate an XIC for a given ion.
        /// </summary>
        /// <param name="label">Labeled ion to calculate XIC for</param>
        /// <returns>Labeled XIC containing calculated XIC and labeled ion</returns>
        private LabeledXic GetXic(LabeledIon label)
        {
            LabeledXic lxic;
            if (_xicCache.ContainsKey(label.Label)) lxic = _xicCache[label.Label];
            else
            {
                var ion = label.Ion;
                Xic xic;
                if (label.IsFragmentIon) xic = Lcms.GetFullProductExtractedIonChromatogram(ion.GetMostAbundantIsotopeMz(),
                                                                                            IcParameters.Instance.ProductIonTolerancePpm,
                                                                                            label.PrecursorIon.GetMostAbundantIsotopeMz());
                else if (label.IsChargeState) xic = Lcms.GetFullPrecursorIonExtractedIonChromatogram(ion.GetMostAbundantIsotopeMz(), 
                                                                                                     IcParameters.Instance.PrecursorTolerancePpm);
                else xic = Lcms.GetFullPrecursorIonExtractedIonChromatogram(ion.GetIsotopeMz(label.Index), IcParameters.Instance.PrecursorTolerancePpm);
                lxic = new LabeledXic(label.Composition, label.Index, xic.ToArray(), label.IonType, label.IsFragmentIon, label.PrecursorIon, label.IsChargeState);
                _xicCache.AddOrUpdate(label.Label, lxic, (key, oldValue) => lxic);
            }
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
                if (fileName != "") PngExporter.Export(Plot, fileName, (int)Plot.Width, (int)Plot.Height, OxyColors.White);
            }
            catch (Exception e)
            {
                _dialogService.ExceptionAlert(e);
            }
        }
#endregion

        #region Private Fields
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

        private readonly ConcurrentDictionary<string, Tuple<LabeledXic, bool>> _smoothedXics;
        private readonly ConcurrentDictionary<string, LabeledXic> _xicCache;
        private SelectablePlotModel _plot;
        private double _area;
        private int _selectedCharge;
        #endregion

        public class SelectedScanChangedMessage : NotificationMessage
        {
            public int Scan { get; private set; }
            public SelectedScanChangedMessage(object sender, int scan, string notification = "SelectedScanChanged")
                : base(sender, notification)
            {
                Scan = scan;
            }
        }
    }
}
