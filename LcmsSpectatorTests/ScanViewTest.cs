using System;
using System.IO;
using GalaSoft.MvvmLight.Messaging;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.TaskServices;
using LcmsSpectator.ViewModels;
using LcmsSpectatorModels.Models;
using LcmsSpectatorModels.Readers;
using LcmsSpectatorTests.DialogServices;
using NUnit.Framework;

namespace LcmsSpectatorTests
{
    [TestFixture]
    public class ScanViewTest
    {
        private const string rawFile =
            @"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.raw";

        private const string idFile =
            @"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.tsv";

        // use filter values known to be in data
        [TestCase(new []{ "Sequence" }, new []{ "EAQ" })]
        [TestCase(new []{ "Protein"}, new []{"YLR"})]
        [TestCase(new[] { "PrecursorMz" }, new[] { "400" })]
        [TestCase(new[] { "Charge" }, new[] { "1" })]
        [TestCase(new[] { "Charge" }, new[] { "2" })]
        [TestCase(new[] { "Charge" }, new[] { "15" })]
        [TestCase(new[] { "Score" }, new[] { "1" })]
        [TestCase(new[] { "Score" }, new[] { "15" })]
        [TestCase(new[] { "QValue" }, new[] { "0.01" })]
        [TestCase(new[] { "QValue" }, new[] { "0.1" })]
        [TestCase(new[] { "RawFile" }, new[] { "Q_2014_0523" })]
        [TestCase(new[] { "Sequence", "Protein", "Charge" }, new[] { "EAQ", "YLR", "2" })]
        public void TestFilter(string[] filters, string[] filterValues)
        {
            Assert.True(filters.Length == filterValues.Length, "Must have the same number of filters and filter values.");
            // init test data
            var idFileReader = IdFileReaderFactory.CreateReader(idFile);
            var ids = idFileReader.Read();
            ids.SetLcmsRun(null, Path.GetFileNameWithoutExtension(rawFile));

            var prsms = ids.AllPrSms;

            // add a prsm from some other raw file for testing raw file filter
            prsms.Add(new PrSm { RawFileName = "asdf"});

            // init scan vm
            var scanVm = new ScanViewModel(new TestableMainDialogService(), new MockTaskService(), prsms, Messenger.Default);
            scanVm.ClearFilters();

            // Add filters
            for (int i = 0; i < filters.Length; i++)
            {
                scanVm.AddFilter(filters[i], filterValues[i]);
            }

            // Sanity check: there should be fewer PrSms than before
            Assert.True(scanVm.FilteredData.Count < prsms.Count);

            var data = scanVm.FilteredData;
            for (int i = 0; i < filters.Length; i++)
            {
                switch (filters[i])
                {
                    case "Sequence":
                        foreach (var prsm in data) Assert.True(prsm.SequenceText.StartsWith(filterValues[i]));
                        break;
                    case "Protein":
                        foreach (var prsm in data) Assert.True(prsm.ProteinName.StartsWith(filterValues[i]));
                        break;
                    case "PrecursorMz":
                        var mz = Convert.ToDouble(filterValues[i]);
                        foreach (var prsm in data) Assert.True(prsm.PrecursorMz >= mz);
                        break;
                    case "Charge":
                        var charge = Convert.ToInt32(filterValues[i]);
                        foreach (var prsm in data) Assert.True(prsm.Charge == charge);
                        break;
                    case "Score":
                        var score = Convert.ToDouble(filterValues[i]);
                        foreach (var prsm in data) Assert.True(prsm.Score >= score); // using MS-PathFinder scoring (higher is better)
                        break;
                    case "QValue":
                        var qValue = Convert.ToDouble(filterValues[i]);
                        foreach (var prsm in data) Assert.True(prsm.QValue <= qValue);
                        break;
                    case "RawFile":
                        foreach (var prsm in data) Assert.True(prsm.RawFileName.StartsWith(filterValues[i]));
                        break;
                }
            }
        }

        [Test]
        public void TestClearFilters()
        {
            // init test data
            var idFileReader = IdFileReaderFactory.CreateReader(idFile);
            var ids = idFileReader.Read();
            ids.SetLcmsRun(null, Path.GetFileNameWithoutExtension(rawFile));

            var prsms = ids.AllPrSms;

            // init scan vm
            var scanVm = new ScanViewModel(new TestableMainDialogService(), new MockTaskService(), prsms, Messenger.Default);
            scanVm.ClearFilters();

            // add filters
            scanVm.AddFilter("Sequence", "EAQ");
            scanVm.AddFilter("Protein", "YLR");

            // Sanity check: there should be fewer PrSms than before
            Assert.True(scanVm.FilteredData.Count < prsms.Count);

            // Clear filters
            scanVm.ClearFilters();

            // All prsms should now be showing
            Assert.True(scanVm.FilteredData.Count == prsms.Count);
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
            foreach (var scan in scans) ids.Add(new PrSm { Scan = scan, Lcms = lcms, RawFileName = Path.GetFileNameWithoutExtension(rawFile), Score = Double.NaN});
            var prsms = ids.AllPrSms;

            // init scan vm
            var scanVm = new ScanViewModel(new TestableMainDialogService(), new MockTaskService(), prsms, Messenger.Default);
            scanVm.ClearFilters();

            Assert.True(prsms.Count == scanVm.FilteredData.Count);

            scanVm.HideUnidentifiedScans = true;

            Assert.True(prsms.Count > scanVm.FilteredData.Count);
        }
    }
}
