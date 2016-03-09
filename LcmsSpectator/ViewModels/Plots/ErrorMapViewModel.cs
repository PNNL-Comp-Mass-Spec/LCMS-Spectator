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
    using System.IO;
    using System.Linq;

    using InformedProteomics.Backend.Data.Composition;
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
        /// The percentage of the sequence explained by fragment ion peaks.
        /// </summary>
        private double sequenceCoverage;

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
                AxisDistance = -0.5,
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
        /// Gets the percentage of the sequence explained by fragment ion peaks.
        /// </summary>
        public double SequenceCoverage
        {
            get { return this.sequenceCoverage; }
            private set { this.RaiseAndSetIfChanged(ref this.sequenceCoverage, value); }
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
            //this.DataTable = new ReactiveList<PeakDataPoint>(dataPoints.Where(dp => !dp.Error.Equals(double.NaN)));
            this.DataTable = new ReactiveList<PeakDataPoint>(dataPoints);

            this.SequenceCoverage = IonUtils.CalculateSequenceCoverage(this.DataTable, sequence.Count);

            // Build and invalidate erorr map plot display
            this.BuildErrorPlotModel(sequence, this.GetErrorDataArray(dataPoints, sequence.Count));
            //this.BuildErrorPlotModel(sequence, this.GetCoverageDataArray(dataPoints, sequence.Count));
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
        private void BuildErrorPlotModel(IReadOnlyList<AminoAcid> sequence, double[,] data)
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
                        ////"{1}: {2:0}" + Environment.NewLine +
                        ////"{3}: {4:0}" + Environment.NewLine +
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
                var symbol = ionType.BaseIonType == null ? ionType.Name : ionType.BaseIonType.Symbol;
                return ionType.Charge == 1 ?
                       symbol :
                       string.Format("{0}({1}+)", symbol, ionType.Charge);
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
        /// <param name="dataPoints">The data Points.</param>
        /// <param name="sequenceLength">The length of the sequence.</param>
        /// <returns>
        /// 2d array where first dimension is sequence and second dimension is ion type
        /// </returns>
        private double[,] GetErrorDataArray(IEnumerable<PeakDataPoint> dataPoints, int sequenceLength)
        {
            var dataDict = new Dictionary<IonType, PeakDataPoint[]>();

            // partition data set by ion type
            foreach (var dataPoint in dataPoints)
            {
                if (!dataDict.ContainsKey(dataPoint.IonType))
                {
                    dataDict.Add(dataPoint.IonType, new PeakDataPoint[sequenceLength]);
                }

                var points = dataDict[dataPoint.IonType];

                int index = dataPoint.Index - 1;

                if (!dataPoint.IonType.IsPrefixIon)
                {
                    index = sequenceLength - dataPoint.Index;
                }

                // If the ion type has multiple options, choose the best one.
                if (points[index] == null || (dataPoint.Y / dataPoint.Error) > (points[index].Y / points[index].Error))
                {
                    points[index] = dataPoint;
                }
            }

            this.ionTypes = dataDict.Keys.ToArray();

            var data = new double[sequenceLength, dataDict.Keys.Count];

            // create two dimensional array from partitioned data
            for (int i = 0; i < sequenceLength; i++)
            {
                for (int j = 0; j < this.ionTypes.Length; j++)
                {
                    var dataPoint = dataDict[this.ionTypes[j]][i];
                    var value = dataPoint == null ? double.NaN : dataPoint.Error;

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
        /// Organize the peak data points by ion type
        /// </summary>
        /// <param name="dataPoints">The data Points.</param>
        /// <param name="sequenceLength">The length of the sequence.</param>
        /// <returns>
        /// 2d array where first dimension is sequence and second dimension is ion type
        /// </returns>
        private double[,] GetCoverageDataArray(IEnumerable<PeakDataPoint> dataPoints, int sequenceLength)
        {
            var dataDict = new Dictionary<BaseIonType, bool[]>();
            // partition data set by ion type
            foreach (var dataPoint in dataPoints)
            {
                if (!dataDict.ContainsKey(dataPoint.IonType.BaseIonType))
                {
                    dataDict.Add(dataPoint.IonType.BaseIonType, new bool[sequenceLength]);
                }

                var points = dataDict[dataPoint.IonType.BaseIonType];

                int index = dataPoint.Index - 1;

                if (!dataPoint.IonType.IsPrefixIon)
                {
                    index = sequenceLength - dataPoint.Index;
                }

                points[index] = true;
            }

            this.ionTypes = dataDict.Keys.Select(
                                     baseIonType => new IonType(
                                                                baseIonType.Symbol,
                                                                Composition.Zero,
                                                                1,
                                                                baseIonType.IsPrefix))
                    .ToArray();

            var data = new double[sequenceLength, dataDict.Keys.Count];

            var errorThresh = IcParameters.Instance.ProductIonTolerancePpm.GetValue();

            // create two dimensional array from partitioned data
            for (int i = 0; i < sequenceLength; i++)
            {
                int j = 0;
                foreach (var baseIonType in dataDict.Keys)
                {
                    data[i, j++] = dataDict[baseIonType][i] ? errorThresh : -1 * (errorThresh + 1);
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
            var filePath = this.dialogService.SaveFile(".png", @"Png Files (*.png)|*.png");
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (directory == null || !Directory.Exists(directory))
                {
                    throw new FormatException(
                        string.Format("Cannot save image due to invalid file name: {0}", filePath));
                }

                DynamicResolutionPngExporter.Export(
                    this.PlotModel,
                    filePath,
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
