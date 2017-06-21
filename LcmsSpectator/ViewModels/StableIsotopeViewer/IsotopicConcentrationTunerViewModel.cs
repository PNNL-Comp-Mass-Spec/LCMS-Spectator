namespace LcmsSpectator.ViewModels.StableIsotopeViewer
{
    using System;
    using System.Threading.Tasks;

    using System.Reactive;

    using InformedProteomics.Backend.Utils;

    using LcmsSpectator.Views.StableIsotopeViewer;

    using OxyPlot;
    using OxyPlot.Annotations;
    using OxyPlot.Axes;
    using OxyPlot.Series;

    using ReactiveUI;

    using IsotopicConcentrationTuner = InformedProteomics.Backend.Utils.IsotopicConcentrationTuner;

    /// <summary>
    /// View model for editing, running, and displaying results for the <see cref="IsotopicConcentrationTuner" />.
    /// </summary>
    public class IsotopicConcentrationTunerViewModel : ReactiveObject
    {
        /// <summary>
        /// The isotopic concentration tuner to set settings for.
        /// </summary>
        private readonly IsotopicConcentrationTuner tuner;

        /// <summary>
        /// A value indicating whether the progress bar should be displayed.
        /// </summary>
        private bool shouldShowProgress;

        /// <summary>
        /// The message describing the current progress of the tuning task.
        /// </summary>
        private string statusMessage;

        /// <summary>
        /// The percentage complete for the tuning process.
        /// </summary>
        private double progress;

        /// <summary>
        /// The amount to increase the concentration of the selected isotope index for each iteration.
        /// </summary>
        private double stepSize;

        /// <summary>
        /// The maximum concentration of the selected isotope to consider.
        /// </summary>
        private double maxConcentration;

        /// <summary>
        /// The name of the element + isotope that will be tuned.
        /// </summary>
        private string title;

        /// <summary>
        /// Initializes a new instance of the <see cref="IsotopicConcentrationTunerViewModel" /> class.
        /// </summary>
        public IsotopicConcentrationTunerViewModel(IsotopicConcentrationTuner tuner = null)
        {
            this.tuner = tuner ?? new IsotopicConcentrationTuner();
            this.RunTuningCommand = ReactiveCommand.CreateAsyncTask(async _ => await this.RunTuning());

            // Set default values
            this.StatusMessage = "Running...";
            this.StepSize = 0.2;
            this.MaxConcentration = this.tuner.MaxConcentration;
            this.Title = string.Format(
                                        "Tune {0}{1}",
                                        this.tuner.Element.Code,
                                        this.tuner.Element.NominalMass + this.tuner.IsotopeIndex);
        }

        /// <summary>
        /// Gets an asynchonous command that runs the tuning process.
        /// </summary>
        public ReactiveCommand<Unit> RunTuningCommand { get; private set; }

        /// <summary>
        /// Gets the plot model that displays the result curve from the tuning.
        /// </summary>
        public PlotModel ResultPlot { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the progress bar should be displayed.
        /// </summary>
        public bool ShouldShowProgress
        {
            get { return this.shouldShowProgress; }
            private set { this.RaiseAndSetIfChanged(ref this.shouldShowProgress, value); }
        }

        /// <summary>
        /// Gets the message describing the current progress of the tuning task.
        /// </summary>
        public string StatusMessage
        {
            get { return this.statusMessage; }
            private set { this.RaiseAndSetIfChanged(ref this.statusMessage, value); }
        }

        /// <summary>
        /// Gets the percentage complete for the tuning process.
        /// </summary>
        public double Progress
        {
            get { return this.progress; }
            private set { this.RaiseAndSetIfChanged(ref this.progress, value); }
        }

        /// <summary>
        /// Gets or sets the amount to increase the concentration of the selected isotope index for each iteration.
        /// </summary>
        public double StepSize
        {
            get { return this.stepSize;}
            set { this.RaiseAndSetIfChanged(ref this.stepSize, value); }
        }

        /// <summary>
        /// Gets or sets the maximum concentration of the selected isotope to consider.
        /// </summary>
        public double MaxConcentration
        {
            get { return this.maxConcentration; }
            set { this.RaiseAndSetIfChanged(ref this.maxConcentration, value); }
        }

        /// <summary>
        /// Gets the name of the element + isotope that will be tuned.
        /// </summary>
        public string Title
        {
            get { return this.title; }
            private set { this.RaiseAndSetIfChanged(ref this.title, value); }
        }

        /// <summary>
        /// Build the plot for the 
        /// </summary>
        /// <param name="curve"></param>
        private void BuildResultPlot(IsotopicConcentrationTuner.IsotopeConcentrationCorrelationCurve curve)
        {
            this.ResultPlot = new PlotModel { Title = "Tuning Results" };

            // Axes
            var xAxis = new LinearAxis
            {
                Title = "Concentration",
                Position = AxisPosition.Left,
            };

            var yAxis = new LinearAxis
            {
                Title = "R",
                Position = AxisPosition.Left,
            };

            // Line series
            var lineSeries = new LineSeries
            {
                MarkerStroke = OxyColors.Red,
                ItemsSource = curve.DataPoints,
                DataFieldX = "IsotopeConcentration",
                DataFieldY = "PearsonCorrelation"
            };

            // Annotate apex
            var lineAnnotation = new LineAnnotation
            {
                X = curve.BestConcentration.IsotopeConcentration,
                TextColor = OxyColors.Gray,
                Text = string.Format("({0}, {1})", 
                                     curve.BestConcentration.IsotopeConcentration,
                                     curve.BestConcentration.PearsonCorrelation),
                TextOrientation = AnnotationTextOrientation.Vertical,
                LineStyle = LineStyle.Dash,
            };

            this.ResultPlot.Axes.Add(xAxis);
            this.ResultPlot.Axes.Add(yAxis);
            this.ResultPlot.Series.Add(lineSeries);
            this.ResultPlot.Annotations.Add(lineAnnotation);
            this.ResultPlot.InvalidatePlot(true);
        }

        /// <summary>
        /// Runs the tuning process and builds the result plot.
        /// </summary>
        /// <returns>Awaitable task.</returns>
        private async Task RunTuning()
        {
            this.Progress = 0.0;

            // Set up progress reporter
            var progressReporter = new Progress<ProgressData>(pd => this.Progress = pd.Percent);

            this.ShouldShowProgress = true;

            // Set up tuner
            this.tuner.StepSize = this.StepSize;
            this.tuner.MaxConcentration = this.MaxConcentration;

            // Run tuning
            var results = await Task.Run(() => this.tuner.Tune(progressReporter));

            // Build result plot
            this.BuildResultPlot(results);

            this.ShouldShowProgress = false;
        }
    }
}
