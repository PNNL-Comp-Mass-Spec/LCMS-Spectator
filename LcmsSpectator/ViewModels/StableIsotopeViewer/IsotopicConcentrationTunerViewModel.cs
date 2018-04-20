using System;
using System.Reactive;
using System.Threading.Tasks;
using InformedProteomics.Backend.Utils;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.StableIsotopeViewer
{
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
        public IsotopicConcentrationTunerViewModel() : this(null)
        {
            // Not using a default parameter to make WPF design-time view happy
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IsotopicConcentrationTunerViewModel" /> class.
        /// </summary>
        public IsotopicConcentrationTunerViewModel(IsotopicConcentrationTuner tuner)
        {
            this.tuner = tuner ?? new IsotopicConcentrationTuner();
            RunTuningCommand = ReactiveCommand.CreateFromTask(async _ => await RunTuning());

            // Set default values
            StatusMessage = "Running...";
            StepSize = 0.2;
            MaxConcentration = this.tuner.MaxConcentration;
            Title = string.Format(
                                        "Tune {0}{1}",
                                        this.tuner.Element.Code,
                                        this.tuner.Element.NominalMass + this.tuner.IsotopeIndex);
        }

        /// <summary>
        /// Gets an asynchonous command that runs the tuning process.
        /// </summary>
        public ReactiveCommand<Unit, Unit> RunTuningCommand { get; }

        /// <summary>
        /// Gets the plot model that displays the result curve from the tuning.
        /// </summary>
        public PlotModel ResultPlot { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the progress bar should be displayed.
        /// </summary>
        public bool ShouldShowProgress
        {
            get => shouldShowProgress;
            private set => this.RaiseAndSetIfChanged(ref shouldShowProgress, value);
        }

        /// <summary>
        /// Gets the message describing the current progress of the tuning task.
        /// </summary>
        public string StatusMessage
        {
            get => statusMessage;
            private set => this.RaiseAndSetIfChanged(ref statusMessage, value);
        }

        /// <summary>
        /// Gets the percentage complete for the tuning process.
        /// </summary>
        public double Progress
        {
            get => progress;
            private set => this.RaiseAndSetIfChanged(ref progress, value);
        }

        /// <summary>
        /// Gets or sets the amount to increase the concentration of the selected isotope index for each iteration.
        /// </summary>
        public double StepSize
        {
            get => stepSize;
            set => this.RaiseAndSetIfChanged(ref stepSize, value);
        }

        /// <summary>
        /// Gets or sets the maximum concentration of the selected isotope to consider.
        /// </summary>
        public double MaxConcentration
        {
            get => maxConcentration;
            set => this.RaiseAndSetIfChanged(ref maxConcentration, value);
        }

        /// <summary>
        /// Gets the name of the element + isotope that will be tuned.
        /// </summary>
        public string Title
        {
            get => title;
            private set => this.RaiseAndSetIfChanged(ref title, value);
        }

        /// <summary>
        /// Build the plot for the
        /// </summary>
        /// <param name="curve"></param>
        private void BuildResultPlot(IsotopicConcentrationTuner.IsotopeConcentrationCorrelationCurve curve)
        {
            ResultPlot = new PlotModel { Title = "Tuning Results" };

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

            ResultPlot.Axes.Add(xAxis);
            ResultPlot.Axes.Add(yAxis);
            ResultPlot.Series.Add(lineSeries);
            ResultPlot.Annotations.Add(lineAnnotation);
            ResultPlot.InvalidatePlot(true);
        }

        /// <summary>
        /// Runs the tuning process and builds the result plot.
        /// </summary>
        /// <returns>Awaitable task.</returns>
        private async Task RunTuning()
        {
            Progress = 0.0;

            // Set up progress reporter
            var progressReporter = new Progress<ProgressData>(pd => Progress = pd.Percent);

            ShouldShowProgress = true;

            // Set up tuner
            tuner.StepSize = StepSize;
            tuner.MaxConcentration = MaxConcentration;

            // Run tuning
            var results = await Task.Run(() => tuner.Tune(progressReporter));

            // Build result plot
            BuildResultPlot(results);

            ShouldShowProgress = false;
        }
    }
}
