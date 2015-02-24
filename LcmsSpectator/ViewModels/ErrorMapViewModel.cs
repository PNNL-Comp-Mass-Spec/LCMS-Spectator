using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.Config;
using LcmsSpectator.PlotModels;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
    public class ErrorMapViewModel: ReactiveObject
    {
        public ErrorMapViewModel()
        {
            PlotModel = new PlotModel();

            _heighMultiplier = 12;

            _xAxis = new LinearAxis
            {
                Title = "Ion Type",
                Position = AxisPosition.Top,
                AbsoluteMinimum = 0,
                Minimum = 0,
                IsZoomEnabled = false,
                IsPanEnabled = false,
                MajorStep = 1.0,
                MinorTickSize = 0,
            };

            _yAxis = new LinearAxis
            {
                Title = "Amino Acid",
                Position = AxisPosition.Left,
                AbsoluteMinimum = 0,
                Minimum = 0,
                IsZoomEnabled = false,
                IsPanEnabled = false,
                MajorStep = 1.0,
                MinorTickSize = 0,
                FontSize = 9
            };

            PlotModel.Axes.Add(_xAxis);
            PlotModel.Axes.Add(_yAxis);

            // When sequence or data points change, update plot
            this.WhenAnyValue(x => x.Sequence, x => x.DataPoints)
                .Throttle(TimeSpan.FromMilliseconds(100), RxApp.TaskpoolScheduler)
                .Where(x => x.Item1 != null && x.Item2 != null)
                .Select(x => GetDataArray(x.Item2))
                .Subscribe(BuildPlotModel);

            // When sequence changes, adjust plot height
            this.WhenAnyValue(x => x.Sequence)
                .Where(sequence => sequence != null && sequence.Count > 0)
                .Select(sequence => sequence.Count*_heighMultiplier)
                .ToProperty(this, x => x.PlotHeight, out _plotHeight, 500);
        }

        #region Public Properties
        /// <summary>
        /// Plot Model for error heat map
        /// </summary>
        public PlotModel PlotModel { get; private set; }

        private readonly ObservableAsPropertyHelper<int> _plotHeight;
        /// <summary>
        /// Height of plot, based on sequence length
        /// </summary>
        public int PlotHeight
        {
            get { return _plotHeight.Value; }
        }

        private int _scan;
        /// <summary>
        /// Scan number of spectrum
        /// </summary>
        public int Scan
        {
            get { return _scan; }
            set { this.RaiseAndSetIfChanged(ref _scan, value); }
        }

        private ReactiveList<PeakDataPoint> _peakDataPoints;
        /// <summary>
        /// The peak data points for the most abundant isotope of each ion
        /// </summary>
        public ReactiveList<PeakDataPoint> DataPoints
        {
            get { return _peakDataPoints; }
            set { this.RaiseAndSetIfChanged(ref _peakDataPoints, value); }
        }

        private Sequence _sequence;
        /// <summary>
        /// The sequence to display
        /// </summary>
        public Sequence Sequence
        {
            get { return _sequence; }
            set { this.RaiseAndSetIfChanged(ref _sequence, value); }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Build error heatmap
        /// </summary>
        /// <param name="data">
        /// Data to be shown on the heatmap.
        /// First dimension is sequence
        /// Second dimension is ion type
        /// </param>
        private void BuildPlotModel(double[,] data)
        {
            var minColor = OxyColor.FromRgb(127, 255, 0);
            var maxColor = OxyColor.FromRgb(255, 0, 0);
            var colorAxis = new LinearColorAxis
            {
                Title = "Error",
                Position = AxisPosition.Right,
                Palette = OxyPalette.Interpolate(1000, new[] { minColor, maxColor }),
                AbsoluteMinimum = 0,
                Minimum = -1*IcParameters.Instance.ProductIonTolerancePpm.GetValue(),
                Maximum = IcParameters.Instance.ProductIonTolerancePpm.GetValue(),
                AbsoluteMaximum = IcParameters.Instance.ProductIonTolerancePpm.GetValue(),
                LowColor = OxyColors.Navy,
            };
            PlotModel.Axes.Add(colorAxis);

            var heatMapSeries = new HeatMapSeries
            {
                //Title = "Error Map",
                Data = data,
                Interpolate = false,
            };
            PlotModel.Series.Add(heatMapSeries);

            heatMapSeries.X0 = 0;
            heatMapSeries.X1 = data.GetLength(1);
            _xAxis.LabelFormatter = x => _ionTypes[Math.Min((int) x, _ionTypes.Length-1)].Name;

            heatMapSeries.Y0 = 0;
            heatMapSeries.Y1 = data.GetLength(0);
            _yAxis.LabelFormatter = y => Sequence[Math.Max(Math.Min((int) y, Sequence.Count-1), 0)]
                                         .Residue.ToString(CultureInfo.InvariantCulture);

            PlotModel.InvalidatePlot(true);
        }

        /// <summary>
        /// Organize the peak data points by ion type
        /// </summary>
        /// <param name="dataPoints">Most abundant isotope peak data point for each ion type</param>
        /// <returns>2d array where first dimension is sequence and second dimension is ion type</returns>
        private double[,] GetDataArray(IEnumerable<PeakDataPoint> dataPoints)
        {
            var dataDict = new Dictionary<IonType, List<double>>();
            foreach (var dataPoint in dataPoints)
            {
                if (!dataDict.ContainsKey(dataPoint.IonType)) dataDict.Add(dataPoint.IonType, new List<double>());
                var points = dataDict[dataPoint.IonType];

                var position = Math.Max(0, Math.Min(dataPoint.Index, points.Count - 1));
                points.Insert(position, dataPoint.Error);
            }

            var data = new double[Sequence.Count, dataDict.Keys.Count];

            _ionTypes = dataDict.Keys.ToArray();
            for (int i = 0; i < Sequence.Count - 1; i++)
            {
                for (int j = 0; j < _ionTypes.Length; j++)
                {
                    var value = dataDict[_ionTypes[j]][i];
                    if (value.Equals(Double.NaN)) value = -1*IcParameters.Instance.ProductIonTolerancePpm.GetValue()-1;
                    data[i, j] = value;
                }
            }
            return data;
        }
        #endregion

        #region Private Members
        private readonly int _heighMultiplier;
        private IonType[] _ionTypes;  
        private readonly LinearAxis _xAxis;
        private readonly LinearAxis _yAxis;
        #endregion
    }
}
