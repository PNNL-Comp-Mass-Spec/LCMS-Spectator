using System;
using System.Collections.Generic;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.PlotModels;
using LcmsSpectator.Utils;
using LcmsSpectatorModels.Config;
using LcmsSpectatorModels.Models;
using OxyPlot.Axes;

namespace LcmsSpectator.ViewModels
{
    public class XicViewModel: ViewModelBase
    {
        public string RawFileName { get; set; }
        public LcMsRun Lcms { get; set; }
        public ColorDictionary Colors { get; set; }
        public DelegateCommand SelectFragmentScanNumberCommand { get; set; }
        public event EventHandler SelectedScanNumberChanged;
        public XicViewModel(string rawFileName, ColorDictionary colors)
        {
            RawFileName = rawFileName;
            Colors = colors;
            _precursorXicCache = new Dictionary<Composition, LabeledXic>();
            _fragmentXicCache = new Dictionary<Composition, LabeledXic>();
            FragmentPlotModel = new XicPlotModel();
            PrecursorPlotModel = new XicPlotModel();
            SelectFragmentScanNumberCommand = new DelegateCommand(SelectFragmentScanNumber);
            SelectedScanNumber = 0;
            _showScanMarkers = false;
        }

        public XicPlotModel FragmentPlotModel
        {
            get { return _fragmentPlotModel; }
            private set
            {
                if (_fragmentPlotModel == value) return;
                _fragmentPlotModel = value;
                OnPropertyChanged("FragmentPlotModel");
            }
        }

        public XicPlotModel PrecursorPlotModel
        {
            get { return _precursorPlotModel; }
            private set
            {
                if (_precursorPlotModel == value) return;
                _precursorPlotModel = value;
                OnPropertyChanged("PrecursorPlotModel");
            }
        }

        public int SelectedScanNumber
        {
            get { return _selectedScanNumber; }
            set
            {
                if (_selectedScanNumber == value) return;
                _selectedScanNumber = value;
                FragmentPlotModel.SetPointMarker(value);
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
                var precursorXics = new List<LabeledXic>();
                // get precursor xics
                foreach (var label in SelectedPrecursors)
                {
                    var ion = label.Ion;
                    LabeledXic lXic;
                    if (_precursorXicCache.ContainsKey(label.Composition)) lXic = _precursorXicCache[label.Composition];
                    else
                    {
                        var xic = Lcms.GetFullExtractedIonChromatogram(ion.GetIsotopeMz(label.Index),
                                                   IcParameters.Instance.PrecursorTolerancePpm);
                        lXic = (_precursorXicCache.ContainsKey(label.Composition)) ?
                                    _precursorXicCache[label.Composition] :
                                    new LabeledXic(label.Composition, label.Index, xic, label.IonType, label.IsFragmentIon);   
                    }
                    precursorXics.Add(lXic);
                }
                _selectedPrecursorXics = precursorXics;
                PrecursorUpdate();
                OnPropertyChanged("SelectedPrecursors");
            }
        }

        public List<LabeledIon> SelectedFragments
        {
            get { return _selectedFragments; }
            set
            {
                _selectedFragments = value;
                var fragmentXics = new List<LabeledXic>();
                // get fragment xics
                foreach (var label in SelectedFragments)
                {
                    var ion = label.Ion;
                    LabeledXic lXic;
                    if (_fragmentXicCache.ContainsKey(label.Composition)) lXic = _fragmentXicCache[label.Composition];
                    else
                    {
                        var xic = Lcms.GetFullFragmentExtractedIonChromatogram(ion.GetMostAbundantIsotopeMz(),
                                                       IcParameters.Instance.ProductIonTolerancePpm,
                                                       label.PrecursorIon.GetMostAbundantIsotopeMz());
                        lXic = new LabeledXic(label.Composition, label.Index, xic, label.IonType, label.IsFragmentIon);
                    }
                    fragmentXics.Add(lXic);
                }
                _selectedFragmentXics = fragmentXics;
                FragmentUpdate();
                OnPropertyChanged("SelectedFragments");
            }
        }

        public bool ShowScanMarkers
        {
            get { return _showScanMarkers; }
            set
            {
                _showScanMarkers = value;
                FragmentUpdate();
                PrecursorUpdate();
                FragmentPlotModel.SetPointMarker(SelectedScanNumber); // preserve scan marker
                OnPropertyChanged("ShowScanMarkers");
            }
        }

        private LinearAxis XicXAxis
        {
            get
            {
                if (_xicXAxis == null)
                {
                    _xicXAxis = new LinearAxis(AxisPosition.Bottom, "Scan #")
                    {
                        Maximum = Lcms.MaxLcScan,
                        Minimum = 0,
                        AbsoluteMinimum = 0,
                        AbsoluteMaximum = Lcms.MaxLcScan
                    };
                    _xicXAxis.Zoom(0, Lcms.MaxLcScan);
                }
                return _xicXAxis;
            }
        }

        public void ZoomToScan(int scanNumber)
        {
            int minX, maxX;
            CalculateBounds(out minX, out maxX);
            XicXAxis.Minimum = minX;
            XicXAxis.Maximum = maxX;
            XicXAxis.Zoom(minX, maxX);
            SelectedScanNumber = scanNumber;
        }

        public void Reset()
        {
            _fragmentXicCache.Clear();
            _precursorXicCache.Clear();
        }

        private void FragmentUpdate()
        {
            if (_selectedFragmentXics.Count == 0) return;
            var fragmentPlotModel = new XicPlotModel("Fragment Ion XIC", XicXAxis,
                                                        _selectedFragmentXics, Colors, ShowScanMarkers);
            fragmentPlotModel.SetPointMarker(SelectedScanNumber);   // preserve marker
            FragmentPlotModel = fragmentPlotModel;
        }

        private void PrecursorUpdate()
        {
            if (_selectedPrecursorXics.Count == 0) return;
            var precursorPlotModel = new XicPlotModel("Precursor Ion XIC", XicXAxis,
                                                        _selectedPrecursorXics, Colors, ShowScanMarkers);
            PrecursorPlotModel = precursorPlotModel;
        }

        private void SelectFragmentScanNumber()
        {
            var x = FragmentPlotModel.SelectedDataPoint.X;
            SelectedScanNumber = (int) Math.Round(x);
            SelectedScanNumberChanged(this, new PrSmChangedEventArgs(CreatePrSm(SelectedScanNumber)));
        }

        private void CalculateBounds(out int minS, out int maxS)
        {
            minS = SelectedScanNumber - 1000;
            maxS = SelectedScanNumber + 1000;
            if (SelectedScanNumber < 1000) minS = 0;
            if (SelectedScanNumber == 0) maxS = Lcms.MaxLcScan;   
        }

        private PrSm CreatePrSm(int scanNum)
        {
            var selectedScanNumber = scanNum;
            var newPrsm = new PrSm
            {
                Lcms = Lcms,
                Scan = selectedScanNumber,
            };
            return newPrsm;
        }

        private List<LabeledIon> _selectedFragments;
        private List<LabeledXic> _selectedFragmentXics;
        private List<LabeledXic> _selectedPrecursorXics; 

        private LinearAxis _xicXAxis;
        
        private bool _showScanMarkers;
        private List<LabeledIon> _selectedPrecursors;
        private int _selectedScanNumber;
        private XicPlotModel _fragmentPlotModel;
        private XicPlotModel _precursorPlotModel;
        private readonly Dictionary<Composition, LabeledXic> _precursorXicCache;
        private readonly Dictionary<Composition, LabeledXic> _fragmentXicCache;
    }
}
