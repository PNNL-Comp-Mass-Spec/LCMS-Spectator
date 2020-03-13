using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using LcmsSpectator.Models;

namespace LcmsSpectator.Readers
{
    /// <summary>
    /// Base class for reading tab-separated value files (.tsv or .txt)
    /// </summary>
    public abstract class BaseTsvReader : BaseReader
    {

        /// <summary>
        /// The path to the TSV file.
        /// </summary>
        protected readonly string filePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="LcmsSpectator.Readers.BaseTsvReader"/> class.
        /// </summary>
        /// <param name="filePath">The path to the TSV file.</param>
        protected BaseTsvReader(string filePath)
        {
            this.filePath = filePath;
        }

        protected abstract IList<PrSm> CreatePrSms(string line, IReadOnlyDictionary<string, int> headers, IEnumerable<string> modIgnoreList);

        /// <summary>
        /// Read a results file.
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
        /// Read results from a TSV file.
        /// </summary>
        /// <param name="stream">The stream for an open TSV file.</param>
        /// <param name="scanStart">Optional filter to apply when reading from the peptide ID file</param>
        /// <param name="scanEnd">Optional filter to apply when reading from the peptide ID file</param>
        /// <param name="modIgnoreList">Ignores modifications contained in this list. </param>
        /// <param name="fileSizeBytes">Size of the source file, in bytes</param>
        /// <param name="progress">Progress</param>
        /// <returns>The Protein-Spectrum-Match identifications.</returns>
        protected async Task<IEnumerable<PrSm>> ReadTsv(
            StreamReader stream,
            int scanStart,
            int scanEnd,
            IReadOnlyCollection<string> modIgnoreList,
            long fileSizeBytes,
            IProgress<double> progress)
        {
            var prsmList = new List<PrSm>();
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
                StoreIDs(prsmList, idData, scanStart, scanEnd);

                progress.Report(bytesRead / (double)fileSizeBytes * 100);
            }

            stream.Close();
            return prsmList;
        }

        /// <summary>
        /// Read from a TSV file inside a zip file.
        /// </summary>
        /// <param name="scanStart">Optional filter to apply when reading from the peptide ID file</param>
        /// <param name="scanEnd">Optional filter to apply when reading from the peptide ID file</param>
        /// <param name="modIgnoreList">Ignores modifications contained in this list.</param>
        /// <param name="progress">The progress reporter.</param>
        /// <returns>Task that creates an identification tree of MSPathFinder identifications.</returns>
        protected async Task<IEnumerable<PrSm>> ReadZip(
            int scanStart,
            int scanEnd,
            IReadOnlyCollection<string> modIgnoreList,
            IProgress<double> progress)
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
                    return await ReadTsv(fileStream, scanStart, scanEnd, modIgnoreList, entry.Length, progress);
                }
            }

            return new List<PrSm>();
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
    }

}
