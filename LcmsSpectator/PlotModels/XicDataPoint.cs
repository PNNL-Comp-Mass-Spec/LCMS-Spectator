using InformedProteomics.Backend.Data.Spectrometry;
using OxyPlot;

namespace LcmsSpectator.PlotModels
{
    public class XicDataPoint: IDataPoint, IDataPointProvider
    {
        public XicDataPoint(double x, int scanNum, double y, int index, string title)
        {
            X = x;
            Y = y;
            ScanNum = scanNum;
            Index = index;
            Title = title;
        }

	    public DataPoint GetDataPoint()
	    {
		    return new DataPoint(X, Y);
	    }

        public double X { get; set; }
        public double Y { get; set; }
        public int ScanNum { get; set; }
        public int Index { get; set; }
        public string Title { get; set; }

        public IonType IonType { get; set; }
    }
}
