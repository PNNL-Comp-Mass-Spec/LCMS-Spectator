using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectatorModels.Config;
using LcmsSpectatorModels.Models;
using LcmsSpectatorModels.Readers;
using NUnit.Framework;

namespace LcmsSpectatorTests
{
    [TestFixture]
    public class IonUtilsTest
    {
        /// <summary>
        /// This test checks the observed isotope envelope vs the theoretical to ensure that
        /// they are aligned.
        /// </summary>
        // top down data
        [TestCase(@"\\protoapps\UserData\Wilkins\TopDown\Anil\QC_Shew_IntactProtein_new_CID-30CE-4Sep14_Bane_C2Column_3.raw", 
                  @"\\protoapps\UserData\Wilkins\TopDown\Anil\QC_Shew_IntactProtein_new_CID-30CE-4Sep14_Bane_C2Column_3_IcTda.tsv")]
        // bottom up (dia) data
        [TestCase(@"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.raw", 
                  @"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.tsv")]
        // bottom up (dda) data
        [TestCase(@"\\protoapps\UserData\Wilkins\BottomUp\DDA\Q_2014_0523_1_0_amol_uL_DDA.raw",
                  @"\\protoapps\UserData\Wilkins\BottomUp\DDA\Q_2014_0523_1_0_amol_uL_DDA_IcTda.tsv")]
        public void TestIsotopePeakAlignment(string rawFilePath, string idFilePath)
        {
            var idFileReader = IdFileReaderFactory.CreateReader(idFilePath);
            var lcms = PbfLcMsRun.GetLcMsRun(rawFilePath, MassSpecDataType.XCaliburRun);
            var ids = idFileReader.Read(lcms, Path.GetFileNameWithoutExtension(rawFilePath));

            var prsms = ids.IdentifiedPrSms;

            const double relIntThres = 0.1;

            const int maxCharge = 15;   // 15 is the maximum charge that MS-PathFinder reports
            var ionTypeFactory = new IonTypeFactory(maxCharge);
            var ionTypes = ionTypeFactory.GetAllKnownIonTypes().ToArray();
            foreach (var prsm in prsms)
            {
                foreach (var ionType in ionTypes)
                {
                    var composition = prsm.Sequence.Aggregate(Composition.Zero, (current, aa) => current + aa.Composition);
                    var ion = ionType.GetIon(composition);
                    
                    var observedPeaks = prsm.Ms2Spectrum.GetAllIsotopePeaks(ion, new Tolerance(10, ToleranceUnit.Ppm), relIntThres);
                }
            }
        }
    }
}
