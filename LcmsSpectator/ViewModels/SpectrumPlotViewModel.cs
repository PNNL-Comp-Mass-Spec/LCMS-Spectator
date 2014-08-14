using System;
using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.PlotModels;
using LcmsSpectator.Utils;
using LcmsSpectatorModels.Config;
using LcmsSpectatorModels.Models;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace LcmsSpectator.ViewModels
{
    public class SpectrumPlotViewModel: ViewModelBase
    {
        public AutoAdjustedYPlotModel Plot { get; set; }
        public SpectrumPlotViewModel(double multiplier, ColorDictionary colors, bool showUnexplainedPeaks=true)
        {
            _showUnexplainedPeaks = showUnexplainedPeaks;
            _multiplier = multiplier;
            _colors = colors;
            Title = "";
        }

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

        public Spectrum Spectrum
        {
            get { return _spectrum; }
            set
            {
                if (_spectrum == value) return;
                _spectrum = value;
                BuildSpectrumPlot();
                OnPropertyChanged("Spectrum");
            }
        }

        public List<LabeledIon> Ions
        {
            get { return _ions;  }
            set
            {
                _ions = value;
                SetPlotSeries();
                OnPropertyChanged("Ions");
            }
        }

        public void AddIonHighlight(LabeledIon labeledIon)
        {
            if (labeledIon == null) return;
            var ionSeries = GetIonSeries(labeledIon);
            if (ionSeries == null) return;
            GuiInvoker.Invoke(Plot.Series.Add, ionSeries.Item1);
            GuiInvoker.Invoke(Plot.Annotations.Add, ionSeries.Item2);
            GuiInvoker.Invoke(Plot.InvalidatePlot, true);
        }

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

        public void Update(Spectrum spectrum, List<LabeledIon> ions)
        {
            _spectrum = spectrum;
            _ions = ions;
            BuildSpectrumPlot();
        }

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
            Plot = new AutoAdjustedYPlotModel(xAxis, _multiplier)
            {
                Title = Title,
                TitleFontSize = 14,
                TitlePadding = 0
            };
            var spectrumSeries = new StemSeries(OxyColors.Black, 0.5);
            foreach (var peak in Spectrum.Peaks) spectrumSeries.Points.Add(new DataPoint(peak.Mz, peak.Intensity));
            GuiInvoker.Invoke(Plot.Series.Add, spectrumSeries);
            Plot.GenerateYAxis("Intensity", "0e0");
            SetPlotSeries();
            OnPropertyChanged("Plot");
        }

        private void SetPlotSeries()
        {
            if (Plot == null || Ions == null) return;
            // remove old ion series
            while (Plot.Series.Count > 1)
            {
                GuiInvoker.Invoke(Plot.Series.RemoveAt, Plot.Series.Count-1);
            }
            Plot.Annotations.Clear();
            // add new ion series
            foreach (var labeledIon in Ions)
            {
                var ionSeries = GetIonSeries(labeledIon);
                if (ionSeries == null) continue;
                GuiInvoker.Invoke(Plot.Series.Add, ionSeries.Item1);
                GuiInvoker.Invoke(Plot.Annotations.Add, ionSeries.Item2);
            }
            Plot.InvalidatePlot(true);
        }

        private Tuple<Series, Annotation> GetIonSeries(LabeledIon labeledIon)
        {
            var labeledIonPeaks = GetIonPeaks(labeledIon);
            // create plots
            var color = _colors.GetColor(labeledIon);
            var ionSeries = new StemSeries(color, 1.5);
            var isotopePeaks = labeledIonPeaks.Peaks;
            if (isotopePeaks == null || isotopePeaks.Length < 1) return null;
            Peak maxPeak = null;
            foreach (var peak in isotopePeaks.Where(peak => peak != null))
            {
                ionSeries.Points.Add(new DataPoint(peak.Mz, peak.Intensity));
                // Find most intense peak
                if (maxPeak == null || peak.Intensity >= maxPeak.Intensity) maxPeak = peak;
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

        private LabeledIonPeaks GetIonPeaks(LabeledIon ion)
        {
            var ionCorrelation = Spectrum.GetCorrScore(ion.Ion, IcParameters.Instance.ProductIonTolerancePpm);
            var isotopePeaks = Spectrum.GetAllIsotopePeaks(ion.Ion, IcParameters.Instance.PrecursorTolerancePpm, 0.1);
            return new LabeledIonPeaks(ion.Composition, ion.Index, isotopePeaks, ionCorrelation, ion.IonType, ion.IsFragmentIon);
        }

        private LinearAxis GenerateXAxis()
        {
            var ms2MaxMz = Spectrum.Peaks.Max().Mz * 1.2;
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

        private readonly ColorDictionary _colors;
        private string _title;
        private LinearAxis _xAxis;
        private readonly double _multiplier;

        private Spectrum _spectrum;
        private List<LabeledIon> _ions;
        private bool _showUnexplainedPeaks;
    }
}
