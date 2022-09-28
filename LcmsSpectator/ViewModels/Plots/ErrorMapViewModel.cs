// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ErrorMapViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class maintains a heat map plot model showing sequence vs ion type vs error.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.Config;
using LcmsSpectator.DialogServices;
using LcmsSpectator.PlotModels;
using LcmsSpectator.Utils;
using LcmsSpectator.Writers.Exporters;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.Plots
{
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
        private readonly LinearAxis xAxis;

        /// <summary>
        /// Vertical axis of error map plot (sequence residue)
        /// </summary>
        private readonly LinearAxis yAxis;

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
        /// Minimum intensity of ions to include
        /// </summary>
        private double minimumIonIntensity;

        /// <summary>
        /// The IonType selected from the error map.
        /// </summary>
        private string selectedIonType;

        /// <summary>
        /// The residue and index of the selected item.
        /// </summary>
        private string selectedAminoAcid;

        /// <summary>
        /// The value of the selected item.
        /// </summary>
        private string selectedValue;

        /// <summary>
        /// The currently selected sequence.
        /// </summary>
        private Sequence selectedSequence;

        /// <summary>
        /// The currently selected peak data points.
        /// </summary>
        private IList<IList<PeakDataPoint>> selectedPeakDataPoints;

        /// <summary>
        /// A value indicating whether multiple charge states for an ion should
        /// be combined into a single row on the heat map.
        /// </summary>
        private bool shouldCombineChargeStates;

        /// <summary>
        /// When true, include unmatched ions in the table
        /// </summary>
        private bool tableShouldIncludeUnmatched;

        private readonly PlotModel plotModel;

        /// <summary>
        /// Default constructor to support WPF design-time use
        /// </summary>
        [Obsolete("For WPF Design-time use only.", true)]
        public ErrorMapViewModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the ErrorMapViewModel class.
        /// </summary>
        /// <param name="dialogService">
        /// The dialog Service.
        /// </param>
        public ErrorMapViewModel(IDialogService dialogService)
        {
            this.dialogService = dialogService;
            plotModel = new ViewResolvingPlotModel { Title = "Error Map", PlotAreaBackground = OxyColors.DimGray };
            selectedPeakDataPoints = Array.Empty<IList<PeakDataPoint>>();
            minimumIonIntensity = 0;
            ShouldCombineChargeStates = true;
            TableShouldIncludeUnmatched = false;

            // Init x axis
            xAxis = new LinearAxis
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
            plotModel.Axes.Add(xAxis);

            // Init Y axis
            yAxis = new LinearAxis
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
            plotModel.Axes.Add(yAxis);

            // Init Color axis
            var minColor = OxyColors.Navy;
            var medColor = OxyColors.White;
            var maxColor = OxyColors.DarkRed;
            colorAxis = new LinearColorAxis
            {
                Title = "Error",
                Position = AxisPosition.Right,
                AxisDistance = -0.5,
                AbsoluteMinimum = 0,
                Palette = OxyPalette.Interpolate(1000, minColor, medColor, maxColor),
                Minimum = -1 * IcParameters.Instance.ProductIonTolerancePpm.GetValue(),
                Maximum = IcParameters.Instance.ProductIonTolerancePpm.GetValue(),
                AbsoluteMaximum = IcParameters.Instance.ProductIonTolerancePpm.GetValue(),
                LowColor = OxyColors.DimGray,
            };
            plotModel.Axes.Add(colorAxis);

            this.WhenAnyValue(x => x.ShouldCombineChargeStates).Subscribe(_ => UpdateNow());

            this.WhenAnyValue(x => x.TableShouldIncludeUnmatched).Subscribe(_ => UpdateNow());

            // Save As Image Command requests a file path from the user and then saves the error map as an image
            SaveAsImageCommand = ReactiveCommand.Create(SaveAsImageImpl);

            SaveDataTableCommand = ReactiveCommand.Create(SaveDataTableImpl);

            UpdateNowCommand = ReactiveCommand.Create(UpdateNow);

            ZoomOutCommand = ReactiveCommand.Create(ZoomOutPlot);
        }

        /// <summary>
        /// Gets the plot Model for error heat map
        /// </summary>
        public IPlotModel PlotModel => plotModel;

        /// <summary>
        /// Gets a command that prompts user for file path and save plot as image.
        /// </summary>
        public ReactiveCommand<Unit, Unit> SaveAsImageCommand { get; }

        /// <summary>
        /// Gets a command that prompt user for file path and save data table as list.
        /// </summary>
        public ReactiveCommand<Unit, Unit> SaveDataTableCommand { get; }

        /// <summary>
        /// Gets a command that updates the data using the current minimum intensity filter
        /// </summary>
        public ReactiveCommand<Unit, Unit> UpdateNowCommand { get; }

        /// <summary>
        /// Gets a command that zooms out the plot fully
        /// </summary>
        public ReactiveCommand<Unit, Unit> ZoomOutCommand { get; }

        /// <summary>
        /// Gets or sets the data that is shown in the "Table" view. This optionally excludes any fragments without data.
        /// </summary>
        public ReactiveList<PeakDataPoint> DataTable
        {
            get => dataTable;
            private set => this.RaiseAndSetIfChanged(ref dataTable, value);
        }

        /// <summary>
        /// Gets or sets a value indicating the minimum intensity of ions to include
        /// </summary>
        public double MinimumIonIntensity
        {
            get => minimumIonIntensity;
            set => this.RaiseAndSetIfChanged(ref minimumIonIntensity, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether multiple charge states for an ion should
        /// be combined into a single row on the heat map.
        /// </summary>
        public bool ShouldCombineChargeStates
        {
            get => shouldCombineChargeStates;
            set => this.RaiseAndSetIfChanged(ref shouldCombineChargeStates, value);
        }

        /// <summary>
        /// Gets the IonType selected from the error map.
        /// </summary>
        public string SelectedIonType
        {
            get => selectedIonType;
            private set => this.RaiseAndSetIfChanged(ref selectedIonType, value);
        }

        /// <summary>
        /// Gets the residue and index of the selected item.
        /// </summary>
        public string SelectedAminoAcid
        {
            get => selectedAminoAcid;
            private set => this.RaiseAndSetIfChanged(ref selectedAminoAcid, value);
        }

        /// <summary>
        /// Gets the value of the selected item.
        /// </summary>
        public string SelectedValue
        {
            get => selectedValue;
            private set => this.RaiseAndSetIfChanged(ref selectedValue, value);
        }

        /// <summary>
        /// Set sequence and data displayed on heat map.
        /// </summary>
        /// <param name="sequence">The sequence to display as the x axis of the plot.</param>
        /// <param name="peakDataPoints">The peak data points to extract error values from.</param>
        public void SetData(Sequence sequence, IList<IList<PeakDataPoint>> peakDataPoints)
        {
            selectedSequence = sequence;

            if (sequence == null || peakDataPoints == null)
            {
                return;
            }

            // Remove all points except for most abundant isotope peaks
            var mostAbundantPeaks = GetMostAbundantIsotopePeaks(peakDataPoints).ToArray();

            // No data, nothing to do
            if (mostAbundantPeaks.Length == 0)
            {
                return;
            }

            selectedPeakDataPoints = peakDataPoints;

            if (TableShouldIncludeUnmatched)
            {
                DataTable = new ReactiveList<PeakDataPoint>(mostAbundantPeaks);
            }
            else
            {
                // Only showing fragment ions found in spectrum in data table
                DataTable = new ReactiveList<PeakDataPoint>(mostAbundantPeaks.Where(dp => !double.IsNaN(dp.Error)));
            }

            // Build and invalidate error map plot display
            BuildErrorPlotModel(sequence, GetErrorDataArray(mostAbundantPeaks, sequence.Count));
            plotModel.MouseDown += MapMouseDown;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the table of ions and errors
        /// should include ions that did match an ion (b ion, y ion, c ion, z ion, etc.)
        /// </summary>
        public bool TableShouldIncludeUnmatched
        {
            get => tableShouldIncludeUnmatched;
            set => this.RaiseAndSetIfChanged(ref tableShouldIncludeUnmatched, value);
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
            colorAxis.Minimum = -1 * IcParameters.Instance.ProductIonTolerancePpm.GetValue();
            colorAxis.AbsoluteMinimum = -1 * IcParameters.Instance.ProductIonTolerancePpm.GetValue();
            colorAxis.Maximum = IcParameters.Instance.ProductIonTolerancePpm.GetValue();
            colorAxis.AbsoluteMaximum = IcParameters.Instance.ProductIonTolerancePpm.GetValue();

            plotModel.Series.Clear();

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
            plotModel.Series.Add(heatMapSeries);

            xAxis.AbsoluteMaximum = sequence.Count;
            yAxis.AbsoluteMaximum = ionTypes.Length + 1;

            // Set yAxis double -> string label converter function
            yAxis.LabelFormatter = y =>
            {
                if (y.Equals(0))
                {
                    return string.Empty;
                }

                var ionType = ionTypes[Math.Min((int)y - 1, ionTypes.Length - 1)];
                var symbol = ionType.BaseIonType == null ? ionType.Name : ionType.BaseIonType.Symbol;
                return ionType.Charge == 1 ?
                       symbol :
                       string.Format("{0}({1}+)", symbol, ionType.Charge);
            };

            // Set xAxis double -> string label converter function
            xAxis.LabelFormatter = x =>
            {
                if (x.Equals(0))
                {
                    return string.Empty;
                }

                var sequenceIndex = Math.Max(Math.Min((int)x - 1, sequence.Count - 1), 0);
                var residue = sequence[sequenceIndex].Residue.ToString(CultureInfo.InvariantCulture);
                return string.Format("{0}{1}", residue, (int)x);
            };

            // Update plot
            plotModel.InvalidatePlot(true);
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
            var dataDict = PartitionData(dataPoints, sequenceLength);

            ionTypes = dataDict.Keys.ToArray();

            var data = new double[sequenceLength, dataDict.Keys.Count];

            // create two dimensional array from partitioned data
            for (var i = 0; i < sequenceLength; i++)
            {
                for (var j = 0; j < ionTypes.Length; j++)
                {
                    var dataPoint = dataDict[ionTypes[j]][i];
                    var value = dataPoint?.Error ?? double.NaN;

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
        /// Partition data by ion type.
        /// Reduce the charge states to only one charge if <see cref="ShouldCombineChargeStates" /> is true.
        /// </summary>
        /// <param name="dataPoints">The data Points.</param>
        /// <param name="sequenceLength">The length of the sequence.</param>
        /// <returns>Peak data points partitioned by ion type.</returns>
        private Dictionary<IonType, PeakDataPoint[]> PartitionData(IEnumerable<PeakDataPoint> dataPoints, int sequenceLength)
        {
            var dataDict = new Dictionary<IonType, PeakDataPoint[]>();

            // partition data set by ion type
            foreach (var dataPoint in dataPoints)
            {
                if (double.IsNaN(dataPoint.Error))
                    continue;

                var ionType = dataPoint.IonType;
                if (ShouldCombineChargeStates)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    ionType = new IonType(ionType.Name, ionType.OffsetComposition, 1, ionType.IsPrefixIon);
#pragma warning restore CS0618 // Type or member is obsolete
                }

                if (!dataDict.ContainsKey(ionType))
                {
                    dataDict.Add(ionType, new PeakDataPoint[sequenceLength]);
                }

                var points = dataDict[ionType];

                var index = dataPoint.Index - 1;

                if (!dataPoint.IonType.IsPrefixIon)
                {
                    index = sequenceLength - dataPoint.Index;
                }

                if (index >= 0 && index < points.Length)
                {
                    // If the ion type has multiple options, choose the best one.
                    if (points[index] == null ||
                        double.IsNaN(points[index].Error) ||
                        (dataPoint.Y / Math.Abs(dataPoint.Error)) > (points[index].Y / Math.Abs(points[index].Error)))
                    {
                        points[index] = dataPoint;
                    }
                }
            }

            return dataDict;
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
                if (peak?.IonType == null || peak.IonType.Name == "Precursor" || peak.Y < MinimumIonIntensity) continue;

                mostAbundantPeaks.Add(peak);
            }

            return mostAbundantPeaks.OrderBy(x => x.IonType.BaseIonType.Symbol).ThenBy(x => x.IonType.Charge);
        }

        /// <summary>
        /// Prompt user for file path and save plot as image to that path.
        /// </summary>
        private void SaveAsImageImpl()
        {
            var filePath = dialogService.SaveFile(".png", "Png Files (*.png)|*.png");
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
                    plotModel,
                    filePath,
                    (int)plotModel.Width,
                    (int)plotModel.Height,
                    OxyColors.White,
                    IcParameters.Instance.ExportImageDpi);
            }
            catch (Exception e)
            {
                dialogService.ExceptionAlert(e);
            }
        }

        /// <summary>
        /// Prompt user for file path and save data table as list.
        /// </summary>
        private void SaveDataTableImpl()
        {
            var filePath = dialogService.SaveFile(".tsv", "Tab-separated value Files (*.tsv)|*.tsv");
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

                var peakExporter = new SpectrumPeakExporter(filePath);
                peakExporter.Export(DataTable.Where(p => !p.X.Equals(double.NaN)).ToList());
            }
            catch (Exception e)
            {
                dialogService.ExceptionAlert(e);
            }
        }

        /// <summary>
        /// Event handler for click event. Selects data based on cell clicked in heat map.
        /// </summary>
        /// <param name="sender">The sender HeatMap PlotModel.</param>
        /// <param name="args">The event arguments.</param>
        private void MapMouseDown(object sender, OxyMouseEventArgs args)
        {
            var series = plotModel.GetSeriesFromPoint(args.Position);
            if (!(series is HeatMapSeries heatMap))
                return;

            var point = heatMap.GetNearestPoint(args.Position, true);
            var dataPoint = point.DataPoint;

            var ionType = ionTypes[((int)dataPoint.Y) - 1];
            var seqIndex = ((int)dataPoint.X) - 1;
            var ionIndex = ionType.IsPrefixIon ? seqIndex + 1 : selectedSequence.Count - seqIndex - 1;
            var aminoAcid = selectedSequence[seqIndex];

            var value = point.Text;

            SelectedIonType = ionType.Name;
            SelectedAminoAcid = string.Format("{0}{1}", aminoAcid.Residue, ionIndex);

            if (value.Contains(" "))
            {
                var parts = value.Split(' ');
                value = parts[1];

                var intPart = value.Split('p')[0];
                if (int.TryParse(intPart, out var result) && result < -1 * IcParameters.Instance.ProductIonTolerancePpm.GetValue())
                {
                    value = "N/A";
                }
            }

            SelectedValue = value;
        }

        private void UpdateNow()
        {
            SetData(selectedSequence, selectedPeakDataPoints);
        }

        private void ZoomOutPlot()
        {
            plotModel.ResetAllAxes();
            plotModel.InvalidatePlot(true);
        }
    }
}
