using System;
using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.PlotModels;
using LcmsSpectatorModels.Models;
using OxyPlot.Axes;

namespace LcmsSpectator.ViewModels
{
    public class SpectrumViewModel: ViewModelBase
    {
        public SpectrumPlotViewModel Ms2SpectrumViewModel { get; set; }
        public SpectrumPlotViewModel PreviousMs1ViewModel { get; set; }
        public SpectrumPlotViewModel NextMs1ViewModel { get; set; }

        public SpectrumViewModel(ColorDictionary colors)
        {
            Ms2SpectrumViewModel = new SpectrumPlotViewModel(1.05, colors);
            PreviousMs1ViewModel = new SpectrumPlotViewModel(1.1, colors);
            NextMs1ViewModel = new SpectrumPlotViewModel(1.1, colors);
        }

        /// <summary>
        /// Ms2 Spectrum.
        /// </summary>
        public Spectrum Ms2Spectrum
        {
            get { return _ms2Spectrum; }
            set
            {
                if (_ms2Spectrum == value) return;
                _ms2Spectrum = value;
                Ms2SpectrumViewModel.Spectrum = _ms2Spectrum;
                Ms2SpectrumViewModel.Title = String.Format("Ms2 Spectrum (Scan: {0})", _ms2Spectrum.ScanNum);
                OnPropertyChanged("Ms2Spectrum");
            }
        }

        /// <summary>
        /// Ms2 fragment ions to highlight
        /// </summary>
        public List<LabeledIon> ProductIons
        {
            get { return _productIons; }
            set
            {
                _productIons = value;
                Ms2SpectrumViewModel.Ions = _productIons;
                Ms2SpectrumViewModel.AddIonHighlight(PrecursorIon);
                OnPropertyChanged("ProductIons");
            }
        }

        /// <summary>
        /// Closest Ms1 Spectrum before Ms2 Spectrum.
        /// </summary>
        public Spectrum PreviousMs1Spectrum
        {
            get { return _previousMs1Spectrum; }
            set
            {
                if (_previousMs1Spectrum == value) return;
                _previousMs1Spectrum = value;
                PreviousMs1ViewModel.Spectrum = _previousMs1Spectrum;
                PreviousMs1ViewModel.Title = String.Format("Previous Ms1 Spectrum (Scan: {0})", _previousMs1Spectrum.ScanNum);
                OnPropertyChanged("PreviousMs1Spectrum");
            }
        }

        /// <summary>
        /// Closest Ms1 Spectrum after Ms2 Spectrum.
        /// </summary>
        public Spectrum NextMs1Spectrum
        {
            get { return _nextMs1Spectrum; }
            set
            {
                if (_nextMs1Spectrum == value) return;
                _nextMs1Spectrum = value;
                NextMs1ViewModel.Spectrum = _nextMs1Spectrum;
                NextMs1ViewModel.Title = String.Format("Next Ms1 Spectrum (Scan: {0})", _nextMs1Spectrum.ScanNum);
                OnPropertyChanged("NextMs1Spectrum");
            }
        }

        /// <summary>
        /// Precursor ion to highlight on Ms1 plots.
        /// </summary>
        public LabeledIon PrecursorIon
        {
            get { return _precursorIon; }
            set
            {
                if (_precursorIon == value) return;
                _precursorIon = value;
                var ionList = new List<LabeledIon> { _precursorIon };
                PreviousMs1ViewModel.Ions = ionList;
                NextMs1ViewModel.Ions = ionList;
                OnPropertyChanged("PrecursorIon");
            }
        }

        /// <summary>
        /// Update Spectrum plots.
        /// </summary>
        /// <param name="ms2">New Ms2 Spectrum.</param>
        /// <param name="productIons">Ms2 fragment ions to highlight.</param>
        /// <param name="prevms1">New Previous Ms1 Spectrum.</param>
        /// <param name="nextms1">New Next Ms1 Spectrum</param>
        /// <param name="precursorIon">Precursor ion to highlight on all plots.</param>
        public void UpdatePlots(Spectrum ms2, List<LabeledIon> productIons, Spectrum prevms1, Spectrum nextms1, LabeledIon precursorIon)
        {
            PrecursorIon = precursorIon;
            Ms2Spectrum = ms2;
            ProductIons = productIons;
            var xAxis = GenerateMs1XAxis(ms2, prevms1, nextms1);
            PreviousMs1ViewModel.XAxis = xAxis;
            NextMs1ViewModel.XAxis = xAxis;
            PreviousMs1Spectrum = prevms1;
            NextMs1Spectrum = nextms1;
        }

        /// <summary>
        /// Generate Shared XAxis for Ms1 spectra plots
        /// </summary>
        /// <param name="ms2">Ms2 Spectrum to get Isoloation Window bounds from.</param>
        /// <param name="prevms1">Closest Ms1 Spectrum before Ms2 Spectrum.</param>
        /// <param name="nextms1">Closest Ms1 Spectrum after Ms2 Spectrum.</param>
        /// <returns>XAxis</returns>
        private LinearAxis GenerateMs1XAxis(Spectrum ms2, Spectrum prevms1, Spectrum nextms1)
        {
            var ms2Prod = (ProductSpectrum) ms2 ;
            var prevms1AbsMax = prevms1.Peaks.Max().Mz;
            var nextms1AbsMax = nextms1.Peaks.Max().Mz;
            var ms1AbsoluteMaximum = (prevms1AbsMax >= nextms1AbsMax) ? prevms1AbsMax : nextms1AbsMax;
            var ms1Min = ms2Prod.IsolationWindow.MinMz;
            var ms1Max = ms2Prod.IsolationWindow.MaxMz;
            var diff = ms1Max - ms1Min;
            var ms1MinMz = ms2Prod.IsolationWindow.MinMz - 0.25*diff;
            var ms1MaxMz = ms2Prod.IsolationWindow.MaxMz + 0.25*diff;
            var xAxis = new LinearAxis(AxisPosition.Bottom, "M/Z")
            {
                Minimum = ms1MinMz,
                Maximum = ms1MaxMz,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = ms1AbsoluteMaximum * 1.25
            };
            xAxis.Zoom(ms1MinMz, ms1MaxMz);
            return xAxis;
        }

        private Spectrum _ms2Spectrum;
        private List<LabeledIon> _productIons;
        private Spectrum _previousMs1Spectrum;
        private Spectrum _nextMs1Spectrum;
        private LabeledIon _precursorIon;
    }
}
