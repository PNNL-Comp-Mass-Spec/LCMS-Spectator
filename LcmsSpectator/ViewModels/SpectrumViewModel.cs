using System;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.DialogServices;
using LcmsSpectator.TaskServices;
using LcmsSpectatorModels.Models;
using OxyPlot.Axes;
using LinearAxis = OxyPlot.Axes.LinearAxis;

namespace LcmsSpectator.ViewModels
{
    public class SpectrumViewModel: ViewModelBase
    {
        private ILcMsRun _lcms;
        public SpectrumPlotViewModel PrimarySpectrumViewModel { get; private set; }
        public SpectrumPlotViewModel Secondary1ViewModel { get; private set; }
        public SpectrumPlotViewModel Secondary2ViewModel { get; private set; }
        public SpectrumViewModel(IDialogService dialogService, ITaskService taskService, IMessenger messenger)
        {
            MessengerInstance = messenger;
            PrimarySpectrumViewModel = new SpectrumPlotViewModel(dialogService, TaskServiceFactory.GetTaskServiceLike(taskService), messenger, 1.05, false);
            Secondary1ViewModel = new SpectrumPlotViewModel(dialogService, TaskServiceFactory.GetTaskServiceLike(taskService), messenger, 1.1, true);
            Secondary2ViewModel = new SpectrumPlotViewModel(dialogService, taskService, messenger, 1.1, true);
            _selectedPrecursorMz = 0;
            MessengerInstance.Register<PropertyChangedMessage<double>>(this, SelectedPrecursorMzChanged);
            MessengerInstance.Register<PropertyChangedMessage<List<LabeledIonViewModel>>>(this, SelectedFragmentLabelsChanged);
            MessengerInstance.Register<PropertyChangedMessage<List<LabeledIonViewModel>>>(this, SelectedPrecursorLabelsChanged);
            MessengerInstance.Register<PropertyChangedMessage<PrSm>>(this, SelectedPrSmChanged);
            MessengerInstance.Register<XicPlotViewModel.SelectedScanChangedMessage>(this, SelectedScanChanged);
            Messenger.Default.Register<SettingsChangedNotification>(this, SettingsChanged);
        }

        public void ClearPlots()
        {
            PrimarySpectrumViewModel.Clear();
            Secondary2ViewModel.Clear();
            Secondary1ViewModel.Clear();
        }

        public ILcMsRun Lcms
        {
            get { return _lcms; }
            set
            {
                _lcms = value;
                RaisePropertyChanged();
            }
        }

        public void UpdateSpectra(int scan, bool fullUpdate=true)
        {
            if (scan == 0)
            {
                PrimarySpectrumViewModel.Clear();
                Secondary2ViewModel.Clear();
                Secondary1ViewModel.Clear();
                return;
            }
            var primary = _lcms.GetSpectrum(scan);

            string primaryTitle;
            string secondary1Title;
            string secondary2Title;

            Spectrum secondary1;
            Spectrum secondary2;

            if (primary is ProductSpectrum)
            {
                primaryTitle = "Ms2 Spectrum";
                secondary1Title = "Previous Ms1 Spectrum";
                secondary2Title = "Next Ms1 Spectrum";
                secondary1 = _lcms.GetSpectrum(_lcms.GetPrevScanNum(scan, 1));
                secondary2 = _lcms.GetSpectrum(_lcms.GetNextScanNum(scan, 1));
            }
            else
            {
                primary = FindNearestMs2Spectrum(scan, _lcms);
                if (primary == null)
                {
                    PrimarySpectrumViewModel.Clear();
                    Secondary1ViewModel.Clear();
                    Secondary2ViewModel.Clear();
                    return;
                }
                if (primary.ScanNum < scan)
                {
                    primaryTitle = "Previous Ms2 Spectrum";
                    secondary1Title = "Previous Ms1 Spectrum";
                    secondary2Title = "Ms1 Spectrum";
                    secondary1 = _lcms.GetSpectrum(_lcms.GetPrevScanNum(primary.ScanNum, 1));
                    secondary2 = _lcms.GetSpectrum(scan);
                }
                else
                {
                    primaryTitle = "Next Ms2 Spectrum";
                    secondary1Title = "Ms1 Spectrum";
                    secondary2Title = "Next Ms1 Spectrum";
                    secondary1 = _lcms.GetSpectrum(scan);
                    secondary2 = _lcms.GetSpectrum(_lcms.GetNextScanNum(primary.ScanNum, 1));
                }
            }

            // Ms2 spectrum plot
            //var heavyStr = "";//SelectedPrSmViewModel.Instance.Heavy ? ", Heavy" : "";
            PrimarySpectrumViewModel.Title = String.Format("{0} (Scan: {1})", primaryTitle, primary.ScanNum);
            PrimarySpectrumViewModel.SpectrumUpdate(primary);
            // Ms1 spectrum plots
            // previous Ms1
            var xAxis1 = GenerateMs1XAxis(primary, secondary1, secondary2);
            Secondary1ViewModel.SpectrumUpdate(secondary1, xAxis1);
            Secondary1ViewModel.Title = secondary1 == null ? "" : String.Format("{0} (Scan: {1})", secondary1Title, secondary1.ScanNum);
            // next Ms1
            var xAxis2 = GenerateMs1XAxis(primary, secondary1, secondary2);
            Secondary2ViewModel.SpectrumUpdate(secondary2, xAxis2);
            Secondary2ViewModel.Title = secondary2 == null ? "" : String.Format("{0} (Scan: {1})", secondary2Title, secondary2.ScanNum);

            bool isInternalChange = false;
            xAxis1.AxisChanged += (o, e) =>
            {
                if (isInternalChange) return;
                isInternalChange = true;
                xAxis2.Zoom(xAxis1.ActualMinimum, xAxis1.ActualMaximum);
                isInternalChange = false;
            };

            xAxis2.AxisChanged += (o, e) =>
            {
                if (isInternalChange) return;
                isInternalChange = true;
                xAxis1.Zoom(xAxis2.ActualMinimum, xAxis2.ActualMaximum);
                isInternalChange = false;
            };
        }

        private void SelectedPrecursorMzChanged(PropertyChangedMessage<double> message)
        {
            if (message.PropertyName == "PrecursorMz")
            {
                _selectedPrecursorMz = message.NewValue;
            }
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
            var xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "M/Z",
                Minimum = ms1MinMz,
                Maximum = ms1MaxMz,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = ms1AbsoluteMaximum * 1.25
            };
            xAxis.Zoom(ms1MinMz, ms1MaxMz);
            return xAxis;
        }

        private void SelectedFragmentLabelsChanged(PropertyChangedMessage<List<LabeledIonViewModel>> message)
        {
            if (message.PropertyName != "FragmentLabels") return;
            var ionListVm = message.Sender as IonListViewModel;
            if (ionListVm == null) return;
            var fragmentLabels = message.NewValue;
            //var labels = (!heavy) ? fragmentLabels : await ionListVm.GetHeavyFragmentIons();
            var labels = fragmentLabels;
            // add precursor ion
            /*if (labels.Count > 0)
            {
                var label1 = labels[0];
                var precursorIon = label1.LabeledIon.PrecursorIon;
                var charge = precursorIon.Charge;
                var precursorIonType = new IonType("Precursor", Composition.H2O, charge, false);
                var ion = new LabeledIon(precursorIon.Composition, 0, precursorIonType, false);
                labels.Add(new LabeledIonViewModel(ion));
            } */
            PrimarySpectrumViewModel.IonUpdate(labels);
        }

        private void SelectedPrecursorLabelsChanged(PropertyChangedMessage<List<LabeledIonViewModel>> message)
        {
            if (message.PropertyName != "PrecursorLabels") return;
            var ionListVm = message.Sender as IonListViewModel;
            if (ionListVm == null) return;
            List<LabeledIonViewModel> precursorLabels = message.NewValue;
            //if (!heavy) precursorLabels = message.NewValue;
            //else precursorLabels = await ionListVm.GetHeavyPrecursorIons();
            if (precursorLabels.Count < 1) return;
            LabeledIonViewModel precursorLabel = precursorLabels.FirstOrDefault(label => label.LabeledIon.Index == 0);
            if (precursorLabel != null)
            {
                Secondary2ViewModel.IonUpdate(new List<LabeledIonViewModel> { precursorLabel });
                Secondary1ViewModel.IonUpdate(new List<LabeledIonViewModel> { precursorLabel });   
            }
        }

        private void SelectedPrSmChanged(PropertyChangedMessage<PrSm> message)
        {
            if (message.PropertyName != "PrSm" || !(message.Sender is PrSmViewModel)) return;
            UpdateSpectra(message.NewValue.Scan, false);
        }

        private void SelectedScanChanged(XicPlotViewModel.SelectedScanChangedMessage message)
        {
            UpdateSpectra(message.Scan);
        }

        private void SettingsChanged(SettingsChangedNotification notification)
        {
            PrimarySpectrumViewModel.SpectrumUpdate();
            Secondary1ViewModel.SpectrumUpdate();
            Secondary2ViewModel.SpectrumUpdate();
        }

        private ProductSpectrum FindNearestMs2Spectrum(int ms1Scan, ILcMsRun lcms)
        {
            var precursormz = _selectedPrecursorMz;

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
                if (prodSpectrum == null) continue;
                if (prodSpectrum.IsolationWindow.Contains(precursormz))
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
                if (prodSpectrum == null) continue;
                if (prodSpectrum.IsolationWindow.Contains(precursormz))
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

        private double _selectedPrecursorMz;
    }
}
