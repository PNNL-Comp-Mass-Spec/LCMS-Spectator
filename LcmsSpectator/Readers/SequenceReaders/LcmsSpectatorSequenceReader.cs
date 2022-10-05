// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LcmsSpectatorSequenceReader.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Reader for protein/peptide sequences in the LCMSSpectator style.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using InformedProteomics.Backend.Data.Sequence;

namespace LcmsSpectator.Readers.SequenceReaders
{
    /// <summary>
    /// Reader for protein/peptide sequences in the LCMSSpectator style.
    /// </summary>
    public class LcmsSpectatorSequenceReader : ISequenceReader
    {
        // Ignore Spelling: Carbamidomethyl

        const string AminoAcidRegEx = "[" + AminoAcid.StandardAminoAcidCharacters + "]";

        /// <summary>
        /// Regular expression for matching modification names inside square brackets
        /// </summary>
        /// <comments>
        /// <para>
        /// Option 1: explicitly look for letters, numbers, and symbols using "\[[A-Z0-9a-z_:()>+-]+\]"</para>
        /// <para>
        /// <para>
        /// Option 2: just match all text between two square brackets using   "\[[^\]]+\]"
        /// </para>
        /// Example modification names:
        /// [Oxidation]
        /// [Carbamidomethyl]
        /// [Label:13C(6)15N(4)]
        /// [Glu->pyro-Glu+Methyl]
        /// </para>
        /// </comments>
        const string ModRegEx = @"\[[^\]]+\]";

        /// <summary>
        /// Standard amino acid set.
        /// </summary>
        private readonly AminoAcidSet mAminoAcidSet;

        /// <summary>
        /// A value indicating whether the n-terminal and c-terminal amino acids should be trimmed.
        /// </summary>
        private readonly bool trimAnnotations;

        /// <summary>
        /// Initializes a new instance of the <see cref="LcmsSpectatorSequenceReader"/> class.
        /// </summary>
        /// <param name="trimAnnotations">
        /// A value indicating whether the n-terminal and c-terminal amino acids should be trimmed.
        /// </param>
        public LcmsSpectatorSequenceReader(bool trimAnnotations = false)
        {
            this.trimAnnotations = trimAnnotations;
            mAminoAcidSet = new AminoAcidSet();
        }

        /// <summary>
        /// RegEx matcher for finding amino acid symbols (capital letters) and modification names surrounded by square brackets
        /// </summary>
        private readonly Regex mResidueAndModMatcher = new Regex("(" + AminoAcidRegEx + "|" + ModRegEx + ")", RegexOptions.Compiled);

        /// <summary>
        /// Parse a protein/peptide sequence in the LCMSSpectator style.
        /// </summary>
        /// <param name="sequence">The sequence as a string.</param>
        /// <returns>The parsed sequence.</returns>
        public Sequence Read(string sequence)
        {
            if (trimAnnotations)
            {
                var firstIndex = sequence.IndexOf('.');
                if (firstIndex >= 0)
                {
                    var index = Math.Min(firstIndex + 1, sequence.Length - 1);
                    sequence = sequence.Substring(index, sequence.Length - index - 1);
                }

                var lastIndex = sequence.LastIndexOf('.');
                if (lastIndex >= 0)
                {
                    var index = Math.Min(lastIndex, sequence.Length - 1);
                    sequence = sequence.Substring(0, index);
                }
            }

            if (string.IsNullOrEmpty(sequence))
            {
                return new Sequence(new List<AminoAcid>());
            }

            var matches = mResidueAndModMatcher.Matches(sequence);

            if (matches.Count == 0)
            {
                return null;
            }

            var aminoAcidList = new List<AminoAcid>();

            AminoAcid aa = null;
            var mods = new List<Modification>();
            foreach (Match match in matches)
            {
                var element = match.Value;
                if (element.Length == 0)
                {
                    continue;
                }

                if (element.Length == 1 && char.IsLetter(element[0]))
                { // amino acid
                    if (aa != null)
                    {
                        aa = mods.Aggregate(aa, (current, mod) => new ModifiedAminoAcid(current, mod));
                        aminoAcidList.Add(aa);
                        mods = new List<Modification>();
                    }

                    aa = mAminoAcidSet.GetAminoAcid(element[0]);
                    if (aa == null)
                    {
                        throw new FormatException("Unrecognized amino acid character: " + element[0]);
                    }
                    ////                    Console.WriteLine("{0} {1} {2}", aa.Residue, aa.Composition, aa.GetMass());
                }
                else
                {
                    var modName = element.Substring(1, element.Length - 2);
                    var mod = Modification.Get(modName);
                    if (mod == null)
                    {
                        throw new FormatException("Unrecognized modification: " + modName);
                    }

                    mods.Add(mod);
                    ////                    Console.WriteLine("{0} {1} {2}", mod.Name, mod.Composition, mod.Composition.AveragineMass);
                }
            }

            if (aa != null)
            {
                aa = mods.Aggregate(aa, (current, mod) => new ModifiedAminoAcid(current, mod));
                aminoAcidList.Add(aa);
            }

            return new Sequence(aminoAcidList);
        }
    }
}
