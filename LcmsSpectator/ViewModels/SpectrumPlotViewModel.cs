using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InformedProteomics.Backend.Data.Spectrometry;
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
        public DelegateCommand SaveAsImageCommand { get; private set; }
        public AutoAdjustedYPlotModel Plot { get; private set; }
        public SpectrumPlotViewModel(IDialogService dialogService, double multiplier, ColorDictionary colors, bool showUnexplainedPeaks=true)
        {
            SaveAsImageCommand = new DelegateCommand(SaveAsImage);
            _dialogService = dialogService;
            _showUnexplainedPeaks = showUnexplainedPeaks;
            _multiplier = multiplier;
            _colors = colors;
            Title = "";
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
                if (Plot != null) Plot.Title = _title;
                OnPropertyChanged("Title");
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
                if (_showUnexplainedPeaks == false && Plot.Series.Count > 0)
                {
                    GuiInvoker.Invoke(Plot.Series.RemoveAt, 0);
                    GuiInvoker.Invoke(Plot.AdjustForZoom);
                    GuiInvoker.Invoke(Plot.InvalidatePlot, true);
                }
                else GuiInvoker.Invoke(BuildSpectrumPlot);
                OnPropertyChanged("ShowUnexplainedPeaks");
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
                Update();
                OnPropertyChanged("FilterSpectrum");
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
                OnPropertyChanged("Spectrum");
            }
        }

        /// <summary>
        /// Ions to get peak highlights for
        /// </summary>
        public List<LabeledIon> Ions
        {
            get { return _ions;  }
            set
            {
                _ions = value;
                OnPropertyChanged("Ions");
            }
        }

        /// <summary>
        /// Explicitely highlight ion peaks
        /// </summary>
        /// <param name="labeledIon"></param>
        public void AddIonHighlight(LabeledIon labeledIon)
        {
            if (labeledIon == null) return;
            var ionSeries = GetIonSeries(labeledIon);
            if (ionSeries == null) return;
            GuiInvoker.Invoke(Plot.Series.Add, ionSeries.Item1);
            GuiInvoker.Invoke(Plot.Annotations.Add, ionSeries.Item2);
            GuiInvoker.Invoke(Plot.InvalidatePlot, true);
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
                if (Plot == null) return;
                GuiInvoker.Invoke(Plot.Axes.Clear);
                GuiInvoker.Invoke(Plot.Axes.Add, _xAxis);
                Plot.GenerateYAxis("Intensity", "0e0");
                Plot.InvalidatePlot(true);
                OnPropertyChanged("XAxis");
            }
        }

        /// <summary>
        /// Update spectrum and ion highlights
        /// </summary>
        public Task Update()
        {
            return Task.Factory.StartNew(BuildSpectrumPlot);
        }

        /// <summary>
        /// Clear spectrum plot
        /// </summary>
        public void ClearPlot()
        {
            Plot = new AutoAdjustedYPlotModel(new LinearAxis { Minimum = 0, Maximum = 100, IsAxisVisible = false }, _multiplier);
            OnPropertyChanged("Plot");
        }

        private void BuildSpectrumPlot()
        {
            if (Spectrum == null)
            {
                ClearPlot();
                return;
            }
            var xAxis = _xAxis ?? GenerateXAxis();
            var plot = new AutoAdjustedYPlotModel(xAxis, _multiplier)
            {
                Title = Title,
                TitleFontSize = 14,
                TitlePadding = 0
            };
            var spectrumSeries = new StemSeries(OxyColors.Black, 0.5)
            {
                TrackerFormatString =
                    "{0}" + Environment.NewLine +
                    "{1}: {2:0.###}" + Environment.NewLine +
                    "{3}: {4:0.##E0}"
            };
            foreach (var peak in Spectrum.Peaks) spectrumSeries.Points.Add(new DataPoint(peak.Mz, peak.Intensity));
            GuiInvoker.Invoke(plot.Series.Add, spectrumSeries);
            plot.GenerateYAxis("Intensity", "0e0");
            SetPlotSeries(plot);
            Plot = plot;
            OnPropertyChanged("Plot");
        }

        private void SetPlotSeries(AutoAdjustedYPlotModel plot)
        {
            if (plot == null || Ions == null) return;
            // remove old ion series
            while (plot.Series.Count > 1)
            {
                GuiInvoker.Invoke(plot.Series.RemoveAt, Plot.Series.Count-1);
            }
            GuiInvoker.Invoke(plot.Annotations.Clear);
            // add new ion series
            foreach (var labeledIon in Ions)
            {
                if (labeledIon.IonType.Charge == 0) continue;
                var ionSeries = GetIonSeries(labeledIon);
                if (ionSeries == null) continue;
                GuiInvoker.Invoke(plot.Series.Add, ionSeries.Item1);
                GuiInvoker.Invoke(plot.Annotations.Add, ionSeries.Item2);
            }
            plot.InvalidatePlot(true);
        }

        private Tuple<Series, Annotation> GetIonSeries(LabeledIon labeledIon)
        {
            var labeledIonPeaks = IonUtils.GetIonPeaks(labeledIon, Spectrum,
                                                       IcParameters.Instance.ProductIonTolerancePpm,
                                                       IcParameters.Instance.PrecursorTolerancePpm);
            // create plots
            var color = _colors.GetColor(labeledIon);
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

        private LinearAxis GenerateXAxis()
        {
            var peaks = Spectrum.Peaks;
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
            if (Plot == null) return;
            var fileName = _dialogService.SaveFile(".png", @"Png Files (*.png)|*.png");
            try
            {
                if (fileName != "") PngExporter.Export(Plot, fileName, (int)Plot.Width, (int)Plot.Height);
            }
            catch (Exception e)
            {
                _dialogService.ExceptionAlert(e);
            }
        }

        private readonly ColorDictionary _colors;
        private string _title;
        private LinearAxis _xAxis;
        private readonly double _multiplier;

        private bool _showFilteredSpectrum;
        private Spectrum _filteredSpectrum;
        private Spectrum _spectrum;

        private List<LabeledIon> _ions;
        private bool _showUnexplainedPeaks;
        private readonly IDialogService _dialogService;
    }
}
