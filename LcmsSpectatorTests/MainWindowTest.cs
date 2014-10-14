﻿using System.IO;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.TaskServices;
using LcmsSpectator.ViewModels;
using LcmsSpectatorModels.Models;
using LcmsSpectatorModels.Readers;
using LcmsSpectatorTests.DialogServices;
using NUnit.Framework;

namespace LcmsSpectatorTests
{
    [TestFixture(@"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.raw",
                 @"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.tsv")]
    public class MainWindowTest
    {
        public MainWindowTest(string rawFilePath, string idFilePath)
        {
            _rawFilePath = rawFilePath;
            _idFilePath = idFilePath;
            var idFileReader = IdFileReaderFactory.CreateReader(idFilePath);
            var lcms = PbfLcMsRun.GetLcMsRun(rawFilePath, MassSpecDataType.XCaliburRun);
            _ids = idFileReader.Read();
            _ids.SetLcmsRun(lcms, Path.GetFileNameWithoutExtension(rawFilePath));
        }

        /// <summary>
        /// This test checks to see if a valid rawfile is being opened and the LcMsRun object for
        /// the raw file is valid.
        /// </summary>
        [Test]
        public void TestRawFileOpening()
        {
            // create empty main window view model
            var mainVm = new MainWindowViewModel(new TestableMainDialogService(), new MockTaskService());
            var xicVm = mainVm.ReadRawFile(_rawFilePath);

            // Check to see if xicVm is valid
            Assert.False(xicVm == null);
            Assert.True(xicVm.RawFilePath == _rawFilePath);
        }

        // TODO: Test for MSPF IDs
        /// <summary>
        /// This test attempts to open a MS-PathFinder TSV file and ensures that the number of IDs
        /// are equal to number of lines in the TSV file.
        /// </summary>
        [Test]
        public void TestMsPathFinderFileOpening()
        {
            // open ms-path finder tsv file
            var file = File.ReadAllLines(_idFilePath);
            var numLines = file.Length - 1; // subtract one for TSV header

            // create MainWindowViewModel
            var mainVm = new MainWindowViewModel(new TestableMainDialogService(), new MockTaskService());
            //mainVm.ReadIdFile();
        }

        /// <summary>
        /// This test checks to see if toggling ShowUnidentifiedScans correctly shows/hides
        /// unidentified scans.
        /// </summary>
        [Test]
        public void TestUnidentifiedScanToggle()
        {
            // create ID Tree of prsms without scan #s
            var idTree = new IdentificationTree();
            var prsm = new PrSm {Scan = 1, RawFileName = Path.GetFileNameWithoutExtension(_rawFilePath)};
            idTree.Add(prsm);

            // create MainWindowViewModel with unidentified scans showing
            var mainVm = new MainWindowViewModel(new TestableMainDialogService(), new MockTaskService(), idTree);
            mainVm.ScanViewModel.HideUnidentifiedScans = false;
            
            // Should be showing one prsm
            Assert.True(mainVm.ScanViewModel.FilteredData.Count == 1);

            // toggle off unidentified scans
            mainVm.ScanViewModel.HideUnidentifiedScans = true;

            // Should be showing 0 prsms
            Assert.True(mainVm.ScanViewModel.FilteredData.Count == 0);
        }

        private readonly string _rawFilePath;
        private readonly string _idFilePath;
        private readonly IdentificationTree _ids;
    }
}
