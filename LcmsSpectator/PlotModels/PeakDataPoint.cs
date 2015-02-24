using InformedProteomics.Backend.Data.Spectrometry;
using OxyPlot;

namespace LcmsSpectator.PlotModels
{
    public class PeakDataPoint: IDataPoint, IDataPointProvider
    {
        public PeakDataPoint(double x, double y, double error, double correlation, string title)
        {
            X = x;
            Y = y;
            Error = error;
            Correlation = correlation;
            Title = title;
        }

		public DataPoint GetDataPoint()
		{
			return new DataPoint(X, Y);
		}
        public char Residue { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Error { get; set; }
        public double Correlation { get; set; }
        public string Title { get; set; }

        public int Index { get; set; }
        public IonType IonType { get; set; }
    }
}
