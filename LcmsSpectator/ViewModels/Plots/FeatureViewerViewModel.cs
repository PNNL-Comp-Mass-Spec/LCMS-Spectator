// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FeatureViewerViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for maintaining feature map plot and feature data manipulation.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels.Plots
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using InformedProteomics.Backend.MassSpecData;
    using DialogServices;
    using Models;
    using Utils;
    using ReactiveUI;

    /// <summary>
    /// View model for maintaining feature map plot and feature data manipulation.
    /// </summary>
    public class FeatureViewerViewModel : ReactiveObject
    {
        /// <summary>
        /// Dialog service for opening dialogs from the view model.
        /// </summary>
        private readonly IDialogService dialogService;

        /// <summary>
        /// Model for PROMEX features.
        /// </summary>
        private readonly ProMexModel proMexModel;

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
        /// A value indicating whether the splash screen is currently being shown.
        /// </summary>
        private bool showSplash;

        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureViewerViewModel"/> class.
        /// </summary>
        /// <param name="lcms">The LCMSRun for the data set.</param>
        /// <param name="dialogService">The dialog service for opening dialogs from the view model.</param>
        public FeatureViewerViewModel(LcMsRun lcms, IDialogService dialogService = null)
        {
            this.dialogService = dialogService ?? new DialogService();

            FeatureMapViewModel = new FeatureMapViewModel(this.dialogService);

            proMexModel = new ProMexModel(lcms);

            MinimumAbundance = 0.0;
            MaximumAbundance = 0.0;

            // Initialize isotopic envelope plot.
            IsotopicEnvelope = new IsotopicEnvelopePlotViewModel();
            IsotopicEnvelopeExpanded = false;
            PointsDisplayed = 5000;

            FeatureMapViewModel.WhenAnyValue(x => x.SelectedFeature).Subscribe(BuildIsotopePlots);

            // Update plot if abundance threshold, or points displayed changes
            this.WhenAnyValue(x => x.AbundanceThreshold, x => x.PointsDisplayed)
                .Throttle(TimeSpan.FromMilliseconds(500), RxApp.TaskpoolScheduler)
                .Select(x => proMexModel.GetFeatures(x.Item1, x.Item2).ToList())
                .Where(filteredFeatures => filteredFeatures.Count > 0)
                .Subscribe(filteredFeatures =>
                        {
                            MinimumAbundance = filteredFeatures.Min(feature => feature.MinPoint.Abundance);
                            MaximumAbundance = filteredFeatures.Max(feature => feature.MinPoint.Abundance);
                            FeatureMapViewModel.Features = filteredFeatures.ToList();
                        });

            var openFeatureFileCommand = ReactiveCommand.Create();
            openFeatureFileCommand.Subscribe(_ => OpenFeatureFileImplementation());
            OpenFeatureFileCommand = openFeatureFileCommand;
        }

        /// <summary>
        /// Gets a command that displays open file dialog to select feature file and then read and display features.
        /// </summary>
        public IReactiveCommand OpenFeatureFileCommand { get; }

        /// <summary>
        /// Gets the view model for the Feature Map plot.
        /// </summary>
        public FeatureMapViewModel FeatureMapViewModel { get; }

        /// <summary>
        /// Gets the view model for the isotopic envelope spectrum.
        /// </summary>
        public IsotopicEnvelopePlotViewModel IsotopicEnvelope
        {
            get => isotopicEnvelope;
            private set => this.RaiseAndSetIfChanged(ref isotopicEnvelope, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the expander control for the isotopic envelope plot should be expanded or not.
        /// </summary>
        public bool IsotopicEnvelopeExpanded
        {
            get => isotopicEnvelopeExpanded;
            set => this.RaiseAndSetIfChanged(ref isotopicEnvelopeExpanded, value);
        }

        /// <summary>
        /// Gets the lowest abundance currently being shown on feature map.
        /// </summary>
        public double MinimumAbundance
        {
            get => minimumAbundance;
            private set => this.RaiseAndSetIfChanged(ref minimumAbundance, value);
        }

        /// <summary>
        /// Gets the highest abundance currently being shown on feature map.
        /// </summary>
        public double MaximumAbundance
        {
            get => maximumAbundance;
            private set => this.RaiseAndSetIfChanged(ref maximumAbundance, value);
        }

        /// <summary>
        /// Gets or sets the minimum value of the abundance threshold slider.
        /// </summary>
        public double MinimumAbundanceThreshold
        {
            get => minimumAbundanceThreshold;
            set => this.RaiseAndSetIfChanged(ref minimumAbundanceThreshold, value);
        }

        /// <summary>
        /// Gets or sets the maximum value of the abundance threshold slider.
        /// </summary>
        public double MaximumAbundanceThreshold
        {
            get => maximumAbundanceThreshold;
            set => this.RaiseAndSetIfChanged(ref maximumAbundanceThreshold, value);
        }

        /// <summary>
        /// Gets or sets the total number of features being displayed on feature map
        /// </summary>
        public int PointsDisplayed
        {
            get => pointsDisplayed;
            set => this.RaiseAndSetIfChanged(ref pointsDisplayed, value);
        }

        /// <summary>
        /// Gets or sets the lowest possible abundance. This is stored in Log10(Abundance).
        /// This is set by the abundance threshold slider.
        /// </summary>
        public double AbundanceThreshold
        {
            get => abundanceThreshold;
            set => this.RaiseAndSetIfChanged(ref abundanceThreshold, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the splash screen is currently being shown.
        /// </summary>
        public bool ShowSplash
        {
            get => showSplash;
            set => this.RaiseAndSetIfChanged(ref showSplash, value);
        }

        /// <summary>
        /// Gets the PROMEX feature file path for this dataset.
        /// </summary>
        public string FeatureFilePath => proMexModel.FeatureFilePath;

        /// <summary>
        /// Read features and display feature map for this data set.
        /// </summary>
        /// <param name="filePath">Path of feature file.</param>
        public void OpenFeatureFile(string filePath)
        {
            try
            {
                proMexModel.ReadFeatures(filePath);
                MaximumAbundanceThreshold = Math.Log10(proMexModel.AbsoluteAbundanceMaximum);
                MinimumAbundanceThreshold = Math.Log10(proMexModel.AbsoluteAbundanceMinimum);
                AbundanceThreshold = Math.Max(maximumAbundanceThreshold, minimumAbundance);
                FeatureMapViewModel.Features = proMexModel.GetFeatures(
                    AbundanceThreshold,
                    PointsDisplayed).ToList();
                ShowSplash = false;
            }
            catch (InvalidCastException)
            {
                dialogService.MessageBox("Cannot open features for this type of data set.");
            }
        }

        /// <summary>
        /// Display open file dialog to select feature file and then read and display features.
        /// </summary>
        public void OpenFeatureFileImplementation()
        {
            var tsvFileName = dialogService.OpenFile(FileConstants.FeatureFileExtensions[0], FileConstants.FeatureFileFormatString);
            if (!string.IsNullOrEmpty(tsvFileName))
            {
                ShowSplash = false;
                OpenFeatureFile(tsvFileName);
            }
        }

        /// <summary>
        /// Update identifications to be displayed.
        /// Always updates feature map plot when called.
        /// </summary>
        /// <param name="idList">The identifications to display.</param>
        /// <param name="updatePlot">Should the plot be updated after setting ids?</param>
        public void UpdateIds(IEnumerable<PrSm> idList, bool updatePlot = true)
        {
            proMexModel.SetIds(idList);
            if (updatePlot)
            {
                FeatureMapViewModel.BuildPlot();
            }
        }

        /// <summary>
        /// Build plot for observed/theoretical isotope envelopes.
        /// </summary>
        /// <param name="selectedFeaturePoint">The selected Feature Point.</param>
        private void BuildIsotopePlots(Feature.FeaturePoint selectedFeaturePoint)
        {
            if (selectedFeaturePoint == null || selectedFeaturePoint.Isotopes.Length == 0)
            {
                return;
            }

            IsotopicEnvelope.BuildPlot(
                selectedFeaturePoint.Isotopes,
                selectedFeaturePoint.Mass,
                selectedFeaturePoint.Charge);
            IsotopicEnvelopeExpanded = true;
        }
    }
}
