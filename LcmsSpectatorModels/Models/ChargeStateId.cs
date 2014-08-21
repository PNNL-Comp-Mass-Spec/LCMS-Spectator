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
        public Sequence HeavySequence { get; private set; }
        public string SequenceText { get; private set; }
        public double PrecursorMz { get; private set; }

        public Dictionary<Tuple<int, string>, PrSm> PrSms { get; set; }
        public List<LabeledXic> SelectedFragmentXics { get; set; }
        public List<LabeledXic> SelectedPrecursorXics { get; set; } 

        public ChargeStateId(int charge, Sequence sequence, Sequence heavySequence, string sequenceText, string proteinNameDesc, double precMz)
        {
            ProteinNameDesc = proteinNameDesc;
            PrecursorMz = precMz;
            Charge = charge;
            Sequence = sequence;
            HeavySequence = heavySequence;
            SequenceText = sequenceText;
            PrSms = new Dictionary<Tuple<int, string>, PrSm>();
        }

        public void Add(PrSm data)
        {
            var key = new Tuple<int, string>(data.Scan, data.RawFileName);
            if (!PrSms.ContainsKey(key)) PrSms.Add(key, data);
            else PrSms[key] = data;
        }

        public void Remove(PrSm data)
        {
            var key = new Tuple<int, string>(data.Scan, data.RawFileName);
            if (PrSms.ContainsKey(key)) PrSms.Remove(key);
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
            var key = new Tuple<int, string>(data.Scan, data.RawFileName);
            return PrSms.ContainsKey(key);
        }


        public void RemovePrSmsFromRawFile(string rawFileName)
        {
            var newPrsms = PrSms.Where(prsm => prsm.Value.RawFileName != rawFileName).ToDictionary(prsm => prsm.Key, prsm => prsm.Value);
            PrSms = newPrsms;
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
