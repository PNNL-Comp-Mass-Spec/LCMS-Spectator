using System;
using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.Data.Sequence;

namespace LcmsSpectatorModels.Models
{
    public class ProteinId: IIdData
    {
        public string SequenceText { get; private set; }
        public Sequence Sequence { get; private set; }
        public string ProteinNameDesc { get; private set; }
        public SequenceGraph SequenceGraph { get; private set; }
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

        public PrSm GetHighestScoringPrSm()
        {
            PrSm highest = null;
            foreach (var proteoform in Proteoforms.Values)
            {
                var pfHighest = proteoform.GetHighestScoringPrSm();
                if (highest == null || pfHighest.MatchedFragments >= highest.MatchedFragments)
                {
                    highest = pfHighest;
                }
            }
            return highest;
        }

        public bool Contains(PrSm data)
        {
            return Proteoforms.Values.Any(proteoform => proteoform.Contains(data));
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
