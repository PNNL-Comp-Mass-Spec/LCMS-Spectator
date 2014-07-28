using System;
using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.PlotModels;
using LcmsSpectatorModels.Models;
using OxyPlot;
using OxyPlot.Axes;

namespace LcmsSpectator.ViewModels
{
    public class SpectrumViewModel: ViewModelBase
    {
        public PlotModel Ms2SpectrumPlotModel { get; set; }
        public PlotModel PreviousMs1PlotModel { get; set; }
        public PlotModel NextMs1PlotModel { get; set; }

        public ColorDictionary Colors { get; set; }

        public SpectrumViewModel(ColorDictionary colors)
        {
            Colors = colors;
            Ms2SpectrumPlotModel = new PlotModel();
            PreviousMs1PlotModel = new PlotModel();
            NextMs1PlotModel = new PlotModel();
        }

        public void UpdatePlots(PrSm prsm, IList<BaseIonType> baseIons, IList<NeutralLoss> neutralLosses, int minCharge, int maxCharge)
        {
            var ms2 = prsm.Ms2Spectrum;
            if (ms2 == null)
            {
                Ms2SpectrumPlotModel = new PlotModel(); OnPropertyChanged("Ms2SpectrumPlotModel");
                PreviousMs1PlotModel = new PlotModel(); OnPropertyChanged("PreviousMs1PlotModel");
                NextMs1PlotModel = new PlotModel(); OnPropertyChanged("NextMs1PlotModel");
                return;
            }

            var prevms1 = prsm.PreviousMs1;
            var nextms1 = prsm.NextMs1;
            LabeledIonPeaks prevms1Ion = null;
            LabeledIonPeaks nextms1Ion = null;
            var ms2PrecursorIon = prsm.PrecursorIonPeaks();
            List<LabeledIonPeaks> ms2Ions = prsm.GetFragmentIons(baseIons, neutralLosses, minCharge, maxCharge);
            ms2Ions.Add(ms2PrecursorIon);

            InitSpectrumPlots(ms2, prevms1, nextms1);
            if (nextms1 != null) nextms1Ion = prsm.NextMs1PrecursorIonPeaks;
            if (prevms1 != null) prevms1Ion = prsm.PrevMs1PrecursorIonPeaks;
            LoadMs2SpectrumPlot(ms2, ms2Ions);
            LoadPreviousMs1PlotModel(prevms1, prevms1Ion);
            LoadNextMs1PlotModel(nextms1, nextms1Ion);
        }

        public void InitSpectrumPlots(Spectrum ms2, Spectrum prevms1, Spectrum nextms1)
        {
            if (ms2 == null) return;
            var ms2MaxMz = ms2.Peaks.Max().Mz * 1.2;
            _ms2XAxis = new LinearAxis(AxisPosition.Bottom, "M/Z")
            {
                Minimum = 0,
                Maximum = ms2MaxMz,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = ms2MaxMz
            };
            _ms2XAxis.Zoom(0, ms2MaxMz);
            var ms2Prod = ms2 as ProductSpectrum;
            if (ms2Prod == null) return;
            var prevms1AbsMax = prevms1.Peaks.Max().Mz;
            var nextms1AbsMax = nextms1.Peaks.Max().Mz;
            var ms1AbsoluteMaximum = (prevms1AbsMax >= nextms1AbsMax) ? prevms1AbsMax : nextms1AbsMax;
            var ms1Min = ms2Prod.IsolationWindow.MinMz;
            var ms1Max = ms2Prod.IsolationWindow.MaxMz;
            var diff = ms1Max - ms1Min;
            var ms1MinMz = ms2Prod.IsolationWindow.MinMz - 0.25*diff;
            var ms1MaxMz = ms2Prod.IsolationWindow.MaxMz + 0.25*diff;
            _ms1XAxis = new LinearAxis(AxisPosition.Bottom, "M/Z")
            {
                Minimum = ms1MinMz,
                Maximum = ms1MaxMz,
                AbsoluteMinimum = 0,
                AbsoluteMaximum = ms1AbsoluteMaximum * 1.25
            };
            _ms1XAxis.Zoom(ms1MinMz, ms1MaxMz);
        }

        private void LoadMs2SpectrumPlot(Spectrum spectrum, IEnumerable<LabeledIonPeaks> ions)
        {
            if (spectrum == null)
            {
                Ms2SpectrumPlotModel = new PlotModel();
                OnPropertyChanged("Ms2SpectrumPlotModel");
                return;
            }
            var title = String.Format("Ms2 Spectrum (Scan {0})", spectrum.ScanNum);
            var plotModel = new SpectrumPlotModel(title, spectrum, ions, Colors, _ms2XAxis, 1.05);
            Ms2SpectrumPlotModel = plotModel;
            OnPropertyChanged("Ms2SpectrumPlotModel");
        }

        private void LoadPreviousMs1PlotModel(Spectrum spectrum, LabeledIonPeaks precursorIon)
        {
            if (spectrum == null)
            {
                PreviousMs1PlotModel = new PlotModel();
                OnPropertyChanged("PreviousMs1PlotModel");
                return;
            }
            var ions = new List<LabeledIonPeaks>();
            if (precursorIon != null) ions.Add(precursorIon);
            var title = String.Format("Previous Ms1 Spectrum (Scan {0})", spectrum.ScanNum);
            var plotModel = new SpectrumPlotModel(title, spectrum, ions, Colors, _ms1XAxis, 1.1);
            PreviousMs1PlotModel = plotModel;
            OnPropertyChanged("PreviousMs1PlotModel");
        }

        private void LoadNextMs1PlotModel(Spectrum spectrum, LabeledIonPeaks precursorIon)
        {
            if (spectrum == null)
            {
                NextMs1PlotModel = new PlotModel();
                OnPropertyChanged("NextMs1PlotModel");
                return;
            }
            var ions = new List<LabeledIonPeaks>();
            if (precursorIon != null) ions.Add(precursorIon);
            var title = String.Format("Next Ms1 Spectrum (Scan {0})", spectrum.ScanNum);
            var plotModel = new SpectrumPlotModel(title, spectrum, ions, Colors, _ms1XAxis, 1.1);
            NextMs1PlotModel = plotModel;
            OnPropertyChanged("NextMs1PlotModel");
        }

        private LinearAxis _ms2XAxis;
        private LinearAxis _ms1XAxis;
    }
}
