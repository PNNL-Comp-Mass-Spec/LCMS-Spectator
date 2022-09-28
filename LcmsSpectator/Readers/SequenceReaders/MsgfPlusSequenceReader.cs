// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MsgfPlusSequenceReader.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Reader for protein/peptide sequences in the MS-GF+ style.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using InformedProteomics.Backend.Data.Sequence;
using LcmsSpectator.Config;

namespace LcmsSpectator.Readers.SequenceReaders
{
    /// <summary>
    /// Reader for protein/peptide sequences in the MS-GF+ style.
    /// </summary>
    public class MsgfPlusSequenceReader : ISequenceReader
    {
        /// <summary>
        /// Standard amino acid set.
        /// </summary>
        private static readonly AminoAcidSet AminoAcidSet;

        /// <summary>
        /// A value indicating whether the n-terminal and c-terminal amino acids should be trimmed.
        /// </summary>
        private readonly bool trimAnnotations;

        /// <summary>
        /// Initializes static members of the <see cref="MsgfPlusSequenceReader"/> class.
        /// </summary>
        static MsgfPlusSequenceReader()
        {
            AminoAcidSet = new AminoAcidSet();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MsgfPlusSequenceReader"/> class.
        /// </summary>
        /// <param name="trimAnnotations">
        /// A value indicating whether the n-terminal and c-terminal amino acids should be trimmed.
        /// </param>
        public MsgfPlusSequenceReader(bool trimAnnotations = false)
        {
            this.trimAnnotations = trimAnnotations;
        }

        /// <summary>
        /// Parse a protein/peptide sequence in the MS-GF+ style.
        /// </summary>
        /// <param name="msgfPlusPeptideStr">The sequence as a string..</param>
        /// <returns>The parsed sequence.</returns>
        public Sequence Read(string msgfPlusPeptideStr)
        {
            if (trimAnnotations)
            {
                var firstIndex = msgfPlusPeptideStr.IndexOf('.');
                if (firstIndex >= 0)
                {
                    var index = Math.Min(firstIndex + 1, msgfPlusPeptideStr.Length - 1);
                    msgfPlusPeptideStr = msgfPlusPeptideStr.Substring(index, msgfPlusPeptideStr.Length - index - 1);
                }

                var lastIndex = msgfPlusPeptideStr.LastIndexOf('.');
                if (lastIndex >= 0)
                {
                    var index = Math.Min(lastIndex, msgfPlusPeptideStr.Length - 1);
                    msgfPlusPeptideStr = msgfPlusPeptideStr.Substring(0, index);
                }
            }

            const string AminoAcidRegex = "[" + AminoAcid.StandardAminoAcidCharacters + "]";
            const string MassRegex = @"[+-]?\d+\.\d+";

            if (string.IsNullOrEmpty(msgfPlusPeptideStr))
            {
                return new Sequence(new List<AminoAcid>());
            }

            if (!Regex.IsMatch(msgfPlusPeptideStr, "(" + AminoAcidRegex + "|" + MassRegex + ")+"))
            {
                return null;
            }

            var stdAaSet = AminoAcidSet;
            var aminoAcidList = new List<AminoAcid>();

            var matches = Regex.Matches(msgfPlusPeptideStr, "(" + AminoAcidRegex + "|" + MassRegex + ")");
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

                    aa = stdAaSet.GetAminoAcid(element[0]);
                    if (aa == null)
                    {
                        throw new Exception("Unrecognized amino acid character: " + element[0]);
                    }
                    ////                    Console.WriteLine("{0} {1} {2}", aa.Residue, aa.Composition, aa.GetMass());
                }
                else
                {
                    double dblMass;
                    string mass;
                    try
                    {
                        dblMass = Math.Round(Convert.ToDouble(element), 3);
                        mass = dblMass.ToString(CultureInfo.InvariantCulture);
                    }
                    catch (FormatException)
                    {
                        throw new IcFileReader.InvalidModificationNameException(string.Format("Found an unrecognized modification {0}", element), element);
                    }
                    catch (Exception)
                    {
                        throw new IcFileReader.InvalidModificationNameException(string.Format("Found an unrecognized modification {0}", element), element);
                    }

                    var modList = Modification.GetFromMass(mass);
                    if (modList == null || modList.Count == 1)
                    {
                        var regMod = IcParameters.Instance.RegisterModification(element, dblMass);
                        modList = new List<Modification> { regMod };
                    }

                    var mod = modList[0];
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
