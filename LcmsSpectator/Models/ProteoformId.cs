﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProteoformId.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class is a container for PRSMs that have the same proteoform.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.MassSpecData;

namespace LcmsSpectator.Models
{
    /// <summary>
    /// This class is a container for PRSMs that have the same proteoform.
    /// </summary>
    public class ProteoformId : IIdData
    {
        // Ignore Spelling: proteoform

        /// <summary>
        /// Initializes a new instance of the <see cref="ProteoformId"/> class.
        /// </summary>
        /// <param name="sequence">The proteoform sequence text</param>
        /// <param name="sequenceText">The string representation of the sequence.</param>
        /// <param name="modLocations">Location string of modifications in the sequence.</param>
        /// <param name="proteinSequence">The protein sequence.</param>
        public ProteoformId(Sequence sequence, string sequenceText, string modLocations, Sequence proteinSequence)
        {
            Sequence = sequence;
            SequenceText = sequenceText;
            PreSequence = string.Empty;
            PostSequence = string.Empty;
            ChargeStates = new Dictionary<int, ChargeStateId>();
            GenerateAnnotation(sequence, proteinSequence, modLocations);
            FormatSequences();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProteoformId"/> class.
        /// This creates the proteoform from a PRSM object.
        /// </summary>
        /// <param name="prsm">The PRSM to generate the ProteoformID from.</param>
        /// <param name="proteinSequence">The sequence of the protein that this Proteoform is associated with.</param>
        public ProteoformId(PrSm prsm, Sequence proteinSequence)
        {
            ProteinName = prsm.ProteinName;
            ProteinDesc = prsm.ProteinDesc;
            Mass = prsm.Mass;
            Sequence = prsm.Sequence;
            SequenceText = prsm.SequenceText;
            PreSequence = string.Empty;
            PostSequence = string.Empty;
            ChargeStates = new Dictionary<int, ChargeStateId>();
            GenerateAnnotation(prsm.Sequence, proteinSequence, prsm.ModificationLocations);
            FormatSequences();
        }

        /// <summary>
        /// The name of the protein.
        /// </summary>
        public string ProteinName { get; }

        /// <summary>
        /// The description of the protein.
        /// </summary>
        public string ProteinDesc { get; }

        /// <summary>
        /// The mass of the proteoform sequence.
        /// </summary>
        public double Mass { get; }

        /// <summary>
        /// The sequence before the proteoform.
        /// </summary>
        public string PreSequence { get; private set; }

        /// <summary>
        /// The proteoform sequence.
        /// </summary>
        public Sequence Sequence { get; }

        /// <summary>
        /// Gets the proteoform sequence
        /// </summary>
        public string SequenceText { get; private set; }

        /// <summary>
        /// Gets the annotation of the proteoform in the protein sequence.
        /// </summary>
        public string Annotation { get; private set; }

        /// <summary>
        /// The sequence after the proteoform.
        /// </summary>
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
            if (!ChargeStates.ContainsKey(id.Charge))
            {
                ChargeStates.Add(id.Charge, new ChargeStateId(id));
            }

            var chargeState = ChargeStates[id.Charge];
            chargeState.Add(id);
        }

        /// <summary>
        /// Remove a Protein-Spectrum-Match identification.
        /// </summary>
        /// <param name="id">Protein-Spectrum-Match to remove.</param>
        public void Remove(PrSm id)
        {
            if (ChargeStates.ContainsKey(id.Charge))
            {
                ChargeStates[id.Charge].Remove(id);
            }
        }

        /// <summary>
        /// Set the LCMSRun for all data.
        /// </summary>
        /// <param name="lcms">LCMSRun to set.</param>
        /// <param name="dataSetName">Name of the data this for the LCMSRun.</param>
        public void SetLcmsRun(ILcMsRun lcms, string dataSetName)
        {
            foreach (var chargeState in ChargeStates.Values)
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
            return ChargeStates.Values.Any(chargeState => chargeState.Contains(id));
        }

        /// <summary>
        /// Get the PRSM in the tree with the highest score.
        /// </summary>
        /// <returns>The PRSM with the highest score.</returns>
        public PrSm GetHighestScoringPrSm()
        {
            PrSm highest = null;
            foreach (var chargeState in ChargeStates.Values)
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
            foreach (var chargeState in ChargeStates)
            {
                chargeState.Value.RemovePrSmsFromRawFile(rawFileName);
                if (chargeState.Value.PrSms.Count > 0)
                {
                    newChargeStates.Add(chargeState.Key, chargeState.Value);
                }
            }

            ChargeStates = newChargeStates;
        }

        /// <summary>
        /// Generate the annotation of a proteoform sequence as a substring of a protein sequence.
        /// </summary>
        /// <param name="sequence">The proteoform sequence.</param>
        /// <param name="proteinSequence">The protein sequence.</param>
        /// <param name="modLocations">The locations of modifications in the proteoform sequence.</param>
        private void GenerateAnnotation(Sequence sequence, Sequence proteinSequence, string modLocations)
        {
            var cleanSequenceStr = sequence.Aggregate(string.Empty, (current, aa) => current + aa.Residue);
            var proteinSequenceStr = proteinSequence.Aggregate(string.Empty, (current, aa) => current + aa.Residue);

            var prevResidueIndex = -1;
            var nextResidueIndex = sequence.Count;

            var index = proteinSequenceStr.IndexOf(cleanSequenceStr, StringComparison.Ordinal);

            if (index >= 0)
            {
                if (index > 0)
                {
                    prevResidueIndex = index - 1;
                }

                if (index < proteinSequenceStr.Length - 1)
                {
                    nextResidueIndex = index + cleanSequenceStr.Length;
                }
            }

            var labelBuilder = new StringBuilder();
            if (prevResidueIndex >= 0)
            {
                labelBuilder.AppendFormat("{0}{1}.", proteinSequenceStr[prevResidueIndex], prevResidueIndex);
                PreSequence = proteinSequenceStr.Substring(0, prevResidueIndex + 1);
            }

            labelBuilder.AppendFormat("({0})", modLocations.Length == 0 ? (cleanSequenceStr.Length < 30 ? cleanSequenceStr : "...") : modLocations);

            if (nextResidueIndex < proteinSequenceStr.Length)
            {
                labelBuilder.AppendFormat(".{0}{1}", proteinSequenceStr[nextResidueIndex], nextResidueIndex);
                PostSequence = proteinSequenceStr.Substring(nextResidueIndex);
            }

            Annotation = labelBuilder.ToString();
        }

        /// <summary>
        /// Format the sequences so they have line breaks every [lineLength] characters.
        /// Does not break in the middle of modifications.
        /// </summary>
        /// <param name="lineLength">The target length of a line.</param>
        private void FormatSequences(int lineLength = 60)
        {
            var pos = 0;
            PreSequence = FormatSequenceText(PreSequence, ref pos, lineLength);
            SequenceText = FormatSequenceText(SequenceText, ref pos, lineLength);
            PostSequence = FormatSequenceText(PostSequence, ref pos, lineLength);
        }

        /// <summary>Insert line breaks into sequence every [lineLength] characters.</summary>
        /// <param name="sequenceText">The sequence to format.</param>
        /// <param name="pos">The current position.</param>
        /// <param name="lineLength">Frequency to insert spaces at.</param>
        /// <returns>The formatted sequence.</returns>
        private string FormatSequenceText(string sequenceText, ref int pos, int lineLength)
        {
            var seqBuilder = new StringBuilder();
            var inMod = false;
            foreach (var c in sequenceText)
            {
                if (c == '[')
                {   // start of modification
                    inMod = true;
                }

                if (c == ']')
                {   // end of modification
                    inMod = false;
                }

                seqBuilder.Append(c);

                if (inMod)
                {
                    continue;
                }

                if (pos > 0 && pos % lineLength == 0)
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
                if (x == null)
                    return -1;

                if (y == null)
                    return 1;

                return string.CompareOrdinal(x.SequenceText, y.SequenceText);
            }
        }
    }
}
