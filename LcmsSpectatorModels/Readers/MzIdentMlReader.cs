using System;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectatorModels.Models;
using LcmsSpectatorModels.Readers.SequenceReaders;
using MTDBFramework.Algorithms;
using MTDBFramework.Data;

namespace LcmsSpectatorModels.Readers
{
    public class MzIdentMlReader: IIdFileReader
    {
        public MzIdentMlReader(string fileName)
        {
            _fileName = fileName;
            var options = new Options();
            options.TargetFilterType = TargetWorkflowType.BOTTOM_UP;
            _mzIdentMlReader = new MTDBFramework.IO.MzIdentMlReader(options);
        }

        public IdentificationTree Read()
        {
            var dataSet = _mzIdentMlReader.Read(_fileName);
            var idTree = new IdentificationTree(ToolType.MsgfPlus);

            var evidences = dataSet.Evidences;

            var sequenceReader = new SequenceReader();

            foreach (var evidence in evidences)
            {
                var msgfPlusEvidence = (MsgfPlusResult) evidence;
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
                        ProteinName = protein.ProteinName,
                        Charge = evidence.Charge,
                        MatchedFragments = evidence.SpecProb,
                        QValue = msgfPlusEvidence.QValue,
                        UseGolfScoring = true,
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
