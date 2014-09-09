using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.ViewModels;
using LcmsSpectatorModels.Config;
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
            var lcms = RafLcMsRun.GetLcMsRun(rawFilePath, MassSpecDataType.XCaliburRun);
            _ids = idFileReader.Read(lcms, Path.GetFileNameWithoutExtension(rawFilePath));
        }

        /// <summary>
        /// This test checks to see if a valid rawfile is being opened and the LcMsRun object for
        /// the raw file is valid.
        /// </summary>
        [Test]
        public void TestRawFileOpening()
        {
            // create empty main window view model
            var mainVm = new MainWindowViewModel(new TestableMainDialogService());
            var xicVm = mainVm.ReadRawFile(_rawFilePath);

            // Check to see if xicVm is valid
            Assert.False(xicVm == null);
            Assert.True(xicVm.RawFilePath == _rawFilePath);
        }

        /// <summary>
        /// This test checks to see if IDs are being filtered by QValue.
        /// The input should be a TSV file with IDs of QValue greater than the QValue threshold
        /// set in the application settings.
        /// </summary>
        [Test]
        public void TestQValueFilter()
        {
            // create main view model and pass it IDs
            // MainWindowViewModel should automatically filter IDs by qvalue
            var mainVm = new MainWindowViewModel(new TestableMainDialogService(), _ids);
            var qValueThresh = IcParameters.Instance.QValueThreshold;

            var prsms = mainVm.PrSms;
            // Check QValue filteration
            foreach (var prsm in prsms)
            {
                Assert.True(prsm.QValue <= qValueThresh);
            }
        }

        private string _rawFilePath;
        private string _idFilePath;
        private readonly IdentificationTree _ids;
    }
}
