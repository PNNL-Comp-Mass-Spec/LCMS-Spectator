﻿using OxyPlot;

namespace LcmsSpectator.PlotModels
{
    public class PeakDataPoint: IDataPoint
    {
        public PeakDataPoint(double x, double y, double error, double correlation)
        {
            X = x;
            Y = y;
            Error = error;
            Correlation = correlation;
        }

        public double X { get; set; }
        public double Y { get; set; }
        public double Error { get; set; }
        public double Correlation { get; set; }
    }
}
