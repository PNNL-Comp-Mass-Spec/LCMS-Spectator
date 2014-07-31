using System;
using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.Data.Sequence;

namespace LcmsSpectatorModels.Models
{
    public class ChargeStateId: IIdData
    {
        public int Charge { get; private set; }
        public string ProteinNameDesc { get; private set; }
        public Sequence Sequence { get; private set; }
        public string SequenceText { get; private set; }
        public double PrecursorMz { get; private set; }

        public Dictionary<int, PrSm> PrSms { get; set; }
        public List<LabeledXic> SelectedFragmentXics { get; set; }
        public List<LabeledXic> SelectedPrecursorXics { get; set; } 

        public ChargeStateId(int charge, Sequence sequence, string sequenceText, string proteinNameDesc, double precMz)
        {
            ProteinNameDesc = proteinNameDesc;
            PrecursorMz = precMz;
            Charge = charge;
            Sequence = sequence;
            SequenceText = sequenceText;
            PrSms = new Dictionary<int, PrSm>();
        }

        public void Add(PrSm data)
        {
            if (!PrSms.ContainsKey(data.Scan)) PrSms.Add(data.Scan, data);
        }

        public void Remove(PrSm data)
        {
            if (PrSms.ContainsKey(data.Scan)) PrSms.Remove(data.Scan);
        }

        public PrSm GetHighestScoringPrSm()
        {
            PrSm highest = null;
            foreach (var prsm in PrSms.Values)
            {
                if (highest == null || prsm.MatchedFragments >= highest.MatchedFragments)
                {
                    highest = prsm;
                }
            }
            return highest;
        }

        public bool Contains(PrSm data)
        {
            return PrSms.Values.Any(prsm => (prsm.CompareTo(data) == 0));
        }
    }

    class ChargeStateComparer : IComparer<ChargeStateId>
    {
        public int Compare(ChargeStateId x, ChargeStateId y)
        {
            return x.Charge.CompareTo(y.Charge);
        }
    }
}
