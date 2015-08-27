// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Feature.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Represents a feature over a LC retention time and charge state range.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Drawing;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Spectrometry;
using QuadTreeLib;

namespace LcmsSpectator.Models.DTO
{
    /// <summary>
    /// Represents a feature over a LC retention time and charge state range.
    /// </summary>
    public class Feature : IHasRect
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
        /// <param name="minPoint">The point for the lowest retention time of the feature.</param>
        /// <param name="maxPoint">The point for the highest retention time of the feature.</param>
        public Feature(FeaturePoint minPoint, FeaturePoint maxPoint)
        {
            this.MinPoint = minPoint;
            this.MaxPoint = maxPoint;
            this.AssociatedPrSms = new List<PrSm>();
            this.AssociatedMs2 = new List<int>();
            this.Rectangle = new RectangleF
            {
                X = (float)this.MinPoint.RetentionTime,
                Y = (float)this.MinPoint.Mass,
                Height = 1,
                Width = (float)(this.MaxPoint.RetentionTime - this.MinPoint.RetentionTime)
            };
        }

        /// <summary>
        /// Gets the point for the lowest retention time of the feature.
        /// </summary>
        public FeaturePoint MinPoint
        {
            get
            {
                return this.minPoint;
            }

            private set
            {
                this.minPoint = value;
                this.minPoint.Feature = this;
            }
        }

        /// <summary>
        /// Gets the point for the highest retention time of the feature.
        /// </summary>
        public FeaturePoint MaxPoint
        {
            get
            {
                return this.maxPoint;
            }

            private set
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
        /// Gets the geometric rectangle representation for this feature.
        /// </summary>
        public RectangleF Rectangle { get; private set; }
        
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
