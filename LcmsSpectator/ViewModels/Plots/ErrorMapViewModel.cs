// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ErrorMapViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class maintains a heat map plot model showing sequence vs ion type vs error.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels.Plots
{
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
    using OxyPlot.Series;

    using ReactiveUI;

    /// <summary>
    /// This class maintains a heat map plot model showing sequence vs ion type vs error.
    /// </summary>
    public class ErrorMapViewModel : ReactiveObject
    {
        /// <summary>
        /// Dialog service for opening dialogs from view model.
        /// </summary>
        private readonly IDialogService dialogService;

        /// <summary>
        /// Horizontal axis of the error map plot (ion types)
        /// </summary>
        private readonly LinearAxis xaxis;

        /// <summary>
        /// Vertical axis of error map plot (sequence residue)
        /// </summary>
        private readonly LinearAxis yaxis;

        /// <summary>
        /// Color axis of the error map plot (peak error)
        /// </summary>
        private readonly LinearColorAxis colorAxis;

        /// <summary>
        /// Ion types to be displayed on y axis.
        /// </summary>
        private IonType[] ionTypes;

        /// <summary>
        /// The data that is shown in the "Table" view. This excludes any fragments without data.
        /// </summary>
        private ReactiveList<PeakDataPoint> dataTable;

        /// <summary>
        /// Initializes a new instance of the ErrorMapViewModel class. 
        /// </summary>
        /// <param name="dialogService">
        /// The dialog Service.
        /// </param>
        public ErrorMapViewModel(IDialogService dialogService)
        {
            this.dialogService = dialogService;
            this.PlotModel = new PlotModel { Title = "Error Map", PlotAreaBackground = OxyColors.Navy };

            // Init x axis
            this.xaxis = new LinearAxis
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
            this.PlotModel.Axes.Add(this.xaxis);

            // Init Y axis
            this.yaxis = new LinearAxis
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
            this.PlotModel.Axes.Add(this.yaxis);

            // Init Color axis
            var minColor = OxyColor.FromRgb(127, 255, 0);
            var maxColor = OxyColor.FromRgb(255, 0, 0);
            this.colorAxis = new LinearColorAxis
            {
                Title = "Error",
                Position = AxisPosition.Right,
                AbsoluteMinimum = 0,
                Palette = OxyPalette.Interpolate(1000, minColor, maxColor),
                Minimum = -1 * IcParameters.Instance.ProductIonTolerancePpm.GetValue(),
                Maximum = IcParameters.Instance.ProductIonTolerancePpm.GetValue(),
                AbsoluteMaximum = IcParameters.Instance.ProductIonTolerancePpm.GetValue(),
                LowColor = OxyColors.Navy,
            };
            this.PlotModel.Axes.Add(this.colorAxis);

            // Save As Image Command requests a file path from the user and then saves the error map as an image
            var saveAsImageCommand = ReactiveCommand.Create();
            saveAsImageCommand.Subscribe(_ => this.SaveAsImageImpl());
            this.SaveAsImageCommand = saveAsImageCommand;
        }

        /// <summary>
        /// Gets the plot Model for error heat map
        /// </summary>
        public PlotModel PlotModel { get; private set; }

        /// <summary>
        /// Gets a command that prompts user for file path and save plot as image.
        /// </summary>
        public IReactiveCommand SaveAsImageCommand { get; private set; }

        /// <summary>
        /// Gets or sets the data that is shown in the "Table" view. This excludes any fragments without data.
        /// </summary>
        public ReactiveList<PeakDataPoint> DataTable
        {
            get { return this.dataTable; }
            set { this.RaiseAndSetIfChanged(ref this.dataTable, value); }
        }

        /// <summary>
        /// Set sequence and data displayed on heat map.
        /// </summary>
        /// <param name="sequence">The sequence to display as the x axis of the plot.</param>
        /// <param name="peakDataPoints">The peak data points to extract error values from.</param>
        public void SetData(Sequence sequence, IEnumerable<IList<PeakDataPoint>> peakDataPoints)
        {
            if (sequence == null || peakDataPoints == null)
            {
                return;
            }

            // Remove all points except for most abundant isotope peaks
            var dataPoints = this.GetMostAbundantIsotopePeaks(peakDataPoints).ToArray();

            // No data, nothing to do
            if (dataPoints.Length == 0)
            {
                return;
            }

            // Remove NaN values for data table (only showing fragment ions found in spectrum in data table)
            this.DataTable = new ReactiveList<PeakDataPoint>(dataPoints.Where(dp => !dp.Error.Equals(double.NaN)));

            // Build and invalidate erorr map plot display
            this.BuildPlotModel(sequence, this.GetDataArray(dataPoints));
        }

        /// <summary>
        /// Build the error heat map
        /// </summary>
        /// <param name="sequence">The sequence to display as x axis on the plot</param>
        /// <param name="data">
        /// Data to be shown on the heat map.
        /// First dimension is sequence
        /// Second dimension is ion type
        /// </param>
        private void BuildPlotModel(IReadOnlyList<AminoAcid> sequence, double[,] data)
        {
            // initialize color axis
            ////var minColor = OxyColor.FromRgb(127, 255, 0);
            this.colorAxis.Minimum = -1 * IcParameters.Instance.ProductIonTolerancePpm.GetValue();
            this.colorAxis.Maximum = IcParameters.Instance.ProductIonTolerancePpm.GetValue();
            this.colorAxis.AbsoluteMaximum = IcParameters.Instance.ProductIonTolerancePpm.GetValue();

            this.PlotModel.Series.Clear();

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
            this.PlotModel.Series.Add(heatMapSeries);

            this.xaxis.AbsoluteMaximum = sequence.Count;
            this.yaxis.AbsoluteMaximum = this.ionTypes.Length;

            // Set yAxis double -> string label converter function
            this.yaxis.LabelFormatter = y =>
            {
                if (y.Equals(0))
                {
                    return string.Empty;
                }

                var ionType = this.ionTypes[Math.Min((int)y - 1, this.ionTypes.Length - 1)];
                return string.Format("{0}({1}+)", ionType.BaseIonType.Symbol, ionType.Charge);
            };

            // Set xAxis double -> string label converter function
            this.xaxis.LabelFormatter = x =>
            {
                if (x.Equals(0))
                {
                    return string.Empty;
                }

                int sequenceIndex = Math.Max(Math.Min((int)x - 1, sequence.Count - 1), 0);
                string residue = sequence[sequenceIndex].Residue.ToString(CultureInfo.InvariantCulture);
                return string.Format("{0}{1}", residue, (int)x);
            };

            // Update plot
            this.PlotModel.InvalidatePlot(true);
        }

        /// <summary>
        /// Organize the peak data points by ion type
        /// </summary>
        /// <param name="dataPoints">
        /// The data Points.
        /// </param>
        /// <returns>
        /// 2d array where first dimension is sequence and second dimension is ion type
        /// </returns>
        private double[,] GetDataArray(IEnumerable<PeakDataPoint> dataPoints)
        {
            var dataDict = new Dictionary<IonType, List<double>>();

            // partition data set by ion type
            foreach (var dataPoint in dataPoints)
            {
                if (!dataDict.ContainsKey(dataPoint.IonType))
                {
                    dataDict.Add(dataPoint.IonType, new List<double>());
                }

                var points = dataDict[dataPoint.IonType];

                int index = dataPoint.Index + 1;

                if (!dataPoint.IonType.IsPrefixIon)
                {
                    index = points.Count - dataPoint.Index;
                }

                var position = Math.Max(0, Math.Min(index, points.Count));
                points.Insert(position, dataPoint.Error);
            }

            var seqLength = dataDict.Values.Max(v => v.Count);

            this.ionTypes = dataDict.Keys.ToArray();

            var data = new double[seqLength, dataDict.Keys.Count];

            // create two dimensional array from partitioned data
            for (int i = 0; i < seqLength; i++)
            {
                for (int j = 0; j < this.ionTypes.Length; j++)
                {
                    var value = dataDict[this.ionTypes[j]][i];

                    if (value.Equals(double.NaN))
                    {
                        value = (-1 * IcParameters.Instance.ProductIonTolerancePpm.GetValue()) - 1;
                    }

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
            var fileName = this.dialogService.SaveFile(".png", @"Png Files (*.png)|*.png");
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    throw new FormatException(
                        string.Format("Cannot save image due to invalid file name: {0}", fileName));
                }

                DynamicResolutionPngExporter.Export(
                    this.PlotModel,
                    fileName,
                    (int)this.PlotModel.Width,
                    (int)this.PlotModel.Height,
                    OxyColors.White,
                    IcParameters.Instance.ExportImageDpi);
            }
            catch (Exception e)
            {
                this.dialogService.ExceptionAlert(e);
            }
        }
    }
}
