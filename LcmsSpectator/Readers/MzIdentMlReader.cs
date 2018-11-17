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
using LcmsSpectator.Models;
using LcmsSpectator.Readers.SequenceReaders;
using PSI_Interface.IdentData;

namespace LcmsSpectator.Readers
{
    /// <summary>
    /// Reader for MZID files.
    /// </summary>
    public class MzIdentMlReader : IIdFileReader
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
            Modifications = new List<Modification>();
        }

        /// <summary>
        /// Read a MZID results file.
        /// </summary>
        /// <param name="modIgnoreList">Ignores modifications contained in this list.</param>
        /// <param name="progress">The progress reporter.</param>
        /// <returns>Identification tree of identifications.</returns>
        public IEnumerable<PrSm> Read(IEnumerable<string> modIgnoreList = null, IProgress<double> progress = null)
        {
            return ReadAsync(modIgnoreList).Result;
        }

        /// <summary>
        /// Read a MZID results file asynchronously.
        /// </summary>
        /// <param name="modIgnoreList">Ignores modifications contained in this list.</param>
        /// <param name="progress">The progress reporter.</param>
        /// <returns>Identification tree of MZID identifications.</returns>
        public async Task<IEnumerable<PrSm>> ReadAsync(IEnumerable<string> modIgnoreList = null, IProgress<double> progress = null)
        {
            var dataset = await Task.Run(() => mzIdentMlReader.Read(filePath));
            var prsms = new List<PrSm>();
            var sequenceReader = new SequenceReader();

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

                    var prsm = new PrSm(sequenceReader)
                    {
                        Heavy = false,
                        Scan = evidence.ScanNum,
                        Charge = evidence.Charge,
                        Sequence = sequence,
                        SequenceText = sequenceStr,
                        ProteinName = pepEv.DbSeq.Accession,
                        ProteinDesc = pepEv.DbSeq.ProteinDescription,
                        Score = evidence.SpecEv,
                        UseGolfScoring = true,
                        QValue = evidence.QValue,
                    };
                    prsms.Add(prsm);
                }
            }

            return prsms;
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
                if (aa is ModifiedAminoAcid)
                {
                    var modAa = aa as ModifiedAminoAcid;
                    sequenceStr.AppendFormat("[{0}]", modAa.Modification.Name);
                }
            }

            return sequenceStr.ToString();
        }

        public IList<Modification> Modifications { get; }
    }
}
