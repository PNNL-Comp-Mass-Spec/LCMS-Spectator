using OxyPlot;

namespace LcmsSpectator.ViewModels
{
    class PlotViewModel
    {
        public PlotModel CorrHistogram { get; set; }
        public PlotModel CorrRoc { get; set; }

        public PlotModel ErrorHistogram { get; set; }
        public PlotModel ErrorRoc { get; set; }

        public PlotModel CosineHistogram { get; set; }
        public PlotModel CosineRoc { get; set; }
    }
}
