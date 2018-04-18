// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PeakDataPoint.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Data point for a Spectrum point. (M/Z vs Intensity)
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.PlotModels
{
    using System.Collections.Generic;

    using InformedProteomics.Backend.Data.Spectrometry;
    using OxyPlot;

    /// <summary>
    /// Data point for a Spectrum point. (M/Z vs Intensity)
    /// </summary>
    public class PeakDataPoint : IDataPoint, IDataPointProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PeakDataPoint"/> class.
        /// </summary>
        /// <param name="x">The X value.</param>
        /// <param name="y">The Y value.</param>
        /// <param name="error">The peak error.</param>
        /// <param name="correlation">The Pearson correlation for the isotopic envelope this peak is a part of.</param>
        /// <param name="title">The title of this point.</param>
        public PeakDataPoint(double x, double y, double error, double correlation, string title)
        {
            X = x;
            Y = y;
            Error = error;
            Correlation = correlation;
            Title = title;
        }

        /// <summary>
        /// Gets or sets the X value of the data point.
        /// The M/Z value.
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Gets or sets the Y value of the data point.
        /// The Intensity value.
        /// </summary>
        public double Y { get; set; }

        public double MonoisotopicMass { get; set; }

        public double TheoMonoisotopicMass { get; set; }

        /// <summary>
        /// Gets or sets the residue that this peak is associated with.
        /// </summary>
        public char Residue { get; set; }

        /// <summary>
        /// Gets or sets the peak error.
        /// </summary>
        public double Error { get; set; }

        /// <summary>
        /// Gets or sets the Pearson correlation for the isotopic envelope this peak is a part of.
        /// </summary>
        public double Correlation { get; set; }

        /// <summary>
        /// Gets or sets the title of this point.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the isotope index of this point.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the ion type that this point is associated with.
        /// </summary>
        public IonType IonType { get; set; }

        /// <summary>
        /// Get an OxyPlot data point for this PeakDataPoint.
        /// </summary>
        /// <returns>OxyPlot data point.</returns>
        public DataPoint GetDataPoint()
        {
            return new DataPoint(X, Y);
        }

        /// <summary>
        /// Comparison class for comparing two data points by their M/Z values.
        /// </summary>
        public class MzComparer : IComparer<PeakDataPoint>
        {
            /// <summary>
            /// Compares two <see cref="PeakDataPoint" /> objects by their M/Z values.
            /// </summary>
            /// <param name="x">The left data point.</param>
            /// <param name="y">The right data point.</param>
            /// <returns>
            /// A value indicating whether the left is less than, equal to, or greater than the right.
            /// </returns>
            public int Compare(PeakDataPoint x, PeakDataPoint y)
            {
                if (x == null)
                    return -1;

                if (y == null)
                    return 1;

                return x.X.CompareTo(y.X);
            }
        }
    }
}
