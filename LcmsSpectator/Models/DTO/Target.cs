// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Target.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Class for target sequence and charge state.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using InformedProteomics.Backend.Data.Sequence;
using LcmsSpectator.Readers.SequenceReaders;

namespace LcmsSpectator.Models.DTO
{
    /// <summary>
    /// Class for target sequence and charge state.
    /// </summary>
    public class Target
    {
        /// <summary>
        /// The target sequence text.
        /// </summary>
        private string sequenceText;

        /// <summary>
        /// Initializes a new instance of the <see cref="Target"/> class.
        /// </summary>
        /// <param name="sequence">The sequence.</param>
        /// <param name="charge">The charge state.</param>
        public Target(string sequence, int charge = 0)
        {
            this.SequenceText = sequence;
            this.Charge = charge;
        }

        /// <summary>
        /// Gets the parsed target sequence.
        /// </summary>
        public Sequence Sequence { get; private set; }

        /// <summary>
        /// Gets or sets the charge state.
        /// </summary>
        public int Charge { get; set; }

        /// <summary>
        /// Gets or sets the target sequence text.
        /// Sequence text is parsed into an InformedProteomics Sequence object when set.
        /// </summary>
        public string SequenceText
        {
            get
            {
                return this.sequenceText;
            }

            set
            {
                var seqReader = new SequenceReader();
                var seq = Sequence;
                try
                {
                    this.Sequence = seqReader.Read(value);
                    this.sequenceText = value;
                }
                catch (Exception)
                {
                    Sequence = seq;
                }
            }
        }
    }
}
