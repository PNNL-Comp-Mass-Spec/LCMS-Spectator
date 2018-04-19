using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.Readers;
using LcmsSpectatorTests.DialogServices;
using NUnit.Framework;
using OxyPlot.Series;
using ReactiveUI;

namespace LcmsSpectatorTests
{

    using LcmsSpectator.Models;
    using LcmsSpectator.ViewModels.Data;
    using LcmsSpectator.ViewModels.Plots;
    using LcmsSpectator.Writers.Exporters;

    [TestFixture]
    public class SpectrumPlotTest
    {
        [TestCase(@"\\proto-2\UnitTest_Files\LcMsSpectator\TopDown\Anil\QC_Shew_IntactProtein_new_CID-30CE-4Sep14_Bane_C2Column_3.raw",
                  @"\\proto-2\UnitTest_Files\LcMsSpectator\TopDown\Anil\QC_Shew_IntactProtein_new_CID-30CE-4Sep14_Bane_C2Column_3_IcTda.tsv")]
        [Category("PNL_Domain")]
        [Ignore("PlotModel.Series not getting populated")]
        public async Task TestDisplaySpectrum(string rawFile, string tsvFile)
        {
            // init
            var idFileReader = IdFileReaderFactory.CreateReader(tsvFile);
            var ids = idFileReader.Read();
            var lcms = PbfLcMsRun.GetLcMsRun(rawFile);
            var idList = ids.ToList();
            foreach (var id in idList)
            {
                id.LcMs = lcms;
                id.RawFileName = Path.GetFileNameWithoutExtension(rawFile);
            }

            // init SpectrumPlotViewModel
            var dialogService = new TestableMainDialogService();
            var spectrumPlotViewModel = new SpectrumPlotViewModel(dialogService, new FragmentationSequenceViewModel(), 1.05, false);

            // init test data
            idList.Sort(new PrSm.PrSmScoreComparer());
            var prsm = idList[0];

            // init test ions
            var ions = new ReactiveList<LabeledIonViewModel>();
            spectrumPlotViewModel.Spectrum = prsm.Ms2Spectrum;
            ////spectrumPlotViewModel.Ions = ions;

            // plot should not be null
            Assert.NotNull(spectrumPlotViewModel.PlotModel);

            // Give the PlotModel time to update
            await MsPathFinderTest.SleepMillisecondsAsync(1000);

            // plot should contain 1 stem series (the spectrum stem series)
            Assert.True(spectrumPlotViewModel.PlotModel.Series.Count == 1);
        }

        /// <summary>
        /// This test checks to see if the correct spectrum is shown when setting ShowFilteredSpectrum
        /// </summary>
        /// <param name="rawFile"></param>
        [TestCase(@"\\proto-2\UnitTest_Files\LcMsSpectator\TopDown\Anil\QC_Shew_IntactProtein_new_CID-30CE-4Sep14_Bane_C2Column_3.raw")]
        [Category("PNL_Domain")]
        [Ignore("PlotModel.Series not getting populated")]
        public async Task TestShowFilteredSpectrum(string rawFile)
        {
            var lcms = PbfLcMsRun.GetLcMsRun(rawFile);
            var scans = lcms.GetScanNumbers(2);

            var specPlotVm = new SpectrumPlotViewModel(new TestableMainDialogService(), new FragmentationSequenceViewModel(), 1.05, false);

            foreach (var scan in scans)
            {
                var spectrum = lcms.GetSpectrum(scan);
                var filteredSpectrum = spectrum.GetFilteredSpectrumBySlope();   // filtered spectrum to test against
                specPlotVm.Spectrum = spectrum;

                await MsPathFinderTest.SleepMillisecondsAsync(1000);

                // check unfiltered spectrum
                specPlotVm.ShowFilteredSpectrum = false;

                await MsPathFinderTest.SleepMillisecondsAsync(1000);

                var spectrumSeries = specPlotVm.PlotModel.Series[0] as StemSeries;
                Assert.False(spectrumSeries == null);
                Assert.True(spectrumSeries.Points.Count == spectrum.Peaks.Length);  // should be the same length
                for (var i = 0; i < spectrumSeries.Points.Count; i++)
                {
                    // compare each peak in spectrum plot to actual spectrum
                    Assert.True(spectrumSeries.Points[i].X.Equals(spectrum.Peaks[i].Mz));
                    Assert.True(spectrumSeries.Points[i].Y.Equals(spectrum.Peaks[i].Intensity));
                }

                await MsPathFinderTest.SleepMillisecondsAsync(1000);

                // check filtered spectrum
                specPlotVm.ShowFilteredSpectrum = true;

                await MsPathFinderTest.SleepMillisecondsAsync(1000);

                var filteredSeries = (StemSeries)specPlotVm.PlotModel.Series[0];
                Assert.True(filteredSeries.Points.Count == filteredSpectrum.Peaks.Length);   // should be the same length
                for (var i = 0; i < filteredSeries.Points.Count; i++)
                {
                    // compare each peak in spectrum plot to actual filtered spectrum
                    Assert.True(filteredSeries.Points[i].X.Equals(filteredSpectrum.Peaks[i].Mz));
                    Assert.True(filteredSeries.Points[i].Y.Equals(filteredSpectrum.Peaks[i].Intensity));
                }
            }
        }

        /// <summary>
        /// This test checks to see if the peak list exporter is working correctly.
        /// </summary>
        /// <param name="rawFile"></param>
        /// <param name="idFile"></param>
        [TestCase(@"\\proto-2\UnitTest_Files\LcMsSpectator\TopDown\Anil\QC_Shew_IntactProtein_new_CID-30CE-4Sep14_Bane_C2Column_3.raw",
                  @"\\proto-2\UnitTest_Files\LcMsSpectator\TopDown\Anil\QC_Shew_IntactProtein_new_CID-30CE-4Sep14_Bane_C2Column_3_IcTda.tsv")]
        [Category("PNL_Domain")]
        [Category("Long_Running")]
        public void TestMassExportPeakLists(string rawFile, string idFile)
        {
            // Read files
            var lcms = PbfLcMsRun.GetLcMsRun(rawFile);
            var idFileReader = IdFileReaderFactory.CreateReader(idFile);
            var ids = idFileReader.Read().ToList();
            foreach (var id in ids)
            {
                id.LcMs = lcms;
                id.RawFileName = Path.GetFileNameWithoutExtension(rawFile);
            }

            var outputDir = MsPathFinderTest.CreateTestResultDirectory("ExporterTest");

            var exporter = new SpectrumPeakExporter(outputDir.FullName);
            exporter.Export(ids.ToList());
        }

}
}
