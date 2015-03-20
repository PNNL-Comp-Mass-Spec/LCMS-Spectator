using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LcmsSpectator.Models;
using LcmsSpectator.Readers.SequenceReaders;

namespace LcmsSpectator.Readers
{
    using System.Threading;

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
            var expectedHeaders = new List<string>
            {
                "Protein",
                "Peptide",
                "Charge",
                "QValue",
            };

            foreach (var header in expectedHeaders.Where(header => !headers.ContainsKey(header)))
            {
                throw new KeyNotFoundException(String.Format("Missing expected column header \"{0}\" in ID file.", header));
            }

            var parts = line.Split('\t');

            int scoreIndex;
            double score = 0;
            if (headers.TryGetValue("SpecEValue", out scoreIndex)) score = Convert.ToDouble(parts[scoreIndex]);
            else if (headers.TryGetValue("MSGFDB_SpecEValue", out scoreIndex)) score = Convert.ToDouble(parts[scoreIndex]);

            int scanIndex, scan=0;
            if (headers.TryGetValue("ScanNum", out scanIndex)) scan = Convert.ToInt32(parts[scanIndex]);
            else if (headers.TryGetValue("Scan", out scanIndex)) scan = Convert.ToInt32(parts[scanIndex]);

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

            bool trim = false;

            if (_filePath.Contains("_syn")) trim = true;

            foreach (var protein in proteinNames)
            {
                var prsm = new PrSm
                {
                    Heavy = false,
                    Scan = scan,
                    Charge = Convert.ToInt32(parts[headers["Charge"]]),
                    Sequence = sequenceReader.Read(sequenceText, trim),
                    SequenceText = sequenceText,
                    ProteinName = protein,
                    ProteinDesc = "",
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
