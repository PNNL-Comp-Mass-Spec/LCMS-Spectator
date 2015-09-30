namespace LcmsSpectator.ViewModels.Settings
{
    using System.Collections;
    using System.Collections.Generic;

    using InformedProteomics.Backend.Data.Spectrometry;
    using LcmsSpectator.Config;
    using LcmsSpectator.ViewModels.Dataset;

    using ReactiveUI;
    
    /// <summary>
    /// View model for tolerance settings.
    /// </summary>
    public class ToleranceSettingsViewModel : BaseSettingsViewModel
    {
        /// <summary>
        /// The precursor ion tolerance value.
        /// </summary>
        private double precursorTolerance;

        /// <summary>
        /// The precursor ion tolerance unit.
        /// </summary>
        private ToleranceUnit precursorToleranceUnit;

        /// <summary>
        /// The product ion tolerance value.
        /// </summary>
        private double productTolerance;

        /// <summary>
        /// The product ion tolerance unit.
        /// </summary>
        private ToleranceUnit productIonToleranceUnit;

        /// <summary>
        /// The minimum pearson correlation for ions displayed in MS/MS spectra.
        /// </summary>
        private double ionCorrelationThreshold;

        /// <summary>
        /// The minimum relative intensity for isotopes of precursor ions displayed. 
        /// </summary>
        private double precursorRelativeIntensityThreshold;

        public ToleranceSettingsViewModel(ToleranceSettings toleranceSettings, IEnumerable<DatasetViewModel> datasets) : base(datasets)
        {
            this.ToleranceUnits = new ReactiveList<ToleranceUnit> { ToleranceUnit.Ppm, ToleranceUnit.Th };

            this.PrecursorTolerance = toleranceSettings.PrecursorTolerance;
            this.PrecursorToleranceUnit = toleranceSettings.PrecursorToleranceUnit;
            this.ProductTolerance = toleranceSettings.ProductTolerance;
            this.ProductIonToleranceUnit = toleranceSettings.ProductIonToleranceUnit;
            this.IonCorrelationThreshold = toleranceSettings.IonCorrelationThreshold;
            this.PrecursorRelativeIntensityThreshold = toleranceSettings.PrecursorRelativeIntensityThreshold;
        }

        /// <summary>
        /// Gets or sets a list of possible tolerance units.
        /// </summary>
        public ReactiveList<ToleranceUnit> ToleranceUnits { get; private set; }

        /// <summary>
        /// Gets the tolerance settings model for this view model.
        /// </summary>
        public ToleranceSettings ToleranceSettings
        {
            get
            {
                return new ToleranceSettings
                {
                    PrecursorTolerance = this.PrecursorTolerance,
                    PrecursorToleranceUnit = this.PrecursorToleranceUnit,
                    ProductTolerance = this.ProductTolerance,
                    ProductIonToleranceUnit = this.ProductIonToleranceUnit,
                    IonCorrelationThreshold = this.IonCorrelationThreshold,
                    PrecursorRelativeIntensityThreshold = this.PrecursorRelativeIntensityThreshold
                };
            }
        }

        /// <summary>
        /// Gets or sets the precursor ion tolerance value.
        /// </summary>
        public double PrecursorTolerance
        {
            get { return this.precursorTolerance; }
            set { this.RaiseAndSetIfChanged(ref this.precursorTolerance, value); }
        }

        /// <summary>
        /// Gets or sets the precursor ion tolerance unit.
        /// </summary>
        public ToleranceUnit PrecursorToleranceUnit
        {
            get { return this.precursorToleranceUnit; }
            set { this.RaiseAndSetIfChanged(ref this.precursorToleranceUnit, value); }
        }

        /// <summary>
        /// Gets or sets the product ion tolerance value.
        /// </summary>
        public double ProductTolerance
        {
            get { return this.productTolerance; }
            set { this.RaiseAndSetIfChanged(ref this.productTolerance, value); }
        }

        /// <summary>
        /// Gets or sets the product ion tolerance unit.
        /// </summary>
        public ToleranceUnit ProductIonToleranceUnit
        {
            get { return this.productIonToleranceUnit; }
            set { this.RaiseAndSetIfChanged(ref this.productIonToleranceUnit, value); }
        }

        /// <summary>
        /// Gets or sets the minimum pearson correlation for ions displayed in MS/MS spectra.
        /// </summary>
        public double IonCorrelationThreshold
        {
            get { return this.ionCorrelationThreshold; }
            set { this.RaiseAndSetIfChanged(ref this.ionCorrelationThreshold, value); }
        }

        /// <summary>
        /// Gets or sets the minimum relative intensity for isotopes of precursor ions displayed. 
        /// </summary>
        public double PrecursorRelativeIntensityThreshold
        {
            get { return this.precursorRelativeIntensityThreshold; }
            set { this.RaiseAndSetIfChanged(ref this.precursorRelativeIntensityThreshold, value); }
        }
    }
}
