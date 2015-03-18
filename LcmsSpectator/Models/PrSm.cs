using System;
using System.Collections.Generic;
using System.Text;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using ReactiveUI;

namespace LcmsSpectator.Models
{
    public class PrSm: ReactiveObject, IComparable<PrSm>
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
            _lcms = null;
            _scan = 0;
            Mass = Double.NaN;
            PrecursorMz = Double.NaN;

            // When sequence updates, update mass
            //this.WhenAnyValue(x => x.Sequence)
            //    .Where(sequence => sequence.Count > 0)
            //    .Select(sequence => sequence.Mass + Composition.H2O.Mass)
            //    .Subscribe(mass => Mass = mass);

            //// When sequence updates, update precursor m/z
            //this.WhenAnyValue(x => x.Sequence)
            //    .Where(sequence => sequence.Count > 0)
            //    .Select(sequence =>
            //    {
            //        var ion = new Ion(sequence.Composition + Composition.H2O, Charge);
            //        return ion.GetMostAbundantIsotopeMz();
            //    })
            //    .Subscribe(precursorMz => PrecursorMz = precursorMz);
        }

        #region Public Properties

        private String _rawFileName;
        /// <summary>
        /// Name of the raw file or data set that this is associated with.
        /// </summary>
        public String RawFileName
        {
            get { return _rawFileName; } 
            set { this.RaiseAndSetIfChanged(ref _rawFileName, value); }
        }

        private ILcMsRun _lcms;
        /// <summary>
        /// Raw file or data set that this is associated with.
        /// </summary>
        public ILcMsRun Lcms
        {
            get { return _lcms; }
            set { this.RaiseAndSetIfChanged(ref _lcms, value); }
        }

        private int _scan;
        /// <summary>
        /// Ms2 Scan number of identification.
        /// </summary>
        public int Scan
        {
            get { return _scan; }
            set { this.RaiseAndSetIfChanged(ref _scan, value); }
        }

        /// <summary>
        /// Retention time of identification.
        /// Requires both Lcms and Scan to be set.
        /// </summary>
        public double RetentionTime { get { return (Lcms == null) ? 0.0 : Lcms.GetElutionTime(Scan); } }

        /// <summary>
        /// Ms2 Spectrum associated with Scan.
        /// Requires both Lcms and Scan to be set.
        /// </summary>
        public ProductSpectrum Ms2Spectrum { get { return (Lcms == null) ? null : Lcms.GetSpectrum(Scan) as ProductSpectrum; } }

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
                var spectrum = Lcms.GetSpectrum(prevms1Scan);
                return spectrum;
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

        private int _charge;
        /// <summary>
        /// Charge state of identification.
        /// </summary>
        public int Charge
        {
            get { return _charge; }
            set { this.RaiseAndSetIfChanged(ref _charge, value); }
        }

        private string _sequenceText;

        /// <summary>
        /// String representing the sequence of the identification.
        /// </summary>
        public string SequenceText
        {
            get { return _sequenceText; }
            set { this.RaiseAndSetIfChanged(ref _sequenceText, value); }
        }

        /// <summary>
        /// String containing list of modifications in the sequence and their positions.
        /// </summary>
        public string ModificationLocations
        {
            get { return GetModificationLocations();  }
        }

        private string _proteinName;
        /// <summary>
        /// Name of identified protein.
        /// </summary>
        public string ProteinName
        {
            get { return _proteinName; }
            set { this.RaiseAndSetIfChanged(ref _proteinName, value); }
        }

        private string _proteinDesc;
        /// <summary>
        /// Description of identified protein.
        /// </summary>
        public string ProteinDesc
        {
            get { return _proteinDesc; }
            set { this.RaiseAndSetIfChanged(ref _proteinDesc, value); }
        }

        /// <summary>
        /// Conjoined protein name and description.
        /// </summary>
        public string ProteinNameDesc { get { return String.Format("{0} {1}", ProteinName, ProteinDesc); } }

        private double _score;
        /// <summary>
        /// Identification score.
        /// </summary>
        public double Score
        {
            get { return _score; }
            set { this.RaiseAndSetIfChanged(ref _score, value); }
        }

        private bool _useGolfScoring;
        /// <summary>
        /// Are higher scores better or lower scores better?
        /// True = lower score is better
        /// False = higher score is better
        /// Default is False.
        /// </summary>
        public bool UseGolfScoring
        {
            get { return _useGolfScoring; }
            set { this.RaiseAndSetIfChanged(ref _useGolfScoring, value); }
        }

        private double _qValue;
        /// <summary>
        /// QValue of identification.
        /// </summary>
        public double QValue
        {
            get { return _qValue; } 
            set { this.RaiseAndSetIfChanged(ref _qValue, value); }
        }

        private bool _heavy;
        /// <summary>
        /// Is this identification sequence have a heavy label?
        /// Default is False.
        /// </summary>
        public bool Heavy
        {
            get { return _heavy; }
            set { this.RaiseAndSetIfChanged(ref _heavy, value); }
        }

        /// <summary>
        /// Actual sequence of identification.
        /// Mass and PrecursorMz are updated to stay consistent with the sequence.
        /// </summary>
        public Sequence Sequence
        {
            get { return _sequence; }
            set
            {
                if (value != null && !value.Equals(_sequence))
                {
                    _sequence = value;
                    if (_sequence.Count > 0)
                    {
                        Mass = _sequence.Mass + Composition.H2O.Mass;
                        var ion = new Ion(_sequence.Composition + Composition.H2O, Charge);
                        PrecursorMz = ion.GetMostAbundantIsotopeMz();   
                    }
                }
                this.RaisePropertyChanged();
            }
        }

        private double _mass;
        /// <summary>
        /// Monoisotopic mass of identification.
        /// This is updated when Sequence changes.
        /// </summary>
        public double Mass
        {
            get { return _mass; }
            set { this.RaiseAndSetIfChanged(ref _mass, value); }
        }

        private double _precursorMz;
        /// <summary>
        /// M/Z of most abundant isotope of identification.
        /// This is updated when Sequence changes.
        /// </summary>
        public double PrecursorMz
        {
            get { return _precursorMz; }
            set { this.RaiseAndSetIfChanged(ref _precursorMz, value); }
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

        #region Private Methods

        private string GetModificationLocations()
        {
            var modificationLocations = new StringBuilder();
            for (int i = 0; i < Sequence.Count; i++)
            {
                var aa = Sequence[i] as ModifiedAminoAcid;
                if (aa != null)
                {
                    modificationLocations.AppendFormat("{0}[{1}] ", i+1, aa.Modification.Name);
                }
            }
            return modificationLocations.ToString();
        }
        #endregion

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
