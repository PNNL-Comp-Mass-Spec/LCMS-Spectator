using System;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.PlotModels;
using LcmsSpectator.Utils;
using LcmsSpectatorModels.Models;
using OxyPlot.Axes;

namespace LcmsSpectator.ViewModels
{
    public class XicViewModel: ViewModelBase
    {
        public XicPlotModel FragmentPlotModel { get; set; }
        public XicPlotModel PrecursorPlotModel { get; set; }

        public LcMsRun Lcms { get; set; }

        public int SelectedScanNumber { get; set; }

        public ColorDictionary Colors { get; set; }

        public DelegateCommand SelectFragmentScanNumberCommand { get; set; }

        public event EventHandler SelectedScanNumberChanged;

        public XicViewModel(ColorDictionary colors)
        {
            Colors = colors;
            SelectFragmentScanNumberCommand = new DelegateCommand(SelectFragmentScanNumber);
            _showScanMarkers = false;
            _currentScan = 0;
        }

        public bool ShowScanMarkers
        {
            get { return _showScanMarkers; }
            set
            {
                _showScanMarkers = value;
                OnPropertyChanged("ShowScanMarkers");
                UpdateZoomed(_currentChargeState);
            }
        }

        public void Update(ChargeStateId chargeState, int selectedScan=0)
        {
            if (_currentChargeState != null) _currentChargeState.ClearCache();
            _currentChargeState = chargeState;
            InitXicPlots(chargeState);
            FragmentUpdate(chargeState, selectedScan);
            PrecursorUpdate(chargeState);
        }

        public void UpdateZoomed(ChargeStateId chargeState)
        {
            _currentChargeState = chargeState;
            FragmentUpdate(chargeState, _currentScan);
            PrecursorUpdate(chargeState);
        }

        public void UpdateSelectedScan(int scanNum)
        {
            if (FragmentPlotModel == null) return;
            _currentScan = scanNum;
            FragmentPlotModel.SetPointMarker(scanNum);
        }

        public void FragmentUpdate(ChargeStateId chargeState, int selectedScan=0)
        {
            if (chargeState != null && _xicXAxis != null)
            {
                var fragmentXics = chargeState.SelectedFragmentXics;
                if (fragmentXics.Count == 0) return;
                var fragmentPlotModel = new XicPlotModel("Fragment Ion XIC", _xicXAxis,
                                                            fragmentXics, Colors, ShowScanMarkers);
                FragmentPlotModel = fragmentPlotModel;
                OnPropertyChanged("FragmentPlotModel");
                _currentScan = selectedScan;
                if (selectedScan != 0) UpdateSelectedScan(selectedScan);
            }
        }

        public void PrecursorUpdate(ChargeStateId chargeState)
        {
            if (chargeState != null && _xicXAxis != null)
            {
                var precursorXics = chargeState.SelectedPrecursorXics;
                if (precursorXics.Count == 0) return;
                var precursorPlotModel = new XicPlotModel("Precursor Ion XIC", _xicXAxis,
                                                            precursorXics, Colors, ShowScanMarkers);
                PrecursorPlotModel = precursorPlotModel;
                OnPropertyChanged("PrecursorPlotModel");
            }
        }

        private void SelectFragmentScanNumber()
        {
            var x = FragmentPlotModel.SelectedDataPoint.X;
            SelectedScanNumber = (int) Math.Round(x);
            SelectedScanNumberChanged(this, new PrSmChangedEventArgs(CreatePrSm(SelectedScanNumber)));
            UpdateSelectedScan(SelectedScanNumber);
        }

        private void InitXicPlots(ChargeStateId chargeState)
        {
            var minS = chargeState.MedianScan - 1000;
            var maxS = chargeState.MedianScan + 1000;
            if (chargeState.MedianScan < 1000) minS = 0;
            if (chargeState.MedianScan == 0) maxS = chargeState.AbsoluteMaxScan;
            // Common x axis
            _xicXAxis = new LinearAxis(AxisPosition.Bottom, "Scan #")
            {
                Maximum = maxS,
                Minimum = minS,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = chargeState.AbsoluteMaxScan
            };
            _xicXAxis.Zoom(minS, maxS);
        }

        private PrSm CreatePrSm(int scanNum)
        {
            var selectedScanNumber = scanNum;
            var newPrsm = new PrSm
            {
                Lcms = Lcms,
                ProteinNameDesc = _currentChargeState.ProteinNameDesc,
                Sequence = _currentChargeState.Sequence,
                SequenceText = _currentChargeState.SequenceText,
                Scan = selectedScanNumber,
                Charge = _currentChargeState.Charge,
            };
            return newPrsm;
        }

        private int _currentScan;

        private ChargeStateId _currentChargeState;

        private LinearAxis _xicXAxis;
        
        private bool _showScanMarkers;
    }
}
