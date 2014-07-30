using System.Collections.Generic;
using System.IO;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectatorModels.Models;

namespace LcmsSpectatorModels.Config
{
    public class IcFileReader
    {
        public IcFileReader(string tsvFile, string rawFile)
        {
            IcParameters.Instance.RawFile = rawFile;
            _tsvFile = tsvFile;
        }

        public IcFileReader(string tsvFile, LcMsRun lcms)
        {
            _lcms = lcms;
            _tsvFile = tsvFile;
        }

        public IdentificationTree Read()
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
                var idData = new PrSm(line, headers, _lcms);
                idTree.Add(idData);
            }

            return idTree;
        }


        private readonly LcMsRun _lcms;
        private readonly string _tsvFile;
//        private const double QValueThreshold = 0.01;
    }
}
