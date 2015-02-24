using System;
using System.Collections.Generic;
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

            this.WhenAnyValue(x => x.Sequence)
                .Where(sequence => sequence != null && sequence.Count > 0)
                .Select(sequence => sequence.Count*_heighMultiplier)
                .ToProperty(this, x => x.PlotHeight, out _plotHeight, 500);
        }

        #region Public Properties
        public PlotModel PlotModel { get; private set; }

        private ObservableAsPropertyHelper<int> _plotHeight;
        public int PlotHeight
        {
            get { return _plotHeight.Value; }
        }

        private int _scan;
        public int Scan
        {
            get { return _scan; }
            set { this.RaiseAndSetIfChanged(ref _scan, value); }
        }

        private ReactiveList<PeakDataPoint> _peakDataPoints; 
        public ReactiveList<PeakDataPoint> DataPoints
        {
            get { return _peakDataPoints; }
            set { this.RaiseAndSetIfChanged(ref _peakDataPoints, value); }
        }

        private Sequence _sequence;
        public Sequence Sequence
        {
            get { return _sequence; }
            set { this.RaiseAndSetIfChanged(ref _sequence, value); }
        }
        #endregion

        private void BuildPlotModel(double[,] data)
        {
            var minColor = OxyColor.FromRgb(127, 255, 0);
            var maxColor = OxyColor.FromRgb(255, 0, 0);
            var colorAxis = new LinearColorAxis
            {
                //Title = "Error",
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
            _yAxis.LabelFormatter = y => Sequence[Math.Max(Math.Min((int) y, Sequence.Count-1), 0)].Residue.ToString();

            PlotModel.InvalidatePlot(true);
        }

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

        private readonly int _heighMultiplier;
        private IonType[] _ionTypes;  
        private readonly LinearAxis _xAxis;
        private readonly LinearAxis _yAxis;
    }
}
