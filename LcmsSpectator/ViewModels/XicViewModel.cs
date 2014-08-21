using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.DialogServices;
using LcmsSpectator.PlotModels;
using LcmsSpectator.Utils;
using LcmsSpectatorModels.Config;
using LcmsSpectatorModels.Models;
using LcmsSpectatorModels.Utils;
using MultiDimensionalPeakFinding;
using OxyPlot.Axes;

namespace LcmsSpectator.ViewModels
{
    public class XicViewModel: ViewModelBase
    {
        public XicPlotViewModel FragmentPlotViewModel { get; set; }
        public XicPlotViewModel HeavyFragmentPlotViewModel { get; set; }
        public XicPlotViewModel PrecursorPlotViewModel { get; set; }
        public XicPlotViewModel HeavyPrecursorPlotViewModel { get; set; }
        public string FragmentAreaRatioLabel { get; private set; }
        public string PrecursorAreaRatioLabel { get; private set; }
        public ColorDictionary Colors { get; set; }
        public RtLcMsRun Lcms { get; private set; }
        public DelegateCommand CloseCommand { get; set; }
        public event EventHandler XicClosing;
        public event EventHandler SelectedScanNumberChanged;
        public XicViewModel(string rawFilePath, ColorDictionary colors, IDialogService dialogService=null)
        {
            if (dialogService == null) dialogService = new DialogService();
            _dialogService = dialogService;
            RawFilePath = rawFilePath;
            Colors = colors;
            FragmentPlotViewModel = new XicPlotViewModel("Fragment XIC", colors, XicXAxis, false, false);
            FragmentPlotViewModel.SelectedScanChanged += SelectFragmentScanNumber;
            HeavyFragmentPlotViewModel = new XicPlotViewModel("Heavy Fragment XIC", colors, XicXAxis, true, false);
            HeavyFragmentPlotViewModel.SelectedScanChanged += SelectFragmentScanNumber;
            PrecursorPlotViewModel = new XicPlotViewModel("Precursor XIC", colors, XicXAxis, false);
            HeavyPrecursorPlotViewModel = new XicPlotViewModel("Heavy Precursor XIC", colors, XicXAxis, true);
            SelectedRetentionTime = 0;
            _showScanMarkers = false;
            _showHeavy = false;
            _showFragmentXic = true;
            XicXAxis.AxisChanged += UpdateAreaRatioLabels;
            CloseCommand = new DelegateCommand(() =>
            {
                if (_dialogService.ConfirmationBox(String.Format("Are you sure you would like to close {0}?", RawFileName), "") && XicClosing != null)
                    XicClosing(this, EventArgs.Empty);
            });
        }

        /// <summary>
        /// Raw file name without path or extension. For displaying on tab header.
        /// </summary>
        public string RawFileName
        {
            get { return Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(RawFilePath)); }
        }


        /// <summary>
        /// Full path to the raw file including extension.
        /// </summary>
        public string RawFilePath
        {
            get { return _rawFilePath; }
            set
            {
                _rawFilePath = value;
                Lcms = RtLcMsRun.GetRtLcMsRun(_rawFilePath, MassSpecDataType.XCaliburRun, 0, 0);
                OnPropertyChanged("RawFilePath");
            }
        }

        /// <summary>
        /// Retention currently selected and highlighted in all plots.
        /// </summary>
        public double SelectedRetentionTime
        {
            get { return _selectedRetentionTime; }
            set
            {
                if (_selectedRetentionTime.Equals(value)) return;
                _selectedRetentionTime = value;
                FragmentPlotViewModel.SelectedRt = value;
                HeavyFragmentPlotViewModel.SelectedRt = value;
                PrecursorPlotViewModel.SelectedRt = value;
                HeavyPrecursorPlotViewModel.SelectedRt = value;
            }
        }

        /// <summary>
        /// Precursor ions to create precursor XICs from.
        /// </summary>
        public List<LabeledIon> SelectedPrecursors
        {
            get { return _selectedPrecursors; }
            set
            {
                if (SelectedPrecursors == value) return;
                _selectedPrecursors = value;
                Task.Factory.StartNew(() =>
                {
                    PrecursorPlotViewModel.Xics = GetXics(_selectedPrecursors);
                    UpdatePrecursorAreaRatioLabels();   // XIC changed, update area
                });
                OnPropertyChanged("SelectedPrecursors");
            }
        }

        /// <summary>
        /// Heavy precursor ions to create heavy precursor XICs from.
        /// </summary>
        public List<LabeledIon> SelectedHeavyPrecursors
        {
            get { return _selectedHeavyPrecursors; }
            set
            {
                if (SelectedHeavyPrecursors == value) return;
                _selectedHeavyPrecursors = value;
                Task.Factory.StartNew(() =>
                {
                    // Only create heavy xics if the heavy xics are visible
                    if (_showHeavy) HeavyPrecursorPlotViewModel.Xics = GetXics(_selectedHeavyPrecursors);
                    UpdatePrecursorAreaRatioLabels();   // XIC changed, update area
                });
                OnPropertyChanged("SelectedHeavyPrecursors");
            }
        }

        /// <summary>
        /// Fragment ions to create fragment XICs from.
        /// </summary>
        public List<LabeledIon> SelectedFragments
        {
            get { return _selectedFragments; }
            set
            {
                _selectedFragments = value;
                Task.Factory.StartNew(() =>
                {
                    if (ShowFragmentXic) FragmentPlotViewModel.Xics = GetXics(_selectedFragments);
                    UpdateFragmentAreaRatioLabels();        // XIC changed, update area
                });
                OnPropertyChanged("SelectedFragments");
            }
        }

        /// <summary>
        /// Heavy fragment ions to create heavy fragment XICs from.
        /// </summary>
        public List<LabeledIon> SelectedHeavyFragments
        {
            get { return _selectedHeavyFragments; }
            set
            {
                _selectedHeavyFragments = value;
                Task.Factory.StartNew(() =>
                {
                    // Only create heavy xics if the heavy xics are visible
                    if (_showHeavy && _showFragmentXic) HeavyFragmentPlotViewModel.Xics = GetXics(_selectedHeavyFragments);
                    UpdateFragmentAreaRatioLabels();    // XIC changed, update area
                });
                OnPropertyChanged("SelectedHeavyFragments");
            }
        }

        /// <summary>
        /// Shows and hides the point markers on the XIC plots.
        /// </summary>
        public bool ShowScanMarkers
        {
            get { return _showScanMarkers; }
            set
            {
                _showScanMarkers = value;
                FragmentPlotViewModel.ShowScanMarkers = _showScanMarkers;
                HeavyFragmentPlotViewModel.ShowScanMarkers = _showScanMarkers;
                PrecursorPlotViewModel.ShowScanMarkers = _showScanMarkers;
                HeavyPrecursorPlotViewModel.ShowScanMarkers = _showScanMarkers;
                OnPropertyChanged("ShowScanMarkers");
            }
        }


        /// <summary>
        /// Toggle fragment XICs.
        /// </summary>
        public bool ShowFragmentXic
        {
            get { return _showFragmentXic; }
            set
            {
                _showFragmentXic = value;
                if (_showFragmentXic)
                {
                    if (_selectedFragments != null) Task.Factory.StartNew(() =>
                    {
                        FragmentPlotViewModel.Xics = GetXics(_selectedFragments);
                    });
                    if (_showHeavy && _selectedHeavyFragments != null) Task.Factory.StartNew(() =>
                    {
                        HeavyFragmentPlotViewModel.Xics = GetXics(_selectedHeavyFragments);
                        UpdateFragmentAreaRatioLabels();
                    });
                }
                OnPropertyChanged("ShowFragmentXic");
            }
        }

        /// <summary>
        /// Shows and hides the heavy XICs.
        /// If the heavy XICs are being toggled on, it updates them.
        /// </summary>
        public bool ShowHeavy
        {
            get { return _showHeavy; }
            set
            {
                _showHeavy = value;
                if (_showHeavy)
                {
                    if (_selectedHeavyPrecursors != null) Task.Factory.StartNew(() => 
                    { 
                        HeavyPrecursorPlotViewModel.Xics = GetXics(_selectedHeavyPrecursors);
                        UpdatePrecursorAreaRatioLabels(); 
                    });
                    if (_selectedHeavyFragments != null && _showFragmentXic) Task.Factory.StartNew(() =>
                    {
                        HeavyFragmentPlotViewModel.Xics = GetXics(_selectedHeavyFragments);
                        UpdateFragmentAreaRatioLabels();
                    });
                }
                OnPropertyChanged("ShowHeavy");
            }
        }

        /// <summary>
        /// Zoom all plots to a particular retention time.
        /// </summary>
        /// <param name="rt">Retention time to zoom to.</param>
        public void ZoomToRt(double rt)
        {
            double minX, maxX;
            SelectedRetentionTime = rt;
            CalculateBounds(out minX, out maxX);
            XicXAxis.Minimum = minX;
            XicXAxis.Maximum = maxX;
            XicXAxis.Zoom(minX, maxX);
        }

        /// <summary>
        /// Highlight a particular retention time for all plots.
        /// </summary>
        /// <param name="rt">Retention time to highlight.</param>
        /// <param name="unique">Is this XicViewModel for the raw file that is selected?</param>
        /// <param name="heavy">Was the light (false) or heavy (true) plot selected?</param>
        public void HighlightRetentionTime(double rt, bool unique, bool heavy)
        {
            FragmentPlotViewModel.HighlightRt(rt, unique && !heavy);
            PrecursorPlotViewModel.HighlightRt(rt, false);
            HeavyFragmentPlotViewModel.HighlightRt(rt, unique && heavy);
            HeavyPrecursorPlotViewModel.HighlightRt(rt, false);
        }

        /// <summary>
        /// Shared x axis for all plots. Sharing an X axis allows all plots to zoom and pan together.
        /// </summary>
        private LinearAxis XicXAxis
        {
            get
            {
                if (_xicXAxis == null)
                {
                    var maxRt = Math.Max(Lcms.MaxRetentionTime, 1.0);
                    _xicXAxis = new LinearAxis(AxisPosition.Bottom, "Retention Time")
                    {
                        MinimumRange = Lcms.MaxRetentionTimeDelta,
                        Maximum = maxRt + 0.0001,
                        Minimum = 0,
                        AbsoluteMinimum = 0,
                        AbsoluteMaximum = maxRt + 0.0001
                    };
                    _xicXAxis.Zoom(0, maxRt);
                }
                return _xicXAxis;
            }
        }

        /// <summary>
        /// Update the ratio labels for the fragment ion Xics.
        /// Ratios are (light / heavy)
        /// </summary>
        private void UpdateFragmentAreaRatioLabels()
        {
            if (!ShowHeavy || !ShowFragmentXic) return;
            if (FragmentPlotViewModel == null || FragmentPlotViewModel.Plot == null) return;
            if (XicXAxis == null) return;
            var min = XicXAxis.ActualMinimum;
            var max = XicXAxis.ActualMaximum;
            Task.Factory.StartNew(() =>
            {
                var fragmentArea = FragmentPlotViewModel.GetAreaOfRange(min, max);
                var heavyFragmentArea = HeavyFragmentPlotViewModel.GetAreaOfRange(min, max);
                var ratio = fragmentArea / heavyFragmentArea;
                if (ratio.Equals(Double.NaN) || ratio < 0) ratio = 0.0;
                string formatted;
                if (ratio > 1000 || ratio < 0.001) formatted = String.Format("{0:0.###EE0}", ratio);
                else formatted = Math.Round(ratio, 3).ToString(CultureInfo.InvariantCulture);
                FragmentAreaRatioLabel = String.Format("Area ratio: {0}", formatted);
                OnPropertyChanged("FragmentAreaRatioLabel");
            });
        }

        /// <summary>
        /// Update the ratio labels for the precursor ion Xics.
        /// Ratios are (light / heavy)
        /// </summary>
        private void UpdatePrecursorAreaRatioLabels()
        {
            if (!ShowHeavy) return;
            if (PrecursorPlotViewModel == null || PrecursorPlotViewModel.Plot == null) return;
            if (XicXAxis == null) return;
            var min = XicXAxis.ActualMinimum;
            var max = XicXAxis.ActualMaximum;
            Task.Factory.StartNew(() =>
            {
                var precursorArea = PrecursorPlotViewModel.GetAreaOfRange(min, max);
                var heavyPrecursorArea = HeavyPrecursorPlotViewModel.GetAreaOfRange(min, max);
                var ratio = precursorArea / heavyPrecursorArea;
                if (ratio.Equals(Double.NaN) || ratio < 0) ratio = 0.0;
                string formatted;
                if (ratio > 1000 || ratio < 0.001) formatted = String.Format("{0:0.###EE0}", ratio);
                else formatted = Math.Round(ratio, 3).ToString(CultureInfo.InvariantCulture);
                PrecursorAreaRatioLabel = String.Format("Area ratio: {0}", formatted);
                OnPropertyChanged("PrecursorAreaRatioLabel");
            });
        }

        /// <summary>
        /// Event handler to update area ratio labels when shared x axis is zoomed or panned.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateAreaRatioLabels(object sender, AxisChangedEventArgs e)
        {
            Task.Factory.StartNew(UpdateFragmentAreaRatioLabels);
            Task.Factory.StartNew(UpdatePrecursorAreaRatioLabels);
        }

        /// <summary>
        /// Event handler to handle when a retention time slice is selected on one of the XICs.
        /// Creates a PrSm and then triggers the SelectedScanNumberChanged event to pass it up
        /// to the MainWindowViewModel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectFragmentScanNumber(object sender, EventArgs e)
        {
            var vm = sender as XicPlotViewModel;
            if (vm == null) return;
            var selectedScanNumber = vm.SelectedScan;

            var otherVm = vm.Heavy ? FragmentPlotViewModel : HeavyFragmentPlotViewModel;
            otherVm.SelectedRt = selectedScanNumber;

            // Create prsm
            var newPrsm = new PrSm
            {
                Heavy = vm.Heavy,
                RawFileName = RawFileName,
                Lcms = Lcms,
                Scan = selectedScanNumber,
            };
            if (SelectedScanNumberChanged != null) SelectedScanNumberChanged(this, new PrSmChangedEventArgs(newPrsm));
        }

        /// <summary>
        /// Calculate the default min and max on the X Axis for when it is zoomed to a point.
        /// </summary>
        /// <param name="minRt">Value calculated for x axis minimum.</param>
        /// <param name="maxRt">Value calculated for x axis maximum.</param>
        private void CalculateBounds(out double minRt, out double maxRt)
        {
            var minLcmsRt = Lcms.MinRetentionTime;
            var maxLcmsRt = Lcms.MaxRetentionTime;
            minRt = SelectedRetentionTime - 1;
            maxRt = SelectedRetentionTime + 1;
            if (SelectedRetentionTime < 1) minRt = 0;
            minRt = Math.Max(minRt, minLcmsRt);
            if (SelectedRetentionTime.Equals(0)) maxRt = maxLcmsRt;
            if (SelectedRetentionTime > maxLcmsRt)
            {
                minRt = 0;
                maxRt = maxLcmsRt;
            }
        }

        /// <summary>
        /// Create Xics from list of ions. Smooths XICs and adds retention time to each point.
        /// </summary>
        /// <param name="ions">Ions to create XICs from.</param>
        /// <returns>Labeled XICs</returns>
        private List<LabeledXic> GetXics(IEnumerable<LabeledIon> ions)
        {
            var xics = new List<LabeledXic>();
            var smoother = (IcParameters.Instance.PointsToSmooth == 0) ? 
                            null : new SavitzkyGolaySmoother(IcParameters.Instance.PointsToSmooth, 2);
            // get fragment xics
            foreach (var label in ions)
            {
                var ion = label.Ion;
                Xic xic;
                if (label.IsFragmentIon) xic = Lcms.GetFullFragmentExtractedIonChromatogram(ion.GetMostAbundantIsotopeMz(),
                                                                                            IcParameters.Instance.ProductIonTolerancePpm,
                                                                                            label.PrecursorIon.GetMostAbundantIsotopeMz());
                else xic = Lcms.GetFullExtractedIonChromatogram(ion.GetIsotopeMz(label.Index), IcParameters.Instance.PrecursorTolerancePpm);
                // add retention time
                var rtXic = new List<XicRetPoint>();
                rtXic.AddRange(xic.Select(xicPoint => new XicRetPoint(xicPoint.ScanNum, Lcms.GetRetentionTime(xicPoint.ScanNum), xicPoint.Mz, xicPoint.Intensity)));
                // smooth
                if (smoother != null) rtXic = IonUtils.SmoothXic(smoother, rtXic).ToList();
                var lXic = new LabeledXic(label.Composition, label.Index, rtXic, label.IonType, label.IsFragmentIon);
                xics.Add(lXic);
            }
            return xics;
        }

        private readonly IDialogService _dialogService;

        private List<LabeledIon> _selectedFragments;
        private List<LabeledIon> _selectedPrecursors;

        private LinearAxis _xicXAxis;
        
        private bool _showScanMarkers;
        private List<LabeledIon> _selectedHeavyPrecursors;
        private List<LabeledIon> _selectedHeavyFragments;
        private bool _showHeavy;
        private string _rawFilePath;
        private double _selectedRetentionTime;
        private bool _showFragmentXic;
    }
}
