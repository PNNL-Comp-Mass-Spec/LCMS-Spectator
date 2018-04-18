// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IcFileReader.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Reader for MSPathFinder results file.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Readers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading.Tasks;
    using InformedProteomics.Backend.Data.Sequence;
    using Models;
    using SequenceReaders;

    /// <summary>
    /// Reader for MSPathFinder results file.
    /// </summary>
    public class IcFileReader : IIdFileReader
    {
        /// <summary>
        /// The path to the TSV file.
        /// </summary>
        private readonly string filePath;

        private readonly bool doNotReadQValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="IcFileReader"/> class.
        /// </summary>
        /// <param name="filePath">The path to the TSV file.</param>
        public IcFileReader(string filePath)
        {
            this.filePath = filePath;
            doNotReadQValue = filePath.ToLower().Contains("_ictarget") || filePath.ToLower().Contains("_icdecoy");
            Modifications = new List<Modification>();
        }

        /// <summary>
        /// Read a MSPathFinder results file.
        /// </summary>
        /// <param name="modIgnoreList">Ignores modifications contained in this list.</param>
        /// <param name="progress">The progress reporter.</param>
        /// <returns><returns>The Protein-Spectrum-Match identifications.</returns></returns>
        public IEnumerable<PrSm> Read(IEnumerable<string> modIgnoreList = null, IProgress<double> progress = null)
        {
            var modIgnore = modIgnoreList == null ? new List<string>() : new List<string>(modIgnoreList);
            return ReadFile(modIgnore, progress).Result;
        }

        /// <summary>
        /// Read a MSPathFinder results file asynchronously.
        /// </summary>
        /// <param name="modIgnoreList">Ignores modifications contained in this list.</param>
        /// <param name="progress">The progress reporter.</param>
        /// <returns>Identification tree of MSPathFinder identifications.</returns>
        public async Task<IEnumerable<PrSm>> ReadAsync(IEnumerable<string> modIgnoreList = null, IProgress<double> progress = null)
        {
            var modIgnore = modIgnoreList == null ? new List<string>() : new List<string>(modIgnoreList);
            return await ReadFile(modIgnore, progress);
        }

        public IList<Modification> Modifications { get; }

        /// <summary>
        /// Create a sequence object with modifications.
        /// </summary>
        /// <param name="cleanSequence">Clean sequence string with no modifications.</param>
        /// <param name="modifications">Modifications (name and position) to insert into the sequence.</param>
        /// <returns>Tuple containing sequence object and sequence text.</returns>
        public string SetModifications(string cleanSequence, string modifications)
        {
            // Build Sequence AminoAcid list
            ////var sequence = new Sequence(cleanSequence, new AminoAcidSet());
            var sequenceText = cleanSequence;
            var parsedModifications = ParseModifications(modifications);

            // Add modifications to sequence
            parsedModifications.Sort(new CompareModByHighestPosition());   // sort in reverse order for insertion
            foreach (var mod in parsedModifications)
            {
                var pos = mod.Item1;
                if (pos > 0)
                {
                    pos--;
                }

                var modLabel = string.Format("[{0}]", mod.Item2.Name);
                sequenceText = sequenceText.Insert(mod.Item1, modLabel);
                ////var aa = sequence[pos];
                ////var modaa = new ModifiedAminoAcid(aa, mod.Item2);
                ////sequence[pos] = modaa;
            }

            return sequenceText;

            ////return new Tuple<Sequence, string>(new Sequence(sequence), sequenceText);
        }

        /// <summary>
        /// Read a MSPathFinder results file.
        /// </summary>
        /// <param name="modIgnoreList">Ignores modifications contained in this list.</param>
        /// <param name="progress">The progress reporter.</param>
        /// <returns>The Protein-Spectrum-Match identifications.</returns>
        private async Task<IEnumerable<PrSm>> ReadFile(List<string> modIgnoreList, IProgress<double> progress)
        {
            progress = progress ?? new Progress<double>();
            var ext = Path.GetExtension(filePath);
            IEnumerable<PrSm> prsms;
            if (string.Equals(ext, ".tsv", StringComparison.OrdinalIgnoreCase))
            {
                var fileInfo = new FileInfo(filePath);
                var reader = new StreamReader(File.Open(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                prsms = await ReadTsv(reader, modIgnoreList, fileInfo.Length, progress);
                return prsms;
            }

            if (string.Equals(ext, ".zip", StringComparison.OrdinalIgnoreCase))
            {
                prsms = await ReadZip(modIgnoreList, progress);
                return prsms;
            }

            throw new ArgumentException(string.Format("Cannot read file with extension \"{0}\"", ext));


        }

        /// <summary>
        /// Read a MSPathFinder results from TSV file.
        /// </summary>
        /// <param name="stream">The stream for an open TSV file.</param>
        /// <param name="modIgnoreList">Ignores modifications contained in this list. </param>
        /// <param name="fileSizeBytes">Size of the source file, in bytes</param>
        /// <param name="progress">Progress</param>
        /// <returns>The Protein-Spectrum-Match identifications.</returns>
        private async Task<IEnumerable<PrSm>> ReadTsv(StreamReader stream, List<string> modIgnoreList, long fileSizeBytes, IProgress<double> progress)
        {

            var prsms = new List<PrSm>();
            var headers = new Dictionary<string, int>();
            var lineCount = 0;
            long bytesRead = 0;

            while (!stream.EndOfStream)
            {
                var line = await stream.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                lineCount++;
                bytesRead += line.Length + 2;

                if (lineCount == 1)
                {
                    // first line
                    var parts = line.Split('\t');
                    for (var i = 0; i < parts.Length; i++)
                    {
                        headers.Add(parts[i], i);
                    }

                    continue;
                }

                var idData = CreatePrSms(line, headers, modIgnoreList);
                if (idData != null)
                {
                    prsms.AddRange(idData);
                }

                progress.Report(bytesRead / (double)fileSizeBytes * 100);
            }

            stream.Close();
            return prsms;
        }

        /// <summary>
        /// Read a MSPathFinder results from GZipped TSV file.
        /// </summary>
        /// <param name="modIgnoreList">Ignores modifications contained in this list.</param>
        /// <param name="progress">The progress reporter.</param>
        /// <returns>Task that creates an identification tree of MSPathFinder identifications.</returns>
        private async Task<IEnumerable<PrSm>> ReadZip(List<string> modIgnoreList, IProgress<double> progress)
        {
            var zipFilePath = filePath;
            var fileName = Path.GetFileNameWithoutExtension(zipFilePath);
            if (fileName != null && fileName.EndsWith("_IcTsv"))
            {
                fileName = fileName.Substring(0, fileName.Length - 6);
            }

            var tsvFileName = string.Format("{0}_IcTda.tsv", fileName);

            var zipArchive = ZipFile.OpenRead(zipFilePath);
            var entry = zipArchive.GetEntry(tsvFileName);
            if (entry != null)
            {
                using (var fileStream = new StreamReader(entry.Open()))
                {
                    return await ReadTsv(fileStream, modIgnoreList, entry.Length, progress);
                }
            }

            return new List<PrSm>();
        }

        /// <summary>
        /// Create Protein-Spectrum-Matches identification from a line of the results file.
        /// </summary>
        /// <param name="line">Single line of the results file.</param>
        /// <param name="headers">Headers of the TSV file.</param>
        /// <param name="modIgnoreList">Ignores modifications contained in this list.</param>
        /// <returns>List of Protein-Spectrum-Match identifications.</returns>
        private IEnumerable<PrSm> CreatePrSms(string line, IReadOnlyDictionary<string, int> headers, IEnumerable<string> modIgnoreList)
        {
            var expectedHeaders = new List<string>
            {
                "#MatchedFragments",
                "ProteinName",
                "Modifications",
                "Sequence",
                "Scan",
                "Charge",
                "ProteinDesc",
            };
            if (!doNotReadQValue)
            {
                expectedHeaders.Add("QValue");
            }

            foreach (var header in expectedHeaders.Where(header => !headers.ContainsKey(header)))
            {
                throw new KeyNotFoundException(string.Format("Missing expected column header \"{0}\" in ID file.", header));
            }

            var parts = line.Split('\t');
            var scoreLabel = "IcScore";
            if (!headers.ContainsKey(scoreLabel))
            {
                scoreLabel = "#MatchedFragments";
            }

            var score = Convert.ToDouble(parts[headers[scoreLabel]]);

            var proteinNames = parts[headers["ProteinName"]].Split(';');
            var prsms = new List<PrSm> { Capacity = proteinNames.Length };

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
                var sequenceData = SetModifications(parts[headers["Sequence"]], parts[headers["Modifications"]]);
                var prsm = new PrSm(sequenceReader)
                {
                    Heavy = false,
                    Scan = Convert.ToInt32(parts[headers["Scan"]]),
                    Charge = Convert.ToInt32(parts[headers["Charge"]]),
                    ////Sequence = sequenceData.Item1,
                    SequenceText = sequenceData,
                    ProteinName = protein,
                    ProteinDesc = parts[headers["ProteinDesc"]].Split(';').FirstOrDefault(),
                    Score = Math.Round(score, 3),
                };
                if (!doNotReadQValue)
                {
                    prsm.QValue = Math.Round(Convert.ToDouble(parts[headers["QValue"]]), 4);
                }
                prsms.Add(prsm);
            }

            return prsms;
        }

        /// <summary>
        /// Parse a modification string containing the name of modifications and their positions.
        /// </summary>
        /// <param name="modifications">Comma-separated list of modification names and their positions.</param>
        /// <returns>List of parsed modifications and their positions.</returns>
        private List<Tuple<int, Modification>> ParseModifications(string modifications)
        {
            var mods = modifications.Split(',');
            var parsedMods = new List<Tuple<int, Modification>>();
            if (mods.Length < 1 || mods[0] == string.Empty)
            {
                return parsedMods;
            }

            foreach (var modParts in mods.Select(mod => mod.Split(' ')))
            {
                if (modParts.Length < 0)
                {
                    throw new FormatException("Unknown Modification");
                }

                var modName = modParts[0];
                var modPos = Convert.ToInt32(modParts[1]);
                var modification = Modification.Get(modName);
                parsedMods.Add(new Tuple<int, Modification>(modPos, modification));
                if (modification == null)
                {
                    throw new InvalidModificationNameException(string.Format("Found an unrecognized modification: {0}", modName), modName);
                }
            }

            return parsedMods;
        }

        /// <summary>
        /// Exception thrown for unknown or modifications.
        /// </summary>
        public class InvalidModificationNameException : Exception
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="InvalidModificationNameException"/> class.
            /// </summary>
            /// <param name="message">Exception message.</param>
            /// <param name="modificationName">The name of the invalid modification.</param>
            public InvalidModificationNameException(string message, string modificationName)
                : base(message)
            {
                ModificationName = modificationName;
            }

            /// <summary>
            /// Gets the name of the invalid modification.
            /// </summary>
            public string ModificationName { get; }
        }

        /// <summary>
        /// Comparer for two modifications that compares by their position in a sequence.
        /// </summary>
        internal class CompareModByHighestPosition : IComparer<Tuple<int, Modification>>
        {
            /// <summary>
            /// Comparer two modifications by their position in a sequence.
            /// </summary>
            /// <param name="x">The left modification.</param>
            /// <param name="y">The right modification.</param>
            /// <returns>
            /// A value indicating whether the left modification has a lower, equal, or higher
            /// position than the right modification.</returns>
            public int Compare(Tuple<int, Modification> x, Tuple<int, Modification> y)
            {
                if (x == null)
                    return -1;

                if (y == null)
                    return 1;

                return y.Item1.CompareTo(x.Item1);
            }
        }
    }
}
