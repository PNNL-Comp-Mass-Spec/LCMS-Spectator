using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms.VisualStyles;
using OxyPlot.Series;

namespace LcmsSpectator.PlotModels
{
    public class XicPointSeries: LineSeries, IDataPointSeries
    {
        public XicPointSeries()
        {
            _plotModelLock = new ReaderWriterLockSlim();
            Index = 0;
        }

        public int Index { get; set; }

        public double GetArea(double min, double max)
        {
            if (ItemsSource != null) return GetAreaItemsSource(min, max);
            if (ActualPoints == null) return 0;
            _plotModelLock.EnterReadLock();
            var area =  ActualPoints.Where(point => point.X >= min && point.X <= max).Sum(point => point.Y);
            _plotModelLock.ExitReadLock();
            return area;
        }

        public double GetMaxYInRange(double minX, double maxX)
        {
            if (ItemsSource != null) return GetMaxYInRangeItemsSource(minX, maxX);
            double maxY = 0.0;
            var dataPoints = ActualPoints;
            _plotModelLock.EnterReadLock();
            if (dataPoints != null && dataPoints.Count > 0 && IsVisible)
            {
                foreach (var point in dataPoints)
                {
                    if (point.X >= minX && point.X <= maxX && point.Y >= maxY)
                        maxY = point.Y;
                }
            }
            _plotModelLock.ExitReadLock();
            return maxY;
        }

        private double GetAreaItemsSource(double minX, double maxX)
        {
            return (from object item in ItemsSource 
                           select item as IDataPoint into point 
                           where point != null && point.X >= minX && point.X <= maxX 
                           select point.Y).Sum();
        }

        private double GetMaxYInRangeItemsSource(double minX, double maxX)
        {
            double maxY = 0.0;
            foreach (var item in ItemsSource)
            {
                var point =  item as IDataPoint;
                if (point != null && point.X >= minX && point.X <= maxX && point.Y >= maxY)
                    maxY = point.Y;
            }
            return maxY;
        }

        protected override void UpdateAxisMaxMin()
        {
            _plotModelLock.EnterWriteLock();
            base.UpdateAxisMaxMin();
            _plotModelLock.ExitWriteLock();
        }

        protected override void UpdateData()
        {
            _plotModelLock.EnterWriteLock();
            base.UpdateData();
            _plotModelLock.ExitWriteLock();
        }

        private readonly ReaderWriterLockSlim _plotModelLock;
    }
}
