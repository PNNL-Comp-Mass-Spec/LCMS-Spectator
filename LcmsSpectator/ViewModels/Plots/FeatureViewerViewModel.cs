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
    using LcmsSpectator.DialogServices;
    using LcmsSpectator.Models;
    using LcmsSpectator.Utils;
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

            this.FeatureMapViewModel = new FeatureMapViewModel(this.dialogService);

            this.proMexModel = new ProMexModel(lcms);

            this.MinimumAbundance = 0.0;
            this.MaximumAbundance = 0.0;

            // Initialize isotopic envelope plot.
            this.IsotopicEnvelope = new IsotopicEnvelopePlotViewModel();
            this.IsotopicEnvelopeExpanded = false;
            this.PointsDisplayed = 5000;

            this.FeatureMapViewModel.WhenAnyValue(x => x.SelectedFeature).Subscribe(this.BuildIsotopePlots);

            // Update plot if abundance threshold, or points displayed changes
            this.WhenAnyValue(x => x.AbundanceThreshold, x => x.PointsDisplayed)
                .Throttle(TimeSpan.FromMilliseconds(500), RxApp.TaskpoolScheduler)
                .Select(x => this.proMexModel.GetFilteredFeatures(x.Item1, x.Item2))
                .Where(filteredFeatures => filteredFeatures.Count > 0)
                .Subscribe(filteredFeatures =>
                        {
                            this.MinimumAbundance = filteredFeatures.Min(feature => feature.MinPoint.Abundance);
                            this.MaximumAbundance = filteredFeatures.Max(feature => feature.MinPoint.Abundance);
                            this.FeatureMapViewModel.Features = filteredFeatures.ToList();
                        });

            var openFeatureFileCommand = ReactiveCommand.Create();
            openFeatureFileCommand.Subscribe(_ => this.OpenFeatureFileImplementation());
            this.OpenFeatureFileCommand = openFeatureFileCommand;
        }

        /// <summary>
        /// Gets a command that displays open file dialog to select feature file and then read and display features.
        /// </summary>
        public IReactiveCommand OpenFeatureFileCommand { get; private set; }

        /// <summary>
        /// Gets the view model for the Feature Map plot.
        /// </summary>
        public FeatureMapViewModel FeatureMapViewModel { get; private set; }

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
        /// Gets or sets a value indicating whether the splash screen is currently being shown.
        /// </summary>
        public bool ShowSplash
        {
            get { return this.showSplash; }
            set { this.RaiseAndSetIfChanged(ref this.showSplash, value); }
        }

        /// <summary>
        /// Gets the PROMEX feature file path for this dataset.
        /// </summary>
        public string FeatureFilePath
        {
            get { return this.proMexModel.FeatureFilePath; }
        }

        /// <summary>
        /// Read features and display feature map for this data set.
        /// </summary>
        /// <param name="filePath">Path of feature file.</param>
        public void OpenFeatureFile(string filePath)
        {
            try
            {
                this.proMexModel.ReadFeatures(filePath);
                this.MaximumAbundanceThreshold = Math.Log10(this.proMexModel.AbsoluteAbundanceMaximum);
                this.MinimumAbundanceThreshold = Math.Log10(this.proMexModel.AbsoluteAbundanceMinimum);
                this.AbundanceThreshold = Math.Max(this.maximumAbundanceThreshold, this.minimumAbundance);
                this.FeatureMapViewModel.Features = this.proMexModel.GetFilteredFeatures(
                    this.AbundanceThreshold,
                    this.PointsDisplayed).ToList();
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
                this.OpenFeatureFile(tsvFileName);
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
            this.proMexModel.SetIds(idList);
            if (updatePlot)
            {
                this.FeatureMapViewModel.BuildPlot();
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

            this.IsotopicEnvelope.BuildPlot(
                selectedFeaturePoint.Isotopes,
                selectedFeaturePoint.Mass,
                selectedFeaturePoint.Charge);
            this.IsotopicEnvelopeExpanded = true;
        }
    }
}
