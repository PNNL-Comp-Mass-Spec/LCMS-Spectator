using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using InformedProteomics.Backend.Data.Sequence;

namespace LcmsSpectatorModels.Readers.SequenceReaders
{
    public class MsgfPlusSequenceReader: ISequenceReader
    {
        static MsgfPlusSequenceReader()
        {
            _aminoAcidSet = new AminoAcidSet();
        }

        public Sequence Read(string msgfPlusPeptideStr)
        {
            const string aminoAcidRegex = @"[" + AminoAcid.StandardAminoAcidCharacters + "]";
            const string massRegex = @"[+-]?\d+\.\d+";

            if (!Regex.IsMatch(msgfPlusPeptideStr, "(" + aminoAcidRegex + "|" + massRegex + ")+")) return null;

            var stdAaSet = _aminoAcidSet;
            var aaList = new List<AminoAcid>();

            var matches = Regex.Matches(msgfPlusPeptideStr, "(" + aminoAcidRegex + "|" + massRegex + ")");
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
                    if (aa == null) throw new Exception("Unrecognized amino acid character: " + element[0]);
                    //                    Console.WriteLine("{0} {1} {2}", aa.Residue, aa.Composition, aa.GetMass());
                }
                else
                {
                    double dblMass = 0.0;
                    string mass;
                    try
                    {
                        dblMass = Math.Round(Convert.ToDouble(element), 3);
                        mass = dblMass.ToString(CultureInfo.InvariantCulture);
                    }
                    catch (FormatException)
                    {
                        throw new InvalidModificationNameException(String.Format("Could not find modification {0}", element), element);
                    }
                    catch (Exception)
                    {
                        throw new InvalidModificationNameException(String.Format("Could not find modification {0}", element), element);
                    }
                    var modList = Modification.GetFromMass(mass);
                    if (modList == null || modList.Count == 1)
                    {
                        var regMod = Modification.RegisterAndGetModification(element, dblMass);
                        modList = new List<Modification> {regMod};
                    }
                    var mod = modList[0];
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

        private static AminoAcidSet _aminoAcidSet;
    }
}
