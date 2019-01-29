// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LabeledIonViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for labeled ion that can be selected/unselected.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using InformedProteomics.Backend.MathAndStats;
using LcmsSpectator.Config;
using LcmsSpectator.PlotModels;
using LcmsSpectator.Utils;
using ReactiveUI;
using Splat;

namespace LcmsSpectator.ViewModels.Data
{
    /// <summary>
    /// View model for labeled ion that can be selected/unselected.
    /// </summary>
    public class LabeledIonViewModel : ReactiveObject
    {
        /// <summary>
        /// Cache that stores the smoothed XICs generated for this ion.
        /// </summary>
        private readonly MemoizingMRUCache<int, IList<XicDataPoint>> xicCache;

        /// <summary>
        /// Lock for the XIC cache.
        /// </summary>
        private readonly object xicCacheLock;

        /// <summary>
        /// Cache that stores peaks generated for this ion for a given spectrum.
        /// </summary>
        /// <remarks>The tuple is a spectrum and a bool for IsDeconvoluted</remarks>
        private readonly MemoizingMRUCache<Tuple<Spectrum, bool>, IList<PeakDataPoint>> peakCache;

        /// <summary>
        /// Lock for the peak cache.
        /// </summary>
        private readonly object peakCacheLock;

        /// <summary>
        /// Stores the raw XIC for this ion for fast access.
        /// </summary>
        private IList<XicPoint> xic;

        /// <summary>
        /// A value indicating whether this LabeledIon has been selected or unselected.
        /// </summary>
        private bool selected;

        /// <summary>
        /// Initializes a new instance of the <see cref="LabeledIonViewModel"/> class.
        /// </summary>
        /// <param name="composition">The empirical formula for this ion.</param>
        /// <param name="ionType">The fragment IonType for this ion.</param>
        /// <param name="isFragmentIon">A value indicating whether this ion is a fragment ion.</param>
        /// <param name="lcms">The LCMSRun for the data set that the ion is part of.</param>
        /// <param name="precursorIon">The precursor ion if this is a fragment ion.</param>
        /// <param name="isChargeState">
        /// A value indicating whether this ion is a neighboring charge state to another
        /// precursor ion.
        /// </param>
        /// <param name="index">The index of this ion.</param>
        public LabeledIonViewModel(Composition composition, IonType ionType, bool isFragmentIon, ILcMsRun lcms, Ion precursorIon = null, bool isChargeState = false, int index = 0)
        {
            Composition = composition;
            PrecursorIon = precursorIon;
            Index = index;
            IonType = ionType;
            IsFragmentIon = isFragmentIon;
            IsChargeState = isChargeState;
            selected = true;
            xicCacheLock = new object();
            peakCacheLock = new object();
            peakCache = new MemoizingMRUCache<Tuple<Spectrum, bool>, IList<PeakDataPoint>>(GetPeakDataPoints, 5);
            xicCache = new MemoizingMRUCache<int, IList<XicDataPoint>>(GetXic, 10);
            Lcms = lcms;
        }

        /// <summary>
        /// Gets the empirical formula for this ion.
        /// </summary>
        public Composition Composition { get; }

        /// <summary>
        /// Gets or sets the precursor ion if this is a fragment ion.
        /// </summary>
        public Ion PrecursorIon { get; set; }

        /// <summary>
        /// Gets or sets the index of this ion.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets the fragment IonType for this ion.
        /// </summary>
        public IonType IonType { get; }

        /// <summary>
        /// Gets a value indicating whether this ion is a fragment ion.
        /// </summary>
        public bool IsFragmentIon { get; }

        /// <summary>
        /// Gets a value indicating whether this ion is a neighboring charge state to another
        /// precursor ion.
        /// </summary>
        public bool IsChargeState { get; }

        /// <summary>
        /// Gets the LCMSRun for the data set that the ion is part of.
        /// </summary>
        public ILcMsRun Lcms { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this LabeledIon has been selected or unselected.
        /// </summary>
        public bool Selected
        {
            get => selected;
            set => this.RaiseAndSetIfChanged(ref selected, value);
        }

        /// <summary>
        /// Gets the ion for this LabeledIon.
        /// </summary>
        public Ion Ion => new Ion(Composition + (IsFragmentIon ? Composition.Zero : Composition.H2O), IonType.Charge);

        /// <summary>
        /// Gets the text label for this LabeledIon.
        /// </summary>
        public virtual string Label
        {
            get
            {
                var annotation = string.Empty;
                if (IsFragmentIon)
                {
                    var baseIonTypeName = IonType.BaseIonType.Symbol;
                    var neutralLoss = IonType.NeutralLoss.Name;
                    annotation = string.Format("{0}{1}{2}({3}+)", baseIonTypeName, Index, neutralLoss, IonType.Charge);
                }
                else
                {
                    if (IsChargeState)
                    {
                        annotation = string.Format("Precursor ({0}+)", IonType.Charge);
                    }
                    else
                    {
                        if (Index < 0)
                        {
                            annotation = string.Format("Precursor [M{0}] ({1}+)", Index, IonType.Charge);
                        }
                        else if (Index == 0)
                        {
                            annotation = string.Format("Precursor ({0}+)", IonType.Charge);
                        }
                        else if (Index > 0)
                        {
                            annotation = string.Format("Precursor [M+{0}] ({1}+)", Index, IonType.Charge);
                        }
                    }
                }

                return annotation;
            }
        }

        /// <summary>
        /// Compare the label of this LabeledIon to another LabeledIon.
        /// </summary>
        /// <param name="other">The LabeledIon to compare to.</param>
        /// <returns>
        /// A value indicating whether this LabeledIon is
        /// equal to, greater than, or less than the other LabeledIon.
        /// </returns>
        public int CompareTo(LabeledIonViewModel other)
        {
            return string.Compare(Label, other.Label, StringComparison.Ordinal);
        }

        /// <summary>
        /// Compare the label of this LabeledIon to another LabeledIon for equality.
        /// </summary>
        /// <param name="other">The LabeledIon to compare to.</param>
        /// <returns>A value indicating whether this LabeledIon is equal to the other LabeledIon.</returns>
        public bool Equals(LabeledIonViewModel other)
        {
            return Label.Equals(other.Label);
        }

        /// <summary>
        /// Get the peaks for the ion asynchronously.
        /// </summary>
        /// <param name="spectrum">The spectrum to get the peaks from</param>
        /// <param name="deconvoluted">A value indicating whether the peaks come from a deconvoluted spectrum.</param>
        /// <param name="useCache">A value indicating whether the cache should be used if possible.</param>
        /// <returns>A task that creates and returns the peaks for the ion.</returns>
        public Task<IList<PeakDataPoint>> GetPeaksAsync(Spectrum spectrum, bool deconvoluted, bool useCache = true)
        {
            return Task.Run(() => GetPeaks(spectrum, deconvoluted, useCache));
        }

        /// <summary>
        /// Find data points in the spectrum that match the ion tracked by this LabeledIonViewModel instance
        /// </summary>
        /// <param name="spectrum">The spectrum to get the peaks from</param>
        /// <param name="deconvoluted">A value indicating whether the peaks come from a deconvoluted spectrum.</param>
        /// <param name="useCache">A value indicating whether the cache should be used if possible.</param>
        /// <returns>The peaks for the ion.</returns>
        public IList<PeakDataPoint> GetPeaks(Spectrum spectrum, bool deconvoluted, bool useCache = true)
        {
            IList<PeakDataPoint> peaks;
            lock (peakCacheLock)
            {
                // MemoizingMRUCache isn't threadsafe. Shouldn't matter for my purposes,
                // but I'm putting a lock around it just in case.
                var key = new Tuple<Spectrum, bool>(spectrum, deconvoluted);
                if (!useCache)
                {
                    peakCache.Invalidate(key);
                }

                // This will call GetPeakDataPoints in this class
                peaks = peakCache.Get(key, useCache);
            }

            return peaks;
        }

        /// <summary>
        /// Get XICs for the ion asynchronously.
        /// </summary>
        /// <param name="pointsToSmooth">Smoothing window width.</param>
        /// <param name="useCache">A value indicating whether the cache should be used if possible</param>
        /// <returns>Task that creates and returns the XICs for this ion</returns>
        public Task<IList<XicDataPoint>> GetXicAsync(int pointsToSmooth, bool useCache = true)
        {
            return Task.Run(() => GetXic(pointsToSmooth, useCache));
        }

        /// <summary>
        /// Get XICs for the ion.
        /// </summary>
        /// <param name="pointsToSmooth">Smoothing window width.</param>
        /// <param name="useCache">A value indicating whether the cache should be used if possible</param>
        /// <returns>The XICs for this ion</returns>
        public IList<XicDataPoint> GetXic(int pointsToSmooth, bool useCache = true)
        {
            IList<XicDataPoint> x;
            lock (xicCacheLock)
            {
                // MemoizingMRUCache isn't thread safe. Shouldn't matter for our purposes,
                // but put a lock around it just in case.
                if (!useCache)
                {
                    xicCache.Invalidate(pointsToSmooth);
                }

                x = xicCache.Get(pointsToSmooth, useCache);
            }

            return x;
        }

        /// <summary>
        /// Find data points in the spectrum that match the ion tracked by this LabeledIonViewModel instance
        /// </summary>
        /// <param name="spectrumInfo">Tuple tracking the spectrum and whether or not it is deconvoluted</param>
        /// <param name="o">Object required for cache</param>
        /// <returns>The peaks for the ion.</returns>
        private IList<PeakDataPoint> GetPeakDataPoints(Tuple<Spectrum, bool> spectrumInfo, object o)
        {
            var tolerance = IsFragmentIon
                            ? IcParameters.Instance.ProductIonTolerancePpm
                            : IcParameters.Instance.PrecursorTolerancePpm;
            var noPeaks = new List<PeakDataPoint>
            {
                new PeakDataPoint(double.NaN, double.NaN, double.NaN, double.NaN, Label)
                {
                    TheoMonoisotopicMass = Ion.Composition.Mass,
                    IonType = IonType,
                    Index = Index
                }
            };

            var spectrum = spectrumInfo.Item1;
            var deconvoluted = spectrumInfo.Item2;

            if (spectrum == null)
            {
                // The spectrum object is null
                return noPeaks;
            }

            // If this is a fragmentation spectrum, use IonType, otherwise set ionType to null
            var ionType = IsFragmentIon ? IonType : null;

            Ion ion;
            if (deconvoluted)
            {
                if (IonType.Charge > 1)
                {
                    // Deconvoluted spectrum means de-charged (only charge 1 ions shown)
                    return new List<PeakDataPoint>();
                }

                if (!IsFragmentIon)
                {
                    ion = new Ion(Composition, 1);
                }
                else
                {
                    var ionTypeFactory = IcParameters.Instance.DeconvolutedIonTypeFactory;
                    var ionTypeName = IonType.Name.Insert(1, @"'");
                    ion = ionTypeFactory.GetIonType(ionTypeName).GetIon(Composition);
                }
            }
            else
            {
                ion = Ion;
            }

            // Retrieve a Tuple containing an array of isotope peaks and their Pearson correlation with the theoretical isotope envelope for this ion.
            var labeledIonPeaks = IonUtils.GetIonPeaks(ion, spectrum, tolerance, deconvoluted);
            if (labeledIonPeaks.Item1 == null)
            {
                // None of data points in the spectrum matched this ion
                return noPeaks;
            }

            if (IsFragmentIon)
            {
                // Assure that the charge state isn't too high for this ion
                // Impose a minimum threshold of 4+
                var maxReasonableCharge = Math.Round(Math.Max(4, Ion.Composition.Mass / 1000.0 * 3));
                if (Ion.Charge > maxReasonableCharge)
                {
                    // Charge state is unreasonably high given the monoisotopic mass
                    return noPeaks;
                }
            }

            var peaks = labeledIonPeaks.Item1;
            var correlation = labeledIonPeaks.Item2;
            if (correlation < IcParameters.Instance.IonCorrelationThreshold)
            {
                return noPeaks;
            }

            var errors = IonUtils.GetIsotopePpmError(peaks, ion, 0.1, deconvoluted);
            var peakDataPoints = new List<PeakDataPoint> { Capacity = errors.Length };
            for (var i = 0; i < errors.Length; i++)
            {
                if (errors[i] != null)
                {
                    // Calculate the monoisotopic m/z so that we can calculate the proper monoisotopic mass for isotope peaks
                    var monoMz = peaks[i].Mz - (InformedProteomics.Backend.Data.Biology.Constants.Proton / Ion.Charge * i);
                    peakDataPoints.Add(new PeakDataPoint(peaks[i].Mz, peaks[i].Intensity, errors[i].Value, correlation, Label)
                    {
                        MonoisotopicMass = (monoMz - InformedProteomics.Backend.Data.Biology.Constants.Proton) * Ion.Charge,
                        TheoMonoisotopicMass = Ion.Composition.Mass,
                        IsotopeIndex = i,
                        Index = Index,
                        IonType = ionType,
                    });
                }
            }

            var sortedPeakDataPoints = peakDataPoints.OrderByDescending(x => x.Y).ToList();
            return sortedPeakDataPoints;
        }

        /// <summary>
        /// Get XICs for the ion.
        /// </summary>
        /// <param name="pointsToSmooth">Smoothing window width. </param>
        /// <param name="o">Object required for the cache.</param>
        /// <returns>The XICs for this ion</returns>
        private IList<XicDataPoint> GetXic(int pointsToSmooth, object o)
        {
            if (xic == null)
            {
                xic = GetXic();
            }

            var x = xic;
            IonType ionType = null;
            if (IsFragmentIon)
            {
                ionType = IonType;
            }

            // smooth
            if (pointsToSmooth > 2)
            {
                var smoother = new SavitzkyGolaySmoother(pointsToSmooth, 2);
                x = IonUtils.SmoothXic(smoother, x);
            }

            return x.Where((t, i) => i <= 1 || i >= x.Count - 1 ||
                            !xic[i - 1].Intensity.Equals(t.Intensity) ||
                            !xic[i + 1].Intensity.Equals(t.Intensity))
                       .Select(
                           t => new XicDataPoint(
                                        Lcms.GetElutionTime(t.ScanNum),
                                        t.ScanNum,
                                        t.Intensity,
                                        Index,
                                        Label)
                                    {
                                        IonType = ionType
                                    }).ToList();
        }

        /// <summary>
        /// Get a raw XIC from the LCMSRun.
        /// </summary>
        /// <returns>The raw XIC.</returns>
        private Xic GetXic()
        {
            Xic x;
            if (IsFragmentIon)
            {
                x = Lcms.GetFullProductExtractedIonChromatogram(
                    Ion.GetMostAbundantIsotopeMz(),
                    IcParameters.Instance.ProductIonTolerancePpm,
                    PrecursorIon.GetMostAbundantIsotopeMz());
            }
            else if (IsChargeState)
            {
                x = Lcms.GetFullPrecursorIonExtractedIonChromatogram(
                   Ion.GetMostAbundantIsotopeMz(),
                   IcParameters.Instance.PrecursorTolerancePpm);
            }
            else
            {
                x = Lcms.GetFullPrecursorIonExtractedIonChromatogram(
                    Ion.GetIsotopeMz(Index),
                    IcParameters.Instance.PrecursorTolerancePpm);
            }

            return x;
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Label) ? "LcmsSpectator.ViewModels.Data.LabeledIonViewModel" : Label;
        }
    }
}
