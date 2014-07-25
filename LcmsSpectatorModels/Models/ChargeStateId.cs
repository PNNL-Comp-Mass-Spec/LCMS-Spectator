using System;
using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectatorModels.Config;

namespace LcmsSpectatorModels.Models
{
    public class ChargeStateId: IIdData
    {
        public int Charge { get; private set; }
        public string ProteinNameDesc { get; private set; }
        public Sequence Sequence { get; private set; }
        public string SequenceText { get; private set; }
        public double PrecursorMz { get; private set; }

        public List<PrSm> PrSms { get; set; }
        public List<LabeledXic> SelectedFragmentXics { get; set; }
        public List<LabeledXic> SelectedPrecursorXics { get; set; } 

        public ChargeStateId(int charge, Sequence sequence, string sequenceText, string proteinNameDesc, double precMz)
        {
            _xicCache = new Dictionary<Tuple<BaseIonType, NeutralLoss, int>, List<LabeledXic>>();
            ProteinNameDesc = proteinNameDesc;
            PrecursorMz = precMz;
            Charge = charge;
            Sequence = sequence;
            SequenceText = sequenceText;
            PrSms = new List<PrSm>();
        }

        public void Add(PrSm data)
        {
            var pos = PrSms.BinarySearch(data);
            if (pos < 0)
            {
                PrSms.Add(data);
                PrSms.Sort();
            }
            else
            {
                throw new InvalidOperationException("Cannot insert duplicate identifications.");
            }
        }

        public PrSm GetHighestScoringPrSm()
        {
            PrSm highest = null;
            foreach (var prsm in PrSms)
            {
                if (highest == null || prsm.MatchedFragments >= highest.MatchedFragments)
                {
                    highest = prsm;
                }
            }
            return highest;
        }

        public bool Contains(PrSm data)
        {
            return PrSms.Any(prsm => (prsm.CompareTo(data) == 0));
        }

        public List<LabeledXic> GetFragmentXics(BaseIonType baseIon, NeutralLoss neutralLoss, int charge)
        {
            var xicKey = new Tuple<BaseIonType, NeutralLoss, int>(baseIon, neutralLoss, charge);
            if (_xicCache.ContainsKey(xicKey)) return _xicCache[xicKey];
            var composition = Sequence.Aggregate(InformedProteomics.Backend.Data.Composition.Composition.Zero, (current, aa) => current + aa.Composition);
            var precursorIon = new Ion(composition + InformedProteomics.Backend.Data.Composition.Composition.H2O, Charge);
            var precursorMz = precursorIon.GetMostAbundantIsotopeMz();

            var ionType = IcParameters.Instance.GetIonType(baseIon, neutralLoss, charge);
            var fragments = Utils.Utils.GetCompositions(Sequence, ionType.BaseIonType.IsPrefix);
            var xics = new List<LabeledXic>();
            for (int i = 0; i < fragments.Count; i++)
            {
                var ion = ionType.GetIon(fragments[i]);
                var maxPeakMz = ion.GetMostAbundantIsotopeMz();
                var fragmentXic = IcParameters.Instance.Lcms.GetFullFragmentExtractedIonChromatogram(maxPeakMz, IcParameters.Instance.ProductIonTolerancePpm, precursorMz);
                var index = i + 1;
                xics.Add(new LabeledXic(MedianScan, index, fragmentXic, ionType));
            }
            _xicCache[xicKey] = xics;
            return xics;
        }

        public List<LabeledXic> GetFragmentXics(IList<BaseIonType> baseIons, IList<NeutralLoss> neutralLosses, int minCharge, int maxCharge)
        {
            var fragmentIons = new List<LabeledXic>();
            foreach (var neutralLoss in neutralLosses)
            {
                foreach (var baseIonType in baseIons)
                {
                    for (var charge = minCharge;
                        charge <= maxCharge;
                        charge++)
                    {
                        fragmentIons.AddRange(GetFragmentXics(baseIonType, neutralLoss, charge));
                    }
                }
            }
            return fragmentIons;
        }

        public List<LabeledXic> PrecursorIonXics()
        {
            var precursorIons = new List<LabeledXic>();
            for (int i = -1; i <= 2; i++)
            {
                precursorIons.Add(PrecursorIonXics(i));
            }
            return precursorIons;
        }

        public LabeledXic PrecursorIonXics(int isotopeIndex)
        {
            var precursorIonType = new IonType("Precursor", InformedProteomics.Backend.Data.Composition.Composition.H2O, Charge, false);
            var composition = Sequence.Aggregate(InformedProteomics.Backend.Data.Composition.Composition.Zero, (current, aa) => current + aa.Composition);
            var ion = new Ion(composition + InformedProteomics.Backend.Data.Composition.Composition.H2O, Charge);
            var precursorMz = ion.GetIsotopeMz(isotopeIndex);
            var precursorXic = IcParameters.Instance.Lcms.GetFullExtractedIonChromatogram(precursorMz, IcParameters.Instance.PrecursorTolerancePpm);
            return new LabeledXic(MedianScan, isotopeIndex, precursorXic, precursorIonType, false);
        }

        public IEnumerable<int> Ms2ScanNumbers
        {
            get
            {
                var composition = Sequence.Aggregate(InformedProteomics.Backend.Data.Composition.Composition.Zero, (current, aa) => current + aa.Composition);
                var precursorIon = new Ion(composition + InformedProteomics.Backend.Data.Composition.Composition.H2O, Charge);
                var precursorMz = precursorIon.GetMostAbundantIsotopeMz();
                return IcParameters.Instance.Lcms.GetMs2ScansForPrecursorMz(precursorMz);
            }
        }

        public IEnumerable<int> Ms1ScanNumbers
        {
            get { return IcParameters.Instance.Lcms.GetScanNumbers(1); }
        }

        public int MedianScan
        {
            get
            {
                var medianIndex = PrSms.Count/2;
                var medianScan = PrSms[medianIndex].Scan;
                return medianScan;
            }
        }

        public int AbsoluteMaxScan
        {
            get { return IcParameters.Instance.Lcms.MaxLcScan; }
        }

        public void ClearCache()
        {
            if (SelectedFragmentXics != null) SelectedFragmentXics.Clear();
            if (SelectedPrecursorXics != null) SelectedPrecursorXics.Clear();
            if (_xicCache != null) _xicCache.Clear();
        }

        private readonly Dictionary<Tuple<BaseIonType, NeutralLoss, int>, List<LabeledXic>> _xicCache;
    }

    class ChargeStateComparer : IComparer<ChargeStateId>
    {
        public int Compare(ChargeStateId x, ChargeStateId y)
        {
            return x.Charge.CompareTo(y.Charge);
        }
    }
}
