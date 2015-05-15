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
    using ReactiveUI;

    /// <summary>
    /// This class represents an entry for a single protein in a FASTA database.
    /// </summary>
    public class FastaEntry : ReactiveObject
    {
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

        /////// <summary>
        /////// Initializes a new instance of the <see cref="FastaEntry"/> class. 
        /////// </summary>
        ////public FastaEntry()
        ////{
        ////    ////// When ProteinSequenceText changes, update ProteinSequence.
        ////    ////this.WhenAnyValue(x => x.ProteinSequenceText)
        ////    ////    .Select(Sequence.GetSequenceFromMsGfPlusPeptideStr)
        ////    ////    .Subscribe(sequence => this.ProteinSequence = sequence);
        ////}

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

        /////// <summary>
        /////// Gets or sets the protein sequence as an InformedProteomics sequence.
        /////// </summary>
        ////public Sequence ProteinSequence
        ////{
        ////    get { return this.proteinSequence; }
        ////    private set { this.RaiseAndSetIfChanged(ref this.proteinSequence, value); }
        ////}

        /// <summary>
        /// Get entry formatted as it should be in a FASTA database file.
        /// </summary>
        /// <returns>The formatted entry as a string.</returns>
        public string GetFormattedEntry()
        {
            return string.Format(">{0} {1}\n{2}", this.ProteinName, this.ProteinDescription, this.ProteinSequenceText);
        }
    }
}
