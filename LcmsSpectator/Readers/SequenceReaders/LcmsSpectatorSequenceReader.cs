using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using InformedProteomics.Backend.Data.Sequence;

namespace LcmsSpectator.Readers.SequenceReaders
{
    public class LcmsSpectatorSequenceReader: ISequenceReader
    {
        public Sequence Read(string sequence, bool trim=false)
        {
            if (trim)
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

            const string aminoAcidRegex = @"[" + AminoAcid.StandardAminoAcidCharacters + "]";
            //const string modRegex = @"\[([A-Z]|[a-z])+\]";
            const string modRegex = @"\[([A-Z]|[a-z]|[0-9])+\]";

            if (String.IsNullOrEmpty(sequence)) return new Sequence(new List<AminoAcid>());
            if (!Regex.IsMatch(sequence, "(" + aminoAcidRegex + "|" + modRegex + ")+")) return null;

            var stdAaSet = new AminoAcidSet();
            var aaList = new List<AminoAcid>();

            var matches = Regex.Matches(sequence, "(" + aminoAcidRegex + "|" + modRegex + ")");
            AminoAcid aa = null;
            var mods = new List<Modification>();
            foreach (Match match in matches)
            {
                var element = match.Value;
                if (element.Length == 0) continue;
                if (element.Length == 1 && char.IsLetter(element[0]))   // amino acid
                {
                    if (aa != null)
                    {
                        aa = mods.Aggregate(aa, (current, mod) => new ModifiedAminoAcid(current, mod));
                        aaList.Add(aa);
                        mods = new List<Modification>();
                    }
                    aa = stdAaSet.GetAminoAcid(element[0]);
                    if (aa == null) throw new FormatException("Unrecognized amino acid character: " + element[0]);
                    //                    Console.WriteLine("{0} {1} {2}", aa.Residue, aa.Composition, aa.GetMass());
                }
                else
                {
                    var modName = element.Substring(1, element.Length - 2);
                    var mod = Modification.Get(modName);
                    if (mod == null) throw new FormatException("Unrecognized modification: " + modName);
                    mods.Add(mod);
                    //                    Console.WriteLine("{0} {1} {2}", mod.Name, mod.Composition, mod.Composition.AveragineMass);
                }
            }

            if (aa != null)
            {
                aa = mods.Aggregate(aa, (current, mod) => new ModifiedAminoAcid(current, mod));
                aaList.Add(aa);
            }

            return new Sequence(aaList);
        }
    }
}
