using System;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.DialogServices;
using LcmsSpectator.PlotModels;
using LcmsSpectatorModels.Models;
using OxyPlot.Axes;
using LinearAxis = OxyPlot.Axes.LinearAxis;

namespace LcmsSpectator.ViewModels
{
    public class SpectrumViewModel: ViewModelBase
    {
        public SpectrumPlotViewModel Ms2SpectrumViewModel { get; private set; }
        public SpectrumPlotViewModel PreviousMs1ViewModel { get; private set; }
        public SpectrumPlotViewModel NextMs1ViewModel { get; private set; }
        public string RawFileName { get; set; }
        public SpectrumViewModel(IDialogService dialogService, ColorDictionary colors)
        {
            Ms2SpectrumViewModel = new SpectrumPlotViewModel(dialogService, 1.05, colors);
            PreviousMs1ViewModel = new SpectrumPlotViewModel(dialogService, 1.1, colors);
            NextMs1ViewModel = new SpectrumPlotViewModel(dialogService, 1.1, colors);
            Messenger.Default.Register<XicPlotViewModel.SelectedScanChangedMessage>(this, SelectedScanChanged);
        }

        /// <summary>
        /// Update Spectrum plots.
        /// </summary>
        /// <param name="ms2">New Ms2 Spectrum.</param>
        /// <param name="productIons">Ms2 fragment ions to highlight.</param>
        /// <param name="prevms1">New Previous Ms1 Spectrum.</param>
        /// <param name="nextms1">New Next Ms1 Spectrum</param>
        /// <param name="precursorIon">Precursor ion to highlight on all plots.</param>
        /// <param name="heavy">Is the spectrum from a heavy peptide?</param>
        public void UpdatePlots(Spectrum ms2, List<LabeledIon> productIons, Spectrum prevms1, Spectrum nextms1, LabeledIon precursorIon, bool heavy=false)
        {
            productIons = new List<LabeledIon>(productIons) {precursorIon};
            // Ms2 spectrum plot
            var heavyStr = heavy ? ", Heavy" : "";
            Ms2SpectrumViewModel.Title = (ms2 == null) ? "" : String.Format("Ms2 Spectrum (Scan: {0}, Raw: {1}{2})", ms2.ScanNum, RawFileName, heavyStr);
            Ms2SpectrumViewModel.Spectrum = ms2;
            Ms2SpectrumViewModel.Ions = productIons;
            Ms2SpectrumViewModel.Update();
            // Ms1 spectrum plots
            var xAxis = GenerateMs1XAxis(ms2, prevms1, nextms1);    // shared x axis
            // previous Ms1
            PreviousMs1ViewModel.XAxis = xAxis;
            PreviousMs1ViewModel.Title = prevms1 == null ? "" : String.Format("Previous Ms1 Spectrum (Scan: {0})", prevms1.ScanNum);
            PreviousMs1ViewModel.Spectrum = prevms1;
            PreviousMs1ViewModel.Ions = new List<LabeledIon> {precursorIon};
            PreviousMs1ViewModel.Update();
            // next Ms1
            NextMs1ViewModel.XAxis = xAxis;
            NextMs1ViewModel.Title = nextms1 == null ? "" : String.Format("Next Ms1 Spectrum (Scan: {0})", nextms1.ScanNum);
            NextMs1ViewModel.Spectrum = nextms1;
            NextMs1ViewModel.Ions = new List<LabeledIon> {precursorIon};
            NextMs1ViewModel.Update();
        }

        public void ClearPlots()
        {
            Ms2SpectrumViewModel.Clear();
            NextMs1ViewModel.Clear();
            PreviousMs1ViewModel.Clear();
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
            var ms2Prod = ms2 as ProductSpectrum;
            if (ms2Prod == null || prevms1 == null || nextms1 == null) return new LinearAxis {Minimum = 0, Maximum = 100};
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

        private void SelectedScanChanged(XicPlotViewModel.SelectedScanChangedMessage message)
        {
            var sender = message.Sender as XicPlotViewModel;
            if (sender != null)
            {
                var scan = message.Scan;
                var lcms = sender.Lcms;
                var ms2 = lcms.GetSpectrum(scan);
                var prevms1 = lcms.GetSpectrum(lcms.GetPrevScanNum(scan, 1));
                var nextms1 = lcms.GetSpectrum(lcms.GetNextScanNum(scan, 1));

                // Ms2 spectrum plot
                var heavyStr = sender.Heavy ? ", Heavy" : "";
                Ms2SpectrumViewModel.Title = (ms2 == null) ? "" : String.Format("Ms2 Spectrum (Scan: {0}, Raw: {1}{2})", ms2.ScanNum, RawFileName, heavyStr);
                Ms2SpectrumViewModel.Spectrum = ms2;
                Ms2SpectrumViewModel.Update();
                // Ms1 spectrum plots
                var xAxis = GenerateMs1XAxis(ms2, prevms1, nextms1);    // shared x axis
                // previous Ms1
                PreviousMs1ViewModel.XAxis = xAxis;
                PreviousMs1ViewModel.Title = prevms1 == null ? "" : String.Format("Previous Ms1 Spectrum (Scan: {0})", prevms1.ScanNum);
                PreviousMs1ViewModel.Spectrum = prevms1;
                PreviousMs1ViewModel.Update();
                // next Ms1
                NextMs1ViewModel.XAxis = xAxis;
                NextMs1ViewModel.Title = nextms1 == null ? "" : String.Format("Next Ms1 Spectrum (Scan: {0})", nextms1.ScanNum);
                NextMs1ViewModel.Spectrum = nextms1;
                NextMs1ViewModel.Update();
            }
        }
    }
}
