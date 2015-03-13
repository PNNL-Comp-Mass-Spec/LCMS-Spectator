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
using OxyPlot.Wpf;
using ReactiveUI;
using LinearAxis = OxyPlot.Axes.LinearAxis;
using TextAnnotation = OxyPlot.Annotations.TextAnnotation;

namespace LcmsSpectator.ViewModels
{
    public class SpectrumPlotViewModel: ReactiveObject
    {
        public SpectrumPlotViewModel(IMainDialogService dialogService, double multiplier, bool autoZoomXAxis=true)
        {
            _dialogService = dialogService;
            _autoZoomXAxis = autoZoomXAxis;
            ShowUnexplainedPeaks = true;
            ShowFilteredSpectrum = false;
            ShowDeconvolutedSpectrum = false;
            Title = "";
            XAxis = new LinearAxis
            {
                Title = "m/z",
                StringFormat = "0.###",
                Position = AxisPosition.Bottom,
            };
            PlotModel = new AutoAdjustedYPlotModel(XAxis, multiplier) { IsLegendVisible = false };
            _ions = new ReactiveList<LabeledIonViewModel>();

            // When Spectrum updates, clear the filtered spectrum, deconvoluted spectrum, and filtered+deconvoluted spectrum
            this.WhenAnyValue(x => x.Spectrum)
                .Subscribe(spectrum =>
                {
                    _spectrumDirty = true;
                    _filteredSpectrum = null;
                    _deconvolutedSpectrum = null;
                    _filteredDeconvolutedSpectrum = null;
                });

            // If deconvolution option has changed, the X Axis should change.
            this.WhenAnyValue(x => x.ShowDeconvolutedSpectrum)
                .Subscribe(_ => _spectrumDirty = true);

            // When Spectrum or ions change, or deconvoluted or filtered spectrum are selected, update spectrum plot
            this.WhenAnyValue(x => x.Spectrum, x => x.Ions,
                x => x.ShowDeconvolutedSpectrum, x => x.ShowFilteredSpectrum,
                x => x.ShowUnexplainedPeaks)
                .Where(x => x.Item1 != null && x.Item2 != null)
                .Throttle(TimeSpan.FromMilliseconds(400), RxApp.TaskpoolScheduler)
                .SelectMany(async x => await Task.WhenAll(x.Item2.Select(ion => ion.GetPeaksAsync(GetSpectrum(), ShowDeconvolutedSpectrum))))
                .Subscribe(UpdatePlotModel);       // Update plot when data changes

            // Update plot when settings change
            IcParameters.Instance.WhenAnyValue(x => x.PrecursorRelativeIntensityThreshold, x => x.ProductIonTolerancePpm,
                        x => x.SpectrumFilterWindowSize, x => x.IonCorrelationThreshold)
                        .Where(_ => Ions != null)
                        .Throttle(TimeSpan.FromMilliseconds(400), RxApp.TaskpoolScheduler)
                        .SelectMany(async x => await Task.WhenAll(Ions.Select(ion => ion.GetPeaksAsync(GetSpectrum(), ShowDeconvolutedSpectrum, false))))
                        .Subscribe(UpdatePlotModel);

            // Show/hide series when ion is selected/unselected
            this.WhenAnyValue(x => x.Ions)
                .Where(ions => ions != null)
                .Subscribe(ions => ions.ItemChanged.Where(x => x.PropertyName == "Selected")
                .Select(x => x.Sender)
                .Subscribe(LabeledIonSelectedChanged));

            // Save As Image Command requests a file path from the user and then saves the spectrum plot as an image
            var saveAsImageCommand = ReactiveCommand.Create();
            saveAsImageCommand.Subscribe(_ => SaveAsImage());
            SaveAsImageCommand = saveAsImageCommand;

            // Error map command opens a new error map window and passes it the most abundant isotope peak data points
            // and the current sequence.
            var openErrorMapCommand = ReactiveCommand.Create();
            openErrorMapCommand.Subscribe(_ => _dialogService.OpenErrorMapWindow(new ErrorMapViewModel
            {
                Sequence = Sequence,
                DataPoints = GetMostAbundantIsotopePeaks(_currentPeakDataPoints, Sequence)
            }));
            OpenErrorMapCommand = openErrorMapCommand;
        }

        #region Public Properties
        private AutoAdjustedYPlotModel _plotModel; 
        /// <summary>
        /// The spectrum plot.
        /// </summary>
        public AutoAdjustedYPlotModel PlotModel
        {
            get { return _plotModel; }
            private set { this.RaiseAndSetIfChanged(ref _plotModel, value); }
        }

        private string _title;
        /// <summary>
        /// Title of spectrum plot.
        /// </summary>
        public string Title
        {
            get { return _title; }
            set { this.RaiseAndSetIfChanged(ref _title, value); }
        }

        private bool _showUnexplainedPeaks ;
        /// <summary>
        /// Toggle "Unexplained Peaks" (spectrum series)
        /// </summary>
        public bool ShowUnexplainedPeaks
        {
            get { return _showUnexplainedPeaks; }
            set { this.RaiseAndSetIfChanged(ref _showUnexplainedPeaks, value); }
        }

        private bool _showFilteredSpectrum;
        /// <summary>
        /// Toggle whether or not the filtered spectrum is showed
        /// </summary>
        public bool ShowFilteredSpectrum
        {
            get { return _showFilteredSpectrum; }
            set { this.RaiseAndSetIfChanged(ref _showFilteredSpectrum, value); }
        }

        private bool _showDeconvolutedSpectrum;
        /// <summary>
        /// Toggle whether or not deconvoluted spectrum is shown
        /// </summary>
        public bool ShowDeconvolutedSpectrum
        {
            get { return _showDeconvolutedSpectrum; }
            set { this.RaiseAndSetIfChanged(ref _showDeconvolutedSpectrum, value); }
        }

        private LinearAxis _xAxis;
        /// <summary>
        /// XAxis of Spectrum plot
        /// </summary>
        public LinearAxis XAxis
        {
            get { return _xAxis; }
            private set { this.RaiseAndSetIfChanged(ref _xAxis, value); }
        }

        private Spectrum _spectrum;
        /// <summary>
        /// Spectrum currently displayed on the spectrum plot
        /// </summary>
        public Spectrum Spectrum
        {
            get { return _spectrum; }
            set
            {
                this.RaiseAndSetIfChanged(ref _spectrum, value);
            }
        }

        private Sequence _sequence;
        /// <summary>
        /// Sequence currently displayed on the spectrum plot.
        /// </summary>
        public Sequence Sequence
        {
            get { return _sequence; }
            set { this.RaiseAndSetIfChanged(ref _sequence, value); }
        }

        private ReactiveList<LabeledIonViewModel> _ions;
        /// <summary>
        /// Ions that are highlighted on the spectrum plot.
        /// </summary>
        public ReactiveList<LabeledIonViewModel> Ions
        {
            get { return _ions; }
            set { this.RaiseAndSetIfChanged(ref _ions, value); }
        }

        /// <summary>
        /// Prompt user for file path and save plot as image.
        /// </summary>
        public IReactiveCommand SaveAsImageCommand { get; private set; }

        /// <summary>
        /// Open error heatmap and table for this spectrum and ions
        /// </summary>
        public IReactiveCommand OpenErrorMapCommand { get; private set; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Prompt user for file path and save plot as image to that path.
        /// </summary>
        public void SaveAsImage()
        {
            var fileName = _dialogService.SaveFile(".png", @"Png Files (*.png)|*.png");
            try
            {
                if (fileName != "")
                {
                    PngExporter.Export(PlotModel, fileName, (int)PlotModel.Width, (int)PlotModel.Height, OxyColors.White);
                }
            }
            catch (Exception e)
            {
                _dialogService.ExceptionAlert(e);
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Build spectrum plot model.
        /// </summary>
        /// <param name="peakDataPoints">Ion peaks to highlight and annotate on spectrum plot.</param>
        private void UpdatePlotModel(IList<PeakDataPoint>[] peakDataPoints)
        {
            if (Spectrum == null) return;
            _currentPeakDataPoints = peakDataPoints;
            PlotModel.Series.Clear();
            PlotModel.Annotations.Clear();
            var spectrum = GetSpectrum();
            var spectrumPeaks = spectrum.Peaks.Select(peak => new PeakDataPoint(peak.Mz, peak.Intensity, 0.0, 0.0, ""));
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
            if (ShowUnexplainedPeaks) PlotModel.Series.Add(spectrumSeries);
            if (_autoZoomXAxis && _spectrumDirty)
            {
                // zoom spectrum if this plot is supposed to be auto zoomed, and only if spectrum has changed
                var peaks = spectrum.Peaks;
                var ms2MaxMz = 1.0; // plot maximum needs to be bigger than 0
                if (peaks.Length > 0) ms2MaxMz = peaks.Max().Mz * 1.1;
                XAxis.AbsoluteMinimum = 0;
                XAxis.AbsoluteMaximum = ms2MaxMz;
                XAxis.Zoom(0, ms2MaxMz);
                _spectrumDirty = false;
            }
            if (String.IsNullOrEmpty(PlotModel.YAxis.Title)) PlotModel.GenerateYAxis("Intensity", "0e0");

            var maxCharge = peakDataPoints.Length > 0 ? Ions.Max(x => x.IonType.Charge) : 2;
            maxCharge = Math.Max(maxCharge, 2);
            var colors = new ColorDictionary(maxCharge);
            foreach (var points in peakDataPoints)
            {
                if (points.Count == 0 || points[0].Error.Equals(Double.NaN)) continue;
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
                PlotModel.Series.Add(ionSeries);
                PlotModel.Annotations.Add(annotation);
            }
            PlotModel.Title = Title;
            PlotModel.InvalidatePlot(true);
            PlotModel.AdjustForZoom();
        }

        /// <summary>
        /// An ion has been selected or deselected. Find the ion peaks and annotation on the map
        /// and toggle their visibility.
        /// </summary>
        /// <param name="labeledIonVm">Ion that has been selected or deselected.</param>
        private void LabeledIonSelectedChanged(LabeledIonViewModel labeledIonVm)
        {
            var label = labeledIonVm.Label;
            StemSeries selectedlineseries = null;
            foreach (var series in PlotModel.Series)
            {
                var lineseries = series as StemSeries;
                if (lineseries != null && lineseries.Title == label)
                {
                    lineseries.IsVisible = labeledIonVm.Selected;
                    selectedlineseries = lineseries;
                    PlotModel.AdjustForZoom();
                }
            }
            foreach (var annotation in PlotModel.Annotations)
            {
                var textAnnotation = annotation as TextAnnotation;
                if (textAnnotation != null && textAnnotation.Text == label && selectedlineseries != null)
                {
                    textAnnotation.TextColor = labeledIonVm.Selected ? selectedlineseries.Color : OxyColors.Transparent;
                    textAnnotation.Background = labeledIonVm.Selected ? OxyColors.White : OxyColors.Transparent;
                    PlotModel.InvalidatePlot(true);
                }
            }
        }

        /// <summary>
        /// Get list of only most abundant isotope peaks of the ion peak data points
        /// Associate residue with the sequence
        /// </summary>
        /// <param name="peakDataPoints">Peak data points for ions on the spectrum plot</param>
        /// <param name="sequence">Sequence for ions display on the spectrum plot</param>
        /// <returns>List of most abundant isotope peak data points</returns>
        private ReactiveList<PeakDataPoint> GetMostAbundantIsotopePeaks(IEnumerable<IList<PeakDataPoint>> peakDataPoints, Sequence sequence=null)
        {
            var mostAbundantPeaks = new ReactiveList<PeakDataPoint>();
            foreach (var peaks in peakDataPoints)
            {
                var peak = peaks.OrderByDescending(p => p.Y).FirstOrDefault();
                if (peak != null && peak.IonType != null && peak.IonType.Name != "Precursor")
                {
                    var index = peak.Index - 1;
                    if (!peak.IonType.BaseIonType.IsPrefix) index = Sequence.Count - 1 - index;
                    if (sequence != null && index >= 0 && index <= sequence.Count - 1) peak.Residue = sequence[index].Residue;
                    mostAbundantPeaks.Add(peak);   
                }
            }
            return mostAbundantPeaks;
        }

        /// <summary>
        /// Get correctly filtered and/or deconvoluted spectrum
        /// </summary>
        /// <returns>Filtered and/or deconvoluted spectrum</returns>
        private Spectrum GetSpectrum()
        {
            // Filtered/Deconvoluted Spectrum?
            var spectrum = Spectrum;
            var tolerance = (_spectrum is ProductSpectrum)
                                ? IcParameters.Instance.ProductIonTolerancePpm
                                : IcParameters.Instance.PrecursorTolerancePpm;
            if (ShowFilteredSpectrum && ShowDeconvolutedSpectrum)
            {
                if (_filteredDeconvolutedSpectrum == null)
                {
                    _filteredDeconvolutedSpectrum = new Spectrum(spectrum.Peaks, spectrum.ScanNum);
                    _filteredDeconvolutedSpectrum.FilterNosieByIntensityHistogram(); 
                    _deconvolutedSpectrum = ProductScorerBasedOnDeconvolutedSpectra.GetDeconvolutedSpectrum(spectrum,
                    Constants.MinCharge, Constants.MaxCharge, tolerance,
                    IcParameters.Instance.IonCorrelationThreshold, Constants.IsotopeOffsetTolerance);
                }
                spectrum = _filteredDeconvolutedSpectrum;
            }
            else if (ShowFilteredSpectrum)
            {
                if (_filteredSpectrum == null) 
                {
                    _filteredSpectrum = new Spectrum(spectrum.Peaks, spectrum.ScanNum);
                    _filteredSpectrum.FilterNosieByIntensityHistogram(); 
                }
                spectrum = _filteredSpectrum;
            }
            else if (ShowDeconvolutedSpectrum)
            {
                if (_deconvolutedSpectrum == null)
                {
                    _deconvolutedSpectrum = ProductScorerBasedOnDeconvolutedSpectra.GetDeconvolutedSpectrum(spectrum,
                    Constants.MinCharge, Constants.MaxCharge, tolerance,
                    IcParameters.Instance.IonCorrelationThreshold, Constants.IsotopeOffsetTolerance);   
                }
                spectrum = _deconvolutedSpectrum;
            }
            return spectrum;
        }
        #endregion

        #region Private Fields
        private readonly IMainDialogService _dialogService;
        private readonly bool _autoZoomXAxis;

        private IList<PeakDataPoint>[] _currentPeakDataPoints;

        private bool _spectrumDirty; // Tracks whether or not the spectrum has changed
        private Spectrum _filteredSpectrum;
        private Spectrum _deconvolutedSpectrum;
        private Spectrum _filteredDeconvolutedSpectrum;

        #endregion
    }
}
