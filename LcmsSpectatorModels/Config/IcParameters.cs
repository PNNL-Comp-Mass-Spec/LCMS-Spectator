using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using InformedProteomics.Backend.Data.Enum;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;

namespace LcmsSpectatorModels.Config
{
    public enum PrecursorViewMode
    {
        Isotopes,
        Charges
    }

    public class IcParameters
    {
        public Tolerance PrecursorTolerancePpm { get; set; }
        public Tolerance ProductIonTolerancePpm { get; set; }
        public double IonCorrelationThreshold { get; set; }
        public List<SearchModification> Modifications { get; set; }
        public int PointsToSmooth { get; set; }
        public double SpectrumFilterWindowSize { get; set; }
        public double PrecursorRelativeIntensityThreshold { get; set; }
        public PrecursorViewMode PrecursorViewMode { get; set; }
        public List<SearchModification> SearchModifications { get; set; }
        public List<Modification> LightModifications { get; set; } 
        public List<Modification> HeavyModifications { get; set; } 
        public IonTypeFactory IonTypeFactory { get; private set; }
        public bool AutomaticallySelectIonTypes { get; set; }
        public List<BaseIonType> CidHcdIonTypes { get; set; }
        public List<BaseIonType> EtdIonTypes { get; set; } 

        public event Action IcParametersUpdated;

        public static IcParameters Instance
        {
            get { return _instance ?? (_instance = new IcParameters()); }
        }

        public IonType GetIonType(BaseIonType baseIonType, NeutralLoss neutralLoss, int charge)
        {
            var chargeStr = charge.ToString(CultureInfo.InvariantCulture);
            if (charge == 1) chargeStr = "";
            var name = baseIonType.Symbol + chargeStr + neutralLoss.Name;
            return IonTypeFactory.GetIonType(name);
        }

        public void Update()
        {
            if (IcParametersUpdated != null) IcParametersUpdated();
        }

        public string GetCidHcdIonTypes()
        {
            return CidHcdIonTypes.Aggregate("", (current, ionType) => current + ionType.Symbol + " ");
        }

        public string GetEtdIonTypes()
        {
            return EtdIonTypes.Aggregate("", (current, ionType) => current + ionType.Symbol + " ");
        }

        public static List<BaseIonType> IonTypeStringParse(string str)
        {
            var parts = str.Split(' ');
            var ionNames = new Dictionary<string, BaseIonType>
            {
                {BaseIonType.A.Symbol, BaseIonType.A},
                {BaseIonType.B.Symbol, BaseIonType.B},
                {BaseIonType.C.Symbol, BaseIonType.C},
                {BaseIonType.X.Symbol, BaseIonType.X},
                {BaseIonType.Y.Symbol, BaseIonType.Y},
                {BaseIonType.Z.Symbol, BaseIonType.Z}
            };

            return (from part in parts where ionNames.ContainsKey(part) select ionNames[part]).ToList();
        }

        private IcParameters()
        {
            Modifications = new List<SearchModification>();

            PrecursorTolerancePpm = new Tolerance(10, ToleranceUnit.Ppm);
            ProductIonTolerancePpm = new Tolerance(10, ToleranceUnit.Ppm);
            IonCorrelationThreshold = 0.7;
            PointsToSmooth = 9;
            SpectrumFilterWindowSize = 2.5;
            PrecursorRelativeIntensityThreshold = 0.1;
            PrecursorViewMode = PrecursorViewMode.Isotopes;
            SearchModifications = new List<SearchModification>
            {
                new SearchModification(Modification.Carbamidomethylation, 'C', SequenceLocation.Everywhere, true)
            };
            LightModifications = new List<Modification>();
            HeavyModifications = new List<Modification> { Modification.LysToHeavyLys, Modification.ArgToHeavyArg };
            IonTypeFactory = new IonTypeFactory(100);
            AutomaticallySelectIonTypes = true;
            CidHcdIonTypes = new List<BaseIonType>{BaseIonType.B, BaseIonType.Y};
            EtdIonTypes = new List<BaseIonType>{BaseIonType.C, BaseIonType.Z};
        }

        private static IcParameters _instance;
    }
}
