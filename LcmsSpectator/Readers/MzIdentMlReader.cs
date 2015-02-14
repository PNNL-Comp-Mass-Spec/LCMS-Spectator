using System.Collections.Generic;
using LcmsSpectator.Models;
using LcmsSpectator.Readers.SequenceReaders;
using MTDBFramework.Algorithms;
using MTDBFramework.Data;

namespace LcmsSpectator.Readers
{
    public class MzIdentMlReader: IIdFileReader
    {
        public MzIdentMlReader(string fileName)
        {
            _fileName = fileName;
            var options = new Options
            {
                MsgfQValue = 1.0,
                MaxMsgfSpecProb = 1.0,
                TargetFilterType = TargetWorkflowType.BOTTOM_UP
            };
            _mzIdentMlReader = new MTDBFramework.IO.MzIdentMlReader(options);
        }

        public IdentificationTree Read(IEnumerable<string> modIgnoreList = null)
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
                        Score = evidence.SpecProb,
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
