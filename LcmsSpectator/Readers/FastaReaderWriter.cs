namespace LcmsSpectator.Readers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using LcmsSpectator.Models;

    public class FastaReaderWriter
    {
        /// <summary>
        /// Read FASTA db file.
        /// </summary>
        /// <param name="filePath">The path to the FASTA DB file.</param>
        /// <returns>Enumerable of entries in the file.</returns>
        public static IEnumerable<FastaEntry> ReadFastaFile(string filePath)
        {
            FastaEntry fastaEntry = null;
            List<FastaEntry> entries = new List<FastaEntry>();
            foreach (var line in File.ReadLines(filePath))
            {
                if (line.Length == 0)
                {
                    continue;
                }

                if (line[0] == '>')
                {
                    if (fastaEntry != null)
                    {
                        entries.Add(fastaEntry);
                    }

                    fastaEntry = new FastaEntry();

                    var parts = line.Split(' ');
                    if (parts[0].Length == 1)
                    {
                        throw new FormatException("Invalid Fasta file format.");
                    }

                    fastaEntry.ProteinName = parts[0].Substring(1, parts[0].Length - 1);
                    if (parts.Length > 1)
                    {
                        fastaEntry.ProteinDescription = parts[1];
                    }
                }
                else
                {
                    fastaEntry.ProteinSequenceText += line;
                }
            }

            if (fastaEntry != null)
            {
                entries.Add(fastaEntry);
            }

            return entries;
        }

        /// <summary>
        /// Write FASTA entries to file.
        /// </summary>
        /// <param name="entries">The entries to write.</param>
        /// <param name="filePath">The path of the file to write the entries to.</param>
        public static void Write(IEnumerable<FastaEntry> entries, string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);

            if (directory == null)
            {
                throw new ArgumentException("Invalid file path.");
            }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var writer = new StreamWriter(filePath))
            {
                foreach (var entry in entries.Where(entry => entry != null))
                {
                    writer.Write(entry.FormattedEntry);   
                }
            }
        }
    }
}
