using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.PlotModels;
using LcmsSpectator.ViewModels;
using LcmsSpectatorModels.Config;
using LcmsSpectatorModels.Models;
using LcmsSpectatorModels.Readers;
using LcmsSpectatorModels.Utils;
using LcmsSpectatorTests.DialogServices;
using MultiDimensionalPeakFinding.PeakDetection;
using NUnit.Framework;
using OxyPlot.Series;

namespace LcmsSpectatorTests
{
    [TestFixture]
    public class SpectrumPlotTest
    {
        [TestCase(@"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.raw",
                 @"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.tsv")]
        public async void TestDisplaySpectrum(string rawFile, string tsvFile)
        {
            // init
            var idFileReader = IdFileReaderFactory.CreateReader(tsvFile);
            var ids = idFileReader.Read();
            var lcms = PbfLcMsRun.GetLcMsRun(rawFile, MassSpecDataType.XCaliburRun);
            ids.SetLcmsRun(lcms, Path.GetFileNameWithoutExtension(rawFile));

            // init SpectrumPlotViewModel
            var colorDictionary = new ColorDictionary(2);
            var dialogService = new TestableMainDialogService();
            var spectrumPlotViewModel = new SpectrumPlotViewModel(dialogService, 1.05, colorDictionary);

            // init test data
            var id = ids.GetHighestScoringPrSm();

            // init test ions
            var ions = new List<LabeledIon>();
            spectrumPlotViewModel.Spectrum = id.Ms2Spectrum;
            spectrumPlotViewModel.Ions = ions;
            await spectrumPlotViewModel.Update();

            // plot should not be null
            Assert.True(spectrumPlotViewModel.Plot != null);

            // plot should contain 1 stem series (the spectrum stem series)
            Assert.True(spectrumPlotViewModel.Plot.Series.Count == 1);
        }

        [TestCase(@"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.raw",
                 @"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.tsv")]
        public async void TestAddIons(string rawFile, string tsvFile)
        {
            // init
            var idFileReader = IdFileReaderFactory.CreateReader(tsvFile);
            var ids = idFileReader.Read();
            var lcms = PbfLcMsRun.GetLcMsRun(rawFile, MassSpecDataType.XCaliburRun);
            ids.SetLcmsRun(lcms, Path.GetFileNameWithoutExtension(rawFile));

            // init SpectrumPlotViewModel
            var colorDictionary = new ColorDictionary(2);
            var dialogService = new TestableMainDialogService();
            var spectrumPlotViewModel = new SpectrumPlotViewModel(dialogService, 1.05, colorDictionary);

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
            var expectedIons = IonUtils.GetIonPeaks(ions, id.Ms2Spectrum, IcParameters.Instance.ProductIonTolerancePpm,
                                                    IcParameters.Instance.PrecursorTolerancePpm,
                                                    IcParameters.Instance.IonCorrelationThreshold);
            spectrumPlotViewModel.Spectrum = id.Ms2Spectrum;
            spectrumPlotViewModel.Ions = ions;
            await spectrumPlotViewModel.Update();

            // there should be ions.count + 1 (spectrum series) plot series
            Assert.True(spectrumPlotViewModel.Plot.Series.Count == (expectedIons.Count + 1));

            // Remove ion types
            baseIonTypes = new List<BaseIonType> { BaseIonType.Y };
            ionTypes = IonUtils.GetIonTypes(ionTypeFactory, baseIonTypes, neutralLosses, minCharge, maxCharge);
            ions = IonUtils.GetFragmentIonLabels(id.Sequence, charge, ionTypes);
            expectedIons = IonUtils.GetIonPeaks(ions, id.Ms2Spectrum, IcParameters.Instance.ProductIonTolerancePpm,
                                        IcParameters.Instance.PrecursorTolerancePpm,
                                        IcParameters.Instance.IonCorrelationThreshold);
            spectrumPlotViewModel.Spectrum = id.Ms2Spectrum;
            spectrumPlotViewModel.Ions = ions;
            await spectrumPlotViewModel.Update();

            // there should be ions.count + 1 (spectrum series) plot series
            Assert.True(spectrumPlotViewModel.Plot.Series.Count == (expectedIons.Count + 1));
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
        public async void TestIonError(string rawFile, string tsvFile)
        {
            // init
            var idFileReader = IdFileReaderFactory.CreateReader(tsvFile);
            var ids = idFileReader.Read();
            var lcms = PbfLcMsRun.GetLcMsRun(rawFile, MassSpecDataType.XCaliburRun);
            ids.SetLcmsRun(lcms, Path.GetFileNameWithoutExtension(rawFile));
            var prsms = ids.IdentifiedPrSms;

            // init SpectrumPlotViewModel
            var colorDictionary = new ColorDictionary(2);
            var dialogService = new TestableMainDialogService();
            var spectrumPlotViewModel = new SpectrumPlotViewModel(dialogService, 1.05, colorDictionary);

            // init ionTypes
            const int maxCharge = 15;
            var ionTypeFactory = new IonTypeFactory(maxCharge);
            var ionTypes = ionTypeFactory.GetAllKnownIonTypes().ToArray();

            foreach (var prsm in prsms)
            {
                var ions = IonUtils.GetFragmentIonLabels(prsm.Sequence, prsm.Charge, ionTypes);
                spectrumPlotViewModel.Ions = ions;
                spectrumPlotViewModel.Spectrum = prsm.Ms2Spectrum;
                await spectrumPlotViewModel.Update();

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
