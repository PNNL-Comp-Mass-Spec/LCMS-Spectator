using System;
using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.MassSpecData;

namespace LcmsSpectatorModels.Models
{
    public class ProteoformId: IIdData
    {
        public string SequenceText { get; private set; }
        public Dictionary<int, ChargeStateId> ChargeStates { get; private set; }

        public ProteoformId(string sequenceText)
        {
            SequenceText = sequenceText;
            ChargeStates = new Dictionary<int, ChargeStateId>();
        }

        public void Add(PrSm data)
        {
            if (!ChargeStates.ContainsKey(data.Charge)) ChargeStates.Add(data.Charge, new ChargeStateId(data.Charge));
            var chargeState = ChargeStates[data.Charge];
            chargeState.Add(data);
        }

        public void Remove(PrSm data)
        {
            if (ChargeStates.ContainsKey(data.Charge)) ChargeStates[data.Charge].Remove(data);
        }

        public void SetLcmsRun(ILcMsRun lcms, string rawFileName)
        {
            foreach (var chargeState in ChargeStates.Values)
            {
                chargeState.SetLcmsRun(lcms, rawFileName);
            }
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
                if (highest == null || chargeStateHighest.CompareTo(highest) >= 0)
                {
                    highest = chargeStateHighest;
                }
            }
            return highest;
        }

        public void RemovePrSmsFromRawFile(string rawFileName)
        {
            var newChargeStates = new Dictionary<int, ChargeStateId>();
            foreach (var chargeState in ChargeStates)
            {
                chargeState.Value.RemovePrSmsFromRawFile(rawFileName);
                if (chargeState.Value.PrSms.Count > 0) newChargeStates.Add(chargeState.Key, chargeState.Value);
            }
            ChargeStates = newChargeStates;
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
