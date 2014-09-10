using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LcmsSpectatorModels.Models;

namespace LcmsSpectatorModels.Readers
{
    public class IcFileReader: IIdFileReader
    {
        public IcFileReader(string tsvFile)
        {
            _tsvFile = tsvFile;
        }

        public IdentificationTree Read()
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
                var idData = CreatePrSms(line, headers);
                idTree.Add(idData);
            }
            file.Close();

            return idTree;
        }

        private IEnumerable<PrSm> CreatePrSms(string line, Dictionary<string, int> headers)
        {
            var parts = line.Split('\t');
            var scoreLabel = "IcScore";
            if (!headers.ContainsKey(scoreLabel)) scoreLabel = "#MatchedFragments";
            var score = Convert.ToDouble(parts[headers[scoreLabel]]);

            var proteinNames = parts[headers["ProteinName"]].Split(';');
            var prsms = new List<PrSm>{Capacity = proteinNames.Length};

            foreach (var protein in proteinNames)
            {
                var prsm = new PrSm
                {
                    Heavy = false,
                    Scan = Convert.ToInt32(parts[headers["Scan"]]),
                    Pre = parts[headers["Pre"]],
                    Protein = parts[headers["Sequence"]],
                    Post = parts[headers["Post"]],
                    Annotation =
                        (parts[headers["Pre"]] + "." + parts[headers["Sequence"]] + "." + parts[headers["Post"]])
                            .Replace('-', '_'),
                    SequenceLabel = new List<string>(),
                    Composition = parts[headers["Composition"]],
                    ProteinName = protein,
                    ProteinDesc = parts[headers["ProteinDesc"]].Split(';').FirstOrDefault(),
                    ProteinLength = Convert.ToInt32(parts[headers["ProteinLength"]]),
                    Start = Convert.ToInt32(parts[headers["Start"]]),
                    End = Convert.ToInt32(parts[headers["End"]]),
                    Charge = Convert.ToInt32(parts[headers["Charge"]]),
                    MostAbundantIsotopeMz = Convert.ToDouble(parts[headers["MostAbundantIsotopeMz"]]),
                    Mass = Convert.ToDouble(parts[headers["Mass"]]),
                    Score = Math.Round(score, 3),
//              IsotopeCorrPrevMs1 = Convert.ToDouble(parts[headers["IsotopeCorrPrevMs1"]]),
//              IsotopeCorrNextMs1 = Convert.ToDouble(parts[headers["IsotopeCorrNextMs1"]]),
//              CorrMostAbundantPlusOneIsoptope = Convert.ToDouble(parts[headers["CorrMostAbundantPlusOneIsotope"]]),
//              ChargeCorrMinusOne = Convert.ToDouble(parts[headers["ChargeCorrMinusOne"]]),
//              ChargeCorrPlusOne = Convert.ToDouble(parts[headers["ChargeCorrPlusOne"]]),
                    QValue = Math.Round(Convert.ToDouble(parts[headers["QValue"]]), 4),
                    PepQValue = Convert.ToDouble(parts[headers["PepQValue"]]),
                };
                prsm.SetModifications(parts[headers["Modifications"]]);
                prsms.Add(prsm);
            }
            return prsms;
        }

        private readonly string _tsvFile;
    }
}
