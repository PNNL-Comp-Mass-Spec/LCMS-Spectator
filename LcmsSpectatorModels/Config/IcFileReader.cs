using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectatorModels.Models;

namespace LcmsSpectatorModels.Config
{
    public class IcFileReader: IIdFileReader
    {
        public IcFileReader(string tsvFile)
        {
            _tsvFile = tsvFile;
        }

        public IdentificationTree Read(LcMsRun lcms, string rawFileName)
        {
            var idTree = new IdentificationTree();
            var file = File.ReadLines(_tsvFile);
            var headers = new Dictionary<string, int>();
            var lineCount = 0;
            foreach (var line in file)
            {
                lineCount++;
                if (lineCount == 1) // first line
                {
                    var parts = line.Split('\t');
                    for (int i = 0; i < parts.Length; i++)
                    {
                        headers.Add(parts[i], i);
                    }
                    continue;
                }
                var idData = CreatePrSm(line, headers, lcms, rawFileName);
                idTree.Add(idData);
            }

            return idTree;
        }

        private PrSm CreatePrSm(string line, Dictionary<string, int> headers, LcMsRun lcms, string rawFileName)
        {
            var parts = line.Split('\t');
            var scoreLabel = "IcScore";
            if (!headers.ContainsKey(scoreLabel)) scoreLabel = "#MatchedFragments";
            var score = Convert.ToDouble(parts[headers[scoreLabel]]);
            var prsm = new PrSm
            {
                Heavy = false,
                RawFileName = rawFileName,
                Lcms = lcms,
                Scan = Convert.ToInt32(parts[headers["Scan"]]),
                Pre = parts[headers["Pre"]],
                Protein = parts[headers["Sequence"]],
                Post = parts[headers["Post"]],
                Annotation = (parts[headers["Pre"]] + "." + parts[headers["Sequence"]] + "." + parts[headers["Post"]]).Replace('-', '_'),
                SequenceLabel = new List<string>(),
                Composition = parts[headers["Composition"]],
                ProteinName = parts[headers["ProteinName"]],
                ProteinDesc = parts[headers["ProteinDesc"]].Split(';').FirstOrDefault(),
                ProteinNameDesc = parts[headers["ProteinName"]] + "; " + parts[headers["ProteinDesc"]],
                ProteinLength = Convert.ToInt32(parts[headers["ProteinLength"]]),
                Start = Convert.ToInt32(parts[headers["Start"]]),
                End = Convert.ToInt32(parts[headers["End"]]),
                Charge = Convert.ToInt32(parts[headers["Charge"]]),
                MostAbundantIsotopeMz = Convert.ToDouble(parts[headers["MostAbundantIsotopeMz"]]),
                Mass = Convert.ToDouble(parts[headers["Mass"]]),
                MatchedFragments = Math.Round(score, 3),
//              IsotopeCorrPrevMs1 = Convert.ToDouble(parts[headers["IsotopeCorrPrevMs1"]]),
//              IsotopeCorrNextMs1 = Convert.ToDouble(parts[headers["IsotopeCorrNextMs1"]]),
//              CorrMostAbundantPlusOneIsoptope = Convert.ToDouble(parts[headers["CorrMostAbundantPlusOneIsotope"]]),
//              ChargeCorrMinusOne = Convert.ToDouble(parts[headers["ChargeCorrMinusOne"]]),
//              ChargeCorrPlusOne = Convert.ToDouble(parts[headers["ChargeCorrPlusOne"]]),
                QValue = Math.Round(Convert.ToDouble(parts[headers["QValue"]]), 4),
                PepQValue = Convert.ToDouble(parts[headers["PepQValue"]]),
            };
            prsm.SetModifications(parts[headers["Modifications"]]);
            return prsm;
        }

        private readonly string _tsvFile;
    }
}
