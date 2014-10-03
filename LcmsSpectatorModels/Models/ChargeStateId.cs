using System;
using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.MassSpecData;

namespace LcmsSpectatorModels.Models
{
    public class ChargeStateId: IIdData
    {
        public int Charge { get; private set; }

        public Dictionary<Tuple<int, string>, PrSm> PrSms { get; set; }

        public ChargeStateId(int charge)
        {
            Charge = charge;
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
                if (!prsm.Score.Equals(Double.NaN) && (highest == null || prsm.CompareTo(highest) >= 0))
                {
                    highest = prsm;
                }
            }
            return highest;
        }

        public void SetLcmsRun(ILcMsRun lcms, string rawFileName)
        {
            foreach (var prsm in PrSms.Values)
            {
                prsm.RawFileName = rawFileName;
                prsm.Lcms = lcms;
            }
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
