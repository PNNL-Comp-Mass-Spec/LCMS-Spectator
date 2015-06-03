namespace LcmsSpectator.ViewModels.Plots
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using LcmsSpectator.Config;
    using LcmsSpectator.DialogServices;
    using LcmsSpectator.Models;
    using LcmsSpectator.Utils;
    using OxyPlot;
    using OxyPlot.Annotations;
    using OxyPlot.Axes;
    using OxyPlot.Series;
    using ReactiveUI;

    public class FeatureMapViewModel : ReactiveObject
    {
        /// <summary>
        /// Default size of highlight annotation.
        /// </summary>
        private const double HighlightSize = 0.008;

        /// <summary>
        /// The total number of distinct colors for the MS/MS scans.
        /// </summary>
        private const int DistinctMsMsColors = 11;

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

        /// <summary>
        /// MS/MS scans that are identified, but not associated with a feature.
        /// </summary>
        ////private Series notFoundMs2S;

        /// <summary>
        /// Color axis for MS/MS scan points.
        /// </summary>
        private readonly LinearColorAxis ms2ColorAxis;

        /// <summary>
        /// Color axis for features.
        /// </summary>
        private readonly LinearColorAxis featureColorAxis;

        /// <summary>
        /// MS/MS scan highlight annotation.
        /// </summary>
        private RectangleAnnotation highlight;

        /// <summary>
        /// The Plot model for the feature map.
        /// </summary>
        private PlotModel featureMap;

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
        /// The lowest abundance currently being shown on feature map.
        /// </summary>
        private double minimumAbundance;

        /// <summary>
        /// The highest abundance currently being shown on feature map.
        /// </summary>
        private double maximumAbundance;

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

        /// <summary>
        /// A value indicating whether the MS/MS points not associated with features are being shown
        /// on the feature map plot.
        /// </summary>
        private bool showNotFoundMs2;

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
        /// Dictionary associating unique protein to color index.
        /// </summary>
        private Dictionary<string, int> colorDictionary;

        public FeatureMapViewModel(IDialogService dialogService)
        {

            // Save As Image Command requests a file path from the user and then saves the spectrum plot as an image
            var saveAsImageCommand = ReactiveCommand.Create();
            saveAsImageCommand.Subscribe(_ => this.SaveAsImageImplementation());
            this.SaveAsImageCommand = saveAsImageCommand;
        }

        /// <summary>
        /// Gets a command that saves the feature map as a PNG image.
        /// </summary>
        public IReactiveCommand SaveAsImageCommand { get; private set; }
        
        /// <summary>
        /// Gets the Plot model for the feature map.
        /// </summary>
        public PlotModel FeatureMap
        {
            get { return this.featureMap; }
            private set { this.RaiseAndSetIfChanged(ref this.featureMap, value); }
        }

        /// <summary>
        /// Gets or sets the minimum for the X axis of the feature map.
        /// </summary>
        public double XMinimum
        {
            get { return this.xminimum; }
            set { this.RaiseAndSetIfChanged(ref this.xminimum, value); }
        }

        /// <summary>
        /// Gets or sets the maximum for the X axis of the feature map.
        /// </summary>
        public double XMaximum
        {
            get { return this.xmaximum; }
            set { this.RaiseAndSetIfChanged(ref this.xmaximum, value); }
        }

        /// <summary>
        /// Gets or sets the minimum for the Y axis of the feature map.
        /// </summary>
        public double YMinimum
        {
            get { return this.yminimum; }
            set { this.RaiseAndSetIfChanged(ref this.yminimum, value); }
        }

        /// <summary>
        /// Gets or sets the maximum for the Y axis of the feature map.
        /// </summary>
        public double YMaximum
        {
            get { return this.ymaximum; }
            set { this.RaiseAndSetIfChanged(ref this.ymaximum, value); }
        }

        /// <summary>
        /// Gets the lowest abundance currently being shown on feature map.
        /// </summary>
        public double MinimumAbundance
        {
            get { return this.minimumAbundance; }
            private set { this.RaiseAndSetIfChanged(ref this.minimumAbundance, value); }
        }

        /// <summary>
        /// Gets the highest abundance currently being shown on feature map.
        /// </summary>
        public double MaximumAbundance
        {
            get { return this.maximumAbundance; }
            private set { this.RaiseAndSetIfChanged(ref this.maximumAbundance, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether a linear or logarithmic axis should be used for abundance.
        /// </summary>
        public bool IsLinearAbundanceAxis
        {
            get { return this.isLinearAbundanceAxis; }
            set { this.RaiseAndSetIfChanged(ref this.isLinearAbundanceAxis, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether a linear or logarithmic axis should be used for abundance.
        /// </summary>
        public bool IsLogarithmicAbundanceAxis
        {
            get { return this.isLogarithmicAbundanceAxis; }
            set { this.RaiseAndSetIfChanged(ref this.isLogarithmicAbundanceAxis, value); }
        }

        /// <summary>
        /// Gets or sets the size (height) of each feature.
        /// </summary>
        public double FeatureSize
        {
            get { return this.featureSize; }
            set { this.RaiseAndSetIfChanged(ref this.featureSize, value); }
        }

        /// <summary>
        /// Highlight an identification on the feature map plot.
        /// </summary>
        /// <param name="prsm">Identification to highlight.</param>
        public void SetHighlight(PrSm prsm)
        {
            if (prsm == null)
            {
                return;
            }

            var rt = prsm.RetentionTime;
            var mass = prsm.Mass;
            if (this.highlight == null)
            {
                this.highlight = new RectangleAnnotation
                {
                    TextPosition = new DataPoint(rt, mass),
                    Fill = OxyColor.FromArgb(100, 255, 255, 0),
                    Stroke = OxyColor.FromRgb(0, 255, 0),
                    StrokeThickness = 2,
                    MinimumX = rt - ((this.xaxis.ActualMaximum - this.xaxis.ActualMinimum) * HighlightSize * 0.5),
                    MaximumX = rt + ((this.xaxis.ActualMaximum - this.xaxis.ActualMinimum) * HighlightSize * 0.5),
                    MinimumY = mass - ((this.yaxis.ActualMaximum - this.yaxis.ActualMinimum) * HighlightSize),
                    MaximumY = mass + ((this.yaxis.ActualMaximum - this.yaxis.ActualMinimum) * HighlightSize),
                };

                this.FeatureMap.Annotations.Add(this.highlight);
            }
            else
            {
                this.highlight.TextPosition = new DataPoint(rt, mass);
                this.highlight.MinimumX = rt - ((this.xaxis.ActualMaximum - this.xaxis.ActualMinimum) * HighlightSize * 0.5);
                this.highlight.MaximumX = rt + ((this.xaxis.ActualMaximum - this.xaxis.ActualMinimum) * HighlightSize * 0.5);
                this.highlight.MinimumY = mass - ((this.yaxis.ActualMaximum - this.yaxis.ActualMinimum) * HighlightSize);
                this.highlight.MaximumY = mass + ((this.yaxis.ActualMaximum - this.yaxis.ActualMinimum) * HighlightSize);
            }
        }

        /// <summary>
        /// Build feature map plot.
        /// </summary>
        /// <param name="features">The features.</param>
        /// <param name="ids">The ids.</param>
        private void BuildPlot(IList<Feature> features, IList<PrSm> ids)
        {
            this.ResetFeaturePlot(); // clear existing plot

            // Calculate min/max abundance and min/max score
            if (features.Count == 0)
            {
                return;
            }

            this.MinimumAbundance = Math.Round(features[features.Count - 1].MinPoint.Abundance, 3);
            this.MaximumAbundance = Math.Round(features[0].MinPoint.Abundance, 3);

            var minAbundance = features.Min(f => f.MinPoint.Abundance);
            var maxAbundance = features.Max(f => f.MaxPoint.Abundance);

            if (!this.IsLinearAbundanceAxis)
            {
                minAbundance = Math.Log10(minAbundance);
                maxAbundance = Math.Log10(maxAbundance);
            }

            this.featureColorAxis.StringFormat = this.IsLinearAbundanceAxis ? "0.###E0" : "0.#";

            // Set bounds for color axis for features
            this.featureColorAxis.AbsoluteMinimum = minAbundance;
            this.featureColorAxis.Minimum = minAbundance;
            this.featureColorAxis.Maximum = maxAbundance;
            this.featureColorAxis.AbsoluteMaximum = maxAbundance;

            var idPrSms = new List<PrSm>();

            foreach (var feature in features)
            {
                // Add ms2s associated with features
                var prsms = ids.Where(prsm => feature.AssociatedMs2.Contains(prsm.Scan)).ToList();
                ////var prsms = feature.AssociatedMs2.Where(scan => this.ids.ContainsKey(scan)).Select(scan => this.ids[scan]).ToList();

                // identified ms2s
                if (prsms.Count > 0)
                {
                    var feature1 = feature;
                    idPrSms.AddRange(prsms.Where(prsm => prsm.Sequence.Count > 0 && Math.Abs(prsm.Mass - feature1.MinPoint.Mass) < 1));
                    var notIdentifiedPrSms = prsms.Where(prsm => prsm.Sequence.Count == 0 || Math.Abs(prsm.Mass - feature1.MinPoint.Mass) >= 1);
                    ////this.CreateMs2ScatterSeries(idPrSms, feature, this.ms2ColorAxis, "Identified Ms2s", Size, this.showFoundIdMs2);
                    var ms2UnIdSeries = this.CreateMs2ScatterSeries(notIdentifiedPrSms, feature, this.ms2ColorAxis, "Unidentified Ms2s", this.FeatureSize, this.showFoundUnIdMs2);
                    ////this.foundUnIdMs2S.Add(ms2UnIdSeries);
                    this.FeatureMap.Series.Add(ms2UnIdSeries);
                }

                // Create and add feature
                this.FeatureMap.Series.Add(this.CreateFeatureSeries(feature, this.featureColorAxis.Palette.Colors, this.FeatureSize, minAbundance, maxAbundance));
            }

            ////this.AddMs2IdPoints(idPrSms, this.ms2ColorAxis, this.ShowFoundIdMs2);
            foreach (var series in this.ms2SeriesDictionary)
            {
                this.FeatureMap.Series.Add(series.Value);
            }

            // Add identified Ms2s with no associated features
            ////this.notFoundMs2S = this.CreateMs2ScatterSeries(this.notFoundMs2, null, this.ms2ColorAxis, "Identified Ms2 (No Feature)", 0, this.showNotFoundMs2);
            ////this.FeatureMap.Series.Add(this.notFoundMs2S);

            // Highlight selected identification.
            ////this.SetHighlight(this.SelectedPrSm);
            this.FeatureMap.IsLegendVisible = false;
            this.FeatureMap.InvalidatePlot(true);
        }

        /// <summary>
        /// Reset plot by clearing all series and resetting minimum and maximum abundance.
        /// </summary>
        private void ResetFeaturePlot()
        {
            this.MinimumAbundance = 0.0;
            this.MaximumAbundance = 0.0;
            this.FeatureMap.Series.Clear();
            this.FeatureMap.InvalidatePlot(true);
        }

        /// <summary>
        /// Select random colors from the palette and assign one to each unique protein.
        /// </summary>
        /// <param name="data">The identifications.</param>
        /// <param name="palette">The palette to select colors from.</param>
        private void SetProteinColorDictionary(IEnumerable<PrSm> data, OxyPalette palette)
        {
            if (this.colorDictionary == null)
            {
                this.colorDictionary = new Dictionary<string, int> { { string.Empty, 0 } };
            }

            var uniqueProteins = data.Select(d => d.ProteinName).Distinct();
            var rand = new Random();
            foreach (var protein in uniqueProteins)
            {
                if (!this.colorDictionary.ContainsKey(protein))
                {
                    int colorIndex;
                    do
                    {   // do not select the same color as unid color.
                        var r = rand.Next(0, DistinctMsMsColors);
                        colorIndex = Math.Min(r * (palette.Colors.Count / DistinctMsMsColors), palette.Colors.Count - 1);
                    }
                    while (colorIndex == 0);

                    this.colorDictionary.Add(protein, colorIndex);
                }
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
            if (!this.IsLinearAbundanceAxis)
            {
                abundance = Math.Log10(abundance);
            }

            var colorIndex = 1
                             + (int)
                               ((abundance - minAbundance) / (maxAbundance - minAbundance)
                                * colors.Count);
            colorIndex = Math.Max(1, Math.Min(colorIndex, colors.Count - 1));

            var c = colors[colorIndex];
            string trackerString = "{0} (ID: {Id:0})" + Environment.NewLine +
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
                TrackerFormatString = trackerString
            };
        }

        /// <summary>
        /// Groups MS/MS ID points in series by protein name.
        /// </summary>
        /// <param name="prsms">The IDs.</param>
        /// <param name="colorAxis">The color axis to select colors from.</param>
        /// <param name="visible">Should the series be visible?</param>
        private void AddMs2IdPoints(IEnumerable<PrSm> prsms, LinearColorAxis colorAxis, bool visible)
        {
            this.ms2SeriesDictionary.Clear();
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
                string name = proteinName;
                var scatterSeries = new ScatterSeries
                {
                    ItemsSource = prsmByProtein[proteinName],
                    Mapping = p =>
                    {
                        var prsm = (PrSm)p;
                        return new ScatterPoint(prsm.RetentionTime, prsm.Mass, 2.0, this.colorDictionary[name]);
                    },
                    Title = proteinName,
                    MarkerType = MarkerType.Cross,
                    ColorAxisKey = colorAxis.Key,
                    TrackerFormatString =
                        "{0}" + Environment.NewLine +
                        "{1}: {2:0.###}" + Environment.NewLine +
                        "{2}: {4:0.###E0}",
                    IsVisible = visible,
                };

                this.ms2SeriesDictionary.Add(proteinName, scatterSeries);
            }
        }

        /// <summary>
        /// Create scatter series for MS/MS points.
        /// </summary>
        /// <param name="prsms">List of identifications containing MS/MS scans to create scatter points for.</param>
        /// <param name="feature">The feature that the MS/MS scans are associated with (can be null)</param>
        /// <param name="colorAxis">The color axis to use to color the scatter points</param>
        /// <param name="title">The title of the scatter series</param>
        /// <param name="size">The size of the scatter points</param>
        /// <param name="visible">Is this MS/MS series visible?</param>
        /// <returns>The scatter series for the MS/MS scans</returns>
        private ScatterSeries CreateMs2ScatterSeries(IEnumerable<PrSm> prsms, Feature feature, LinearColorAxis colorAxis, string title, double size, bool visible)
        {
            return new ScatterSeries
            {
                ItemsSource = prsms,
                Mapping = p =>
                {
                    var prsm = (PrSm)p;
                    return new ScatterPoint(prsm.RetentionTime, feature == null ? prsm.Mass : feature.MinPoint.Mass, Math.Max(size * 0.8, 3.0), this.colorDictionary[prsm.ProteinName]);
                },
                Title = title,
                MarkerType = MarkerType.Cross,
                ColorAxisKey = colorAxis.Key,
                TrackerFormatString =
                    "{0}" + Environment.NewLine +
                    "{1}: {2:0.###}" + Environment.NewLine +
                    "{2}: {4:0.###E0}",
                IsVisible = visible,
            };
        }

        /// <summary>
        /// Implementation for <see cref="SaveAsImageCommand" />.
        /// Gets a command that saves the feature map as a PNG image.
        /// </summary>
        private void SaveAsImageImplementation()
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
                    this.FeatureMap,
                    filePath,
                    (int)this.FeatureMap.Width,
                    (int)this.FeatureMap.Height,
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
