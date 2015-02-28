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
using LcmsSpectator.Models;
using LcmsSpectator.PlotModels;
using LcmsSpectator.Utils;
using ReactiveUI;
using Splat;

namespace LcmsSpectator.ViewModels
{
    public class LabeledIonViewModel : ReactiveObject
    {
        public LabeledIonViewModel(Composition composition, IonType ionType, bool isFragmentIon, ILcMsRun lcms, Ion precursorIon = null, bool isChargeState = false, int index=0)
        {
            Composition = composition;
            PrecursorIon = precursorIon;
            Index = index;
            IonType = ionType;
            IsFragmentIon = isFragmentIon;
            IsChargeState = isChargeState;
            _selected = true;
            _xicCacheLock = new object();
            _peakCacheLock = new object();
            _peakCache = new MemoizingMRUCache<Tuple<Spectrum, bool>, IList<PeakDataPoint>>(GetPeakDataPoints, 5);
            _xicCache = new MemoizingMRUCache<int, IList<XicDataPoint>>(GetXic, 10);
            Lcms = lcms;
        }
        #region Public Properties

        public Composition Composition { get; private set; }
        public Ion PrecursorIon { get; set; }
        public int Index { get; set; }
        public IonType IonType { get; private set; }
        public bool IsFragmentIon { get; private set; }
        public bool IsChargeState { get; private set; }
        public ILcMsRun Lcms { get; private set; }

        private bool _selected;
        public bool Selected
        {
            get { return _selected; }
            set { this.RaiseAndSetIfChanged(ref _selected, value); }
        }

        public Ion Ion
        {
            get { return IonType.GetIon(Composition); }
        }

        public virtual string Label
        {
            get
            {
                var annotation = "";
                if (IsFragmentIon)
                {
                    var baseIonTypeName = IonType.BaseIonType.Symbol;
                    var neutralLoss = IonType.NeutralLoss.Name;
                    annotation = String.Format("{0}{1}{2}({3}+)", baseIonTypeName, Index, neutralLoss, IonType.Charge);
                }
                else
                {
                    if (IsChargeState)
                    {
                        annotation = String.Format("Precursor ({0}+)", IonType.Charge);
                    }
                    else
                    {
                        if (Index < 0) annotation = String.Format("Precursor [M{0}] ({1}+)", Index, IonType.Charge);
                        else if (Index == 0) annotation = String.Format("Precursor ({0}+)", IonType.Charge);
                        else if (Index > 0) annotation = String.Format("Precursor [M+{0}] ({1}+)", Index, IonType.Charge);
                    }
                }
                return annotation;
            }
        }
        #endregion

        #region Public Methods
        public int CompareTo(LabeledIon other)
        {
            return String.Compare(Label, other.Label, StringComparison.Ordinal);
        }

        public bool Equals(LabeledIon other)
        {
            return Label.Equals(other.Label);
        }

        public Task<IList<PeakDataPoint>> GetPeaksAsync(Spectrum spectrum, bool deconvoluted, bool useCache=true)
        {
            return Task.Run(() => GetPeaks(spectrum, deconvoluted, useCache));
        }

        public IList<PeakDataPoint> GetPeaks(Spectrum spectrum, bool deconvoluted, bool useCache=true)
        {
            IList<PeakDataPoint> peaks;
            lock (_peakCacheLock)
            {
                // MemoizingMRUCache isn't threadsafe. Shouldn't matter for my purposes,
                // but I'm putting a lock around it just in case.
                var key = new Tuple<Spectrum, bool>(spectrum, deconvoluted);
                if (!useCache) _peakCache.Invalidate(key);
                peaks = _peakCache.Get(key, useCache);
            }
            return peaks;
        }

        public Task<IList<XicDataPoint>> GetXicAsync(int pointsToSmooth, bool useCache=true)
        {
            return Task.Run(() => GetXic(pointsToSmooth, useCache));
        }

        public IList<XicDataPoint> GetXic(int pointsToSmooth, bool useCache=true)
        {
            IList<XicDataPoint> xic;
            lock (_xicCacheLock)
            {
                // MemoizingMRUCache isn't threadsafe. Shouldn't matter for my purposes,
                // but I'm putting a lock around it just in case.
                if (!useCache) _xicCache.Invalidate(pointsToSmooth);
                xic = _xicCache.Get(pointsToSmooth, useCache);   
            }
            return xic;
        }
        #endregion

        #region Private Methods
        private IList<PeakDataPoint> GetPeakDataPoints(Tuple<Spectrum, bool> spectrum, object o)
        {
            var tolerance = IsFragmentIon
                            ? IcParameters.Instance.ProductIonTolerancePpm
                            : IcParameters.Instance.PrecursorTolerancePpm;
            var noPeaks = new List<PeakDataPoint> {new PeakDataPoint(Double.NaN, Double.NaN, Double.NaN, Double.NaN, Label){IonType = IonType, Index = Index}};
            var peakDataPoints = new List<PeakDataPoint>();
            IonType ionType = null;
            if (IsFragmentIon) ionType = IonType;
            var deconvoluted = spectrum.Item2;
            Ion ion;
            if (deconvoluted)
            {
                if (IonType.Charge > 1) return peakDataPoints; // Deconvoluted spectrum means decharged (only charge 1 ions shown)
                var ionTypeFactory = IcParameters.Instance.DeconvolutedIonTypeFactory;
                var ionTypeName = IonType.Name.Insert(1, @"'");
                ion = ionTypeFactory.GetIonType(ionTypeName).GetIon(Composition);
            }
            else ion = Ion;
            var labeledIonPeaks = IonUtils.GetIonPeaks(ion, spectrum.Item1, tolerance);
            if (labeledIonPeaks.Item1 == null) return noPeaks;
            var peaks = labeledIonPeaks.Item1;
            var correlation = labeledIonPeaks.Item2;
            if (correlation < IcParameters.Instance.IonCorrelationThreshold) return noPeaks;
            var errors = IonUtils.GetIsotopePpmError(peaks, ion, 0.1);
            peakDataPoints = new List<PeakDataPoint> { Capacity = errors.Length };
            for (int i = 0; i < errors.Length; i++)
            {
                if (errors[i] != null)
                {
                    peakDataPoints.Add(new PeakDataPoint(peaks[i].Mz, peaks[i].Intensity, errors[i].Value, correlation, Label)
                    {
                        Index = Index,
                        IonType = ionType,
                    });
                }
            }
            peakDataPoints = peakDataPoints.OrderByDescending(x => x.Y).ToList();
            return peakDataPoints;
        }

        private IList<XicDataPoint> GetXic(int pointsToSmooth, object o)
        {
            if (_xic == null) _xic = GetXic();
            var xic = _xic;
            IonType ionType = null;
            if (IsFragmentIon) ionType = IonType;
            // smooth
            if (pointsToSmooth > 2)
            {
                var smoother = new SavitzkyGolaySmoother(pointsToSmooth, 2);
                xic = IonUtils.SmoothXic(smoother, _xic);   
            }
            return xic.Where((t, i) => i <= 1 || i >= _xic.Count - 1 ||
                            !_xic[i - 1].Intensity.Equals(t.Intensity) ||
                            !_xic[i + 1].Intensity.Equals(t.Intensity))
                       .Select(t => new XicDataPoint(Lcms.GetElutionTime(t.ScanNum),
                               t.ScanNum, t.Intensity, Index, Label){IonType = ionType}).ToList();
        }

        private Xic GetXic()
        {
            Xic xic;
            if (IsFragmentIon) xic = Lcms.GetFullProductExtractedIonChromatogram(Ion.GetMostAbundantIsotopeMz(),
                                                                                  IcParameters.Instance.ProductIonTolerancePpm,
                                                                                  PrecursorIon.GetMostAbundantIsotopeMz());
            else if (IsChargeState) xic = Lcms.GetFullPrecursorIonExtractedIonChromatogram(Ion.GetMostAbundantIsotopeMz(),
                                                                                                  IcParameters.Instance.PrecursorTolerancePpm);
            else xic = Lcms.GetFullPrecursorIonExtractedIonChromatogram(Ion.GetIsotopeMz(Index), IcParameters.Instance.PrecursorTolerancePpm);
            return xic;
        }
        #endregion

        #region Private Members
        private readonly Object _xicCacheLock;
        private readonly Object _peakCacheLock;
        private readonly MemoizingMRUCache<int, IList<XicDataPoint>> _xicCache;
        private readonly MemoizingMRUCache<Tuple<Spectrum, bool>, IList<PeakDataPoint>> _peakCache;
        private IList<XicPoint> _xic;
        #endregion
    }
}
