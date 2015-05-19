// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FastaEntry.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class represents an entry for a single protein in a FASTA database.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Models
{
    using System;
    using System.Text;
    using ReactiveUI;

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
        /// The stored formatted entry for fast access.
        /// </summary>
        private string formattedEntry;

        /// <summary>
        /// A value indicating whether this entry has been selected.
        /// </summary>
        private bool selected;

        /// <summary>
        /// Initializes a new instance of the <see cref="FastaEntry"/> class. 
        /// </summary>
        public FastaEntry()
        {
            ////// When ProteinSequenceText changes, update ProteinSequence.
            ////this.WhenAnyValue(x => x.ProteinSequenceText)
            ////    .Select(Sequence.GetSequenceFromMsGfPlusPeptideStr)
            ////    .Subscribe(sequence => this.ProteinSequence = sequence);

            this.ProteinName = string.Empty;
            this.ProteinSequenceText = string.Empty;
            this.proteinDescription = string.Empty;
            this.Selected = true;
            this.formattedEntry = null;

            this.WhenAnyValue(x => x.ProteinName, x => x.ProteinDescription, x => x.ProteinSequenceText)
                .Subscribe(_ => this.formattedEntry = null);
        }

        /////// <summary>
        /////// The protein sequence as an InformedProteomics sequence.
        /////// </summary>
        ////private Sequence proteinSequence;

        /// <summary>
        /// Gets or sets the protein name.
        /// </summary>
        public string ProteinName
        {
            get { return this.proteinName; }
            set { this.RaiseAndSetIfChanged(ref this.proteinName, value); }
        }

        /// <summary>
        /// Gets or sets the protein description.
        /// </summary>
        public string ProteinDescription
        {
            get { return this.proteinDescription; }
            set { this.RaiseAndSetIfChanged(ref this.proteinDescription, value); }
        }

        /// <summary>
        /// Gets or sets the protein sequence as a string.
        /// </summary>
        public string ProteinSequenceText
        {
            get { return this.proteinSequenceText; }
            set { this.RaiseAndSetIfChanged(ref this.proteinSequenceText, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this entry has been selected.
        /// </summary>
        public bool Selected
        {
            get { return this.selected; }
            set { this.RaiseAndSetIfChanged(ref this.selected, value); }
        }

        /////// <summary>
        /////// Gets or sets the protein sequence as an InformedProteomics sequence.
        /////// </summary>
        ////public Sequence ProteinSequence
        ////{
        ////    get { return this.proteinSequence; }
        ////    private set { this.RaiseAndSetIfChanged(ref this.proteinSequence, value); }
        ////}

        /// <summary>
        /// Gets an entry formatted as it should be in a FASTA database file.
        /// </summary>
        public string FormattedEntry 
        {
            get
            {
                if (this.formattedEntry != null)
                {
                    return this.formattedEntry;
                }

                var startIndex = 0;
                var endIndex = Math.Min(MaxLineLength, this.ProteinSequenceText.Length - startIndex);
                var strbuilder =
                    new StringBuilder(
                        this.ProteinSequenceText.Length + (this.ProteinSequenceText.Length / MaxLineLength));

                while (endIndex <= this.ProteinSequenceText.Length)
                {
                    var length = endIndex - startIndex;
                    strbuilder.Append(this.ProteinSequenceText.Substring(startIndex, length));
                    strbuilder.Append('\n');
                    startIndex += MaxLineLength;
                    if (endIndex == this.ProteinSequenceText.Length)
                    {
                        break;
                    }

                    endIndex = Math.Min(endIndex + MaxLineLength, this.ProteinSequenceText.Length);
                }

                this.formattedEntry = string.Format(
                    ">{0} {1}\n{2}",
                    this.ProteinName,
                    this.ProteinDescription,
                    strbuilder);
                return this.formattedEntry;
            }
        }
    }
}
