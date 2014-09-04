using System.Collections.Generic;
using System.IO;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.PlotModels;
using LcmsSpectator.ViewModels;
using LcmsSpectatorModels.Config;
using LcmsSpectatorModels.Models;
using LcmsSpectatorModels.Readers;
using LcmsSpectatorModels.Utils;
using LcmsSpectatorTests.DialogServices;
using NUnit.Framework;

namespace LcmsSpectatorTests
{
    [TestFixture]
    public class SpectrumPlotTest
    {
        private const string RawFilePath = @"C:\Users\wilk011\Documents\DataFiles\Data\BottomUp\HCD_QCShew\raw\QC_Shew_13_04_A_17Feb14_Samwise_13-07-28.raw";
        private const string IdFilePath = @"C:\Users\wilk011\Documents\DataFiles\Data\BottomUp\HCD_QCShew\tsv\QC_Shew_13_04_A_17Feb14_Samwise_13-07-28.tsv";

        [Test]
        public async void TestDisplaySpectrum()
        {
            // init SpectrumPlotViewModel
            var colorDictionary = new ColorDictionary(2);
            var dialogService = new TestableMainDialogService();
            var spectrumPlotViewModel = new SpectrumPlotViewModel(dialogService, 1.05, colorDictionary);

            // init test data
            var idFileReader = IdFileReaderFactory.CreateReader(IdFilePath);
            var lcms = RafLcMsRun.GetLcMsRun(RawFilePath, MassSpecDataType.XCaliburRun);
            var ids = idFileReader.Read(lcms, Path.GetFileNameWithoutExtension(RawFilePath));
            var id = ids.GetHighestScoringPrSm();

            // init test ions
            var ions = new List<LabeledIon>();
            await spectrumPlotViewModel.Update(id.Ms2Spectrum, ions);

            // plot should not be null
            Assert.True(spectrumPlotViewModel.Plot != null);

            // plot should contain 1 stem series (the spectrum stem series)
            Assert.True(spectrumPlotViewModel.Plot.Series.Count == 1);
        }

        [Test]
        public async void TestAddIons()
        {
            // init SpectrumPlotViewModel
            var colorDictionary = new ColorDictionary(2);
            var dialogService = new TestableMainDialogService();
            var spectrumPlotViewModel = new SpectrumPlotViewModel(dialogService, 1.05, colorDictionary);

            // init test data
            var idFileReader = IdFileReaderFactory.CreateReader(IdFilePath);
            var lcms = RafLcMsRun.GetLcMsRun(RawFilePath, MassSpecDataType.XCaliburRun);
            var ids = idFileReader.Read(lcms, Path.GetFileNameWithoutExtension(RawFilePath));
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
            await spectrumPlotViewModel.Update(id.Ms2Spectrum, ions);

            // there should be ions.count + 1 (spectrum series) plot series
            Assert.True(spectrumPlotViewModel.Plot.Series.Count == (expectedIons.Count + 1));

            // Remove ion types
            baseIonTypes = new List<BaseIonType> { BaseIonType.Y };
            ionTypes = IonUtils.GetIonTypes(ionTypeFactory, baseIonTypes, neutralLosses, minCharge, maxCharge);
            ions = IonUtils.GetFragmentIonLabels(id.Sequence, charge, ionTypes);
            expectedIons = IonUtils.GetIonPeaks(ions, id.Ms2Spectrum, IcParameters.Instance.ProductIonTolerancePpm,
                                        IcParameters.Instance.PrecursorTolerancePpm,
                                        IcParameters.Instance.IonCorrelationThreshold);
            await spectrumPlotViewModel.Update(id.Ms2Spectrum, ions);

            // there should be ions.count + 1 (spectrum series) plot series
            Assert.True(spectrumPlotViewModel.Plot.Series.Count == (expectedIons.Count + 1));
        }
    }
}
