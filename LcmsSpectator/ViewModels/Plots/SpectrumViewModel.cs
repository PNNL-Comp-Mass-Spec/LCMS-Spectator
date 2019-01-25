// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SpectrumViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for displaying MS and MS/MS spectrum plots.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.Config;
using LcmsSpectator.DialogServices;
using LcmsSpectator.Models;
using LcmsSpectator.ViewModels.Data;
using OxyPlot.Axes;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.Plots
{
    /// <summary>
    /// View model for displaying MS and MS/MS spectrum plots.
    /// </summary>
    public class SpectrumViewModel : ReactiveObject
    {
        /// <summary>
        /// LCMSRun for the data set that the spectrum plots are part of.
        /// </summary>
        private readonly ILcMsRun lcms;

        /// <summary>
        /// The spectrum plot view model for MS/MS spectrum.
        /// </summary>
        private SpectrumPlotViewModel ms2SpectrumViewModel;

        /// <summary>
        /// The spectrum plot view model for previous MS1 spectrum.
        /// </summary>
        private SpectrumPlotViewModel prevMs1SpectrumViewModel;

        /// <summary>
        /// The spectrum plot view model for next MS1 spectrum.
        /// </summary>
        private SpectrumPlotViewModel nextMs1SpectrumViewModel;

        /// <summary>
        /// The view model for the spectrum shown in the primary spectrum plot view
        /// </summary>
        private SpectrumPlotViewModel primarySpectrumViewModel;

        /// <summary>
        /// The view model for the spectrum shown in the first secondary spectrum plot view
        /// </summary>
        private SpectrumPlotViewModel secondary1ViewModel;

        /// <summary>
        /// The view model for the spectrum shown in the second secondary spectrum plot view
        /// </summary>
        private SpectrumPlotViewModel secondary2ViewModel;

        /// <summary>
        /// A value indicating whether or not the change to an axis was caused
        /// by synchronizing axes.
        /// </summary>
        private bool isAxisInternalChange;

        /// <summary>
        /// The fragmentation sequence (fragment/precursor ion generator)
        /// </summary>
        private FragmentationSequence fragmentationSequence;

        /// <summary>
        /// Default constructor to support WPF design-time use
        /// </summary>
        [Obsolete("For WPF Design-time use only.", true)]
        public SpectrumViewModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpectrumViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        /// <param name="lcms">LCMSRun for the data set that the spectrum plots are part of.</param>
        public SpectrumViewModel(IMainDialogService dialogService, ILcMsRun lcms)
        {
            this.lcms = lcms;
            Ms2SpectrumViewModel = new SpectrumPlotViewModel(dialogService, new FragmentationSequenceViewModel(), 1.15);

            PrevMs1SpectrumViewModel = new SpectrumPlotViewModel(
                dialogService,
                new PrecursorSequenceIonViewModel { PrecursorViewMode = PrecursorViewMode.Charges },
                1.25,
                false);

            NextMs1SpectrumViewModel = new SpectrumPlotViewModel(
                dialogService,
                new PrecursorSequenceIonViewModel { PrecursorViewMode = PrecursorViewMode.Charges },
                1.25,
                false);

            // When prev ms1 spectrum plot is zoomed/panned, next ms1 spectrum plot should zoom/pan
            isAxisInternalChange = false;
            PrevMs1SpectrumViewModel.XAxis.AxisChanged += (o, e) =>
                {
                    if (isAxisInternalChange)
                    {
                        return;
                    }

                    isAxisInternalChange = true;
                    NextMs1SpectrumViewModel.XAxis.Zoom(PrevMs1SpectrumViewModel.XAxis.ActualMinimum, PrevMs1SpectrumViewModel.XAxis.ActualMaximum);
                isAxisInternalChange = false;
            };

            // When next ms1 spectrum plot is zoomed/panned, prev ms1 spectrum plot should zoom/pan
            NextMs1SpectrumViewModel.XAxis.AxisChanged += (o, e) =>
                {
                    if (isAxisInternalChange)
                    {
                        return;
                    }

                    isAxisInternalChange = true;
                    PrevMs1SpectrumViewModel.XAxis.Zoom(NextMs1SpectrumViewModel.XAxis.ActualMinimum, NextMs1SpectrumViewModel.XAxis.ActualMaximum);
                isAxisInternalChange = false;
            };

            this.WhenAnyValue(x => x.FragmentationSequence)
                .Where(fragSeq => fragSeq != null)
                .Subscribe(fragSeq =>
                {
                    Ms2SpectrumViewModel.FragmentationSequenceViewModel.FragmentationSequence = fragSeq;
                    PrevMs1SpectrumViewModel.FragmentationSequenceViewModel.FragmentationSequence = fragSeq;
                    NextMs1SpectrumViewModel.FragmentationSequenceViewModel.FragmentationSequence = fragSeq;
                });

            // By default, MS2 Spectrum is shown in the primary view
            PrimarySpectrumViewModel = Ms2SpectrumViewModel;
            Secondary1ViewModel = PrevMs1SpectrumViewModel;
            Secondary2ViewModel = NextMs1SpectrumViewModel;

            // Wire commands to swap the spectrum that is shown in the primary view
            SwapSecondary1Command = ReactiveCommand.Create(SwapSecondary1);
            SwapSecondary2Command = ReactiveCommand.Create(SwapSecondary2);
        }

        /// <summary>
        /// Gets a command that swaps the first secondary view model with primary view model.
        /// </summary>
        public ReactiveCommand<Unit, Unit> SwapSecondary1Command { get; }

        /// <summary>
        /// Gets a command that swaps the second secondary view model with primary view model.
        /// </summary>
        public ReactiveCommand<Unit, Unit> SwapSecondary2Command { get; }

        /// <summary>
        /// Gets the spectrum plot view model for MS/MS spectrum.
        /// </summary>
        public SpectrumPlotViewModel Ms2SpectrumViewModel
        {
            get => ms2SpectrumViewModel;
            private set => this.RaiseAndSetIfChanged(ref ms2SpectrumViewModel, value);
        }

        /// <summary>
        /// Gets the spectrum plot view model for previous MS1 spectrum.
        /// </summary>
        public SpectrumPlotViewModel PrevMs1SpectrumViewModel
        {
            get => prevMs1SpectrumViewModel;
            private set => this.RaiseAndSetIfChanged(ref prevMs1SpectrumViewModel, value);
        }

        /// <summary>
        /// Gets the spectrum plot view model for next MS1 spectrum.
        /// </summary>
        public SpectrumPlotViewModel NextMs1SpectrumViewModel
        {
            get => nextMs1SpectrumViewModel;
            private set => this.RaiseAndSetIfChanged(ref nextMs1SpectrumViewModel, value);
        }

        /// <summary>
        /// Gets the view model for the spectrum shown in the primary spectrum plot view
        /// </summary>
        public SpectrumPlotViewModel PrimarySpectrumViewModel
        {
            get => primarySpectrumViewModel;
            private set => this.RaiseAndSetIfChanged(ref primarySpectrumViewModel, value);
        }

        /// <summary>
        /// Gets the view model for the spectrum shown in the first secondary spectrum plot view
        /// </summary>
        public SpectrumPlotViewModel Secondary1ViewModel
        {
            get => secondary1ViewModel;
            private set => this.RaiseAndSetIfChanged(ref secondary1ViewModel, value);
        }

        /// <summary>
        /// Gets the view model for the spectrum shown in the second secondary spectrum plot view
        /// </summary>
        public SpectrumPlotViewModel Secondary2ViewModel
        {
            get => secondary2ViewModel;
            private set => this.RaiseAndSetIfChanged(ref secondary2ViewModel, value);
        }

        /// <summary>
        /// Gets or sets the fragmentation sequence (fragment/precursor ion generator)
        /// </summary>
        public FragmentationSequence FragmentationSequence
        {
            get => fragmentationSequence;
            set => this.RaiseAndSetIfChanged(ref fragmentationSequence, value);
        }

        /// <summary>
        /// Update the spectrum plots to show a new set of spectra.
        /// </summary>
        /// <param name="scan">The scan number of the primary spectrum to display</param>
        /// <param name="precursorMz">The precursor M/Z of the ID displayed</param>
        public void UpdateSpectra(int scan, double precursorMz = 0)
        {
            if (scan == 0 || lcms == null)
            {
                return;
            }

            var primary = lcms.GetSpectrum(scan);

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
                secondary1 = lcms.GetSpectrum(lcms.GetPrevScanNum(scan, 1));
                secondary2 = lcms.GetSpectrum(lcms.GetNextScanNum(scan, 1));
            }
            else
            {
                // The primary spectrum that we want to show is a MS1 spectrum
                if (lcms != null)
                {
                    primary = FindNearestMs2Spectrum(scan, precursorMz);
                }

                if (primary == null)
                {
                    // no ms2 spectrum found
                    primary = lcms.GetSpectrum(scan);
                    primaryTitle = "MS1 Spectrum";
                    secondary1Title = string.Empty;
                    secondary2Title = string.Empty;
                    secondary1 = null;
                    secondary2 = null;
                }
                else if (primary.ScanNum < scan)
                {
                    // The primary spectrum scan is above the ms/ms spectrum scan that we selected
                    primaryTitle = "Previous MS/MS Spectrum";
                    secondary1Title = "Previous Ms1 Spectrum";
                    secondary2Title = "Ms1 Spectrum";
                    secondary1 = lcms.GetSpectrum(lcms.GetPrevScanNum(primary.ScanNum, 1));
                    secondary2 = lcms.GetSpectrum(scan);
                }
                else
                {
                    // The primary spectrum scan is below the ms/ms spectrum scan that we selected
                    primaryTitle = "Next MS/MS Spectrum";
                    secondary1Title = "MS1 Spectrum";
                    secondary2Title = "Next MS1 Spectrum";
                    secondary1 = lcms.GetSpectrum(scan);
                    secondary2 = lcms.GetSpectrum(lcms.GetNextScanNum(primary.ScanNum, 1));
                }
            }

            // Ms2 spectrum plot
            Ms2SpectrumViewModel.Title = string.Format("{0} (Scan: {1})", primaryTitle, primary.ScanNum);
            Ms2SpectrumViewModel.Spectrum = primary;

            // previous Ms1
            SetMs1XAxis(PrevMs1SpectrumViewModel.XAxis, primary, secondary1);
            PrevMs1SpectrumViewModel.Spectrum = secondary1;
            PrevMs1SpectrumViewModel.Title = secondary1 == null ? string.Empty : string.Format("{0} (Scan: {1})", secondary1Title, secondary1.ScanNum);

            // next Ms1
            SetMs1XAxis(Secondary2ViewModel.XAxis, primary, secondary1);
            NextMs1SpectrumViewModel.Spectrum = secondary2;
            NextMs1SpectrumViewModel.Title = secondary2 == null ? string.Empty : string.Format("{0} (Scan: {1})", secondary2Title, secondary2.ScanNum);
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
        /// Set minimum and maximum values for shared XAxis for MS1 spectra plots
        /// </summary>
        /// <param name="xaxis">X Axis to set min and max values for.</param>
        /// <param name="ms2">MS/MS spectrum to get isolation window bounds from.</param>
        /// <param name="ms1">The MS1 that the plot for this axis displays</param>
        private void SetMs1XAxis(Axis xaxis, Spectrum ms2, Spectrum ms1)
        {
            if (!(ms2 is ProductSpectrum ms2Prod) || ms1 == null)
            {
                return;
            }

            var ms1AbsMax = ms1.Peaks.Max().Mz;
            var ms1Min = ms2Prod.IsolationWindow.MinMz;
            var ms1Max = ms2Prod.IsolationWindow.MaxMz;
            var diff = ms1Max - ms1Min;
            var ms1MinMz = ms2Prod.IsolationWindow.MinMz - (0.25 * diff);
            var ms1MaxMz = ms2Prod.IsolationWindow.MaxMz + (0.25 * diff);
            xaxis.Minimum = ms1MinMz;
            xaxis.Maximum = ms1MaxMz;
            xaxis.AbsoluteMinimum = 0;
            xaxis.AbsoluteMaximum = ms1AbsMax;
            xaxis.Zoom(ms1MinMz, ms1MaxMz);
        }

        /// <summary>
        /// Attempt to find the nearest MS/MS spectrum given an MS1 spectrum.
        /// </summary>
        /// <param name="ms1Scan">MS1 scan number</param>
        /// <param name="precursorMz">Precursor M/Z that should be in MS/MS spectrum's isolation window range.</param>
        /// <returns>Product spectrum for the nearest MS/MS spectrum. Returns null if one cannot be found.</returns>
        private ProductSpectrum FindNearestMs2Spectrum(int ms1Scan, double precursorMz)
        {
            // Do not have a valid LCMSRun or PrecursorMz, so we're not going to find an ms2 spectrum.
            if (!(lcms is LcMsRun lcmsRun) || precursorMz.Equals(0))
            {
                return null;
            }

            var scans = lcmsRun.GetFragmentationSpectraScanNums(precursorMz);
            if (scans.Length == 0)
            {
                return null;
            }

            var index = Array.BinarySearch(scans, ms1Scan);
            index = index < 0 ? ~index : index;
            var lowIndex = Math.Max(0, index - 1);
            var highIndex = Math.Min(scans.Length - 1, index + 1);

            var lowDiff = Math.Abs(index - lowIndex);
            var highDiff = Math.Abs(highIndex - index);

            var lowSpec = lcms.GetSpectrum(scans[lowIndex]) as ProductSpectrum;
            var highSpec = lcms.GetSpectrum(scans[highIndex]) as ProductSpectrum;

            ProductSpectrum spectrum;

            if ((lowDiff < highDiff || (lowDiff == highDiff && lowDiff > 0)) && lowSpec != null)
            {
                spectrum = lowSpec;
            }
            else if (highDiff < lowDiff && highSpec != null)
            {
                spectrum = highSpec;
            }
            else
            {
                spectrum = null;
            }

            return spectrum;
        }
    }
}
