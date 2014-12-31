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
        public RelayCommand<string> SaveAsImageCommand { get; private set; }

        public SpectrumPlotViewModel(IDialogService dialogService, ITaskService taskService, IMessenger messenger,  double multiplier, bool updateXAxis)
        {
            MessengerInstance = messenger;
            MessengerInstance.Register<SettingsChangedNotification>(this, SettingsChanged);
            MessengerInstance.Register<PropertyChangedMessage<int>>(this, SelectedChargeChanged);
            MessengerInstance.Register<PropertyChangedMessage<bool>>(this, LabeledIonSelectedChanged);
            //Messenger.Default.Register<PropertyChangedMessage<double>>(this, PrecursorChanged);
            _taskService = taskService;
            SaveAsImageCommand = new RelayCommand<string>(SaveAsImage);
            _dialogService = dialogService;
            _showUnexplainedPeaks = true;
            _showFilteredSpectrum = false;
            _showDeconvolutedSpectrum = false;
            _multiplier = multiplier;
            _updateXAxis = updateXAxis;
            Title = "";
            _ionCache = new Dictionary<string, IEnumerable<PeakDataPoint>>();
            Clear();
        }

        /// <summary>
        /// The spectrum plot.
        /// </summary>
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
        /// Title of spectrum plot.
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
                if (_updateXAxis) SpectrumUpdate(_spectrum, _xAxis);
                else SpectrumUpdate();
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
                if (_updateXAxis) SpectrumUpdate(_spectrum, _xAxis);
                else SpectrumUpdate();
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Update spectrum and ion highlights
        /// </summary>
        public void SpectrumUpdate(Spectrum spectrum=null, LinearAxis xAxis=null, bool fullUpdate=true)
        {
            _taskService.Enqueue(() =>
            {
                if (spectrum != null) _spectrum = spectrum;
                _xAxis = xAxis;
                _filteredSpectrum = null; // reset filtered spectrum
                _deconvolutedSpectrum = null;
                _filtDeconSpectrum = null; 
                Plot = BuildSpectrumPlot();
                if (fullUpdate) IonUpdate();
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
                var s = CreateIonSeries(useCache);
                SetPlotSeries(s.Item1, s.Item2);
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

        private void SelectedChargeChanged(PropertyChangedMessage<int> message)
        {
            if (message.PropertyName == "Charge")
            {
                _selectedCharge = message.NewValue;
            }
        }
        
        private AutoAdjustedYPlotModel BuildSpectrumPlot()
        {
            if (_spectrum == null) return new AutoAdjustedYPlotModel(new LinearAxis{Position=AxisPosition.Bottom}, 1.05);
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
                        _filteredSpectrum = _spectrum.GetFilteredSpectrumByIntensityHistogram(IcParameters.Instance.SpectrumFilterWindowSize);
                    _filtDeconSpectrum = ProductScorerBasedOnDeconvolutedSpectra.GetDeconvolutedSpectrum(spectrum,
                                    Constants.MinCharge, Constants.MaxCharge, tolerance,
                                    IcParameters.Instance.IonCorrelationThreshold, Constants.IsotopeOffsetTolerance);
                }
                spectrum = _filtDeconSpectrum;
            }
            else if (ShowFilteredSpectrum)
            {
                if (_filteredSpectrum == null)
                    _filteredSpectrum = _spectrum.GetFilteredSpectrumByIntensityHistogram(IcParameters.Instance.SpectrumFilterWindowSize);
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
            var spectrumSeries = new StemSeries
            {
                Color=OxyColors.Black,
                StrokeThickness = 0.5,
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

        private void SetPlotSeries(IEnumerable<Series> series, IEnumerable<Annotation> annotations)
        {
            if (Plot == null || _currentSpectrum == null) return;
            StemSeries spectrumSeries = null;
            if (ShowUnexplainedPeaks && Plot.Series.Count > 0) spectrumSeries = Plot.Series[0] as StemSeries;
            Plot.Series.Clear();
            Plot.Annotations.Clear();
            if (spectrumSeries != null) Plot.Series.Add(spectrumSeries);
            foreach (var s in series) if (s != null) Plot.Series.Add(s);
            foreach (var a in annotations) if (a != null) Plot.Annotations.Add(a);
            Plot.IsLegendVisible = false;
            Plot.AdjustForZoom();
            Plot.InvalidatePlot(true);
        }

        private Tuple<IEnumerable<Series>, IEnumerable<Annotation>>  CreateIonSeries(bool useCache=true)
        {
            if (_ions == null) return new Tuple<IEnumerable<Series>, IEnumerable<Annotation>>(new List<Series>(), new List<Annotation>());
            if (!useCache) _ionCache.Clear();
            // add new ion series
            var seriesstore = new Dictionary<string, Tuple<IEnumerable<PeakDataPoint>, Series, Annotation>>();
            foreach (var ion in _ions)
            {
                if (!seriesstore.ContainsKey(ion.LabeledIon.Label)) seriesstore.Add(ion.LabeledIon.Label, null);
            }
            var colors = new ColorDictionary(Math.Min(Math.Max(_selectedCharge - 1, 2), 15));
            IonTypeFactory deconIonTypeFactory = null;
            if (ShowDeconvolutedSpectrum) deconIonTypeFactory = IonTypeFactory.GetDeconvolutedIonTypeFactory(BaseIonType.AllBaseIonTypes,
                                                                                   NeutralLoss.CommonNeutralLosses);
            Parallel.ForEach(_ions, ionVm =>
            {
                var ditf = deconIonTypeFactory;
                if (ionVm.LabeledIon.IonType.Charge == 0 || (ShowDeconvolutedSpectrum && ionVm.LabeledIon.IonType.Charge > 1)) return;
                var ionPeaks = (useCache && _ionCache.ContainsKey(ionVm.LabeledIon.Label)) ? _ionCache[ionVm.LabeledIon.Label] : GetPeakDataPoints(ionVm.LabeledIon, _currentSpectrum, ditf);
                var ionSeries = GetIonSeries(ionVm.LabeledIon, ionPeaks, colors);
                seriesstore[ionVm.LabeledIon.Label] = ionSeries;
            });
            if (useCache)
            {
                foreach (var series in seriesstore.Where(series => series.Value != null))
                {
                        if (!_ionCache.ContainsKey(series.Key)) _ionCache.Add(series.Key, series.Value.Item1);
                        else _ionCache[series.Key] = series.Value.Item1;
                }   
            }
            var values = seriesstore.Values.ToList();
            var s = new Series[values.Count];
            var a = new Annotation[values.Count];
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null) continue;
                s[i] = values[i].Item2;
                a[i] = values[i].Item3;
            }
            return new Tuple<IEnumerable<Series>, IEnumerable<Annotation>>(s, a);
        }

        private IEnumerable<PeakDataPoint> GetPeakDataPoints(LabeledIon labeledIon, Spectrum spectrum, IonTypeFactory ionTypeFactory = null)
        {
            if (ShowDeconvolutedSpectrum && ionTypeFactory != null && labeledIon.IsFragmentIon)
            {
                labeledIon = IonUtils.GetDeconvolutedLabeledIon(labeledIon, ionTypeFactory);
            }
            var labeledIonPeaks = IonUtils.GetIonPeaks(labeledIon, spectrum,
                                           IcParameters.Instance.ProductIonTolerancePpm,
                                           IcParameters.Instance.PrecursorTolerancePpm, ShowDeconvolutedSpectrum);
            var obsPeaks = labeledIonPeaks.Peaks;
            if (labeledIonPeaks.CorrelationScore < IcParameters.Instance.IonCorrelationThreshold || 
                obsPeaks == null || 
                obsPeaks.Length < 1) 
                    return null;
            var errors = IonUtils.GetIsotopePpmError(obsPeaks, labeledIonPeaks.Ion, 0.1);
            var peakDataPoints = new List<PeakDataPoint> {Capacity = errors.Length};
            for (int i = 0; i < errors.Length; i++)
            {
                if (errors[i] != null)
                {
                    peakDataPoints.Add(new PeakDataPoint(obsPeaks[i].Mz, obsPeaks[i].Intensity, errors[i].Value, labeledIonPeaks.CorrelationScore));
                }
            }
            return peakDataPoints;
        }

        private Tuple<IEnumerable<PeakDataPoint>, Series, Annotation> GetIonSeries(LabeledIon labeledIon, IEnumerable<PeakDataPoint> peakDataPoints, ColorDictionary colors)
        {
            if (peakDataPoints == null) return null;
            PeakDataPoint maxPeak = null;
            var color = colors.GetColor(labeledIon);
            var peaks = peakDataPoints as IList<PeakDataPoint> ?? peakDataPoints.ToList();
            var ionSeries = new StemSeries
            {
                Color = color,
                StrokeThickness = 1.5,
                ItemsSource = peaks,
                Mapping = x => new DataPoint(((PeakDataPoint)x).X, ((PeakDataPoint)x).Y),
                Title = labeledIon.Label,
                TrackerFormatString =
                    "{0}" + Environment.NewLine +
                    "{1}: {2:0.###}" + Environment.NewLine +
                    "{3}: {4:0.##E0}" + Environment.NewLine +
                    "Error: {Error:G4}ppm" + Environment.NewLine +
                    "Correlation: {Correlation:0.###}"
            };
            foreach (PeakDataPoint p in peaks)
            {
                if (maxPeak == null || p.Y >= maxPeak.Y) maxPeak = p;
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
                TextPosition = new DataPoint(maxPeak.X, maxPeak.Y),
                StrokeThickness = 0
            };
            return new Tuple<IEnumerable<PeakDataPoint>, Series, Annotation>(peaks, ionSeries, annotation);
        }

        private LinearAxis GenerateXAxis(Spectrum spectrum)
        {
            var peaks = spectrum.Peaks;
            var ms2MaxMz = 1.0;    // plot maximum needs to be bigger than 0
            if (peaks.Length > 0) ms2MaxMz = peaks.Max().Mz * 1.1;
            var xAxis = new LinearAxis
            {
                //Minimum = 0,
                //Maximum = ms2MaxMz,
                Position = AxisPosition.Bottom,
                Title="Mz",
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
                        textAnnotation.FontSize = message.NewValue ? 0 : 12;
                    }
                }
            }
        }

        private void SettingsChanged(SettingsChangedNotification notification)
        {
            if (_updateXAxis) SpectrumUpdate(_spectrum, _xAxis);
            else SpectrumUpdate();
        }

        //private void PrecursorChanged(PropertyChangedMessage<double> message)
        //{
        //    if (message.PropertyName == "PrecursorMz")
        //    {
        //        _taskService.Enqueue(() =>_updateIons = false);
        //    }
        //}

        private void SaveAsImage(string fileType)
        {
            string fileName = fileType == "png" ? _dialogService.SaveFile(".png", @"Png Files (*.png)|*.png") 
                : _dialogService.SaveFile(".svg", @"Svg Files (*.svg)|*.svg");
            try
            {
                if (fileName != "" && fileType == "png")
                {
                    PngExporter.Export(Plot, fileName, (int)Plot.Width, (int)Plot.Height, OxyColors.White);
                }
                else if (fileName != "" && fileType == "svg")
                {
                    /*using (var svgStream = )
                    {
                        OxyPlot.SvgExporter.Export(Plot, svgStream, (int)Plot.Width, (int)Plot.Height, false);   
                    }*/
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
        private readonly bool _updateXAxis;


        private readonly Dictionary<string, IEnumerable<PeakDataPoint>> _ionCache;
        private bool _showDeconvolutedSpectrum;
        private AutoAdjustedYPlotModel _plot;

        private int _selectedCharge;
    }
}
