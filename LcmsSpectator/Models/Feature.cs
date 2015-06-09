// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Feature.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Represents a feature over a LC retention time and charge state range.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Models
{
    using System.Collections.Generic;
    using InformedProteomics.Backend.Data.Biology;
    using InformedProteomics.Backend.Data.Spectrometry;

    /// <summary>
    /// Represents a feature over a LC retention time and charge state range.
    /// </summary>
    public class Feature
    {
        /// <summary>
        /// The point for the lowest retention time of the feature.
        /// </summary>
        private FeaturePoint minPoint;

        /// <summary>
        /// The point for the highest retention time of the feature.
        /// </summary>
        private FeaturePoint maxPoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="Feature"/> class.
        /// </summary>
        public Feature()
        {
            this.AssociatedPrSms = new List<PrSm>();
            this.AssociatedMs2 = new List<int>();
        }

        /// <summary>
        /// Gets or sets the point for the lowest retention time of the feature.
        /// </summary>
        public FeaturePoint MinPoint
        {
            get
            {
                return this.minPoint;
            }

            set
            {
                this.minPoint = value;
                this.minPoint.Feature = this;
            }
        }

        /// <summary>
        /// Gets or sets the point for the highest retention time of the feature.
        /// </summary>
        public FeaturePoint MaxPoint
        {
            get
            {
                return this.maxPoint;
            }

            set
            {
                this.maxPoint = value;
                this.maxPoint.Feature = this;
            }
        }

        /// <summary>
        /// Gets or sets the ID of the feature.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets the list of MS/MS IDs associated with this feature.
        /// </summary>
        public List<PrSm> AssociatedPrSms { get; private set; }

        /// <summary>
        /// Gets the list of MS/MS scan number associated with this feature.
        /// </summary>
        public List<int> AssociatedMs2 { get; private set; }
        
        /// <summary>
        /// Represents a single LC retention time point for a MS1 feature.
        /// </summary>
        public class FeaturePoint
        {
            /// <summary>
            /// Gets or sets the ID of the feature.
            /// </summary>
            public int Id { get; set; }

            /// <summary>
            /// Gets or sets the feature that this point belongs to.
            /// </summary>
            public Feature Feature { get; set; }

            /// <summary>
            /// Gets or sets the MS1 scan number.
            /// </summary>
            public int Scan { get; set; }

            /// <summary>
            /// Gets or sets the LC retention time.
            /// </summary>
            public double RetentionTime { get; set; }

            /// <summary>
            /// Gets or sets the ion associated with the feature point.
            /// </summary>
            public Ion Ion { get; set; }

            /// <summary>
            /// Gets or sets the mass of feature.
            /// </summary>
            public double Mass { get; set; }

            /// <summary>
            /// Gets or sets the charge of the feature point.
            /// </summary>
            public int Charge { get; set; }

            /// <summary>
            /// Gets or sets the mass-to-charge ratio of the feature point.
            /// </summary>
            public double Mz { get; set; }

            /// <summary>
            /// Gets or sets the abundance of the feature.
            /// </summary>
            public double Abundance { get; set; }

            /// <summary>
            /// Gets or sets the score of the feature.
            /// </summary>
            public double Score { get; set; }

            /// <summary>
            /// Gets or sets the actual isotopic profile for the feature.
            /// </summary>
            public Isotope[] Isotopes { get; set; }

            /// <summary>
            /// Gets or sets the Pearson correlation of the actual isotopic profile to the theoretical.
            /// </summary>
            public double Correlation { get; set; }
        }
    }
}
