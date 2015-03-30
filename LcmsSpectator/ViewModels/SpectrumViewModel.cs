using System;
using System.Linq;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.DialogServices;
using ReactiveUI;
using LinearAxis = OxyPlot.Axes.LinearAxis;

namespace LcmsSpectator.ViewModels
{
    public class SpectrumViewModel: ReactiveObject
    {
        public SpectrumViewModel(IMainDialogService dialogService, ILcMsRun lcms)
        {
            _lcms = lcms;
            Ms2SpectrumViewModel = new SpectrumPlotViewModel(dialogService, 1.05);
            PrevMs1SpectrumViewModel = new SpectrumPlotViewModel(dialogService, 1.1, false);
            NextMs1SpectrumViewModel = new SpectrumPlotViewModel(dialogService, 1.1, false);

            // When prev ms1 spectrum plot is zoomed/panned, next ms1 spectrum plot should zoom/pan
            _isAxisInternalChange = false;
            PrevMs1SpectrumViewModel.XAxis.AxisChanged += (o, e) =>
            {
                if (_isAxisInternalChange) return;
                _isAxisInternalChange = true;
                NextMs1SpectrumViewModel.XAxis.Zoom(PrevMs1SpectrumViewModel.XAxis.ActualMinimum, PrevMs1SpectrumViewModel.XAxis.ActualMaximum);
                _isAxisInternalChange = false;
            };

            // When next ms1 spectrum plot is zoomed/panned, prev ms1 spectrum plot should zoom/pan
            NextMs1SpectrumViewModel.XAxis.AxisChanged += (o, e) =>
            {
                if (_isAxisInternalChange) return;
                _isAxisInternalChange = true;
                PrevMs1SpectrumViewModel.XAxis.Zoom(NextMs1SpectrumViewModel.XAxis.ActualMinimum, NextMs1SpectrumViewModel.XAxis.ActualMaximum);
                _isAxisInternalChange = false;
            };

            // By default, MS2 Spectrum is shown in the primary view
            PrimarySpectrumViewModel = Ms2SpectrumViewModel;
            Secondary1ViewModel = PrevMs1SpectrumViewModel;
            Secondary2ViewModel = NextMs1SpectrumViewModel;

            // Wire commands to swap the spectrum that is shown in the primary view

            var swapSecondary1Command = ReactiveCommand.Create();
            swapSecondary1Command.Subscribe(_ => SwapSecondary1());
            SwapSecondary1Command = swapSecondary1Command;

            var swapSecondary2Command = ReactiveCommand.Create();
            swapSecondary2Command.Subscribe(_ => SwapSecondary2());
            SwapSecondary2Command = swapSecondary2Command;
        }

        // Commands
        public IReactiveCommand SwapSecondary1Command { get; private set; }
        public IReactiveCommand SwapSecondary2Command { get; private set; }

        private SpectrumPlotViewModel _ms2SpectrumViewModel;
        /// <summary>
        /// Spectrum plot view model for MS/MS spectrum
        /// </summary>
        public SpectrumPlotViewModel Ms2SpectrumViewModel
        {
            get { return _ms2SpectrumViewModel; }
            set { this.RaiseAndSetIfChanged(ref _ms2SpectrumViewModel, value); }
        }

        private SpectrumPlotViewModel _prevMs1SpectrumViewModel;
        /// <summary>
        /// Spectrum plot view model for previous MS1 spectrum.
        /// </summary>
        public SpectrumPlotViewModel PrevMs1SpectrumViewModel
        {
            get { return _prevMs1SpectrumViewModel; }
            set { this.RaiseAndSetIfChanged(ref _prevMs1SpectrumViewModel, value); }
        }

        private SpectrumPlotViewModel _nextMs1SpectrumViewModel;
        /// <summary>
        /// Spectrum plot view model for next MS1 spectrum.
        /// </summary>
        public SpectrumPlotViewModel NextMs1SpectrumViewModel
        {
            get { return _nextMs1SpectrumViewModel; }
            set { this.RaiseAndSetIfChanged(ref _nextMs1SpectrumViewModel, value); }
        }

        private SpectrumPlotViewModel _primarySpectrumViewModel;
        /// <summary>
        /// Spectrum shown in the primary spectrum plot view
        /// </summary>
        public SpectrumPlotViewModel PrimarySpectrumViewModel
        {
            get { return _primarySpectrumViewModel; }
            private set { this.RaiseAndSetIfChanged(ref _primarySpectrumViewModel, value); }
        }

        private SpectrumPlotViewModel _secondary1ViewModel;
        /// <summary>
        /// Spectrum shown in the first secondary plot view (top by default)
        /// </summary>
        public SpectrumPlotViewModel Secondary1ViewModel
        {
            get { return _secondary1ViewModel; }
            private set { this.RaiseAndSetIfChanged(ref _secondary1ViewModel, value); }
        }

        private SpectrumPlotViewModel _secondary2ViewModel;
        /// <summary>
        /// Spectrum shown in the first secondary plot view (bottom by default)
        /// </summary>
        public SpectrumPlotViewModel Secondary2ViewModel
        {
            get { return _secondary2ViewModel; }
            private set { this.RaiseAndSetIfChanged(ref _secondary2ViewModel, value); }
        }

        public void UpdateSpectra(int scan, double precursorMz = 0)
        {
            if (scan == 0 || _lcms == null) return;
            var primary = _lcms.GetSpectrum(scan);

            string primaryTitle;
            string secondary1Title;
            string secondary2Title;

            Spectrum secondary1;
            Spectrum secondary2;

            if (primary is ProductSpectrum)
            {
                // The primary spectrum we want to show is an MS/MS spectrum
                primaryTitle = "MS/MS Spectrum";
                secondary1Title = "Previous Ms1 Spectrum";
                secondary2Title = "Next Ms1 Spectrum";
                secondary1 = _lcms.GetSpectrum(_lcms.GetPrevScanNum(scan, 1));
                secondary2 = _lcms.GetSpectrum(_lcms.GetNextScanNum(scan, 1));
            }
            else
            {
                // The primary spectrum that we want to show is a MS1 spectrum
                if (_lcms != null) primary = FindNearestMs2Spectrum(scan, precursorMz);
                if (primary == null) return;
                if (primary.ScanNum < scan)
                {
                    // The primary spectrum scan is above the ms/ms spectrum scan that we selected
                    primaryTitle = "Previous MS1 Spectrum";
                    secondary1Title = "Previous Ms1 Spectrum";
                    secondary2Title = "Ms1 Spectrum";
                    secondary1 = _lcms.GetSpectrum(_lcms.GetPrevScanNum(primary.ScanNum, 1));
                    secondary2 = _lcms.GetSpectrum(scan);
                }
                else
                {
                    // The primary spectrum scan is below the ms/ms spectrum scan that we selected
                    primaryTitle = "Next MS/MS Spectrum";
                    secondary1Title = "MS1 Spectrum";
                    secondary2Title = "Next MS1 Spectrum";
                    secondary1 = _lcms.GetSpectrum(scan);
                    secondary2 = _lcms.GetSpectrum(_lcms.GetNextScanNum(primary.ScanNum, 1));
                }
            }

            // Ms2 spectrum plot
            Ms2SpectrumViewModel.Title = String.Format("{0} (Scan: {1})", primaryTitle, primary.ScanNum);
            Ms2SpectrumViewModel.Spectrum = primary;
            // Ms1 spectrum plots
            // previous Ms1
            SetMs1XAxis(PrevMs1SpectrumViewModel.XAxis, primary, secondary1);
            PrevMs1SpectrumViewModel.Spectrum = secondary1;
            PrevMs1SpectrumViewModel.Title = secondary1 == null ? "" : String.Format("{0} (Scan: {1})", secondary1Title, secondary1.ScanNum);
            // next Ms1
            SetMs1XAxis(Secondary2ViewModel.XAxis, primary, secondary1);
            NextMs1SpectrumViewModel.Spectrum = secondary2;
            NextMs1SpectrumViewModel.Title = secondary2 == null ? "" : String.Format("{0} (Scan: {1})", secondary2Title, secondary2.ScanNum);
        }

        /// <summary>
        /// Swap the spectrum that is shown in the primary view with spectrum shown in the
        /// first secondary view
        /// </summary>
        public void SwapSecondary1()
        {
            var primary = PrimarySpectrumViewModel;
            var secondary = Secondary1ViewModel;
            PrimarySpectrumViewModel = null;
            Secondary1ViewModel = null;
            PrimarySpectrumViewModel = secondary;
            Secondary1ViewModel = primary;
        }

        /// <summary>
        /// Swap the spectrum that is shown in the primary view with spectrum shown in the
        /// second secondary view
        /// </summary>
        public void SwapSecondary2()
        {
            var primary = PrimarySpectrumViewModel;
            var secondary = Secondary2ViewModel;
            PrimarySpectrumViewModel = null;
            Secondary2ViewModel = null;
            PrimarySpectrumViewModel = secondary;
            Secondary2ViewModel = primary;
        }

        /// <summary>
        /// Set Shared XAxis for Ms1 spectra plots
        /// </summary>
        /// <param name="xAxis"></param>
        /// <param name="ms2">Ms2 Spectrum to get Isoloation Window bounds from.</param>
        /// <param name="ms1"></param>
        /// <returns>XAxis</returns>
        private void SetMs1XAxis(LinearAxis xAxis, Spectrum ms2, Spectrum ms1)
        {
            var ms2Prod = ms2 as ProductSpectrum;
            if (ms2Prod == null || ms1 == null) return;
            var ms1AbsMax = ms1.Peaks.Max().Mz;
            var ms1Min = ms2Prod.IsolationWindow.MinMz;
            var ms1Max = ms2Prod.IsolationWindow.MaxMz;
            var diff = ms1Max - ms1Min;
            var ms1MinMz = ms2Prod.IsolationWindow.MinMz - 0.25*diff;
            var ms1MaxMz = ms2Prod.IsolationWindow.MaxMz + 0.25*diff;
            xAxis.Minimum = ms1MinMz;
            xAxis.Maximum = ms1MaxMz;
            xAxis.AbsoluteMinimum = 0;
            xAxis.AbsoluteMaximum = ms1AbsMax;
            xAxis.Zoom(ms1MinMz, ms1MaxMz);
        }

        /// <summary>
        /// Attempt to find the nearest ms2 spectrum given an ms1 spectrum.
        /// </summary>
        /// <param name="ms1Scan">Ms1 scan number</param>
        /// <param name="precursorMz">Precursor M/Z that should be in ms2's isolation window range.</param>
        /// <returns>Product spectrum for the nearest Ms2 spectrum. Returns null if one cannot be found.</returns>
        private ProductSpectrum FindNearestMs2Spectrum(int ms1Scan, double precursorMz)
        {
            // Do not have a valid PrecursorMz, so we're not going to find an ms2 spectrum.
            if (precursorMz.Equals(0)) return null;
            // down
            int highScan = ms1Scan;
            ProductSpectrum highSpec = null;
            bool found = false;
            double highDist = 0.0;
            while (!found)
            {
                // look for spectrum in the lower part of the scan range
                highScan = _lcms.GetNextScanNum(highScan, 2);
                if (highScan == _lcms.MaxLcScan + 1)
                {
                    highDist = Double.PositiveInfinity;
                    break;
                }
                var spectrum = _lcms.GetSpectrum(highScan);
                var prodSpectrum = spectrum as ProductSpectrum;
                if (prodSpectrum == null) break; // Found another ms1, so stop looking
                if (prodSpectrum.IsolationWindow.Contains(precursorMz))
                {
                    highSpec = prodSpectrum;
                    found = true;
                }
                highDist++;
            }
            // up
            ProductSpectrum lowSpec = null;
            int lowScan = ms1Scan;
            found = false;
            double lowDist = 0.0;
            while (!found)
            {
                // look for spectrum in the higher part of the scan range
                lowScan = _lcms.GetPrevScanNum(lowScan, 2);
                if (lowScan == _lcms.MinLcScan - 1)
                {
                    lowDist = Double.PositiveInfinity;
                    break;
                }
                var spectrum = _lcms.GetSpectrum(lowScan);
                var prodSpectrum = spectrum as ProductSpectrum;
                if (prodSpectrum == null) break; // Found another ms1, so stop looking
                if (prodSpectrum.IsolationWindow.Contains(precursorMz))
                {
                    lowSpec = prodSpectrum;
                    found = true;
                }
                lowDist++;
            }

            ProductSpectrum nextMs2;
            if (highDist <= lowDist && highSpec != null) nextMs2 = highSpec;
            else nextMs2 = lowSpec;
            return nextMs2;
        }

        private readonly ILcMsRun _lcms;
        private bool _isAxisInternalChange;
    }
}
