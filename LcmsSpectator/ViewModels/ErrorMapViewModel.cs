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
        /// <summary>
        /// Create new view model that maintains a heatmap showing sequence vs ion type vs error.
        /// </summary>
        public ErrorMapViewModel()
        {
            PlotModel = new PlotModel { Title = "Error Map", PlotAreaBackground = OxyColors.Navy };

            _heighMultiplier = 15;

            _xAxis = new LinearAxis
            {
                Title = "Amino Acid",
                Position = AxisPosition.Top,
                AbsoluteMinimum = 0,
                Minimum = 0,
                IsZoomEnabled = false,
                IsPanEnabled = false,
                MajorStep = 1.0,
                MajorTickSize = 0,
                MinorStep = 0.5,
                MinorTickSize = 20,
                MaximumPadding = 0,
            };

            _yAxis = new LinearAxis
            {
                Title = "Ion Type",
                Position = AxisPosition.Left,
                AbsoluteMinimum = 0,
                Minimum = 0,
                IsZoomEnabled = false,
                IsPanEnabled = false,
                MajorStep = 1.0,
                MajorTickSize = 0,
                MinorStep = 0.5,
                MinorTickSize = 20,
                MaximumPadding = 0,
            };

            PlotModel.Axes.Add(_xAxis);
            PlotModel.Axes.Add(_yAxis);

            // When sequence or data points change, update plot
            this.WhenAnyValue(x => x.Sequence, x => x.DataPoints)
                .Where(x => x.Item1 != null && x.Item2 != null)
                .Throttle(TimeSpan.FromSeconds(1), RxApp.TaskpoolScheduler)
                .Select(_ => GetDataArray())
                .Subscribe(BuildPlotModel);

            // When data points change, update data table
            this.WhenAnyValue(x => x.DataPoints)
                .Throttle(TimeSpan.FromSeconds(1), RxApp.TaskpoolScheduler)
                .Select(dataPoints => dataPoints.Where(dp => !dp.Error.Equals(Double.NaN)))
                .Select(filteredTable => new ReactiveList<PeakDataPoint>(filteredTable))
                .ToProperty(this, x => x.DataTable, out _dataTable, new ReactiveList<PeakDataPoint>());

            // When sequence changes, adjust plot height
            this.WhenAnyValue(x => x.Sequence)
                .Where(sequence => sequence != null && sequence.Count > 0)
                .Throttle(TimeSpan.FromSeconds(1), RxApp.TaskpoolScheduler)
                .Select(sequence => sequence.Count*_heighMultiplier)
                .ToProperty(this, x => x.PlotWidth, out _plotWidth, 500);
        }

        #region Public Properties
        /// <summary>
        /// Plot Model for error heat map
        /// </summary>
        public PlotModel PlotModel { get; private set; }

        private readonly ObservableAsPropertyHelper<int> _plotWidth;
        /// <summary>
        /// Width of plot, based on sequence length
        /// </summary>
        public int PlotWidth
        {
            get { return _plotWidth.Value; }
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

        private readonly ObservableAsPropertyHelper<ReactiveList<PeakDataPoint>> _dataTable;
        /// <summary>
        /// The data that is shown in the "Table" view. This excludes any fragments without data.
        /// </summary>
        public ReactiveList<PeakDataPoint> DataTable
        {
            get { return _dataTable.Value; }
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
            // initialize color axis
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

            // initialize heat map
            var heatMapSeries = new HeatMapSeries
            {
                Data = data,
                Interpolate = false,
                X0 = 1,
                X1 = data.GetLength(0)+1,
                Y0 = 1,
                Y1 = data.GetLength(1),
                TrackerFormatString = 
                        "{1}: {2:0}" + Environment.NewLine +
                        "{3}: {4:0}" + Environment.NewLine +
                        "{5}: {6:0.###}ppm",
            };
            PlotModel.Series.Add(heatMapSeries);

            // Set yAxis double -> string label converter function
            _yAxis.LabelFormatter = y =>
            {
                if (y.Equals(0)) return " ";
                var ionType = _ionTypes[Math.Min((int) y - 1, _ionTypes.Length - 1)];
                return String.Format("{0}({1}+)", ionType.BaseIonType.Symbol, ionType.Charge);
            };

            //var revSequence = new Sequence(Sequence);
            //revSequence.Reverse();

            // Set yAxis double -> string label converter function
            _xAxis.LabelFormatter = x => x.Equals(0) ? " " : Sequence[Math.Max(Math.Min((int) x - 1, Sequence.Count-1), 0)]
                                         .Residue.ToString(CultureInfo.InvariantCulture);

            // Update plot
            PlotModel.InvalidatePlot(true);
        }

        /// <summary>
        /// Organize the peak data points by ion type
        /// </summary>
        /// <returns>2d array where first dimension is sequence and second dimension is ion type</returns>
        private double[,] GetDataArray()
        {
            var dataDict = new Dictionary<IonType, List<double>>();

            // partition data set by ion type
            foreach (var dataPoint in DataPoints)
            {
                if (!dataDict.ContainsKey(dataPoint.IonType)) dataDict.Add(dataPoint.IonType, new List<double>());
                var points = dataDict[dataPoint.IonType];

                int index = dataPoint.Index+1;

                if (!dataPoint.IonType.IsPrefixIon) index = points.Count - (dataPoint.Index);

                var position = Math.Max(0, Math.Min(index, points.Count));
                points.Insert(position, dataPoint.Error);
            }

            _ionTypes = dataDict.Keys.ToArray();

            var data = new double[Sequence.Count - 1, dataDict.Keys.Count];

            // create two dimensional array from partitioned data
            for (int i = 0; i < Sequence.Count-1; i++)
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
