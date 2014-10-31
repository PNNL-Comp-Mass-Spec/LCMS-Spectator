using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Messaging;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectatorModels.Models;
using LcmsSpectatorModels.Readers.SequenceReaders;

namespace LcmsSpectatorModels.Readers
{
    public class MsgfFileReader: IIdFileReader
    {
        public MsgfFileReader(string tsvFile)
        {
            _filePath = tsvFile;
        }

        public IdentificationTree Read(IEnumerable<string> modIgnoreList = null)
        {
            return ReadFromTsvFile();
        }

        private IdentificationTree ReadFromTsvFile()
        {
            var idTree = new IdentificationTree(ToolType.MsgfPlus);
            var file = new StreamReader(File.Open(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
//            var file = File.ReadLines(_filePath);
            var headers = new Dictionary<string, int>();
            var lineCount = 0;
            while (!file.EndOfStream)
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
            return idTree;
        }

        private IEnumerable<PrSm> CreatePrSms(string line, Dictionary<string, int> headers, IEnumerable<string> modIgnoreList = null)
        {
            var parts = line.Split('\t');
            var score = Convert.ToDouble(parts[headers["SpecEValue"]]);

            var proteinNames = parts[headers["Protein"]].Split(';');
            var prsms = new List<PrSm> { Capacity = proteinNames.Length };

            var sequenceText = parts[headers["Peptide"]];

            if (modIgnoreList != null)
            {
                foreach (var mod in modIgnoreList)
                {
                    if (sequenceText.Contains(mod)) return null;
                }
            }

            var sequenceReader = new SequenceReader();

            foreach (var protein in proteinNames)
            {
                var prsm = new PrSm
                {
                    Heavy = false,
                    Scan = Convert.ToInt32(parts[headers["ScanNum"]]),
                    Sequence = sequenceReader.Read(sequenceText),
                    SequenceText = sequenceText,
                    ProteinName = protein,
                    ProteinDesc = "",
                    Charge = Convert.ToInt32(parts[headers["Charge"]]),
                    Score = score,
                    UseGolfScoring = true,
                    QValue = Math.Round(Convert.ToDouble(parts[headers["QValue"]]), 4),
                };
                prsms.Add(prsm);
            }
            return prsms;
        }

        private readonly string _filePath;
    }
}
