using System;
using System.IO;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.Models;
using LcmsSpectator.Readers;
using LcmsSpectator.ViewModels;
using LcmsSpectatorTests.DialogServices;
using NUnit.Framework;

namespace LcmsSpectatorTests
{
    using System.Linq;

    using LcmsSpectator.ViewModels.Data;
    using LcmsSpectator.ViewModels.Filters;

    [TestFixture]
    public class ScanViewTest
    {
        private const string rawFile =
            @"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.raw";

        private const string idFile =
            @"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.tsv";

        [Test]
        public void TestClearFilters()
        {
            // init test data
            var idFileReader = IdFileReaderFactory.CreateReader(idFile);
            var ids = idFileReader.Read();
            ids.SetLcmsRun(null, Path.GetFileNameWithoutExtension(rawFile));

            var prsms = ids.AllPrSms;

            // init scan vm
            var scanVm = new ScanViewModel(new TestableMainDialogService(), prsms);
            scanVm.ClearFilters();

            // add filters
            ////scanVm.AddFilter("Sequence", "EAQ");
            var sequenceFilter = scanVm.Filters.FirstOrDefault(x => x.Name == "Sequence") as MultiValueFilterViewModel;
            Assert.NotNull(sequenceFilter);
            sequenceFilter.Values.Add("EAQ");
           
            /////scanVm.AddFilter("Protein", "YLR");
            var proteinFilter =
                scanVm.Filters.FirstOrDefault(x => x.Name == "Protein Name") as MultiValueFilterViewModel;
            Assert.NotNull(proteinFilter);
            sequenceFilter.Values.Add("YLR");

            // Sanity check: there should be fewer PrSms than before
            Assert.True(scanVm.FilteredData.Length < prsms.Count);

            // Clear filters
            scanVm.ClearFilters();

            // All prsms should now be showing
            Assert.True(scanVm.FilteredData.Length == prsms.Count);
        }

        [Test]
        public void TestHideUnidentifedScans()
        {
            // init test data
            var idFileReader = IdFileReaderFactory.CreateReader(idFile);
            var ids = idFileReader.Read();
            var lcms = PbfLcMsRun.GetLcMsRun(rawFile);
            ids.SetLcmsRun(null, Path.GetFileNameWithoutExtension(rawFile));
            var scans = lcms.GetScanNumbers(2);
            foreach (var scan in scans)
            {
                ids.Add(new PrSm { Scan = scan, LcMs = lcms, RawFileName = Path.GetFileNameWithoutExtension(rawFile), Score = double.NaN});
            }

            var prsms = ids.AllPrSms;

            // init scan vm
            var scanVm = new ScanViewModel(new TestableMainDialogService(), prsms);
            scanVm.ClearFilters();

            Assert.AreEqual(prsms.Count, scanVm.FilteredData.Length);

            ////scanVm.HideUnidentifiedScans = true;

            Assert.True(prsms.Count > scanVm.FilteredData.Length);
        }
    }
}
