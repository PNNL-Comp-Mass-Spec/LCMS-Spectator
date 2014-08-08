using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.DialogServices;
using LcmsSpectator.PlotModels;
using LcmsSpectator.Utils;
using LcmsSpectatorModels.Config;
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
        public string RawFileName { get; set; }
        public LcMsRun Lcms { get; set; }
        public ColorDictionary Colors { get; set; }
        public DelegateCommand CloseCommand { get; set; }
        public event EventHandler XicClosing;
        public event EventHandler SelectedScanNumberChanged;
        public XicViewModel(string rawFileName, LcMsRun lcms, ColorDictionary colors, IDialogService dialogService=null)
        {
            if (dialogService == null) dialogService = new DialogService();
            _dialogService = dialogService;
            RawFileName = rawFileName;
            Lcms = lcms;
            Colors = colors;
            FragmentPlotViewModel = new XicPlotViewModel("Fragment XIC", colors, XicXAxis, false, true, false);
            FragmentPlotViewModel.SelectedScanChanged += SelectFragmentScanNumber;
            HeavyFragmentPlotViewModel = new XicPlotViewModel("Heavy Fragment XIC", colors, XicXAxis, true, true, false);
            HeavyFragmentPlotViewModel.SelectedScanChanged += SelectFragmentScanNumber;
            PrecursorPlotViewModel = new XicPlotViewModel("Precursor XIC", colors, XicXAxis, false, false);
            HeavyPrecursorPlotViewModel = new XicPlotViewModel("Heavy Precursor XIC", colors, XicXAxis, true, false);
            SelectedScanNumber = 0;
            _showScanMarkers = false;
            _showHeavy = false;
            CloseCommand = new DelegateCommand(() =>
            {
                if (_dialogService.ConfirmationBox(String.Format("Are you sure you would like to close {0}?", RawFileName), "") && XicClosing != null)
                    XicClosing(this, EventArgs.Empty);
            });
        }

        public int SelectedScanNumber
        {
            get { return _selectedScanNumber; }
            set
            {
                if (_selectedScanNumber == value) return;
                _selectedScanNumber = value;
                FragmentPlotViewModel.SelectedScan = value;
                HeavyFragmentPlotViewModel.SelectedScan = value;
                PrecursorPlotViewModel.SelectedScan = value;
                HeavyPrecursorPlotViewModel.SelectedScan = value;
                OnPropertyChanged("SelectedScanNumber");
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
                    Task.Factory.StartNew(() => { HeavyPrecursorPlotViewModel.Xics = GetXics(_selectedHeavyPrecursors); });
                    Task.Factory.StartNew(() => { HeavyFragmentPlotViewModel.Xics = GetXics(_selectedHeavyFragments); });
                }
                OnPropertyChanged("ShowHeavy");
            }
        }

        private LinearAxis XicXAxis
        {
            get
            {
                if (_xicXAxis == null)
                {
                    var maxLcScan = Math.Max(Lcms.MaxLcScan+1, 1);
                    _xicXAxis = new LinearAxis(AxisPosition.Bottom, "Scan #")
                    {
                        Maximum = maxLcScan,
                        Minimum = 0,
                        AbsoluteMinimum = 0,
                        AbsoluteMaximum = maxLcScan
                    };
                    _xicXAxis.Zoom(0, maxLcScan);
                }
                return _xicXAxis;
            }
        }

        public void ZoomToScan(int scanNumber)
        {
            int minX, maxX;
            SelectedScanNumber = scanNumber;
            CalculateBounds(out minX, out maxX);
            XicXAxis.Minimum = minX;
            XicXAxis.Maximum = maxX;
            XicXAxis.Zoom(minX, maxX);
        }

        public void HighlightScan(int scanNum, bool unique, bool heavy)
        {
            FragmentPlotViewModel.HighlightScan(scanNum, unique && !heavy);
            HeavyFragmentPlotViewModel.HighlightScan(scanNum, unique && heavy);
        }

        private void SelectFragmentScanNumber(object sender, EventArgs e)
        {
            var vm = sender as XicPlotViewModel;
            if (vm == null) return;
            _selectedScanNumber = vm.SelectedScan;

            var otherVm = vm.Heavy ? FragmentPlotViewModel : HeavyFragmentPlotViewModel;
            otherVm.SelectedScan = _selectedScanNumber;

            // Create prsm
            var selectedScanNumber = _selectedScanNumber;
            var newPrsm = new PrSm
            {
                Heavy = vm.Heavy,
                RawFileName = RawFileName,
                Lcms = Lcms,
                Scan = selectedScanNumber,
            };
            if (SelectedScanNumberChanged != null) SelectedScanNumberChanged(this, new PrSmChangedEventArgs(newPrsm));
        }

        private void CalculateBounds(out int minS, out int maxS)
        {
            minS = SelectedScanNumber - 1000;
            maxS = SelectedScanNumber + 1000;
            if (SelectedScanNumber < 1000) minS = 0;
            minS = Math.Max(minS, Lcms.MinLcScan);
            if (SelectedScanNumber == 0) maxS = Lcms.MaxLcScan;
            if (SelectedScanNumber > Lcms.MaxLcScan)
            {
                minS = 0;
                maxS = Lcms.MaxLcScan;
            }
        }

        private List<LabeledXic> GetXics(IEnumerable<LabeledIon> ions)
        {
            var xics = new List<LabeledXic>();
            // get fragment xics
            foreach (var label in ions)
            {
                var ion = label.Ion;
                Xic xic;
                if (label.IsFragmentIon) xic = Lcms.GetFullFragmentExtractedIonChromatogram(ion.GetMostAbundantIsotopeMz(),
                                                                                            IcParameters.Instance.ProductIonTolerancePpm,
                                                                                            label.PrecursorIon.GetMostAbundantIsotopeMz());
                else xic = Lcms.GetFullExtractedIonChromatogram(ion.GetIsotopeMz(label.Index), IcParameters.Instance.PrecursorTolerancePpm);   
                var lXic = new LabeledXic(label.Composition, label.Index, xic, label.IonType, label.IsFragmentIon);
                xics.Add(lXic);
            }
            return xics;
        }

        private IDialogService _dialogService;

        private List<LabeledIon> _selectedFragments;
        private List<LabeledIon> _selectedPrecursors;

        private LinearAxis _xicXAxis;
        
        private bool _showScanMarkers;
        private int _selectedScanNumber;
        private List<LabeledIon> _selectedHeavyPrecursors;
        private List<LabeledIon> _selectedHeavyFragments;
        private bool _showHeavy;
    }
}
