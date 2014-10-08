using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.TopDown.Scoring;
using LcmsSpectator.DialogServices;
using LcmsSpectator.PlotModels;
using LcmsSpectator.Utils;
using LcmsSpectatorModels.Config;
using LcmsSpectatorModels.Models;
using LcmsSpectatorModels.Utils;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;
using Annotation = OxyPlot.Annotations.Annotation;
using LinearAxis = OxyPlot.Axes.LinearAxis;
using Series = OxyPlot.Series.Series;
using TextAnnotation = OxyPlot.Annotations.TextAnnotation;

namespace LcmsSpectator.ViewModels
{
    public class SpectrumPlotViewModel: ViewModelBase
    {
        public RelayCommand SaveAsImageCommand { get; private set; }
        public ITaskService PlotTaskService { get; private set; }
        public SpectrumPlotViewModel(IDialogService dialogService, ITaskService taskService, double multiplier, bool showUnexplainedPeaks=true)
        {
            PlotTaskService = taskService;
            SaveAsImageCommand = new RelayCommand(SaveAsImage);
            _dialogService = dialogService;
            _showUnexplainedPeaks = showUnexplainedPeaks;
            _showFilteredSpectrum = false;
            _showDeconvolutedSpectrum = false;
            _multiplier = multiplier;
            Title = "";
            _ionCache = new Dictionary<string, Tuple<Series, Annotation>>();
            SpectrumUpdate();
        }

        public AutoAdjustedYPlotModel Plot
        {
            get { return _plot; }
            private set
            {
                _plot = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Title of plot.
        /// </summary>
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Toggle "Unexplained Peaks" (spectrum series)
        /// </summary>
        public bool ShowUnexplainedPeaks
        {
            get { return _showUnexplainedPeaks; }
            set
            {
                if (_showUnexplainedPeaks == value) return;
                _showUnexplainedPeaks = value;
                SpectrumUpdate();
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Toggle whether or not the filtered spectrum is showed
        /// </summary>
        public bool ShowFilteredSpectrum
        {
            get { return _showFilteredSpectrum; }
            set
            {
                _showFilteredSpectrum = value;
                _ionCache.Clear();
                SpectrumUpdate();
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Toggle whether or not deconvoluted spectrum is shown
        /// </summary>
        public bool ShowDeconvolutedSpectrum
        {
            get { return _showDeconvolutedSpectrum; }
            set
            {
                _showDeconvolutedSpectrum = value;
                _ionCache.Clear();
                SpectrumUpdate();
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Spectrum to display.
        /// </summary>
        public Spectrum Spectrum
        {
            get { return _spectrum; }
            set
            {
                if (_spectrum == value) return;
                _spectrum = value;
                _filteredSpectrum = null; // reset filtered spectrum
                _deconvolutedSpectrum = null;
                _filtDeconSpectrum = null;
                _ionCache.Clear();
                SpectrumUpdate();
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Ions to get peak highlights for
        /// </summary>
        public List<LabeledIonViewModel> Ions
        {
            get { return _ions;  }
            set
            {
                _ions = value;
                IonUpdate();
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Plot's x axis
        /// </summary>
        public LinearAxis XAxis
        {
            get { return _xAxis; }
            set
            {
                _xAxis = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Update spectrum and ion highlights
        /// </summary>
        public void SpectrumUpdate()
        {
            PlotTaskService.Enqueue(() => BuildSpectrumPlot());
        }

        public void IonUpdate()
        {
            PlotTaskService.Enqueue(() =>
            {
                if (Plot != null && Plot.Series.Count > 0)
                {
                    var spectrumSeries = Plot.Series[0] as StemSeries;
                    Plot = BuildSpectrumPlot(spectrumSeries);
                }
                else Plot = BuildSpectrumPlot();
            });
        }

        public void UpdateAll(Spectrum spectrum, List<LabeledIonViewModel> ions, LinearAxis xAxis = null)
        {
            PlotTaskService.Enqueue(() =>
            {
                _spectrum = spectrum;
                _xAxis = xAxis;
                _filteredSpectrum = null; // reset filtered spectrum
                _deconvolutedSpectrum = null;
                _filtDeconSpectrum = null;
                _ionCache.Clear();
                _ions = ions;
                Plot = BuildSpectrumPlot();
            });
        }

        /// <summary>
        /// Clear spectrum plot
        /// </summary>
        public void Clear()
        {
            Spectrum = null;
            SpectrumUpdate();
        }
        
        private AutoAdjustedYPlotModel BuildSpectrumPlot(StemSeries spectrumSeries = null)
        {
            if (Spectrum == null) return new AutoAdjustedYPlotModel(new LinearAxis(), 1.05);
            // Filtered/Deconvoluted Spectrum?
            var spectrum = Spectrum;
            var tolerance = (Spectrum is ProductSpectrum)
                                ? IcParameters.Instance.ProductIonTolerancePpm
                                : IcParameters.Instance.PrecursorTolerancePpm;
            if (spectrumSeries == null)
            {
                if (ShowFilteredSpectrum && ShowDeconvolutedSpectrum)
                {
                    if (_filtDeconSpectrum == null)
                    {
                        if (_filteredSpectrum == null)
                            _filteredSpectrum =
                                Spectrum.GetFilteredSpectrumBySlope(IcParameters.Instance.SpectrumFilterSlope);
                        _filtDeconSpectrum =
                            ProductScorerBasedOnDeconvolutedSpectra.GetDeconvolutedSpectrum(_filteredSpectrum,
                                Constants.MinCharge, Constants.MaxCharge, tolerance,
                                IcParameters.Instance.IonCorrelationThreshold, Constants.IsotopeOffsetTolerance);
                    }
                    spectrum = _filtDeconSpectrum;
                }
                else if (ShowFilteredSpectrum)
                {
                    if (_filteredSpectrum == null)
                        _filteredSpectrum =
                            Spectrum.GetFilteredSpectrumBySlope(IcParameters.Instance.SpectrumFilterSlope);
                    spectrum = _filteredSpectrum;
                }
                else if (ShowDeconvolutedSpectrum)
                {
                    if (_deconvolutedSpectrum == null)
                        _deconvolutedSpectrum = ProductScorerBasedOnDeconvolutedSpectra.GetDeconvolutedSpectrum(
                            spectrum,
                            Constants.MinCharge, Constants.MaxCharge, tolerance,
                            IcParameters.Instance.IonCorrelationThreshold, Constants.IsotopeOffsetTolerance);
                    spectrum = _deconvolutedSpectrum;
                }
                spectrumSeries = new StemSeries(OxyColors.Black, 0.5)
                {
                    TrackerFormatString =
                        "{0}" + Environment.NewLine +
                        "{1}: {2:0.###}" + Environment.NewLine +
                        "{3}: {4:0.##E0}"
                };
                if (ShowUnexplainedPeaks)
                {
                    foreach (var peak in spectrum.Peaks) spectrumSeries.Points.Add(new DataPoint(peak.Mz, peak.Intensity));
                }
            }
            // Create XAxis if there is none
            var xAxis = _xAxis ?? (_xAxis = GenerateXAxis(spectrum));
            var plot = new AutoAdjustedYPlotModel(xAxis, _multiplier)
            {
                Title = Title,
                TitleFontSize = 14,
                TitlePadding = 0
            };
            if (ShowUnexplainedPeaks) plot.Series.Add(spectrumSeries);
            SetPlotSeries(plot, spectrum);
            plot.GenerateYAxis("Intensity", "0e0");
            return plot;
        }

        private void SetPlotSeries(PlotModel plot, Spectrum spectrum)
        {
            if (plot == null || Ions == null || Ions.Count == 0) return;
            // add new ion series
            var seriesstore = Ions.ToDictionary<LabeledIonViewModel, string, Tuple<Series, Annotation>>(ionVm => ionVm.LabeledIon.Label, ion => null);
            var colors = new ColorDictionary(Math.Min(Math.Max(SelectedPrSmViewModel.Instance.Charge - 1, 2), 15));
            Parallel.ForEach(Ions, ionVm =>
            {
                if (ionVm.LabeledIon.IonType.Charge == 0) return;
                var ionSeries = _ionCache.ContainsKey(ionVm.LabeledIon.Label) ? _ionCache[ionVm.LabeledIon.Label] : GetIonSeries(ionVm.LabeledIon, spectrum, colors);
                seriesstore[ionVm.LabeledIon.Label] = ionSeries;
            });
            foreach (var series in seriesstore.Where(series => series.Value != null))
            {
                plot.Series.Add(series.Value.Item1);
                plot.Annotations.Add(series.Value.Item2);
                if (!_ionCache.ContainsKey(series.Key)) _ionCache.Add(series.Key, series.Value);
                else _ionCache[series.Key] = series.Value;
            }
        }

        private Tuple<Series, Annotation> GetIonSeries(LabeledIon labeledIon, Spectrum spectrum, ColorDictionary colors)
        {
            var labeledIonPeaks = IonUtils.GetIonPeaks(labeledIon, spectrum,
                                                       IcParameters.Instance.ProductIonTolerancePpm,
                                                       IcParameters.Instance.PrecursorTolerancePpm);
            // create plots
            var color = colors.GetColor(labeledIon);
            var ionSeries = new StemSeries(color, 1.5)
            {
                TrackerFormatString =
                    "{0}" + Environment.NewLine +
                    "{1}: {2:0.###}" + Environment.NewLine +
                    "{3}: {4:0.##E0}" + Environment.NewLine +
                    "Error: {Error:G4}ppm" + Environment.NewLine +
                    "Correlation: {Correlation:0.###}"
            };
            if (labeledIonPeaks.IsFragmentIon &&
                labeledIonPeaks.CorrelationScore < IcParameters.Instance.IonCorrelationThreshold) return null;
            var obsPeaks = labeledIonPeaks.Peaks;
            if (obsPeaks == null || obsPeaks.Length < 1) return null;
            Peak maxPeak = null;
            var errors = IonUtils.GetIsotopePpmError(obsPeaks, labeledIonPeaks.Ion, 0.1);
            for (int i = 0; i < errors.Length; i++)
            {
                if (errors[i] != null)
                {
                    ionSeries.Points.Add(new PeakDataPoint(obsPeaks[i].Mz, obsPeaks[i].Intensity, errors[i].Value, labeledIonPeaks.CorrelationScore));
                    // Find most intense peak
                    if (maxPeak == null || obsPeaks[i].Intensity >= maxPeak.Intensity) maxPeak = obsPeaks[i];
                }
            }
            if (maxPeak == null) return null;
            // Create ion name annotation
            var annotation = new TextAnnotation
            {
                Text = labeledIon.Label,
                TextColor = color,
                FontWeight = FontWeights.Bold,
                Layer = AnnotationLayer.AboveSeries,
                Background = OxyColors.White,
                Padding = new OxyThickness(0.1),
                Position = new DataPoint(maxPeak.Mz, maxPeak.Intensity),
                StrokeThickness = 0
            };
            return new Tuple<Series, Annotation>(ionSeries, annotation);
        }

        private LinearAxis GenerateXAxis(Spectrum spectrum)
        {
            var peaks = spectrum.Peaks;
            var ms2MaxMz = 1.0;    // plot maximum needs to be bigger than 0
            if (peaks.Length > 0) ms2MaxMz = peaks.Max().Mz * 1.1;
            var xAxis = new LinearAxis(AxisPosition.Bottom, "M/Z")
            {
                Minimum = 0,
                Maximum = ms2MaxMz,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = ms2MaxMz
            };
            xAxis.Zoom(0, ms2MaxMz);
            return xAxis;
        }

        private void SaveAsImage()
        {
            var fileName = _dialogService.SaveFile(".png", @"Png Files (*.png)|*.png");
            try
            {
                if (fileName != "")
                {
                    PngExporter.Export(Plot, fileName, (int)Plot.Width, (int)Plot.Height);
                }
            }
            catch (Exception e)
            {
                _dialogService.ExceptionAlert(e);
            }
        }

        private string _title;
        private LinearAxis _xAxis;
        private readonly double _multiplier;

        private bool _showFilteredSpectrum;
        private Spectrum _filteredSpectrum;
        private Spectrum _deconvolutedSpectrum;
        private Spectrum _filtDeconSpectrum;
        private Spectrum _spectrum;

        private List<LabeledIonViewModel> _ions;
        private bool _showUnexplainedPeaks;
        private readonly IDialogService _dialogService;

        private readonly Dictionary<string, Tuple<Series, Annotation>> _ionCache;
        private bool _showDeconvolutedSpectrum;
        private AutoAdjustedYPlotModel _plot;
    }
}
