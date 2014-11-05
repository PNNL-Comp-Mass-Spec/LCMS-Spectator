using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using InformedProteomics.TopDown.Scoring;
using LcmsSpectator.PlotModels;
using LcmsSpectator.TaskServices;
using LcmsSpectator.ViewModels;
using LcmsSpectatorModels.Config;
using LcmsSpectatorModels.Readers;
using LcmsSpectatorModels.Utils;
using LcmsSpectatorTests.DialogServices;
using NUnit.Framework;
using OxyPlot.Annotations;
using OxyPlot.Series;

namespace LcmsSpectatorTests
{
    [TestFixture]
    public class SpectrumPlotTest
    {
        [TestCase(@"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.raw",
                 @"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.tsv")]
        public void TestDisplaySpectrum(string rawFile, string tsvFile)
        {
            // init
            var idFileReader = IdFileReaderFactory.CreateReader(tsvFile);
            var ids = idFileReader.Read();
            var lcms = PbfLcMsRun.GetLcMsRun(rawFile);
            ids.SetLcmsRun(lcms, Path.GetFileNameWithoutExtension(rawFile));

            // init SpectrumPlotViewModel
            SelectedPrSmViewModel.Instance.Charge = 2;
            var dialogService = new TestableMainDialogService();
            var spectrumPlotViewModel = new SpectrumPlotViewModel(dialogService, new MockTaskService(), 1.05, false);

            // init test data
            var id = ids.GetHighestScoringPrSm();

            // init test ions
            var ions = new List<LabeledIonViewModel>();
            spectrumPlotViewModel.SpectrumUpdate(id.Ms2Spectrum);
            spectrumPlotViewModel.IonUpdate(ions);

            // plot should not be null
            Assert.True(spectrumPlotViewModel.Plot != null);

            // plot should contain 1 stem series (the spectrum stem series)
            Assert.True(spectrumPlotViewModel.Plot.Series.Count == 1);
        }

        [TestCase(@"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.raw",
                 @"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.tsv")]
        public void TestAddIons(string rawFile, string tsvFile)
        {
            // init
            var idFileReader = IdFileReaderFactory.CreateReader(tsvFile);
            var ids = idFileReader.Read();
            var lcms = PbfLcMsRun.GetLcMsRun(rawFile);
            ids.SetLcmsRun(lcms, Path.GetFileNameWithoutExtension(rawFile));

            // init SpectrumPlotViewModel
            SelectedPrSmViewModel.Instance.Charge = 2;
            var dialogService = new TestableMainDialogService();
            var spectrumPlotViewModel = new SpectrumPlotViewModel(dialogService, new MockTaskService(),  1.05, false);

            // init test data
            var id = ids.GetHighestScoringPrSm();

            // init test ions
            var baseIonTypes = new List<BaseIonType> { BaseIonType.B, BaseIonType.Y };
            var neutralLosses = new List<NeutralLoss> { NeutralLoss.NoLoss };
            const int charge = 1;
            const int minCharge = 1, maxCharge = 2;
            var ionTypeFactory = new IonTypeFactory(maxCharge);
            var ionTypes = IonUtils.GetIonTypes(ionTypeFactory, baseIonTypes, neutralLosses, minCharge, maxCharge);
            var ions = IonUtils.GetFragmentIonLabels(id.Sequence, charge, ionTypes);
            var ionVms = new List<LabeledIonViewModel>();
            foreach (var label in ions) ionVms.Add(new LabeledIonViewModel(label));
            var expectedIons = IonUtils.GetIonPeaks(ions, id.Ms2Spectrum, IcParameters.Instance.ProductIonTolerancePpm,
                                                    IcParameters.Instance.PrecursorTolerancePpm,
                                                    IcParameters.Instance.IonCorrelationThreshold, false);
            spectrumPlotViewModel.SpectrumUpdate(id.Ms2Spectrum);
            spectrumPlotViewModel.IonUpdate(ionVms);

            // there should be ions.count + 1 (spectrum series) plot series
            Assert.True(spectrumPlotViewModel.Plot.Series.Count == (expectedIons.Count + 1));

            // Remove ion types
            baseIonTypes = new List<BaseIonType> { BaseIonType.Y };
            ionTypes = IonUtils.GetIonTypes(ionTypeFactory, baseIonTypes, neutralLosses, minCharge, maxCharge);
            ions = IonUtils.GetFragmentIonLabels(id.Sequence, charge, ionTypes);
            ionVms = new List<LabeledIonViewModel>();
            foreach (var label in ions) ionVms.Add(new LabeledIonViewModel(label));
            expectedIons = IonUtils.GetIonPeaks(ions, id.Ms2Spectrum, IcParameters.Instance.ProductIonTolerancePpm,
                                        IcParameters.Instance.PrecursorTolerancePpm,
                                        IcParameters.Instance.IonCorrelationThreshold, false);
            spectrumPlotViewModel.SpectrumUpdate(id.Ms2Spectrum);
            spectrumPlotViewModel.IonUpdate(ionVms);
            //spectrumPlotViewModel.UpdateSpectrum();

            // there should be ions.count + 1 (spectrum series) plot series
            Assert.True(spectrumPlotViewModel.Plot.Series.Count == (expectedIons.Count + 1));
        }

        /// <summary>
        /// This test checks to see if the correct spectrum is shown when setting ShowFilteredSpectrum
        /// </summary>
        /// <param name="rawFile"></param>
        [TestCase(@"\\protoapps\UserData\Wilkins\TopDown\Anil\QC_Shew_IntactProtein_new_CID-30CE-4Sep14_Bane_C2Column_3.raw")]
        public void TestShowFilteredSpectrum(string rawFile)
        {
            var lcms = PbfLcMsRun.GetLcMsRun(rawFile);
            var scans = lcms.GetScanNumbers(2);

            const int maxCharge = 15;
            SelectedPrSmViewModel.Instance.Charge = maxCharge;
            var specPlotVm = new SpectrumPlotViewModel(new TestableMainDialogService(), new MockTaskService(), 1.05, false);

            foreach (var scan in scans)
            {
                var spectrum = lcms.GetSpectrum(scan);
                var filteredSpectrum = spectrum.GetFilteredSpectrumBySlope();   // filtered spectrum to test against
                specPlotVm.SpectrumUpdate(spectrum);

                // check unfiltered spectrum
                specPlotVm.ShowFilteredSpectrum = false;
                //if (!specPlotVm.PlotTask.IsCompleted) await specPlotVm.PlotTask;
                var spectrumSeries = specPlotVm.Plot.Series[0] as StemSeries;
                Assert.True(spectrumSeries.Points.Count == spectrum.Peaks.Length);  // should be the same length
                for (int i = 0; i < spectrumSeries.Points.Count; i++)
                {
                    // compare each peak in spectrum plot to actual spectrum
                    Assert.True(spectrumSeries.Points[i].X.Equals(spectrum.Peaks[i].Mz));
                    Assert.True(spectrumSeries.Points[i].Y.Equals(spectrum.Peaks[i].Intensity));
                }

                // check filtered spectrum
                specPlotVm.ShowFilteredSpectrum = true;
                //if (!specPlotVm.PlotTask.IsCompleted) await specPlotVm.PlotTask;
                var filteredSeries = specPlotVm.Plot.Series[0] as StemSeries;
                Assert.True(filteredSeries.Points.Count == filteredSpectrum.Peaks.Length);   // should be the same length
                for (int i = 0; i < filteredSeries.Points.Count; i++)
                {
                    // compare each peak in spectrum plot to actual filtered spectrum
                    Assert.True(filteredSeries.Points[i].X.Equals(filteredSpectrum.Peaks[i].Mz));
                    Assert.True(filteredSeries.Points[i].Y.Equals(filteredSpectrum.Peaks[i].Intensity));
                }
            }
        }

        [TestCase(@"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.raw",
                  @"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.tsv")]
        public void TestShowDeconvolutedIons(string rawFile, string tsvFile)
        {
            var lcms = PbfLcMsRun.GetLcMsRun(rawFile);
            var scans = lcms.GetScanNumbers(2);
            var idFileReader = IdFileReaderFactory.CreateReader(tsvFile);
            var ids = idFileReader.Read().AllPrSms;

            const int maxCharge = 15;
            SelectedPrSmViewModel.Instance.Charge = maxCharge;
            var specPlotVm = new SpectrumPlotViewModel(new TestableMainDialogService(), new MockTaskService(), 1.05, false);
            specPlotVm.ShowDeconvolutedSpectrum = true;

            var ionTypeFactory = new IonTypeFactory(maxCharge);
            var ionTypes = ionTypeFactory.GetAllKnownIonTypes();

            foreach (var prsm in ids)
            {
                specPlotVm.SpectrumUpdate(prsm.Ms2Spectrum);
                var ions = IonUtils.GetFragmentIonLabels(prsm.Sequence, prsm.Charge, ionTypes.ToList());
                var ionVms = new List<LabeledIonViewModel>();
                foreach (var label in ions) ionVms.Add(new LabeledIonViewModel(label));
                specPlotVm.IonUpdate(ionVms);
                foreach (var annotation in specPlotVm.Plot.Annotations)
                {
                    var textAnnotation = annotation as TextAnnotation;
                    Assert.True(textAnnotation.Text.Contains("'"));
                    Assert.True(textAnnotation.Text.Contains("1+"));
                }
            }
        }

        /// <summary>
        /// This test checks to see if the spectrum plot is showing a valid ppm error for the ion highlights
        /// </summary>
        /// <param name="rawFile"></param>
        /// <param name="tsvFile"></param>
        [TestCase(@"\\protoapps\UserData\Wilkins\TopDown\Anil\QC_Shew_IntactProtein_new_CID-30CE-4Sep14_Bane_C2Column_3.raw",
          @"\\protoapps\UserData\Wilkins\TopDown\Anil\QC_Shew_IntactProtein_new_CID-30CE-4Sep14_Bane_C2Column_3_IcTda.tsv")]
        // bottom up (dia) data
        [TestCase(@"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.raw",
                  @"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.tsv")]
        // bottom up (dda) data
        [TestCase(@"\\protoapps\UserData\Wilkins\BottomUp\DDA\Q_2014_0523_1_0_amol_uL_DDA.raw",
                  @"\\protoapps\UserData\Wilkins\BottomUp\DDA\Q_2014_0523_1_0_amol_uL_DDA_IcTda.tsv")]
        public void TestIonError(string rawFile, string tsvFile)
        {
            // init
            var idFileReader = IdFileReaderFactory.CreateReader(tsvFile);
            var ids = idFileReader.Read();
            var lcms = PbfLcMsRun.GetLcMsRun(rawFile);
            ids.SetLcmsRun(lcms, Path.GetFileNameWithoutExtension(rawFile));
            var prsms = ids.IdentifiedPrSms;

            // init SpectrumPlotViewModel
            SelectedPrSmViewModel.Instance.Charge = 2;
            var dialogService = new TestableMainDialogService();
            var spectrumPlotViewModel = new SpectrumPlotViewModel(dialogService, new MockTaskService(), 1.05, false);

            // init ionTypes
            const int maxCharge = 15;
            var ionTypeFactory = new IonTypeFactory(maxCharge);
            var ionTypes = ionTypeFactory.GetAllKnownIonTypes().ToArray();

            foreach (var prsm in prsms)
            {
                var ions = IonUtils.GetFragmentIonLabels(prsm.Sequence, prsm.Charge, ionTypes);
                var ionVms = new List<LabeledIonViewModel>();
                foreach (var label in ions) ionVms.Add(new LabeledIonViewModel(label));
                spectrumPlotViewModel.IonUpdate(ionVms);
                spectrumPlotViewModel.SpectrumUpdate(prsm.Ms2Spectrum);
                //spectrumPlotViewModel.UpdateSpectrum();
                //if (!spectrumPlotViewModel.PlotTask.IsCompleted) await spectrumPlotViewModel.PlotTask;

                foreach (var series in spectrumPlotViewModel.Plot.Series)
                {
                    if (series is StemSeries)
                    {
                        var stemSeries = series as StemSeries;
                        foreach (var dataPoint in stemSeries.Points)
                        {
                            if (dataPoint is PeakDataPoint)
                            {
                                var peakDataPoint = dataPoint as PeakDataPoint;
                                Assert.True(peakDataPoint.Error <= IcParameters.Instance.ProductIonTolerancePpm.GetValue());
                            }
                        }
                    }
                }
            }

        }
    }
}
