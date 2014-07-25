using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectatorModels.Config;

namespace LcmsSpectatorModels.Models
{
    public class PrSm: IComparable<PrSm>
    {
        public LcMsRun Lcms { get; set; }
        public int Scan { get; set; }
        public Spectrum Ms2Spectrum { get { return (Lcms == null) ? null : Lcms.GetSpectrum(Scan); } }
        public string Protein { get; set; }
        public string Annotation { get; set; }
        public string SequenceText { get; set; }
        public List<string> SequenceLabel { get; set; }
        public Sequence Sequence { get; set; }
        public string Pre { get; set; }
        public string Post { get; set; }
        public List<Tuple<int, Modification>> Modifications { get; set; }
        public string Composition { get; set; }
        public string ProteinName { get; set; }
        public string ProteinDesc { get; set; }
        public string ProteinNameDesc { get; set; }
        public int ProteinLength { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
        public int Charge { get; set; }
        public double MostAbundantIsotopeMz { get; set; }
        public double Mass { get; set; }
        public double MatchedFragments { get; set; }
        //public double IsotopeCorrPrevMs1 { get; set; }
        //public double IsotopeCorrNextMs1 { get; set; }
        //public double CorrMostAbundantPlusOneIsoptope { get; private set; }
        //public double ChargeCorrMinusOne { get; set; }
        //public double ChargeCorrPlusOne { get; set; }
        public double QValue { get; set; }
        public double PepQValue { get; set; }
        public static double CorrelationThreshold = 0.7;

        public PrSm(string line, IDictionary<string, int> headers, LcMsRun lcms)
        {
            Lcms = lcms;
            var parts = line.Split('\t');
            Scan = Convert.ToInt32(parts[headers["Scan"]]);
            Pre = parts[headers["Pre"]];
            Protein = parts[headers["Sequence"]];
            Post = parts[headers["Post"]];
            Annotation = (Pre + "." + Protein + "." + Post).Replace('-','_');
            SequenceLabel = new List<string>();
            SetModifications(parts[headers["Modifications"]]);
            Composition = parts[headers["Composition"]];
            ProteinName = parts[headers["ProteinName"]];
            ProteinDesc = parts[headers["ProteinDesc"]].Split(';').FirstOrDefault();
            ProteinNameDesc = ProteinName + "; " + ProteinDesc;
            ProteinLength = Convert.ToInt32(parts[headers["ProteinLength"]]);
            Start = Convert.ToInt32(parts[headers["Start"]]);
            End = Convert.ToInt32(parts[headers["End"]]);
            Charge = Convert.ToInt32(parts[headers["Charge"]]);
            MostAbundantIsotopeMz = Convert.ToDouble(parts[headers["MostAbundantIsotopeMz"]]);
            Mass = Convert.ToDouble(parts[headers["Mass"]]);
            var scoreLabel = "IcScore";
            if (!headers.ContainsKey(scoreLabel)) scoreLabel = "#MatchedFragments";
            MatchedFragments = Convert.ToDouble(parts[headers[scoreLabel]]);
            MatchedFragments = Math.Round(MatchedFragments, 3);
//            IsotopeCorrPrevMs1 = Convert.ToDouble(parts[headers["IsotopeCorrPrevMs1"]]);
//            IsotopeCorrNextMs1 = Convert.ToDouble(parts[headers["IsotopeCorrNextMs1"]]);
//            CorrMostAbundantPlusOneIsoptope = Convert.ToDouble(parts[headers["CorrMostAbundantPlusOneIsotope"]]);
//            ChargeCorrMinusOne = Convert.ToDouble(parts[headers["ChargeCorrMinusOne"]]);
//            ChargeCorrPlusOne = Convert.ToDouble(parts[headers["ChargeCorrPlusOne"]]);
            QValue = Math.Round(Convert.ToDouble(parts[headers["QValue"]]), 4);
            PepQValue = Convert.ToDouble(parts[headers["PepQValue"]]);
        }

        public PrSm() {}

        public List<LabeledIon> GetFragmentIons(BaseIonType baseIon, NeutralLoss neutralLoss, int charge)
        {
            var ionType = IcParameters.Instance.GetIonType(baseIon, neutralLoss, charge);
            var fragments = Utils.Utils.GetCompositions(Sequence, ionType.BaseIonType.IsPrefix);
            var peaks = new List<LabeledIon>();

            for (int i = 0; i < fragments.Count; i++) 
            {
                var ion = ionType.GetIon(fragments[i]);
                var correlationScore = Ms2Spectrum.GetCorrScore(ion, IcParameters.Instance.ProductIonTolerancePpm);
                var isotopePeaks = Ms2Spectrum.GetAllIsotopePeaks(ion, IcParameters.Instance.ProductIonTolerancePpm, RelativeIntensityThreshold);
                var index = i + 1;
                if (isotopePeaks == null)   isotopePeaks = new Peak[0];
                peaks.Add(new LabeledIon(Scan, index, isotopePeaks, correlationScore, ionType));
            }
            return peaks;
        }

        public List<LabeledIon> GetFragmentIons(IList<BaseIonType> baseIons, IList<NeutralLoss> neutralLosses, int minCharge, int maxCharge)
        {
            var fragmentIons = new List<LabeledIon>();
            foreach (var neutralLoss in neutralLosses)
            {
                foreach (var baseIonType in baseIons)
                {
                    for (var charge = minCharge;
                        charge <= maxCharge;
                        charge++)
                    {
                        fragmentIons.AddRange(GetFragmentIons(baseIonType, neutralLoss, charge));
                    }
                }
            }
            return fragmentIons;
        }

        public LabeledIon PrecursorIonPeaks(Spectrum spectrum=null)
        {
            if (spectrum == null) spectrum = Ms2Spectrum;
            var composition = Sequence.Aggregate(InformedProteomics.Backend.Data.Composition.Composition.Zero, (current, aa) => current + aa.Composition);
            var precursorIonType = new IonType("Precursor", InformedProteomics.Backend.Data.Composition.Composition.H2O, Charge, false);
            var ion = new Ion(composition + InformedProteomics.Backend.Data.Composition.Composition.H2O, Charge);
            var isotopePeaks = spectrum.GetAllIsotopePeaks(ion, IcParameters.Instance.PrecursorTolerancePpm,
                RelativeIntensityThreshold);
            var correlationScore = spectrum.GetCorrScore(ion, IcParameters.Instance.ProductIonTolerancePpm);
            if (isotopePeaks == null) isotopePeaks = new Peak[0];
            return new LabeledIon(Scan, 0, isotopePeaks, correlationScore, precursorIonType, false);
        }

        public double PrecursorMz
        {
            get
            {
                var composition = Sequence.Aggregate(InformedProteomics.Backend.Data.Composition.Composition.Zero, (current, aa) => current + aa.Composition);
                var ion = new Ion(composition + InformedProteomics.Backend.Data.Composition.Composition.H2O, Charge);
                return Math.Round(ion.GetMonoIsotopicMz(), 2);
            }
        }

        public Spectrum PreviousMs1
        {
            get
            {
                var prevms1ScanNum = Lcms.GetPrevScanNum(Scan, 1);
                return Lcms.GetSpectrum(prevms1ScanNum);
            }
        }

        public LabeledIon PrevMs1PrecursorIonPeaks
        {
            get
            {
                var spectrum = PreviousMs1;
                if (spectrum == null) return null;
                return PrecursorIonPeaks(spectrum);
            }
        }

        public Spectrum NextMs1
        {
            get
            {
                var nextms1ScanNum = Lcms.GetNextScanNum(Scan, 1);
                return Lcms.GetSpectrum(nextms1ScanNum);
            }
        }

        public LabeledIon NextMs1PrecursorIonPeaks
        {
            get
            {
                var spectrum = NextMs1;
                if (spectrum == null) return null;
                return PrecursorIonPeaks(spectrum);
            }
        }

        public int CompareTo(PrSm other)
        {
            return Scan.CompareTo(other.Scan);
        }

        private void ParseModifications(string modifications)
        {
            var mods = modifications.Split(',');
            if (mods.Length < 1 || mods[0] == "") return;
            foreach (var modParts in mods.Select(mod => mod.Split(' ')))
            {
                if (modParts.Length < 0) throw new FormatException("Invalid modification.");
                var modName = modParts[0];
                var modPos = Convert.ToInt32(modParts[1]);
                Modifications.Add(new Tuple<int, Modification>(modPos, Modification.Get(modName)));
            }
        }

        private void SetModifications(string modifications)
        {
            Modifications = new List<Tuple<int, Modification>>();
            // Build Sequence AminoAcid list
            SequenceText = Protein;
            Sequence = new Sequence(SequenceText, new AminoAcidSet());
            foreach (var aa in Sequence) SequenceLabel.Add(aa.Residue.ToString(CultureInfo.InvariantCulture));
            ParseModifications(modifications);

            // Add modifications to sequence
            Modifications.Sort(new CompareModByHighestPosition());   // sort in reverse order for insertion
            foreach (var mod in Modifications)
            {
                var pos = mod.Item1;
                if (pos > 0) pos--;
                var modLabel = String.Format("[{0}]", mod.Item2.Name);
                SequenceText = SequenceText.Insert(mod.Item1, modLabel);
                SequenceLabel[pos] += modLabel;
                var aa = Sequence[pos];
                var modaa = new ModifiedAminoAcid(aa, mod.Item2);
                Sequence[pos] = modaa;
            }
        }

        private const double RelativeIntensityThreshold = 0.1;
    }

    internal class CompareModByHighestPosition : IComparer<Tuple<int, Modification>>
    {
        public int Compare(Tuple<int, Modification> x, Tuple<int, Modification> y)
        {
            return (y.Item1.CompareTo(x.Item1));
        }
    }
}
