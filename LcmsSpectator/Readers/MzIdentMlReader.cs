// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MzIdentMlReader.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Reader for MZID files.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.SearchResults;
using LcmsSpectator.Config;
using LcmsSpectator.Models;
using LcmsSpectator.Readers.SequenceReaders;
using PSI_Interface.IdentData;

namespace LcmsSpectator.Readers
{
    /// <summary>
    /// Reader for MZID files.
    /// </summary>
    public class MzIdentMlReader : BaseReader
    {
        /// <summary>
        /// Path for MZID file.
        /// </summary>
        private readonly string filePath;

        /// <summary>
        /// MTDB Creator MZID reader.
        /// </summary>
        private readonly SimpleMZIdentMLReader mzIdentMlReader;

        /// <summary>
        /// Initializes a new instance of the <see cref="MzIdentMlReader"/> class.
        /// </summary>
        /// <param name="filePath">The path for the MZID file.</param>
        public MzIdentMlReader(string filePath)
        {
            this.filePath = filePath;
            mzIdentMlReader = new SimpleMZIdentMLReader();
        }

        /// <summary>
        /// Read a MZID results file asynchronously.
        /// </summary>
        /// <param name="scanStart">Optional filter to apply when reading from the peptide ID file</param>
        /// <param name="scanEnd">Optional filter to apply when reading from the peptide ID file</param>
        /// <param name="modIgnoreList">Ignores modifications contained in this list.</param>
        /// <param name="progress">The progress reporter.</param>
        /// <returns>Identification tree of MZID identifications.</returns>
        protected override async Task<IEnumerable<PrSm>> ReadFile(
            int scanStart,
            int scanEnd,
            IReadOnlyCollection<string> modIgnoreList = null,
            IProgress<double> progress = null)
        {
            var dataset = await Task.Run(() => mzIdentMlReader.Read(filePath));
            var prsmList = new List<PrSm>();
            var sequenceReader = new SequenceReader();

            foreach (var mod in dataset.SearchModifications)
            {
                if (mod.Name.Equals("unknown modification", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (Modification.Get(mod.Name) == null)
                {
                    // Add the modification to the list, with an unspecified accession number
                    var modData = new Modification(0, mod.Mass, mod.Name);
                    Modification.Register(modData);
                    // Could use IcParameters.Instance.RegisterModification, but we want to support whatever the name in the file is, regardless of if the mass is a duplicate.
                    IcParameters.Instance.RegisteredModifications.Add(modData);
                }
            }

            foreach (var evidence in dataset.Identifications)
            {
                var sequence = evidence.Peptide.GetIpSequence();

                var sequenceStr = GetSequenceStr(sequence);

                foreach (var pepEv in evidence.PepEvidence)
                {
                    if (pepEv.IsDecoy || pepEv.DbSeq.Accession.StartsWith("XXX"))
                    {
                        continue;
                    }

                    if (scanStart > 0 && (evidence.ScanNum < scanStart || evidence.ScanNum > scanEnd))
                    {
                        continue;
                    }

                    var prsm = new PrSm(sequenceReader)
                    {
                        Heavy = false,
                        Scan = evidence.ScanNum,
                        NativeId = evidence.NativeId,
                        Charge = evidence.Charge,
                        Sequence = sequence,
                        SequenceText = sequenceStr,
                        ProteinName = pepEv.DbSeq.Accession,
                        ProteinDesc = pepEv.DbSeq.ProteinDescription,
                        Score = evidence.SpecEv,
                        UseGolfScoring = true,
                        QValue = evidence.QValue,
                    };
                    prsmList.Add(prsm);
                }
            }

            return prsmList;
        }

        /// <summary>
        /// Get the sequence as a string.
        /// </summary>
        /// <param name="sequence">The sequence to convert</param>
        /// <returns></returns>
        private string GetSequenceStr(Sequence sequence)
        {
            var sequenceStr = new StringBuilder();
            foreach (var aa in sequence)
            {
                sequenceStr.Append(aa.Residue);
                if (aa is ModifiedAminoAcid modifiedAA)
                {
                    sequenceStr.AppendFormat("[{0}]", modifiedAA.Modification.Name);
                }
            }

            return sequenceStr.ToString();
        }
    }
}
