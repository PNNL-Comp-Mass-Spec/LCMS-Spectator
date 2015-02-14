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
        public SpectrumViewModel(IDialogService dialogService, ILcMsRun lcms)
        {
            _lcms = lcms;
            PrimarySpectrumViewModel = new SpectrumPlotViewModel(dialogService, 1.05);
            Secondary1ViewModel = new SpectrumPlotViewModel(dialogService, 1.1, false);
            Secondary2ViewModel = new SpectrumPlotViewModel(dialogService, 1.1, false);

            _isAxisInternalChange = false;
            Secondary1ViewModel.XAxis.AxisChanged += (o, e) =>
            {
                if (_isAxisInternalChange) return;
                _isAxisInternalChange = true;
                Secondary2ViewModel.XAxis.Zoom(Secondary1ViewModel.XAxis.ActualMinimum, Secondary1ViewModel.XAxis.ActualMaximum);
                _isAxisInternalChange = false;
            };

            Secondary2ViewModel.XAxis.AxisChanged += (o, e) =>
            {
                if (_isAxisInternalChange) return;
                _isAxisInternalChange = true;
                Secondary1ViewModel.XAxis.Zoom(Secondary2ViewModel.XAxis.ActualMinimum, Secondary2ViewModel.XAxis.ActualMaximum);
                _isAxisInternalChange = false;
            };

            var swapSecondary1Command = ReactiveCommand.Create();
            swapSecondary1Command.Subscribe(_ => SwapSecondary1());
            SwapSecondary1Command = swapSecondary1Command;

            var swapSecondary2Command = ReactiveCommand.Create();
            swapSecondary2Command.Subscribe(_ => SwapSecondary2());
            SwapSecondary2Command = swapSecondary2Command;
        }

        #region Public Properties
        public IReactiveCommand SwapSecondary1Command { get; private set; }
        public IReactiveCommand SwapSecondary2Command { get; private set; }

        private SpectrumPlotViewModel _primarySpectrumViewModel ;
        public SpectrumPlotViewModel PrimarySpectrumViewModel
        {
            get { return _primarySpectrumViewModel; }
            private set { this.RaiseAndSetIfChanged(ref _primarySpectrumViewModel, value); }
        }

        private SpectrumPlotViewModel _secondary1ViewModel;
        public SpectrumPlotViewModel Secondary1ViewModel
        {
            get { return _secondary1ViewModel; }
            private set { this.RaiseAndSetIfChanged(ref _secondary1ViewModel, value); }
        }

        private SpectrumPlotViewModel _secondary2ViewModel;
        public SpectrumPlotViewModel Secondary2ViewModel
        {
            get { return _secondary2ViewModel; }
            private set { this.RaiseAndSetIfChanged(ref _secondary2ViewModel, value); }
        }
        #endregion

        #region Public Methods
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
                primaryTitle = "MS/MS Spectrum";
                secondary1Title = "Previous Ms1 Spectrum";
                secondary2Title = "Next Ms1 Spectrum";
                secondary1 = _lcms.GetSpectrum(_lcms.GetPrevScanNum(scan, 1));
                secondary2 = _lcms.GetSpectrum(_lcms.GetNextScanNum(scan, 1));
            }
            else
            {
                primary = FindNearestMs2Spectrum(scan, precursorMz, _lcms);
                if (primary == null) return;
                if (primary.ScanNum < scan)
                {
                    primaryTitle = "Previous MS1 Spectrum";
                    secondary1Title = "Previous Ms1 Spectrum";
                    secondary2Title = "Ms1 Spectrum";
                    secondary1 = _lcms.GetSpectrum(_lcms.GetPrevScanNum(primary.ScanNum, 1));
                    secondary2 = _lcms.GetSpectrum(scan);
                }
                else
                {
                    primaryTitle = "Next MS/MS Spectrum";
                    secondary1Title = "MS1 Spectrum";
                    secondary2Title = "Next MS1 Spectrum";
                    secondary1 = _lcms.GetSpectrum(scan);
                    secondary2 = _lcms.GetSpectrum(_lcms.GetNextScanNum(primary.ScanNum, 1));
                }
            }

            // Ms2 spectrum plot
            PrimarySpectrumViewModel.Title = String.Format("{0} (Scan: {1})", primaryTitle, primary.ScanNum);
            PrimarySpectrumViewModel.Spectrum = primary;
            // Ms1 spectrum plots
            // previous Ms1
            SetMs1XAxis(Secondary1ViewModel.XAxis, primary, secondary1);
            Secondary1ViewModel.Spectrum = secondary1;
            Secondary1ViewModel.Title = secondary1 == null ? "" : String.Format("{0} (Scan: {1})", secondary1Title, secondary1.ScanNum);
            // next Ms1
            SetMs1XAxis(Secondary2ViewModel.XAxis, primary, secondary1);
            Secondary2ViewModel.Spectrum = secondary2;
            Secondary2ViewModel.Title = secondary2 == null ? "" : String.Format("{0} (Scan: {1})", secondary2Title, secondary2.ScanNum);
        }

        public void SwapSecondary1()
        {
            var primary = PrimarySpectrumViewModel;
            var secondary = Secondary1ViewModel;
            PrimarySpectrumViewModel = null;
            Secondary1ViewModel = null;
            PrimarySpectrumViewModel = secondary;
            Secondary1ViewModel = primary;
        }

        public void SwapSecondary2()
        {
            var primary = PrimarySpectrumViewModel;
            var secondary = Secondary2ViewModel;
            PrimarySpectrumViewModel = null;
            Secondary2ViewModel = null;
            PrimarySpectrumViewModel = secondary;
            Secondary2ViewModel = primary;
        }
        #endregion

        #region Private Methods

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


        private ProductSpectrum FindNearestMs2Spectrum(int ms1Scan, double precursorMz, ILcMsRun lcms)
        {
            if (precursorMz.Equals(0)) return null;

            int highScan = ms1Scan;
            ProductSpectrum highSpec = null;
            bool found = false;
            double highDist = 0.0;
            while (!found)
            {
                highScan = lcms.GetNextScanNum(highScan, 2);
                if (highScan == lcms.MaxLcScan + 1)
                {
                    highDist = Double.PositiveInfinity;
                    break;
                }
                var spectrum = lcms.GetSpectrum(highScan);
                var prodSpectrum = spectrum as ProductSpectrum;
                if (prodSpectrum == null) break;
                if (prodSpectrum.IsolationWindow.Contains(precursorMz))
                {
                    highSpec = prodSpectrum;
                    found = true;
                }
                highDist++;
            }

            ProductSpectrum lowSpec = null;
            int lowScan = ms1Scan;
            found = false;
            double lowDist = 0.0;
            while (!found)
            {
                lowScan = lcms.GetPrevScanNum(lowScan, 2);
                if (lowScan == lcms.MinLcScan - 1)
                {
                    lowDist = Double.PositiveInfinity;
                    break;
                }
                var spectrum = lcms.GetSpectrum(lowScan);
                var prodSpectrum = spectrum as ProductSpectrum;
                if (prodSpectrum == null) break;
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
        #endregion

        private readonly ILcMsRun _lcms;
        private bool _isAxisInternalChange;
    }
}
