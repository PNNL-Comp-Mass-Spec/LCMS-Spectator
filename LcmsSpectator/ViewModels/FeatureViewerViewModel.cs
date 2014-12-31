using System;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.PlotModels;
using LcmsSpectator.TaskServices;
using LcmsSpectatorModels.Models;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace LcmsSpectator.ViewModels
{
    public class FeatureViewerViewModel: ViewModelBase
    {
        public FeatureViewerViewModel(ITaskService taskService, IMessenger messenger)
        {
            MessengerInstance = messenger;
            _taskService = taskService;
            MessengerInstance.Register<PropertyChangedMessage<PrSm>>(this, SelectedPrSmChanged);
            MessengerInstance.Register<PropertyChangedMessage<int>>(this, SelectedScanChanged);
            MessengerInstance.Register<PropertyChangedMessage<List<PrSm>>>(this, FilteredIdsChanged);
            FeatureSelectedCommand = new RelayCommand(FeatureSelected);
            FeatureMap = new PlotModel {Title = "Feature Map"};
            _ipxAxis = new LinearAxis {Position = AxisPosition.Bottom, Title = "Mass"};
            IsotopicEnvelope = new AutoAdjustedYPlotModel(_ipxAxis, 1.05)
            {
                Title = "Isotopic Envelope"
            };
            IsotopicEnvelope.GenerateYAxis("Relative Intensity", "");
            IsotopicEnvelopeCorrelation = 0.0;
            _isLoading = false;
            FeatureMap.MouseDown += FeatureMap_MouseDown;
            _pointsDisplayed = 2000;
            _yAxis = new LinearAxis { Position = AxisPosition.Left, Title="Monoisotopic Mass" };
            _xAxis = new LinearAxis { Position = AxisPosition.Bottom, Title = "Retention Time" };
        }
        #region Properties
        /// <summary>
        /// Command activated when a feature is selected (double clicked) on the
        /// feature map plot.
        /// </summary>
        public RelayCommand FeatureSelectedCommand { get; private set; }

        /// <summary>
        /// The Plot model for the feature map.
        /// </summary>
        public PlotModel FeatureMap
        {
            get { return _featureMap; }
            private set
            {
                _featureMap = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Plot model for the isotopic envelope spectrum.
        /// </summary>
        public AutoAdjustedYPlotModel IsotopicEnvelope
        {
            get { return _isotopicEnvelope; }
            set
            {
                _isotopicEnvelope = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// IsLoading toggles whether or not the loading screen is being shown.
        /// </summary>
        public bool IsLoading
        {
            get { return _isLoading; }
            private set
            {
                _isLoading = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Pearson correlation of observed isotope evelope vs theoretical.
        /// </summary>
        public double IsotopicEnvelopeCorrelation
        {
            get { return _isotopicEnvelopeCorrelation; }
            set
            {
                _isotopicEnvelopeCorrelation = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Score of lowest point currently being shown on feature map.
        /// </summary>
        public double MinimumScore
        {
            get { return _minimumScore; }
            private set
            {
                _minimumScore = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Score of highest point currently being shown on feature map.
        /// </summary>
        public double MaximumScore
        {
            get { return _maximumScore; }
            set
            {
                _maximumScore = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Lowest abundance currently being shown on feature map.
        /// </summary>
        public double MinimumAbundance
        {
            get { return _minimumAbundance; }
            private set
            {
                _minimumAbundance = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Highest abundance currently being shown on feature map.
        /// </summary>
        public double MaximumAbundance
        {
            get { return _maximumAbundance; }
            private set
            {
                _maximumAbundance = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Minimum value of the abundance threshold slider.
        /// </summary>
        public double MinimumAbundanceThreshold
        {
            get { return _minimumAbundanceThreshold; }
            set
            {
                _minimumAbundanceThreshold = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Maximum value of the abundance threshold slider.
        /// </summary>
        public double MaximumAbundanceThreshold
        {
            get { return _maximumAbundanceThreshold; }
            set
            {
                _maximumAbundanceThreshold = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Total number of features being displayed on feature map
        /// </summary>
        public int PointsDisplayed
        {
            get { return _pointsDisplayed; }
            set
            {
                _pointsDisplayed = value;
                _taskService.Enqueue(() => BuildPlot(_scoreThreshold, _abundanceThreshold, _pointsDisplayed, _showFoundMs2, _showNotFoundMs2));
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Lowest possible abundance. This is stored in Log10(Abundance).
        /// This is set by the abundance threshold slider.
        /// </summary>
        public double AbundanceThreshold
        {
            get { return _abundanceThreshold; }
            set
            {
                _abundanceThreshold = value;
                _taskService.Enqueue(() => BuildPlot(_scoreThreshold, _abundanceThreshold, _pointsDisplayed, _showFoundMs2, _showNotFoundMs2));
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Lowest possible score.
        /// This is set by the score threshold slider.
        /// </summary>
        public double ScoreThreshold
        {
            get { return _scoreThreshold; }
            set
            {
                _scoreThreshold = value;
                _taskService.Enqueue(() => BuildPlot(_scoreThreshold, _abundanceThreshold, _pointsDisplayed, _showFoundMs2, _showNotFoundMs2));
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Toggles whether or not the ms2 points associated with features are being shown on
        /// the feature map plot.
        /// </summary>
        public bool ShowFoundMs2
        {
            get { return _showFoundMs2; }
            set
            {
                _showFoundMs2 = value;
                _taskService.Enqueue(() => BuildPlot(_scoreThreshold, _abundanceThreshold, _pointsDisplayed, _showFoundMs2, _showNotFoundMs2));
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Toggles whether or not the ms2 points not associated with features are being shown
        /// on the feature map plot.
        /// </summary>
        public bool ShowNotFoundMs2
        {
            get { return _showNotFoundMs2; }
            set
            {
                _showNotFoundMs2 = value;
                _taskService.Enqueue(() => BuildPlot(_scoreThreshold, _abundanceThreshold, _pointsDisplayed, _showFoundMs2, _showNotFoundMs2));
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// The currently selected identification. This is the ID highlighted on the feature
        /// map plot.
        /// </summary>
        public PrSm SelectedPrSm
        {
            get { return _selectedPrSm; }
            set
            {
                var oldValue = _selectedPrSm;
                _selectedPrSm = value;
                RaisePropertyChanged("SelectedPrSm", oldValue, _selectedPrSm, true);
            }
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Set data to be shown on feature map.
        /// </summary>
        /// <param name="lcms">The LcMsRun dataset to be displayed.</param>
        /// <param name="features">Features to be displayed.</param>
        /// <param name="ids">Identifications to be displayed.</param>
        /// <param name="updatePlot">Should the plot be updated after setting data?</param>
        public void SetData(LcMsRun lcms, IEnumerable<Feature> features, IEnumerable<PrSm> ids, bool updatePlot=true)
        {
            _ids = new Dictionary<int, PrSm>();
            foreach (var id in ids)
            {
                if (id.Sequence.Count > 0) _ids.Add(id.Scan, id);
            }
            _lcms = lcms;
            _features = features.ToList();
            var scanHash = new Dictionary<int, List<double>>();
            _notFoundMs2 = new List<PrSm>();
            foreach (var feature in _features)
            {
                feature.MinPoint.RetentionTime = _lcms.GetElutionTime(feature.MinPoint.Scan);
                feature.MaxPoint.RetentionTime = _lcms.GetElutionTime(feature.MaxPoint.Scan);
                var ms2ScansMin = lcms.GetFragmentationSpectraScanNums(feature.MinPoint.Mz);
                var ms2ScansMax = lcms.GetFragmentationSpectraScanNums(feature.MaxPoint.Mz);
                Feature feature1 = feature;
                var featureMs2Scans = ms2ScansMin.Where(s => s >= feature1.MinPoint.Scan && s <= feature1.MaxPoint.Scan)
                                      .Union(ms2ScansMax.Where(s => s >= feature1.MinPoint.Scan && s <= feature1.MaxPoint.Scan)).ToList();
                feature.AssociatedMs2.AddRange(featureMs2Scans);
                foreach (var scan in featureMs2Scans)
                {
                    if (!_ids.ContainsKey(scan)) _ids.Add(scan, new PrSm{Scan = scan, Lcms = _lcms});
                    TryInsert(scanHash, scan, feature.MinPoint.Mass);
                }
            }
            foreach (var id in _ids.Values)
            {
                if (TryInsert(scanHash, id.Scan, id.Mass))
                {
                    _notFoundMs2.Add(id);
                }
            }
            MaximumAbundanceThreshold = Math.Log10(_features.Max(f => f.MinPoint.Abundance));
            MinimumAbundanceThreshold = Math.Log10(_features.Min(f => f.MinPoint.Abundance));
            _abundanceThreshold = Math.Max(_maximumAbundanceThreshold, _minimumAbundance);
            RaisePropertyChanged("AbundanceThreshold");
            _yAxis.AbsoluteMinimum = 0;
            _yAxis.AbsoluteMaximum = _features.Max(f => f.MinPoint.Mass);
            _xAxis.AbsoluteMinimum = 0; 
            _xAxis.AbsoluteMaximum = _lcms.MaxLcScan;
            _showFoundMs2 = true;
            _showNotFoundMs2 = true;
            if (updatePlot)
                _taskService.Enqueue(() => BuildPlot(_scoreThreshold, _abundanceThreshold, _pointsDisplayed, _showFoundMs2, _showNotFoundMs2));
        }

        /// <summary>
        /// Update identifications to be displayed.
        /// Always updates feature map plot when called.
        /// </summary>
        /// <param name="ids">The identifications to display.</param>
        public void UpdateIds(IEnumerable<PrSm> ids)
        {
            _ids = new Dictionary<int, PrSm>();
            foreach (var id in ids) if (id.Sequence.Count > 0)  _ids.Add(id.Scan, id);
            if (_features == null) return;
            _notFoundMs2 = new List<PrSm>();
            var scanHash = new Dictionary<int, List<double>>();
            foreach (var feature in _features)
            {
                feature.MinPoint.RetentionTime = _lcms.GetElutionTime(feature.MinPoint.Scan);
                feature.MaxPoint.RetentionTime = _lcms.GetElutionTime(feature.MaxPoint.Scan);
                var ms2ScansMin = _lcms.GetFragmentationSpectraScanNums(feature.MinPoint.Mz);
                var ms2ScansMax = _lcms.GetFragmentationSpectraScanNums(feature.MaxPoint.Mz);
                Feature feature1 = feature;
                var featureMs2Scans = ms2ScansMin.Where(s => s >= feature1.MinPoint.Scan && s <= feature1.MaxPoint.Scan)
                                      .Union(ms2ScansMax.Where(s => s >= feature1.MinPoint.Scan && s <= feature1.MaxPoint.Scan)).ToList();
                feature.AssociatedMs2.AddRange(featureMs2Scans);
                foreach (var scan in featureMs2Scans) TryInsert(scanHash, scan, feature.MinPoint.Mass);
            }
            foreach (var id in _ids.Values)
            {
                if (TryInsert(scanHash, id.Scan, id.Mass))
                {
                    _notFoundMs2.Add(id);
                }
            }
            _taskService.Enqueue(() => BuildPlot(_scoreThreshold, _abundanceThreshold, _pointsDisplayed, _showFoundMs2, _showNotFoundMs2));
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// A new identification has been selected for the data set.
        /// Highlights the point associated with this identification.
        /// </summary>
        /// <param name="message">Event message containing identification selected.</param>
        private void SelectedPrSmChanged(PropertyChangedMessage<PrSm> message)
        {
            if (message.PropertyName != "PrSm" || !(message.Sender is PrSmViewModel)) return;
            var prsm = message.NewValue;
            if (prsm == null) return;
            _selectedPrSm = prsm;
            _taskService.Enqueue(() =>
            {
                SetHighlight(prsm);
                FeatureMap.InvalidatePlot(true);
            });
            RaisePropertyChanged("SelectedPrSm");
        }

        /// <summary>
        /// A new scan number has been selected for this data set.
        /// </summary>
        /// <param name="message">Event message containing new scan number.</param>
        private void SelectedScanChanged(PropertyChangedMessage<int> message)
        {
            if (message.PropertyName == "Scan" && message.Sender is PrSmViewModel && SelectedPrSm != null)
            {
                if (FeatureMap != null)
                {
                    SetHighlight(new PrSm() { Scan = message.NewValue, Sequence = _selectedPrSm.Sequence, Lcms = _selectedPrSm.Lcms });
                    FeatureMap.InvalidatePlot(true);
                }
            }
        }

        /// <summary>
        /// Event handler for mouse click event to set SelectedDataPoint
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void FeatureMap_MouseDown(object sender, OxyMouseEventArgs args)
        {
            _selectedFeaturePoint = null;
            _selectedPrSmPoint = null;
            var series = FeatureMap.GetSeriesFromPoint(args.Position, 10);
            if (series is LineSeries)
            {
                var result = series.GetNearestPoint(args.Position, false);
                if (result == null) return;
                _selectedFeaturePoint = result.Item as FeaturePoint;
            }
            else if (series is ScatterSeries)
            {
                var result = series.GetNearestPoint(args.Position, false);
                if (result == null) return;
                _selectedPrSmPoint = result.Item as PrSm;
            }
        }

        /// <summary>
        /// Identifications have been filtered.
        /// </summary>
        /// <param name="message">Event message containing new list of filtered identifications.</param>
        private void FilteredIdsChanged(PropertyChangedMessage<List<PrSm>> message)
        {
            UpdateIds(message.NewValue);
        }
#endregion

        #region Private methods
        /// <summary>
        /// Build feature map plot.
        /// </summary>
        /// <param name="scoreThreshold"></param>
        /// <param name="abundanceThreshold"></param>
        /// <param name="pointsDisplayed"></param>
        /// <param name="showFoundMs2"></param>
        /// <param name="showNotFoundMs2"></param>
        private void BuildPlot(double scoreThreshold, double abundanceThreshold, int pointsDisplayed, bool showFoundMs2, bool showNotFoundMs2)
        {
            // Filter features based on score threshold, abundance threshold, points to display
            var filteredFeatures = FilterData(_features, scoreThreshold, abundanceThreshold, pointsDisplayed);
            // Calculate min/max abundance and min/max score
            
            if (filteredFeatures.Count > 0)
            {
                MinimumAbundance = Math.Round(filteredFeatures[filteredFeatures.Count - 1].MinPoint.Abundance, 3);
                MaximumAbundance = Math.Round(filteredFeatures[0].MinPoint.Abundance, 3);
                MinimumScore = filteredFeatures.Min(f => f.MinPoint.Score);
                MaximumScore = filteredFeatures.Max(f => f.MinPoint.Score);
            }
            else
            {

                // No features after filteration. Empty plot.
                MinimumAbundance = 0.0;
                MaximumAbundance = 0.0;
                MinimumScore = 0.0;
                MaximumScore = 0.0;
                FeatureMap.Series.Clear();
                FeatureMap.InvalidatePlot(true);
                return;
            }
            int medianAbundanceIndex = filteredFeatures.Count/2;
            var medianAbundance = filteredFeatures[medianAbundanceIndex].MinPoint.Abundance;
            var minScore = Math.Max(filteredFeatures.Min(f => f.MinPoint.Score), ScoreThreshold);
            const int numColors = 5000;
            var minColor = OxyColor.FromRgb(255, 220, 233);
            var midColor = OxyColor.FromRgb(255, 0, 0);
            var maxColor = OxyColor.FromRgb(150, 0, 0);
            var colorAxis = new LinearColorAxis     // Color axis for features
            {
                Title = "Score",
                Position = AxisPosition.Right,
                Palette = OxyPalette.Interpolate(numColors, new[] { minColor, midColor, maxColor }),
                AbsoluteMinimum = minScore,
                Minimum = minScore,
                Maximum = 1.0,
                AbsoluteMaximum = 1.0,
            };
            var ms2ColorAxis = new LinearColorAxis      // Color axis for ms2s
            {
                Key = "ms2s",
                Position = AxisPosition.None,
                Palette = OxyPalettes.Rainbow(5000),
                Minimum = 1,
                Maximum = 5000,
            };

            // Color score for identified ms2s
            const int idColorScore = 3975;      // gold color
            // Color score for unidentified ms2s
            const int unidColorScore = 2925;    // greenish color
            
            // Clear existing series on plot.
            FeatureMap.Series.Clear();
            FeatureMap.Axes.Clear();

            // Add new color axes
            FeatureMap.Axes.Add(colorAxis);
            FeatureMap.Axes.Add(ms2ColorAxis);
            FeatureMap.Axes.Add(_xAxis);
            FeatureMap.Axes.Add(_yAxis);
            var colors = colorAxis.Palette.Colors;
            //var colors = OxyPalette.Interpolate(numColors, new[] {minColor, maxColor}).Colors;
            foreach (var feature in filteredFeatures)
            {
                var feature1 = feature;
                var size = Math.Min(feature.MinPoint.Abundance / (2*medianAbundance), 7.0);
                var ms2SeriesCollection = new List<ScatterSeries>();
                if (showFoundMs2)
                {
                    // Add ms2s associated with features
                    var prsms = feature.AssociatedMs2.Select(scan => _ids[scan]);
                    // identified ms2s
                    var ms2Series = new ScatterSeries
                    {
                        ItemsSource = prsms,
                        Mapping = p =>
                        {
                            var prsm = (PrSm) p;
                            var colorScore = (prsm.Sequence.Count > 0) ? idColorScore : unidColorScore;
                            return new ScatterPoint(prsm.RetentionTime, feature1.MinPoint.Mass, Math.Max(size*0.8, 3.0), colorScore);
                        },
                        Title = "Ms2 Scan",
                        MarkerType = MarkerType.Cross,
                        ColorAxisKey = ms2ColorAxis.Key,
                        TrackerFormatString =
                            "{0}" + Environment.NewLine +
                            "{1}: {2:0.###}" + Environment.NewLine +
                            "{2}: {4:0.##E0}"
                    };
                    ms2SeriesCollection.Add(ms2Series);
                }
                // Add feature
                var colorIndex = 1 + (int)((feature1.MinPoint.Score - minScore) / (1.0 - minScore) * colors.Count);
                if (colorIndex < 1) colorIndex = 1;
                if (colorIndex >= colors.Count) colorIndex = colors.Count - 1;
                var c = colors[colorIndex];
                var ls = new LineSeries
                {
                    ItemsSource = new [] { feature.MinPoint, feature.MaxPoint },
                    Mapping = fp => new DataPoint(((FeaturePoint)fp).RetentionTime, ((FeaturePoint)fp).Mass), 
                    Title="",
                    Color = c,
                    LineStyle = LineStyle.Solid,
                    StrokeThickness = size,
                    TrackerFormatString =
                        "{0}" + Environment.NewLine +
                        "{1}: {2:0.###}" + Environment.NewLine +
                        "Scan: {Scan:0}" + Environment.NewLine +
                        "{3}: {4:0.##E0}" + Environment.NewLine +
                        "Abundance: {Abundance:0.##E0}" + Environment.NewLine +
                        "Charge: {Charge:0}" + Environment.NewLine +
                        "Score: {Score:0.###}"
                };
                FeatureMap.Series.Add(ls);
                foreach (var series in ms2SeriesCollection) FeatureMap.Series.Add(series);
            }
            // Add identified Ms2s with no associated features
            if (showNotFoundMs2)
            {
                var ids = new ScatterSeries
                {
                    ItemsSource = _notFoundMs2,
                    Mapping = p => new ScatterPoint(_lcms.GetElutionTime(((PrSm)p).Scan), ((PrSm)p).Mass, 3.0, 1000),
                    Title = "Identified Ms2 With No Feature",
                    MarkerType = MarkerType.Cross,
                    ColorAxisKey = ms2ColorAxis.Key,
                    TrackerFormatString =
                        "{0}" + Environment.NewLine +
                        "{1}: {2:0.###}" + Environment.NewLine +
                        "{3}: {4:0.##E0}" + Environment.NewLine +
                        "QValue: {QValue:0.###}"
                };
                FeatureMap.Series.Add(ids);
            }
            // Highlight selected identification.
            if (SelectedPrSm != null)
            {
                SetHighlight(SelectedPrSm);
            }

            FeatureMap.IsLegendVisible = false;
            FeatureMap.InvalidatePlot(true);
        }

        /// <summary>
        /// Called when a point has been double clicked on the feature map plot.
        /// Feature selected: Show observed/theoretical isotope envelopes
        /// Ms2 selected: Trigger event for the application to display this ms2.
        /// </summary>
        private void FeatureSelected()
        {
            if (_selectedFeaturePoint != null)
                BuildIsotopePlots();
            else if (_selectedPrSmPoint != null)
            {
                SelectedPrSm = _selectedPrSmPoint;
            }
        }

        /// <summary>
        /// Highlight an identification on the feature map plot.
        /// </summary>
        /// <param name="prsm">Identification to highlight.</param>
        private void SetHighlight(PrSm prsm)
        {
            prsm.Lcms = _lcms;
            var rt = prsm.RetentionTime;
            var mass = prsm.Mass;
            if (_circleHighlight == null)
            {
                _circleHighlight = new EllipseAnnotation
                {
                    Height = 3000,
                    Width = 6,
                    X = rt,
                    Y = mass,
                    Fill = OxyColor.FromArgb(100, 255, 255, 0),
                    Stroke = OxyColors.Green,

                };
                FeatureMap.Annotations.Add(_circleHighlight);
            }
            else
            {
                _circleHighlight.X = rt;
                _circleHighlight.Y = mass;
            }
        }

        /// <summary>
        /// Build plot for observed/theoretical isotope envelopes.
        /// </summary>
        private void BuildIsotopePlots()
        {
            if (_selectedFeaturePoint == null || _selectedFeaturePoint.Isotopes.Length == 0)
            {
                return;   
            }
            var isotopes = _selectedFeaturePoint.Isotopes;
            IsLoading = true;
            //var theoIsotopeProfile = Averagine.GetTheoreticalIsotopeProfile(_selectedFeaturePoint.Mass, _selectedFeaturePoint.Charge);
            var envelope = Averagine.GetIsotopomerEnvelope(_selectedFeaturePoint.Mass);
            var peakMap = new Dictionary<int, Peak>();
            //const double relativeIntensityThreshold = 0.1;

            for (var isotopeIndex = 0; isotopeIndex < envelope.Envolope.Length; isotopeIndex++)
            {
                var intensity = envelope.Envolope[isotopeIndex];
                //if (intensity < relativeIntensityThreshold) continue;
                var mz = Ion.GetIsotopeMz(_selectedFeaturePoint.Mass, _selectedFeaturePoint.Charge, isotopeIndex);
                var mass = (mz*_selectedFeaturePoint.Charge*Constants.Proton) - _selectedFeaturePoint.Charge*Constants.Proton;
                peakMap.Add(isotopeIndex, new Peak(mass, intensity));
            }

            IsotopicEnvelope.Series.Clear();

            var min = peakMap.Values.Min(p => p.Mz);
            var max = peakMap.Values.Max(p => p.Mz);

            min -= (max - min)/3;
            max += (max - min)/3;

            var theoSeries = new StemSeries
            {
                ItemsSource = peakMap.Values,
                Mapping = p => new DataPoint(((Peak)p).Mz, ((Peak)p).Intensity),
                Color = OxyColor.FromArgb(120, 0, 0, 0),
                StrokeThickness = 3.0
            };

            foreach (Peak p in peakMap.Values)
            {
                theoSeries.Points.Add(new DataPoint(p.Mz, p.Intensity));
            }

            IsotopicEnvelope.Series.Add(theoSeries);

            var actSeries = new StemSeries
            {
                ItemsSource = isotopes,
                Mapping = p => new DataPoint(peakMap[((Isotope)p).Index].Mz, ((Isotope)p).Ratio),
                Color = OxyColor.FromArgb(120, 255, 0, 0),
                StrokeThickness = 3.0,
                TrackerFormatString =
                "{0}" + Environment.NewLine +
                "{1}: {2:0.###}" + Environment.NewLine +
                "{3}: {4:0.##E0}" + Environment.NewLine +
                "Index: {Index:0.###}"
            };

            IsotopicEnvelope.Series.Add(actSeries);

            _ipxAxis.Minimum = min;
            _ipxAxis.Maximum = max;
            _ipxAxis.Zoom(min, max);
            IsotopicEnvelope.AdjustForZoom();

            IsotopicEnvelopeCorrelation = _selectedFeaturePoint.Score;
                /*FitScoreCalculator.GetPearsonCorrelation(theoIsotopeProfile.Select(p => p.Intensity).ToArray(),
                    isotopes.Select(p => p.Ratio).ToArray()); */
            IsotopicEnvelope.InvalidatePlot(true);
            IsLoading = false;
        }

        /// <summary>
        /// Filter feature list.
        /// </summary>
        /// <param name="features">Features to filter.</param>
        /// <param name="scoreThreshold">Minimum score threshold to filter by.</param>
        /// <param name="abundanceThreshold">Minimum abundance threshold to filter by.</param>
        /// <param name="pointsDisplayed">Maximum number of features.</param>
        /// <returns>List of filtered features.</returns>
        private List<Feature> FilterData(IEnumerable<Feature> features, double scoreThreshold, double abundanceThreshold, int pointsDisplayed)
        {
            var maxAbundance = Math.Pow(abundanceThreshold, 10);
            var filteredFeatures =
                features.Where(feature => feature.MinPoint.Abundance <= maxAbundance && feature.MinPoint.Score >= scoreThreshold)
                         .OrderByDescending(feature => feature.MinPoint.Abundance).ToList();
            var numDisplayed = Math.Min(pointsDisplayed, filteredFeatures.Count);
            var topNPoints = filteredFeatures.GetRange(0, numDisplayed);
            return topNPoints;
        }

        //private bool ContainsPoint(Dictionary<int, List<double>> points, int scan, double mass,
        //    double massTolerance = 0.01)
        //{
        //    bool success = false;
        //    List<double> masses;
        //    if (points.TryGetValue(scan, out masses))
        //    {
        //        var index = masses.BinarySearch(mass);
        //        if (index < 0) index *= -1;
        //        var lowIndex = Math.Min(Math.Max(0, index - 1), masses.Count - 1);
        //        var hiIndex = Math.Min(index + 1, masses.Count - 1);
        //        if ((Math.Abs(mass - masses[hiIndex]) <= massTolerance &&
        //             Math.Abs(mass - masses[lowIndex]) <= massTolerance))
        //        {
        //            success = true;
        //        }
        //    }
        //    return success;
        //}

        private bool TryInsert(Dictionary<int, List<double>> points, int scan, double mass, double massTolerance=0.001)
        {
            bool success = false;
            List<double> masses;
            if (!points.TryGetValue(scan, out masses))
            {
                points.Add(scan, new List<double>{mass});
                success = true;
            }
            else
            {
                var index = masses.BinarySearch(mass);
                if (index < 0) index *= -1;
                var lowIndex = Math.Min(Math.Max(0, index-1), masses.Count-1);
                var hiIndex = Math.Min(index + 1, masses.Count - 1);
                if ((Math.Abs(mass - masses[hiIndex]) <= massTolerance &&
                     Math.Abs(mass - masses[lowIndex]) <= massTolerance))
                {
                    masses.Insert(hiIndex, mass);
                    success = true;
                }
            }
            return success;
        }
        #endregion

        #region Private Fields
        private readonly ITaskService _taskService;

        private readonly LinearAxis _xAxis;
        private readonly LinearAxis _yAxis;
        private EllipseAnnotation _circleHighlight;

        private LcMsRun _lcms;
        private List<Feature> _features;
        private Dictionary<int, PrSm> _ids;
        private List<PrSm> _notFoundMs2; 
        private double _abundanceThreshold;
        private double _scoreThreshold;
        private PlotModel _featureMap;
        private double _maximumAbundance;
        private int _pointsDisplayed;
        private double _minimumAbundance;
        private double _maximumAbundanceThreshold;
        private double _minimumAbundanceThreshold;
        private double _minimumScore;
        private double _maximumScore;
        private AutoAdjustedYPlotModel _isotopicEnvelope;
        private double _isotopicEnvelopeCorrelation;
       
        private FeaturePoint _selectedFeaturePoint;
        private PrSm _selectedPrSmPoint;
        private bool _isLoading;
        private readonly LinearAxis _ipxAxis;
        private bool _showFoundMs2;
        private bool _showNotFoundMs2;
        private PrSm _selectedPrSm;
        #endregion
    }
}
