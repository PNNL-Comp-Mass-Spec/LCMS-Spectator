using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LcmsSpectator.ViewModels
{
    using OxyPlot;

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
