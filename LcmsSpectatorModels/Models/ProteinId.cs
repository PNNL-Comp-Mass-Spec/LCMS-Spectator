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
        public List<ProteoformId> Proteoforms { get; private set; }
        public ProteinId(PrSm prsm)
        {
            SequenceText = prsm.Protein;
            Sequence = prsm.Sequence;
            ProteinNameDesc = prsm.ProteinNameDesc;
//            var aaSet = new AminoAcidSet(IcParameters.Instance.Modifications, IcParameters.Instance.MaxDynamicModificationsPerSequence);
//            SequenceGraph = null; //GraphXSequenceGraph.Create(aaSet, prsm.Annotation, IcParameters.Instance.Modifications);
            Proteoforms = new List<ProteoformId>();
            Add(prsm);
        }

        public void Add(PrSm data)
        {
            var proteoform = GetProteoform(data);
            proteoform.Add(data);
        }

        public PrSm GetHighestScoringPrSm()
        {
            PrSm highest = null;
            foreach (var proteoform in Proteoforms)
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
            return Proteoforms.Any(proteoform => proteoform.Contains(data));
        }

        public ProteoformId GetProteoform(PrSm data)
        {
            if (data.Sequence.Count > Sequence.Count)
            {
                Sequence = data.Sequence;
                SequenceText = data.Protein;
//                var aaSet = new AminoAcidSet(IcParameters.Instance.Modifications, IcParameters.Instance.MaxDynamicModificationsPerSequence);
//                SequenceGraph = null; //GraphXSequenceGraph.Create(aaSet, data.Annotation, IcParameters.Instance.Modifications);
            }
            var searchProteoform = new ProteoformId(data.Sequence, data.SequenceText);
            var pos = Proteoforms.BinarySearch(searchProteoform, new SequenceComparer());
            ProteoformId proteoform;
            if (pos < 0)
            {
                Proteoforms.Add(searchProteoform);
                proteoform = Proteoforms.Last();
                Proteoforms.Sort(new SequenceComparer());
            }
            else
            {
                proteoform = Proteoforms[pos];
            }
            return proteoform;
        }

        public List<PrSm> Scans
        {
            get
            {
                return (from proteoform in Proteoforms
                        from charge in proteoform.ChargeStates
                        from prsm in charge.PrSms select prsm).ToList();
            }
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
