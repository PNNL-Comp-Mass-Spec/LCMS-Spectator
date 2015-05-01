namespace LcmsSpectator.Config
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using InformedProteomics.Backend.Data.Composition;
    using InformedProteomics.Backend.Data.Enum;
    using InformedProteomics.Backend.Data.Sequence;
    using InformedProteomics.Backend.Data.Spectrometry;
    using LcmsSpectator.Utils;
    using LcmsSpectator.ViewModels;
    using LcmsSpectator.ViewModels.Modifications;

    using ReactiveUI;

    /// <summary>
    /// Represents the type of precursor ions displayed.
    /// </summary>
    public enum PrecursorViewMode
    {
        /// <summary>
        /// Isotopes of the precursor ion are displayed.
        /// </summary>
        Isotopes,

        /// <summary>
        /// Neighboring charge states of the precursor ion are displayed.
        /// </summary>
        Charges
    }

    /// <summary>
    /// This class contains settings fields for the LcMsSpectator Application.
    /// </summary>
    public class IcParameters: ReactiveObject
    {
        public event Action IcParametersUpdated;

        public static IcParameters Instance
        {
            get { return _instance ?? (_instance = new IcParameters()); }
        }

        /// <summary>
        /// Get IonType from IonTypeFactory.
        /// </summary>
        /// <param name="baseIonType">Base ion type of ion type to get.</param>
        /// <param name="neutralLoss">Neutral loss of ion type to get.</param>
        /// <param name="charge">Charge of ion type to get</param>
        /// <returns>IonType with the given base ion type, neutral loss, and charge.</returns>
        public IonType GetIonType(BaseIonType baseIonType, NeutralLoss neutralLoss, int charge)
        {
            var chargeStr = charge.ToString(CultureInfo.InvariantCulture);
            if (charge == 1)
            {
                chargeStr = string.Empty;
            }

            var name = baseIonType.Symbol + chargeStr + neutralLoss.Name;
            return IonTypeFactory.GetIonType(name);
        }

        /// <summary>
        /// Trigger settings updated event.
        /// </summary>
        public void Update()
        {
            if (IcParametersUpdated != null) IcParametersUpdated();
        }

        /// <summary>
        /// Get string containing list of ion types to be highlighted for CID and HCD spectra.
        /// </summary>
        /// <returns>String containing ion type symbols separated by spaces.</returns>
        public string GetCidHcdIonTypes()
        {
            return CidHcdIonTypes.Aggregate(string.Empty, (current, ionType) => current + ionType.Symbol + " ");
        }

        /// <summary>
        /// Get string containing list of ion types to be highlighted for ETD spectra.
        /// </summary>
        /// <returns>String containing ion type symbols separated by spaces.</returns>
        public string GetEtdIonTypes()
        {
            return EtdIonTypes.Aggregate(string.Empty, (current, ionType) => current + ionType.Symbol + " ");
        }

        /// <summary>
        /// Parse a list of base ion type symbols separated by spaces.
        /// </summary>
        /// <param name="str">String containing ion type symbols separated by spaces.</param>
        /// <returns>List of BaseIonTypes representing each iontype in the string.</returns>
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
        /// <summary>
        /// Tolerance used for finding precursor ions in the application.
        /// </summary>
        public Tolerance PrecursorTolerancePpm
        {
            get { return _precursorTolerancePpm; }
            set { this.RaiseAndSetIfChanged(ref _precursorTolerancePpm, value); }
        }

        private Tolerance _productIonTolerance;
        /// <summary>
        /// Tolerance used for finding product ions in the application.
        /// </summary>
        public Tolerance ProductIonTolerancePpm
        {
            get { return _productIonTolerance; }
            set { this.RaiseAndSetIfChanged(ref _productIonTolerance, value); }
        }

        private double _ionCorrelationThreshold;
        /// <summary>
        /// Minimum pearson correlation for ions displayed in MS/MS spectra.
        /// </summary>
        public double IonCorrelationThreshold
        {
            get { return _ionCorrelationThreshold; } 
            set { this.RaiseAndSetIfChanged(ref _ionCorrelationThreshold, value); }
        }

        private int _pointsToSmooth;
        /// <summary>
        /// The default value to use for Savitzky-Golay smoothing in XIC plot smoothing slider.
        /// </summary>
        public int PointsToSmooth
        {
            get { return _pointsToSmooth; } 
            set { this.RaiseAndSetIfChanged(ref _pointsToSmooth, value); }
        }

        private double _precursorRelativeIntensityThreshold;
        /// <summary>
        /// Minimum relative intensity for isotopes of precursor ions displayed. 
        /// </summary>
        public double PrecursorRelativeIntensityThreshold
        {
            get { return _precursorRelativeIntensityThreshold; }
            set { this.RaiseAndSetIfChanged(ref _precursorRelativeIntensityThreshold, value); }
        }

        private bool _showInstrumentData;
        /// <summary>
        /// Toggles whether the charge and mass reported by the instrument be stored in the PrSms.
        /// </summary>
        public bool ShowInstrumentData
        {
            get { return _showInstrumentData; }
            set { this.RaiseAndSetIfChanged(ref _showInstrumentData, value); }
        }

        private PrecursorViewMode _precursorViewMode;
        /// <summary>
        /// The type of precursor ions displayed in the application.
        /// </summary>
        public PrecursorViewMode PrecursorViewMode
        {
            get { return _precursorViewMode; }
            set { this.RaiseAndSetIfChanged(ref _precursorViewMode, value); }
        }

        private List<SearchModification> _searchModifications;
        /// <summary>
        /// List of static modifications used by the application.
        /// </summary>
        public List<SearchModification> SearchModifications
        {
            get { return _searchModifications; } 
            set { this.RaiseAndSetIfChanged(ref _searchModifications, value); }
        }

        private ReactiveList<ModificationViewModel> _lightModifications;
        /// <summary>
        /// Modifications that are applied to the light sequence when ShowHeavy is selected.
        /// </summary>
        public ReactiveList<ModificationViewModel> LightModifications
        {
            get { return _lightModifications; }
            private set { this.RaiseAndSetIfChanged(ref _lightModifications, value); }
        }

        private ReactiveList<ModificationViewModel> _heavyModifications;
        /// <summary>
        /// Modifications that are applied to the heavy sequence when ShowHeavy is selected.
        /// </summary>
        public ReactiveList<ModificationViewModel> HeavyModifications
        {
            get { return _heavyModifications; }
            private set { this.RaiseAndSetIfChanged(ref _heavyModifications, value); }
        }

        private IonTypeFactory _ionTypeFactory;
        /// <summary>
        /// Factory for generating flyweight ion types.
        /// </summary>
        public IonTypeFactory IonTypeFactory
        {
            get { return _ionTypeFactory; }
            private set { this.RaiseAndSetIfChanged(ref _ionTypeFactory, value); }
        }

        private IonTypeFactory _deconvolutedIonTypeFactory;
        /// <summary>
        /// Factory for generating flyweight deconvoluted ion types.
        /// </summary>
        public IonTypeFactory DeconvolutedIonTypeFactory
        {
            get { return _deconvolutedIonTypeFactory; }
            private set { this.RaiseAndSetIfChanged(ref _deconvolutedIonTypeFactory, value); }
        }

        private bool _automaticallySelectIonTypes;
        /// <summary>
        /// Toggles whether or not ion type should automatically be selected base on the activation method
        /// of the currently select MS/MS spectrum.
        /// </summary>
        public bool AutomaticallySelectIonTypes
        {
            get { return _automaticallySelectIonTypes; }
            set { this.RaiseAndSetIfChanged(ref _automaticallySelectIonTypes, value); }
        }

        private List<BaseIonType> _cidBaseIonTypes;
        /// <summary>
        /// Ion types that are automatically selected when the activation method of the 
        /// selected MS/MS spectrum is CID or HCD.
        /// </summary>
        public List<BaseIonType> CidHcdIonTypes
        {
            get { return _cidBaseIonTypes; }
            set { this.RaiseAndSetIfChanged(ref _cidBaseIonTypes, value); }
        }

        private List<BaseIonType> _etdIonTypes;
        /// <summary>
        /// Ion types that are automatically selected when the activation method of the 
        /// selected MS/MS spectrum is ETD.
        /// </summary>
        public List<BaseIonType> EtdIonTypes
        {
            get { return _etdIonTypes; }
            set { this.RaiseAndSetIfChanged(ref _etdIonTypes, value); }
        }

        public int ExportImageDpi { get; set; }

        /// <summary>
        /// Modifications, containing location and residue, that are registered with LcMsSpectator.
        /// </summary>
        public ReactiveList<Modification> RegisteredModifications { get; private set; }

        /// <summary>
        /// Register a new modification with the application given an empirical formula.
        /// </summary>
        /// <param name="modName">Name of modification.</param>
        /// <param name="composition">Empirical formula of modification.</param>
        /// <returns>The modification that was registered with the application.</returns>
        public Modification RegisterModification(string modName, Composition composition)
        {
            var mod = Modification.RegisterAndGetModification(modName, composition);
            this.RegisteredModifications.Add(mod);
            return mod;
        }

        /// <summary>
        /// Register a new modification with the application given a delta mass shift.
        /// </summary>
        /// <param name="modName">Name of modification.</param>
        /// <param name="mass">Delta mass of modification.</param>
        /// <returns>The modification that was registered with the application.</returns>
        public Modification RegisterModification(string modName, double mass)
        {
            var mod = Modification.RegisterAndGetModification(modName, mass);
            this.RegisteredModifications.Add(mod);
            return mod;
        }

        /// <summary>
        /// Update or register a modification.
        /// </summary>
        /// <param name="modName">The name of the modification.</param>
        /// <param name="composition">The composition of the modification.</param>
        /// <returns>The registered modification.</returns>
        public Modification UpdateOrRegisterModification(string modName, Composition composition)
        {
            var mod = Modification.UpdateAndGetModification(modName, composition);
            var regMod = this.RegisteredModifications.FirstOrDefault(m => m.Name == modName);
            if (regMod != null)
            {
                this.RegisteredModifications.Remove(regMod);
            }

            this.RegisteredModifications.Add(mod);
            return mod;
        }

        /// <summary>
        /// Update or register a modification.
        /// </summary>
        /// <param name="modName">The name of the modification.</param>
        /// <param name="mass">The mass of the modification.</param>
        /// <returns>The registered modification.</returns>
        public Modification UpdateOrRegisterModification(string modName, double mass)
        {
            var mod = Modification.UpdateAndGetModification(modName, mass);
            var regMod = this.RegisteredModifications.FirstOrDefault(m => m.Name == modName);
            if (regMod != null)
            {
                this.RegisteredModifications.Remove(regMod);
            }

            this.RegisteredModifications.Add(mod);
            return mod;
        }

        /// <summary>
        /// Unregister a modification.
        /// </summary>
        /// <param name="modification">The modification to unregister.</param>
        public void UnregisterModification(Modification modification)
        {
            Modification.UnregisterModification(modification);
            if (this.RegisteredModifications.Contains(modification))
            {
                this.RegisteredModifications.Remove(modification);
            }
        }

        /// <summary>
        /// Instantiate default values of LcMsSpectator application settings.
        /// </summary>
        private IcParameters()
        {
            PrecursorTolerancePpm = new Tolerance(10, ToleranceUnit.Ppm);
            ProductIonTolerancePpm = new Tolerance(10, ToleranceUnit.Ppm);
            IonCorrelationThreshold = 0.7;
            PointsToSmooth = 9;
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

            RegisteredModifications = new ReactiveList<Modification>(Modification.CommonModifications);

            ExportImageDpi = 96;
        }

        private static IcParameters _instance;
    }
}
