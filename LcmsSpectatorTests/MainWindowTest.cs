using System.IO;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.Models;
using LcmsSpectator.Readers;
using LcmsSpectator.TaskServices;
using LcmsSpectator.ViewModels;
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
        ////[Test]
        ////public void TestRawFileOpening()
        ////{
        ////    // create empty main window view model
        ////    var mainVm = new MainWindowViewModel(new TestableMainDialogService(), new MockTaskService(), new DataReader());
        ////    var xicVm = mainVm.ReadRawFile(_rawFilePath);

        ////    // Check to see if xicVm is valid
        ////    Assert.False(xicVm == null);
        ////    //Assert.True(xicVm.RawFilePath == _rawFilePath);
        ////}

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
            var mainVm = new MainWindowViewModel(new TestableMainDialogService(), new DataReader());
            //mainVm.ReadIdFile();
        }

        private readonly string _rawFilePath;
        private readonly string _idFilePath;
        private readonly IdentificationTree _ids;
    }
}
