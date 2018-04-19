// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PrSm.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   A class that represents a protein-spectrum match identification.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.Readers.SequenceReaders;
using ReactiveUI;

namespace LcmsSpectator.Models
{
    /// <summary>
    /// A class that represents a protein-spectrum match identification.
    /// </summary>
    public class PrSm : ReactiveObject, IComparable<PrSm>
    {
        /// <summary>
        /// Sequence reader for parsing sequences for the dataset this PRSM belongs to.
        /// </summary>
        private readonly ISequenceReader sequenceReader;

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
        private double qvalue;

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
        /// <param name="sequenceReader">
        /// Sequence reader for parsing sequences for the dataset this PRSM belongs to.
        /// </param>
        public PrSm(ISequenceReader sequenceReader = null)
        {
            this.sequenceReader = sequenceReader ?? new SequenceReader();
            RawFileName = string.Empty;
            SequenceText = string.Empty;
            Sequence = new Sequence(new List<AminoAcid>());
            ProteinName = string.Empty;
            ProteinDesc = string.Empty;
            UseGolfScoring = false;
            Heavy = false;
            lcms = null;
            scan = 0;
            Mass = double.NaN;
            PrecursorMz = double.NaN;
            QValue = -1.0;
            charge = 1;
        }

        #region Public Properties

        /// <summary>
        /// Gets or sets the name of the raw file or data set that this is associated with.
        /// </summary>
        public string RawFileName
        {
            get => rawFileName;
            set => this.RaiseAndSetIfChanged(ref rawFileName, value);
        }

        /// <summary>
        /// Gets or sets the raw file or data set that this is associated with.
        /// </summary>
        public ILcMsRun LcMs
        {
            get => lcms;
            set => this.RaiseAndSetIfChanged(ref lcms, value);
        }

        /// <summary>
        /// Gets or sets the MS/MS Scan number of identification.
        /// </summary>
        public int Scan
        {
            get => scan;
            set => this.RaiseAndSetIfChanged(ref scan, value);
        }

        /// <summary>
        /// Gets the retention time of identification.
        /// Requires both LCMS and Scan to be set.
        /// </summary>
        public double RetentionTime => LcMs?.GetElutionTime(Scan) ?? 0.0;

        /// <summary>
        /// Gets the MS/MS Spectrum associated with Scan.
        /// Requires both LCMS and Scan to be set.
        /// </summary>
        public ProductSpectrum Ms2Spectrum => LcMs?.GetSpectrum(Scan) as ProductSpectrum;

        /// <summary>
        /// Gets the ActivationMethod for the MS/MS Spectrum.
        /// </summary>
        public ActivationMethod ActivationMethod => LcMs?.GetSpectrum(Scan, false) is ProductSpectrum spectrum ? spectrum.ActivationMethod : ActivationMethod.Unknown;

        /// <summary>
        /// Gets the spectrum for previous MS1 scan before Scan.
        /// Requires both LCMS and Scan to be set.
        /// </summary>
        public Spectrum PreviousMs1
        {
            get
            {
                if (LcMs == null)
                {
                    return null;
                }

                var prevms1Scan = LcMs.GetPrevScanNum(Scan, 1);
                var spectrum = LcMs.GetSpectrum(prevms1Scan);
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
                if (LcMs == null)
                {
                    return null;
                }

                var nextms1Scan = LcMs.GetNextScanNum(Scan, 1);
                return LcMs.GetSpectrum(nextms1Scan);
            }
        }

        /// <summary>
        /// Gets the scan and data set label for identification.
        /// </summary>
        public string ScanText => string.Format("{0} ({1})", Scan, RawFileName);

        /// <summary>
        /// Gets or sets the charge state of identification.
        /// </summary>
        public int Charge
        {
            get => charge;
            set => this.RaiseAndSetIfChanged(ref charge, value);
        }

        /// <summary>
        /// Gets or sets the string representing the sequence of the identification.
        /// </summary>
        public string SequenceText
        {
            get => sequenceText;

            set
            {
                if (sequenceText != value)
                {
                    var parsedSequence = ParseSequence(value);
                    if (parsedSequence != null)
                    {
                        sequenceText = value;
                        Sequence = parsedSequence;

                        this.RaisePropertyChanged();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the string containing list of modifications in the sequence and their positions.
        /// </summary>
        public string ModificationLocations => GetModificationLocations();

        /// <summary>
        /// Gets or sets the name of identified protein.
        /// </summary>
        public string ProteinName
        {
            get => proteinName;
            set => this.RaiseAndSetIfChanged(ref proteinName, value);
        }

        /// <summary>
        /// Gets or sets the description of identified protein.
        /// </summary>
        public string ProteinDesc
        {
            get => proteinDesc;
            set => this.RaiseAndSetIfChanged(ref proteinDesc, value);
        }

        /// <summary>
        /// Gets the conjoined protein name and description.
        /// </summary>
        public string ProteinNameDesc => string.Format("{0} {1}", ProteinName, ProteinDesc);

        /// <summary>
        /// Gets or sets the identification score.
        /// </summary>
        public double Score
        {
            get => score;
            set => this.RaiseAndSetIfChanged(ref score, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether higher scores or lower scores are better.
        /// </summary>
        public bool UseGolfScoring
        {
            get => useGolfScoring;
            set => this.RaiseAndSetIfChanged(ref useGolfScoring, value);
        }

        /// <summary>
        /// Gets or sets the QValue of identification.
        /// </summary>
        public double QValue
        {
            get => qvalue;
            set => this.RaiseAndSetIfChanged(ref qvalue, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the identification sequence has a heavy label?
        /// </summary>
        public bool Heavy
        {
            get => heavy;
            set => this.RaiseAndSetIfChanged(ref heavy, value);
        }

        /// <summary>
        /// Gets or sets the actual sequence of identification.
        /// Mass and PrecursorMZ are updated to stay consistent with the sequence.
        /// </summary>
        public Sequence Sequence
        {
            get => sequence;

            set
            {
                if (value != null && !value.Equals(sequence))
                {
                    sequence = value;
                    if (sequence.Count > 0)
                    {
                        Mass = sequence.Mass + Composition.H2O.Mass;
                        var ion = new Ion(sequence.Composition + Composition.H2O, Charge);
                        PrecursorMz = ion.GetMostAbundantIsotopeMz();
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
            get => mass;
            set => this.RaiseAndSetIfChanged(ref mass, value);
        }

        /// <summary>
        /// Gets or sets the M/Z of most abundant isotope of identification.
        /// This is updated when Sequence changes.
        /// </summary>
        public double PrecursorMz
        {
            get => precursorMz;
            set => this.RaiseAndSetIfChanged(ref precursorMz, value);
        }
        #endregion

        /// <summary>
        /// Set newSequence as the visibile sequence and actual underlying sequence.
        /// If the underlying sequence is null, the visible sequence will be parsed to
        /// create it.
        /// </summary>
        /// <param name="sequenceStr">The visible sequence.</param>
        /// <param name="newSequence">Actual underlying sequence.</param>
        public void SetSequence(string sequenceStr, Sequence newSequence = null)
        {
            if (newSequence != null)
            {
                sequenceText = sequenceStr;
                Sequence = newSequence;
            }
            else
            {
                SequenceText = sequenceStr;
            }
        }

        /// <summary>
        /// Update all modifications in the sequence.
        /// </summary>
        public void UpdateModifications()
        {
            var aminoAcidSet = new AminoAcidSet();
            for (var i = 0; i < Sequence.Count; i++)
            {
                if (Sequence[i] is ModifiedAminoAcid modAminoAcid)
                {
                    var modification = Modification.Get(modAminoAcid.Modification.Name);
                    if (modification != null)
                    {
                        Sequence[i] = new ModifiedAminoAcid(aminoAcidSet.GetAminoAcid(modAminoAcid.Residue), modification);
                    }
                }
            }

            Sequence = new Sequence(Sequence);
        }

        /// <summary>
        /// Compares this to another PRSM object by Score.
        /// </summary>
        /// <param name="other">PRSM object to compare to.</param>
        /// <returns>Integer indicating if this is greater than the PRSM object.</returns>
        public int CompareTo(PrSm other)
        {
            var comp = UseGolfScoring ? other.Score.CompareTo(Score) : Score.CompareTo(other.Score);
            return comp;
        }

        /// <summary>
        /// Gets the fragmentation sequence for the PRSM.
        /// </summary>
        /// <returns>The FragmentationSequence.</returns>
        public FragmentationSequence GetFragmentationSequence()
        {
            return LcMs == null ? null : new FragmentationSequence(sequence, charge, lcms, ActivationMethod);
        }

        /// <summary>
        /// Get location of modifications in sequence.
        /// </summary>
        /// <returns>String containing modifications in the form: ResidueIndex[Modification]</returns>
        private string GetModificationLocations()
        {
            var modificationLocations = new StringBuilder();
            for (var i = 0; i < Sequence.Count; i++)
            {
                if (Sequence[i] is ModifiedAminoAcid aa)
                {
                    modificationLocations.AppendFormat("{0}{1}[{2}] ", aa.Residue, i + 1, aa.Modification.Name);
                }
            }

            if (modificationLocations.Length > 0)
            {
                modificationLocations.Remove(modificationLocations.Length - 1, 1);
            }

            return modificationLocations.ToString();
        }

        /// <summary>
        /// Parse sequence string into a InformedProteomics sequence.
        /// </summary>
        /// <param name="sequenceStr">The sequence string</param>
        /// <returns>InformedProteomics sequence object.</returns>
        private Sequence ParseSequence(string sequenceStr)
        {
            try
            {
                var seq = sequenceReader.Read(sequenceStr);
                return seq;
            }
            catch (Exception)
            {
                return null;
            }
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
                if (x == null)
                    return -1;

                if (y == null)
                    return 1;

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
                if (x == null)
                    return -1;

                if (y == null)
                    return 1;

                return x.Scan.CompareTo(y.Scan);
            }
        }
    }
}
