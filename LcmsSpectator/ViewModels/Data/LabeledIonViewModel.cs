// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LabeledIonViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for labeled ion that can be selected/unselected.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using InformedProteomics.Backend.Data.Biology;
    using InformedProteomics.Backend.Data.Composition;
    using InformedProteomics.Backend.Data.Spectrometry;
    using InformedProteomics.Backend.MassSpecData;
    using InformedProteomics.Backend.Utils;

    using LcmsSpectator.Config;
    using LcmsSpectator.PlotModels;
    using LcmsSpectator.Utils;

    using ReactiveUI;

    using Splat;

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
            this.Composition = composition;
            this.PrecursorIon = precursorIon;
            this.Index = index;
            this.IonType = ionType;
            this.IsFragmentIon = isFragmentIon;
            this.IsChargeState = isChargeState;
            this.selected = true;
            this.xicCacheLock = new object();
            this.peakCacheLock = new object();
            this.peakCache = new MemoizingMRUCache<Tuple<Spectrum, bool>, IList<PeakDataPoint>>(this.GetPeakDataPoints, 5);
            this.xicCache = new MemoizingMRUCache<int, IList<XicDataPoint>>(this.GetXic, 10);
            this.Lcms = lcms;
        }

        /// <summary>
        /// Gets the empirical formula for this ion.
        /// </summary>
        public Composition Composition { get; private set; }

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
        public IonType IonType { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this ion is a fragment ion.
        /// </summary>
        public bool IsFragmentIon { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this ion is a neighboring charge state to another
        /// precursor ion.
        /// </summary>
        public bool IsChargeState { get; private set; }

        /// <summary>
        /// Gets the LCMSRun for the data set that the ion is part of.
        /// </summary>
        public ILcMsRun Lcms { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this LabeledIon has been selected or unselected.
        /// </summary>
        public bool Selected
        {
            get { return this.selected; }
            set { this.RaiseAndSetIfChanged(ref this.selected, value); }
        }

        /// <summary>
        /// Gets the ion for this LabeledIon.
        /// </summary>
        public Ion Ion
        {
            get { return this.IonType.GetIon(this.Composition); }
        }

        /// <summary>
        /// Gets the text label for this LabeledIon.
        /// </summary>
        public virtual string Label
        {
            get
            {
                var annotation = string.Empty;
                if (this.IsFragmentIon)
                {
                    var baseIonTypeName = this.IonType.BaseIonType.Symbol;
                    var neutralLoss = this.IonType.NeutralLoss.Name;
                    annotation = string.Format("{0}{1}{2}({3}+)", baseIonTypeName, this.Index, neutralLoss, this.IonType.Charge);
                }
                else
                {
                    if (this.IsChargeState)
                    {
                        annotation = string.Format("Precursor ({0}+)", this.IonType.Charge);
                    }
                    else
                    {
                        if (this.Index < 0)
                        {
                            annotation = string.Format("Precursor [M{0}] ({1}+)", this.Index, this.IonType.Charge);
                        }
                        else if (this.Index == 0)
                        {
                            annotation = string.Format("Precursor ({0}+)", this.IonType.Charge);
                        }
                        else if (this.Index > 0)
                        {
                            annotation = string.Format("Precursor [M+{0}] ({1}+)", this.Index, this.IonType.Charge);
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
            return string.Compare(this.Label, other.Label, StringComparison.Ordinal);
        }

        /// <summary>
        /// Compare the label of this LabeledIon to another LabeledIon for equality.
        /// </summary>
        /// <param name="other">The LabeledIon to compare to.</param>
        /// <returns>A value indicating whether this LabeledIon is equal to the other LabeledIon.</returns>
        public bool Equals(LabeledIonViewModel other)
        {
            return this.Label.Equals(other.Label);
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
            return Task.Run(() => this.GetPeaks(spectrum, deconvoluted, useCache));
        }

        /// <summary>
        /// Get the peaks for the ion.
        /// </summary>
        /// <param name="spectrum">The spectrum to get the peaks from</param>
        /// <param name="deconvoluted">A value indicating whether the peaks come from a deconvoluted spectrum.</param>
        /// <param name="useCache">A value indicating whether the cache should be used if possible.</param>
        /// <returns>The peaks for the ion.</returns>
        public IList<PeakDataPoint> GetPeaks(Spectrum spectrum, bool deconvoluted, bool useCache = true)
        {
            IList<PeakDataPoint> peaks;
            lock (this.peakCacheLock)
            {
                // MemoizingMRUCache isn't threadsafe. Shouldn't matter for my purposes,
                // but I'm putting a lock around it just in case.
                var key = new Tuple<Spectrum, bool>(spectrum, deconvoluted);
                if (!useCache)
                {
                    this.peakCache.Invalidate(key);
                }

                peaks = this.peakCache.Get(key, useCache);
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
            return Task.Run(() => this.GetXic(pointsToSmooth, useCache));
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
            lock (this.xicCacheLock)
            {
                // MemoizingMRUCache isn't threadsafe. Shouldn't matter for my purposes,
                // but I'm putting a lock around it just in case.
                if (!useCache)
                {
                    this.xicCache.Invalidate(pointsToSmooth);
                }

                x = this.xicCache.Get(pointsToSmooth, useCache);
            }

            return x;
        }

        /// <summary>
        /// Get the peaks for the ion.
        /// </summary>
        /// <param name="spectrum">The spectrum to get the peaks from</param>
        /// <param name="o">Object required for cache</param>
        /// <returns>The peaks for the ion.</returns>
        private IList<PeakDataPoint> GetPeakDataPoints(Tuple<Spectrum, bool> spectrum, object o)
        {
            var tolerance = this.IsFragmentIon
                            ? IcParameters.Instance.ProductIonTolerancePpm
                            : IcParameters.Instance.PrecursorTolerancePpm;
            var noPeaks = new List<PeakDataPoint> { new PeakDataPoint(double.NaN, double.NaN, double.NaN, double.NaN, this.Label) { IonType = this.IonType, Index = this.Index } };
            var peakDataPoints = new List<PeakDataPoint>();
            IonType ionType = null;
            if (this.IsFragmentIon)
            {
                ionType = this.IonType;
            }

            var deconvoluted = spectrum.Item2;
            Ion ion;
            if (deconvoluted)
            {
                if (this.IonType.Charge > 1)
                {
                    return peakDataPoints; // Deconvoluted spectrum means decharged (only charge 1 ions shown)
                }

                if (!this.IsFragmentIon)
                {
                    ion = new Ion(this.Composition, 1);
                }
                else
                {
                    var ionTypeFactory = IcParameters.Instance.DeconvolutedIonTypeFactory;
                    var ionTypeName = this.IonType.Name.Insert(1, @"'");
                    ion = ionTypeFactory.GetIonType(ionTypeName).GetIon(this.Composition);
                }
            }
            else
            {
                ion = this.Ion;
            }

            var labeledIonPeaks = IonUtils.GetIonPeaks(ion, spectrum.Item1, tolerance, deconvoluted);
            if (labeledIonPeaks.Item1 == null)
            {
                return noPeaks;
            }

            var peaks = labeledIonPeaks.Item1;
            var correlation = labeledIonPeaks.Item2;
            if (correlation < IcParameters.Instance.IonCorrelationThreshold)
            {
                return noPeaks;
            }

            var errors = IonUtils.GetIsotopePpmError(peaks, ion, 0.1, deconvoluted);
            peakDataPoints = new List<PeakDataPoint> { Capacity = errors.Length };
            for (int i = 0; i < errors.Length; i++)
            {
                if (errors[i] != null)
                {
                    peakDataPoints.Add(new PeakDataPoint(peaks[i].Mz, peaks[i].Intensity, errors[i].Value, correlation, this.Label)
                    {
                        Index = this.Index,
                        IonType = ionType,
                    });
                }
            }

            peakDataPoints = peakDataPoints.OrderByDescending(x => x.Y).ToList();
            return peakDataPoints;
        }

        /// <summary>
        /// Get XICs for the ion.
        /// </summary>
        /// <param name="pointsToSmooth">Smoothing window width. </param>
        /// <param name="o">Object required for the cache.</param>
        /// <returns>The XICs for this ion</returns>
        private IList<XicDataPoint> GetXic(int pointsToSmooth, object o)
        {
            if (this.xic == null)
            {
                this.xic = this.GetXic();
            }

            var x = this.xic;
            IonType ionType = null;
            if (this.IsFragmentIon)
            {
                ionType = this.IonType;
            }

            // smooth
            if (pointsToSmooth > 2)
            {
                var smoother = new SavitzkyGolaySmoother(pointsToSmooth, 2);
                x = IonUtils.SmoothXic(smoother, x);   
            }

            return x.Where((t, i) => i <= 1 || i >= x.Count - 1 ||
                            !this.xic[i - 1].Intensity.Equals(t.Intensity) ||
                            !this.xic[i + 1].Intensity.Equals(t.Intensity))
                       .Select(
                           t => new XicDataPoint(
                                        this.Lcms.GetElutionTime(t.ScanNum),
                                        t.ScanNum, 
                                        t.Intensity, 
                                        this.Index, 
                                        this.Label)
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
            if (this.IsFragmentIon)
            {
                x = this.Lcms.GetFullProductExtractedIonChromatogram(
                    this.Ion.GetMostAbundantIsotopeMz(),
                    IcParameters.Instance.ProductIonTolerancePpm,
                    this.PrecursorIon.GetMostAbundantIsotopeMz());   
            }
            else if (this.IsChargeState)
            {
                x = this.Lcms.GetFullPrecursorIonExtractedIonChromatogram(
                   this.Ion.GetMostAbundantIsotopeMz(),
                   IcParameters.Instance.PrecursorTolerancePpm);   
            }
            else
            {
                x = this.Lcms.GetFullPrecursorIonExtractedIonChromatogram(
                    this.Ion.GetIsotopeMz(this.Index),
                    IcParameters.Instance.PrecursorTolerancePpm);
            }

            return x;
        }
    }
}
