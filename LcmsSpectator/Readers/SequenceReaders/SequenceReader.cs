// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceReader.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Reader for protein/peptide sequences in the LCMSSpectator style or MS-GF+ style.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using InformedProteomics.Backend.Data.Sequence;

namespace LcmsSpectator.Readers.SequenceReaders
{
    /// <summary>
    /// Reader for protein/peptide sequences in the LCMSSpectator style or MS-GF+ style.
    /// </summary>
    public class SequenceReader : ISequenceReader
    {
        private readonly MsgfPlusSequenceReader mMsgfPlusSequenceParser;

        private readonly LcmsSpectatorSequenceReader mLcmsSpectatorSequenceParser;

        /// <summary>
        /// Initializes a new instance of the <see cref="SequenceReader"/> class.
        /// </summary>
        /// <param name="trimAnnotations">
        /// A value indicating whether the N-terminal and C-terminal amino acids should be trimmed.
        /// </param>
        public SequenceReader(bool trimAnnotations = false)
        {
            mMsgfPlusSequenceParser = new MsgfPlusSequenceReader(trimAnnotations);
            mLcmsSpectatorSequenceParser = new LcmsSpectatorSequenceReader(trimAnnotations);
        }

        /// <summary>
        /// Parse a protein/peptide sequence in the LCMSSpectator style or MS-GF+ style.
        /// </summary>
        /// <param name="sequence">The sequence as a string..</param>
        /// <returns>The parsed sequence.</returns>
        public Sequence Read(string sequence)
        {
            return sequence.Contains("[")
                ? mLcmsSpectatorSequenceParser.Read(sequence)
                : mMsgfPlusSequenceParser.Read(sequence);
        }
    }
}
