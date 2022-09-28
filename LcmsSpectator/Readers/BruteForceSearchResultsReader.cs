// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IcFileReader.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Reader for MSPathFinder results file.
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
    /// Reader for generic results files, with the following tab-separated columns (order does not matter)
    /// Score   Protein   Description   Sequence   Scan
    /// </summary>
    public class BruteForceSearchResultsReader : BaseTsvReader
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LcmsSpectator.Readers.BruteForceSearchResultsReader"/> class.
        /// </summary>
        /// <param name="filePath">The path to the TSV file.</param>
        public BruteForceSearchResultsReader(string filePath) : base(filePath)
        {
        }

        /// <summary>
        /// Read a generic tab-separated results file with the following tab-separated columns (order does not matter)
        /// Score   Protein   Description   Sequence   Scan
        /// </summary>
        /// <param name="scanStart">Optional filter to apply when reading from the peptide ID file</param>
        /// <param name="scanEnd">Optional filter to apply when reading from the peptide ID file</param>
        /// <param name="modIgnoreList">Ignores modifications contained in this list.</param>
        /// <param name="progress">The progress reporter.</param>
        /// <returns>The Protein-Spectrum-Match identifications.</returns>
        protected override async Task<IEnumerable<PrSm>> ReadFile(int scanStart, int scanEnd, IReadOnlyCollection<string> modIgnoreList = null, IProgress<double> progress = null)
        {
            progress = progress ?? new Progress<double>();
            var ext = Path.GetExtension(filePath);

            IEnumerable<PrSm> prsmList;
            if (string.Equals(ext, ".tsv", StringComparison.OrdinalIgnoreCase))
            {
                var fileInfo = new FileInfo(filePath);
                var reader = new StreamReader(File.Open(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                prsmList = await ReadTsv(reader, scanStart, scanEnd, modIgnoreList, fileInfo.Length, progress);
                return prsmList;
            }

            if (string.Equals(ext, ".zip", StringComparison.OrdinalIgnoreCase))
            {
                prsmList = await ReadZip(scanStart, scanEnd, modIgnoreList, progress);
                return prsmList;
            }

            throw new ArgumentException(string.Format("Cannot read file with extension \"{0}\"", ext));
        }

        /// <summary>
        /// Create Protein-Spectrum-Matches identification from a line of the results file.
        /// </summary>
        /// <param name="line">Single line of the results file.</param>
        /// <param name="headers">Headers of the TSV file; keys are header names, values are the column index</param>
        /// <param name="modIgnoreList">Ignores modifications contained in this list.</param>
        /// <returns>List of Protein-Spectrum-Match identifications.</returns>
        protected override IList<PrSm> CreatePrSms(string line, IReadOnlyDictionary<string, int> headers, IEnumerable<string> modIgnoreList)
        {
            var expectedHeaders = new List<string>
            {
                "Score",
                "Protein",
                "Description",
                "Sequence",
                "Scan",
            };

            foreach (var header in expectedHeaders.Where(header => !headers.ContainsKey(header)))
            {
                throw new KeyNotFoundException(string.Format("Missing expected column header \"{0}\" in ID file.", header));
            }

            var parts = line.Split('\t');
            var scoreLabel = "Score";

            var score = Convert.ToDouble(parts[headers[scoreLabel]]);

            var proteinNames = parts[headers["Protein"]].Split(';');
            var prsmList = new List<PrSm> { Capacity = proteinNames.Length };

            if (modIgnoreList != null)
            {
                if (modIgnoreList.Select(mod => string.Format("{0} ", mod)).Any(searchMod => parts[headers["Modifications"]].Contains(searchMod)))
                {
                    return null;
                }
            }

            var sequenceReader = new SequenceReader();

            foreach (var protein in proteinNames)
            {
                var prsm = new PrSm(sequenceReader)
                {
                    Heavy = false,
                    Scan = Convert.ToInt32(parts[headers["Scan"]]),
                    Charge = 1,
                    // Skip: Sequence = sequenceData.Item1,
                    SequenceText = parts[headers["Sequence"]],
                    ProteinName = protein,
                    ProteinDesc = parts[headers["Description"]].Split(';').FirstOrDefault(),
                    Score = Math.Round(score, 3),
                };
                prsmList.Add(prsm);
            }

            return prsmList;
        }
    }
}