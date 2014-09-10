using System;
using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.MassSpecData;

namespace LcmsSpectatorModels.Models
{
    public class ProteinId: IIdData
    {
        public string SequenceText { get; private set; }
        public Sequence Sequence { get; private set; }
        public string ProteinNameDesc { get; private set; }
//        public SequenceGraph SequenceGraph { get; private set; }
        public Dictionary<string, ProteoformId> Proteoforms { get; private set; }
        public ProteinId(Sequence sequence, string sequenceText, string proteinNameDesc)
        {
            SequenceText = sequenceText;
            Sequence = sequence;
            ProteinNameDesc = proteinNameDesc;
            Proteoforms = new Dictionary<string, ProteoformId>();
        }

        public void Add(PrSm data)
        {
            if (!Proteoforms.ContainsKey(data.SequenceText)) Proteoforms.Add(data.SequenceText, new ProteoformId(data.Sequence, data.SequenceText));
            var proteoform = Proteoforms[data.SequenceText];
            proteoform.Add(data);
        }

        public void Remove(PrSm data)
        {
            if (Proteoforms.ContainsKey(data.SequenceText)) Proteoforms[data.SequenceText].Remove(data);
        }

        public PrSm GetHighestScoringPrSm()
        {
            PrSm highest = null;
            foreach (var proteoform in Proteoforms.Values)
            {
                var pfHighest = proteoform.GetHighestScoringPrSm();
                if (highest == null || pfHighest.CompareTo(highest) >= 0)
                {
                    highest = pfHighest;
                }
            }
            return highest;
        }

        public void SetLcmsRun(ILcMsRun lcms, string rawFileName)
        {
            foreach (var proteoform in Proteoforms.Values)
            {
                proteoform.SetLcmsRun(lcms, rawFileName);
            }
        }

        public bool Contains(PrSm data)
        {
            return Proteoforms.Values.Any(proteoform => proteoform.Contains(data));
        }

        public void RemovePrSmsFromRawFile(string rawFileName)
        {
            var newProteoforms = new Dictionary<string, ProteoformId>();
            foreach (var proteoform in Proteoforms)
            {
                proteoform.Value.RemovePrSmsFromRawFile(rawFileName);
                if (proteoform.Value.ChargeStates.Count > 0) newProteoforms.Add(proteoform.Key, proteoform.Value);
            }
            Proteoforms = newProteoforms;
        }
    }

    class ProteinIdNameDescComparer : IComparer<ProteinId>
    {
        public int Compare(ProteinId x, ProteinId y)
        {
            return (String.Compare(x.ProteinNameDesc, y.ProteinNameDesc, StringComparison.Ordinal));
        }
    }
}
