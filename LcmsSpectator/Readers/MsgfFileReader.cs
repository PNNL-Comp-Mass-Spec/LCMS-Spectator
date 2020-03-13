// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MsgfFileReader.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Reader for MS-GF+ results file.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LcmsSpectator.Models;
using LcmsSpectator.Readers.SequenceReaders;

namespace LcmsSpectator.Readers
{
    /// <summary>
    /// Reader for MS-GF+ results file.
    /// </summary>
    public class MsgfFileReader : BaseReader
    {
        /// <summary>
        /// The path to the TSV file.
        /// </summary>
        private readonly string filePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="MsgfFileReader"/> class.
        /// </summary>
        /// <param name="filePath">The path to the TSV file.</param>
        public MsgfFileReader(string filePath)
        {
            this.filePath = filePath;
        }

        /// <summary>
        /// Read a MS-GF+ results from TSV file.
        /// </summary>
        /// <param name="scanStart">Optional filter to apply when reading from the peptide ID file</param>
        /// <param name="scanEnd">Optional filter to apply when reading from the peptide ID file</param>
        /// <param name="modIgnoreList">Ignores modifications contained in this list.</param>
        /// <param name="progress">The progress reporter.</param>
        /// <returns>The Protein-Spectrum-Match identifications.</returns>
        protected override async Task<IEnumerable<PrSm>> ReadFile(
            int scanStart,
            int scanEnd,
            IReadOnlyCollection<string> modIgnoreList = null,
            IProgress<double> progress = null)
        {
            var prsms = new List<PrSm>();
            var file = new StreamReader(File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            var headers = new Dictionary<string, int>();
            var lineCount = 0;

            while (!file.EndOfStream)
            {
                var line = await file.ReadLineAsync();
                lineCount++;
                if (lineCount == 1 && line != null)
                { // first line
                    var parts = line.Split('\t');
                    for (var i = 0; i < parts.Length; i++)
                    {
                        headers.Add(parts[i], i);
                    }

                    continue;
                }

                var idData = CreatePrSms(line, headers);
                prsms.AddRange(idData);
            }

            return prsms;
        }

        /// <summary>
        /// Create Protein-Spectrum-Matches identification from a line of the results file.
        /// </summary>
        /// <param name="line">Single line of the results file.</param>
        /// <param name="headers">Headers of the TSV file.</param>
        /// <param name="modIgnoreList">Ignores modifications contained in this list.</param>
        /// <returns>List of Protein-Spectrum-Match identifications.</returns>
        private IEnumerable<PrSm> CreatePrSms(string line, IReadOnlyDictionary<string, int> headers, IReadOnlyCollection<string> modIgnoreList = null)
        {
            var expectedHeaders = new List<string>
            {
                "Protein",
                "Peptide",
                "Charge",
                "QValue",
            };

            foreach (var header in expectedHeaders.Where(header => !headers.ContainsKey(header)))
            {
                throw new KeyNotFoundException(string.Format("Missing expected column header \"{0}\" in ID file.", header));
            }

            var parts = line.Split('\t');

            double score = 0;
            if (headers.TryGetValue("SpecEValue", out var scoreIndex))
            {
                score = Convert.ToDouble(parts[scoreIndex]);
            }
            else if (headers.TryGetValue("MSGFDB_SpecEValue", out scoreIndex))
            {
                score = Convert.ToDouble(parts[scoreIndex]);
            }

            var scan = 0;
            if (headers.TryGetValue("ScanNum", out var scanIndex))
            {
                scan = Convert.ToInt32(parts[scanIndex]);
            }
            else if (headers.TryGetValue("Scan", out scanIndex))
            {
                scan = Convert.ToInt32(parts[scanIndex]);
            }

            var proteinNames = parts[headers["Protein"]].Split(';');
            var prsms = new List<PrSm> { Capacity = proteinNames.Length };

            var sequenceText = parts[headers["Peptide"]];

            if (modIgnoreList != null)
            {
                if (modIgnoreList.Any(sequenceText.Contains))
                {
                    return null;
                }
            }

            var trimAnnotations = filePath.Contains("_syn");
            var sequenceReader = new SequenceReader(trimAnnotations);

            foreach (var protein in proteinNames)
            {
                var qValue = ParseDouble(parts[headers["QValue"]], 4);
                var charge = int.Parse(parts[headers["Charge"]]);

                var prsm = new PrSm(sequenceReader)
                {
                    Heavy = false,
                    Scan = scan,
                    Charge = charge,
                    ////Sequence = sequenceReader.Read(sequenceText, trim),
                    SequenceText = sequenceText,
                    ProteinName = protein,
                    ProteinDesc = string.Empty,
                    Score = score,
                    UseGolfScoring = true,
                    QValue = qValue
                };
                prsms.Add(prsm);
            }

            return prsms;
        }
    }
}
