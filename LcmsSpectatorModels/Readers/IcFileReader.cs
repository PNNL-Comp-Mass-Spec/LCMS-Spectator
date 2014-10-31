using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using InformedProteomics.Backend.Data.Sequence;
using LcmsSpectatorModels.Models;

namespace LcmsSpectatorModels.Readers
{
    public class IcFileReader: IIdFileReader
    {
        public IcFileReader(string tsvFile)
        {
            _tsvFile = tsvFile;
        }

        public IdentificationTree Read(IEnumerable<string> modIgnoreList = null)
        {
            var idTree = new IdentificationTree(ToolType.MsPathFinder);
            var file = new StreamReader(File.Open(_tsvFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            //var file = File.ReadLines(_tsvFile);
            var headers = new Dictionary<string, int>();
            var lineCount = 0;
            while(!file.EndOfStream)
            {
                var line = file.ReadLine();
                lineCount++;
                if (lineCount == 1 && line != null) // first line
                {
                    var parts = line.Split('\t');
                    for (int i = 0; i < parts.Length; i++)
                    {
                        headers.Add(parts[i], i);
                    }
                    continue;
                }
                var idData = CreatePrSms(line, headers, modIgnoreList);
                if (idData != null) idTree.Add(idData);
            }
            file.Close();

            return idTree;
        }

        private IEnumerable<PrSm> CreatePrSms(string line, Dictionary<string, int> headers, IEnumerable<string> modIgnoreList=null)
        {
            var parts = line.Split('\t');
            var scoreLabel = "IcScore";
            if (!headers.ContainsKey(scoreLabel)) scoreLabel = "#MatchedFragments";
            var score = Convert.ToDouble(parts[headers[scoreLabel]]);

            var proteinNames = parts[headers["ProteinName"]].Split(';');
            var prsms = new List<PrSm>{Capacity = proteinNames.Length};

            if (modIgnoreList != null)
            {
                foreach (var mod in modIgnoreList)
                {
                    var searchMod = String.Format("{0} ", mod);
                    if (parts[headers["Modifications"]].Contains(searchMod)) return null;
                }
            }

            foreach (var protein in proteinNames)
            {
                var sequenceData = SetModifications(parts[headers["Sequence"]], parts[headers["Modifications"]]);
                var prsm = new PrSm
                {
                    Heavy = false,
                    Scan = Convert.ToInt32(parts[headers["Scan"]]),
                    Sequence = sequenceData.Item1,
                    SequenceText = sequenceData.Item2,
                    ProteinName = protein,
                    ProteinDesc = parts[headers["ProteinDesc"]].Split(';').FirstOrDefault(),
                    Charge = Convert.ToInt32(parts[headers["Charge"]]),
                    Score = Math.Round(score, 3),
                    QValue = Math.Round(Convert.ToDouble(parts[headers["QValue"]]), 4),
                };
                prsms.Add(prsm);
            }
            return prsms;
        }

        public Tuple<Sequence, string> SetModifications(string cleanSequence, string modifications)
        {
            // Build Sequence AminoAcid list
            var sequence = new Sequence(cleanSequence, new AminoAcidSet());
            var sequenceText = cleanSequence;
            var parsedModifications = ParseModifications(modifications);

            // Add modifications to sequence
            parsedModifications.Sort(new CompareModByHighestPosition());   // sort in reverse order for insertion
            foreach (var mod in parsedModifications)
            {
                var pos = mod.Item1;
                if (pos > 0) pos--;
                var modLabel = String.Format("[{0}]", mod.Item2.Name);
                sequenceText = sequenceText.Insert(mod.Item1, modLabel);
                var aa = sequence[pos];
                var modaa = new ModifiedAminoAcid(aa, mod.Item2);
                sequence[pos] = modaa;
            }
            return new Tuple<Sequence, string>(sequence, sequenceText);
        }

        private List<Tuple<int, Modification>> ParseModifications(string modifications)
        {
            var mods = modifications.Split(',');
            var parsedMods = new List<Tuple<int, Modification>>();
            if (mods.Length < 1 || mods[0] == "") return parsedMods;
            foreach (var modParts in mods.Select(mod => mod.Split(' ')))
            {
                if (modParts.Length < 0) throw new FormatException("Unknown Modification");
                var modName = modParts[0];
                var modPos = Convert.ToInt32(modParts[1]);
                var modification = Modification.Get(modName);
                parsedMods.Add(new Tuple<int, Modification>(modPos, modification));
                if (modification == null)
                {
                    throw new InvalidModificationNameException(String.Format("Found an unrecognized modification: {0}", modName), modName);
                }
            }
            return parsedMods;
        }

        private readonly string _tsvFile;
    }

    public class InvalidModificationNameException : Exception
    {
        public string ModificationName { get; private set; }
        public InvalidModificationNameException(string message, string modificationName): base(message)
        {
            ModificationName = modificationName;
        }
    }

    internal class CompareModByHighestPosition : IComparer<Tuple<int, Modification>>
    {
        public int Compare(Tuple<int, Modification> x, Tuple<int, Modification> y)
        {
            return (y.Item1.CompareTo(x.Item1));
        }
    }
}
