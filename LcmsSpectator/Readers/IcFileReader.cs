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
using System.Linq;
using InformedProteomics.Backend.Data.Sequence;
using LcmsSpectator.Models;
using LcmsSpectator.Readers.SequenceReaders;

namespace LcmsSpectator.Readers
{
    /// <summary>
    /// Reader for MSPathFinder results file.
    /// </summary>
    public class IcFileReader : BaseTsvReader
    {

        private readonly bool doNotReadQValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="IcFileReader"/> class.
        /// </summary>
        /// <param name="filePath">The path to the TSV file.</param>
        public IcFileReader(string filePath) : base(filePath)
        {
            // Dataset_IcTda.tsv files has column QValue
            // Files _IcTarget.tsv and _IcDecoy.tsv do not have that column

            doNotReadQValue = filePath.ToLower().Contains("_ictarget") || filePath.ToLower().Contains("_icdecoy");
        }

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
                ////var modifiedAA = new ModifiedAminoAcid(aa, mod.Item2);
                ////sequence[pos] = modifiedAA;
            }

            return sequenceText;

            ////return new Tuple<Sequence, string>(new Sequence(sequence), sequenceText);
        }

        /// <summary>
        /// Create Protein-Spectrum-Matches identification from a line of the results file.
        /// </summary>
        /// <param name="line">Single line of the results file.</param>
        /// <param name="headers">Headers of the TSV file.</param>
        /// <param name="modIgnoreList">Ignores modifications contained in this list.</param>
        /// <returns>List of Protein-Spectrum-Match identifications.</returns>
        protected override IList<PrSm> CreatePrSms(string line, IReadOnlyDictionary<string, int> headers, IEnumerable<string> modIgnoreList)
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
                var sequenceData = SetModifications(parts[headers["Sequence"]], parts[headers["Modifications"]]);
                var prsm = new PrSm(sequenceReader)
                {
                    Heavy = false,
                    Scan = Convert.ToInt32(parts[headers["Scan"]]),
                    Charge = Convert.ToInt32(parts[headers["Charge"]]),
                    // Skip: Sequence = sequenceData.Item1,
                    SequenceText = sequenceData,
                    ProteinName = protein,
                    ProteinDesc = parts[headers["ProteinDesc"]].Split(';').FirstOrDefault(),
                    Score = Math.Round(score, 3),
                };
                if (!doNotReadQValue)
                {
                    prsm.QValue = Math.Round(Convert.ToDouble(parts[headers["QValue"]]), 4);
                }
                prsmList.Add(prsm);
            }

            return prsmList;
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
