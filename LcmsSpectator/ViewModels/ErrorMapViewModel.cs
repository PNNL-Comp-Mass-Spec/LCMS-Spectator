using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.Config;
using LcmsSpectator.DialogServices;
using LcmsSpectator.PlotModels;
using LcmsSpectator.Utils;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Wpf;
using ReactiveUI;
using HeatMapSeries = OxyPlot.Series.HeatMapSeries;
using LinearAxis = OxyPlot.Axes.LinearAxis;
using LinearColorAxis = OxyPlot.Axes.LinearColorAxis;

namespace LcmsSpectator.ViewModels
{
    public class ErrorMapViewModel: ReactiveObject
    {
        /// <summary>
        /// Create new view model that maintains a heatmap showing sequence vs ion type vs error.
        /// </summary>
        public ErrorMapViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
            PlotModel = new PlotModel { Title = "Error Map", PlotAreaBackground = OxyColors.Navy };

            // Init error map axes
            _xAxis = new LinearAxis
            {
                Title = "Amino Acid",
                Position = AxisPosition.Top,
                AbsoluteMinimum = 0,
                Minimum = 0,
                MajorTickSize = 0,
                MinorStep = 2,
                Angle = -90,
                MinorTickSize = 10,
                MaximumPadding = 0,
                FontSize = 10
            };
            PlotModel.Axes.Add(_xAxis);

            _yAxis = new LinearAxis
            {
                Title = "Ion Type",
                Position = AxisPosition.Left,
                AbsoluteMinimum = 0,
                Minimum = 0,
                MajorStep = 1.0,
                MajorTickSize = 0,
                MinorStep = 0.5,
                MinorTickSize = 20,
                MaximumPadding = 0,
                FontSize = 10
            };
            PlotModel.Axes.Add(_yAxis);

            _colorAxis = new LinearColorAxis
            {
                Title = "Error",
                Position = AxisPosition.Right,
                AbsoluteMinimum = 0,
                Minimum = -1 * IcParameters.Instance.ProductIonTolerancePpm.GetValue(),
                Maximum = IcParameters.Instance.ProductIonTolerancePpm.GetValue(),
                AbsoluteMaximum = IcParameters.Instance.ProductIonTolerancePpm.GetValue(),
                LowColor = OxyColors.Navy,
            };
            PlotModel.Axes.Add(_colorAxis);

            // Save As Image Command requests a file path from the user and then saves the error map as an image
            var saveAsImageCommand = ReactiveCommand.Create();
            saveAsImageCommand.Subscribe(_ => SaveAsImageImpl());
            SaveAsImageCommand = saveAsImageCommand;
        }

        /// <summary>
        /// Plot Model for error heat map
        /// </summary>
        public PlotModel PlotModel { get; private set; }

        /// <summary>
        /// Prompt user for file path and save plot as image.
        /// </summary>
        public IReactiveCommand SaveAsImageCommand { get; private set; }

        private ReactiveList<PeakDataPoint> _dataTable;
        /// <summary>
        /// The data that is shown in the "Table" view. This excludes any fragments without data.
        /// </summary>
        public ReactiveList<PeakDataPoint> DataTable
        {
            get { return _dataTable; }
            set { this.RaiseAndSetIfChanged(ref _dataTable, value); }
        }

        /// <summary>
        /// Set sequence and data displayed on heat map.
        /// </summary>
        /// <param name="sequence">The sequence to display as the x axis of the plot.</param>
        /// <param name="peakDataPoints">The peak data points to extract error values from.</param>
        public void SetData(Sequence sequence, IEnumerable<IList<PeakDataPoint>> peakDataPoints)
        {
            if (sequence == null || peakDataPoints == null) return;

            // Remove all points except for most abundant isotope peaks
            var dataPoints = GetMostAbundantIsotopePeaks(peakDataPoints).ToArray();

            // No data, nothing to do
            if (dataPoints.Length == 0) return;

            // Remove NaN values for data table (only showing fragment ions found in spectrum in data table)
            DataTable = new ReactiveList<PeakDataPoint>(dataPoints.Where(dp => !dp.Error.Equals(Double.NaN)));

            // Build and invalidate erorr map plot display
            BuildPlotModel(sequence, GetDataArray(dataPoints));
        }

        /// <summary>
        /// Build error heatmap
        /// </summary>
        /// <param name="sequence">The sequence to display as x axis on the plot</param>
        /// <param name="data">
        /// Data to be shown on the heatmap.
        /// First dimension is sequence
        /// Second dimension is ion type
        /// </param>
        private void BuildPlotModel(IReadOnlyList<AminoAcid> sequence, double[,] data)
        {
            // initialize color axis
            var minColor = OxyColor.FromRgb(127, 255, 0);
            var maxColor = OxyColor.FromRgb(255, 0, 0);
            _colorAxis.Palette = OxyPalette.Interpolate(1000, minColor, maxColor);
            _colorAxis.Minimum = -1*IcParameters.Instance.ProductIonTolerancePpm.GetValue();
            _colorAxis.Maximum = IcParameters.Instance.ProductIonTolerancePpm.GetValue();
            _colorAxis.AbsoluteMaximum = IcParameters.Instance.ProductIonTolerancePpm.GetValue();
            _colorAxis.LowColor = OxyColors.Navy;

            PlotModel.Series.Clear();

            // initialize heat map
            var heatMapSeries = new HeatMapSeries
            {
                Data = data,
                Interpolate = false,
                X0 = 1,
                X1 = data.GetLength(0),
                Y0 = 1,
                Y1 = data.GetLength(1),
                TrackerFormatString = 
                        "{1}: {2:0}" + Environment.NewLine +
                        "{3}: {4:0}" + Environment.NewLine +
                        "{5}: {6:0.###}ppm",
            };
            PlotModel.Series.Add(heatMapSeries);

            _xAxis.AbsoluteMaximum = sequence.Count;
            _yAxis.AbsoluteMaximum = _ionTypes.Length;

            // Set yAxis double -> string label converter function
            _yAxis.LabelFormatter = y =>
            {
                if (y.Equals(0)) return " ";
                var ionType = _ionTypes[Math.Min((int) y - 1, _ionTypes.Length - 1)];
                return String.Format("{0}({1}+)", ionType.BaseIonType.Symbol, ionType.Charge);
            };

            // Set xAxis double -> string label converter function
            _xAxis.LabelFormatter = x => x.Equals(0) ? " " : String.Format("{0}{1}", sequence[Math.Max(Math.Min((int)x - 1, sequence.Count - 1), 0)]
                                         .Residue.ToString(CultureInfo.InvariantCulture), (int)x);

            // Update plot
            PlotModel.InvalidatePlot(true);
        }

        /// <summary>
        /// Organize the peak data points by ion type
        /// </summary>
        /// <returns>2d array where first dimension is sequence and second dimension is ion type</returns>
        private double[,] GetDataArray(IEnumerable<PeakDataPoint> dataPoints)
        {
            var dataDict = new Dictionary<IonType, List<double>>();

            // partition data set by ion type
            foreach (var dataPoint in dataPoints)
            {
                if (!dataDict.ContainsKey(dataPoint.IonType)) dataDict.Add(dataPoint.IonType, new List<double>());
                var points = dataDict[dataPoint.IonType];

                int index = dataPoint.Index+1;

                if (!dataPoint.IonType.IsPrefixIon) index = points.Count - (dataPoint.Index);

                var position = Math.Max(0, Math.Min(index, points.Count));
                points.Insert(position, dataPoint.Error);
            }

            var seqLength = dataDict.Values.Max(v => v.Count);

            _ionTypes = dataDict.Keys.ToArray();

            var data = new double[seqLength, dataDict.Keys.Count];

            // create two dimensional array from partitioned data
            for (int i = 0; i < seqLength; i++)
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

        /// <summary>
        /// Get list of only most abundant isotope peaks of the ion peak data points
        /// Associate residue with the sequence
        /// </summary>
        /// <param name="peakDataPoints">Peak data points for ions on the spectrum plot</param>
        /// <returns>List of most abundant isotope peak data points</returns>
        private IEnumerable<PeakDataPoint> GetMostAbundantIsotopePeaks(IEnumerable<IList<PeakDataPoint>> peakDataPoints)
        {
            var mostAbundantPeaks = new ReactiveList<PeakDataPoint>();
            foreach (var peaks in peakDataPoints)
            {
                var peak = peaks.OrderByDescending(p => p.Y).FirstOrDefault();
                if (peak != null && peak.IonType != null && peak.IonType.Name != "Precursor")
                {
                    mostAbundantPeaks.Add(peak);
                }
            }
            return mostAbundantPeaks;
        }

        /// <summary>
        /// Prompt user for file path and save plot as image to that path.
        /// </summary>
        private void SaveAsImageImpl()
        {
            var fileName = _dialogService.SaveFile(".png", @"Png Files (*.png)|*.png");
            try
            {
                if (!String.IsNullOrEmpty(fileName)) throw new FormatException(String.Format("Cannot save image due to invalid file name: {0}", fileName));
                DynamicResolutionPngExporter.Export(PlotModel, fileName, (int)PlotModel.Width, (int)PlotModel.Height, OxyColors.White, IcParameters.Instance.ExportImageDpi);
            }
            catch (Exception e)
            {
                _dialogService.ExceptionAlert(e);
            }
        }

        private readonly IDialogService _dialogService;
        private IonType[] _ionTypes;  
        private readonly LinearAxis _xAxis;
        private readonly LinearAxis _yAxis;
        private readonly LinearColorAxis _colorAxis;
    }
}
