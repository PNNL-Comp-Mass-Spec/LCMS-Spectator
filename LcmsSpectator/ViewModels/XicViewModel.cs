using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.DialogServices;
using LcmsSpectator.PlotModels;
using LcmsSpectator.Utils;
using LcmsSpectatorModels.Models;
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
        public ILcMsRun Lcms { get; private set; }
        public DelegateCommand CloseCommand { get; private set; }
        public DelegateCommand OpenHeavyModificationsCommand { get; private set; }
        public event EventHandler XicClosing;
        public event EventHandler SelectedScanNumberChanged;
        public XicViewModel(string rawFilePath, ColorDictionary colors, IMainDialogService dialogService=null)
        {
            if (dialogService == null) dialogService = new MainDialogService();
            _dialogService = dialogService;
            RawFilePath = rawFilePath;
            Colors = colors;
            FragmentPlotViewModel = new XicPlotViewModel(_dialogService, Lcms, "Fragment XIC", colors, XicXAxis, false, false);
            FragmentPlotViewModel.SelectedScanChanged += SelectFragmentScanNumber;
            FragmentPlotViewModel.PlotChanged += PlotChanged;
            HeavyFragmentPlotViewModel = new XicPlotViewModel(_dialogService, Lcms, "Heavy Fragment XIC", colors, XicXAxis, true, false);
            HeavyFragmentPlotViewModel.SelectedScanChanged += SelectFragmentScanNumber;
            HeavyFragmentPlotViewModel.PlotChanged += PlotChanged;
            PrecursorPlotViewModel = new XicPlotViewModel(_dialogService, Lcms, "Precursor XIC", colors, XicXAxis, false);
            PrecursorPlotViewModel.PlotChanged += PlotChanged;
            HeavyPrecursorPlotViewModel = new XicPlotViewModel(_dialogService, Lcms, "Heavy Precursor XIC", colors, XicXAxis, true);
            HeavyPrecursorPlotViewModel.PlotChanged += PlotChanged;
            SelectedRetentionTime = 0;
            _showScanMarkers = false;
            _showHeavy = false;
            _showFragmentXic = false;
            XicXAxis.AxisChanged += XAxisChanged;
            CloseCommand = new DelegateCommand(() =>
            {
                if (_dialogService.ConfirmationBox(String.Format("Are you sure you would like to close {0}?", RawFileName), "") && XicClosing != null)
                    XicClosing(this, EventArgs.Empty);
            });
            OpenHeavyModificationsCommand = new DelegateCommand(OpenHeavyModifications);
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
                Lcms = RafLcMsRun.GetLcMsRun(_rawFilePath, MassSpecDataType.XCaliburRun, 0, 0);
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
                _selectedPrecursors = value;
                if (!_showHeavy) PrecursorPlotViewModel.Ions = _selectedPrecursors;
                UpdatePrecursorAreaRatioLabels();   // XIC changed, update area
                OnPropertyChanged("SelectedPrecursors");
            }
        }

        /// <summary>
        /// Light precursor ions to create light precursor XICs from.
        /// </summary>
        public List<LabeledIon> SelectedLightPrecursors
        {
            get { return _selectedLightPrecursors; }
            set
            {
                _selectedLightPrecursors = value;
                // Only create light xics if the heavy xics are visible
                if (_showHeavy) PrecursorPlotViewModel.Ions = _selectedLightPrecursors;
                UpdatePrecursorAreaRatioLabels();   // XIC changed, update area
                OnPropertyChanged("SelectedLightPrecursors");
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
                _selectedHeavyPrecursors = value;
                // Only create heavy xics if the heavy xics are visible
                if (_showHeavy) HeavyPrecursorPlotViewModel.Ions = _selectedHeavyPrecursors;
                UpdatePrecursorAreaRatioLabels();   // XIC changed, update area
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
                if (ShowFragmentXic && !ShowHeavy) FragmentPlotViewModel.Ions = _selectedFragments;
                UpdateFragmentAreaRatioLabels();        // XIC changed, update area
                OnPropertyChanged("SelectedFragments");
            }
        }

        /// <summary>
        /// Heavy fragment ions to create heavy fragment XICs from.
        /// </summary>
        public List<LabeledIon> SelectedLightFragments
        {
            get { return _selectedLightFragments; }
            set
            {
                _selectedLightFragments = value;
                // Only create heavy xics if the heavy xics are visible
                if (_showHeavy && _showFragmentXic) FragmentPlotViewModel.Ions = _selectedLightFragments;
                UpdateFragmentAreaRatioLabels();    // XIC changed, update area
                OnPropertyChanged("SelectedLightFragments");
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
                // Only create heavy xics if the heavy xics are visible
                if (_showHeavy && _showFragmentXic) HeavyFragmentPlotViewModel.Ions = _selectedHeavyFragments;
                UpdateFragmentAreaRatioLabels();    // XIC changed, update area
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
                    if (_showHeavy && _selectedHeavyFragments != null)
                    {
                        FragmentPlotViewModel.Ions = _selectedLightFragments;
                        HeavyFragmentPlotViewModel.Ions = _selectedHeavyFragments;
                        UpdateFragmentAreaRatioLabels();
                    }
                    else if (_selectedFragments != null)
                    {
                        FragmentPlotViewModel.Ions = _selectedFragments;
                    }
                }
                else
                {

                    if (_showHeavy && _selectedHeavyFragments != null)
                    {
                        HeavyFragmentPlotViewModel.Ions = new List<LabeledIon>();
                        UpdateFragmentAreaRatioLabels();
                    }
                    else if (_selectedFragments != null)
                    {
                        FragmentPlotViewModel.Ions = new List<LabeledIon>();
                    }
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
                    if (_selectedHeavyPrecursors != null)
                    {
                        PrecursorPlotViewModel.Ions = _selectedLightPrecursors;
                        HeavyPrecursorPlotViewModel.Ions = _selectedHeavyPrecursors;
                        UpdatePrecursorAreaRatioLabels(); 
                    }
                    if (_selectedHeavyFragments != null && _showFragmentXic)
                    {
                        FragmentPlotViewModel.Ions = _selectedLightFragments;
                        HeavyFragmentPlotViewModel.Ions = _selectedHeavyFragments;
                        UpdateFragmentAreaRatioLabels();
                    }
                }
                else
                {
                    if (_selectedHeavyPrecursors != null)
                    {
                        PrecursorPlotViewModel.Ions = _selectedPrecursors;
                        HeavyPrecursorPlotViewModel.Ions = new List<LabeledIon>();
                        UpdatePrecursorAreaRatioLabels();
                    }
                    if (_selectedHeavyFragments != null && _showFragmentXic)
                    {
                        FragmentPlotViewModel.Ions = _selectedFragments;
                        HeavyFragmentPlotViewModel.Ions = new List<LabeledIon>();
                        UpdateFragmentAreaRatioLabels();
                    }
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
        /// Update and regenerate all plots
        /// </summary>
        public void UpdatePlots()
        {
            if (_showHeavy)
            {
                if (_selectedHeavyPrecursors != null)
                {
                    PrecursorPlotViewModel.Ions = _selectedLightPrecursors;
                    HeavyPrecursorPlotViewModel.Ions = _selectedHeavyPrecursors;
                    UpdatePrecursorAreaRatioLabels();
                }
                if (_selectedHeavyFragments != null && _showFragmentXic)
                {
                    FragmentPlotViewModel.Ions = _selectedLightFragments;
                    HeavyFragmentPlotViewModel.Ions = _selectedHeavyFragments;
                }
            }
            else
            {
                if (_selectedHeavyPrecursors != null)
                {
                    PrecursorPlotViewModel.Ions = _selectedPrecursors;
                    HeavyPrecursorPlotViewModel.Ions = new List<LabeledIon>();
                    UpdatePrecursorAreaRatioLabels();
                }
                if (_selectedHeavyFragments != null && _showFragmentXic)
                {
                    FragmentPlotViewModel.Ions = _selectedFragments;
                    HeavyFragmentPlotViewModel.Ions = new List<LabeledIon>();
                }
            }
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
                    var maxRt = Math.Max(Lcms.GetElutionTime(Lcms.MaxLcScan), 1.0);
                    _xicXAxis = new LinearAxis(AxisPosition.Bottom, "Retention Time")
                    {
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
        /// Event handler for XAxis changed to update area ratio labels when shared x axis is zoomed or panned.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XAxisChanged(object sender, AxisChangedEventArgs e)
        {
            UpdateFragmentAreaRatioLabels();
            UpdatePrecursorAreaRatioLabels();
        }

        /// <summary>
        /// Event handler for Plot changed to update area ratio labels when plots are updated.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlotChanged(object sender, EventArgs e)
        {
            var plotvm = sender as XicPlotViewModel;
            if (plotvm == null || plotvm == FragmentPlotViewModel || plotvm == HeavyFragmentPlotViewModel) UpdateFragmentAreaRatioLabels();
            if (plotvm == null || plotvm == PrecursorPlotViewModel || plotvm == HeavyPrecursorPlotViewModel) UpdatePrecursorAreaRatioLabels();
        }

        private void OpenHeavyModifications()
        {
            _dialogService.OpenHeavyModifications(new HeavyModificationsWindowViewModel());
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
            var minLcmsRt = Lcms.GetElutionTime(Lcms.MaxLcScan);
            var maxLcmsRt = Lcms.GetElutionTime(Lcms.MaxLcScan);
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

        private readonly IMainDialogService _dialogService;

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
        private List<LabeledIon> _selectedLightPrecursors;
        private List<LabeledIon> _selectedLightFragments;
    }
}
