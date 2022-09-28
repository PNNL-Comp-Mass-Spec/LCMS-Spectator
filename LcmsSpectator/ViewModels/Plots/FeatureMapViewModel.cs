// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FeatureMapViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model that maintains a (false) heat map of LCMS features.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using LcmsSpectator.Config;
using LcmsSpectator.DialogServices;
using LcmsSpectator.Models;
using LcmsSpectator.PlotModels.ColorDictionaries;
using LcmsSpectator.Utils;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.Plots
{
    /// <summary>
    /// View model that maintains a (false) heat map of LCMS features.
    /// </summary>
    public class FeatureMapViewModel : ReactiveObject
    {
        /// <summary>
        /// Scale of the highlight.
        /// </summary>
        private const double HighlightScale = 0.008;

        /// <summary>
        /// Dialog service for opening dialogs from the view model.
        /// </summary>
        private readonly IDialogService dialogService;

        /// <summary>
        /// Dictionary associating unique protein to MS/MS series
        /// </summary>
        private readonly Dictionary<string, ScatterSeries> ms2SeriesDictionary;

        /// <summary>
        /// The X Axis of the feature map.
        /// </summary>
        private readonly LinearAxis xaxis;

        /// <summary>
        /// The Y Axis of the feature map.
        /// </summary>
        private readonly LinearAxis yaxis;

        // <summary>
        // MS/MS scans that are identified, but not associated with a feature.
        // </summary>
        // private Series notFoundMs2S;

        /// <summary>
        /// Color axis for MS/MS scan points.
        /// </summary>
        private readonly LinearColorAxis ms2ColorAxis;

        /// <summary>
        /// Color axis for features.
        /// </summary>
        private readonly LinearColorAxis featureColorAxis;

        /// <summary>
        /// Dictionary associating unique protein to color index.
        /// </summary>
        private readonly ProteinColorDictionary colorDictionary;

        /// <summary>
        /// MS/MS scan highlight annotation.
        /// </summary>
        private RectangleAnnotation highlight;

        /// <summary>
        /// The minimum for the X axis of the feature map.
        /// </summary>
        private double xminimum;

        /// <summary>
        /// The maximum for the X axis of the feature map.
        /// </summary>
        private double xmaximum;

        /// <summary>
        /// The minimum for the X axis of the feature map.
        /// </summary>
        private double yminimum;

        /// <summary>
        /// The maximum for the Y axis of the feature map.
        /// </summary>
        private double ymaximum;

        /// <summary>
        /// The minimum abundance displayed on feature map.
        /// </summary>
        private double abundanceMinimum;

        /// <summary>
        /// The maximum abundance displayed on feature map.
        /// </summary>
        private double abundanceMaximum;

        /// <summary>
        /// A value indicating whether whether the  identified MS/MS points associated with features are being shown on
        /// the feature map plot.
        /// </summary>
        private bool showFoundIdMs2;

        /// <summary>
        /// A value indicating whether the unidentified MS/mS points associated with features are being shown on
        /// the feature map plot.
        /// </summary>
        private bool showFoundUnIdMs2;

        // <summary>
        // MS/MS scans that are identified, but not associated with a feature.
        // </summary>
        // private Series notFoundMs2S;

        /// <summary>
        /// The feature point selected by the user.
        /// </summary>
        private Feature.FeaturePoint selectedFeaturePoint;

        /// <summary>
        /// The identification selected by the user.
        /// </summary>
        private PrSm selectedPrSmPoint;

        /// <summary>
        /// A value indicating whether a linear or logarithmic axis should be used for abundance.
        /// </summary>
        private bool isLinearAbundanceAxis;

        /// <summary>
        /// A value indicating whether a linear or logarithmic axis should be used for abundance.
        /// </summary>
        private bool isLogarithmicAbundanceAxis;

        /// <summary>
        /// The size (height) of each feature.
        /// </summary>
        private double featureSize;

        /// <summary>
        /// A value indicating whether the manual adjustment text boxes should be displayed.
        /// </summary>
        private bool showManualAdjustment;

        /// <summary>
        /// The currently selected identification. This is the ID highlighted on the feature
        /// map plot.
        /// </summary>
        private PrSm selectedPrSm;

        /// <summary>
        /// The features displayed on the Feature Map plot.
        /// </summary>
        private List<Feature> features;

        /// <summary>
        /// The feature selected by double clicking on feature map.
        /// </summary>
        private Feature.FeaturePoint selectedFeature;

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureMapViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from the view model.</param>
        public FeatureMapViewModel(IDialogService dialogService = null)
        {
            this.dialogService = dialogService ?? new DialogService();

            ms2SeriesDictionary = new Dictionary<string, ScatterSeries>();

            ShowFoundIdMs2 = false;
            ShowFoundUnIdMs2 = false;

            IsLinearAbundanceAxis = false;
            IsLogarithmicAbundanceAxis = true;
            FeatureSize = 0.1;

            FeatureSelectedCommand = ReactiveCommand.Create(FeatureSelectedImplementation);

            // Save As Image Command requests a file path from the user and then saves the spectrum plot as an image
            SaveAsImageCommand = ReactiveCommand.Create(SaveAsImageImplementation);

            // Initialize color axes.
            const int numColors = 5000;
            featureColorAxis = new LinearColorAxis     // Color axis for features
            {
                Title = "Abundance",
                Position = AxisPosition.Right,
                Palette = OxyPalette.Interpolate(numColors, IcParameters.Instance.FeatureColors),
            };

            colorDictionary = new ProteinColorDictionary();
            ms2ColorAxis = new LinearColorAxis      // Color axis for ms2s
            {
                Key = "ms2s",
                Position = AxisPosition.None,
                Palette = colorDictionary.OxyPalette,
                Minimum = 1,
                Maximum = colorDictionary.OxyPalette.Colors.Count,
                AxisTitleDistance = 1
            };

            // Initialize x and y axes.
            yaxis = new LinearAxis { Position = AxisPosition.Left, Title = "Monoisotopic Mass", StringFormat = "0.###" };
            xaxis = new LinearAxis { Position = AxisPosition.Bottom, Title = "Retention Time", StringFormat = "0.###", };

            // Change size of scan highlight annotation when feature map x and y axes are zoomed or panned.
            var isInternalChange = false;
            xaxis.AxisChanged += (o, e) =>
            {
                XMinimum = Math.Round(xaxis.ActualMinimum, 3);
                XMaximum = Math.Round(xaxis.ActualMaximum, 3);
                if (!isInternalChange && highlight != null &&
                    highlight.TextPosition.X >= xaxis.ActualMinimum && highlight.TextPosition.X <= xaxis.ActualMaximum &&
                   highlight.TextPosition.Y >= yaxis.ActualMinimum && highlight.TextPosition.Y <= yaxis.ActualMaximum)
                {
                    var x = highlight.TextPosition.X;
                    isInternalChange = true;
                    highlight.MinimumX = x - ((xaxis.ActualMaximum - xaxis.ActualMinimum) * HighlightScale * 0.5);
                    highlight.MaximumX = x + ((xaxis.ActualMaximum - xaxis.ActualMinimum) * HighlightScale * 0.5);
                }

                isInternalChange = false;
            };
            yaxis.AxisChanged += (o, e) =>
            {
                YMinimum = Math.Round(yaxis.ActualMinimum, 3);
                YMaximum = Math.Round(yaxis.ActualMaximum, 3);
                if (!isInternalChange && highlight != null &&
                    highlight.TextPosition.X >= xaxis.ActualMinimum && highlight.TextPosition.X <= xaxis.ActualMaximum &&
                    highlight.TextPosition.Y >= yaxis.ActualMinimum && highlight.TextPosition.Y <= yaxis.ActualMaximum)
                {
                    var y = highlight.TextPosition.Y;
                    isInternalChange = true;
                    highlight.MinimumY = y - ((yaxis.ActualMaximum - yaxis.ActualMinimum) * HighlightScale);
                    highlight.MaximumY = y + ((yaxis.ActualMaximum - yaxis.ActualMinimum) * HighlightScale);
                }

                isInternalChange = false;
            };

            // Initialize feature map.
            FeatureMap = new PlotModel { Title = "Feature Map" };
            FeatureMap.MouseDown += FeatureMapMouseDown;
            FeatureMap.Axes.Add(featureColorAxis);
            FeatureMap.Axes.Add(ms2ColorAxis);
            FeatureMap.Axes.Add(xaxis);
            FeatureMap.Axes.Add(yaxis);

            // When ShowNotFoundMs2 changes, update the NotFoundMs2 series
            this.WhenAnyValue(x => x.ShowFoundUnIdMs2)
                .Where(_ => FeatureMap != null && ms2SeriesDictionary.ContainsKey(string.Empty))
                .Subscribe(showFoundUnIdMs2 =>
                {
                    ms2SeriesDictionary[string.Empty].IsVisible = showFoundUnIdMs2;
                    FeatureMap.InvalidatePlot(true);
                });

            // When ShowFoundIdMs2 changes, update all ms2 series
            this.WhenAnyValue(x => x.ShowFoundIdMs2)
                .Where(_ => FeatureMap != null)
                .Subscribe(showFoundMs2 =>
                {
                    foreach (var protein in ms2SeriesDictionary.Keys.Where(key => !string.IsNullOrWhiteSpace(key)))
                    {
                        ms2SeriesDictionary[protein].IsVisible = showFoundMs2;
                    }

                    FeatureMap.InvalidatePlot(true);
                });

            this.WhenAnyValue(x => x.IsLinearAbundanceAxis)
                .Subscribe(isLinearAbundanceAxis => IsLogarithmicAbundanceAxis = !isLinearAbundanceAxis);

            this.WhenAnyValue(x => x.IsLogarithmicAbundanceAxis)
                .Subscribe(isLogarithmicAbundanceAxis => IsLinearAbundanceAxis = !isLogarithmicAbundanceAxis);

            // Update plot axes when FeaturePlotXMin, YMin, XMax, and YMax change
            this.WhenAnyValue(x => x.XMinimum, x => x.XMaximum)
                .Throttle(TimeSpan.FromSeconds(1), RxApp.TaskpoolScheduler)
                .Where(x => !xaxis.ActualMinimum.Equals(x.Item1) || !xaxis.ActualMaximum.Equals(x.Item2))
                .Subscribe(
                    x =>
                    {
                        xaxis.Zoom(x.Item1, x.Item2);
                        FeatureMap.InvalidatePlot(false);
                    });
            this.WhenAnyValue(y => y.YMinimum, x => x.YMaximum)
                .Throttle(TimeSpan.FromSeconds(1), RxApp.TaskpoolScheduler)
                .Where(y => !yaxis.ActualMinimum.Equals(y.Item1) || !yaxis.ActualMaximum.Equals(y.Item2))
                .Subscribe(
                    y =>
                    {
                        yaxis.Zoom(y.Item1, y.Item2);
                        FeatureMap.InvalidatePlot(false);
                    });

            // When SelectedPrSm is changed, update highlighted prsm on plot
            this.WhenAnyValue(x => x.SelectedPrSm)
                .Where(selectedPrSm => selectedPrSm != null)
                .Subscribe(selectedPrSm =>
                {
                    SetHighlight(selectedPrSm);
                    FeatureMap.InvalidatePlot(true);
                    SelectedPrSm.WhenAnyValue(x => x.Scan)
                        .Subscribe(scan =>
                        {
                            SetHighlight(selectedPrSm);
                            FeatureMap.InvalidatePlot(true);
                        });
                });

            var propMon = this.WhenAnyValue(x => x.IsLinearAbundanceAxis, x => x.FeatureSize, x => x.Features)
                .Select(x => featureColorAxis.Title = x.Item1 ? "Abundance" : "Abundance (Log10)");

            var colorMon = IcParameters.Instance.WhenAnyValue(x => x.IdColors, x => x.Ms2ScanColor, x => x.FeatureColors)
                .Select(x =>
                {
                    var colorList = new List<OxyColor> { Capacity = x.Item1.Length + 1 };
                    colorList.Add(x.Item2);
                    colorList.AddRange(x.Item1);
                    colorDictionary.SetColors(colorList);
                    ms2ColorAxis.Palette = colorDictionary.OxyPalette;
                    featureColorAxis.Palette = OxyPalette.Interpolate(numColors, x.Item3);
                    return string.Empty;
                });

            // Link two observables to a single throttle and action.
            propMon.Merge(colorMon).Throttle(TimeSpan.FromSeconds(1), RxApp.TaskpoolScheduler).Subscribe(x => BuildPlot());
        }

        /// <summary>
        /// Gets a command that saves the feature map as a PNG image.
        /// </summary>
        public ReactiveCommand<Unit, Unit> SaveAsImageCommand { get; }

        /// <summary>
        /// Gets a command activated when a feature is selected (double clicked) on the
        /// feature map plot.
        /// </summary>
        public ReactiveCommand<Unit, Unit> FeatureSelectedCommand { get; }

        /// <summary>
        /// Gets the Plot model for the feature map.
        /// </summary>
        public PlotModel FeatureMap { get; }

        /// <summary>
        /// Gets or sets the minimum for the X axis of the feature map.
        /// </summary>
        public double XMinimum
        {
            get => xminimum;
            set => this.RaiseAndSetIfChanged(ref xminimum, value);
        }

        /// <summary>
        /// Gets or sets the maximum for the X axis of the feature map.
        /// </summary>
        public double XMaximum
        {
            get => xmaximum;
            set => this.RaiseAndSetIfChanged(ref xmaximum, value);
        }

        /// <summary>
        /// Gets or sets the minimum for the Y axis of the feature map.
        /// </summary>
        public double YMinimum
        {
            get => yminimum;
            set => this.RaiseAndSetIfChanged(ref yminimum, value);
        }

        /// <summary>
        /// Gets or sets the maximum for the Y axis of the feature map.
        /// </summary>
        public double YMaximum
        {
            get => ymaximum;
            set => this.RaiseAndSetIfChanged(ref ymaximum, value);
        }

        /// <summary>
        /// Gets or sets the minimum abundance displayed on feature map.
        /// </summary>
        public double AbundanceMinimum
        {
            get => abundanceMinimum;
            set => this.RaiseAndSetIfChanged(ref abundanceMinimum, value);
        }

        /// <summary>
        /// Gets or sets the maximum abundance displayed on feature map.
        /// </summary>
        public double AbundanceMaximum
        {
            get => abundanceMaximum;
            set => this.RaiseAndSetIfChanged(ref abundanceMaximum, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether a linear or logarithmic axis should be used for abundance.
        /// </summary>
        public bool IsLinearAbundanceAxis
        {
            get => isLinearAbundanceAxis;
            set => this.RaiseAndSetIfChanged(ref isLinearAbundanceAxis, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether a linear or logarithmic axis should be used for abundance.
        /// </summary>
        public bool IsLogarithmicAbundanceAxis
        {
            get => isLogarithmicAbundanceAxis;
            set => this.RaiseAndSetIfChanged(ref isLogarithmicAbundanceAxis, value);
        }

        /// <summary>
        /// Gets or sets the size (height) of each feature.
        /// </summary>
        public double FeatureSize
        {
            get => featureSize;
            set => this.RaiseAndSetIfChanged(ref featureSize, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether whether the  identified MS/MS points associated with features are being shown on
        /// the feature map plot.
        /// </summary>
        public bool ShowFoundIdMs2
        {
            get => showFoundIdMs2;
            set => this.RaiseAndSetIfChanged(ref showFoundIdMs2, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the unidentified MS/mS points associated with features are being shown on
        /// the feature map plot.
        /// </summary>
        public bool ShowFoundUnIdMs2
        {
            get => showFoundUnIdMs2;
            set => this.RaiseAndSetIfChanged(ref showFoundUnIdMs2, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the manual adjustment text boxes should be displayed.
        /// </summary>
        public bool ShowManualAdjustment
        {
            get => showManualAdjustment;
            set => this.RaiseAndSetIfChanged(ref showManualAdjustment, value);
        }

        /// <summary>
        /// Gets or sets the currently selected identification. This is the ID highlighted on the feature
        /// map plot.
        /// </summary>
        public PrSm SelectedPrSm
        {
            get => selectedPrSm;
            set => this.RaiseAndSetIfChanged(ref selectedPrSm, value);
        }

        /// <summary>
        /// Gets or sets the selected feature point.
        /// </summary>
        public Feature.FeaturePoint SelectedFeature
        {
            get => selectedFeature;
            set => this.RaiseAndSetIfChanged(ref selectedFeature, value);
        }

        /// <summary>
        /// Gets or sets the list of features displayed on the feature plot.
        /// </summary>
        public List<Feature> Features
        {
            get => features;
            set => this.RaiseAndSetIfChanged(ref features, value);
        }

        /// <summary>
        /// Build feature map plot.
        /// </summary>
        public void BuildPlot()
        {
            ResetFeaturePlot(); // clear existing plot

            // Calculate min/max abundance and min/max score
            if (features == null || features.Count == 0)
            {
                return;
            }

            var minAbundance = features.Min(f => f.MinPoint.Abundance);
            var maxAbundance = features.Max(f => f.MaxPoint.Abundance);

            if (!IsLinearAbundanceAxis)
            {
                minAbundance = Math.Log10(minAbundance);
                maxAbundance = Math.Log10(maxAbundance);
            }

            featureColorAxis.StringFormat = IsLinearAbundanceAxis ? "0.###E0" : "0.#";

            // Set bounds for color axis for features
            featureColorAxis.AbsoluteMinimum = minAbundance;
            featureColorAxis.Minimum = minAbundance;
            featureColorAxis.Maximum = maxAbundance;
            featureColorAxis.AbsoluteMaximum = maxAbundance;

            var idPrSms = new List<PrSm>();

            foreach (var feature in features)
            {
                // Identified MS/MS scans associated with feature
                idPrSms.AddRange(feature.AssociatedPrSms);

                // Create and add feature
                FeatureMap.Series.Add(CreateFeatureSeries(feature, featureColorAxis.Palette.Colors, FeatureSize, minAbundance, maxAbundance));
            }

            AddIdentificationPoints(idPrSms);
            foreach (var series in ms2SeriesDictionary)
            {
                FeatureMap.Series.Add(series.Value);
            }

            // Highlight selected identification.
            SetHighlight(SelectedPrSm);
            FeatureMap.IsLegendVisible = false;
            FeatureMap.InvalidatePlot(true);
        }

        /// <summary>
        /// Reset plot by clearing all series and resetting minimum and maximum abundance.
        /// </summary>
        private void ResetFeaturePlot()
        {
            if (FeatureMap != null)
            {
                FeatureMap.Series.Clear();
                FeatureMap.InvalidatePlot(true);
            }
        }

        /// <summary>
        /// Create a line series for a feature containing a min point and max point.
        /// </summary>
        /// <param name="feature">Feature to create series for.</param>
        /// <param name="colors">The color scale to use to color the feature.</param>
        /// <param name="size">The size of the feature.</param>
        /// <param name="minAbundance">Minimum abundance of all features.</param>
        /// <param name="maxAbundance">Maximum abundance of all features.</param>
        /// <returns>Line series for feature.</returns>
        private LineSeries CreateFeatureSeries(Feature feature, IList<OxyColor> colors, double size, double minAbundance, double maxAbundance)
        {
            var abundance = feature.MinPoint.Abundance;
            if (!IsLinearAbundanceAxis)
            {
                abundance = Math.Log10(abundance);
            }

            var colorIndex = 1
                             + (int)
                               ((abundance - minAbundance) / (maxAbundance - minAbundance)
                                * colors.Count);
            colorIndex = Math.Max(1, Math.Min(colorIndex, colors.Count - 1));

            var c = colors[colorIndex];
            var trackerString = "{0} (ID: {Id:0})" + Environment.NewLine +
                                   "{1}: {2:0.###} (Scan: {Scan:0})" + Environment.NewLine +
                                   "{3}: {4:0.###}" + Environment.NewLine +
                                   "Abundance: {Abundance:0.###E0}" + Environment.NewLine +
                                   "Probability: {Score:0.###}" + Environment.NewLine +
                                   "Correlation: {Correlation:0.###}" + Environment.NewLine +
                                   "Charge: {Charge:0}";
            return new LineSeries
            {
                ItemsSource = new[] { feature.MinPoint, feature.MaxPoint },
                Mapping = fp => new DataPoint(((Feature.FeaturePoint)fp).RetentionTime, ((Feature.FeaturePoint)fp).Mass),
                Title = "Feature",
                Color = c,
                LineStyle = LineStyle.Solid,
                StrokeThickness = size,
                TrackerFormatString = trackerString,
            };
        }

        /// <summary>
        /// Groups MS/MS ID points in series by protein name.
        /// </summary>
        /// <param name="prsms">The IDs.</param>
        private void AddIdentificationPoints(IEnumerable<PrSm> prsms)
        {
            ms2SeriesDictionary.Clear();
            var prsmByProtein = new Dictionary<string, List<PrSm>>();

            foreach (var prsm in prsms)
            {
                if (!prsmByProtein.ContainsKey(prsm.ProteinName))
                {
                    prsmByProtein.Add(prsm.ProteinName, new List<PrSm>());
                }

                prsmByProtein[prsm.ProteinName].Add(prsm);
            }

            foreach (var proteinName in prsmByProtein.Keys)
            {
                var name = proteinName;
                var scatterSeries = new ScatterSeries
                {
                    ItemsSource = prsmByProtein[proteinName],
                    Mapping = p =>
                    {
                        var prsm = (PrSm)p;
                        var size = prsm.Sequence.Count > 0 ? Math.Min((featureSize * 4) + 2, 4) : Math.Min(featureSize * 2, 4);
                        return new ScatterPoint(prsm.RetentionTime, prsm.Mass, size, colorDictionary.GetColorCode(name));
                    },
                    Title = proteinName,
                    MarkerType = MarkerType.Cross,
                    ColorAxisKey = ms2ColorAxis.Key,
                    TrackerFormatString =
                        "{0}" + Environment.NewLine +
                        "{1}: {2:0.###}" + Environment.NewLine +
                        "{3}: {4:0.###E0}",
                    IsVisible = proteinName == string.Empty ? showFoundUnIdMs2 : showFoundIdMs2,
                };

                ms2SeriesDictionary.Add(proteinName, scatterSeries);
            }
        }

        /// <summary>
        /// Event handler for mouse click event to set SelectedDataPoint
        /// </summary>
        /// <param name="sender">The sender PlotView</param>
        /// <param name="args">The event arguments.</param>
        private void FeatureMapMouseDown(object sender, OxyMouseEventArgs args)
        {
            selectedFeaturePoint = null;
            selectedPrSmPoint = null;
            var series = FeatureMap.GetSeriesFromPoint(args.Position, 10);
            if (series is LineSeries)
            {
                // Was a feature clicked?
                var result = series.GetNearestPoint(args.Position, false);
                if (result == null)
                {
                    return;
                }

                if (result.Item is Feature.FeaturePoint featurePoint)
                {
                    // See if there is a ms2 point closer than this feature point
                    PrSm closestPrSm = null;
                    if (showFoundIdMs2 || showFoundUnIdMs2)
                    {
                        var feature = featurePoint.Feature;
                        var prsms = feature.AssociatedPrSms;
                        var minDist = result.Position.DistanceToSquared(args.Position);
                        foreach (var prsm in prsms)
                        {
                            if ((prsm.Sequence.Count == 0 && !showFoundUnIdMs2)
                                || (prsm.Sequence.Count > 0 && !showFoundIdMs2))
                            {
                                continue;
                            }

                            var sp = xaxis.Transform(prsm.RetentionTime, featurePoint.Mass, yaxis);
                            var distSq = sp.DistanceToSquared(args.Position);
                            if (closestPrSm == null || distSq < minDist)
                            {
                                closestPrSm = prsm;
                                minDist = distSq;
                            }
                        }
                    }

                    if (closestPrSm != null)
                    {
                        selectedPrSmPoint = closestPrSm;
                    }

                    selectedFeaturePoint = featurePoint;
                }
            }
            else if (series is ScatterSeries)
            {
                // Was a ms2 cross clicked?
                var result = series.GetNearestPoint(args.Position, false);
                if (result == null)
                {
                    return;
                }

                selectedPrSmPoint = result.Item as PrSm;
            }
        }

        /// <summary>
        /// Highlight an identification on the feature map plot.
        /// </summary>
        /// <param name="prsm">Identification to highlight.</param>
        private void SetHighlight(PrSm prsm)
        {
            if (prsm == null)
            {
                return;
            }

            ////prsm.LcMs = this.lcms;
            var rt = prsm.RetentionTime;
            var mass = prsm.Mass;
            if (highlight == null)
            {
                highlight = new RectangleAnnotation
                {
                    TextPosition = new DataPoint(rt, mass),
                    Fill = OxyColor.FromArgb(100, 255, 255, 0),
                    Stroke = OxyColor.FromRgb(0, 255, 0),
                    StrokeThickness = 2,
                    MinimumX = rt - ((xaxis.ActualMaximum - xaxis.ActualMinimum) * HighlightScale * 0.5),
                    MaximumX = rt + ((xaxis.ActualMaximum - xaxis.ActualMinimum) * HighlightScale * 0.5),
                    MinimumY = mass - ((yaxis.ActualMaximum - yaxis.ActualMinimum) * HighlightScale),
                    MaximumY = mass + ((yaxis.ActualMaximum - yaxis.ActualMinimum) * HighlightScale),
                };

                FeatureMap.Annotations.Add(highlight);
            }
            else
            {
                highlight.TextPosition = new DataPoint(rt, mass);
                highlight.MinimumX = rt - ((xaxis.ActualMaximum - xaxis.ActualMinimum) * HighlightScale * 0.5);
                highlight.MaximumX = rt + ((xaxis.ActualMaximum - xaxis.ActualMinimum) * HighlightScale * 0.5);
                highlight.MinimumY = mass - ((yaxis.ActualMaximum - yaxis.ActualMinimum) * HighlightScale);
                highlight.MaximumY = mass + ((yaxis.ActualMaximum - yaxis.ActualMinimum) * HighlightScale);
            }
        }

        /// <summary>
        /// Called when a point has been double clicked on the feature map plot.
        /// Feature selected: Show observed/theoretical isotope envelopes
        /// MS/MS selected: Trigger event for the application to display this MS/MS.
        /// </summary>
        private void FeatureSelectedImplementation()
        {
            SelectedFeature = selectedFeaturePoint;

            if (selectedPrSmPoint != null)
            {
                SelectedPrSm = selectedPrSmPoint;
            }
        }

        /// <summary>
        /// Implementation for <see cref="SaveAsImageCommand" />.
        /// Gets a command that saves the feature map as a PNG image.
        /// </summary>
        private void SaveAsImageImplementation()
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
                    FeatureMap,
                    filePath,
                    (int)FeatureMap.Width,
                    (int)FeatureMap.Height,
                    OxyColors.White,
                    IcParameters.Instance.ExportImageDpi);
            }
            catch (Exception e)
            {
                dialogService.ExceptionAlert(e);
            }
        }
    }
}
