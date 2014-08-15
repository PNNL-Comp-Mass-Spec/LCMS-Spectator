using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OxyPlot;

namespace LcmsSpectator.PlotModels
{
    public class XicDataPoint: IDataPoint
    {
        public XicDataPoint(double x, int scanNum, double y)
        {
            X = x;
            Y = y;
            ScanNum = scanNum;
        }

        public double X { get; set; }
        public double Y { get; set; }
        public int ScanNum { get; set; }
    }
}
