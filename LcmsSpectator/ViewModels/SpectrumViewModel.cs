using System;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.DialogServices;
using LcmsSpectator.TaskServices;
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
        public SpectrumViewModel(IDialogService dialogService, ITaskService taskService)
        {
            _fragmentLabels = new List<LabeledIonViewModel>();
            _precursorLabels = new List<LabeledIonViewModel>();
            Ms2SpectrumViewModel = new SpectrumPlotViewModel(dialogService, TaskServiceFactory.GetTaskServiceLike(taskService), 1.05);
            PreviousMs1ViewModel = new SpectrumPlotViewModel(dialogService, TaskServiceFactory.GetTaskServiceLike(taskService), 1.1);
            NextMs1ViewModel = new SpectrumPlotViewModel(dialogService, taskService, 1.1);
            Messenger.Default.Register<PropertyChangedMessage<List<LabeledIonViewModel>>>(this, SelectedFragmentLabelsChanged);
            Messenger.Default.Register<PropertyChangedMessage<List<LabeledIonViewModel>>>(this, SelectedPrecursorLabelsChanged);
            Messenger.Default.Register<PropertyChangedMessage<int>>(this, SelectedScanChanged);
            Messenger.Default.Register<SettingsChangedNotification>(this, SettingsChanged);
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

        private async void SelectedFragmentLabelsChanged(PropertyChangedMessage<List<LabeledIonViewModel>> message)
        {
            if (message.PropertyName != "FragmentLabels") return;
            var ionListVm = message.Sender as IonListViewModel;
            if (ionListVm == null) return;
            var fragmentLabels = message.NewValue;
            var heavy = SelectedPrSmViewModel.Instance.Heavy;
            var labels = (!heavy) ? fragmentLabels : await ionListVm.GetHeavyFragmentIons();
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
            _fragmentLabels = labels;
            Ms2SpectrumViewModel.IonUpdate(labels);
        }

        private async void SelectedPrecursorLabelsChanged(PropertyChangedMessage<List<LabeledIonViewModel>> message)
        {
            if (message.PropertyName != "PrecursorLabels") return;
            var ionListVm = message.Sender as IonListViewModel;
            if (ionListVm == null) return;
            var heavy = SelectedPrSmViewModel.Instance.Heavy;
            List<LabeledIonViewModel> precursorLabels;
            if (!heavy) precursorLabels = message.NewValue;
            else precursorLabels = await ionListVm.GetHeavyPrecursorIons();
            if (precursorLabels.Count < 2) return;
            LabeledIonViewModel precursorLabel = null;
            foreach (var label in precursorLabels.Where(label => label.LabeledIon.Index == 0))
            {
                precursorLabel = label;
                break;
            }
            _precursorLabels = precursorLabels;
            if (precursorLabel != null)
            {
                NextMs1ViewModel.IonUpdate(new List<LabeledIonViewModel> { precursorLabel });
                PreviousMs1ViewModel.IonUpdate(new List<LabeledIonViewModel> { precursorLabel });   
            }
        }

        private void SelectedScanChanged(PropertyChangedMessage<int> message)
        {
            if (message.PropertyName != "Scan" || message.Sender != SelectedPrSmViewModel.Instance) return;
            var scan = message.NewValue;
            if (scan == 0)
            {
                Ms2SpectrumViewModel.Clear();
                NextMs1ViewModel.Clear();
                PreviousMs1ViewModel.Clear();
                return;
            }
            var lcms = SelectedPrSmViewModel.Instance.Lcms;
            var rawFileName = SelectedPrSmViewModel.Instance.RawFileName;
            var ms2 = lcms.GetSpectrum(scan);
            var prevms1 = lcms.GetSpectrum(lcms.GetPrevScanNum(scan, 1));
            var nextms1 = lcms.GetSpectrum(lcms.GetNextScanNum(scan, 1));

            // get ions
            //var precursors = SelectedPrSmViewModel.Instance.PrecursorLabels; /*await SelectedPrSmViewModel.Instance.PrecursorLabelUpdate; */
            //var precursorIon = precursors.Count > 2 ? new List<LabeledIonViewModel> { precursors[1] } : new List<LabeledIonViewModel>();

            // Ms2 spectrum plot
            var heavyStr = SelectedPrSmViewModel.Instance.Heavy ? ", Heavy" : "";
            Ms2SpectrumViewModel.Title = (ms2 == null) ? "" : String.Format("Ms2 Spectrum (Scan: {0}, Raw: {1}{2})", ms2.ScanNum, rawFileName, heavyStr);
            Ms2SpectrumViewModel.SpectrumUpdate(ms2);
            // Ms1 spectrum plots
            var xAxis = GenerateMs1XAxis(ms2, prevms1, nextms1);    // shared x axis
            // previous Ms1
            PreviousMs1ViewModel.SpectrumUpdate(prevms1, xAxis);
            PreviousMs1ViewModel.Title = prevms1 == null ? "" : String.Format("Previous Ms1 Spectrum (Scan: {0})", prevms1.ScanNum);
            // next Ms1
            NextMs1ViewModel.SpectrumUpdate(nextms1, xAxis);
            NextMs1ViewModel.Title = nextms1 == null ? "" : String.Format("Next Ms1 Spectrum (Scan: {0})", nextms1.ScanNum);
        }

        private void SettingsChanged(SettingsChangedNotification notification)
        {
            var prsm = SelectedPrSmViewModel.Instance.PrSm;
            var scan = prsm.Scan;
            if (scan == 0)
            {
                Ms2SpectrumViewModel.Clear();
                NextMs1ViewModel.Clear();
                PreviousMs1ViewModel.Clear();
                return;
            }
            var lcms = SelectedPrSmViewModel.Instance.Lcms;
            var rawFileName = SelectedPrSmViewModel.Instance.RawFileName;
            var ms2 = lcms.GetSpectrum(scan);
            var prevms1 = lcms.GetSpectrum(lcms.GetPrevScanNum(scan, 1));
            var nextms1 = lcms.GetSpectrum(lcms.GetNextScanNum(scan, 1));

            var precursorIon = _precursorLabels.Count > 2 ? new List<LabeledIonViewModel> { _precursorLabels[1] } : new List<LabeledIonViewModel>();

            // Ms2 spectrum plot
            var heavyStr = SelectedPrSmViewModel.Instance.Heavy ? ", Heavy" : "";
            Ms2SpectrumViewModel.Title = (ms2 == null) ? "" : String.Format("Ms2 Spectrum (Scan: {0}, Raw: {1}{2})", ms2.ScanNum, rawFileName, heavyStr);
            Ms2SpectrumViewModel.SpectrumUpdate(ms2);
            Ms2SpectrumViewModel.IonUpdate(_fragmentLabels);
            // Ms1 spectrum plots
            var xAxis = GenerateMs1XAxis(ms2, prevms1, nextms1);    // shared x axis
            // previous Ms1
            PreviousMs1ViewModel.SpectrumUpdate(prevms1, xAxis);
            PreviousMs1ViewModel.IonUpdate(precursorIon);
            PreviousMs1ViewModel.Title = prevms1 == null ? "" : String.Format("Previous Ms1 Spectrum (Scan: {0})", prevms1.ScanNum);
            // next Ms1
            NextMs1ViewModel.SpectrumUpdate(nextms1, xAxis);
            NextMs1ViewModel.IonUpdate(precursorIon);
            NextMs1ViewModel.Title = nextms1 == null ? "" : String.Format("Next Ms1 Spectrum (Scan: {0})", nextms1.ScanNum);
        }

        private List<LabeledIonViewModel> _fragmentLabels;
        private List<LabeledIonViewModel> _precursorLabels;
    }
}
