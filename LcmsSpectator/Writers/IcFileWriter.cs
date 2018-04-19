// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IcFileWriter.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Writer for MSPathFinder TSV result format.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Writers
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using InformedProteomics.Backend.Data.Sequence;
    using Models;

    /// <summary>
    /// Writer for MSPathFinder TSV result format.
    /// </summary>
    public class IcFileWriter : IIdWriter
    {
        /// <summary>
        /// The file path.
        /// </summary>
        private readonly string filePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="IcFileWriter"/> class.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        public IcFileWriter(string filePath)
        {
            this.filePath = filePath;
        }

        /// <summary>Write IDs to file.</summary>
        /// <param name="ids">The IDs to write to a file.</param>
        public void Write(IEnumerable<PrSm> ids)
        {
            using (var writer = new StreamWriter(filePath))
            {
                // Print headers
                writer.WriteLine(
                                 "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}",
                                 "Scan",
                                 "Sequence",
                                 "Modifications",
                                 "Composition",
                                 "ProteinName",
                                 "ProteinDesc",
                                 "Charge",
                                 "MostAbundantIsotopeMz",
                                 "Mass",
                                 "#MatchedFragments",
                                 "QValue");

                // Print IDs
                foreach (var prsm in ids)
                {
                    if (prsm.Sequence.Count == 0)
                    {
                        continue;
                    }

                    var sequenceStringBuilder = new StringBuilder(prsm.Sequence.Count);
                    var modStringBuilder = new StringBuilder();
                    for (var i = 0; i < prsm.Sequence.Count; i++)
                    {
                        sequenceStringBuilder.Append(prsm.Sequence[i].Residue);
                        if (prsm.Sequence[i] is ModifiedAminoAcid modAa)
                        {
                            if (modStringBuilder.Length > 0)
                            {
                                modStringBuilder.Append(",");
                            }

                            modStringBuilder.AppendFormat("{0} {1}", modAa.Modification.Name, i);
                        }
                    }

                    writer.WriteLine(
                        "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}",
                        prsm.Scan,
                        sequenceStringBuilder,
                        modStringBuilder,
                        prsm.Sequence.Composition,
                        prsm.ProteinName,
                        prsm.ProteinDesc,
                        prsm.Charge,
                        prsm.PrecursorMz,
                        prsm.Mass,
                        prsm.Score,
                        prsm.QValue);
                }
            }
        }
    }
}
