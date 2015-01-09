using System;
using System.Collections.Generic;
using System.Globalization;
using InformedProteomics.Backend.Data.Enum;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;

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
        }

        private static IcParameters _instance;
    }
}
