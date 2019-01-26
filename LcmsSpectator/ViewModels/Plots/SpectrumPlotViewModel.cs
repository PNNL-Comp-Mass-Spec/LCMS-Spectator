// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SpectrumPlotViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class maintains a plot model for an MS spectrum.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.Config;
using LcmsSpectator.DialogServices;
using LcmsSpectator.PlotModels;
using LcmsSpectator.PlotModels.ColorDictionaries;
using LcmsSpectator.Utils;
using LcmsSpectator.ViewModels.Data;
using LcmsSpectator.ViewModels.SequenceViewer;
using LcmsSpectator.Writers.Exporters;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.Plots
{
    /// <summary>
    /// This class maintains a plot model for an MS spectrum.
    /// </summary>
    public class SpectrumPlotViewModel : ReactiveObject
    {
        /// <summary>
        /// Method used to filter data for the fragmentation spectrum plot and the fragmentation ion view
        /// </summary>
        public enum NoiseFilterModes
        {
            [Description("Disabled")]
            Disabled = 0,

            [Description("Intensity Histogram")]
            IntensityHistogram = 1,

            [Description("S/N")]
            SignalToNoiseRatio = 2,

            [Description("Local Window S/N")]
            LocalWindowSignalToNoiseRatio = 3
        }

        /// <summary>
        /// Used to filter which peaks are shown in the fragmentation spectrum plot
        /// </summary>
        public enum PeakFilterModes
        {

            /// <summary>
            /// Display all peaks
            /// </summary>
            [Description("All")]
            All = -1,

            // ReSharper disable UnusedMember.Global

            /// <summary>
            /// Display the top 20 peaks
            /// </summary>
            [Description("20")]
            Top20 = 20,

            /// <summary>
            /// Display the top 100 peaks
            /// </summary>
            [Description("100")]
            Top100 = 100,

            /// <summary>
            /// Display the top 500 peaks
            /// </summary>
            [Description("500")]
            Top500 = 500,

            /// <summary>
            /// Display the top 1000 peaks
            /// </summary>
            [Description("1000")]
            Top1000 = 1000,

            /// <summary>
            /// Display the top 2500 peaks
            /// </summary>
            [Description("2500")]
            Top2500 = 2500,

            /// <summary>
            /// Display the top 5000 peaks
            /// </summary>
            [Description("5000")]
            Top5000 = 5000,

            /// <summary>
            /// Display the top 10000 peaks
            /// </summary>
            [Description("10000")]
            Top10000 = 10000,

            /// <summary>
            /// Display the top 15000 peaks
            /// </summary>
            [Description("15000")]
            Top15000 = 15000

            // ReSharper restore UnusedMember.Global
        }

        /// <summary>
        /// Dialog service for opening dialogs from view model.
        /// </summary>
        private readonly IMainDialogService dialogService;

        /// <summary>
        /// Tracks whether or not the x axis of the spectrum plot should be
        /// automatically updated when the plot is built.
        /// </summary>
        private readonly bool autoZoomXAxis;

        /// <summary>
        /// Stores the error map for this spectrum.
        /// </summary>
        private readonly ErrorMapViewModel errorMapViewModel;

        /// <summary>
        /// Backing field for plot model.
        /// The plot model for the mass spectrum plot.
        /// </summary>
        private AutoAdjustedYPlotModel plotModel;

        /// <summary>
        /// Tracks whether or not the spectrum has changed
        /// </summary>
        private bool spectrumDirty;

        /// <summary>
        /// Stores the filtered version of the spectrum for fast access
        /// </summary>
        private Spectrum filteredSpectrum;

        /// <summary>
        /// Noise filter used when updating filteredSpectrum
        /// </summary>
        private NoiseFilterModes filteredSpectrumNoiseMode = NoiseFilterModes.Disabled;

        /// <summary>
        /// Stores the de-convoluted version of the spectrum for fast access
        /// </summary>
        private Spectrum deconvolutedSpectrum;

        /// <summary>
        /// Stores the filtered and de-convoluted version of the spectrum for fast access
        /// </summary>
        private Spectrum filteredDeconvolutedSpectrum;

        /// <summary>
        /// Noise filter used when updating filteredDeconvolutedSpectrum
        /// </summary>
        private NoiseFilterModes filteredDeconvolutedSpectrumNoiseMode = NoiseFilterModes.Disabled;

        /// <summary>
        /// List of ions that are highlighted on the spectrum plot.
        /// </summary>
        private LabeledIonViewModel[] ions;

        /// <summary>
        /// The view model for the fragmentation sequence (fragment ion generator)
        /// </summary>
        private IFragmentationSequenceViewModel fragmentationSequenceViewModel;

        /// <summary>
        /// The spectrum that is currently displayed on the spectrum plot.
        /// </summary>
        private Spectrum spectrum;

        /// <summary>
        /// The XAxis of spectrum PlotModel plot.
        /// </summary>
        private LinearAxis xAxis;

        /// <summary>
        /// A value indicating whether or not de-convoluted spectrum is showing.
        /// </summary>
        private bool showDeconvolutedSpectrum;

        /// <summary>
        /// A value indicating whether or not the deconvoluted ions are showing.
        /// </summary>
        private bool showDeconvolutedIons;

        /// <summary>
        /// A value indicating whether or not the filtered spectrum is showing.
        /// </summary>
        [Obsolete("Use NoiseFilterMode")]
        private bool showFilteredSpectrum;

        /// <summary>
        /// The minimum for the X axis of the spectrum plot.
        /// </summary>
        private double xMinimum;

        /// <summary>
        /// The maximum for the X axis of the spectrum plot.
        /// </summary>
        private double xMaximum;

        /// <summary>
        /// The minimum for the X axis of the spectrum plot.
        /// </summary>
        private double yMinimum;

        /// <summary>
        /// The maximum for the Y axis of the spectrum plot.
        /// </summary>
        private double yMaximum;

        /// <summary>
        /// A value indicating whether this plot should automatically
        /// adjust the Y Axis depending on the range selected on the X axis.
        /// </summary>
        private bool autoAdjustYAxis;

        /// <summary>
        /// Most recent scan number when GetSpectrum() was called
        /// </summary>
        private int mostRecentScanNum;

        /// <summary>
        /// Most recent peak filter mode when GetSpectrum() was called
        /// </summary>
        private PeakFilterModes mostRecentPeakFilterMode = PeakFilterModes.All;

        /// <summary>
        /// A value indicating whether the manual adjustment text boxes should be displayed.
        /// </summary>
        private bool showManualAdjustment;

        /// <summary>
        /// A value indicating whether the "Unexplained Peaks" (spectrum series)
        /// of the spectrum plot is visible.
        /// </summary>
        private bool showUnexplainedPeaks;

        /// <summary>
        /// Method used to filter data for the fragmentation spectrum plot and the fragmentation ion view
        /// </summary>
        private NoiseFilterModes noiseFilterMode = NoiseFilterModes.IntensityHistogram;

        /// <summary>
        /// A value indicating whether to show all peaks in a spectrum, or the top N peaks (sorted by intensity)
        /// </summary>
        private PeakFilterModes peakFilterMode;

        /// <summary>
        /// The title of spectrum plot.
        /// </summary>
        private string title;

        /// <summary>
        /// The peak data points for the current plot.
        /// </summary>
        private IList<PeakDataPoint>[] peakDataPoints;

        /// <summary>
        /// The actual spectrum that is being displayed.
        /// </summary>
        private Spectrum currentSpectrum;

        /// <summary>
        /// The scan selection view model. Updated when the spectrum is updated.
        /// </summary>
        private ScanSelectionViewModel scanSelectionViewModel;

        /// <summary>
        /// Initializes a new instance of the SpectrumPlotViewModel class.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from ViewModel.</param>
        /// <param name="fragSeqVm">The view model for the fragmentation sequence (fragment ion generator)</param>
        /// <param name="multiplier">Multiplier that determines how much space to leave above the tallest point.</param>
        /// <param name="autoZoomXAxis">Should this view model automatically zoom the plot?</param>
        public SpectrumPlotViewModel(IMainDialogService dialogService, IFragmentationSequenceViewModel fragSeqVm, double multiplier, bool autoZoomXAxis = true)
        {
            this.dialogService = dialogService;
            FragmentationSequenceViewModel = fragSeqVm;
            this.autoZoomXAxis = autoZoomXAxis;
            errorMapViewModel = new ErrorMapViewModel(dialogService);
            ShowUnexplainedPeaks = true;
            NoiseFilterMode = NoiseFilterModes.Disabled;
            ShowDeconvolutedSpectrum = false;

            NoiseFilterModeList = new ReactiveList<NoiseFilterModes>(Enum.GetValues(typeof(NoiseFilterModes)).Cast<NoiseFilterModes>());

            PeakFilterModeList = new ReactiveList<PeakFilterModes>(Enum.GetValues(typeof(PeakFilterModes)).Cast<PeakFilterModes>());

            // Default to plot the top 5000 peaks
            PeakFilterMode = PeakFilterModes.Top5000;

            AutoAdjustYAxis = true;
            Title = string.Empty;
            XAxis = new LinearAxis
            {
                Title = "m/z",
                StringFormat = "0.###",
                Position = AxisPosition.Bottom,
            };

            PlotModel = new AutoAdjustedYPlotModel(XAxis, multiplier)
            {
                IsLegendVisible = false,
                YAxis =
                {
                    Title = "Intensity",
                    StringFormat = "0e0"
                }
            };

            SequenceViewerViewModel = new SequenceViewerViewModel();

            ions = new LabeledIonViewModel[0];

            // When Spectrum updates, clear the filtered spectrum, deconvoluted spectrum, and filtered+deconvoluted spectrum
            this.WhenAnyValue(x => x.Spectrum)
                .Subscribe(spectrum =>
                {
                    spectrumDirty = true;
                    filteredSpectrum = null;
                    deconvolutedSpectrum = null;
                    filteredDeconvolutedSpectrum = null;
                });

            // If deconvolution option has changed, the X Axis should change.
            this.WhenAnyValue(x => x.ShowDeconvolutedSpectrum)
                .Subscribe(_ => spectrumDirty = true);

            // When Spectrum or ions change, or deconvoluted or filtered spectrum are selected, update spectrum plot
            this.WhenAnyValue(
                              x => x.Spectrum,
                              x => x.FragmentationSequenceViewModel.LabeledIonViewModels,
                              x => x.ShowDeconvolutedSpectrum,
                              x => x.ShowDeconvolutedIons,
                              x => x.NoiseFilterMode,
                              x => x.ShowUnexplainedPeaks,
                              x => x.PeakFilterMode)
                .Where(x => x.Item1 != null && x.Item2 != null)
                .Throttle(TimeSpan.FromMilliseconds(400), RxApp.TaskpoolScheduler)
                .SelectMany(async x =>
                {
                    // Retrieve all of the possible fragment ions (b ions, y ions, etc.) for all charge states from 1+ to the Charge of the precursor ion minus 1
                    var vms = await FragmentationSequenceViewModel.GetLabeledIonViewModels();
                    var spec = GetSpectrum();
                    return await
                            Task.WhenAll(
                                vms.Select(
                                    ion =>
                                        ion.GetPeaksAsync(spec,
                                            ShowDeconvolutedSpectrum || ShowDeconvolutedIons)));
                })
                .Subscribe(dataPoints =>
                {
                    ions = FragmentationSequenceViewModel.LabeledIonViewModels;
                    SetTerminalResidues(dataPoints);
                    UpdatePlotModel(dataPoints);

                    if (FragmentationSequenceViewModel is FragmentationSequenceViewModel model)
                    {
                        SequenceViewerViewModel.FragmentationSequence = model;

                        ProductSpectrum spectrumForFragmentView;
                        switch (NoiseFilterMode)
                        {
                            case NoiseFilterModes.Disabled:
                                spectrumForFragmentView = Spectrum as ProductSpectrum;
                                break;

                            case NoiseFilterModes.IntensityHistogram:
                                var histFilteredSpectrum = new ProductSpectrum(Spectrum.Peaks, Spectrum.ScanNum);
                                histFilteredSpectrum.SetMsLevel(Spectrum.MsLevel);
                                histFilteredSpectrum.FilterNoiseByIntensityHistogram();
                                spectrumForFragmentView = histFilteredSpectrum;
                                break;

                            case NoiseFilterModes.SignalToNoiseRatio:
                                var snFilteredSpectrum = new ProductSpectrum(Spectrum.Peaks, Spectrum.ScanNum);
                                snFilteredSpectrum.SetMsLevel(Spectrum.MsLevel);
                                snFilteredSpectrum.FilterNoise(IcParameters.Instance.MinimumSignalToNoise);
                                spectrumForFragmentView = snFilteredSpectrum;
                                break;

                            case NoiseFilterModes.LocalWindowSignalToNoiseRatio:
                                var windowedSnFilteredSpectrum = new ProductSpectrum(Spectrum.Peaks, Spectrum.ScanNum);
                                windowedSnFilteredSpectrum.SetMsLevel(Spectrum.MsLevel);
                                windowedSnFilteredSpectrum.FilterNoiseByLocalWindow(IcParameters.Instance.MinimumSignalToNoise);
                                spectrumForFragmentView = windowedSnFilteredSpectrum;
                                break;

                            default:
                                throw new InvalidEnumArgumentException(nameof(NoiseFilterMode));
                        }

                        SequenceViewerViewModel.SelectedSpectrum = spectrumForFragmentView;
                    }
                });       // Update plot when data changes

            this.WhenAnyValue(x => x.Spectrum).Where(spectrum => spectrum == null).Subscribe(
                _ =>
                    {
                        PlotModel.Annotations.Clear();
                        PlotModel.ClearSeries();
                        PlotModel.InvalidatePlot(true);
                    });

            // Update ions when relative intensity threshold changes.
            IcParameters.Instance.WhenAnyValue(x => x.PrecursorRelativeIntensityThreshold).Subscribe(precursorRelInt =>
            {
                if (FragmentationSequenceViewModel is PrecursorSequenceIonViewModel precursorFragVm)
                {
                    precursorFragVm.RelativeIntensityThreshold = precursorRelInt;
                }
            });

            // Update plot when settings change
            IcParameters.Instance.WhenAnyValue(x => x.ProductIonTolerancePpm, x => x.IonCorrelationThreshold)
                        .Throttle(TimeSpan.FromMilliseconds(400), RxApp.TaskpoolScheduler)
                        .SelectMany(async x =>
                        {
                            var spec = GetSpectrum();
                            return await Task.WhenAll(ions.Select(ion =>
                                    ion.GetPeaksAsync(spec, ShowDeconvolutedSpectrum, false)));
                        })
                        .Subscribe(UpdatePlotModel);

            // When AutoAdjustYAxis changes, update value in plot model.
            this.WhenAnyValue(x => x.AutoAdjustYAxis)
                .Subscribe(autoAdjust =>
                {
                    PlotModel.AutoAdjustYAxis = autoAdjust;
                    PlotModel.YAxis.IsZoomEnabled = !autoAdjust;
                    PlotModel.YAxis.IsPanEnabled = !autoAdjust;

                    if (autoAdjust)
                    {
                        PlotModel.XAxis.Reset();
                        PlotModel.YAxis.Reset();
                    }
                });

            // Update plot axes when FeaturePlotXMin, YMin, XMax, and YMax change
            this.WhenAnyValue(x => x.XMinimum, x => x.XMaximum)
                .Throttle(TimeSpan.FromSeconds(1), RxApp.TaskpoolScheduler)
                .Where(x => !xAxis.ActualMinimum.Equals(x.Item1) || !xAxis.ActualMaximum.Equals(x.Item2))
                .Subscribe(x =>
                {
                    xAxis.Zoom(x.Item1, x.Item2);
                    PlotModel.InvalidatePlot(false);
                });
            this.WhenAnyValue(y => y.YMinimum, y => y.YMaximum)
                .Throttle(TimeSpan.FromSeconds(1), RxApp.TaskpoolScheduler)
                .Where(y => !PlotModel.YAxis.ActualMinimum.Equals(y.Item1) || !PlotModel.YAxis.ActualMaximum.Equals(y.Item2))
                .Subscribe(
                    y =>
                    {
                        PlotModel.YAxis.Zoom(y.Item1, y.Item2);
                        PlotModel.InvalidatePlot(false);
                    });

            // Update X min and max properties when x axis is panned or zoomed
            xAxis.AxisChanged += (o, e) =>
            {
                XMinimum = Math.Round(xAxis.ActualMinimum, 3);
                XMaximum = Math.Round(xAxis.ActualMaximum, 3);
            };

            // Update Y min and max properties when Y axis is panned or zoomed
            PlotModel.YAxis.AxisChanged += (o, e) =>
            {
                YMinimum = Math.Round(PlotModel.YAxis.ActualMinimum, 3);
                YMaximum = Math.Round(PlotModel.YAxis.ActualMaximum, 3);
            };

            // Save As Image Command requests a file path from the user and then saves the spectrum plot as an image
            SaveAsImageCommand = ReactiveCommand.Create(SaveAsImageImplementation);

            // Error map command opens a new error map window and passes it the most abundant isotope peak data points
            // and the current sequence.
            OpenErrorMapCommand = ReactiveCommand.Create(() => dialogService.OpenErrorMapWindow(errorMapViewModel));
            OpenScanSelectionCommand = ReactiveCommand.Create(OpenScanSelectionImplementation);
            SaveAsTsvCommand = ReactiveCommand.Create(SaveAsTsvImplementation);
            SaveToClipboardCommand = ReactiveCommand.Create(SaveToClipboardImplementation);
            ToggleNoiseFilterModeCommand = ReactiveCommand.Create<NoiseFilterModes>(arg => NoiseFilterMode = arg);
            TogglePeakFilterModeCommand = ReactiveCommand.Create<PeakFilterModes>(arg => PeakFilterMode = arg);
        }

        /// <summary>
        /// Gets the spectrum plot.
        /// </summary>
        public AutoAdjustedYPlotModel PlotModel
        {
            get => plotModel;
            private set => this.RaiseAndSetIfChanged(ref plotModel, value);
        }

        /// <summary>
        /// Gets or sets the title of spectrum plot.
        /// </summary>
        public string Title
        {
            get => title;
            set => this.RaiseAndSetIfChanged(ref title, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the "Unexplained Peaks" (spectrum series)
        /// of the spectrum plot is visible.
        /// </summary>
        public bool ShowUnexplainedPeaks
        {
            get => showUnexplainedPeaks;
            set => this.RaiseAndSetIfChanged(ref showUnexplainedPeaks, value);
        }

        /// <summary>
        /// List of Noise Filter Modes
        /// </summary>
        public IReadOnlyReactiveList<NoiseFilterModes> NoiseFilterModeList { get; }

        /// <summary>
        /// Gets or sets a value indicating how to remove noise peaks from the spectral data
        /// </summary>
        public NoiseFilterModes NoiseFilterMode
        {
            get => noiseFilterMode;
            set => this.RaiseAndSetIfChanged(ref noiseFilterMode, value);
        }

        /// <summary>
        /// List of Peak Filter Modes
        /// </summary>
        public IReadOnlyReactiveList<PeakFilterModes> PeakFilterModeList { get; }

        /// <summary>
        /// Gets or sets a value indicating whether to show all peaks in a spectrum, or the top N peaks (sorted by intensity)
        /// </summary>
        public PeakFilterModes PeakFilterMode
        {
            get => peakFilterMode;
            set => this.RaiseAndSetIfChanged(ref peakFilterMode, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the filtered spectrum is showing.
        /// </summary>
        [Obsolete("Use NoiseFilterMode")]
        public bool ShowFilteredSpectrum
        {
            get => showFilteredSpectrum;
            set => this.RaiseAndSetIfChanged(ref showFilteredSpectrum, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not de-convoluted spectrum is showing.
        /// </summary>
        public bool ShowDeconvolutedSpectrum
        {
            get => showDeconvolutedSpectrum;
            set => this.RaiseAndSetIfChanged(ref showDeconvolutedSpectrum, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the deconvoluted ions are showing.
        /// </summary>
        public bool ShowDeconvolutedIons
        {
            get => showDeconvolutedIons;
            set => this.RaiseAndSetIfChanged(ref showDeconvolutedIons, value);
        }

        /// <summary>
        /// Gets or sets the minimum for the X axis of the spectrum plot.
        /// </summary>
        public double XMinimum
        {
            get => xMinimum;
            set => this.RaiseAndSetIfChanged(ref xMinimum, value);
        }

        /// <summary>
        /// Gets or sets the maximum for the X axis of the spectrum plot.
        /// </summary>
        public double XMaximum
        {
            get => xMaximum;
            set => this.RaiseAndSetIfChanged(ref xMaximum, value);
        }

        /// <summary>
        /// Gets or sets the minimum for the Y axis of the spectrum plot.
        /// </summary>
        public double YMinimum
        {
            get => yMinimum;
            set => this.RaiseAndSetIfChanged(ref yMinimum, value);
        }

        /// <summary>
        /// Gets or sets the maximum for the Y axis of the spectrum plot.
        /// </summary>
        public double YMaximum
        {
            get => yMaximum;
            set => this.RaiseAndSetIfChanged(ref yMaximum, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this plot should automatically
        /// adjust the Y Axis depending on the range selected on the X axis.
        /// </summary>
        public bool AutoAdjustYAxis
        {
            get => autoAdjustYAxis;
            set => this.RaiseAndSetIfChanged(ref autoAdjustYAxis, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the manual adjustment text boxes should be displayed.
        /// </summary>
        public bool ShowManualAdjustment
        {
            get => showManualAdjustment;
            set => this.RaiseAndSetIfChanged(ref showManualAdjustment, value);
        }

        /// <summary>
        /// Gets the XAxis of the spectrum PlotModel.
        /// </summary>
        public LinearAxis XAxis
        {
            get => xAxis;
            private set => this.RaiseAndSetIfChanged(ref xAxis, value);
        }

        /// <summary>
        /// Gets or sets the spectrum that is currently displayed on the spectrum plot.
        /// </summary>
        public Spectrum Spectrum
        {
            get => spectrum;
            set => this.RaiseAndSetIfChanged(ref spectrum, value);
        }

        /// <summary>
        /// Gets the view model for the fragmentation sequence (fragment ion generator)
        /// </summary>
        public IFragmentationSequenceViewModel FragmentationSequenceViewModel
        {
            get => fragmentationSequenceViewModel;
            set => this.RaiseAndSetIfChanged(ref fragmentationSequenceViewModel, value);
        }

        /// <summary>
        /// Gets or sets the sequence viewer.
        /// </summary>
        public SequenceViewerViewModel SequenceViewerViewModel { get; }

        /// <summary>
        /// Gets a command that prompts user for file path and save plot as image.
        /// </summary>
        public ReactiveCommand<Unit, Unit> SaveAsImageCommand { get; }

        /// <summary>
        /// Gets a command that opens error heat map and table for this spectrum and ions
        /// </summary>
        public ReactiveCommand<Unit, Unit> OpenErrorMapCommand { get; }

        /// <summary>
        /// Gets a command that opens the scan selection view model
        /// </summary>
        public ReactiveCommand<Unit, Unit> OpenScanSelectionCommand { get; }

        /// <summary>
        /// Gets a command that prompts user for file path and save spectrum as TSV to that path.
        /// </summary>
        public ReactiveCommand<Unit, Unit> SaveAsTsvCommand { get; }

        /// <summary>
        /// Gets a command that copies the peaks in view to the user's clipboard.
        /// </summary>
        public ReactiveCommand<Unit, Unit> SaveToClipboardCommand { get; }

        /// <summary>
        /// Gets a command used to update the noise filter mode
        /// </summary>
        public ReactiveCommand<NoiseFilterModes, Unit> ToggleNoiseFilterModeCommand { get; }

        /// <summary>
        /// Gets a command used to update the peak filter mode
        /// </summary>
        public ReactiveCommand<PeakFilterModes, Unit> TogglePeakFilterModeCommand { get; }

        /// <summary>
        /// Build spectrum plot model.
        /// </summary>
        /// <param name="dataPoints">Ion peaks to highlight and annotate on spectrum plot.</param>
        private void UpdatePlotModel(IList<PeakDataPoint>[] dataPoints)
        {
            if (Spectrum == null)
            {
                return;
            }

            errorMapViewModel.SetData(FragmentationSequenceViewModel.FragmentationSequence.Sequence, dataPoints);
            PlotModel.Series.Clear();
            PlotModel.Annotations.Clear();
            currentSpectrum = GetSpectrum();
            peakDataPoints = dataPoints;
            var spectrumPeaks = currentSpectrum.Peaks.Select(peak => new PeakDataPoint(peak.Mz, peak.Intensity, 0.0, 0.0, string.Empty));
            var spectrumSeries = new PeakPointSeries
            {
                ItemsSource = spectrumPeaks,
                Color = OxyColors.Black,
                StrokeThickness = 0.5,
                TrackerFormatString =
                    "{0}" + Environment.NewLine +
                    "{1}: {2:0.###}" + Environment.NewLine +
                    "{3}: {4:0.##E0}"
            };
            if (ShowUnexplainedPeaks)
            {
                PlotModel.Series.Add(spectrumSeries);
            }

            if (autoZoomXAxis && spectrumDirty)
            {
                // zoom spectrum if this plot is supposed to be auto zoomed, and only if spectrum has changed
                var peaks = currentSpectrum.Peaks;
                var ms2MaxMz = 1.0; // plot maximum needs to be bigger than 0
                if (peaks.Length > 0)
                {
                    ms2MaxMz = peaks.Max().Mz * 1.1;
                }

                XAxis.AbsoluteMinimum = 0;
                XAxis.AbsoluteMaximum = ms2MaxMz;
                XAxis.Zoom(0, ms2MaxMz);
                spectrumDirty = false;
            }

            var maxCharge = ions.Length > 0 ? ions.Max(x => x.IonType.Charge) : 2;

            maxCharge = Math.Max(maxCharge, 2);
            var colors = new IonColorDictionary(maxCharge);
            foreach (var points in peakDataPoints)
            {
                if (points.Count == 0 || points[0].Error.Equals(double.NaN))
                {
                    continue;
                }

                var firstPoint = points[0];
                var color = firstPoint.IonType != null ? colors.GetColor(firstPoint.IonType.BaseIonType, firstPoint.IonType.Charge)
                                                           : colors.GetColor(firstPoint.Index);
                var ionSeries = new PeakPointSeries
                {
                    Color = color,
                    StrokeThickness = 1.5,
                    ItemsSource = points,
                    Title = points[0].Title,
                    TrackerFormatString =
                        "{0}" + Environment.NewLine +
                        "{1}: {2:0.###}" + Environment.NewLine +
                        "{3}: {4:0.##E0}" + Environment.NewLine +
                        "Error: {Error:G4}ppm" + Environment.NewLine +
                        "Correlation: {Correlation:0.###}"
                };

                // Create ion name annotation
                var annotationName = points[0].Title.Contains("Precursor")
                                         ? string.Format("{0}\n{1,12:F3}",
                                                         points[0].Title,
                                                         points[0].X)
                                         : points[0].Title;
                var annotation = new TextAnnotation
                {
                    Text = annotationName,
                    TextColor = color,
                    FontWeight = FontWeights.Bold,
                    Layer = AnnotationLayer.AboveSeries,
                    FontSize = 12,
                    Background = OxyColors.White,
                    Padding = new OxyThickness(0.1),
                    TextPosition = new DataPoint(points[0].X, points[0].Y),
                    TextHorizontalAlignment = HorizontalAlignment.Center,
                    StrokeThickness = 0
                };

                PlotModel.Series.Add(ionSeries);
                PlotModel.Annotations.Add(annotation);
            }

            PlotModel.Title = Title;
            PlotModel.InvalidatePlot(true);
            PlotModel.AdjustForZoom();
        }

        /// <summary>
        /// Get correctly filtered and/or de-convoluted spectrum (for use in the plot)
        /// </summary>
        /// <returns>Filtered and/or de-convoluted spectrum</returns>
        private Spectrum GetSpectrum()
        {
            if (Spectrum == null)
            {
                return new Spectrum(new Peak[0], 0);
            }

            // Filtered/Deconvoluted Spectrum?
            var spectrumToReturn = Spectrum;
            var tolerance = (spectrumToReturn is ProductSpectrum)
                                ? IcParameters.Instance.ProductIonTolerancePpm
                                : IcParameters.Instance.PrecursorTolerancePpm;
            if (NoiseFilterMode != NoiseFilterModes.Disabled && ShowDeconvolutedSpectrum)
            {
                if (filteredDeconvolutedSpectrum == null || filteredDeconvolutedSpectrumNoiseMode != NoiseFilterMode)
                {
                    filteredDeconvolutedSpectrum = new Spectrum(spectrumToReturn.Peaks, spectrumToReturn.ScanNum);
                    filteredDeconvolutedSpectrumNoiseMode = NoiseFilterMode;

                    switch (NoiseFilterMode)
                    {
                        case NoiseFilterModes.IntensityHistogram:
                            filteredDeconvolutedSpectrum.FilterNoiseByIntensityHistogram();
                            break;

                        case NoiseFilterModes.SignalToNoiseRatio:
                            filteredDeconvolutedSpectrum.FilterNoise(IcParameters.Instance.MinimumSignalToNoise);
                            break;

                        case NoiseFilterModes.LocalWindowSignalToNoiseRatio:
                            filteredDeconvolutedSpectrum.FilterNoiseByLocalWindow(IcParameters.Instance.MinimumSignalToNoise);
                            break;

                        default:
                            throw new InvalidEnumArgumentException(nameof(NoiseFilterMode));
                    }

                    deconvolutedSpectrum = Deconvoluter.GetCombinedDeconvolutedSpectrum(
                        spectrumToReturn,
                            Constants.MinCharge,
                            Constants.MaxCharge,
                            Constants.IsotopeOffsetTolerance,
                            tolerance,
                            IcParameters.Instance.IonCorrelationThreshold);

                    //this.deconvolutedSpectrum = ProductScorerBasedOnDeconvolutedSpectra.GetDeconvolutedSpectrum(
                    //    spectrumToReturn,
                    //    Constants.MinCharge,
                    //    Constants.MaxCharge,
                    //    tolerance,
                    //    IcParameters.Instance.IonCorrelationThreshold,
                    //    Constants.IsotopeOffsetTolerance);
                }

                spectrumToReturn = filteredDeconvolutedSpectrum;
            }
            else if (NoiseFilterMode != NoiseFilterModes.Disabled)
            {
                if (filteredSpectrum == null || filteredSpectrumNoiseMode != NoiseFilterMode)
                {
                    filteredSpectrum = new Spectrum(spectrumToReturn.Peaks, spectrumToReturn.ScanNum);
                    filteredSpectrumNoiseMode = NoiseFilterMode;

                    switch (NoiseFilterMode)
                    {
                        case NoiseFilterModes.IntensityHistogram:
                            filteredSpectrum.FilterNoiseByIntensityHistogram();
                            break;

                        case NoiseFilterModes.SignalToNoiseRatio:
                            filteredSpectrum.FilterNoise(IcParameters.Instance.MinimumSignalToNoise);
                            break;

                        case NoiseFilterModes.LocalWindowSignalToNoiseRatio:
                            filteredSpectrum.FilterNoiseByLocalWindow(IcParameters.Instance.MinimumSignalToNoise);
                            break;

                        default:
                            throw new InvalidEnumArgumentException(nameof(NoiseFilterMode));
                    }

                }

                spectrumToReturn = filteredSpectrum;
            }
            else if (ShowDeconvolutedSpectrum)
            {
                if (deconvolutedSpectrum == null)
                {
                    deconvolutedSpectrum = Deconvoluter.GetCombinedDeconvolutedSpectrum(
                        spectrumToReturn,
                        Constants.MinCharge,
                        Constants.MaxCharge,
                        Constants.IsotopeOffsetTolerance,
                        tolerance,
                        IcParameters.Instance.IonCorrelationThreshold);
                    //this.deconvolutedSpectrum = ProductScorerBasedOnDeconvolutedSpectra.GetDeconvolutedSpectrum(
                    //    spectrumToReturn,
                    //    Constants.MinCharge,
                    //    Constants.MaxCharge,
                    //    tolerance,
                    //    IcParameters.Instance.IonCorrelationThreshold,
                    //    Constants.IsotopeOffsetTolerance);
                }

                spectrumToReturn = deconvolutedSpectrum;
            }

            if (mostRecentScanNum != Spectrum.ScanNum || mostRecentPeakFilterMode != PeakFilterMode)
            {
                mostRecentScanNum = Spectrum.ScanNum;
                mostRecentPeakFilterMode = PeakFilterMode;
                spectrumDirty = true;
            }

            if (PeakFilterMode == PeakFilterModes.All)
                return spectrumToReturn;

            var numPeaksToShow = (int)PeakFilterMode;

            var topNPeaks = spectrumToReturn.Peaks.OrderByDescending(p => p.Intensity).Take(numPeaksToShow).OrderBy(p => p.Mz).ToList();
            return new Spectrum(topNPeaks, spectrumToReturn.ScanNum);
        }

        private void SetTerminalResidues(IEnumerable<IList<PeakDataPoint>> dataPoints)
        {
            var sequence = FragmentationSequenceViewModel.FragmentationSequence.Sequence;
            var residueCount = sequence.Count;

            foreach (var dataPoint in dataPoints.SelectMany(x => x))
            {
                if (dataPoint.IonType != null && dataPoint.IonType.Name != "Precursor")
                {
                    var index = dataPoint.IonType.IsPrefixIon
                        ? dataPoint.Index - 1
                        : residueCount - dataPoint.Index;

                    if (index >= 0 && index < residueCount)
                    {
                        dataPoint.Residue = sequence[index].Residue;
                    }
                }
            }
        }

        /// <summary>
        /// Prompt user for file path and save spectrum as TSV to that path.
        /// </summary>
        private void SaveAsTsvImplementation()
        {
            if (currentSpectrum == null)
            {
                return;
            }

            if (peakDataPoints == null)
            {
                peakDataPoints = new IList<PeakDataPoint>[0];
            }

            var filePath = dialogService.SaveFile(".tsv", @"TSV Files (*.tsv)|*.tsv");
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            var fragmentPeaks =
                peakDataPoints.SelectMany(peaks => peaks)
                    .Where(peak => !peak.X.Equals(double.NaN))
                    .Where(peak => !peak.Y.Equals(double.NaN))
                    .OrderBy(peak => peak.X)
                    .ToArray();

            var peakExporter = new SpectrumPeakExporter(string.Empty, null, IcParameters.Instance.ProductIonTolerancePpm);
            peakExporter.Export(
                    currentSpectrum,
                    fragmentPeaks,
                    filePath);
        }

        /// <summary>
        /// Copies the peaks in view to the user's clipboard.
        /// </summary>
        private void SaveToClipboardImplementation()
        {
            if (currentSpectrum == null)
            {
                return;
            }

            if (peakDataPoints == null)
            {
                peakDataPoints = new IList<PeakDataPoint>[0];
            }

            var fragmentPeaks =
                peakDataPoints.SelectMany(peaks => peaks)
                    .Where(peak => !peak.X.Equals(double.NaN))
                    .Where(peak => !peak.Y.Equals(double.NaN))
                    .OrderBy(peak => peak.X)
                    .ToArray();

            var peakExporter = new SpectrumPeakExporter(string.Empty, null, IcParameters.Instance.ProductIonTolerancePpm);
            peakExporter.ExportToClipBoard(
                    currentSpectrum,
                    fragmentPeaks,
                    PlotModel.XAxis.ActualMinimum,
                    PlotModel.XAxis.ActualMaximum);
        }

        /// <summary>
        /// Implementation for the <see cref="OpenScanSelectionCommand" />.
        /// Opens scans selection window to allow users to select scans,
        /// and then updates the plots.
        /// </summary>
        private void OpenScanSelectionImplementation()
        {
            if (!(FragmentationSequenceViewModel.FragmentationSequence.LcMsRun is LcMsRun lcms))
                return;

            var msLevel = Spectrum is ProductSpectrum ? 2 : 1;
            if (scanSelectionViewModel == null || !scanSelectionViewModel.Contains(Spectrum.ScanNum))
            {
                var scans = lcms.GetScanNumbers(msLevel);
                var minScan = 0;
                var maxScan = 0;
                if (Spectrum != null)
                {
                    minScan = Spectrum.ScanNum;
                    maxScan = Spectrum.ScanNum;
                }

                scanSelectionViewModel = new ScanSelectionViewModel(Spectrum?.MsLevel ?? 1, scans)
                {
                    MinScanNumber = minScan,
                    MaxScanNumber = maxScan,
                    BaseScan = minScan
                };
            }

            if (dialogService.OpenScanSelectionWindow(scanSelectionViewModel))
            {
                Spectrum = scanSelectionViewModel.GetSelectedSpectrum(lcms);
                Title = scanSelectionViewModel.ScanNumbers.Count > 1 ?
                            string.Format(
                                "Summed MS{0} Spectra (Scans {1}-{2})",
                                msLevel,
                                scanSelectionViewModel.ScanNumbers.Min(),
                                scanSelectionViewModel.ScanNumbers.Max()) :
                             string.Format("MS{0} Spectrum (Scan: {1})", msLevel, Spectrum.ScanNum);
            }
        }

        /// <summary>
        /// Prompt user for file path and save plot as image to that path.
        /// </summary>
        private void SaveAsImageImplementation()
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
    }
}
