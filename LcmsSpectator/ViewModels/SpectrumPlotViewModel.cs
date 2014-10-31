using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.TopDown.Scoring;
using LcmsSpectator.DialogServices;
using LcmsSpectator.PlotModels;
using LcmsSpectator.TaskServices;
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
using LineSeries = OxyPlot.Series.LineSeries;
using Series = OxyPlot.Series.Series;
using TextAnnotation = OxyPlot.Annotations.TextAnnotation;

namespace LcmsSpectator.ViewModels
{
    public class SpectrumPlotViewModel: ViewModelBase
    {
        public RelayCommand SaveAsImageCommand { get; private set; }

        public SpectrumPlotViewModel(IDialogService dialogService, ITaskService taskService, double multiplier, bool showUnexplainedPeaks=true)
        {
            Messenger.Default.Register<PropertyChangedMessage<bool>>(this, LabeledIonSelectedChanged);
            //Messenger.Default.Register<PropertyChangedMessage<double>>(this, PrecursorChanged);
            _taskService = taskService;
            SaveAsImageCommand = new RelayCommand(SaveAsImage);
            _dialogService = dialogService;
            _showUnexplainedPeaks = showUnexplainedPeaks;
            _showFilteredSpectrum = false;
            _showDeconvolutedSpectrum = false;
            _multiplier = multiplier;
            _updateIons = false;
            Title = "";
            _ionCache = new Dictionary<string, Tuple<Series, Annotation>>();
            Clear();
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
                SpectrumUpdate();
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Update spectrum and ion highlights
        /// </summary>
        public void SpectrumUpdate(Spectrum spectrum=null, LinearAxis xAxis=null)
        {
            _taskService.Enqueue(() =>
            {
                if (spectrum != null) _spectrum = spectrum;
                _xAxis = xAxis;
                _filteredSpectrum = null; // reset filtered spectrum
                _deconvolutedSpectrum = null;
                _filtDeconSpectrum = null; 
                Plot = BuildSpectrumPlot();
                IonUpdate();
                /*if (_updateIons) IonUpdate();
                else if (_ions != null) _ions.Clear();
                _updateIons = true;*/
            });
        }

        public void IonUpdate(List<LabeledIonViewModel> ions = null)
        {
            _taskService.Enqueue(() =>
            {
                bool useCache = false;
                if (ions != null)
                {
                    _ions = ions;
                    useCache = true;
                }
                SetPlotSeries(useCache);
            });
        }

        /// <summary>
        /// Clear spectrum plot
        /// </summary>
        public void Clear()
        {
            _taskService.Enqueue(() =>
            {
                _spectrum = null;
                _xAxis = null;
                _filteredSpectrum = null; // reset filtered spectrum
                _deconvolutedSpectrum = null;
                _filtDeconSpectrum = null;
                Plot = BuildSpectrumPlot();
                IonUpdate();
            });
        }
        
        private AutoAdjustedYPlotModel BuildSpectrumPlot()
        {
            if (_spectrum == null) return new AutoAdjustedYPlotModel(new LinearAxis(AxisPosition.Bottom), 1.05);
            // Filtered/Deconvoluted Spectrum?
            var spectrum = _spectrum;
            var tolerance = (_spectrum is ProductSpectrum)
                                ? IcParameters.Instance.ProductIonTolerancePpm
                                : IcParameters.Instance.PrecursorTolerancePpm;
            if (ShowFilteredSpectrum && ShowDeconvolutedSpectrum)
            {
                if (_filtDeconSpectrum == null)
                {
                    if (_filteredSpectrum == null)
                        _filteredSpectrum = _spectrum.GetFilteredSpectrumByIntensityHistogram();
                    _filtDeconSpectrum = ProductScorerBasedOnDeconvolutedSpectra.GetDeconvolutedSpectrum(spectrum,
                                    Constants.MinCharge, Constants.MaxCharge, tolerance,
                                    IcParameters.Instance.IonCorrelationThreshold, Constants.IsotopeOffsetTolerance);
                }
                spectrum = _filtDeconSpectrum;
            }
            else if (ShowFilteredSpectrum)
            {
                if (_filteredSpectrum == null)
                    _filteredSpectrum = _spectrum.GetFilteredSpectrumByIntensityHistogram();
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
            _currentSpectrum = spectrum;
            var spectrumSeries = new StemSeries(OxyColors.Black, 0.5)
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
            // Create XAxis if there is none
            var xAxis = _xAxis ?? (_xAxis = GenerateXAxis(spectrum));
            var plot = new AutoAdjustedYPlotModel(xAxis, _multiplier)
            {
                Title = Title,
                TitleFontSize = 14,
                TitlePadding = 0
            };
            if (ShowUnexplainedPeaks) plot.Series.Add(spectrumSeries);
            plot.GenerateYAxis("Intensity", "0e0");
            return plot;
        }

        private void SetPlotSeries(bool useCache=true)
        {
            if (Plot == null || _ions == null || _ions.Count == 0 || _currentSpectrum == null) return;
            StemSeries spectrumSeries = null;
            if (ShowUnexplainedPeaks && Plot.Series.Count > 0) spectrumSeries = Plot.Series[0] as StemSeries;
            if (!useCache) _ionCache.Clear();
            GuiInvoker.Invoke(() =>
            {
                Plot.Series.Clear();
                Plot.Annotations.Clear();
                if (spectrumSeries != null) Plot.Series.Add(spectrumSeries);
            });
            // add new ion series
            var seriesstore = new Dictionary<string, Tuple<Series, Annotation>>();
            foreach (var ion in _ions)
            {
                if (!seriesstore.ContainsKey(ion.LabeledIon.Label)) seriesstore.Add(ion.LabeledIon.Label, null);
            }
            var colors = new ColorDictionary(Math.Min(Math.Max(SelectedPrSmViewModel.Instance.Charge - 1, 2), 15));
            Parallel.ForEach(_ions, ionVm =>
            {
                if (ionVm.LabeledIon.IonType.Charge == 0 || (ShowDeconvolutedSpectrum && ionVm.LabeledIon.IonType.Charge > 1)) return;
                var ionSeries = (useCache && _ionCache.ContainsKey(ionVm.LabeledIon.Label)) ? _ionCache[ionVm.LabeledIon.Label] : GetIonSeries(ionVm.LabeledIon, _currentSpectrum, colors);
                seriesstore[ionVm.LabeledIon.Label] = ionSeries;
            });
            foreach (var series in seriesstore.Where(series => series.Value != null))
            {
                KeyValuePair<string, Tuple<Series, Annotation>> series1 = series;
                GuiInvoker.Invoke(() =>
                {
                    Plot.Series.Add(series1.Value.Item1);
                    Plot.Annotations.Add(series1.Value.Item2); 
                });
                if (useCache)
                {
                    if (!_ionCache.ContainsKey(series.Key)) _ionCache.Add(series.Key, series.Value);
                    else _ionCache[series.Key] = series.Value;
                }
            }
            Plot.IsLegendVisible = false;
            Plot.InvalidatePlot(true);
            Plot.AdjustForZoom();
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
                Title = labeledIon.Label,
                TrackerFormatString =
                    "{0}" + Environment.NewLine +
                    "{1}: {2:0.###}" + Environment.NewLine +
                    "{3}: {4:0.##E0}" + Environment.NewLine +
                    "Error: {Error:G4}ppm" + Environment.NewLine +
                    "Correlation: {Correlation:0.###}"
            };
            if (labeledIonPeaks.IsFragmentIon && !ShowDeconvolutedSpectrum &&
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
                FontSize = 12,
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
                //Minimum = 0,
                //Maximum = ms2MaxMz,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = ms2MaxMz
            };
            xAxis.Zoom(0, ms2MaxMz);
            return xAxis;
        }

        private void LabeledIonSelectedChanged(PropertyChangedMessage<bool> message)
        {
            if (message.PropertyName == "Selected" && message.Sender is LabeledIonViewModel)
            {
                var labeledIonVm = message.Sender as LabeledIonViewModel;
                var label = labeledIonVm.LabeledIon.Label;
                foreach (var series in Plot.Series)
                {
                    var lineSeries = series as StemSeries;
                    if (lineSeries != null && lineSeries.Title == label)
                    {
                        lineSeries.IsVisible = message.NewValue;
                        Plot.AdjustForZoom();
                    }
                }
                foreach (var annotation in Plot.Annotations)
                {
                    var textAnnotation = annotation as TextAnnotation;
                    if (textAnnotation != null && textAnnotation.Text == label)
                    {
                        if (message.NewValue) textAnnotation.FontSize = 0;
                        else textAnnotation.FontSize = 12;
                    }
                }
            }
        }

        private void PrecursorChanged(PropertyChangedMessage<double> message)
        {
            if (message.PropertyName == "PrecursorMz")
            {
                _taskService.Enqueue(() =>_updateIons = false);
            }
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

        private readonly IDialogService _dialogService;
        private readonly ITaskService _taskService;

        private string _title;
        private LinearAxis _xAxis;
        private readonly double _multiplier;

        private bool _showFilteredSpectrum;
        private Spectrum _currentSpectrum;
        private Spectrum _filteredSpectrum;
        private Spectrum _deconvolutedSpectrum;
        private Spectrum _filtDeconSpectrum;
        private Spectrum _spectrum;

        private List<LabeledIonViewModel> _ions;
        private bool _showUnexplainedPeaks;

        private bool _updateIons;

        private readonly Dictionary<string, Tuple<Series, Annotation>> _ionCache;
        private bool _showDeconvolutedSpectrum;
        private AutoAdjustedYPlotModel _plot;
    }
}
