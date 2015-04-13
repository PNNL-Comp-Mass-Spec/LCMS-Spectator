// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceReader.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Reader for protein/peptide sequences in the LCMSSpectator style or MS-GF+ style.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Readers.SequenceReaders
{
    using InformedProteomics.Backend.Data.Sequence;

    /// <summary>
    /// Reader for protein/peptide sequences in the LCMSSpectator style or MS-GF+ style.
    /// </summary>
    public class SequenceReader : ISequenceReader
    {
        /// <summary>
        /// Parse a protein/peptide sequence in the LCMSSpectator style or MS-GF+ style.
        /// </summary>
        /// <param name="sequence">The sequence as a string..</param>
        /// <param name="trim">A value indicating whether the n-terminal and c-terminal amino acids should be trimmed.</param>
        /// <returns>The parsed sequence.</returns>
        public Sequence Read(string sequence, bool trim = false)
        {
            ISequenceReader reader;
            if (!sequence.Contains("["))
            {
                reader = new MsgfPlusSequenceReader();
            }
            else
            {
                reader = new LcmsSpectatorSequenceReader();
            }

            return reader.Read(sequence, trim);
        }
    }
}
