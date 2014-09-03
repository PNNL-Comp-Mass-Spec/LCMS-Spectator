using InformedProteomics.Backend.MassSpecData;
using LcmsSpectatorModels.Models;
using LcmsSpectatorModels.Readers.SequenceReaders;
using MTDBFramework.Data;

namespace LcmsSpectatorModels.Readers
{
    public class MzIdentMlReader: IIdFileReader
    {
        public MzIdentMlReader(string fileName)
        {
            _fileName = fileName;
            _mzIdentMlReader = new MTDBFramework.IO.MzIdentMlReader(new Options());
        }

        public IdentificationTree Read(ILcMsRun lcms, string rawFileName)
        {
            var dataSet = _mzIdentMlReader.Read(_fileName);
            var tool = dataSet.Tool;
            var toolType = tool == LcmsIdentificationTool.MsgfPlus ? ToolType.MsgfPlus : ToolType.Other;
            var idTree = new IdentificationTree(toolType);

            var evidences = dataSet.Evidences;

            var sequenceReader = new SequenceReader();

            foreach (var evidence in evidences)
            {
                var sequenceText = evidence.SeqWithNumericMods;
                var index = sequenceText.IndexOf('.');
                var lastIndex = sequenceText.LastIndexOf('.');
                if (index != lastIndex && index >= 0 && lastIndex >= 0 && sequenceText.Length > 1) // remove 
                    sequenceText = sequenceText.Substring(index+1, sequenceText.Length - (sequenceText.Length - lastIndex) - (index+1));
                foreach (var protein in evidence.Proteins)
                {
                    var prsm = new PrSm
                    {
                        SequenceText = sequenceText,
                        Sequence = sequenceReader.Read(sequenceText),
                        Scan = evidence.Scan,
                        Lcms = lcms,
                        RawFileName = rawFileName,
                        ProteinName = protein.ProteinName,
                        Charge = evidence.Charge,
                    };
                    idTree.Add(prsm);
                }
            }
            return idTree;
        }

        private readonly string _fileName;
        private readonly MTDBFramework.IO.MzIdentMlReader _mzIdentMlReader;
    }
}
