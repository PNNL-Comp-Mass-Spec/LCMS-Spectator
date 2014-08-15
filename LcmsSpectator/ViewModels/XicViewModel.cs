using System;
using System.Collections.Generic;
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
            FragmentPlotViewModel = new XicPlotViewModel("Fragment XIC", colors, XicXAxis, false, true, false);
            FragmentPlotViewModel.SelectedScanChanged += SelectFragmentScanNumber;
            HeavyFragmentPlotViewModel = new XicPlotViewModel("Heavy Fragment XIC", colors, XicXAxis, true, true, false);
            HeavyFragmentPlotViewModel.SelectedScanChanged += SelectFragmentScanNumber;
            PrecursorPlotViewModel = new XicPlotViewModel("Precursor XIC", colors, XicXAxis, false, false);
            HeavyPrecursorPlotViewModel = new XicPlotViewModel("Heavy Precursor XIC", colors, XicXAxis, true, false);
            SelectedRetentionTime = 0;
            _showScanMarkers = false;
            _showHeavy = false;
            XicXAxis.AxisChanged += UpdateAreaRatioLabels;
            CloseCommand = new DelegateCommand(() =>
            {
                if (_dialogService.ConfirmationBox(String.Format("Are you sure you would like to close {0}?", RawFileName), "") && XicClosing != null)
                    XicClosing(this, EventArgs.Empty);
            });
        }

        public string RawFileName
        {
            get { return Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(RawFilePath)); }
        }

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
                    UpdatePrecursorAreaRatioLabels();
                });
                OnPropertyChanged("SelectedPrecursors");
            }
        }

        public List<LabeledIon> SelectedHeavyPrecursors
        {
            get { return _selectedHeavyPrecursors; }
            set
            {
                if (SelectedHeavyPrecursors == value) return;
                _selectedHeavyPrecursors = value;
                Task.Factory.StartNew(() =>
                {
                    if (_showHeavy) HeavyPrecursorPlotViewModel.Xics = GetXics(_selectedHeavyPrecursors);
                    UpdatePrecursorAreaRatioLabels();
                });
                OnPropertyChanged("SelectedHeavyPrecursors");
            }
        }

        public List<LabeledIon> SelectedFragments
        {
            get { return _selectedFragments; }
            set
            {
                _selectedFragments = value;
                Task.Factory.StartNew(() =>
                {
                    FragmentPlotViewModel.Xics = GetXics(_selectedFragments);
                    UpdateFragmentAreaRatioLabels(); 
                });
                OnPropertyChanged("SelectedFragments");
            }
        }

        public List<LabeledIon> SelectedHeavyFragments
        {
            get { return _selectedHeavyFragments; }
            set
            {
                _selectedHeavyFragments = value;
                Task.Factory.StartNew(() =>
                {
                    if (_showHeavy) HeavyFragmentPlotViewModel.Xics = GetXics(_selectedHeavyFragments);
                    UpdateFragmentAreaRatioLabels(); 
                });
                OnPropertyChanged("SelectedHeavyFragments");
            }
        }

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
                    if (_selectedHeavyFragments != null) Task.Factory.StartNew(() =>
                    {
                        HeavyFragmentPlotViewModel.Xics = GetXics(_selectedHeavyFragments);
                        UpdateFragmentAreaRatioLabels();
                    });
                }
                OnPropertyChanged("ShowHeavy");
            }
        }

        public void ZoomToRt(double rt)
        {
            double minX, maxX;
            SelectedRetentionTime = rt;
            CalculateBounds(out minX, out maxX);
            XicXAxis.Minimum = minX;
            XicXAxis.Maximum = maxX;
            XicXAxis.Zoom(minX, maxX);
        }

        public void HighlightRetentionTime(double rt, bool unique, bool heavy)
        {
            FragmentPlotViewModel.HighlightRt(rt, unique && !heavy);
            HeavyFragmentPlotViewModel.HighlightRt(rt, unique && heavy);
        }

        private LinearAxis XicXAxis
        {
            get
            {
                if (_xicXAxis == null)
                {
                    var maxRt = Math.Max(Lcms.MaxRetentionTime, 1.0);
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

        private void UpdateFragmentAreaRatioLabels()
        {
            if (!ShowHeavy) return;
            if (FragmentPlotViewModel == null || FragmentPlotViewModel.Plot == null) return;
            if (XicXAxis == null) return;
            var min = (int)XicXAxis.ActualMinimum;
            var max = (int)XicXAxis.ActualMaximum;
            Task.Factory.StartNew(() =>
            {
                var fragmentArea = FragmentPlotViewModel.GetAreaOfRange(min, max);
                var heavyFragmentArea = HeavyFragmentPlotViewModel.GetAreaOfRange(min, max);
                var ratio = fragmentArea / heavyFragmentArea;
                if (ratio.Equals(Double.NaN) || ratio < 0) ratio = 0.0;
                ratio = Math.Round(ratio, 4);
                FragmentAreaRatioLabel = String.Format("Area ratio: {0}", ratio);
                OnPropertyChanged("FragmentAreaRatioLabel");
            });
        }

        private void UpdatePrecursorAreaRatioLabels()
        {
            if (!ShowHeavy) return;
            if (PrecursorPlotViewModel == null || PrecursorPlotViewModel.Plot == null) return;
            if (XicXAxis == null) return;
            var min = (int)XicXAxis.ActualMinimum;
            var max = (int)XicXAxis.ActualMaximum;
            Task.Factory.StartNew(() =>
            {
                var precursorArea = PrecursorPlotViewModel.GetAreaOfRange(min, max);
                var heavyPrecursorArea = HeavyPrecursorPlotViewModel.GetAreaOfRange(min, max);
                var ratio = precursorArea / heavyPrecursorArea;
                if (ratio.Equals(Double.NaN) || ratio < 0) ratio = 0.0;
                ratio = Math.Round(ratio, 4);
                PrecursorAreaRatioLabel = String.Format("Area ratio: {0}", ratio);
                OnPropertyChanged("PrecursorAreaRatioLabel");
            });
        }

        private void UpdateAreaRatioLabels(object sender, AxisChangedEventArgs e)
        {
            Task.Factory.StartNew(UpdateFragmentAreaRatioLabels);
            Task.Factory.StartNew(UpdatePrecursorAreaRatioLabels);
        }

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

        private List<LabeledXic> GetXics(IEnumerable<LabeledIon> ions)
        {
            var xics = new List<LabeledXic>();
            var smoother = new SavitzkyGolaySmoother(IcParameters.Instance.PointsToSmooth, 2);
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
                var smoothedXic = IonUtils.SmoothXic(smoother, rtXic).ToList();
                var lXic = new LabeledXic(label.Composition, label.Index, smoothedXic, label.IonType, label.IsFragmentIon);
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
    }
}
