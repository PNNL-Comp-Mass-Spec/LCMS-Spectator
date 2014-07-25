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
        public List<ChargeStateId> ChargeStates { get; private set; }

        public ProteoformId(Sequence sequence, string sequenceText)
        {
            Sequence = sequence;
            SequenceText = sequenceText;
            ChargeStates = new List<ChargeStateId>();
        }

        public void Add(PrSm data)
        {
            var chargeState = GetChargeState(data);
            chargeState.Add(data);
        }

        public ChargeStateId GetChargeState(PrSm data)
        {
            var searchChargeState = new ChargeStateId(data.Charge, Sequence, data.SequenceText, data.ProteinNameDesc, data.PrecursorMz);
            var pos = ChargeStates.BinarySearch(searchChargeState, new ChargeStateComparer());
            ChargeStateId chargeState;
            if (pos < 0)
            {
                ChargeStates.Add(searchChargeState);
                chargeState = ChargeStates.Last();
                ChargeStates.Sort(new ChargeStateComparer());
            }
            else
            {
                chargeState = ChargeStates[pos];
            }
            return chargeState;
        }

        public PrSm GetHighestScoringPrSm()
        {
            PrSm highest = null;
            foreach (var chargeState in ChargeStates)
            {
                var chargeStateHighest = chargeState.GetHighestScoringPrSm();
                if (highest == null || chargeStateHighest.MatchedFragments >= highest.MatchedFragments)
                {
                    highest = chargeStateHighest;
                }
            }
            return highest;
        }

        public bool Contains(PrSm data)
        {
            return ChargeStates.Any(chargeState => chargeState.Contains(data));
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
