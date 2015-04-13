// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SpectrumPlotViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class maintains a plot model for an MS spectrum.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using InformedProteomics.Backend.Data.Sequence;
    using InformedProteomics.Backend.Data.Spectrometry;
    using InformedProteomics.TopDown.Scoring;
    using LcmsSpectator.Config;
    using LcmsSpectator.DialogServices;
    using LcmsSpectator.PlotModels;
    using LcmsSpectator.Utils;
    using OxyPlot;
    using OxyPlot.Annotations;
    using OxyPlot.Axes;
    using OxyPlot.Series;
    using ReactiveUI;
    using LinearAxis = OxyPlot.Axes.LinearAxis;
    using TextAnnotation = OxyPlot.Annotations.TextAnnotation;

    /// <summary>
    /// This class maintains a plot model for an MS spectrum.
    /// </summary>
    public class SpectrumPlotViewModel : ReactiveObject
    {
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
        /// // Tracks whether or not the spectrum has changed
        /// </summary>
        private bool spectrumDirty;

        /// <summary>
        /// Stores the filtered version of the spectrum for fast access
        /// </summary>
        private Spectrum filteredSpectrum;

        /// <summary>
        /// Stores the deconvoluted version of the spectrum for fast access
        /// </summary>
        private Spectrum deconvolutedSpectrum;

        /// <summary>
        /// Stores the filtered and deconvoluted version of the spectrum for fast access
        /// </summary>
        private Spectrum filteredDeconvolutedSpectrum;

        /// <summary>
        /// List of ions that are highlighted on the spectrum plot.
        /// </summary>
        private ReactiveList<LabeledIonViewModel> ions;

        /// <summary>
        /// The sequence of the ions currently displayed on the spectrum plot.
        /// </summary>
        private Sequence sequence;

        /// <summary>
        /// The spectrum that is currently displayed on the spectrum plot.
        /// </summary>
        private Spectrum spectrum;

        /// <summary>
        /// The XAxis of spectrum PlotModel plot.
        /// </summary>
        private LinearAxis xAxis;

        /// <summary>
        /// A value indicating whether or not deconvoluted spectrum is showing.
        /// </summary>
        private bool showDeconvolutedSpectrum;

        /// <summary>
        /// A value indicating whether or not the filtered spectrum is showing.
        /// </summary>
        private bool showFilteredSpectrum;

        /// <summary>
        /// A value indicating whether the "Unexplained Peaks" (spectrum series)
        /// of the spectrum plot is visible.
        /// </summary>
        private bool showUnexplainedPeaks;

        /// <summary>
        /// The title of spectrum plot.
        /// </summary>
        private string title;

        /// <summary>
        /// Initializes a new instance of the SpectrumPlotViewModel class. 
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from ViewModel.</param>
        /// <param name="multiplier">How much padding should be before the lowest peak and after the highest peak?</param>
        /// <param name="autoZoomXAxis">Should this view model automatically zoom the plot?</param>
        public SpectrumPlotViewModel(IMainDialogService dialogService, double multiplier, bool autoZoomXAxis = true)
        {
            this.dialogService = dialogService;
            this.autoZoomXAxis = autoZoomXAxis;
            this.errorMapViewModel = new ErrorMapViewModel(dialogService);
            this.ShowUnexplainedPeaks = true;
            this.ShowFilteredSpectrum = false;
            this.ShowDeconvolutedSpectrum = false;
            this.Title = string.Empty;
            this.XAxis = new LinearAxis
            {
                Title = "m/z",
                StringFormat = "0.###",
                Position = AxisPosition.Bottom,
            };
            this.PlotModel = new AutoAdjustedYPlotModel(this.XAxis, multiplier) { IsLegendVisible = false };
            this.ions = new ReactiveList<LabeledIonViewModel>();

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
                .Subscribe(_ => this.spectrumDirty = true);

            // When Spectrum or ions change, or deconvoluted or filtered spectrum are selected, update spectrum plot
            this.WhenAnyValue(
                              x => x.Spectrum,
                              x => x.Ions,
                              x => x.ShowDeconvolutedSpectrum,
                              x => x.ShowFilteredSpectrum,
                              x => x.ShowUnexplainedPeaks)
                .Where(x => x.Item1 != null && x.Item2 != null)
                .Throttle(TimeSpan.FromMilliseconds(400), RxApp.TaskpoolScheduler)
                .SelectMany(async x => await Task.WhenAll(x.Item2.Select(ion => ion.GetPeaksAsync(this.GetSpectrum(), this.ShowDeconvolutedSpectrum))))
                .Subscribe(this.UpdatePlotModel);       // Update plot when data changes

            // Update plot when settings change
            IcParameters.Instance.WhenAnyValue(x => x.PrecursorRelativeIntensityThreshold, x => x.ProductIonTolerancePpm, x => x.IonCorrelationThreshold)
                        .Where(_ => this.Ions != null)
                        .Throttle(TimeSpan.FromMilliseconds(400), RxApp.TaskpoolScheduler)
                        .SelectMany(async x => await Task.WhenAll(this.Ions.Select(ion => ion.GetPeaksAsync(this.GetSpectrum(), this.ShowDeconvolutedSpectrum, false))))
                        .Subscribe(this.UpdatePlotModel);

            // Show/hide series when ion is selected/unselected
            this.WhenAnyValue(x => x.Ions)
                .Where(ions => ions != null)
                .Subscribe(ions => ions.ItemChanged.Where(x => x.PropertyName == "Selected")
                .Select(x => x.Sender)
                .Subscribe(this.LabeledIonSelectedChanged));

            // Save As Image Command requests a file path from the user and then saves the spectrum plot as an image
            var saveAsImageCommand = ReactiveCommand.Create();
            saveAsImageCommand.Subscribe(_ => this.SaveAsImageImplementation());
            this.SaveAsImageCommand = saveAsImageCommand;

            // Error map command opens a new error map window and passes it the most abundant isotope peak data points
            // and the current sequence.
            var openErrorMapCommand = ReactiveCommand.Create();
            openErrorMapCommand.Subscribe(_ => dialogService.OpenErrorMapWindow(this.errorMapViewModel));
            this.OpenErrorMapCommand = openErrorMapCommand;
        }

        /// <summary>
        /// Gets the spectrum plot.
        /// </summary>
        public AutoAdjustedYPlotModel PlotModel
        {
            get { return this.plotModel; }
            private set { this.RaiseAndSetIfChanged(ref this.plotModel, value); }
        }

        /// <summary>
        /// Gets or sets the title of spectrum plot.
        /// </summary>
        public string Title
        {
            get { return this.title; }
            set { this.RaiseAndSetIfChanged(ref this.title, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the "Unexplained Peaks" (spectrum series)
        /// of the spectrum plot is visible.
        /// </summary>
        public bool ShowUnexplainedPeaks
        {
            get { return this.showUnexplainedPeaks; }
            set { this.RaiseAndSetIfChanged(ref this.showUnexplainedPeaks, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the filtered spectrum is showing.
        /// </summary>
        public bool ShowFilteredSpectrum
        {
            get { return this.showFilteredSpectrum; }
            set { this.RaiseAndSetIfChanged(ref this.showFilteredSpectrum, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not deconvoluted spectrum is showing.
        /// </summary>
        public bool ShowDeconvolutedSpectrum
        {
            get { return this.showDeconvolutedSpectrum; }
            set { this.RaiseAndSetIfChanged(ref this.showDeconvolutedSpectrum, value); }
        }

        /// <summary>
        /// Gets the XAxis of the spectrum PlotModel.
        /// </summary>
        public LinearAxis XAxis
        {
            get { return this.xAxis; }
            private set { this.RaiseAndSetIfChanged(ref this.xAxis, value); }
        }

        /// <summary>
        /// Gets or sets the spectrum that is currently displayed on the spectrum plot.
        /// </summary>
        public Spectrum Spectrum
        {
            get { return this.spectrum; }
            set { this.RaiseAndSetIfChanged(ref this.spectrum, value); }
        }

        /// <summary>
        /// Gets or sets the sequence currently displayed on the spectrum plot.
        /// </summary>
        public Sequence Sequence
        {
            get { return this.sequence; }
            set { this.RaiseAndSetIfChanged(ref this.sequence, value); }
        }

        /// <summary>
        /// Gets or sets a list of ions that are highlighted on the spectrum plot.
        /// </summary>
        public ReactiveList<LabeledIonViewModel> Ions
        {
            get { return this.ions; }
            set { this.RaiseAndSetIfChanged(ref this.ions, value); }
        }

        /// <summary>
        /// Gets a command that prompts user for file path and save plot as image.
        /// </summary>
        public IReactiveCommand SaveAsImageCommand { get; private set; }

        /// <summary>
        /// Gets a command that opens error heat map and table for this spectrum and ions
        /// </summary>
        public IReactiveCommand OpenErrorMapCommand { get; private set; }

        /// <summary>
        /// Build spectrum plot model.
        /// </summary>
        /// <param name="peakDataPoints">Ion peaks to highlight and annotate on spectrum plot.</param>
        private void UpdatePlotModel(IList<PeakDataPoint>[] peakDataPoints)
        {
            if (Spectrum == null)
            {
                return;
            }

            this.errorMapViewModel.SetData(Sequence, peakDataPoints);
            this.PlotModel.Series.Clear();
            this.PlotModel.Annotations.Clear();
            var currentSpectrum = this.GetSpectrum();
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
            if (this.ShowUnexplainedPeaks)
            {
                this.PlotModel.Series.Add(spectrumSeries);
            }

            if (this.autoZoomXAxis && this.spectrumDirty)
            {
                // zoom spectrum if this plot is supposed to be auto zoomed, and only if spectrum has changed
                var peaks = currentSpectrum.Peaks;
                var ms2MaxMz = 1.0; // plot maximum needs to be bigger than 0
                if (peaks.Length > 0)
                {
                    ms2MaxMz = peaks.Max().Mz * 1.1;
                }

                this.XAxis.AbsoluteMinimum = 0;
                this.XAxis.AbsoluteMaximum = ms2MaxMz;
                this.XAxis.Zoom(0, ms2MaxMz);
                this.spectrumDirty = false;
            }

            if (string.IsNullOrEmpty(this.PlotModel.YAxis.Title))
            {
                this.PlotModel.GenerateYAxis("Intensity", "0e0");
            }

            var maxCharge = peakDataPoints.Length > 0 ? this.Ions.Max(x => x.IonType.Charge) : 2;
            maxCharge = Math.Max(maxCharge, 2);
            var colors = new ColorDictionary(maxCharge);
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
                var annotation = new TextAnnotation
                {
                    Text = points[0].Title,
                    TextColor = color,
                    FontWeight = FontWeights.Bold,
                    Layer = AnnotationLayer.AboveSeries,
                    FontSize = 12,
                    Background = OxyColors.White,
                    Padding = new OxyThickness(0.1),
                    TextPosition = new DataPoint(points[0].X, points[0].Y),
                    StrokeThickness = 0
                };
                this.PlotModel.Series.Add(ionSeries);
                this.PlotModel.Annotations.Add(annotation);
            }

            this.PlotModel.Title = this.Title;
            this.PlotModel.InvalidatePlot(true);
            this.PlotModel.AdjustForZoom();
        }

        /// <summary>
        /// An ion has been selected or deselected. Find the ion peaks and annotation on the map
        /// and toggle their visibility.
        /// </summary>
        /// <param name="labeledIonVm">Ion that has been selected or deselected.</param>
        private void LabeledIonSelectedChanged(LabeledIonViewModel labeledIonVm)
        {
            var label = labeledIonVm.Label;
            StemSeries selectedLineSeries = null;
            foreach (var series in this.PlotModel.Series)
            {
                var lineseries = series as StemSeries;
                if (lineseries != null && lineseries.Title == label)
                {
                    lineseries.IsVisible = labeledIonVm.Selected;
                    selectedLineSeries = lineseries;
                    this.PlotModel.AdjustForZoom();
                }
            }

            foreach (var annotation in this.PlotModel.Annotations)
            {
                var textAnnotation = annotation as TextAnnotation;
                if (textAnnotation != null && textAnnotation.Text == label && selectedLineSeries != null)
                {
                    textAnnotation.TextColor = labeledIonVm.Selected ? selectedLineSeries.Color : OxyColors.Transparent;
                    textAnnotation.Background = labeledIonVm.Selected ? OxyColors.White : OxyColors.Transparent;
                    this.PlotModel.InvalidatePlot(true);
                }
            }
        }

        /// <summary>
        /// Get correctly filtered and/or deconvoluted spectrum
        /// </summary>
        /// <returns>Filtered and/or deconvoluted spectrum</returns>
        private Spectrum GetSpectrum()
        {
            // Filtered/Deconvoluted Spectrum?
            var currentSpectrum = Spectrum;
            var tolerance = (currentSpectrum is ProductSpectrum)
                                ? IcParameters.Instance.ProductIonTolerancePpm
                                : IcParameters.Instance.PrecursorTolerancePpm;
            if (this.ShowFilteredSpectrum && this.ShowDeconvolutedSpectrum)
            {
                if (this.filteredDeconvolutedSpectrum == null)
                {
                    this.filteredDeconvolutedSpectrum = new Spectrum(currentSpectrum.Peaks, currentSpectrum.ScanNum);
                    this.filteredDeconvolutedSpectrum.FilterNosieByIntensityHistogram();
                    this.deconvolutedSpectrum = ProductScorerBasedOnDeconvolutedSpectra.GetDeconvolutedSpectrum(
                        currentSpectrum,
                        Constants.MinCharge,
                        Constants.MaxCharge,
                        tolerance,
                        IcParameters.Instance.IonCorrelationThreshold, 
                        Constants.IsotopeOffsetTolerance);
                }

                currentSpectrum = this.filteredDeconvolutedSpectrum;
            }
            else if (this.ShowFilteredSpectrum)
            {
                if (this.filteredSpectrum == null) 
                {
                    this.filteredSpectrum = new Spectrum(currentSpectrum.Peaks, currentSpectrum.ScanNum);
                    this.filteredSpectrum.FilterNosieByIntensityHistogram(); 
                }

                currentSpectrum = this.filteredSpectrum;
            }
            else if (this.ShowDeconvolutedSpectrum)
            {
                if (this.deconvolutedSpectrum == null)
                {
                    this.deconvolutedSpectrum = ProductScorerBasedOnDeconvolutedSpectra.GetDeconvolutedSpectrum(
                        currentSpectrum,
                        Constants.MinCharge,
                        Constants.MaxCharge,
                        tolerance,
                        IcParameters.Instance.IonCorrelationThreshold,
                        Constants.IsotopeOffsetTolerance);   
                }

                currentSpectrum = this.deconvolutedSpectrum;
            }

            return currentSpectrum;
        }

        /// <summary>
        /// Prompt user for file path and save plot as image to that path.
        /// </summary>
        private void SaveAsImageImplementation()
        {
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
    }
}
