using System;
using OxyPlot;
using OxyPlot.Wpf;

namespace LcmsSpectator.Utils
{
    public class DynamicResolutionPngExporter
    {
        public static void Export(PlotModel plotModel, string fileName,
            int suggestedWidth, int suggestedHeight, OxyColor backgroundColor, int dpi)
        {
            var upscaleFactor = dpi/96.0;
            int width = (int)Math.Ceiling(suggestedWidth*upscaleFactor);
            int height = (int)Math.Ceiling(suggestedHeight*upscaleFactor);

            PngExporter.Export(plotModel, fileName, width, height, backgroundColor, dpi);
        }
    }
}
