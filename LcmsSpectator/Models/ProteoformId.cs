// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProteoformId.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class is a container for PRSMs that have the same proteoform.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using InformedProteomics.Backend.Data.Sequence;
    using InformedProteomics.Backend.MassFeature;
    using InformedProteomics.Backend.MassSpecData;

    /// <summary>
    /// This class is a container for PRSMs that have the same proteoform.
    /// </summary>
    public class ProteoformId : IIdData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProteoformId"/> class.
        /// </summary>
        /// <param name="sequence">The proteoform's sequence text.</param>
        /// <param name="sequenceText">The string representation of the sequence.</param>
        /// <param name="modLocations">Location string of modifications in the sequence.</param>
        /// <param name="proteinSequence">The protein sequence.</param>
        public ProteoformId(Sequence sequence, string sequenceText, string modLocations, Sequence proteinSequence)
        {
            this.Sequence = sequence;
            this.SequenceText = sequenceText;
            this.PreSequence = string.Empty;
            this.PostSequence = string.Empty;
            this.ChargeStates = new Dictionary<int, ChargeStateId>();
            this.GenerateAnnotation(sequence, proteinSequence, modLocations);
            this.FormatSequences();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProteoformId"/> class.
        /// This creates the proteform from a PRSM object.
        /// </summary>
        /// <param name="prsm">The PRSM to generate the ProteoformID from.</param>
        /// <param name="proteinSequence">The sequence of the protein that this Proteoform is associated with.</param>
        public ProteoformId(PrSm prsm, Sequence proteinSequence)
        {
            this.Sequence = prsm.Sequence;
            this.SequenceText = prsm.SequenceText;
            this.PreSequence = string.Empty;
            this.PostSequence = string.Empty;
            this.ChargeStates = new Dictionary<int, ChargeStateId>();
            this.GenerateAnnotation(prsm.Sequence, proteinSequence, prsm.ModificationLocations);
            this.FormatSequences();
        }

        public string PreSequence { get; private set; }

        public Sequence Sequence { get; private set; }

        /// <summary>
        /// Gets the proteoform's sequence text.
        /// </summary>
        public string SequenceText { get; private set; }

        /// <summary>
        /// Gets the annotation of the proteoform in the protein sequence.
        /// </summary>
        public string Annotation { get; private set; }

        public string PostSequence { get; private set; }

        /// <summary>
        /// Gets a dictionary that maps a charge state to a ChargeStateID.
        /// </summary>
        public Dictionary<int, ChargeStateId> ChargeStates { get; private set; }

        /// <summary>
        /// Add a Protein-Spectrum-Match identification.
        /// </summary>
        /// <param name="id">Protein-Spectrum-Math to add</param>
        public void Add(PrSm id)
        {
            if (!this.ChargeStates.ContainsKey(id.Charge))
            {
                this.ChargeStates.Add(id.Charge, new ChargeStateId(id.Charge));
            }

            var chargeState = this.ChargeStates[id.Charge];
            chargeState.Add(id);
        }

        /// <summary>
        /// Remove a Protein-Spectrum-Match identification.
        /// </summary>
        /// <param name="id">Protein-Spectrum-Match to remove.</param>
        public void Remove(PrSm id)
        {
            if (this.ChargeStates.ContainsKey(id.Charge))
            {
                this.ChargeStates[id.Charge].Remove(id);
            }
        }

        /// <summary>
        /// Set the LCMSRun for all data.
        /// </summary>
        /// <param name="lcms">LCMSRun to set.</param>
        /// <param name="dataSetName">Name of the data this for the LCMSRun.</param>
        public void SetLcmsRun(ILcMsRun lcms, string dataSetName)
        {
            foreach (var chargeState in this.ChargeStates.Values)
            {
                chargeState.SetLcmsRun(lcms, dataSetName);
            }
        }

        /// <summary>
        /// Determines whether the item contains a given identification.
        /// </summary>
        /// <param name="id">the ID to search for.</param>
        /// <returns>A value indicating whether the item contains the identification.</returns>
        public bool Contains(PrSm id)
        {
            return this.ChargeStates.Values.Any(chargeState => chargeState.Contains(id));
        }

        /// <summary>
        /// Get the PRSM in the tree with the highest score.
        /// </summary>
        /// <returns>The PRSM with the highest score.</returns>
        public PrSm GetHighestScoringPrSm()
        {
            PrSm highest = null;
            foreach (var chargeState in this.ChargeStates.Values)
            {
                var chargeStateHighest = chargeState.GetHighestScoringPrSm();
                if (highest == null || chargeStateHighest.CompareTo(highest) >= 0)
                {
                    highest = chargeStateHighest;
                }
            }

            return highest;
        }

        /// <summary>
        /// Remove all PRSMs that are part of a certain data set.
        /// </summary>
        /// <param name="rawFileName">Name of the data set.</param>
        public void RemovePrSmsFromRawFile(string rawFileName)
        {
            var newChargeStates = new Dictionary<int, ChargeStateId>();
            foreach (var chargeState in this.ChargeStates)
            {
                chargeState.Value.RemovePrSmsFromRawFile(rawFileName);
                if (chargeState.Value.PrSms.Count > 0)
                {
                    newChargeStates.Add(chargeState.Key, chargeState.Value);
                }
            }

            this.ChargeStates = newChargeStates;
        }

        private void GenerateAnnotation(Sequence sequence, Sequence proteinSequence, string modLocations)
        {
            var cleanSequenceStr = sequence.Aggregate(string.Empty, (current, aa) => current + aa.Residue);
            var proteinSequenceStr = proteinSequence.Aggregate(string.Empty, (current, aa) => current + aa.Residue);

            int prevResidueIndex = -1;
            int nextResidueIndex = sequence.Count;

            var index = proteinSequenceStr.IndexOf(cleanSequenceStr, StringComparison.Ordinal);

            if (index >= 0)
            {
                if (index > 0)
                {
                    prevResidueIndex = index - 1;
                }

                if (index < proteinSequenceStr.Length - 1)
                {
                    nextResidueIndex = index + cleanSequenceStr.Length + 1;
                }
            }

            var labelBuilder = new StringBuilder();
            if (prevResidueIndex >= 0)
            {
                labelBuilder.AppendFormat("{0}{1}.", proteinSequenceStr[prevResidueIndex], prevResidueIndex);
                this.PreSequence = proteinSequenceStr.Substring(0, prevResidueIndex + 1);
            }

            labelBuilder.AppendFormat("({0})", modLocations.Length == 0 ? (cleanSequenceStr.Length < 30 ? cleanSequenceStr : "...") : modLocations);

            if (nextResidueIndex < proteinSequenceStr.Length)
            {
                labelBuilder.AppendFormat(".{0}{1}", proteinSequenceStr[nextResidueIndex], nextResidueIndex);
                this.PreSequence = proteinSequenceStr.Substring(nextResidueIndex);
            }

            this.Annotation = labelBuilder.ToString();
        }

        private void FormatSequences()
        {
            int pos = 0;
            const int MaxLineLength = 60;

            this.PreSequence = this.FormatSequenceText(this.PreSequence, ref pos, MaxLineLength);
            this.SequenceText = this.FormatSequenceText(this.SequenceText, ref pos, MaxLineLength);
            this.PostSequence = this.FormatSequenceText(this.PostSequence, ref pos, MaxLineLength);
        }

        private string FormatSequenceText(string sequenceText, ref int pos, int maxLineLength)
        {
            var seqBuilder = new StringBuilder();
            bool inMod = false;
            foreach (var a in sequenceText)
            {
                if (a == '[')
                {
                    inMod = true;
                }

                if (a == ']')
                {
                    inMod = false;
                }

                seqBuilder.Append(a);

                if (inMod)
                {
                    continue;
                }

                if (pos > 0 && pos % maxLineLength == 0)
                {
                    seqBuilder.Append('\n');
                }

                pos++;
            }

            return seqBuilder.ToString();
        }

        /// <summary>
        /// Comparison class for comparing ProteoformID by sequence text.
        /// </summary>
        internal class SequenceComparer : IComparer<ProteoformId>
        {
            /// <summary>
            /// Compare two ProteinIDs by protein name.
            /// </summary>
            /// <param name="x">Left proteoform.</param>
            /// <param name="y">Right proteoform.</param>
            /// <returns>
            /// Integer value indicating whether the left proteoform sequence is 
            /// less than, equal to, or greater than right proteoform sequence.
            /// </returns>
            public int Compare(ProteoformId x, ProteoformId y)
            {
                return string.Compare(x.SequenceText, y.SequenceText, StringComparison.Ordinal);
            }
        }
    }
}
