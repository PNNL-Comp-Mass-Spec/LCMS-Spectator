// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XicDataPoint.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Data point for a XIC point. (Retention Time vs Intensity)
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.PlotModels
{
    using InformedProteomics.Backend.Data.Spectrometry;
    using OxyPlot;
    
    /// <summary>
    /// Data point for a XIC point. (Retention Time vs Intensity)
    /// </summary>
    public class XicDataPoint : IDataPoint, IDataPointProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XicDataPoint"/> class.
        /// </summary>
        /// <param name="x">The X value. (Retention Time)</param>
        /// <param name="scanNum">The scan number.</param>
        /// <param name="y">The Y Value. (Intensity)</param>
        /// <param name="index">The isotope/charge index.</param>
        /// <param name="title">The title.</param>
        public XicDataPoint(double x, int scanNum, double y, int index, string title)
        {
            this.X = x;
            this.Y = y;
            this.ScanNum = scanNum;
            this.Index = index;
            this.Title = title;
        }

        /// <summary>
        /// Gets or sets the X value of the data point. (Retention Time)
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Gets or sets the X value of the data point. (Intensity)
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Gets or sets the scan number.
        /// </summary>
        public int ScanNum { get; set; }

        /// <summary>
        /// Gets or sets the isotope/charge index.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the ion type that this XIC point is associated with.
        /// </summary>
        public IonType IonType { get; set; }

        /// <summary>
        /// Get an OxyPlot data point for this XICDataPoint.
        /// </summary>
        /// <returns>OxyPlot data point.</returns>
        public DataPoint GetDataPoint()
        {
            return new DataPoint(this.X, this.Y);
        }
    }
}
