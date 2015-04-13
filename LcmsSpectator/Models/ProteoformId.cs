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
    using InformedProteomics.Backend.MassSpecData;

    /// <summary>
    /// This class is a container for PRSMs that have the same proteoform.
    /// </summary>
    public class ProteoformId : IIdData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProteoformId"/> class.
        /// </summary>
        /// <param name="sequenceText">The proteoform's sequence text.</param>
        public ProteoformId(string sequenceText)
        {
            this.SequenceText = sequenceText;
            this.ChargeStates = new Dictionary<int, ChargeStateId>();
        }

        /// <summary>
        /// Gets the proteoform's sequence text.
        /// </summary>
        public string SequenceText { get; private set; }

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
