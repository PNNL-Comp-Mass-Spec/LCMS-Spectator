// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProteinId.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class is a container for PRSMs that have the same protein identified.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.MassSpecData;

namespace LcmsSpectator.Models
{
    /// <summary>
    /// This class is a container for PRSMs that have the same protein identified.
    /// </summary>
    public class ProteinId : IIdData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProteinId"/> class.
        /// </summary>
        /// <param name="sequence">The unmodified complete protein sequence.</param>
        /// <param name="proteinName">The protein name.</param>
        public ProteinId(Sequence sequence, string proteinName)
        {
            Sequence = sequence;
            ProteinName = proteinName;
            Proteoforms = new Dictionary<string, ProteoformId>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProteinId"/> class.
        /// Constructor for creating a ProteinId from a FASTA Entry.
        /// </summary>
        /// <param name="fastaEntry">The FASTA Entry.</param>
        public ProteinId(FastaEntry fastaEntry)
        {
            Proteoforms = new Dictionary<string, ProteoformId>();
            Sequence = new Sequence(fastaEntry.ProteinSequenceText, new AminoAcidSet());
            ProteinName = fastaEntry.ProteinName;
            ProteinDescription = fastaEntry.ProteinDescription;
        }

        /// <summary>
        /// Gets the unmodified complete protein sequence.
        /// </summary>
        public Sequence Sequence { get; }

        /// <summary>
        /// Gets the protein name.
        /// </summary>
        public string ProteinName { get; }

        /// <summary>
        /// Gets the description of the protein.
        /// </summary>
        public string ProteinDescription { get; }

        /// <summary>
        /// Gets a dictionary that maps a protein name to a ProteinID.
        /// </summary>
        public Dictionary<string, ProteoformId> Proteoforms { get; private set; }

        /// <summary>
        /// Add a Protein-Spectrum-Match identification.
        /// </summary>
        /// <param name="id">Protein-Spectrum-Match to add</param>
        public void Add(PrSm id)
        {
            if (!Proteoforms.ContainsKey(id.SequenceText))
            {
                // Even though we just checked if Proteoforms has this key,
                // multiple threads could be updating this list simultaneously,
                // so we could still encounter duplicate key errors

                // However, that likely indicates a programming error, so we will not include a try/catch block here

                Proteoforms.Add(id.SequenceText, new ProteoformId(id, Sequence));

                // try
                // {
                //     Proteoforms.Add(id.SequenceText, new ProteoformId(id, Sequence));
                // }
                // catch (ArgumentException)
                // {
                //     // Ignore this error
                // }
            }

            var proteoform = Proteoforms[id.SequenceText];
            proteoform.Add(id);
        }

        /// <summary>
        /// Remove a Protein-Spectrum-Match identification.
        /// </summary>
        /// <param name="id">Protein-Spectrum-Match to remove.</param>
        public void Remove(PrSm id)
        {
            if (Proteoforms.ContainsKey(id.SequenceText))
            {
                Proteoforms[id.SequenceText].Remove(id);
            }
        }

        public void ClearIds()
        {
            Proteoforms.Clear();
        }

        /// <summary>
        /// Get the PRSM in the tree with the highest score.
        /// </summary>
        /// <returns>The PRSM with the highest score.</returns>
        public PrSm GetHighestScoringPrSm()
        {
            PrSm highest = null;
            foreach (var proteoform in Proteoforms.Values)
            {
                var highestProtein = proteoform.GetHighestScoringPrSm();
                if (highest == null || highestProtein.CompareTo(highest) >= 0)
                {
                    highest = highestProtein;
                }
            }

            return highest;
        }

        /// <summary>
        /// Set the LCMSRun for all data.
        /// </summary>
        /// <param name="lcms">LCMSRun to set.</param>
        /// <param name="dataSetName">Name of the data this for the LCMSRun.</param>
        public void SetLcmsRun(ILcMsRun lcms, string dataSetName)
        {
            foreach (var proteoform in Proteoforms.Values)
            {
                proteoform.SetLcmsRun(lcms, dataSetName);
            }
        }

        /// <summary>
        /// Determines whether the item contains a given identification.
        /// </summary>
        /// <param name="id">the ID to search for.</param>
        /// <returns>A value indicating whether the item contains the identification.</returns>
        public bool Contains(PrSm id)
        {
            return Proteoforms.Values.Any(proteoform => proteoform.Contains(id));
        }

        /// <summary>
        /// Remove all PRSMs that are part of a certain data set.
        /// </summary>
        /// <param name="rawFileName">Name of the data set.</param>
        public void RemovePrSmsFromRawFile(string rawFileName)
        {
            var newProteoforms = new Dictionary<string, ProteoformId>();
            foreach (var proteoform in Proteoforms)
            {
                proteoform.Value.RemovePrSmsFromRawFile(rawFileName);
                if (proteoform.Value.ChargeStates.Count > 0)
                {
                    newProteoforms.Add(proteoform.Key, proteoform.Value);
                }
            }

            Proteoforms = newProteoforms;
        }

        /// <summary>
        /// Comparison class for comparing ProteinID by protein name.
        /// </summary>
        public class ProteinIdNameDescComparer : IComparer<ProteinId>
        {
            /// <summary>
            /// Compare two ProteinIDs by protein name.
            /// </summary>
            /// <param name="x">Left protein.</param>
            /// <param name="y">Right protein.</param>
            /// <returns>
            /// Integer value indicating whether the left protein is
            /// less than, equal to, or greater than right protein.
            /// </returns>
            public int Compare(ProteinId x, ProteinId y)
            {
                if (x == null)
                    return -1;

                if (y == null)
                    return 1;

                return string.CompareOrdinal(x.ProteinName, y.ProteinName);
            }
        }
    }
}
