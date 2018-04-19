// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XicPlotViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class maintains a plot model for a extracted ion chromatogram.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using LcmsSpectator.PlotModels.ColorDictionaries;

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
    using Config;
    using DialogServices;
    using PlotModels;
    using Utils;
    using Data;
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
        /// Initializes a new instance of the <see cref="XicPlotViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">Dialog service </param>
        /// <param name="fragSeqVm">The view model for the fragmentation sequence (fragment ion generator)</param>
        /// <param name="lcms">LCMS run data set for this XIC plot. </param>
        /// <param name="title">Title of XIC plot </param>
        /// <param name="xaxis">XAxis for XIC plot. </param>
        /// <param name="showLegend">Should a legend be shown on the plot by default? </param>
        /// <param name="vertAxes">Y axis position</param>
        public XicPlotViewModel(IDialogService dialogService, IFragmentationSequenceViewModel fragSeqVm, ILcMsRun lcms, string title, LinearAxis xaxis, bool showLegend = true, AxisPosition vertAxes = AxisPosition.Left)
        {
            this.dialogService = dialogService;
            FragmentationSequenceViewModel = fragSeqVm;
            this.lcms = lcms;
            this.showLegend = showLegend;
            this.xaxis = xaxis;
            pointsToSmooth = IcParameters.Instance.PointsToSmooth;
            PlotTitle = title;
            PlotModel = new SelectablePlotModel(xaxis, 1.05)
            {
                YAxis =
                {
                    Title = "Intensity",
                    StringFormat = "0e0",
                    Position = vertAxes
                }
            };

            ions = new LabeledIonViewModel[0];

            var retentionTimeSelectedCommand = ReactiveCommand.Create();
            retentionTimeSelectedCommand
            .Select(_ => PlotModel.SelectedDataPoint as XicDataPoint)
            .Where(dp => dp != null)
            .Subscribe(dp => SelectedScan = dp.ScanNum);
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
                var areaStr = string.Format(CultureInfo.InvariantCulture, "{0:0.##E0}", area);
                PlotTitle = string.Format("{0} (Area: {1})", title, areaStr);
            });

            // Update area when x Axis is zoomed/panned
            this.xaxis.AxisChanged += async (o, e) =>
            {
                Area = await GetCurrentAreaAsync();
            };

            // Update point marker when selected scan changes
            this.WhenAnyValue(x => x.SelectedScan, x => x.IsPlotUpdating)
                .Where(x => x.Item2)
                .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
                .Subscribe(x => PlotModel.SetPrimaryPointMarker(this.lcms.GetElutionTime(x.Item1)));

            // When point markers are toggled, change the marker type on each series
            this.WhenAnyValue(x => x.ShowPointMarkers)
                .Select(showPointMarkers => showPointMarkers ? MarkerType.Circle : MarkerType.None)
                .Subscribe(
                    markerType =>
                        {
                            foreach (var lineSeries in PlotModel.Series.OfType<LineSeries>())
                            {
                                lineSeries.MarkerType = markerType;
                            }

                            PlotModel.InvalidatePlot(true);
                        });

            this.WhenAnyValue(
                    x => x.FragmentationSequenceViewModel.LabeledIonViewModels,
                    x => x.PointsToSmooth,
                    x => x.IsPlotUpdating)
                .Where(x => x.Item3 && x.Item1 != null)
                .SelectMany(async x => await GetXicDataPointsAsync(x.Item1, PointsToSmooth))
                .Subscribe(xicPoints =>
                {
                    ions = FragmentationSequenceViewModel.LabeledIonViewModels;
                    UpdatePlotModel(xicPoints);
                });

            // Update ions when relative intensity threshold changes.
            IcParameters.Instance.WhenAnyValue(x => x.PrecursorRelativeIntensityThreshold).Subscribe(precRelInt =>
            {
                if (FragmentationSequenceViewModel is PrecursorSequenceIonViewModel precFragVm)
                {
                    precFragVm.RelativeIntensityThreshold = precRelInt;
                }
            });

            // Update plot when settings change
            IcParameters.Instance.WhenAnyValue(x => x.ProductIonTolerancePpm)
                .Where(_ => ions != null)
                .Throttle(TimeSpan.FromMilliseconds(250), RxApp.TaskpoolScheduler)
                .SelectMany(async x => await GetXicDataPointsAsync(ions, PointsToSmooth, false))
                .Subscribe(UpdatePlotModel);
        }

        /// <summary>
        /// Gets command triggered when new scan is selected (double clicked) on XIC.
        /// </summary>
        public IReactiveCommand RetentionTimeSelectedCommand { get; }

        /// <summary>
        /// Gets command for exporting XIC plot as an image.
        /// </summary>
        public IReactiveCommand SaveAsImageCommand { get; }

        /// <summary>
        /// Gets the plot model for the extracted ion chromatogram plot.
        /// </summary>
        public SelectablePlotModel PlotModel
        {
            get => plotModel;
            private set => this.RaiseAndSetIfChanged(ref plotModel, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the plot should be updating data.
        /// </summary>
        public bool IsPlotUpdating
        {
            get => isPlotUpdating;
            set => this.RaiseAndSetIfChanged(ref isPlotUpdating, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the legend for the plot.
        /// </summary>
        public bool ShowLegend
        {
            get => showLegend;
            set => this.RaiseAndSetIfChanged(ref showLegend, value);
        }

        /// <summary>
        /// Gets or sets the smoothing window size
        /// </summary>
        public int PointsToSmooth
        {
            get => pointsToSmooth;
            set => this.RaiseAndSetIfChanged(ref pointsToSmooth, value);
        }

        /// <summary>
        /// Gets the view model for the fragmentation sequence (fragment ion generator)
        /// </summary>
        public IFragmentationSequenceViewModel FragmentationSequenceViewModel
        {
            get => fragmentationSequenceViewModel;
            private set => this.RaiseAndSetIfChanged(ref fragmentationSequenceViewModel, value);
        }

        /// <summary>
        /// Gets or sets the scan number selected on plot.
        /// </summary>
        public int SelectedScan
        {
            get => selectedScan;
            set => this.RaiseAndSetIfChanged(ref selectedScan, value);
        }

        /// <summary>
        /// Gets the area under curve of XIC plot.
        /// </summary>
        public double Area
        {
            get => area;
            private set => this.RaiseAndSetIfChanged(ref area, value);
        }

        /// <summary>
        /// Gets the title of the XIC plot, including area.
        /// </summary>
        public string PlotTitle
        {
            get => plotTitle;
            private set => this.RaiseAndSetIfChanged(ref plotTitle, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not point markers are displayed on the XIC plot.
        /// </summary>
        public bool ShowPointMarkers
        {
            get => showPointMarkers;
            set => this.RaiseAndSetIfChanged(ref showPointMarkers, value);
        }

        /// <summary>
        /// Clear data on plot.
        /// </summary>
        public void ClearPlot()
        {
            PlotModel.ClearSeries();
        }

        /// <summary>
        /// Calculates area under the curve of the XIC plot.
        /// </summary>
        /// <returns>Area under the curve of the XIC plot.</returns>
        public double GetCurrentArea()
        {
            return PlotModel.Series.OfType<XicPointSeries>().Where(xicPointSeries => xicPointSeries.IsVisible && xicPointSeries.Index >= 0)
                .Sum(xicPointSeries => xicPointSeries.GetArea(xaxis.ActualMinimum, xaxis.ActualMaximum));
        }

        /// <summary>
        /// Get a task that asynchronously calculates area under the curve of the XIC plot.
        /// </summary>
        /// <returns>Task that calculates the area under the curve of the XIC plot.</returns>
        public Task<double> GetCurrentAreaAsync()
        {
            return Task.Run(() => GetCurrentArea());
        }

        /// <summary>
        /// Generate plot by smoothing XICs, creating a LineSeries for each XIC
        /// </summary>
        /// <param name="xicPoints">The XICs to display on the plot.</param>
        public void UpdatePlotModel(IList<XicDataPoint>[] xicPoints)
        {
            // add XICs
            PlotModel.ClearSeries();
            if (xicPoints == null)
            {
                return;
            }

            var seriesstore =
                ions.ToDictionary<LabeledIonViewModel, string, Tuple<LineSeries, IList<XicDataPoint>>>(
                    ion => ion.Label,
                    ion => null);
            var maxCharge = (ions.Length > 0) ? ions.Max(ion => ion.IonType.Charge) : 2;
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

            PlotModel.IsLegendVisible = showLegend;
            PlotModel.InvalidatePlot(true);
            PlotModel.AdjustForZoom();
            PlotModel.SetPrimaryPointMarker(lcms.GetElutionTime(SelectedScan));
            Area = GetCurrentArea();
        }

        /// <summary>
        /// Open a save file dialog and save plot as PNG image to user selected location.
        /// </summary>
        public void SaveAsImage()
        {
            var filePath = dialogService.SaveFile(".png", @"Png Files (*.png)|*.png");
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
                    PlotModel,
                    filePath,
                    (int)PlotModel.Width,
                    (int)PlotModel.Height,
                    OxyColors.White,
                    IcParameters.Instance.ExportImageDpi);
            }
            catch (Exception e)
            {
                dialogService.ExceptionAlert(e);
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
