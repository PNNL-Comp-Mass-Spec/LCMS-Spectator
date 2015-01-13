using System;
using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;

namespace LcmsSpectatorModels.Models
{
    public class PrSm: IComparable<PrSm>
    {
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

        #region Public Properties
        /// <summary>
        /// Name of the raw file or data set that this is associated with.
        /// </summary>
        public String RawFileName { get; set; }
        
        /// <summary>
        /// Raw file or data set that this is associated with.
        /// </summary>
        public ILcMsRun Lcms { get; set; }
        
        /// <summary>
        /// Ms2 Scan number of identification.
        /// </summary>
        public int Scan { get; set; }

        /// <summary>
        /// Retention time of identification.
        /// Requires both Lcms and Scan to be set.
        /// </summary>
        public double RetentionTime { get { return (Lcms == null) ? 0.0 : Lcms.GetElutionTime(Scan); } }

        /// <summary>
        /// Ms2 Spectrum associated with Scan.
        /// Requires both Lcms and Scan to be set.
        /// </summary>
        public Spectrum Ms2Spectrum { get { return (Lcms == null) ? null : Lcms.GetSpectrum(Scan); } }

        /// <summary>
        /// Spectrum for previous ms1 scan before Scan.
        /// Requires both Lcms and Scan to be set.
        /// </summary>
        public Spectrum PreviousMs1
        {
            get
            {
                if (Lcms == null) return null;
                var prevms1Scan = Lcms.GetPrevScanNum(Scan, 1);
                return Lcms.GetSpectrum(prevms1Scan);
            }
        }

        /// <summary>
        /// Spectrum for next ms1 scan after Scan.
        /// Requires both Lcms and Scan to be set.
        /// </summary>
        public Spectrum NextMs1
        {
            get
            {
                if (Lcms == null) return null;
                var nextms1Scan = Lcms.GetNextScanNum(Scan, 1);
                return Lcms.GetSpectrum(nextms1Scan);
            }
        }

        /// <summary>
        /// Scan and data set label for identification.
        /// </summary>
        public string ScanText
        {
            get { return String.Format("{0} ({1})", Scan, RawFileName); }
        }

        /// <summary>
        /// Charge state of identification.
        /// </summary>
        public int Charge { get; set; }

        /// <summary>
        /// String representing the sequence of the identification.
        /// </summary>
        public string SequenceText { get; set; }

        /// <summary>
        /// Name of identified protein.
        /// </summary>
        public string ProteinName { get; set; }

        /// <summary>
        /// Description of identified protein.
        /// </summary>
        public string ProteinDesc { get; set; }

        /// <summary>
        /// Conjoined protein name and description.
        /// </summary>
        public string ProteinNameDesc { get { return String.Format("{0} {1}", ProteinName, ProteinDesc); } }

        /// <summary>
        /// Identification score.
        /// </summary>
        public double Score { get; set; }
        
        /// <summary>
        /// Are higher scores better or lower scores better?
        /// True = lower score is better
        /// False = higher score is better
        /// Default is False.
        /// </summary>
        public bool UseGolfScoring { get; set; }

        /// <summary>
        /// QValue of identification.
        /// </summary>
        public double QValue { get; set; }
        
        /// <summary>
        /// Is this identification sequence have a heavy label?
        /// Default is False.
        /// </summary>
        public bool Heavy { get; set; }

        /// <summary>
        /// Actual sequence of identification.
        /// </summary>
        public Sequence Sequence
        {
            get { return _sequence; }
            set
            {
                _sequence = value;
                double mass;
                if (_sequence.Count > 0)
                {
                    var composition = new Composition(Composition.H2O);
                    composition = _sequence.Aggregate(composition, (current, aa) => current + aa.Composition);
                    mass = composition.Mass;
                }
                else mass = Double.NaN;
                Mass = mass;
            }
        }

        /// <summary>
        /// Monoisotopic mass of identification.
        /// This is updated when Sequence changes.
        /// </summary>
        public double Mass { get; set; }

        /// <summary>
        /// M/Z of most abundant isotope of identification.
        /// </summary>
        public double PrecursorMz
        {
            get
            {
                if (Sequence.Count == 0) return Double.NaN;
                var composition = Sequence.Aggregate(Composition.Zero, (current, aa) => current + aa.Composition);
                var ion = new Ion(composition + Composition.H2O, Charge);
                return ion.GetMostAbundantIsotopeMz();
            }
        }
        #endregion

        /// <summary>
        /// Compares this to another PrSm object by Score.
        /// </summary>
        /// <param name="other">PrSm object to compare to.</param>
        /// <returns>Integer indicating if this is greater than the PrSm object.</returns>
        public int CompareTo(PrSm other)
        {
            var comp = UseGolfScoring ? other.Score.CompareTo(Score) : Score.CompareTo(other.Score);
            return comp;
        }

        private Sequence _sequence;
    }

    /// <summary>
    /// Compares two PrSm objects by Score.
    /// </summary>
    public class PrSmScoreComparer : IComparer<PrSm>
    {
        public int Compare(PrSm x, PrSm y)
        {
            return (x.Score.CompareTo(y.Score));
        }
    }

    /// <summary>
    /// Compares two PrSm objects by Scan.
    /// </summary>
    public class PrSmScanComparer : IComparer<PrSm>
    {
        public int Compare(PrSm x, PrSm y)
        {
            return (x.Scan.CompareTo(y.Scan));
        }
    }
}
