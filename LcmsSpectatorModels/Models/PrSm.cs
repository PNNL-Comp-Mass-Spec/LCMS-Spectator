using System;
using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;

namespace LcmsSpectatorModels.Models
{
    public class PrSm: IComparable<PrSm>
    {
        public String RawFileName { get; set; }
        public ILcMsRun Lcms { get; set; }
        public int Scan { get; set; }
        public int Charge { get; set; }
        public string SequenceText { get; set; }
        public Sequence Sequence { get; set; }
        public string ProteinName { get; set; }
        public string ProteinDesc { get; set; }
        public double Score { get; set; }
        public bool UseGolfScoring { get; set; } // lower the score the better
        public double QValue { get; set; }
        public bool Heavy { get; set; }

        public double RetentionTime { get { return (Lcms == null) ? 0.0 : Lcms.GetElutionTime(Scan); } }
        public Spectrum Ms2Spectrum { get { return (Lcms == null) ? null : Lcms.GetSpectrum(Scan); } }
        public string ProteinNameDesc { get { return String.Format("{0} {1}", ProteinName, ProteinDesc); } }
        
        public PrSm()
        {
            RawFileName = "";
            SequenceText = "";
            Sequence = new Sequence(new List<AminoAcid>());
            ProteinName = "";
            ProteinDesc = "";
            UseGolfScoring = false;
            Heavy = false;
        }

        public string ScanText
        {
            get { return String.Format("{0} ({1})", Scan, RawFileName); }
        }

        public Spectrum PreviousMs1 
        {
            get
            {
                if (Lcms == null) return null;
                var prevms1Scan = Lcms.GetPrevScanNum(Scan, 1);
                return Lcms.GetSpectrum(prevms1Scan);
            }
        }

        public Spectrum NextMs1
        {
            get
            {
                if (Lcms == null) return null;
                var nextms1Scan = Lcms.GetNextScanNum(Scan, 1);
                return Lcms.GetSpectrum(nextms1Scan);
            }
        }

        public double Mass
        {
            get
            {
                var composition = Sequence.Aggregate(InformedProteomics.Backend.Data.Composition.Composition.Zero, (current, aa) => current + aa.Composition);
                return Math.Round(composition.Mass, 2);
            }
        }

        public double PrecursorMz
        {
            get
            {
                if (Sequence.Count == 0) return 0.0;
                var composition = Sequence.Aggregate(InformedProteomics.Backend.Data.Composition.Composition.Zero, (current, aa) => current + aa.Composition);
                var ion = new Ion(composition + InformedProteomics.Backend.Data.Composition.Composition.H2O, Charge);
                return Math.Round(ion.GetMostAbundantIsotopeMz(), 2);
            }
        }

        public int CompareTo(PrSm other)
        {
            var comp = UseGolfScoring ? other.Score.CompareTo(Score) : Score.CompareTo(other.Score);
            return comp;
        }
    }

    public class PrSmScoreComparer : IComparer<PrSm>
    {
        public int Compare(PrSm x, PrSm y)
        {
            return (x.Score.CompareTo(y.Score));
        }
    }
}
