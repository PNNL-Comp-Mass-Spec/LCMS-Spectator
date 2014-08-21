using System;
using System.Collections.Generic;
using System.Linq;

namespace LcmsSpectatorModels.Models
{
    public class IdentificationTree: IIdData
    {
        public Dictionary<string, ProteinId> Proteins { get; private set; }

        public IdentificationTree()
        {
            Proteins = new Dictionary<string, ProteinId>();
        }

        public IEnumerable<ProteinId> ProteinIds
        {
            get
            {
                return (from protein in Proteins.Values
                            where protein.Sequence.Count != 0
                            select protein);
            }
        }

        public void Add(PrSm data)
        {
            RemoveUnidentifiedScan(data);
            if (!Proteins.ContainsKey(data.ProteinName)) Proteins.Add(data.ProteinName, new ProteinId(data.Sequence, data.SequenceText, data.ProteinNameDesc));
            var protein = Proteins[data.ProteinName];
            protein.Add(data);
        }

        public void Add(IEnumerable<PrSm> prsms)
        {
            foreach (var prsm in prsms) Add(prsm);
        }

        public void Add(IdentificationTree idTree)
        {
            Add(idTree.AllPrSms);
        }

        public void Remove(PrSm data)
        {
            if (Proteins.ContainsKey(data.ProteinName)) Proteins[data.ProteinName].Remove(data);
        }

        public void RemoveUnidentifiedScan(PrSm data)
        {
            ProteinId protein;
            ProteoformId proteoform;
            ChargeStateId chargeState;
            if (Proteins.TryGetValue("", out protein) &&
                protein.Proteoforms.TryGetValue("", out proteoform) &&
                proteoform.ChargeStates.TryGetValue(0, out chargeState)) chargeState.Remove(data);
        }

        public bool Contains(PrSm data)
        {
            return Proteins.Values.Any(protein => protein.Contains(data));
        }

        public PrSm GetHighestScoringPrSm()
        {
            PrSm highest = null;
            foreach (var protein in Proteins.Values)
            {
                var pHighest = protein.GetHighestScoringPrSm();
                if (highest == null || pHighest.MatchedFragments >= highest.MatchedFragments)
                {
                    highest = pHighest;
                }
            }
            return highest;
        }

        public ProteinId GetProtein(PrSm data)
        {
            ProteinId protein = null;
            if (Proteins.ContainsKey(data.ProteinName)) protein = Proteins[data.ProteinName];
            return protein;
        }

        public ProteoformId GetProteoform(PrSm data)
        {
            var protein = GetProtein(data);
            if (protein == null) return null;
            ProteoformId proteoform = null;
            if (protein.Proteoforms.ContainsKey(data.SequenceText)) proteoform = protein.Proteoforms[data.SequenceText];
            return proteoform;
        }

        public ChargeStateId GetChargeState(PrSm data)
        {
            var proteoform = GetProteoform(data);
            if (proteoform == null) return null;
            ChargeStateId chargeState = null;
            if (proteoform.ChargeStates.ContainsKey(data.Charge)) chargeState = proteoform.ChargeStates[data.Charge];
            return chargeState;
        }

        public PrSm GetPrSm(PrSm data)
        {
            var chargeState = GetChargeState(data);
            if (chargeState == null) return null;
            PrSm prsm = null;
            if (chargeState.Contains(data)) prsm = chargeState.PrSms[new Tuple<int, string>(data.Scan, data.RawFileName)];
            return prsm;
        }

        public List<PrSm> AllPrSms
        {
            get
            {
                return (from protein in Proteins.Values
                        from proteoform in protein.Proteoforms.Values
                        from charge in proteoform.ChargeStates.Values
                        from prsm in charge.PrSms.Values
                            select prsm).ToList();
            }
        }

        public List<PrSm> IdentifiedPrSms
        {
            get
            {
                return (from protein in Proteins.Values
                        from proteoform in protein.Proteoforms.Values
                        from charge in proteoform.ChargeStates.Values
                        from prsm in charge.PrSms.Values
                            where prsm.MatchedFragments >= 0
                            select prsm).ToList();
            }
        }

        public IdentificationTree GetTreeFilteredByQValue(double qValue)
        {
            var idTree = new IdentificationTree();
            var prsms = AllPrSms;
            foreach (var prsm in prsms)
            {
                if (prsm.QValue <= qValue) idTree.Add(prsm);
            }
            return idTree;
        }

        public void RemovePrSmsFromRawFile(string rawFileName)
        {
            var newProteins = new Dictionary<string, ProteinId>();
            foreach (var protein in Proteins)
            {
                protein.Value.RemovePrSmsFromRawFile(rawFileName);
                if (protein.Value.Proteoforms.Count > 0) newProteins.Add(protein.Key, protein.Value);
            }
            Proteins = newProteins;
        }
    }
}
