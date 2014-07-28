using System;
using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectatorModels.Models;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace LcmsSpectator.PlotModels
{
    public class SpectrumPlotModel: AutoAdjustedYPlotModel
    {
        public Spectrum Spectrum { get; private set; }

        public SpectrumPlotModel(string title, Spectrum spectrum, IEnumerable<LabeledIonPeaks> ions, ColorDictionary colors, Axis xAxis, double mult): base(xAxis, mult)
        {
            _title = title;
            Spectrum = spectrum;
            _colors = colors;
            GeneratePlot(ions);
        }

        private void GeneratePlot(IEnumerable<LabeledIonPeaks> ions)
        {
            Title = _title;
            var spectrumSeries = new StemSeries(OxyColors.Black, 0.5);
            foreach (var peak in Spectrum.Peaks) spectrumSeries.Points.Add(new DataPoint(peak.Mz, peak.Intensity));
            Series.Add(spectrumSeries);
            GenerateYAxis("Intensity", "0e0");
            var ionHighlights = GetIonSeries(ions, _colors);
            foreach (var s in ionHighlights.Item1) Series.Add(s);
            foreach (var a in ionHighlights.Item2) Annotations.Add(a);
        }

        private Tuple<List<Series>, List<Annotation>> GetIonSeries(IEnumerable<LabeledIonPeaks> ions, ColorDictionary colors)
        {
            var series = new List<Series>();
            var annotations = new List<Annotation>();
            // Create ion series
            foreach (var ion in ions)
            {
                var color = colors.GetColor(ion);
                if (ion.CorrelationScore < PrSm.CorrelationThreshold) continue;
                var ionSeries = new StemSeries(color, 1.5);
                var isotopePeaks = ion.Peaks;
                if (isotopePeaks == null || isotopePeaks.Length < 1) continue;
                Peak maxPeak = null;
                foreach (var peak in isotopePeaks.Where(peak => peak != null))
                {
                    ionSeries.Points.Add(new DataPoint(peak.Mz, peak.Intensity));
                    // Find most intense peak
                    if (maxPeak == null || peak.Intensity >= maxPeak.Intensity) maxPeak = peak;
                }
                if (maxPeak == null) continue;
                // Create ion name annotation
                var annotation = new TextAnnotation
                {
                    Text = ion.Label,
                    TextColor = color,
                    FontWeight = FontWeights.Bold,
                    Layer = AnnotationLayer.AboveSeries,
                    Background = OxyColors.White,
                    Padding = new OxyThickness(0.1),
                    Position = new DataPoint(maxPeak.Mz, maxPeak.Intensity),
                    StrokeThickness = 0
                };
                series.Add(ionSeries);
                annotations.Add(annotation);
            }
            return new Tuple<List<Series>, List<Annotation>>(series, annotations);
        }

        private string _title;
        private readonly ColorDictionary _colors;
    }
}
