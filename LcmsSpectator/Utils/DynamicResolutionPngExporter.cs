// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DynamicResolutionPngExporter.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Exporter for PNG files with dynamic resolution.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Utils
{
    using System;
    using OxyPlot;
    using OxyPlot.Wpf;

    /// <summary>
    /// Exporter for PNG files with dynamic resolution.
    /// </summary>
    public class DynamicResolutionPngExporter
    {
        /// <summary>The export.</summary>
        /// <param name="plotModel">The plot model to export.</param>
        /// <param name="filePath">The output file path.</param>
        /// <param name="suggestedWidth">The suggested width.</param>
        /// <param name="suggestedHeight">The suggested height.</param>
        /// <param name="backgroundColor">The background color.</param>
        /// <param name="dpi">The DPI resolution.</param>
        public static void Export(
                                    PlotModel plotModel,
                                    string filePath,
                                    int suggestedWidth,
                                    int suggestedHeight,
                                    OxyColor backgroundColor,
                                    int dpi)
        {
            var upscaleFactor = dpi / 96.0;
            int width = (int)Math.Ceiling(suggestedWidth * upscaleFactor);
            int height = (int)Math.Ceiling(suggestedHeight * upscaleFactor);

            PngExporter.Export(plotModel, filePath, width, height, backgroundColor, dpi);
        }
    }
}
