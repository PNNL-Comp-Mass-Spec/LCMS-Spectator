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
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using InformedProteomics.Backend.MassSpecData;
    using LcmsSpectator.Config;
    using LcmsSpectator.DialogServices;
    using LcmsSpectator.PlotModels;
    using LcmsSpectator.PlotModels.ColorDicionaries;
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
        private readonly LinearAxis xaxis;

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
        /// The view model for the fragmentation sequence (fragment ion generator)
        /// </summary>
        private IFragmentationSequenceViewModel fragmentationSequenceViewModel;

        /// <summary>
        /// Ions to generate XICs from
        /// </summary>
        private LabeledIonViewModel[] ions;

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
        /// The type of XICs shown 
        /// (isotopes of the precursor ion or neighboring charge states of the precursor ion)
        /// </summary>
        private PrecursorViewMode precursorViewMode;

        /// <summary>
        /// Initializes a new instance of the <see cref="XicPlotViewModel"/> class. 
        /// </summary>
        /// <param name="dialogService">Dialog service </param>
        /// <param name="fragSeqVm">The view model for the fragmentation sequence (fragment ion generator)</param>
        /// <param name="lcms">LCMS run data set for this XIC plot. </param>
        /// <param name="title">Title of XIC plot </param>
        /// <param name="xaxis">XAxis for XIC plot. </param>
        /// <param name="showLegend">Should a legend be shown on the plot by default? </param>
        public XicPlotViewModel(IDialogService dialogService, IFragmentationSequenceViewModel fragSeqVm, ILcMsRun lcms, string title, LinearAxis xaxis, bool showLegend = true)
        {
            this.dialogService = dialogService;
            this.FragmentationSequenceViewModel = fragSeqVm;
            this.lcms = lcms;
            this.showLegend = showLegend;
            this.xaxis = xaxis;
            this.pointsToSmooth = IcParameters.Instance.PointsToSmooth;
            this.PlotTitle = title;
            this.PlotModel = new SelectablePlotModel(xaxis, 1.05)
            {
                YAxis =
                {
                    Title = "Intensity",
                    StringFormat = "0e0"
                }
            };

            this.ions = new LabeledIonViewModel[0];

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
            this.xaxis.AxisChanged += async (o, e) =>
            {
                this.Area = await this.GetCurrentAreaAsync();
            };

            // Update point marker when selected scan changes
            this.WhenAnyValue(x => x.SelectedScan, x => x.IsPlotUpdating)
                .Where(x => x.Item2)
                .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
                .Subscribe(x => this.PlotModel.SetPrimaryPointMarker(this.lcms.GetElutionTime(x.Item1)));

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

            this.WhenAnyValue(x => x.FragmentationSequenceViewModel.LabeledIonViewModels, x => x.PointsToSmooth, x => x.IsPlotUpdating, x => x.PrecursorViewMode)
                .Where(x => x.Item3 && x.Item1 != null)
                .SelectMany(async x => await this.GetXicDataPointsAsync(x.Item1, this.PointsToSmooth))
                .Subscribe(xicPoints =>
                {
                    this.ions = this.FragmentationSequenceViewModel.LabeledIonViewModels;
                    this.UpdatePlotModel(xicPoints);
                });

            // Update ions when relative intensity threshold changes.
            IcParameters.Instance.WhenAnyValue(x => x.PrecursorRelativeIntensityThreshold).Subscribe(precRelInt =>
            {
                var precFragVm = this.FragmentationSequenceViewModel as PrecursorSequenceIonViewModel;
                if (precFragVm != null)
                {
                    precFragVm.RelativeIntensityThreshold = precRelInt;
                }
            });

            // Update plot when settings change
            IcParameters.Instance.WhenAnyValue(x => x.ProductIonTolerancePpm)
                .Where(_ => this.ions != null)
                .Throttle(TimeSpan.FromMilliseconds(250), RxApp.TaskpoolScheduler)
                .SelectMany(async x => await this.GetXicDataPointsAsync(this.ions, this.PointsToSmooth, false))
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
        /// Gets the view model for the fragmentation sequence (fragment ion generator)
        /// </summary>
        public IFragmentationSequenceViewModel FragmentationSequenceViewModel
        {
            get { return this.fragmentationSequenceViewModel; }
            private set { this.RaiseAndSetIfChanged(ref this.fragmentationSequenceViewModel, value); }
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
        /// Gets or sets the type of XICs shown 
        /// (isotopes of the precursor ion or neighboring charge states of the precursor ion)
        /// </summary>
        public PrecursorViewMode PrecursorViewMode
        {
            get { return this.precursorViewMode; }
            set { this.RaiseAndSetIfChanged(ref this.precursorViewMode, value); }
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
                .Sum(xicPointSeries => xicPointSeries.GetArea(this.xaxis.ActualMinimum, this.xaxis.ActualMaximum));
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
                this.ions.ToDictionary<LabeledIonViewModel, string, Tuple<LineSeries, IList<XicDataPoint>>>(
                    ion => ion.Label,
                    ion => null);
            var maxCharge = (this.ions.Length > 0) ? this.ions.Max(ion => ion.IonType.Charge) : 2;
            maxCharge = Math.Max(maxCharge, 2);
            var colors = new IonColorDictionary(maxCharge);
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

            this.PlotModel.IsLegendVisible = this.showLegend;
            this.PlotModel.InvalidatePlot(true);
            this.PlotModel.AdjustForZoom();
            this.PlotModel.SetPrimaryPointMarker(this.lcms.GetElutionTime(this.SelectedScan));
            this.Area = this.GetCurrentArea();
        }

        /// <summary>
        /// Open a save file dialog and save plot as PNG image to user selected location.
        /// </summary>
        public void SaveAsImage()
        {
            var filePath = this.dialogService.SaveFile(".png", @"Png Files (*.png)|*.png");
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (directory == null || !Directory.Exists(directory))
                {
                    throw new FormatException(
                        string.Format("Cannot save image due to invalid file name: {0}", filePath));
                }

                DynamicResolutionPngExporter.Export(
                    this.PlotModel,
                    filePath,
                    (int)this.PlotModel.Width,
                    (int)this.PlotModel.Height,
                    OxyColors.White,
                    IcParameters.Instance.ExportImageDpi);
            }
            catch (Exception e)
            {
                this.dialogService.ExceptionAlert(e);
            }
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
