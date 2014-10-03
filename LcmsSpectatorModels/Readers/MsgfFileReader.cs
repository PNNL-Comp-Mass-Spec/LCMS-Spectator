using System;
using System.Collections.Generic;
using System.IO;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectatorModels.Models;

namespace LcmsSpectatorModels.Readers
{
    public class MsgfFileReader: IIdFileReader
    {
        public MsgfFileReader(string tsvFile)
        {
            _filePath = tsvFile;
        }

        public IdentificationTree Read()
        {
            var ext = Path.GetFileNameWithoutExtension(_filePath);
            return (ext == ".gz") ? ReadFromMzId() : ReadFromTsvFile();
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

        private IEnumerable<PrSm> CreatePrSms(string line, Dictionary<string, int> headers)
        {
            var parts = line.Split('\t');
            var score = Convert.ToDouble(parts[headers["SpecEValue"]]);

            var proteinNames = parts[headers["Protein"]].Split(';');
            var prsms = new List<PrSm> { Capacity = proteinNames.Length };

            foreach (var protein in proteinNames)
            {
                var prsm = new PrSm
                {
                    Heavy = false,
                    Scan = Convert.ToInt32(parts[headers["ScanNum"]]),
                    Sequence = Sequence.GetSequenceFromMsGfPlusPeptideStr(parts[headers["Peptide"]]),
                    SequenceText = parts[headers["Peptide"]],
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

        private IdentificationTree ReadFromMzId()
        {
            throw new NotImplementedException();
        /*     var oReader = new PHRPReader.clsPHRPReader(_filePath) {SkipDuplicatePSMs = true};
               var idTree = new IdentificationTree();
                while (oReader.MoveNext())
                {
                    var prsm = new PrSm
                    {
                        Scan = oReader.CurrentPSM.ScanNumber,
                        Charge = oReader.CurrentPSM.Charge,
                    };
                    idTree.Add(prsm);
                } */
        } 

        private readonly string _filePath;
    }
}
