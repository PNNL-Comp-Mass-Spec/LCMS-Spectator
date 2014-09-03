using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using InformedProteomics.Backend.Data.Enum;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;

namespace LcmsSpectatorModels.Config
{
    public class IcParameters
    {
        public LcMsRun Lcms { get; set; }
        public string DatabaseFile { get; set; }
        public int SearchMode { get; set; }
        public bool Tda { get; set; }
        public Tolerance PrecursorTolerancePpm { get; set; }
        public Tolerance ProductIonTolerancePpm { get; set; }
        public int MaxDynamicModificationsPerSequence { get; set; }
        public double QValueThreshold { get; set; }
        public double IonCorrelationThreshold { get; set; }
        public List<SearchModification> Modifications { get; set; }
        public int PointsToSmooth { get; set; }
        public List<SearchModification> SearchModifications { get; set; }
        public List<Modification> LightModifications { get; set; } 
        public List<Modification> HeavyModifications { get; set; } 
        public IonTypeFactory IonTypeFactory { get; set; }

        public event Action IcParametersUpdated;

        public static IcParameters Instance
        {
            get { return _instance ?? (_instance = new IcParameters()); }
        }

        public string RawFile
        {
            get { return _rawFile; }
            set
            {
                _rawFile = value;
                ReadRawFile();
            }
        }

        public string ParamFile
        {
            get { return _paramFile; }
            set
            {
                _paramFile = value;
                ReadParamFile();
            }
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
            QValueThreshold = 0.01;
            IonCorrelationThreshold = 0.7;
            MaxDynamicModificationsPerSequence = 0;
            PointsToSmooth = 9;
            SearchModifications = new List<SearchModification>
            {
                new SearchModification(Modification.Carbamidomethylation, 'C', SequenceLocation.Everywhere, true)
            };
            LightModifications = new List<Modification>();
            HeavyModifications = new List<Modification> { Modification.LysToHeavyLys, Modification.ArgToHeavyArg };
            IonTypeFactory = new IonTypeFactory(15);
        }

        private void ReadRawFile()
        {
            Lcms = LcMsRun.GetLcMsRun(_rawFile, MassSpecDataType.XCaliburRun, 0, 0);
        }

        private void ReadParamFile()
        {
            var file = File.ReadLines(_paramFile);

            foreach (var line in file)
            {
                var parts = line.Split('\t');
                if (parts.Length < 2) throw new Exception("Invalid configuration file.");
                switch (parts[0])
                {
                    case "SpecFile":
                        if (Lcms == null)   Lcms = LcMsRun.GetLcMsRun(parts[1], MassSpecDataType.XCaliburRun, 0, 0);
                        break;
                    case "DatabaseFile":
                        DatabaseFile = parts[1];
                        break;
                    case "SearchMode":
                        SearchMode = Convert.ToInt32(parts[1]);
                        break;
                    case "Tda":
                        Tda = Convert.ToBoolean(parts[1]);
                        break;
                    case "PrecursorIonTolerancePpm":
                        PrecursorTolerancePpm = new Tolerance(Convert.ToDouble(parts[1]), ToleranceUnit.Ppm);
                        break;
                    case "ProductIonTolerancePpm":
                        ProductIonTolerancePpm = new Tolerance(Convert.ToDouble(parts[1]), ToleranceUnit.Ppm);
                        break;
                    case "MaxDynamicModificationsPerSequence":
                        MaxDynamicModificationsPerSequence = Convert.ToInt32(parts[1]);
                        break;
                    case "Modification":
                        var modParts = parts[1].Split(',');
//                        var compositionStr = modParts[0];
                        var residue = modParts[1];
                        var isFixed = modParts[2];
                        var position = modParts[3];
                        var modName = modParts[4];
                        var modification = Modification.Get(modName);
                        SequenceLocation sequenceLocation;
                        Enum.TryParse(position, out sequenceLocation);
//                        var composition = Composition.Parse(compositionStr);
                        var isFixedModification = (isFixed == "fix");
                        Modifications.Add(new SearchModification(modification, residue.ToCharArray()[0],
                                                                 sequenceLocation, isFixedModification));
                        break;
                }
            }
        }

        private static IcParameters _instance;
        private string _rawFile;
        private string _paramFile;
    }
}
