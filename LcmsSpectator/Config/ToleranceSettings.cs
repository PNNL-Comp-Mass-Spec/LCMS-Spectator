using InformedProteomics.Backend.Data.Spectrometry;

namespace LcmsSpectator.Config
{
    public class ToleranceSettings
    {
        public ToleranceSettings()
        {
            this.PrecursorTolerance = 10;
            this.PrecursorToleranceUnit = ToleranceUnit.Ppm;

            this.ProductTolerance = 10;
            this.ProductIonToleranceUnit = ToleranceUnit.Ppm;
            
            this.IonCorrelationThreshold = 0.7;
            this.PrecursorRelativeIntensityThreshold = 0.1;
        }

        /// <summary>
        /// Gets or sets the precursor ion tolerance value.
        /// </summary>
        public double PrecursorTolerance { get; set; }

        /// <summary>
        /// Gets or sets the precursor ion tolerance unit.
        /// </summary>
        public ToleranceUnit PrecursorToleranceUnit { get; set; }

        /// <summary>
        /// Gets or sets the product ion tolerance value.
        /// </summary>
        public double ProductTolerance { get; set; }

        /// <summary>
        /// Gets or sets the product ion tolerance unit.
        /// </summary>
        public ToleranceUnit ProductIonToleranceUnit { get; set; }

        /// <summary>
        /// Gets or sets the minimum pearson correlation for ions displayed in MS/MS spectra.
        /// </summary>
        public double IonCorrelationThreshold { get; set; }

        /// <summary>
        /// Gets or sets the minimum relative intensity for isotopes of precursor ions displayed. 
        /// </summary>
        public double PrecursorRelativeIntensityThreshold { get; set; }

        /// <summary>
        /// Gets the precursor ion tolerance as a <see cref="Tolerance" />.
        /// </summary>
        /// <returns>The <see cref="Tolerance" />.</returns>
        public Tolerance GetPrecursorTolerance()
        {
            return new Tolerance(this.PrecursorTolerance, this.PrecursorToleranceUnit);
        }

        /// <summary>
        /// Gest the product ion tolerance as a <see cref="Tolerance" />.
        /// </summary>
        /// <returns>The <see cref="Tolerance" />.</returns>
        public Tolerance GetProductTolerance()
        {
            return new Tolerance(this.ProductTolerance, this.PrecursorToleranceUnit);
        }
    }
}
