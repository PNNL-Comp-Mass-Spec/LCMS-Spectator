// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IsotopicEnvelopePlotViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   A view model that maintains a spectrum plot that shows the real vs theoretical isotopic envelope.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels.Plots
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using InformedProteomics.Backend.Data.Biology;
    using InformedProteomics.Backend.Data.Composition;
    using InformedProteomics.Backend.Data.Spectrometry;

    using PlotModels;

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
        private readonly LinearAxis xaxis;

        /// <summary>
        /// Initializes a new instance of the <see cref="IsotopicEnvelopePlotViewModel"/> class.
        /// </summary>
        public IsotopicEnvelopePlotViewModel()
        {
            xaxis = new LinearAxis { Position = AxisPosition.Bottom, Title = "Mass", StringFormat = "0.###" };
            PlotModel = new AutoAdjustedYPlotModel(xaxis, 1.05)
            {
                Title = "Isotopic Envelope",
                YAxis =
                {
                    Title = "Relative Intensity",
                    StringFormat = "0e0"
                }
            };
        }

        /// <summary>
        /// Gets the plot displaying theoretical isotopic profile and actual isotopic profile.
        /// </summary>
        public AutoAdjustedYPlotModel PlotModel { get; }

        /// <summary>
        /// Build the isotope plot showing theoretical isotopic profile and
        /// actual isotopic profile.
        /// This will calculate the theoretical using averagine from the provided monoisotopic mass.
        /// </summary>
        /// <param name="actual">Actual isotopic profile.</param>
        /// <param name="mass">Monoisotopic mass, for calculating theoretical isotopic profile.</param>
        /// <param name="charge">Charge, for calculating actual isotopic profile.</param>
        public void BuildPlot(Isotope[] actual, double mass, int charge)
        {
            // Calculate theoretical isotopic profile using averagine
            var theoEnvelope = Averagine.GetIsotopomerEnvelope(mass);
            var theoretical = new PeakDataPoint[theoEnvelope.Envelope.Length];

            // Calculate m/z for each isotope index (observed)
            for (var isotopeIndex = 0; isotopeIndex < theoEnvelope.Envelope.Length; isotopeIndex++)
            {
                var intensity = theoEnvelope.Envelope[isotopeIndex];
                var mz = Ion.GetIsotopeMz(mass, charge, isotopeIndex);
                var m = (mz * charge * Constants.Proton) - (charge * Constants.Proton);
                theoretical[isotopeIndex] = new PeakDataPoint(m, intensity, 0.0, 0.0, string.Empty);
            }

            // Create peak data points from isotopes and calculate m/z values (actual)
            var observed = actual.Select(i => new PeakDataPoint(theoretical[i.Index].X, i.Ratio, 0.0, 0.0, string.Empty) { Index = i.Index }).ToArray();

            BuildPlot(theoretical, observed, false);
        }

        /// <summary>
        /// Build the isotope plot showing theoretical isotopic profile and
        /// actual isotopic profile.
        /// </summary>
        /// <param name="theoretical">Actual isotopic profile.</param>
        /// <param name="observed">Actual isotopic profile.</param>
        /// <param name="isProfile">A value indicating whether the peak list is profile mode.</param>
        public void BuildPlot(IList<Peak> theoretical, IList<Peak> observed, bool isProfile)
        {
            BuildPlot(GetPeakDataPoints(theoretical), GetPeakDataPoints(observed), isProfile);
        }

        /// <summary>
        /// Build the isotope plot showing theoretical isotopic profile and
        /// actual isotopic profile.
        /// </summary>
        /// <param name="theoretical">Actual isotopic profile.</param>
        /// <param name="observed">Actual isotopic profile.</param>
        /// <param name="isProfile">A value indicating whether the peak list is profile mode.</param>
        public void BuildPlot(PeakDataPoint[] theoretical, PeakDataPoint[] observed, bool isProfile)
        {
            PlotModel.Series.Clear();

            // Create series for theoretical isotope profile
            var theoSeries = new PeakPointSeries
            {
                Title = "Theoretical",
                ItemsSource = theoretical,
                Color = OxyColor.FromArgb(120, 0, 0, 0),
                StrokeThickness = 3.0,
                TrackerFormatString =
                "{0}" + Environment.NewLine +
                "{1}: {2:0.###}" + Environment.NewLine +
                "{3}: {4:0.###}" + Environment.NewLine +
                "Index: {Index:0.###}"
            };
            PlotModel.Series.Add(theoSeries);

            if (isProfile)
            {
                // Create series for actual isotope profile
                var actSeries = new ProfilePeakPointSeries
                {
                    Title = "Observed",
                    ItemsSource = observed,
                    Color = OxyColor.FromArgb(120, 255, 0, 0),
                    StrokeThickness = 1.0,
                    TrackerFormatString =
                    "{0}" + Environment.NewLine +
                    "{1}: {2:0.###}" + Environment.NewLine +
                    "{3}: {4:0.###}" + Environment.NewLine +
                    "Index: {Index:0.###}"
                };
                PlotModel.Series.Add(actSeries);
            }
            else
            {
                // Create series for actual isotope profile
                var actSeries = new PeakPointSeries
                {
                    Title = "Observed",
                    ItemsSource = observed,
                    Color = OxyColor.FromArgb(120, 255, 0, 0),
                    StrokeThickness = 3.0,
                    TrackerFormatString =
                    "{0}" + Environment.NewLine +
                    "{1}: {2:0.###}" + Environment.NewLine +
                    "{3}: {4:0.###}" + Environment.NewLine +
                    "Index: {Index:0.###}"
                };
                PlotModel.Series.Add(actSeries);
            }

            // Calculate min and max boundaries for plot
            var min = theoretical.Min(p => p.X);
            var max = theoretical.Max(p => p.X);
            min -= (max - min) / 3;
            var absMin = Math.Max(0, min - 10);
            max += (max - min) / 3;
            var absMax = max + 10;
            xaxis.Minimum = min;
            xaxis.AbsoluteMinimum = absMin;
            xaxis.Maximum = max;
            xaxis.AbsoluteMaximum = absMax;
            xaxis.Zoom(min, max);

            PlotModel.IsLegendVisible = true;
            PlotModel.InvalidatePlot(true);
            PlotModel.AdjustForZoom();
        }

        /// <summary>
        /// Get the peak data points from a list of peaks.
        /// </summary>
        /// <param name="peaks">The peaks to convert.</param>
        /// <returns>Array of peaks converted to PeakDataPoints.</returns>
        private PeakDataPoint[] GetPeakDataPoints(IList<Peak> peaks)
        {
            var max = peaks.Max(p => p.Intensity);
            var peakDataPoints = new PeakDataPoint[peaks.Count];
            for (var i = 0; i < peaks.Count; i++)
            {
                var peak = peaks[i];
                peakDataPoints[i] = new PeakDataPoint(peak.Mz, peak.Intensity / max, 0.0, 0.0, string.Empty) { Index = i };
            }

            return peakDataPoints;
        }
    }
}
