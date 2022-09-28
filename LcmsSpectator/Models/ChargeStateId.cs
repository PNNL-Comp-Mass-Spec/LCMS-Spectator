// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChargeStateId.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class is a container for PRSMs that have the same charge state.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.MassSpecData;

namespace LcmsSpectator.Models
{
    /// <summary>
    /// This class is a container for PRSMs that have the same charge state.
    /// </summary>
    public class ChargeStateId : IIdData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChargeStateId"/> class.
        /// </summary>
        /// <param name="charge">The charge state.</param>
        public ChargeStateId(int charge)
        {
            Charge = charge;
            PrSms = new Dictionary<Tuple<int, string>, PrSm>();
        }

        public ChargeStateId(PrSm prsm)
        {
            Charge = prsm.Charge;
            ProteinName = prsm.ProteinName;
            ProteinDesc = prsm.ProteinDesc;
            Mass = prsm.Mass;
            Mz = prsm.PrecursorMz;
            PrSms = new Dictionary<Tuple<int, string>, PrSm>();
        }

        public string ProteinName { get; }

        public string ProteinDesc { get; }

        public double Mass { get; }

        public double Mz { get; }

        /// <summary>
        /// Gets the charge for this charge state.
        /// </summary>
        public int Charge { get; }

        /// <summary>
        /// Gets a dictionary mapping scan number and data set name to a PRSM.
        /// </summary>
        public Dictionary<Tuple<int, string>, PrSm> PrSms { get; private set; }

        /// <summary>
        /// Add a Protein-Spectrum-Match identification.
        /// </summary>
        /// <param name="id">Protein-Spectrum-Math to add</param>
        public void Add(PrSm id)
        {
            var key = new Tuple<int, string>(id.Scan, id.RawFileName);
            if (!PrSms.ContainsKey(key))
            {
                PrSms.Add(key, id);
            }
            else
            {
                PrSms[key] = id;
            }
        }

        /// <summary>
        /// Remove a Protein-Spectrum-Match identification.
        /// </summary>
        /// <param name="id">Protein-Spectrum-Match to remove.</param>
        public void Remove(PrSm id)
        {
            var key = new Tuple<int, string>(id.Scan, id.RawFileName);
            if (PrSms.ContainsKey(key))
            {
                PrSms.Remove(key);
            }
        }

        /// <summary>
        /// Get the PRSM in the tree with the highest score.
        /// </summary>
        /// <returns>The PRSM with the highest score.</returns>
        public PrSm GetHighestScoringPrSm()
        {
            PrSm highest = null;
            foreach (var prsm in PrSms.Values)
            {
                if (!prsm.Score.Equals(double.NaN) && (highest == null || prsm.CompareTo(highest) >= 0))
                {
                    highest = prsm;
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
            foreach (var prsm in PrSms.Values)
            {
                prsm.RawFileName = dataSetName;
                prsm.LcMs = lcms;
            }
        }

        /// <summary>
        /// Determines whether the item contains a given identification.
        /// </summary>
        /// <param name="id">the ID to search for.</param>
        /// <returns>A value indicating whether the item contains the identification.</returns>
        public bool Contains(PrSm id)
        {
            var key = new Tuple<int, string>(id.Scan, id.RawFileName);
            return PrSms.ContainsKey(key);
        }

        /// <summary>
        /// Remove all PRSMs that are part of a certain data set.
        /// </summary>
        /// <param name="rawFileName">Name of the data set.</param>
        public void RemovePrSmsFromRawFile(string rawFileName)
        {
            PrSms = PrSms.Where(prsm => prsm.Value.RawFileName != rawFileName).ToDictionary(prsm => prsm.Key, prsm => prsm.Value);
        }

        /// <summary>
        /// Comparison class for comparing ChargeStateID by charge state.
        /// </summary>
        public class ChargeStateComparer : IComparer<ChargeStateId>
        {
            /// <summary>
            /// Compare two ChargeStateIDs by charge.
            /// </summary>
            /// <param name="x">Left charge state.</param>
            /// <param name="y">Right charge state.</param>
            /// <returns>
            /// Integer value indicating whether the left charge state is
            /// less than, equal to, or greater than right charge state.
            /// </returns>
            public int Compare(ChargeStateId x, ChargeStateId y)
            {
                if (x == null)
                    return -1;

                if (y == null)
                    return 1;

                return x.Charge.CompareTo(y.Charge);
            }
        }
    }
}
