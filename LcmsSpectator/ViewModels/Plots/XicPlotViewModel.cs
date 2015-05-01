// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XicPlotViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class maintains a plot model for a extracted ion chromatogram.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels.Plots
{
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
    using LcmsSpectator.Utils;
    using LcmsSpectator.ViewModels.Data;

    using OxyPlot;
    using OxyPlot.Axes;
    using OxyPlot.Series;

    using ReactiveUI;

    /// <summary>
    /// This class maintains a plot model for a extracted ion chromatogram.
    /// </summary>
    public class XicPlotViewModel : ReactiveObject
    {
        /// <summary>
        /// Dialog service for opening dialogs from view model.
        /// </summary>
        private readonly IDialogService dialogService;

        /// <summary>
        /// LCMSRun for the data set that this XIC plot is part of.
        /// </summary>
        private readonly ILcMsRun lcms;

        /// <summary>
        /// The XAxis of XIC PlotModel plot.
        /// </summary>
        private readonly LinearAxis xAxis;

        /// <summary>
        /// The plot model for the extracted ion chromatogram plot.
        /// </summary>
        private SelectablePlotModel plotModel;

        /// <summary>
        /// A value indicating whether the plot should be updating data.
        /// </summary>
        private bool isPlotUpdating;

        /// <summary>
        /// A value indicating whether or not the legend for the plot.
        /// </summary>
        private bool showLegend;

        /// <summary>
        /// Smoothing window size
        /// </summary>
        private int pointsToSmooth;

        /// <summary>
        /// Ions to generate XICs from
        /// </summary>
        private ReactiveList<LabeledIonViewModel> ions;

        /// <summary>
        /// The scan number selected on plot.
        /// </summary>
        private int selectedScan;

        /// <summary>
        /// The area under curve of XIC plot.
        /// </summary>
        private double area;

        /// <summary>
        /// The title of the XIC plot, including area.
        /// </summary>
        private string plotTitle;

        /// <summary>
        /// A value indicating whether or not point markers are displayed on the XIC plot.
        /// </summary>
        private bool showPointMarkers;

        /// <summary>
        /// Initializes a new instance of the <see cref="XicPlotViewModel"/> class. 
        /// </summary>
        /// <param name="dialogService">Dialog service </param>
        /// <param name="lcms">LCMS run data set for this XIC plot. </param>
        /// <param name="title">Title of XIC plot </param>
        /// <param name="xAxis">XAxis for XIC plot. </param>
        /// <param name="showLegend">Should a legend be shown on the plot by default? </param>
        public XicPlotViewModel(IDialogService dialogService, ILcMsRun lcms, string title, LinearAxis xAxis, bool showLegend = true)
        {
            this.dialogService = dialogService;
            this.lcms = lcms;
            this.showLegend = showLegend;
            this.xAxis = xAxis;
            this.pointsToSmooth = IcParameters.Instance.PointsToSmooth;
            this.PlotTitle = title;
            this.PlotModel = new SelectablePlotModel(xAxis, 1.05);
            this.ions = new ReactiveList<LabeledIonViewModel>();

            var retentionTimeSelectedCommand = ReactiveCommand.Create();
            retentionTimeSelectedCommand
            .Select(_ => this.PlotModel.SelectedDataPoint as XicDataPoint)
            .Where(dp => dp != null)
            .Subscribe(dp => this.SelectedScan = dp.ScanNum);
            this.RetentionTimeSelectedCommand = retentionTimeSelectedCommand;

            var saveAsImageCommand = ReactiveCommand.Create();
            saveAsImageCommand.Subscribe(_ => this.SaveAsImage());
            this.SaveAsImageCommand = saveAsImageCommand;

            // When ShowLegend is updated, IsLegendVisible on the plot should be updated
            this.WhenAnyValue(x => x.ShowLegend).Subscribe(v =>
            {
                this.PlotModel.IsLegendVisible = v;
                this.PlotModel.InvalidatePlot(true);
            });

            // When area updates, plot title should update
            this.WhenAnyValue(x => x.Area).Subscribe(area =>
            {
                var areaStr = string.Format(CultureInfo.InvariantCulture, "{0:0.##E0}", area);
                this.PlotTitle = string.Format("{0} (Area: {1})", title, areaStr);
            });

            // Update area when x Axis is zoomed/panned
            this.xAxis.AxisChanged += async (o, e) =>
            {
                this.Area = await this.GetCurrentAreaAsync();
            };

            // Update point marker when selected scan changes
            this.WhenAnyValue(x => x.SelectedScan, x => x.IsPlotUpdating)
                .Where(x => x.Item2)
                .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
                .Subscribe(x => this.PlotModel.SetUniquePointMarker(this.lcms.GetElutionTime(x.Item1)));

            // Update plot when ions change, or plot visibility changes
            this.WhenAnyValue(x => x.Ions, x => x.IsPlotUpdating, x => x.PointsToSmooth)
                .Where(x => x.Item1 != null && x.Item2)    // Do not do anything if plot isn't visible
                .Throttle(TimeSpan.FromMilliseconds(250), RxApp.TaskpoolScheduler)
                .SelectMany(async x => await this.GetXicDataPointsAsync(x.Item1, x.Item3))
                .Subscribe(this.UpdatePlotModel);       // Update plot when data changes

            // Show/hide series when ion is selected/unselected
            this.WhenAnyValue(x => x.Ions)
                .Where(ions => ions != null)
                .Subscribe(ions => ions.ItemChanged.Where(x => x.PropertyName == "Selected")
                .Select(x => x.Sender)
                .Subscribe(this.LabeledIonSelectedChanged));

            // When point markers are toggled, change the marker type on each series
            this.WhenAnyValue(x => x.ShowPointMarkers)
                .Select(showPointMarkers => showPointMarkers ? MarkerType.Circle : MarkerType.None)
                .Subscribe(
                    markerType =>
                        {
                            foreach (var lineSeries in this.PlotModel.Series.OfType<LineSeries>())
                            {
                                lineSeries.MarkerType = markerType;
                            }

                            this.PlotModel.InvalidatePlot(true);
                        });

            // Update plot when settings change
            IcParameters.Instance.WhenAnyValue(x => x.PrecursorRelativeIntensityThreshold, x => x.ProductIonTolerancePpm)
                .Where(_ => this.Ions != null)
                .Throttle(TimeSpan.FromMilliseconds(250), RxApp.TaskpoolScheduler)
                .SelectMany(async x => await this.GetXicDataPointsAsync(this.Ions, this.PointsToSmooth, false))
                .Subscribe(this.UpdatePlotModel);
        }

        /// <summary>
        /// Gets command triggered when new scan is selected (double clicked) on XIC.
        /// </summary>
        public IReactiveCommand RetentionTimeSelectedCommand { get; private set; }

        /// <summary>
        /// Gets command for exporting XIC plot as an image.
        /// </summary>
        public IReactiveCommand SaveAsImageCommand { get; private set; }

        /// <summary>
        /// Gets the plot model for the extracted ion chromatogram plot.
        /// </summary>
        public SelectablePlotModel PlotModel
        {
            get { return this.plotModel; }
            private set { this.RaiseAndSetIfChanged(ref this.plotModel, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the plot should be updating data.
        /// </summary>
        public bool IsPlotUpdating
        {
            get { return this.isPlotUpdating; }
            set { this.RaiseAndSetIfChanged(ref this.isPlotUpdating, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the legend for the plot.
        /// </summary>
        public bool ShowLegend
        {
            get { return this.showLegend; }
            set { this.RaiseAndSetIfChanged(ref this.showLegend, value); }
        }

        /// <summary>
        /// Gets or sets the smoothing window size
        /// </summary>
        public int PointsToSmooth
        {
            get { return this.pointsToSmooth; }
            set { this.RaiseAndSetIfChanged(ref this.pointsToSmooth, value); }
        }

        /// <summary>
        /// Gets or sets ions to generate XICs from
        /// </summary>
        public ReactiveList<LabeledIonViewModel> Ions
        {
            get { return this.ions; }
            set { this.RaiseAndSetIfChanged(ref this.ions, value); }
        }

        /// <summary>
        /// Gets or sets the scan number selected on plot.
        /// </summary>
        public int SelectedScan
        {
            get { return this.selectedScan; }
            set { this.RaiseAndSetIfChanged(ref this.selectedScan, value); }
        }

        /// <summary>
        /// Gets the area under curve of XIC plot.
        /// </summary>
        public double Area
        {
            get { return this.area; }
            private set { this.RaiseAndSetIfChanged(ref this.area, value); }
        }

        /// <summary>
        /// Gets the title of the XIC plot, including area.
        /// </summary>
        public string PlotTitle
        {
            get { return this.plotTitle; }
            private set { this.RaiseAndSetIfChanged(ref this.plotTitle, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not point markers are displayed on the XIC plot.
        /// </summary>
        public bool ShowPointMarkers
        {
            get { return this.showPointMarkers; }
            set { this.RaiseAndSetIfChanged(ref this.showPointMarkers, value); }
        }

        /// <summary>
        /// Clear data on plot.
        /// </summary>
        public void ClearPlot()
        {
            this.PlotModel.ClearSeries();
        }

        /// <summary>
        /// Calculates area under the curve of the XIC plot.
        /// </summary>
        /// <returns>Area under the curve of the XIC plot.</returns> 
        public double GetCurrentArea()
        {
            return this.PlotModel.Series.OfType<XicPointSeries>().Where(xicPointSeries => xicPointSeries.IsVisible && xicPointSeries.Index >= 0)
                .Sum(xicPointSeries => xicPointSeries.GetArea(this.xAxis.ActualMinimum, this.xAxis.ActualMaximum));
        }

        /// <summary>
        /// Get a task that asynchronously calculates area under the curve of the XIC plot.
        /// </summary>
        /// <returns>Task that calculates the area under the curve of the XIC plot.</returns>
        public Task<double> GetCurrentAreaAsync()
        {
            return Task.Run(() => this.GetCurrentArea());
        }

        /// <summary>
        /// Generate plot by smoothing XICs, creating a LineSeries for each XIC
        /// </summary>
        /// <param name="xicPoints">The XICs to display on the plot.</param>
        public void UpdatePlotModel(IList<XicDataPoint>[] xicPoints)
        {
            // add XICs
            this.PlotModel.ClearSeries();
            if (xicPoints == null)
            {
                return;
            }

            var seriesstore =
                this.Ions.ToDictionary<LabeledIonViewModel, string, Tuple<LineSeries, IList<XicDataPoint>>>(
                    ion => ion.Label,
                    ion => null);
            var maxCharge = (this.ions.Count > 0) ? this.ions.Max(ion => ion.IonType.Charge) : 2;
            maxCharge = Math.Max(maxCharge, 2);
            var colors = new ColorDictionary(maxCharge);
            foreach (var xic in xicPoints)
            {
                if (xic == null || xic.Count == 0)
                {
                    continue;
                }

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
                    MarkerType = this.ShowPointMarkers ? MarkerType.Circle : MarkerType.None,
                    TrackerFormatString =
                        "{0}" + Environment.NewLine +
                        "{1}: {2:0.###}" + Environment.NewLine +
                        "Scan #: {ScanNum}" + Environment.NewLine +
                        "{3}: {4:0.##E0}"
                };
                seriesstore[xic[0].Title] = new Tuple<LineSeries, IList<XicDataPoint>>(series, xic);
                this.PlotModel.Series.Insert(0, series);
            }

            if (string.IsNullOrEmpty(this.PlotModel.YAxis.Title))
            {
                this.PlotModel.GenerateYAxis("Intensity", "0e0");
            }

            this.PlotModel.IsLegendVisible = this.showLegend;
            this.PlotModel.InvalidatePlot(true);
            this.PlotModel.AdjustForZoom();
            this.PlotModel.SetUniquePointMarker(this.lcms.GetElutionTime(this.SelectedScan));
            this.Area = this.GetCurrentArea();
        }

        /// <summary>
        /// Open a save file dialog and save plot as PNG image to user selected location.
        /// </summary>
        public void SaveAsImage()
        {
            if (this.PlotModel == null)
            {
                return;
            }

            var fileName = this.dialogService.SaveFile(".png", @"Png Files (*.png)|*.png");
            try
            {
                if (fileName != string.Empty)
                {
                    DynamicResolutionPngExporter.Export(
                        this.PlotModel,
                        fileName,
                        (int)this.PlotModel.Width,
                        (int)this.PlotModel.Height,
                        OxyColors.White,
                        IcParameters.Instance.ExportImageDpi);
                }
            }
            catch (Exception e)
            {
                this.dialogService.ExceptionAlert(e);
            }
        }

        /// <summary>
        /// Finds series associated with labeledIonViewModel and shows/hides it depending on
        /// labeledIonViewModel selected property.
        /// </summary>
        /// <param name="labeledIonViewModel">View model for ion to hide series for.</param>
        private void LabeledIonSelectedChanged(LabeledIonViewModel labeledIonViewModel)
        {
            foreach (var series in this.PlotModel.Series)
            {
                var lineSeries = series as LineSeries;
                if (lineSeries != null && lineSeries.Title == labeledIonViewModel.Label)
                {
                    lineSeries.IsVisible = labeledIonViewModel.Selected;
                    this.PlotModel.AdjustForZoom();
                    this.Area = this.GetCurrentArea();
                }
            }

            this.Area = this.GetCurrentArea();
            this.PlotModel.AdjustForZoom();
        }

        /// <summary>Calculate XICs for ions.</summary>
        /// <param name="labeledIons">Ions to calculate XICs for. </param>
        /// <param name="smoothingPoints">Smoothing window size in points.</param>
        /// <param name="useCache">Should a cached XIC be used?</param>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task<IList<XicDataPoint>[]> GetXicDataPointsAsync(IEnumerable<LabeledIonViewModel> labeledIons, int smoothingPoints, bool useCache = true)
        {
            return await Task.WhenAll(labeledIons.Select(ion => ion.GetXicAsync(smoothingPoints, useCache)));
        }
    }
}
