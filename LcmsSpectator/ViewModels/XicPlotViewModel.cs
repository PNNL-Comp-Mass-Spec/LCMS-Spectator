using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.Config;
using LcmsSpectator.DialogServices;
using LcmsSpectator.PlotModels;
using OxyPlot;
using OxyPlot.Wpf;
using ReactiveUI;
using LinearAxis = OxyPlot.Axes.LinearAxis;
using LineSeries = OxyPlot.Series.LineSeries;

namespace LcmsSpectator.ViewModels
{
    public class XicPlotViewModel: ReactiveObject
    {
        /// <summary>
        /// Xic Plot View Model constructor
        /// </summary>
        /// <param name="dialogService">Dialog service</param>
        /// <param name="lcms">Lcms run data set for this Xic plot.</param>
        /// <param name="title">Title of XIC plot</param>
        /// <param name="xAxis">XAxis for XIC plot.</param>
        /// <param name="showLegend">Should a legend be shown on the plot by default?</param>
        public XicPlotViewModel(IDialogService dialogService, ILcMsRun lcms, string title, LinearAxis xAxis, bool showLegend=true)
        {
            _dialogService = dialogService;
            _title = title;
            _lcms = lcms;
            _showLegend = showLegend;
            _xAxis = xAxis;
            _pointsToSmooth = IcParameters.Instance.PointsToSmooth;
            PlotTitle = _title;
            PlotModel = new SelectablePlotModel(xAxis, 1.05);
            _ions = new ReactiveList<LabeledIonViewModel>();

            var retentionTimeSelectedCommand = ReactiveCommand.Create();
            retentionTimeSelectedCommand.Subscribe(_ =>
            {
                var dataPoint = PlotModel.SelectedDataPoint as XicDataPoint;
                if (dataPoint == null) return;
                SelectedScan = dataPoint.ScanNum;
            });
            RetentionTimeSelectedCommand = retentionTimeSelectedCommand;

            var saveAsImageCommand = ReactiveCommand.Create();
            saveAsImageCommand.Subscribe(_ => SaveAsImage());
            SaveAsImageCommand = saveAsImageCommand;

            // When ShowLegend is updated, IsLegendVisible on the plot should be updated
            this.WhenAnyValue(x => x.ShowLegend).Subscribe(v =>
            {
                PlotModel.IsLegendVisible = v;
                PlotModel.InvalidatePlot(true);
            });

            // When area updates, plot title should update
            this.WhenAnyValue(x => x.Area).Subscribe(area =>
            {
                var areaStr = String.Format(CultureInfo.InvariantCulture, "{0:0.##E0}", area);
                PlotTitle = String.Format("{0} (Area: {1})", _title, areaStr);
            });

            // Update area when x Axis is zoomed/panned
            _xAxis.AxisChanged += (async (o, e) =>
            {
                Area = await GetCurrentAreaAsync();
            });

            // Update point marker when selected scan changes
            this.WhenAnyValue(x => x.SelectedScan, x => x.IsPlotVisible)
                .Where(x => x.Item2)
                .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
                .Subscribe(x => PlotModel.SetUniquePointMarker(_lcms.GetElutionTime(x.Item1)));

            // Update plot when ions change, or plot visibility changes
            this.WhenAnyValue(x => x.Ions, x => x.IsPlotVisible, x => x.PointsToSmooth)
                .Where(x => x.Item1 != null && x.Item2)    // Do not do anything if plot isn't visible
                .Throttle(TimeSpan.FromMilliseconds(250), RxApp.TaskpoolScheduler)
                .SelectMany(async x => await GetXicDataPointsAsync(x.Item1, x.Item3))
                .Subscribe(UpdatePlotModel);       // Update plot when data changes

            // Show/hide series when ion is selected/unselected
            this.WhenAnyValue(x => x.Ions)
                .Where(ions => ions != null)
                .Subscribe(ions => ions.ItemChanged.Where(x => x.PropertyName == "Selected")
                .Select(x => x.Sender)
                .Subscribe(LabeledIonSelectedChanged));

            // When point markers are toggled, change the marker type on each series
            this.WhenAnyValue(x => x.ShowPointMarkers)
                .Select(showPointMarkers => showPointMarkers ? MarkerType.Circle : MarkerType.None)
                .Subscribe(
                    markerType =>
                        {
                            foreach (var lineSeries in this.PlotModel.Series.OfType<LineSeries>()) lineSeries.MarkerType = markerType;
                            PlotModel.InvalidatePlot(true);
                        });

            // Update plot when settings change
            IcParameters.Instance.WhenAnyValue(x => x.PrecursorRelativeIntensityThreshold, x => x.ProductIonTolerancePpm)
                .Where(_ => Ions != null)
                .Throttle(TimeSpan.FromMilliseconds(250), RxApp.TaskpoolScheduler)
                .SelectMany(async x => await GetXicDataPointsAsync(Ions, PointsToSmooth, false))
                .Subscribe(UpdatePlotModel);
        }

        #region Public Properties
        /// <summary>
        /// Command triggered when new scan is selected (double clicked) on XIC.
        /// </summary>
        public IReactiveCommand RetentionTimeSelectedCommand { get; private set; }

        /// <summary>
        /// Command for exporting xic plot as an image.
        /// </summary>
        public IReactiveCommand SaveAsImageCommand { get; private set; }

        private SelectablePlotModel _plotModel;
        /// <summary>
        /// Plot model for XIC
        /// </summary>
        public SelectablePlotModel PlotModel
        {
            get { return _plotModel; }
            private set { this.RaiseAndSetIfChanged(ref _plotModel, value); }
        }

        private bool _isPlotVisible;
        /// <summary>
        /// Get/set if the plot should be showing data.
        /// </summary>
        public bool IsPlotVisible
        {
            get { return _isPlotVisible; }
            set { this.RaiseAndSetIfChanged(ref _isPlotVisible, value); }
        }

        private bool _showLegend;
        /// <summary>
        /// Shows or hides the legend for the plot.
        /// </summary>
        public bool ShowLegend
        {
            get { return _showLegend; }
            set { this.RaiseAndSetIfChanged(ref _showLegend, value); }
        }

        private int _pointsToSmooth;
        /// <summary>
        /// Property for setting the smoothing window size
        /// </summary>
        public int PointsToSmooth
        {
            get { return _pointsToSmooth; }
            set { this.RaiseAndSetIfChanged(ref _pointsToSmooth, value); }
        }

        private ReactiveList<LabeledIonViewModel> _ions;
        /// <summary>
        /// Ions to generate XICs from
        /// </summary>
        public ReactiveList<LabeledIonViewModel> Ions
        {
            get { return _ions; }
            set { this.RaiseAndSetIfChanged(ref _ions, value); }
        }

        private int _selectedScan;
        /// <summary>
        /// The scan number selected on plot.
        /// </summary>
        public int SelectedScan
        {
            get { return _selectedScan; }
            set { this.RaiseAndSetIfChanged(ref _selectedScan, value); }
        }

        private double _area;
        /// <summary>
        /// Area under curve of XIC
        /// </summary>
        public double Area
        {
            get { return _area; }
            set { this.RaiseAndSetIfChanged(ref _area, value); }
        }

        private string _plotTitle;
        /// <summary>
        /// Title of plot, including area
        /// </summary>
        public string PlotTitle
        {
            get { return _plotTitle; }
            private set { this.RaiseAndSetIfChanged(ref _plotTitle, value); }
        }

        private bool _showPointMarkers;
        /// <summary>
        /// Toggles whether or not point markers are displayed on the XIC plot.
        /// </summary>
        public bool ShowPointMarkers
        {
            get { return _showPointMarkers; }            
            set { this.RaiseAndSetIfChanged(ref _showPointMarkers, value); }
        }
#endregion

        #region Public Methods
        /// <summary>
        /// Clear data on plot.
        /// </summary>
        public void ClearPlot()
        {
            PlotModel.Series.Clear();
        }

        /// <summary>
        /// Get current area under curve of XIC plot
        /// </summary>
        /// <returns></returns> 
        public double GetCurrentArea()
        {
            var area =  PlotModel.Series.OfType<XicPointSeries>().Where(xicPointSeries => xicPointSeries.IsVisible && xicPointSeries.Index >= 0)
                .Sum(xicPointSeries => xicPointSeries.GetArea(_xAxis.ActualMinimum, _xAxis.ActualMaximum));
            return area;
        }

        /// <summary>
        /// Get an awaitable task for XIC area.
        /// </summary>
        /// <returns></returns>
        public Task<double> GetCurrentAreaAsync()
        {
            return Task.Run(() => GetCurrentArea());
        }

        /// <summary>
        /// Generate plot by smoothing XICs, creating a LineSeries for each xic, and adding it to the Plot
        /// </summary>
        public void UpdatePlotModel(IList<XicDataPoint>[] xicPoints)
        {
            // add XICs
            PlotModel.ClearSeries();
            if (xicPoints == null) return;
            var seriesstore = Ions.ToDictionary<LabeledIonViewModel, string, Tuple<LineSeries, IList<XicDataPoint>>>(ion => ion.Label, ion => null);
            var maxCharge = (_ions.Count > 0) ? _ions.Max(ion => ion.IonType.Charge) : 2;
            maxCharge = Math.Max(maxCharge, 2);
            var colors = new ColorDictionary(maxCharge);
            foreach (var xic in xicPoints)
            {
                if (xic == null || xic.Count == 0) continue;
                var firstPoint = xic[0];
                var color = firstPoint.IonType != null ? colors.GetColor(firstPoint.IonType.BaseIonType, firstPoint.IonType.Charge)
                                                           : colors.GetColor(firstPoint.Index);
                // create line series for xic
                var series = new XicPointSeries
                {
                    Index = firstPoint.Index,
                    ItemsSource = xic,
                    StrokeThickness = 1.5,
                    Title = xic[0].Title,
                    Color = color,
                    LineStyle = xic[0].Index >= 0 ? LineStyle.Solid : LineStyle.Dash,
                    MarkerSize = 3,
                    MarkerStroke = color,
                    MarkerStrokeThickness = 1,
                    MarkerFill = OxyColors.White,
                    MarkerType = ShowPointMarkers ? MarkerType.Circle : MarkerType.None,
                    TrackerFormatString =
                        "{0}" + Environment.NewLine +
                        "{1}: {2:0.###}" + Environment.NewLine +
                        "Scan #: {ScanNum}" + Environment.NewLine +
                        "{3}: {4:0.##E0}"
                };
                seriesstore[xic[0].Title] = new Tuple<LineSeries, IList<XicDataPoint>>(series, xic);
                PlotModel.Series.Insert(0, series);
            }
            if (String.IsNullOrEmpty(PlotModel.YAxis.Title)) PlotModel.GenerateYAxis("Intensity", "0e0");
            PlotModel.IsLegendVisible = _showLegend;
            PlotModel.InvalidatePlot(true);
            PlotModel.AdjustForZoom();
            PlotModel.SetUniquePointMarker(_lcms.GetElutionTime(SelectedScan));
            Area = GetCurrentArea();
        }

        /// <summary>
        /// Open a save file dialog and save plot as png image to user selected location.
        /// </summary>
        public void SaveAsImage()
        {
            if (PlotModel == null) return;
            var fileName = _dialogService.SaveFile(".png", @"Png Files (*.png)|*.png");
            try
            {
                if (fileName != "") PngExporter.Export(PlotModel, fileName, (int)PlotModel.Width, (int)PlotModel.Height, OxyColors.White);
            }
            catch (Exception e)
            {
                _dialogService.ExceptionAlert(e);
            }
        }
#endregion

        #region Private Methods
        /// <summary>
        /// Finds series associated with labeledIonVm and shows/hides it depending on
        /// labeledIonVm selected property.
        /// </summary>
        /// <param name="labeledIonVm">Ion to hide series for.</param>
        private void LabeledIonSelectedChanged(LabeledIonViewModel labeledIonVm)
        {
            foreach (var series in PlotModel.Series)
            {
                var lineSeries = series as LineSeries;
                if (lineSeries != null && lineSeries.Title == labeledIonVm.Label)
                {
                    lineSeries.IsVisible = labeledIonVm.Selected;
                    PlotModel.AdjustForZoom();
                    Area = GetCurrentArea();
                }
            }
            Area = GetCurrentArea();
            PlotModel.AdjustForZoom();
        }

        private async Task<IList<XicDataPoint>[]> GetXicDataPointsAsync(IEnumerable<LabeledIonViewModel> ions, int smoothingPoints, bool useCache=true)
        {
            return await Task.WhenAll(ions.Select(ion => ion.GetXicAsync(smoothingPoints, useCache)));
        }
#endregion

        #region Private Fields
        private readonly IDialogService _dialogService;
        private readonly ILcMsRun _lcms;

        private readonly string _title;
        private readonly LinearAxis _xAxis;

        #endregion
    }
}
