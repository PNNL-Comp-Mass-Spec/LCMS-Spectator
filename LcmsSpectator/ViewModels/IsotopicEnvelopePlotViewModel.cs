// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IsotopicEnvelopePlotViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   A view model that maintains a spectrum plot that shows the real vs theoretical isotopic envelope.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels
{
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
    
    /// <summary>
    /// A view model that maintains a spectrum plot that shows the real vs theoretical isotopic envelope.
    /// </summary>
    public class IsotopicEnvelopePlotViewModel : ReactiveObject
    {
        /// <summary>
        /// The XAxis of spectrum PlotModel plot.
        /// </summary>
        private readonly LinearAxis xAxis;

        /// <summary>
        /// Initializes a new instance of the <see cref="IsotopicEnvelopePlotViewModel"/> class. 
        /// </summary>
        public IsotopicEnvelopePlotViewModel()
        {
            this.xAxis = new LinearAxis { Position = AxisPosition.Bottom, Title = "Mass", StringFormat = "0.###" };
            this.PlotModel = new AutoAdjustedYPlotModel(this.xAxis, 1.05)
            {
                Title = "Isotopic Envelope"
            };
            this.PlotModel.GenerateYAxis("Relative Intensity", "0e0");
        }

        /// <summary>
        /// Gets the plot displaying theoretical isotopic profile and actual isotopic profile.
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

            this.PlotModel.Series.Clear();

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
            this.PlotModel.Series.Add(theoSeries);

            // Calculate m/z for each isotope index
            for (var isotopeIndex = 0; isotopeIndex < theoEnvelope.Envolope.Length; isotopeIndex++)
            {
                var intensity = theoEnvelope.Envolope[isotopeIndex];
                var mz = Ion.GetIsotopeMz(mass, charge, isotopeIndex);
                var m = (mz * charge * Constants.Proton) - (charge * Constants.Proton);
                peakMap.Add(isotopeIndex, new PeakDataPoint(m, intensity, 0.0, 0.0, string.Empty));
            }

            // Create peak data points from isotopes and calculate m/z values
            var isotopePeaks = actual.Select(i => new PeakDataPoint(peakMap[i.Index].X, i.Ratio, 0.0, 0.0, string.Empty) { Index = i.Index });

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
            this.PlotModel.Series.Add(actSeries);

            // Calculate min and max boundaries for plot
            var min = peakMap.Values.Min(p => p.X);
            var max = peakMap.Values.Max(p => p.X);
            min -= (max - min) / 3;
            var absMin = Math.Max(0, min - 10);
            max += (max - min) / 3;
            var absMax = max + 10;
            this.xAxis.Minimum = min;
            this.xAxis.AbsoluteMinimum = absMin;
            this.xAxis.Maximum = max;
            this.xAxis.AbsoluteMaximum = absMax;
            this.xAxis.Zoom(min, max);

            this.PlotModel.IsLegendVisible = true;
            this.PlotModel.InvalidatePlot(true);
            this.PlotModel.AdjustForZoom();
        }
    }
}
