using System;
using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.Data.Sequence;

namespace LcmsSpectatorModels.Models
{
    public class ProteoformId: IIdData
    {
        public Sequence Sequence { get; private set; }
        public string SequenceText { get; private set; }
        public Dictionary<int, ChargeStateId> ChargeStates { get; private set; }

        public ProteoformId(Sequence sequence, string sequenceText)
        {
            Sequence = sequence;
            SequenceText = sequenceText;
            ChargeStates = new Dictionary<int, ChargeStateId>();
        }

        public void Add(PrSm data)
        {
            if (!ChargeStates.ContainsKey(data.Charge)) ChargeStates.Add(data.Charge, new ChargeStateId(data.Charge, data.Sequence, data.SequenceText, data.ProteinNameDesc, data.PrecursorMz));
            var chargeState = ChargeStates[data.Charge];
            chargeState.Add(data);
        }

        public void Remove(PrSm data)
        {
            if (ChargeStates.ContainsKey(data.Charge)) ChargeStates[data.Charge].Remove(data);
        }

        public bool Contains(PrSm data)
        {
            return ChargeStates.Values.Any(chargeState => chargeState.Contains(data));
        }

        public PrSm GetHighestScoringPrSm()
        {
            PrSm highest = null;
            foreach (var chargeState in ChargeStates.Values)
            {
                var chargeStateHighest = chargeState.GetHighestScoringPrSm();
                if (highest == null || chargeStateHighest.MatchedFragments >= highest.MatchedFragments)
                {
                    highest = chargeStateHighest;
                }
            }
            return highest;
        }

    }

    internal class SequenceComparer : IComparer<ProteoformId>
    {
        public int Compare(ProteoformId x, ProteoformId y)
        {
            return (String.Compare(x.SequenceText, y.SequenceText, StringComparison.Ordinal));
        }
    }
}
