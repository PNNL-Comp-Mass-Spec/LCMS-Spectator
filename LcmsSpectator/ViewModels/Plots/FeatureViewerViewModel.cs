// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FeatureViewerViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for feature map plot.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels.Plots
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;
    using InformedProteomics.Backend.MassSpecData;
    using LcmsSpectator.Config;
    using LcmsSpectator.DialogServices;
    using LcmsSpectator.Models;
    using LcmsSpectator.PlotModels.ColorDicionaries;
    using LcmsSpectator.Readers;
    using LcmsSpectator.Utils;
    using OxyPlot;
    using OxyPlot.Annotations;
    using OxyPlot.Axes;
    using OxyPlot.Series;
    using ReactiveUI;
    using Constants = InformedProteomics.Backend.Data.Biology.Constants;

    /// <summary>
    /// View model for feature map plot.
    /// </summary>
    public class FeatureViewerViewModel : ReactiveObject
    {
        /// <summary>
        /// Default size of highlight annotation.
        /// </summary>
        private const double HighlightSize = 0.008;

        /// <summary>
        /// Dialog service for opening dialogs from the view model.
        /// </summary>
        private readonly IDialogService dialogService;

        /// <summary>
        /// The LCMSRun for the data set this feature map shows.
        /// </summary>
        private readonly LcMsRun lcms;

        /// <summary>
        /// The X Axis of the feature map.
        /// </summary>
        private readonly LinearAxis xaxis;

        /// <summary>
        /// The Y Axis of the feature map.
        /// </summary>
        private readonly LinearAxis yaxis;

        /// <summary>
        /// Color axis for MS/MS scan points.
        /// </summary>
        private readonly LinearColorAxis ms2ColorAxis;

        /// <summary>
        /// Color axis for features.
        /// </summary>
        private readonly LinearColorAxis featureColorAxis;

        /// <summary>
        /// Dictionary associating unique protein to MS/MS series
        /// </summary>
        private readonly Dictionary<string, ScatterSeries> ms2SeriesDictionary;

        /// <summary>
        /// Dictionary associating unique protein to color index.
        /// </summary>
        private readonly ProteinColorDictionary colorDictionary;

        /// <summary>
        /// MS/MS scan highlight annotation.
        /// </summary>
        private RectangleAnnotation highlight;

        /// <summary>
        /// MS/MS scans that are identified.
        /// </summary>
        private List<Series> foundIdMs2S;

        /// <summary>
        /// MS/MS scans that are not identified.
        /// </summary>
        private List<Series> foundUnIdMs2S;

        /// <summary>
        /// MS/MS scans that are identified, but not associated with a feature.
        /// </summary>
        ////private Series notFoundMs2S;

        /// <summary>
        /// The feature data displayed on the feature map.
        /// </summary>
        private List<Feature> features;

        /// <summary>
        /// Dictionary that maps feature points to the feature that they are part of.
        /// </summary>
        private Dictionary<Feature.FeaturePoint, Feature> featuresByScan;

        /// <summary>
        /// Dictionary that maps scan numbers to identifications.
        /// </summary>
        private Dictionary<int, PrSm> ids;

        /// <summary>
        /// The identifications that are not associated with features.
        /// </summary>
        private List<PrSm> notFoundMs2;

        /// <summary>
        /// The feature point selected by the user.
        /// </summary>
        private Feature.FeaturePoint selectedFeaturePoint;

        /// <summary>
        /// The identification selected by the user.
        /// </summary>
        private PrSm selectedPrSmPoint;

        /// <summary>
        /// A value indicating whether the manual adjustment text boxes should be displayed.
        /// </summary>
        private bool showManualAdjustment;

        /// <summary>
        /// The Plot model for the feature map.
        /// </summary>
        private PlotModel featureMap;

        /// <summary>
        /// The minimum for the X axis of the feature map.
        /// </summary>
        private double featurePlotXMinimum;

        /// <summary>
        /// The maximum for the X axis of the feature map.
        /// </summary>
        private double featurePlotXMaximum;

        /// <summary>
        /// The minimum for the X axis of the feature map.
        /// </summary>
        private double featurePlotYMinimum;

        /// <summary>
        /// The maximum for the Y axis of the feature map.
        /// </summary>
        private double featurePlotYMaximum;

        /// <summary>
        /// The view model for the isotopic envelope spectrum.
        /// </summary>
        private IsotopicEnvelopePlotViewModel isotopicEnvelope;

        /// <summary>
        /// A value indicating whether the expander control for the isotopic envelope plot should be expanded or not.
        /// </summary>
        private bool isotopicEnvelopeExpanded;

        /// <summary>
        /// The lowest abundance currently being shown on feature map.
        /// </summary>
        private double minimumAbundance;

        /// <summary>
        /// The highest abundance currently being shown on feature map.
        /// </summary>
        private double maximumAbundance;

        /// <summary>
        /// The minimum value of the abundance threshold slider.
        /// </summary>
        private double minimumAbundanceThreshold;

        /// <summary>
        /// The maximum value of the abundance threshold slider.
        /// </summary>
        private double maximumAbundanceThreshold;

        /// <summary>
        /// The total number of features being displayed on feature map
        /// </summary>
        private int pointsDisplayed;

        /// <summary>
        /// The lowest possible abundance. This is stored in Log10(Abundance).
        /// This is set by the abundance threshold slider.
        /// </summary>
        private double abundanceThreshold;

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
        /// The currently selected identification. This is the ID highlighted on the feature
        /// map plot.
        /// </summary>
        private PrSm selectedPrSm;

        /// <summary>
        /// The feature selected by double clicking on feature map.
        /// </summary>
        private Feature.FeaturePoint selectedFeature;

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
        /// A value indicating whether the splash screen is currently being shown.
        /// </summary>
        private bool showSplash;

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureViewerViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">The dialog service for opening dialogs from the view model.</param>
        /// <param name="lcms">The LCMSRun for the data set.</param>
        public FeatureViewerViewModel(IDialogService dialogService, LcMsRun lcms)
        {
            this.dialogService = dialogService;
            this.lcms = lcms;

            this.ms2SeriesDictionary = new Dictionary<string, ScatterSeries>();

            this.ShowFoundIdMs2 = false;
            this.ShowFoundUnIdMs2 = false;
            this.ShowNotFoundMs2 = false;

            this.IsLinearAbundanceAxis = false;
            this.IsLogarithmicAbundanceAxis = true;
            this.FeatureSize = 0.1;

            // Initialize isotopic envelope plot.
            this.IsotopicEnvelope = new IsotopicEnvelopePlotViewModel();
            this.IsotopicEnvelopeExpanded = false;
            this.PointsDisplayed = 5000;

            // Initialize color axes.
            const int NumColors = 5000;
            ////var minColor = OxyColor.FromRgb(255, 160, 160);
            ////var midColor = OxyColor.FromRgb(255, 0, 0);
            ////var maxColor = OxyColor.FromRgb(150, 0, 0);
            var minColor = OxyColors.Black;
            var lowMidColor = OxyColors.Blue;
            var highMidColor = OxyColors.Orange;
            var maxColor = OxyColors.Red;
            this.featureColorAxis = new LinearColorAxis     // Color axis for features
            {
                Title = "Abundance",
                Position = AxisPosition.Right,
                Palette = OxyPalette.Interpolate(NumColors, minColor, lowMidColor, highMidColor, maxColor),
            };

            this.colorDictionary = new ProteinColorDictionary();
            this.ms2ColorAxis = new LinearColorAxis      // Color axis for ms2s
            {
                Key = "ms2s",
                Position = AxisPosition.None,
                Palette = this.colorDictionary.OxyPalette,
                Minimum = 1,
                Maximum = this.colorDictionary.OxyPalette.Colors.Count,
            };

            // Initialize x and y axes.
            this.yaxis = new LinearAxis { Position = AxisPosition.Left, Title = "Monoisotopic Mass", StringFormat = "0.###" };
            this.xaxis = new LinearAxis { Position = AxisPosition.Bottom, Title = "Retention Time", StringFormat = "0.###", };

            // Change size of scan highlight annotation when feature map x and y axes are zoomed or panned.
            bool isInternalChange = false;
            this.xaxis.AxisChanged += (o, e) =>
            {
                this.FeaturePlotXMinimum = Math.Round(this.xaxis.ActualMinimum, 3);
                this.FeaturePlotXMaximum = Math.Round(this.xaxis.ActualMaximum, 3);
                if (!isInternalChange && this.highlight != null &&
                    this.highlight.TextPosition.X >= this.xaxis.ActualMinimum && this.highlight.TextPosition.X <= this.xaxis.ActualMaximum &&
                   this.highlight.TextPosition.Y >= this.yaxis.ActualMinimum && this.highlight.TextPosition.Y <= this.yaxis.ActualMaximum)
                {
                    var x = this.highlight.TextPosition.X;
                    isInternalChange = true;
                    this.highlight.MinimumX = x - ((this.xaxis.ActualMaximum - this.xaxis.ActualMinimum) * HighlightSize * 0.5);
                    this.highlight.MaximumX = x + ((this.xaxis.ActualMaximum - this.xaxis.ActualMinimum) * HighlightSize * 0.5);
                }

                isInternalChange = false;
            };
            this.yaxis.AxisChanged += (o, e) =>
            {
                this.FeaturePlotYMinimum = Math.Round(this.yaxis.ActualMinimum, 3);
                this.FeaturePlotYMaximum = Math.Round(this.yaxis.ActualMaximum, 3);
                if (!isInternalChange && this.highlight != null &&
                    this.highlight.TextPosition.X >= this.xaxis.ActualMinimum && this.highlight.TextPosition.X <= this.xaxis.ActualMaximum &&
                    this.highlight.TextPosition.Y >= this.yaxis.ActualMinimum && this.highlight.TextPosition.Y <= this.yaxis.ActualMaximum)
                {
                    var y = this.highlight.TextPosition.Y;
                    isInternalChange = true;
                    this.highlight.MinimumY = y - ((this.yaxis.ActualMaximum - this.yaxis.ActualMinimum) * HighlightSize);
                    this.highlight.MaximumY = y + ((this.yaxis.ActualMaximum - this.yaxis.ActualMinimum) * HighlightSize);
                }

                isInternalChange = false;
            };

            // Initialize feature map.
            this.FeatureMap = new PlotModel { Title = "Feature Map" };
            this.FeatureMap.MouseDown += this.FeatureMapMouseDown;
            this.FeatureMap.Axes.Add(this.featureColorAxis);
            this.FeatureMap.Axes.Add(this.ms2ColorAxis);
            this.FeatureMap.Axes.Add(this.xaxis);
            this.FeatureMap.Axes.Add(this.yaxis);

            // When ShowNoutFoundMs2 changes, update the NoutFoundMs2 series
            ////this.WhenAnyValue(x => x.ShowNotFoundMs2)
            ////    .Where(_ => this.notFoundMs2S != null && this.FeatureMap != null)
            ////    .Subscribe(showNotFoundMs2 =>
            ////    {
            ////        this.notFoundMs2S.IsVisible = showNotFoundMs2;
            ////        this.FeatureMap.InvalidatePlot(true);
            ////    });

            // When ShowFoundIdMs2 changes, update all ms2 series
            this.WhenAnyValue(x => x.ShowFoundIdMs2)
                .Where(_ => this.foundIdMs2S != null && this.FeatureMap != null)
                .Subscribe(showFoundMs2 =>
                    {
                        foreach (var ms2 in this.ms2SeriesDictionary)
                        {
                            ms2.Value.IsVisible = showFoundMs2;
                        }

                        this.FeatureMap.InvalidatePlot(true);
                    });

            // When ShowFoundUnIdMs2 changes, update all ms2 series
            this.WhenAnyValue(x => x.ShowFoundUnIdMs2)
                .Where(_ => this.foundUnIdMs2S != null && this.FeatureMap != null)
                .Subscribe(showFoundMs2 =>
                    {
                        foreach (var ms2 in this.foundUnIdMs2S)
                        {
                            ms2.IsVisible = showFoundMs2;
                        }

                        this.FeatureMap.InvalidatePlot(true);
                    });

            // When SelectedPrSm is changed, update highlighted prsm on plot
            this.WhenAnyValue(x => x.SelectedPrSm)
                .Where(selectedPrSm => selectedPrSm != null)
                .Subscribe(selectedPrSm =>
                {
                    this.SetHighlight(selectedPrSm);
                    this.FeatureMap.InvalidatePlot(true);
                    this.SelectedPrSm.WhenAnyValue(x => x.Scan)
                        .Subscribe(scan =>
                        {
                            this.SetHighlight(selectedPrSm);
                            this.FeatureMap.InvalidatePlot(true);
                        });
                });

            // Update plot axes when FeaturePlotXMin, YMin, XMax, and YMax change
            this.WhenAnyValue(x => x.FeaturePlotXMinimum, x => x.FeaturePlotXMaximum)
                .Throttle(TimeSpan.FromSeconds(1), RxApp.TaskpoolScheduler)
                .Where(x => !this.xaxis.ActualMinimum.Equals(x.Item1) || !this.xaxis.ActualMaximum.Equals(x.Item2))
                .Subscribe(
                    x =>
                        {
                            this.xaxis.Zoom(x.Item1, x.Item2);
                            this.FeatureMap.InvalidatePlot(false);
                        });
            this.WhenAnyValue(y => y.FeaturePlotYMinimum, x => x.FeaturePlotYMaximum)
                .Throttle(TimeSpan.FromSeconds(1), RxApp.TaskpoolScheduler)
                .Where(y => !this.yaxis.ActualMinimum.Equals(y.Item1) || !this.yaxis.ActualMaximum.Equals(y.Item2))
                .Subscribe(
                    y =>
                        {
                            this.yaxis.Zoom(y.Item1, y.Item2);
                            this.FeatureMap.InvalidatePlot(false);
                        });

            this.WhenAnyValue(x => x.IsLinearAbundanceAxis)
                .Subscribe(isLinearAbundanceAxis => this.IsLogarithmicAbundanceAxis = !isLinearAbundanceAxis);

            this.WhenAnyValue(x => x.IsLogarithmicAbundanceAxis)
                .Subscribe(isLogarithmicAbundanceAxis => this.IsLinearAbundanceAxis = !isLogarithmicAbundanceAxis);

            this.WhenAnyValue(x => x.IsLinearAbundanceAxis)
                .Subscribe(_ => this.BuildPlot(this.AbundanceThreshold, this.PointsDisplayed));

            // Update plot if score threshold, abundance threshold, or points displayed changes
            this.WhenAnyValue(x => x.AbundanceThreshold, x => x.PointsDisplayed, x => x.FeatureSize)
                .Throttle(TimeSpan.FromMilliseconds(500), RxApp.TaskpoolScheduler)
                .Subscribe(x => this.BuildPlot(x.Item1, x.Item2));

            var featureSelectedCommand = ReactiveCommand.Create();
            featureSelectedCommand.Subscribe(_ => this.FeatureSelectedImplementation());
            this.FeatureSelectedCommand = featureSelectedCommand;

            var openFeatureFileCommand = ReactiveCommand.Create();
            openFeatureFileCommand.Subscribe(_ => this.OpenFeatureFileImplementation());
            this.OpenFeatureFileCommand = openFeatureFileCommand;

            // Save As Image Command requests a file path from the user and then saves the spectrum plot as an image
            var saveAsImageCommand = ReactiveCommand.Create();
            saveAsImageCommand.Subscribe(_ => this.SaveAsImageImplementation());
            this.SaveAsImageCommand = saveAsImageCommand;
        }

        /// <summary>
        /// Gets a command that displays open file dialog to select feature file and then read and display features.
        /// </summary>
        public IReactiveCommand OpenFeatureFileCommand { get; private set; }

        /// <summary>
        /// Gets a command that saves the feature map as a PNG image.
        /// </summary>
        public IReactiveCommand SaveAsImageCommand { get; private set; }

        /// <summary>
        /// Gets a command activated when a feature is selected (double clicked) on the
        /// feature map plot.
        /// </summary>
        public IReactiveCommand FeatureSelectedCommand { get; private set; }

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
        public double FeaturePlotXMinimum
        {
            get { return this.featurePlotXMinimum; }
            set { this.RaiseAndSetIfChanged(ref this.featurePlotXMinimum, value); }
        }

        /// <summary>
        /// Gets or sets the maximum for the X axis of the feature map.
        /// </summary>
        public double FeaturePlotXMaximum
        {
            get { return this.featurePlotXMaximum; }
            set { this.RaiseAndSetIfChanged(ref this.featurePlotXMaximum, value); }
        }

        /// <summary>
        /// Gets or sets the minimum for the Y axis of the feature map.
        /// </summary>
        public double FeaturePlotYMinimum
        {
            get { return this.featurePlotYMinimum; }
            set { this.RaiseAndSetIfChanged(ref this.featurePlotYMinimum, value); }
        }

        /// <summary>
        /// Gets or sets the maximum for the Y axis of the feature map.
        /// </summary>
        public double FeaturePlotYMaximum
        {
            get { return this.featurePlotYMaximum; }
            set { this.RaiseAndSetIfChanged(ref this.featurePlotYMaximum, value); }
        }

        /// <summary>
        /// Gets the view model for the isotopic envelope spectrum.
        /// </summary>
        public IsotopicEnvelopePlotViewModel IsotopicEnvelope
        {
            get { return this.isotopicEnvelope; }
            private set { this.RaiseAndSetIfChanged(ref this.isotopicEnvelope, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the expander control for the isotopic envelope plot should be expanded or not.
        /// </summary>
        public bool IsotopicEnvelopeExpanded
        {
            get { return this.isotopicEnvelopeExpanded; }
            set { this.RaiseAndSetIfChanged(ref this.isotopicEnvelopeExpanded, value); }
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
        /// Gets or sets the minimum value of the abundance threshold slider.
        /// </summary>
        public double MinimumAbundanceThreshold
        {
            get { return this.minimumAbundanceThreshold; }
            set { this.RaiseAndSetIfChanged(ref this.minimumAbundanceThreshold, value); }
        }

        /// <summary>
        /// Gets or sets the maximum value of the abundance threshold slider.
        /// </summary>
        public double MaximumAbundanceThreshold
        {
            get { return this.maximumAbundanceThreshold; }
            set { this.RaiseAndSetIfChanged(ref this.maximumAbundanceThreshold, value); }
        }

        /// <summary>
        /// Gets or sets the total number of features being displayed on feature map
        /// </summary>
        public int PointsDisplayed
        {
            get { return this.pointsDisplayed; }
            set { this.RaiseAndSetIfChanged(ref this.pointsDisplayed, value); }
        }

        /// <summary>
        /// Gets or sets the lowest possible abundance. This is stored in Log10(Abundance).
        /// This is set by the abundance threshold slider.
        /// </summary>
        public double AbundanceThreshold
        {
            get { return this.abundanceThreshold; }
            set { this.RaiseAndSetIfChanged(ref this.abundanceThreshold, value); }
        }
      
        /// <summary>
        /// Gets or sets a value indicating whether whether the  identified MS/MS points associated with features are being shown on
        /// the feature map plot.
        /// </summary>
        public bool ShowFoundIdMs2
        {
            get { return this.showFoundIdMs2; }
            set { this.RaiseAndSetIfChanged(ref this.showFoundIdMs2, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the unidentified MS/mS points associated with features are being shown on
        /// the feature map plot.
        /// </summary>
        public bool ShowFoundUnIdMs2
        {
            get { return this.showFoundUnIdMs2; }
            set { this.RaiseAndSetIfChanged(ref this.showFoundUnIdMs2, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the MS/MS points not associated with features are being shown
        /// on the feature map plot.
        /// </summary>
        public bool ShowNotFoundMs2
        {
            get { return this.showNotFoundMs2; }
            set { this.RaiseAndSetIfChanged(ref this.showNotFoundMs2, value); }
        }

        /// <summary>
        /// Gets or sets the currently selected identification. This is the ID highlighted on the feature
        /// map plot.
        /// </summary>
        public PrSm SelectedPrSm
        {
            get { return this.selectedPrSm; }
            set { this.RaiseAndSetIfChanged(ref this.selectedPrSm, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the manual adjustment text boxes should be displayed.
        /// </summary>
        public bool ShowManualAdjustment
        {
            get { return this.showManualAdjustment; }
            set { this.RaiseAndSetIfChanged(ref this.showManualAdjustment, value); }
        }

        /// <summary>
        /// Gets the feature selected by double clicking on feature map.
        /// </summary>
        public Feature.FeaturePoint SelectedFeature
        {
            get { return this.selectedFeature; }
            private set { this.RaiseAndSetIfChanged(ref this.selectedFeature, value); }
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
        /// Gets or sets a value indicating whether the splash screen is currently being shown.
        /// </summary>
        public bool ShowSplash
        {
            get { return this.showSplash; }
            set { this.RaiseAndSetIfChanged(ref this.showSplash, value); }
        }

        /// <summary>
        /// Gets or sets the path to the feature file that this view model visualizes.
        /// </summary>
        public string FeatureFilePath { get; set; }

        /// <summary>
        /// Read features and display feature map for this data set.
        /// </summary>
        /// <param name="filePath">Path of feature file.</param>
        public void OpenFeatureFile(string filePath)
        {
            try
            {
                var featureList = FeatureReader.Read(filePath);
                this.SetFeatures(featureList.ToList());
                this.FeatureFilePath = filePath;
                this.ShowSplash = false;
            }
            catch (InvalidCastException)
            {
                this.dialogService.MessageBox("Cannot open features for this type of data set.");
            }
        }

        /// <summary>
        /// Display open file dialog to select feature file and then read and display features.
        /// </summary>
        public void OpenFeatureFileImplementation()
        {
            var tsvFileName = this.dialogService.OpenFile(FileConstants.FeatureFileExtensions[0], FileConstants.FeatureFileFormatString);
            if (!string.IsNullOrEmpty(tsvFileName))
            {
                this.ShowSplash = false;
                var featureList = FeatureReader.Read(tsvFileName);
                this.SetFeatures(featureList.ToList());
            }
        }

        /// <summary>
        /// Set data to be shown on feature map.
        /// </summary>
        /// <param name="featureList">Features to be displayed.</param>
        /// <param name="updatePlot">Should the plot be updated after setting data?</param>
        public void SetFeatures(IEnumerable<Feature> featureList, bool updatePlot = true)
        {
            this.features = featureList.ToList();
            this.featuresByScan = new Dictionary<Feature.FeaturePoint, Feature>();
            foreach (var feature in this.features)
            {
                this.featuresByScan.Add(feature.MinPoint, feature);
                this.featuresByScan.Add(feature.MaxPoint, feature);
                feature.MinPoint.RetentionTime = this.lcms.GetElutionTime(feature.MinPoint.Scan);
                feature.MaxPoint.RetentionTime = this.lcms.GetElutionTime(feature.MaxPoint.Scan);

                for (int c = feature.MinPoint.Charge; c <= feature.MaxPoint.Charge; c++)
                {
                    var mz = (feature.MinPoint.Mass + (c * Constants.Proton)) / c;
                    feature.AssociatedMs2.AddRange(this.lcms.GetFragmentationSpectraScanNums(mz)
                                           .Where(s => s >= feature.MinPoint.Scan && s <= feature.MaxPoint.Scan));
                }
            }

            this.MaximumAbundanceThreshold = Math.Log10(this.features.Max(f => f.MinPoint.Abundance));
            this.MinimumAbundanceThreshold = Math.Log10(this.features.Min(f => f.MinPoint.Abundance));
            this.AbundanceThreshold = Math.Max(this.maximumAbundanceThreshold, this.minimumAbundance);
            this.yaxis.AbsoluteMinimum = 0;
            this.yaxis.AbsoluteMaximum = this.features.Max(f => f.MinPoint.Mass);
            this.xaxis.AbsoluteMinimum = 0;
            this.xaxis.AbsoluteMaximum = this.lcms.MaxLcScan;

            if (updatePlot)
            {
                this.BuildPlot(this.abundanceThreshold, this.pointsDisplayed);
            }

            this.ShowSplash = false;
        }

        /// <summary>
        /// Update identifications to be displayed.
        /// Always updates feature map plot when called.
        /// </summary>
        /// <param name="idList">The identifications to display.</param>
        /// <param name="updatePlot">Should the plot be updated after setting ids?</param>
        public void UpdateIds(IEnumerable<PrSm> idList, bool updatePlot = true)
        {
            this.ids = new Dictionary<int, PrSm>();
            foreach (var id in idList)
            {
                if (id.Sequence.Count > 0 && !this.ids.ContainsKey(id.Scan))
                {
                    this.ids.Add(id.Scan, id);
                }
            }

            if (this.features == null)
            {
                return;
            }

            this.notFoundMs2 = new List<PrSm>();
            var scanHash = new Dictionary<int, List<double>>();
            var accountedFor = new HashSet<int>();
            var sortedIds = this.ids.Values.OrderBy(id => id.Scan).ToList();
            foreach (var feature in this.features)
            {
                var minIndex = sortedIds.BinarySearch(
                    new PrSm { Scan = feature.MinPoint.Scan },
                    new PrSm.PrSmScanComparer());
                if (minIndex < 0)
                {
                    minIndex *= -1;
                }

                minIndex = Math.Max(Math.Min(minIndex, sortedIds.Count), 0);
                var maxIndex = sortedIds.BinarySearch(
                    new PrSm { Scan = feature.MaxPoint.Scan },
                    new PrSm.PrSmScanComparer());
                if (maxIndex < 0)
                {
                    maxIndex *= -1;
                }

                maxIndex = Math.Max(Math.Min(maxIndex, sortedIds.Count), 0);
                for (int i = minIndex; i < maxIndex; i++)
                {
                    var id = sortedIds[i];
                    if (accountedFor.Contains(id.Scan) || id.Scan < feature.MinPoint.Scan
                        || id.Scan > feature.MaxPoint.Scan || !(Math.Abs(id.Mass - feature.MinPoint.Mass) < 2)
                        || feature.AssociatedMs2.Contains(id.Scan))
                    {
                        continue;
                    }

                    feature.AssociatedMs2.Add(id.Scan);
                    accountedFor.Add(id.Scan);
                }

                foreach (var scan in feature.AssociatedMs2)
                {
                    if (!this.ids.ContainsKey(scan))
                    {
                        this.ids.Add(scan, new PrSm { Scan = scan, LcMs = this.lcms, Mass = feature.MinPoint.Mass });
                    }

                    if (!accountedFor.Contains(scan))
                    {
                        accountedFor.Add(scan);
                    }

                    this.Insert(scanHash, scan, this.ids[scan].Mass, 1);
                }
            }

            foreach (var id in this.ids.Values.Where(id => !accountedFor.Contains(id.Scan)))
            {
                this.notFoundMs2.Add(id);
            }

            if (updatePlot)
            {
                this.BuildPlot(this.abundanceThreshold, this.pointsDisplayed);
            }
        }

        /// <summary>
        /// Event handler for mouse click event to set SelectedDataPoint
        /// </summary>
        /// <param name="sender">The sender PlotView</param>
        /// <param name="args">The event arguments.</param>
        private void FeatureMapMouseDown(object sender, OxyMouseEventArgs args)
        {
            this.selectedFeaturePoint = null;
            this.selectedPrSmPoint = null;
            var series = this.FeatureMap.GetSeriesFromPoint(args.Position, 10);
            if (series is LineSeries)
            { // Was a feature clicked?
                var result = series.GetNearestPoint(args.Position, false);
                if (result == null)
                {
                    return;
                }

                var featurePoint = result.Item as Feature.FeaturePoint;
                if (featurePoint != null)
                {
                    // See if there is a ms2 point closer than this feature point
                    PrSm closestPrSm = null;
                    if (this.showFoundIdMs2 || this.showFoundUnIdMs2)
                    {
                        var feature = this.featuresByScan[featurePoint];
                        var ms2Scans = feature.AssociatedMs2;
                        var minDist = result.Position.DistanceToSquared(args.Position);
                        foreach (var scan in ms2Scans)
                        {
                            if (!this.ids.ContainsKey(scan))
                            {
                                continue;
                            }

                            var prsm = this.ids[scan];
                            if ((prsm.Sequence.Count == 0 && !this.showFoundUnIdMs2)
                                || (prsm.Sequence.Count > 0 && !this.showFoundIdMs2))
                            {
                                continue;
                            }

                            var sp = this.xaxis.Transform(prsm.RetentionTime, featurePoint.Mass, this.yaxis);
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
                        this.selectedPrSmPoint = closestPrSm;
                    }

                    this.selectedFeaturePoint = featurePoint;
                }
            }
            else if (series is ScatterSeries)
            { // Was a ms2 cross clicked?
                var result = series.GetNearestPoint(args.Position, false);
                if (result == null)
                {
                    return;
                }

                this.selectedPrSmPoint = result.Item as PrSm;
            }
        }

        /// <summary>
        /// Build feature map plot.
        /// </summary>
        /// <param name="abundance">Highest abundance to display</param>
        /// <param name="points">Maximum number of feature points to display</param>
        private void BuildPlot(double abundance, int points)
        {
            this.ResetFeaturePlot(); // clear existing plot
            // Filter features based on score threshold, abundance threshold, points to display
            var filteredFeatures = this.FilterData(this.features, abundance, points);

            // Calculate min/max abundance and min/max score
            if (filteredFeatures.Count == 0)
            {
                return;
            }

            this.MinimumAbundance = Math.Round(filteredFeatures[filteredFeatures.Count - 1].MinPoint.Abundance, 3);
            this.MaximumAbundance = Math.Round(filteredFeatures[0].MinPoint.Abundance, 3);

            var minAbundance = filteredFeatures.Min(f => f.MinPoint.Abundance);
            var maxAbundance = filteredFeatures.Max(f => f.MaxPoint.Abundance);   

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

            foreach (var feature in filteredFeatures)
            {
                // Add ms2s associated with features
                var prsms = feature.AssociatedMs2.Where(scan => this.ids.ContainsKey(scan)).Select(scan => this.ids[scan]).ToList();
                
                // identified ms2s
                if (prsms.Count > 0)
                {
                    var feature1 = feature;
                    idPrSms.AddRange(prsms.Where(prsm => prsm.Sequence.Count > 0 && Math.Abs(prsm.Mass - feature1.MinPoint.Mass) < 1));
                    var notIdentifiedPrSms = prsms.Where(prsm => prsm.Sequence.Count == 0 || Math.Abs(prsm.Mass - feature1.MinPoint.Mass) >= 1);
                    ////this.CreateMs2ScatterSeries(idPrSms, feature, this.ms2ColorAxis, "Identified Ms2s", Size, this.showFoundIdMs2);
                    var ms2UnIdSeries = this.CreateMs2ScatterSeries(notIdentifiedPrSms, feature, this.ms2ColorAxis, "Unidentified Ms2s", this.FeatureSize, this.showFoundUnIdMs2);
                    this.foundUnIdMs2S.Add(ms2UnIdSeries);
                    this.FeatureMap.Series.Add(ms2UnIdSeries);
                }

                // Create and add feature
                this.FeatureMap.Series.Add(this.CreateFeatureSeries(feature, this.featureColorAxis.Palette.Colors, this.FeatureSize, minAbundance, maxAbundance));
            }

            this.AddMs2IdPoints(idPrSms, this.ms2ColorAxis, this.ShowFoundIdMs2);
            foreach (var series in this.ms2SeriesDictionary)
            {
                this.FeatureMap.Series.Add(series.Value);
            }

            // Add identified Ms2s with no associated features
            ////this.notFoundMs2S = this.CreateMs2ScatterSeries(this.notFoundMs2, null, this.ms2ColorAxis, "Identified Ms2 (No Feature)", 0, this.showNotFoundMs2);
            ////this.FeatureMap.Series.Add(this.notFoundMs2S);
            
            // Highlight selected identification.
            this.SetHighlight(this.SelectedPrSm);
            this.FeatureMap.IsLegendVisible = false;
            this.FeatureMap.InvalidatePlot(true);
        }

        /// <summary>
        /// Reset plot by clearing all series and resetting minimum and maximum abundance.
        /// </summary>
        private void ResetFeaturePlot()
        {
            this.foundIdMs2S = new List<Series>();
            this.foundUnIdMs2S = new List<Series>();
            this.MinimumAbundance = 0.0;
            this.MaximumAbundance = 0.0;
            this.FeatureMap.Series.Clear();
            this.FeatureMap.InvalidatePlot(true);
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
                TrackerFormatString = trackerString,
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
                        return new ScatterPoint(prsm.RetentionTime, prsm.Mass, 2.0, this.colorDictionary.GetColorCode(name));
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
                    return new ScatterPoint(
                        prsm.RetentionTime,
                        feature == null ? prsm.Mass : feature.MinPoint.Mass,
                        Math.Max(size * 0.8, 3.0),
                        this.colorDictionary.GetColorCode(prsm.ProteinName));
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
        /// Called when a point has been double clicked on the feature map plot.
        /// Feature selected: Show observed/theoretical isotope envelopes
        /// MS/MS selected: Trigger event for the application to display this MS/MS.
        /// </summary>
        private void FeatureSelectedImplementation()
        {
            this.SelectedFeature = this.selectedFeaturePoint;
            if (this.selectedFeaturePoint != null)
            {
                this.BuildIsotopePlots();
            }

            if (this.selectedPrSmPoint != null)
            {
                this.SelectedPrSm = this.selectedPrSmPoint;
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

            prsm.LcMs = this.lcms;
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
        /// Build plot for observed/theoretical isotope envelopes.
        /// </summary>
        private void BuildIsotopePlots()
        {
            if (this.selectedFeaturePoint == null || this.selectedFeaturePoint.Isotopes.Length == 0)
            {
                return;
            }

            this.IsotopicEnvelope.BuildPlot(
                this.selectedFeaturePoint.Isotopes,
                this.selectedFeaturePoint.Mass,
                this.selectedFeaturePoint.Charge);
            this.IsotopicEnvelopeExpanded = true;
        }

        /// <summary>
        /// Filter feature list.
        /// </summary>
        /// <param name="featureList">Features to filter.</param>
        /// <param name="abundance">Minimum abundance threshold to filter by.</param>
        /// <param name="points">Maximum number of features.</param>
        /// <returns>List of filtered features.</returns>
        private List<Feature> FilterData(IEnumerable<Feature> featureList, double abundance, int points)
        {
            var maxAbundance = Math.Pow(abundance, 10);
            if (featureList == null)
            {
                return new List<Feature>();
            }

            var filteredFeatures =
                featureList.Where(feature => feature.MinPoint.Abundance <= maxAbundance) ////&& feature.MinPoint.Score >= scoreThreshold)
                         .OrderByDescending(feature => feature.MinPoint.Abundance).ToList();
            var numDisplayed = Math.Min(points, filteredFeatures.Count);
            var topNPoints = filteredFeatures.GetRange(0, numDisplayed);
            return topNPoints;
        }

        /// <summary>
        /// Insert a point into the scan vs mass table.
        /// </summary>
        /// <param name="points">Scan vs mass table.</param>
        /// <param name="scan">Scan of point to insert.</param>
        /// <param name="mass">Mass of point to insert.</param>
        /// <param name="massTolerance">Mass tolerance.</param>
        private void Insert(Dictionary<int, List<double>> points, int scan, double mass, double massTolerance = 0.1)
        {
            List<double> masses;
            if (!points.TryGetValue(scan, out masses))
            {
                points.Add(scan, new List<double> { mass });
            }
            else
            {
                var index = masses.BinarySearch(mass);
                if (index < 0)
                {
                    index *= -1;
                }

                var lowIndex = Math.Min(Math.Max(0, index - 1), masses.Count - 1);
                var highIndex = Math.Min(index + 1, masses.Count - 1);
                if (Math.Abs(mass - masses[highIndex]) <= massTolerance &&
                     Math.Abs(mass - masses[lowIndex]) <= massTolerance)
                {
                    masses.Insert(highIndex, mass);
                }
            }
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
