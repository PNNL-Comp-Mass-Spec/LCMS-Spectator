using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using InformedProteomics.Backend.Data.Enum;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.ViewModels;
using ReactiveUI;

namespace LcmsSpectator.Config
{
    public enum PrecursorViewMode
    {
        Isotopes,
        Charges
    }

    public class IcParameters: ReactiveObject
    {
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

        private Tolerance _precursorTolerancePpm;
        public Tolerance PrecursorTolerancePpm
        {
            get { return _precursorTolerancePpm; }
            set { this.RaiseAndSetIfChanged(ref _precursorTolerancePpm, value); }
        }

        private Tolerance _productIonTolerance;
        public Tolerance ProductIonTolerancePpm
        {
            get { return _productIonTolerance; }
            set { this.RaiseAndSetIfChanged(ref _productIonTolerance, value); }
        }

        private double _ionCorrelationThreshold;
        public double IonCorrelationThreshold
        {
            get { return _ionCorrelationThreshold; } 
            set { this.RaiseAndSetIfChanged(ref _ionCorrelationThreshold, value); }
        }

        private List<SearchModification> _modifications;
        public List<SearchModification> Modifications
        {
            get { return _modifications; } 
            set { this.RaiseAndSetIfChanged(ref _modifications, value); }
        }

        private int _pointsToSmooth;
        public int PointsToSmooth
        {
            get { return _pointsToSmooth; } 
            set { this.RaiseAndSetIfChanged(ref _pointsToSmooth, value); }
        }

        private double _spectrumFilterWindowSize;
        public double SpectrumFilterWindowSize
        {
            get { return _spectrumFilterWindowSize; } 
            set { this.RaiseAndSetIfChanged(ref _spectrumFilterWindowSize, value); }
        }

        private double _precursorRelativeIntensityThreshold;
        public double PrecursorRelativeIntensityThreshold
        {
            get { return _precursorRelativeIntensityThreshold; }
            set { this.RaiseAndSetIfChanged(ref _precursorRelativeIntensityThreshold, value); }
        }

        private bool _showInstrumentData;
        public bool ShowInstrumentData
        {
            get { return _showInstrumentData; }
            set { this.RaiseAndSetIfChanged(ref _showInstrumentData, value); }
        }

        private PrecursorViewMode _precursorViewMode;
        public PrecursorViewMode PrecursorViewMode
        {
            get { return _precursorViewMode; }
            set { this.RaiseAndSetIfChanged(ref _precursorViewMode, value); }
        }

        private List<SearchModification> _searchModifications;
        public List<SearchModification> SearchModifications
        {
            get { return _searchModifications; } 
            set { this.RaiseAndSetIfChanged(ref _searchModifications, value); }
        }

        private ReactiveList<ModificationViewModel> _lightModifications;
        public ReactiveList<ModificationViewModel> LightModifications
        {
            get { return _lightModifications; }
            private set { this.RaiseAndSetIfChanged(ref _lightModifications, value); }
        }

        private ReactiveList<ModificationViewModel> _heavyModifications;
        public ReactiveList<ModificationViewModel> HeavyModifications
        {
            get { return _heavyModifications; }
            private set { this.RaiseAndSetIfChanged(ref _heavyModifications, value); }
        }

        private IonTypeFactory _ionTypeFactory;
        public IonTypeFactory IonTypeFactory
        {
            get { return _ionTypeFactory; }
            private set { this.RaiseAndSetIfChanged(ref _ionTypeFactory, value); }
        }

        private IonTypeFactory _deconvolutedIonTypeFactory;
        public IonTypeFactory DeconvolutedIonTypeFactory
        {
            get { return _deconvolutedIonTypeFactory; }
            private set { this.RaiseAndSetIfChanged(ref _deconvolutedIonTypeFactory, value); }
        }

        private bool _automaticallySelectIonTypes;
        public bool AutomaticallySelectIonTypes
        {
            get { return _automaticallySelectIonTypes; }
            set { this.RaiseAndSetIfChanged(ref _automaticallySelectIonTypes, value); }
        }

        private List<BaseIonType> _cidBaseIonTypes;
        public List<BaseIonType> CidHcdIonTypes
        {
            get { return _cidBaseIonTypes; }
            set { this.RaiseAndSetIfChanged(ref _cidBaseIonTypes, value); }
        }

        private List<BaseIonType> _etdIonTypes;
        public List<BaseIonType> EtdIonTypes
        {
            get { return _etdIonTypes; }
            set { this.RaiseAndSetIfChanged(ref _etdIonTypes, value); }
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
            ShowInstrumentData = false;
            PrecursorViewMode = PrecursorViewMode.Isotopes;
            SearchModifications = new List<SearchModification>
            {
                new SearchModification(Modification.Carbamidomethylation, 'C', SequenceLocation.Everywhere, true)
            };
            LightModifications = new ReactiveList<ModificationViewModel>
            {
                new ModificationViewModel(Modification.LysToHeavyLys){Selected = false}, 
                new ModificationViewModel(Modification.ArgToHeavyArg) {Selected = false}
            };
            HeavyModifications = new ReactiveList<ModificationViewModel>
            {
                new ModificationViewModel(Modification.LysToHeavyLys), 
                new ModificationViewModel(Modification.ArgToHeavyArg)
            };
            IonTypeFactory = new IonTypeFactory(100);
            DeconvolutedIonTypeFactory = IonTypeFactory.GetDeconvolutedIonTypeFactory(BaseIonType.AllBaseIonTypes,
                                                                                      NeutralLoss.CommonNeutralLosses);
            AutomaticallySelectIonTypes = true;
            CidHcdIonTypes = new List<BaseIonType>{BaseIonType.B, BaseIonType.Y};
            EtdIonTypes = new List<BaseIonType>{BaseIonType.C, BaseIonType.Z};
        }

        private static IcParameters _instance;
    }
}
