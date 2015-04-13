// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MtdbReader.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Reader for MTDB file.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Readers
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using InformedProteomics.Backend.Data.Composition;
    using InformedProteomics.Backend.Data.Sequence;
    using LcmsSpectator.Config;
    using LcmsSpectator.Models;
    using MTDBFramework;
    using MTDBFramework.Database;

    /// <summary>
    /// Reader for MTDB file.
    /// </summary>
    public class MtdbReader : IIdFileReader
    {
        /// <summary>
        /// The path to the database file.
        /// </summary>
        private readonly string filePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="MtdbReader"/> class.
        /// </summary>
        /// <param name="filePath">The path to the database file.</param>
        public MtdbReader(string filePath)
        {
            this.filePath = filePath;
        }

        /// <summary>
        /// Read a MTDB Creator results file.
        /// </summary>
        /// <param name="modIgnoreList">Ignores modifications contained in this list.</param>
        /// <returns>Identification tree of MTDB Creator identifications.</returns>
        public IdentificationTree Read(IEnumerable<string> modIgnoreList = null)
        {
            return this.ReadAsync().Result;
        }

        /// <summary>
        /// Read a MTDB Creator results file asynchronously.
        /// </summary>
        /// <param name="modIgnoreList">Ignores modifications contained in this list.</param>
        /// <returns>Identification tree of MZID identifications.</returns>
        public async Task<IdentificationTree> ReadAsync(IEnumerable<string> modIgnoreList = null)
        {
            IdentificationTree tree = new IdentificationTree();

            if (!File.Exists(this.filePath))
            {
                return tree;
            }

            TargetDatabase database = await Task.Run(() => MtdbCreator.LoadDB(this.filePath));

            foreach (var target in database.ConsensusTargets)
            {
                foreach (var id in target.Evidences)
                {
                    // Degan: attempting to make it recognize the proteins from .mtdb format
                    foreach (var prot in id.Proteins)
                    {
                        string strippedSequence = target.Sequence;
                        strippedSequence = strippedSequence.Remove(0, 2);
                        strippedSequence = strippedSequence.Remove(strippedSequence.Length - 2, 2);
                        PrSm entry = new PrSm { Sequence = new Sequence(strippedSequence, new AminoAcidSet()) };

                        string rawSequence = target.Sequence;
                        int offset = target.Sequence.IndexOf('.');
                        int length = target.Sequence.Length;

                        foreach (var ptm in id.Ptms)
                        {
                            var position = rawSequence.Length - (length - (ptm.Location + offset));
                            string symbol = string.Empty;

                            // We only need to add the sign on positive values - the '-' is automatic on negative values
                            if (ptm.Mass >= 0)
                            {
                                symbol = "+";
                            }

                            rawSequence = rawSequence.Insert(position + 1, symbol + ptm.Mass);

                            Composition modComposition = Composition.ParseFromPlainString(ptm.Formula);
                            Modification mod = IcParameters.Instance.RegisterModification(ptm.Name, modComposition);
                            entry.Sequence[ptm.Location - 1] = new ModifiedAminoAcid(entry.Sequence[ptm.Location - 1], mod);
                        }

                        // Degan: attempting to make it recognize the proteins from .mtdb format
                        entry.ProteinName = prot.ProteinName;

                        // Degan: choosing stripped sequence rather than the raw sequence to exclude prior and successive amino acid
                        entry.SequenceText = strippedSequence;
                        entry.Charge = id.Charge;
                        entry.Scan = id.Scan;
                        tree.Add(entry);
                    }
                }
            }

            return tree;
        }
    }
}
