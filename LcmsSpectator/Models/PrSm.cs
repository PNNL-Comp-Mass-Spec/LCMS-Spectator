// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PrSm.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   A class that represents a protein-spectrum match identification.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using InformedProteomics.Backend.Data.Biology;
    using InformedProteomics.Backend.Data.Composition;
    using InformedProteomics.Backend.Data.Sequence;
    using InformedProteomics.Backend.Data.Spectrometry;
    using InformedProteomics.Backend.MassSpecData;
    using ReactiveUI;
    
    /// <summary>
    /// A class that represents a protein-spectrum match identification.
    /// </summary>
    public class PrSm : ReactiveObject, IComparable<PrSm>
    {
        /// <summary>
        /// The name of the raw file or data set that this is associated with.
        /// </summary>
        private string rawFileName;

        /// <summary>
        /// The raw file or data set that this is associated with.
        /// </summary>
        private ILcMsRun lcms;

        /// <summary>
        /// The MS/MS Scan number of identification.
        /// </summary>
        private int scan;

        /// <summary>
        /// The charge state of identification.
        /// </summary>
        private int charge;

        /// <summary>
        /// The string representing the sequence of the identification.
        /// </summary>
        private string sequenceText;

        /// <summary>
        /// The name of identified protein.
        /// </summary>
        private string proteinName;

        /// <summary>
        /// The description of identified protein.
        /// </summary>
        private string proteinDesc;

        /// <summary>
        /// The identification score.
        /// </summary>
        private double score;

        /// <summary>
        /// A value indicating whether higher scores or lower scores are better.
        /// </summary>
        private bool useGolfScoring;

        /// <summary>
        /// The QValue of identification.
        /// </summary>
        private double qValue;

        /// <summary>
        /// A value indicating whether the identification sequence has a heavy label?
        /// </summary>
        private bool heavy;

        /// <summary>
        /// The monoisotopic mass of the identification.
        /// This is updated when Sequence changes.
        /// </summary>
        private double mass;

        /// <summary>
        /// The M/Z of most abundant isotope of identification.
        /// This is updated when Sequence changes.
        /// </summary>
        private double precursorMz;

        /// <summary>
        /// The actual sequence of identification.
        /// </summary>
        private Sequence sequence;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrSm"/> class.
        /// </summary>
        public PrSm()
        {
            this.RawFileName = string.Empty;
            this.SequenceText = string.Empty;
            this.Sequence = new Sequence(new List<AminoAcid>());
            this.ProteinName = string.Empty;
            this.ProteinDesc = string.Empty;
            this.UseGolfScoring = false;
            this.Heavy = false;
            this.lcms = null;
            this.scan = 0;
            this.Mass = double.NaN;
            this.PrecursorMz = double.NaN;
        }

        #region Public Properties

        /// <summary>
        /// Gets or sets the name of the raw file or data set that this is associated with.
        /// </summary>
        public string RawFileName
        {
            get { return this.rawFileName; } 
            set { this.RaiseAndSetIfChanged(ref this.rawFileName, value); }
        }

        /// <summary>
        /// Gets or sets the raw file or data set that this is associated with.
        /// </summary>
        public ILcMsRun LcMs
        {
            get { return this.lcms; }
            set { this.RaiseAndSetIfChanged(ref this.lcms, value); }
        }

        /// <summary>
        /// Gets or sets the MS/MS Scan number of identification.
        /// </summary>
        public int Scan
        {
            get { return this.scan; }
            set { this.RaiseAndSetIfChanged(ref this.scan, value); }
        }

        /// <summary>
        /// Gets the retention time of identification.
        /// Requires both LCMS and Scan to be set.
        /// </summary>
        public double RetentionTime
        {
            get
            {
                return (this.LcMs == null) ? 0.0 : this.LcMs.GetElutionTime(this.Scan);
            }
        }

        /// <summary>
        /// Gets the MS/MS Spectrum associated with Scan.
        /// Requires both LCMS and Scan to be set.
        /// </summary>
        public ProductSpectrum Ms2Spectrum
        {
            get
            {
                return (this.LcMs == null) ? null : this.LcMs.GetSpectrum(this.Scan) as ProductSpectrum;
            }
        }

        /// <summary>
        /// Gets the spectrum for previous MS1 scan before Scan.
        /// Requires both LCMS and Scan to be set.
        /// </summary>
        public Spectrum PreviousMs1
        {
            get
            {
                if (this.LcMs == null)
                {
                    return null;
                }

                var prevms1Scan = this.LcMs.GetPrevScanNum(this.Scan, 1);
                var spectrum = this.LcMs.GetSpectrum(prevms1Scan);
                return spectrum;
            }
        }

        /// <summary>
        /// Gets the spectrum for next MS1 scan after Scan.
        /// Requires both LCMS and Scan to be set.
        /// </summary>
        public Spectrum NextMs1
        {
            get
            {
                if (this.LcMs == null)
                {
                    return null;
                }

                var nextms1Scan = this.LcMs.GetNextScanNum(this.Scan, 1);
                return this.LcMs.GetSpectrum(nextms1Scan);
            }
        }

        /// <summary>
        /// Gets the scan and data set label for identification.
        /// </summary>
        public string ScanText
        {
            get { return string.Format("{0} ({1})", this.Scan, this.RawFileName); }
        }

        /// <summary>
        /// Gets or sets the charge state of identification.
        /// </summary>
        public int Charge
        {
            get { return this.charge; }
            set { this.RaiseAndSetIfChanged(ref this.charge, value); }
        }

        /// <summary>
        /// Gets or sets the string representing the sequence of the identification.
        /// </summary>
        public string SequenceText
        {
            get { return this.sequenceText; }
            set { this.RaiseAndSetIfChanged(ref this.sequenceText, value); }
        }

        /// <summary>
        /// Gets the string containing list of modifications in the sequence and their positions.
        /// </summary>
        public string ModificationLocations
        {
            get { return this.GetModificationLocations(); }
        }

        /// <summary>
        /// Gets or sets the name of identified protein.
        /// </summary>
        public string ProteinName
        {
            get { return this.proteinName; }
            set { this.RaiseAndSetIfChanged(ref this.proteinName, value); }
        }

        /// <summary>
        /// Gets or sets the description of identified protein.
        /// </summary>
        public string ProteinDesc
        {
            get { return this.proteinDesc; }
            set { this.RaiseAndSetIfChanged(ref this.proteinDesc, value); }
        }

        /// <summary>
        /// Gets the conjoined protein name and description.
        /// </summary>
        public string ProteinNameDesc
        {
            get
            {
                return string.Format("{0} {1}", this.ProteinName, this.ProteinDesc);
            }
        }

        /// <summary>
        /// Gets or sets the identification score.
        /// </summary>
        public double Score
        {
            get { return this.score; }
            set { this.RaiseAndSetIfChanged(ref this.score, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether higher scores or lower scores are better.
        /// </summary>
        public bool UseGolfScoring
        {
            get { return this.useGolfScoring; }
            set { this.RaiseAndSetIfChanged(ref this.useGolfScoring, value); }
        }

        /// <summary>
        /// Gets or sets the QValue of identification.
        /// </summary>
        public double QValue
        {
            get { return this.qValue; }
            set { this.RaiseAndSetIfChanged(ref this.qValue, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the identification sequence has a heavy label?
        /// </summary>
        public bool Heavy
        {
            get { return this.heavy; }
            set { this.RaiseAndSetIfChanged(ref this.heavy, value); }
        }

        /// <summary>
        /// Gets or sets the actual sequence of identification.
        /// Mass and PrecursorMZ are updated to stay consistent with the sequence.
        /// </summary>
        public Sequence Sequence
        {
            get
            {
                return this.sequence;
            }

            set
            {
                if (value != null && !value.Equals(this.sequence))
                {
                    this.sequence = value;
                    if (this.sequence.Count > 0)
                    {
                        this.Mass = this.sequence.Mass + Composition.H2O.Mass;
                        var ion = new Ion(this.sequence.Composition + Composition.H2O, this.Charge);
                        this.PrecursorMz = ion.GetMostAbundantIsotopeMz();   
                    }
                }

                this.RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the monoisotopic mass of the identification.
        /// This is updated when Sequence changes.
        /// </summary>
        public double Mass
        {
            get { return this.mass; }
            set { this.RaiseAndSetIfChanged(ref this.mass, value); }
        }

        /// <summary>
        /// Gets or sets the M/Z of most abundant isotope of identification.
        /// This is updated when Sequence changes.
        /// </summary>
        public double PrecursorMz
        {
            get { return this.precursorMz; }
            set { this.RaiseAndSetIfChanged(ref this.precursorMz, value); }
        }
        #endregion

        /// <summary>
        /// Compares this to another PRSM object by Score.
        /// </summary>
        /// <param name="other">PRSM object to compare to.</param>
        /// <returns>Integer indicating if this is greater than the PRSM object.</returns>
        public int CompareTo(PrSm other)
        {
            var comp = this.UseGolfScoring ? other.Score.CompareTo(this.Score) : this.Score.CompareTo(other.Score);
            return comp;
        }

        /// <summary>
        /// Get location of modifications in sequence.
        /// </summary>
        /// <returns>String containing modifications in the form: ResidueIndex[Modification]</returns>
        private string GetModificationLocations()
        {
            var modificationLocations = new StringBuilder();
            for (int i = 0; i < Sequence.Count; i++)
            {
                var aa = Sequence[i] as ModifiedAminoAcid;
                if (aa != null)
                {
                    modificationLocations.AppendFormat("{0}{1}[{2}] ", aa.Residue, i + 1, aa.Modification.Name);
                }
            }

            return modificationLocations.ToString();
        }

        /// <summary>
        /// Compares two PRSM objects by Score value.
        /// </summary>
        public class PrSmScoreComparer : IComparer<PrSm>
        {
            /// <summary>
            /// Compare two PRSM objects by maximum Score value.
            /// </summary>
            /// <param name="x">Left PRSM</param>
            /// <param name="y">Right PRSM</param>
            /// <returns>Integer indicating if the left PRSM is greater than the right PRSM.</returns>
            public int Compare(PrSm x, PrSm y)
            {
                var comp = x.UseGolfScoring ? x.Score.CompareTo(y.Score) : y.Score.CompareTo(x.Score);
                return comp;
            }
        }

        /// <summary>
        /// Compares two PRSM objects by Scan number.
        /// </summary>
        public class PrSmScanComparer : IComparer<PrSm>
        {
            /// <summary>
            /// Compare two PRSM objects by Scan number.
            /// </summary>
            /// <param name="x">Left PRSM</param>
            /// <param name="y">Right PRSM</param>
            /// <returns>Integer indicating if the left PRSM is greater than the right PRSM.</returns>
            public int Compare(PrSm x, PrSm y)
            {
                return x.Scan.CompareTo(y.Scan);
            }
        }
    }
}
