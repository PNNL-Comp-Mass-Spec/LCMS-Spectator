// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FastaEntry.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class represents an entry for a single protein in a FASTA database.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Text;
using InformedProteomics.Backend.Data.Sequence;
using ReactiveUI;

namespace LcmsSpectator.Models
{
    /// <summary>
    /// This class represents an entry for a single protein in a FASTA database.
    /// </summary>
    public class FastaEntry : ReactiveObject
    {
        /// <summary>
        /// Maximum possible line length of a FASTA sequence entry.
        /// </summary>
        private const int MaxLineLength = 70;

        /// <summary>
        /// The protein name.
        /// </summary>
        private string proteinName;

        /// <summary>
        /// The protein description.
        /// </summary>
        private string proteinDescription;

        /// <summary>
        /// The protein sequence as a string.
        /// </summary>
        private string proteinSequenceText;

        /// <summary>
        /// A value indicating whether this entry has been selected.
        /// </summary>
        private bool selected;

        /// <summary>
        /// Gets or sets the protein sequence as an InformedProteomics sequence.
        /// </summary>
        private Sequence proteinSequence;

        /// <summary>
        /// Initializes a new instance of the <see cref="FastaEntry"/> class.
        /// </summary>
        public FastaEntry()
        {
            ////// When ProteinSequenceText changes, update ProteinSequence.
            ////this.WhenAnyValue(x => x.ProteinSequenceText)
            ////    .Select(Sequence.GetSequenceFromMsGfPlusPeptideStr)
            ////    .Subscribe(sequence => this.ProteinSequence = sequence);

            ProteinName = string.Empty;
            ProteinSequenceText = string.Empty;
            proteinDescription = string.Empty;
            Selected = true;
        }

        /// <summary>
        /// Gets or sets the protein name.
        /// </summary>
        public string ProteinName
        {
            get => proteinName;
            set => this.RaiseAndSetIfChanged(ref proteinName, value);
        }

        /// <summary>
        /// Gets or sets the protein description.
        /// </summary>
        public string ProteinDescription
        {
            get => proteinDescription;
            set => this.RaiseAndSetIfChanged(ref proteinDescription, value);
        }

        /// <summary>
        /// Gets or sets the protein sequence as a string.
        /// </summary>
        public string ProteinSequenceText
        {
            get => proteinSequenceText;
            set => this.RaiseAndSetIfChanged(ref proteinSequenceText, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this entry has been selected.
        /// </summary>
        public bool Selected
        {
            get => selected;
            set => this.RaiseAndSetIfChanged(ref selected, value);
        }

        /// <summary>
        /// Gets or sets the protein sequence as an InformedProteomics sequence.
        /// </summary>
        public Sequence ProteinSequence
        {
            get => proteinSequence;
            set => this.RaiseAndSetIfChanged(ref proteinSequence, value);
        }

        /// <summary>
        /// Gets an entry formatted as it should be in a FASTA database file.
        /// </summary>
        public string FormattedEntry
        {
            get
            {
                var endIndex = Math.Min(MaxLineLength, ProteinSequenceText.Length);
                var sb = new StringBuilder(ProteinSequenceText.Length + ProteinSequenceText.Length / MaxLineLength);

                var startIndex = 0;

                while (endIndex <= ProteinSequenceText.Length)
                {
                    var length = endIndex - startIndex;
                    sb.Append(ProteinSequenceText, startIndex, length);
                    sb.Append('\n');
                    startIndex += MaxLineLength;
                    if (endIndex == ProteinSequenceText.Length)
                    {
                        break;
                    }

                    endIndex = Math.Min(endIndex + MaxLineLength, ProteinSequenceText.Length);
                }

                return string.Format(
                    ">{0} {1}\n{2}",
                    ProteinName,
                    ProteinDescription,
                    sb);
            }
        }
    }
}
