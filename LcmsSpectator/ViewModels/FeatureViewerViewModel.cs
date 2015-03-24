using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.Models;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
    public class FeatureViewerViewModel: ReactiveObject
    {
        public FeatureViewerViewModel()
        {
            var featureSelectedCommand = ReactiveCommand.Create();
            featureSelectedCommand.Subscribe(_ => FeatureSelected());
            FeatureSelectedCommand = featureSelectedCommand;
            IsotopicEnvelope = new IsotopicEnvelopePlotViewModel();
            FeatureMap = new PlotModel {Title = "Feature Map"};
            _isotopicEnvelopeExpanded = false;
            FeatureMap.MouseDown += FeatureMap_MouseDown;
            _pointsDisplayed = 5000;
            //_viewDivisions = 9;
            _showFoundIdMs2 = false;
            _showFoundUnIdMs2 = false;
            _showNotFoundMs2 = false;
            _yAxis = new LinearAxis { Position = AxisPosition.Left, Title = "Monoisotopic Mass", StringFormat = "0.###" };
            _xAxis = new LinearAxis { Position = AxisPosition.Bottom, Title = "Retention Time", StringFormat = "0.###", };
            const int numColors = 5000;
            var minColor = OxyColor.FromRgb(255, 160, 160);
            var midColor = OxyColor.FromRgb(255, 0, 0);
            var maxColor = OxyColor.FromRgb(150, 0, 0);
            _featureColorAxis = new LinearColorAxis     // Color axis for features
            {
                Title = "Abundance",
                Position = AxisPosition.Right,
                Palette = OxyPalette.Interpolate(numColors, minColor, midColor, maxColor),
                StringFormat = "0.###E0"
            };
            _ms2ColorAxis = new LinearColorAxis      // Color axis for ms2s
            {
                Key = "ms2s",
                Position = AxisPosition.None,
                Palette = OxyPalettes.Rainbow(5000),
                Minimum = 1,
                Maximum = 5000,
            };
            FeatureMap.Axes.Add(_featureColorAxis);
            FeatureMap.Axes.Add(_ms2ColorAxis);
            FeatureMap.Axes.Add(_xAxis);
            FeatureMap.Axes.Add(_yAxis);
            bool isInternalChange = false;
            _xAxis.AxisChanged += (o, e) =>
            {
                if (!isInternalChange && _highlight != null &&
                    _highlight.TextPosition.X >= _xAxis.ActualMinimum && _highlight.TextPosition.X <= _xAxis.ActualMaximum &&
                    _highlight.TextPosition.Y >= _yAxis.ActualMinimum && _highlight.TextPosition.Y <= _yAxis.ActualMaximum)
                {
                    var x = _highlight.TextPosition.X;
                    isInternalChange = true;
                    _highlight.MinimumX = x - (_xAxis.ActualMaximum - _xAxis.ActualMinimum) * HighlightSize * 0.5;
                    _highlight.MaximumX = x + (_xAxis.ActualMaximum - _xAxis.ActualMinimum) * HighlightSize * 0.5;
                }
                isInternalChange = false;
            };
            _yAxis.AxisChanged += (o, e) =>
            {
                if (!isInternalChange && _highlight != null &&
                    _highlight.TextPosition.X >= _xAxis.ActualMinimum && _highlight.TextPosition.X <= _xAxis.ActualMaximum &&
                    _highlight.TextPosition.Y >= _yAxis.ActualMinimum && _highlight.TextPosition.Y <= _yAxis.ActualMaximum)
                {
                    var y = _highlight.TextPosition.Y;
                    isInternalChange = true;
                    _highlight.MinimumY = y - (_yAxis.ActualMaximum - _yAxis.ActualMinimum) * HighlightSize;
                    _highlight.MaximumY = y + (_yAxis.ActualMaximum - _yAxis.ActualMinimum) * HighlightSize;
                }
                isInternalChange = false;
            };

            // When ShowNoutFoundMs2 changes, update the NoutFoundMs2 series
            this.WhenAnyValue(x => x.ShowNotFoundMs2)
                .Where(_ => _notFoundMs2S != null && FeatureMap != null)
                .Subscribe(showNotFoundMs2 =>
                {
                    _notFoundMs2S.IsVisible = showNotFoundMs2;
                    FeatureMap.InvalidatePlot(true);
                });

            // When ShowFoundIdMs2 changes, update all ms2 series
            this.WhenAnyValue(x => x.ShowFoundIdMs2)
                .Where(_ => _foundIdMs2S != null && FeatureMap != null)
                .Subscribe(showFoundMs2 =>
                {
                    foreach (var ms2 in _foundIdMs2S) ms2.IsVisible = showFoundMs2;
                    FeatureMap.InvalidatePlot(true);
                });

            // When ShowFoundUnIdMs2 changes, update all ms2 series
            this.WhenAnyValue(x => x.ShowFoundUnIdMs2)
                .Where(_ => _foundUnIdMs2S != null && FeatureMap != null)
                .Subscribe(showFoundMs2 =>
                {
                    foreach (var ms2 in _foundUnIdMs2S) ms2.IsVisible = showFoundMs2;
                    FeatureMap.InvalidatePlot(true);
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

            // Update plot if score threshold, abundance threshold, or points displayed changes
            this.WhenAnyValue(x => x.AbundanceThreshold, x => x.PointsDisplayed)
                .Throttle(TimeSpan.FromMilliseconds(500), RxApp.TaskpoolScheduler)
                .Subscribe(x => BuildPlot(x.Item1, x.Item2));
        }

        #region Public Properties
        /// <summary>
        /// Command activated when a feature is selected (double clicked) on the
        /// feature map plot.
        /// </summary>
        public IReactiveCommand FeatureSelectedCommand { get; private set; }

        private PlotModel _featureMap;
        /// <summary>
        /// The Plot model for the feature map.
        /// </summary>
        public PlotModel FeatureMap
        {
            get { return _featureMap; }
            private set { this.RaiseAndSetIfChanged(ref _featureMap, value); }
        }

        private IsotopicEnvelopePlotViewModel _isotopicEnvelope;
        /// <summary>
        /// View model for the isotopic envelope spectrum.
        /// </summary>
        public IsotopicEnvelopePlotViewModel IsotopicEnvelope
        {
            get { return _isotopicEnvelope; }
            set { this.RaiseAndSetIfChanged(ref _isotopicEnvelope, value); }
        }

        private bool _isotopicEnvelopeExpanded;
        /// <summary>
        /// Whether or not the expander control for the isotopic envelope plot should be expanded or not.
        /// </summary>
        public bool IsotopicEnvelopeExpanded
        {
            get { return _isotopicEnvelopeExpanded; }
            set { this.RaiseAndSetIfChanged(ref _isotopicEnvelopeExpanded, value); }
        }

        private double _minimumAbundance;
        /// <summary>
        /// Lowest abundance currently being shown on feature map.
        /// </summary>
        public double MinimumAbundance
        {
            get { return _minimumAbundance; }
            private set { this.RaiseAndSetIfChanged(ref _minimumAbundance, value); }
        }

        private double _maximumAbundance;
        /// <summary>
        /// Highest abundance currently being shown on feature map.
        /// </summary>
        public double MaximumAbundance
        {
            get { return _maximumAbundance; }
            private set { this.RaiseAndSetIfChanged(ref _maximumAbundance, value); }
        }

        private double _minimumAbundanceThreshold;
        /// <summary>
        /// Minimum value of the abundance threshold slider.
        /// </summary>
        public double MinimumAbundanceThreshold
        {
            get { return _minimumAbundanceThreshold; }
            set { this.RaiseAndSetIfChanged(ref _minimumAbundanceThreshold, value); }
        }

        private double _maximumAbundanceThreshold;
        /// <summary>
        /// Maximum value of the abundance threshold slider.
        /// </summary>
        public double MaximumAbundanceThreshold
        {
            get { return _maximumAbundanceThreshold; }
            set { this.RaiseAndSetIfChanged(ref _maximumAbundanceThreshold, value); }
        }

        private int _pointsDisplayed;
        /// <summary>
        /// Total number of features being displayed on feature map
        /// </summary>
        public int PointsDisplayed
        {
            get { return _pointsDisplayed; }
            set { this.RaiseAndSetIfChanged(ref _pointsDisplayed, value); }
        }

        private double _abundanceThreshold;
        /// <summary>
        /// Lowest possible abundance. This is stored in Log10(Abundance).
        /// This is set by the abundance threshold slider.
        /// </summary>
        public double AbundanceThreshold
        {
            get { return _abundanceThreshold; }
            set { this.RaiseAndSetIfChanged(ref _abundanceThreshold, value); }
        }

        private bool _showFoundIdMs2;
        /// <summary>
        /// Toggles whether or not the  identified ms2 points associated with features are being shown on
        /// the feature map plot.
        /// </summary>
        public bool ShowFoundIdMs2
        {
            get { return _showFoundIdMs2; }
            set { this.RaiseAndSetIfChanged(ref _showFoundIdMs2, value); }
        }

        private bool _showFoundUnIdMs2;
        /// <summary>
        /// Toggles whether or not the unidentified ms2 points associated with features are being shown on
        /// the feature map plot.
        /// </summary>
        public bool ShowFoundUnIdMs2
        {
            get { return _showFoundUnIdMs2; }
            set { this.RaiseAndSetIfChanged(ref _showFoundUnIdMs2, value); }
        }

        private bool _showNotFoundMs2;
        /// <summary>
        /// Toggles whether or not the ms2 points not associated with features are being shown
        /// on the feature map plot.
        /// </summary>
        public bool ShowNotFoundMs2
        {
            get { return _showNotFoundMs2; }
            set { this.RaiseAndSetIfChanged(ref _showNotFoundMs2, value); }
        }

        private PrSm _selectedPrSm;
        /// <summary>
        /// The currently selected identification. This is the ID highlighted on the feature
        /// map plot.
        /// </summary>
        public PrSm SelectedPrSm
        {
            get { return _selectedPrSm; }
            set { this.RaiseAndSetIfChanged(ref _selectedPrSm, value); }
        }

        private FeaturePoint _selectedFeature;
        /// <summary>
        /// Feature selected by double clicking on feature map.
        /// </summary>
        public FeaturePoint SelectedFeature
        {
            get { return _selectedFeature; }
            private set { this.RaiseAndSetIfChanged(ref _selectedFeature, value); }
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
            _lcms = lcms;
            _features = features.ToList();
            UpdateIds(ids, false);
            MaximumAbundanceThreshold = Math.Log10(_features.Max(f => f.MinPoint.Abundance));
            MinimumAbundanceThreshold = Math.Log10(_features.Min(f => f.MinPoint.Abundance));
            AbundanceThreshold = Math.Max(_maximumAbundanceThreshold, _minimumAbundance);
            _yAxis.AbsoluteMinimum = 0;
            _yAxis.AbsoluteMaximum = _features.Max(f => f.MinPoint.Mass);
            _xAxis.AbsoluteMinimum = 0;
            _xAxis.AbsoluteMaximum = _lcms.MaxLcScan;
            if (updatePlot) BuildPlot(_abundanceThreshold, _pointsDisplayed);
        }

        /// <summary>
        /// Update identifications to be displayed.
        /// Always updates feature map plot when called.
        /// </summary>
        /// <param name="ids">The identifications to display.</param>
        /// <param name="updatePlot">Should the plot be updated after setting ids?</param>
        public void UpdateIds(IEnumerable<PrSm> ids, bool updatePlot=true)
        {
            _ids = new Dictionary<int, PrSm>();
            foreach (var id in ids) if (id.Sequence.Count > 0)  _ids.Add(id.Scan, id);
            if (_features == null) return;
            _notFoundMs2 = new List<PrSm>();
            var scanHash = new Dictionary<int, List<double>>();
            var accountedFor = new HashSet<int>();
            var sortedIds = _ids.Values.OrderBy(id => id.Scan).ToList();
            _featuresByScan = new Dictionary<FeaturePoint, Feature>();
            foreach (var feature in _features)
            {
                var mass = feature.MinPoint.Mass;
                _featuresByScan.Add(feature.MinPoint, feature);
                _featuresByScan.Add(feature.MaxPoint, feature);
                var ms2Scans = new List<int>();
                for (int c = feature.MinPoint.Scan; c <= feature.MaxPoint.Scan; c++)
                {
                    var mz = (mass + c * Constants.Proton) / c;
                    ms2Scans.AddRange(_lcms.GetFragmentationSpectraScanNums(mz));
                }
                feature.MinPoint.RetentionTime = _lcms.GetElutionTime(feature.MinPoint.Scan);
                feature.MaxPoint.RetentionTime = _lcms.GetElutionTime(feature.MaxPoint.Scan);
                Feature feature1 = feature;
                var featureMs2Scans = (ms2Scans.Where(s => s >= feature1.MinPoint.Scan && s <= feature1.MaxPoint.Scan));
                feature.AssociatedMs2.AddRange(featureMs2Scans);
                var minIndex = sortedIds.BinarySearch(new PrSm { Scan = feature.MinPoint.Scan }, new PrSmScanComparer());
                if (minIndex < 0) minIndex *= -1;
                minIndex = Math.Max(Math.Min(minIndex, sortedIds.Count), 0);
                var maxIndex = sortedIds.BinarySearch(new PrSm { Scan = feature.MaxPoint.Scan }, new PrSmScanComparer());
                if (maxIndex < 0) maxIndex *= -1;
                maxIndex = Math.Max(Math.Min(maxIndex, sortedIds.Count), 0);
                for (int i = minIndex; i < maxIndex; i++)
                {
                    var id = sortedIds[i];
                    if (accountedFor.Contains(id.Scan)) continue;
                    if (id.Scan >= feature.MinPoint.Scan && id.Scan <= feature.MaxPoint.Scan &&
                        Math.Abs(id.Mass - feature.MinPoint.Mass) < 2)
                    {
                        if (!feature.AssociatedMs2.Contains(id.Scan))
                        {
                            feature.AssociatedMs2.Add(id.Scan);
                            accountedFor.Add(id.Scan);
                        }
                    }
                }
                foreach (var scan in feature.AssociatedMs2)
                {
                    if (!_ids.ContainsKey(scan))
                        _ids.Add(scan, new PrSm { Scan = scan, Lcms = _lcms, Mass = feature.MinPoint.Mass });
                    if (!accountedFor.Contains(scan)) accountedFor.Add(scan);
                    Insert(scanHash, scan, _ids[scan].Mass, massTolerance: 1);
                }
            }
            foreach (var id in _ids.Values)
            {
                //if (TryInsert(scanHash, id.Scan, id.Mass, 1) && id.Sequence.Count > 0)
                if (!accountedFor.Contains(id.Scan))
                {
                    _notFoundMs2.Add(id);
                }
            }
            if (updatePlot) BuildPlot(_abundanceThreshold, _pointsDisplayed);
        }
        #endregion

        #region Event Handlers
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
            if (series is LineSeries) // Was a feature clicked?
            {
                var result = series.GetNearestPoint(args.Position, false);
                if (result == null) return;
                var featurePoint = result.Item as FeaturePoint;
                if (featurePoint != null)
                {
                    // See if there is a ms2 point closer than this feature point
                    PrSm closestPrSm = null;
                    if (_showFoundIdMs2 || _showFoundUnIdMs2)
                    {
                        var feature = _featuresByScan[featurePoint];
                        var ms2Scans = feature.AssociatedMs2;
                        var minDist = result.Position.DistanceToSquared(args.Position);
                        foreach (var scan in ms2Scans)
                        {
                            if (!_ids.ContainsKey(scan)) continue;
                            var prsm = _ids[scan];
                            if ((prsm.Sequence.Count == 0 && !_showFoundUnIdMs2)
                                || (prsm.Sequence.Count > 0 && !_showFoundIdMs2)) continue;
                            var sp = _xAxis.Transform(prsm.RetentionTime, featurePoint.Mass, _yAxis);
                            var distSq = sp.DistanceToSquared(args.Position);
                            if (closestPrSm == null || distSq < minDist)
                            {
                                closestPrSm = prsm;
                                minDist = distSq;
                            }
                        }   
                    }
                    if (closestPrSm != null) _selectedPrSmPoint = closestPrSm;
                    _selectedFeaturePoint = featurePoint;
                }
            }
            else if (series is ScatterSeries) // Was a ms2 cross clicked?
            {
                var result = series.GetNearestPoint(args.Position, false);
                if (result == null) return;
                _selectedPrSmPoint = result.Item as PrSm;
            }
        }
#endregion

        #region Private methods

        /// <summary>
        /// Build feature map plot.
        /// </summary>
        /// <param name="abundanceThreshold"></param>
        /// <param name="pointsDisplayed"></param>
        private void BuildPlot(double abundanceThreshold, int pointsDisplayed)
        {
            ResetFeaturePlot(); // clear existing plot
            // Filter features based on score threshold, abundance threshold, points to display
            var filteredFeatures = FilterData(_features, abundanceThreshold, pointsDisplayed);
            // Calculate min/max abundance and min/max score
            if (filteredFeatures.Count == 0) return;

            MinimumAbundance = Math.Round(filteredFeatures[filteredFeatures.Count - 1].MinPoint.Abundance, 3);
            MaximumAbundance = Math.Round(filteredFeatures[0].MinPoint.Abundance, 3);

            int medianAbundanceIndex = filteredFeatures.Count/2;
            var medianAbundance = filteredFeatures[medianAbundanceIndex].MinPoint.Abundance;
            var minAbundance = filteredFeatures.Min(f => f.MinPoint.Abundance);
            var maxAbundance = filteredFeatures.Max(f => f.MaxPoint.Abundance);
            // Set bounds for color axis for features
            _featureColorAxis.AbsoluteMinimum = minAbundance;
            _featureColorAxis.Minimum = minAbundance;
            _featureColorAxis.Maximum = maxAbundance;
            _featureColorAxis.AbsoluteMaximum = maxAbundance;

            foreach (var feature in filteredFeatures)
            {
                var size = Math.Min(feature.MinPoint.Abundance / (2 * medianAbundance), 7.0);
                // Add ms2s associated with features
                var prsms = feature.AssociatedMs2.Where(scan => _ids.ContainsKey(scan)).Select(scan => _ids[scan]).ToList();
                // identified ms2s
                if (prsms.Count > 0)
                {
                    var feature1 = feature;
                    var idPrSms = prsms.Where(prsm => prsm.Sequence.Count > 0 && Math.Abs(prsm.Mass - feature1.MinPoint.Mass) < 1);
                    var unIdPrSms = prsms.Where(prsm => prsm.Sequence.Count == 0 || Math.Abs(prsm.Mass - feature1.MinPoint.Mass) >= 1);
                    var ms2IdSeries = CreateMs2ScatterSeries(idPrSms, feature, _ms2ColorAxis, "Identified Ms2s", size, IdColorScore, _showFoundIdMs2);
                    _foundIdMs2S.Add(ms2IdSeries);
                    var ms2UnIdSeries = CreateMs2ScatterSeries(unIdPrSms, feature, _ms2ColorAxis, "Unidentified Ms2s", size, UnidColorScore, _showFoundUnIdMs2);
                    _foundUnIdMs2S.Add(ms2UnIdSeries);
                    FeatureMap.Series.Add(ms2IdSeries);
                    FeatureMap.Series.Add(ms2UnIdSeries);
                }
                // Create and add feature
                FeatureMap.Series.Add(CreateFeatureSeries(feature, _featureColorAxis.Palette.Colors, size, minAbundance, maxAbundance));
            }
            // Add identified Ms2s with no associated features
            _notFoundMs2S = CreateMs2ScatterSeries(_notFoundMs2, null, _ms2ColorAxis, "Identified Ms2 (No Feature)", 0, 1000, _showNotFoundMs2);
            FeatureMap.Series.Add(_notFoundMs2S);
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
            _foundIdMs2S = new List<Series>();
            _foundUnIdMs2S = new List<Series>();
            MinimumAbundance = 0.0;
            MaximumAbundance = 0.0;
            FeatureMap.Series.Clear();
            FeatureMap.InvalidatePlot(true);
        }

        /// <summary>
        /// Create a line series for a feature containing a minpoint and maxpoint.
        /// </summary>
        /// <param name="feature">Feature to create series for.</param>
        /// <param name="colors">The color scale to use to color the feature.</param>
        /// <param name="size">The size of the feature.</param>
        /// <param name="minAbundance">Minimum abundance of all features.</param>
        /// <param name="maxAbundance">Maximum abundance of all features.</param>
        /// <returns></returns>
        private LineSeries CreateFeatureSeries(Feature feature, IList<OxyColor> colors, double size, double minAbundance, double maxAbundance)
        {
            var colorIndex = 1 + (int)((feature.MinPoint.Abundance - minAbundance) / (maxAbundance - minAbundance) * colors.Count);
            if (colorIndex < 1) colorIndex = 1;
            if (colorIndex >= colors.Count) colorIndex = colors.Count - 1;
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
                Mapping = fp => new DataPoint(((FeaturePoint)fp).RetentionTime, ((FeaturePoint)fp).Mass),
                Title = "Feature",
                Color = c,
                LineStyle = LineStyle.Solid,
                StrokeThickness = size,
                TrackerFormatString = trackerString
            };
        }

        /// <summary>
        /// Create scatter series for Ms2 points.
        /// </summary>
        /// <param name="prsms">List of identifications containing ms2 scans to create scatter points for.</param>
        /// <param name="feature">The feature that the ms2s are associated with (nullable)</param>
        /// <param name="colorAxis">The color axis to use to color the scatter points</param>
        /// <param name="title">The title of the scatter series</param>
        /// <param name="size">The size of the scatter points</param>
        /// <param name="colorScore">Index of color to use in color axis.</param>
        /// <param name="visible">Is this ms2 series visible?</param>
        /// <returns></returns>
        private ScatterSeries CreateMs2ScatterSeries(IEnumerable<PrSm> prsms, Feature feature, LinearColorAxis colorAxis, string title, double size, double colorScore, bool visible)
        {
            return new ScatterSeries
            {
                ItemsSource = prsms,
                Mapping = p =>
                {
                    var prsm = (PrSm)p;
                    return new ScatterPoint(prsm.RetentionTime, feature == null ? prsm.Mass : feature.MinPoint.Mass, Math.Max(size * 0.8, 3.0), colorScore);
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
        /// Ms2 selected: Trigger event for the application to display this ms2.
        /// </summary>
        private void FeatureSelected()
        {
            SelectedFeature = _selectedFeaturePoint;
            if (_selectedFeaturePoint != null) BuildIsotopePlots();
            if (_selectedPrSmPoint != null) SelectedPrSm = _selectedPrSmPoint;
        }

        /// <summary>
        /// Highlight an identification on the feature map plot.
        /// </summary>
        /// <param name="prsm">Identification to highlight.</param>
        private void SetHighlight(PrSm prsm)
        {
            if (prsm == null) return;
            prsm.Lcms = _lcms;
            var rt = prsm.RetentionTime;
            var mass = prsm.Mass;
            if (_highlight == null)
            {
                _highlight = new RectangleAnnotation
                {
                    TextPosition = new DataPoint(rt, mass),
                    Fill = OxyColor.FromArgb(100, 255, 255, 0),
                    Stroke = OxyColor.FromRgb(0,255,0),
                    StrokeThickness = 2,
                    MinimumX = rt - (_xAxis.ActualMaximum - _xAxis.ActualMinimum) * HighlightSize * 0.5,
                    MaximumX = rt + (_xAxis.ActualMaximum - _xAxis.ActualMinimum) * HighlightSize * 0.5,
                    MinimumY = mass - (_yAxis.ActualMaximum - _yAxis.ActualMinimum) * HighlightSize,
                    MaximumY = mass + (_yAxis.ActualMaximum - _yAxis.ActualMinimum) * HighlightSize,
                };
                FeatureMap.Annotations.Add(_highlight);
            }
            else
            {
                _highlight.TextPosition = new DataPoint(rt, mass);
                _highlight.MinimumX = rt - (_xAxis.ActualMaximum - _xAxis.ActualMinimum) * HighlightSize * 0.5;
                _highlight.MaximumX = rt + (_xAxis.ActualMaximum - _xAxis.ActualMinimum) * HighlightSize * 0.5;
                _highlight.MinimumY = mass - (_yAxis.ActualMaximum - _yAxis.ActualMinimum) * HighlightSize;
                _highlight.MaximumY = mass + (_yAxis.ActualMaximum - _yAxis.ActualMinimum) * HighlightSize;
            }
        }

        /// <summary>
        /// Build plot for observed/theoretical isotope envelopes.
        /// </summary>
        private void BuildIsotopePlots()
        {
            if (_selectedFeaturePoint == null || _selectedFeaturePoint.Isotopes.Length == 0) return;
            IsotopicEnvelope.BuildPlot(_selectedFeaturePoint.Isotopes, _selectedFeaturePoint.Mass, _selectedFeaturePoint.Charge);
            IsotopicEnvelopeExpanded = true;
        }

        /// <summary>
        /// Filter feature list.
        /// </summary>
        /// <param name="features">Features to filter.</param>
        /// <param name="abundanceThreshold">Minimum abundance threshold to filter by.</param>
        /// <param name="pointsDisplayed">Maximum number of features.</param>
        /// <returns>List of filtered features.</returns>
        private List<Feature> FilterData(IEnumerable<Feature> features, double abundanceThreshold, int pointsDisplayed)
        {
            var maxAbundance = Math.Pow(abundanceThreshold, 10);
            if (features == null) return new List<Feature>();
            var filteredFeatures =
                features.Where(feature => feature.MinPoint.Abundance <= maxAbundance) //&& feature.MinPoint.Score >= scoreThreshold)
                         .OrderByDescending(feature => feature.MinPoint.Abundance).ToList();
            var numDisplayed = Math.Min(pointsDisplayed, filteredFeatures.Count);
            var topNPoints = filteredFeatures.GetRange(0, numDisplayed);
            //var topNPoints = Partition(filteredFeatures, numDisplayed);
            return topNPoints;
        }

        private void Insert(Dictionary<int, List<double>> points, int scan, double mass, double massTolerance = 0.1)
        {
            List<double> masses;
            if (!points.TryGetValue(scan, out masses))
            {
                points.Add(scan, new List<double>{mass});
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
                }
            }
        }
        #endregion

        #region Private Fields
        // Plot elements
        private readonly LinearAxis _xAxis;
        private readonly LinearAxis _yAxis;
        private RectangleAnnotation _highlight;
        private List<Series> _foundIdMs2S;
        private List<Series> _foundUnIdMs2S; 
        private Series _notFoundMs2S;
        private readonly LinearColorAxis _ms2ColorAxis;
        private readonly LinearColorAxis _featureColorAxis;

        // Data
        private LcMsRun _lcms;
        private List<Feature> _features;
        private Dictionary<FeaturePoint, Feature> _featuresByScan;
        private Dictionary<int, PrSm> _ids;
        private List<PrSm> _notFoundMs2;

        // Selections
        private FeaturePoint _selectedFeaturePoint;
        private PrSm _selectedPrSmPoint;

        private const double HighlightSize = 0.008;

        // Color score for identified ms2s
        private const int IdColorScore = 3975;      // gold color
        // Color score for unidentified ms2s
        private const int UnidColorScore = 2925;    // greenish color

//        private int _viewDivisions;

        #endregion
    }
}
