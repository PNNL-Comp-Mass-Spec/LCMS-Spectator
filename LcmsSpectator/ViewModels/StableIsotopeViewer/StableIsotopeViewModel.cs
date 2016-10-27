namespace LcmsSpectator.ViewModels.StableIsotopeViewer
{
    using System;

    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;

    using InformedProteomics.Backend.Data.Biology;
    using InformedProteomics.Backend.Data.Composition;
    using InformedProteomics.Backend.Data.Spectrometry;
    using InformedProteomics.Backend.Utils;

    using LcmsSpectator.DialogServices;
    using LcmsSpectator.PlotModels;
    using LcmsSpectator.ViewModels.Data;
    using LcmsSpectator.ViewModels.Plots;

    using ReactiveUI;


    public class StableIsotopeViewModel : ReactiveObject
    {
        /// <summary>
        /// Dialog service for opening LCMSSpectator-specific dialogs.
        /// </summary>
        private readonly IMainDialogService dialogService;

        /// <summary>
        /// Default number of peaks when clearing/creating the peak list.
        /// </summary>
        private const int DefaultNumberOfPeakFields = 4;

        /// <summary>
        /// The selected element for editing isotope ratios.
        /// </summary>
        private IsotopeProportionSelectorViewModel selectedElement;

        /// <summary>
        /// The monoisotopic mass to calculate the theoretical isotopic profile for.
        /// </summary>
        private double mass;

        /// <summary>
        /// The charge state for calculation theoretical M/Zs of isotope peaks.
        /// </summary>
        private int charge;

        /// <summary>
        /// The peak tolerance value for matching observed peaks to theoretical peaks.
        /// </summary>
        private double toleranceValue;

        /// <summary>
        /// The peak tolerance unit for matching observed peaks to theoretical peaks.
        /// </summary>
        private ToleranceUnit toleranceUnit;

        /// <summary>
        /// Yhe least abundant theoretical isotope peak to consider, relative to the highest theoretical isotope peak.
        /// </summary>
        private double relativeIntensityThreshold;

        /// <summary>
        /// A value indicating whether the peak list is profile mode.
        /// </summary>
        private bool isProfile;

        /// <summary>
        /// Initializes new instance of the <see cref="StableIsotopeViewModel"/> class.
        /// </summary>
        public StableIsotopeViewModel(IMainDialogService dialogService = null)
        {
            this.dialogService = dialogService ?? new MainDialogService();

            // Initialize default values

            this.IsotopeProportions = new Dictionary<string, IsotopeProportionSelectorViewModel>
            {
                { "C", new IsotopeProportionSelectorViewModel(Atom.Get("C"), IsoProfilePredictor.Predictor.ProbC) },
                { "H", new IsotopeProportionSelectorViewModel(Atom.Get("H"), IsoProfilePredictor.Predictor.ProbH) },
                { "N", new IsotopeProportionSelectorViewModel(Atom.Get("N"), IsoProfilePredictor.Predictor.ProbN) },
                { "O", new IsotopeProportionSelectorViewModel(Atom.Get("O"), IsoProfilePredictor.Predictor.ProbO) },
                { "S", new IsotopeProportionSelectorViewModel(Atom.Get("S"), IsoProfilePredictor.Predictor.ProbS) }
            };

            this.SelectedElement = this.IsotopeProportions["C"];

            this.Mass = 0;
            this.Charge = 1;

            this.ToleranceUnits = new ReactiveList<ToleranceUnit> { ToleranceUnit.Ppm, ToleranceUnit.Th };
            this.ToleranceValue = 10.0;
            this.ToleranceUnit = ToleranceUnit.Ppm;

            this.RelativeIntensityThreshold = 0.1;

            this.ObservedPeaks = new ReactiveList<ListItemViewModel<PeakDataPoint>> { ChangeTrackingEnabled = true };
            
            // Add some empty peaks initialy to peak list
            for (int i = 0; i < DefaultNumberOfPeakFields; i++)
            {
                this.ObservedPeaks.Add(new ListItemViewModel<PeakDataPoint>(new PeakDataPoint(0, 0, 0, 0, string.Empty)));
            }

            this.IsotopicEnvelopePlotViewModel = new IsotopicEnvelopePlotViewModel();

            // When selected element is changed, reset all values
            this.WhenAnyValue(x => x.SelectedElement).Subscribe(
                el =>
                    {
                        foreach (var element in this.IsotopeProportions.Values)
                        {
                            element.Reset();
                        }
                    });

            // When a peak's ShouldBeRemoved flag is set, remove it
            this.ObservedPeaks.ItemChanged.Where(i => i.PropertyName == "ShouldBeRemoved")
                .Where(i => i.Sender.ShouldBeRemoved)
                .Subscribe(i => this.ObservedPeaks.Remove(i.Sender));

            // Commands

            this.BuildPlotCommand = ReactiveCommand.Create(this.WhenAnyValue(x => x.Mass).Select(mass => mass > 0.0));
            this.BuildPlotCommand.Subscribe(_ => this.BuildIsotopicProfilePlot());

            this.ResetToDefaultProportionsCommand = ReactiveCommand.Create();
            this.ResetToDefaultProportionsCommand
                .Where(_ => this.SelectedElement != null)
                .Subscribe(_ => this.SelectedElement.Reset());

            this.TuneConcentrationCommand = ReactiveCommand.Create();
            this.TuneConcentrationCommand.Subscribe(_ => this.TuneConcentration());

            this.PastePeaksFromClipboardCommand = ReactiveCommand.Create();
            this.PastePeaksFromClipboardCommand.Subscribe(_ => this.PastePeaksFromClipboard());

            this.ClearObservedPeaksCommand = ReactiveCommand.Create();
            this.ClearObservedPeaksCommand.Subscribe(_ => this.ObservedPeaks.Clear());

            this.AddObservedPeakCommand = ReactiveCommand.Create();
            this.AddObservedPeakCommand.Subscribe(_ => this.ObservedPeaks
                                                           .Add(new ListItemViewModel<PeakDataPoint>(
                                                                new PeakDataPoint(0.0, 0.0, 0.0, 0.0, string.Empty))));
        }

        /// <summary>
        /// Gets a command that builds the isotopic profile plot.
        /// </summary>
        public ReactiveCommand<object> BuildPlotCommand { get; private set; }

        /// <summary>
        /// Gets a command that resets the proportions for the selected element back to their defaults.
        /// </summary>
        public ReactiveCommand<object> ResetToDefaultProportionsCommand { get; private set; }

        /// <summary>
        /// Gets a command that opens a dialog for tuning the selected isotope concentration to find the
        /// value that produces a theoretical isotopic distribution that best fits 
        /// the observed peak list.
        /// </summary>
        public ReactiveCommand<object> TuneConcentrationCommand { get; private set; }

        /// <summary>
        /// Gets a command that pastes a peak list from the clipboard.
        /// </summary>
        public ReactiveCommand<object> PastePeaksFromClipboardCommand { get; private set; }

        /// <summary>
        /// Gets a command that clears the observed peak list.
        /// </summary>
        public ReactiveCommand<object> ClearObservedPeaksCommand { get; private set; }

        /// <summary>
        /// Gets a command that adds a new, empty peak to the observed peak list.
        /// </summary>
        public ReactiveCommand<object> AddObservedPeakCommand { get; private set; }

        /// <summary>
        /// Gets a mapping between the code for an element and the isotope proportions for that element.
        /// </summary>
        public Dictionary<string, IsotopeProportionSelectorViewModel> IsotopeProportions { get; private set; }

        /// <summary>
        /// Gets an enumerable that contains only a reference to this class.
        /// </summary>
        /// <remarks>This is a hack to allow the settings to be edited in a DataGrid.</remarks>
        public IEnumerable<StableIsotopeViewModel> SettingsCollection { get { return new List<StableIsotopeViewModel> { this }; } }

        /// <summary>
        /// Gets or sets the selected element for editing isotope ratios.
        /// </summary>
        public IsotopeProportionSelectorViewModel SelectedElement
        {
            get { return this.selectedElement; }
            set { this.RaiseAndSetIfChanged(ref this.selectedElement, value); }
        }

        /// <summary>
        /// Gets the view model for the plot displaying the isotopic envelope comparison.
        /// </summary>
        public IsotopicEnvelopePlotViewModel IsotopicEnvelopePlotViewModel { get; private set; }

        /// <summary>
        /// Gets or sets the monoisotopic mass to calculate the theoretical isotopic profile for.
        /// </summary>
        public double Mass
        {
            get { return this.mass; }
            set { this.RaiseAndSetIfChanged(ref this.mass, value);}
        }

        /// <summary>
        /// Gets or sets the charge state for calculation theoretical M/Zs of isotope peaks.
        /// </summary>
        public int Charge
        {
            get { return this.charge; }
            set { this.RaiseAndSetIfChanged(ref this.charge, value); }
        }

        /// <summary>
        /// Gets the peak tolerance value for matching observed peaks to theoretical peaks.
        /// </summary>
        public double ToleranceValue
        {
            get { return this.toleranceValue; }
            set { this.RaiseAndSetIfChanged(ref this.toleranceValue, value); }
        }

        /// <summary>
        /// Gets the peak tolerance unit for matching observed peaks to theoretical peaks.
        /// </summary>
        public ToleranceUnit ToleranceUnit
        {
            get { return this.toleranceUnit; }
            set { this.RaiseAndSetIfChanged(ref this.toleranceUnit, value); }
        }

        /// <summary>
        /// Gets a list of the possible tolerance units.
        /// </summary>
        public ReactiveList<ToleranceUnit> ToleranceUnits { get; private set; }

        /// <summary>
        /// Gets or sets the least abundant theoretical isotope peak to consider, relative to the highest theoretical isotope peak.
        /// </summary>
        public double RelativeIntensityThreshold
        {
            get { return this.relativeIntensityThreshold; }
            set { this.RaiseAndSetIfChanged(ref this.relativeIntensityThreshold, value); }
        }

        /// <summary>
        /// Gets or sets the list of observed peaks.
        /// </summary>
        public ReactiveList<ListItemViewModel<PeakDataPoint>> ObservedPeaks { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the peak list is profile mode.
        /// </summary>
        public bool IsProfile
        {
            get { return this.isProfile; }
            set { this.RaiseAndSetIfChanged(ref this.isProfile, value); }
        }

        /// <summary>
        /// Builds the isotopic profile plot based on the selected isotope ratios.
        /// </summary>
        private void BuildIsotopicProfilePlot()
        {
            if (this.Mass.Equals(0.0))
            {   // Mass has not been set
                return;
            }

            // Set up the concentration tuner if any of the proportions changed.
            var predictor = new IsoProfilePredictor(
                            this.IsotopeProportions["C"].GetProportions(),
                            this.IsotopeProportions["H"].GetProportions(),
                            this.IsotopeProportions["N"].GetProportions(),
                            this.IsotopeProportions["O"].GetProportions(),
                            this.IsotopeProportions["S"].GetProportions(),
                            this.RelativeIntensityThreshold
                        );
            var averagine = new Averagine();

            var theoreticalPeaks = averagine.GetTheoreticalIsotopeProfileInst(
                                                this.Mass,
                                                this.Charge,
                                                this.RelativeIntensityThreshold,
                                                predictor);

            //var actualPeaks = isotopicConcentrationTuner.AlignObservedPeaks(
            //    this.ObservedPeaks.Select(peakDataPoint => new Peak(peakDataPoint.X, peakDataPoint.Y)).ToList(),
            //    theoreticalPeaks);

            this.IsotopicEnvelopePlotViewModel.BuildPlot(
                                                         theoreticalPeaks,
                                                         this.ObservedPeaks.Select(pd => new Peak(pd.Item.X, pd.Item.Y)).ToList(),
                                                         this.IsProfile);
        }

        /// <summary>
        /// Opens a dialog for tuning the selected isotope concentration to find the
        /// value that produces a theoretical isotopic distribution that best fits 
        /// the observed peak list.
        /// Implementation of <see cref="TuneConcentrationCommand" />.
        /// </summary>
        private void TuneConcentration()
        {
            var selectedProportion = this.SelectedElement.IsotopeRatios.FirstOrDefault(ir => ir.IsSelected);
            if (selectedProportion == null)
            {   // There was not a selected isotope proportion.
                this.dialogService.MessageBox("Please select an isotope proportion to manipulate.");
                return;
            }

            // Set up concentration tuner.
            var concentrationTuner = new IsotopicConcentrationTuner
            {
                Mass = this.Mass,
                Charge = this.Charge,
                Element = this.SelectedElement.Atom,
                IsotopeIndex = selectedProportion.IsotopeIndex,
                ObservedPeaks = this.ObservedPeaks.Select(peakDataPoint => new Peak(peakDataPoint.Item.X, peakDataPoint.Item.Y)).ToList(),
                RelativeIntensityThreshold = this.RelativeIntensityThreshold,
                Tolerance = new Tolerance(this.ToleranceValue, this.ToleranceUnit)
            };

            var concentrationTunerViewModel = new IsotopicConcentrationTunerViewModel(concentrationTuner);
            this.dialogService.OpenIsotopicConcentrationTuner(concentrationTunerViewModel);
        }

        /// <summary>
        /// Pastes delimited peak list from clipboard.
        /// Assumes M/Z is first column and Intensity is second column.
        /// Impelementation of <see cref="PastePeaksFromClipboardCommand" />.
        /// </summary>
        private void PastePeaksFromClipboard()
        {
            this.ObservedPeaks.Clear();

            var clipBoardText = System.Windows.Clipboard.GetText();

            var peaks = this.ParseTextPeaks(clipBoardText);
            foreach (var peak in peaks)
            {
                this.ObservedPeaks.Add(new ListItemViewModel<PeakDataPoint>(peak));
            }
        }

        /// <summary>
        /// Parses peak list from a tab separated string where the first column is M/Z and
        /// the second column is Intensity.
        /// </summary>
        /// <param name="peakList"></param>
        /// <returns></returns>
        private List<PeakDataPoint> ParseTextPeaks(string peakList)
        {
            var dataPoints = new List<PeakDataPoint>();
            var lines = peakList.Split('\n');
            foreach (var line in lines)
            {
                var parts = line.Split('\t');
                if (parts.Length < 2)
                {
                    continue;
                }

                double mz, intensity;
                if (!double.TryParse(parts[0], out mz) || !double.TryParse(parts[1], out intensity))
                {
                    continue;
                }

                dataPoints.Add(new PeakDataPoint(mz, intensity, 0, 0, string.Empty));
            }

            return dataPoints;
        }
    }
}
