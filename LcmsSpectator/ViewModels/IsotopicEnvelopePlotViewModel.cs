using System;
using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.PlotModels;
using OxyPlot;
using OxyPlot.Axes;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
    public class IsotopicEnvelopePlotViewModel: ReactiveObject
    {
        /// <summary>
        /// Create a new instance of the IsotopicEnvelopePlotViewModel. 
        /// </summary>
        public IsotopicEnvelopePlotViewModel()
        {
            _xAxis = new LinearAxis { Position = AxisPosition.Bottom, Title = "Mass", StringFormat = "0.###" };
            PlotModel = new AutoAdjustedYPlotModel(_xAxis, 1.05)
            {
                Title = "Isotopic Envelope"
            };
            PlotModel.GenerateYAxis("Relative Intensity", "0e0");
        }

        /// <summary>
        /// The plot displaying theoretical isotopic profile and actual isotopic profile.
        /// </summary>
        public AutoAdjustedYPlotModel PlotModel { get; private set; }

        /// <summary>
        /// Build the isotope plot showing theoretical isotopic profile and
        /// actual isotopic profile.
        /// </summary>
        /// <param name="actual">Actual isotopic profile.</param>
        /// <param name="mass">Mass, for calculating theoretical isotopic profile.</param>
        /// <param name="charge">Charge, for calculating actual isotopic profile.</param>
        public void BuildPlot(Isotope[] actual, double mass, int charge)
        {
            // Calculate theoretical isotopic profile using averagine
            var theoEnvelope = Averagine.GetIsotopomerEnvelope(mass);
            var peakMap = new Dictionary<int, PeakDataPoint>();

            PlotModel.Series.Clear();


            // Create series for theoretical isotope profile
            var theoSeries = new PeakPointSeries
            {
                Title = "Theoretical",
                ItemsSource = peakMap.Values,
                Color = OxyColor.FromArgb(120, 0, 0, 0),
                StrokeThickness = 3.0,
                TrackerFormatString =
                "{0}" + Environment.NewLine +
                "{1}: {2:0.###}" + Environment.NewLine +
                "{3}: {4:0.###}" + Environment.NewLine +
                "Index: {Index:0.###}"
            };
            PlotModel.Series.Add(theoSeries);

            // Calculate m/z for each isotope index
            for (var isotopeIndex = 0; isotopeIndex < theoEnvelope.Envolope.Length; isotopeIndex++)
            {
                var intensity = theoEnvelope.Envolope[isotopeIndex];
                var mz = Ion.GetIsotopeMz(mass, charge, isotopeIndex);
                var m = (mz * charge * Constants.Proton) - charge * Constants.Proton;
                peakMap.Add(isotopeIndex, new PeakDataPoint(m, intensity, 0.0, 0.0, ""));
            }

            // Create peak data points from isotopes and calculate m/z values
            var isotopePeaks = actual.Select(i => new PeakDataPoint(peakMap[i.Index].X, i.Ratio, 0.0, 0.0, "") { Index = i.Index });

            // Create series for actual isotope profile
            var actSeries = new PeakPointSeries
            {
                Title = "Observed",
                ItemsSource = isotopePeaks,
                Color = OxyColor.FromArgb(120, 255, 0, 0),
                StrokeThickness = 3.0,
                TrackerFormatString =
                "{0}" + Environment.NewLine +
                "{1}: {2:0.###}" + Environment.NewLine +
                "{3}: {4:0.###}" + Environment.NewLine +
                "Index: {Index:0.###}"
            };
            PlotModel.Series.Add(actSeries);

            // Calculate min and max boundaries for plot
            var min = peakMap.Values.Min(p => p.X);
            var max = peakMap.Values.Max(p => p.X);
            min -= (max - min) / 3;
            var absMin = Math.Max(0, min - 10);
            max += (max - min) / 3;
            var absMax = max + 10;
            _xAxis.Minimum = min;
            _xAxis.AbsoluteMinimum = absMin;
            _xAxis.Maximum = max;
            _xAxis.AbsoluteMaximum = absMax;
            _xAxis.Zoom(min, max);

            PlotModel.IsLegendVisible = true;
            PlotModel.InvalidatePlot(true);
            PlotModel.AdjustForZoom();
        }

        private readonly LinearAxis _xAxis;
    }
}
