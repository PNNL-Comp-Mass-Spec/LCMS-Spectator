using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.Readers;
using LcmsSpectator.Utils;
using NUnit.Framework;
using OxyPlot;
using Splat;

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
            var ids = idFileReader.Read();
            ids.SetLcmsRun(lcms, Path.GetFileNameWithoutExtension(rawFilePath));

            var prsms = ids.IdentifiedPrSms;

            const double relIntThres = 0.1;
            var tolerance = new Tolerance(10, ToleranceUnit.Ppm);
            const int maxCharge = 15;
            var ionTypeFactory = new IonTypeFactory(maxCharge);
            var ionTypes = ionTypeFactory.GetAllKnownIonTypes().ToArray();
            foreach (var prsm in prsms)
            {
                foreach (var ionType in ionTypes)
                {
                    var composition = prsm.Sequence.Aggregate(Composition.Zero, (current, aa) => current + aa.Composition);
                    var ion = ionType.GetIon(composition);
                    var observedPeaks = prsm.Ms2Spectrum.GetAllIsotopePeaks(ion, tolerance, relIntThres);
                    if (observedPeaks == null) continue;
                    var errors = IonUtils.GetIsotopePpmError(observedPeaks, ion, relIntThres);
                    foreach (var error in errors)
                    {
                        if (error == null) continue;
                        Assert.True(error <= tolerance.GetValue());
                    }
                }
            }
        }

        [Test]
        public void TestITraqMod()
        {
            var aminoAcidSet = new AminoAcidSet();
            var p = aminoAcidSet.GetAminoAcid('P');
            var a = aminoAcidSet.GetAminoAcid('A');
            var q = aminoAcidSet.GetAminoAcid('Q');

            var itraqMod = Modification.Itraq4Plex;
            Console.WriteLine(itraqMod.Mass);

            var modp = new ModifiedAminoAcid(p, itraqMod);
            var sequence = new Sequence(new List<AminoAcid> {modp, a, q});
            Console.WriteLine(sequence.Mass);
        }

        [Test]
        public void TestCacheWithSingleKey()
        {
            var results = new List<int>();
            var cache = new MemoizingMRUCache<Composition, int>((a,b) => { results.Add(0); return 0; }, 10);

            var composition = new Composition(3, 6, 0, 0, 0);
            Assert.True(results.Count == 0);
            cache.Get(composition);
            Assert.True(results.Count == 1);
            var composition2 = new Composition(3, 6, 0, 0, 0);
            cache.Get(composition2);
            Assert.True(results.Count == 1);
        }

        [Test]
        public void TestCacheWithTupleKey()
        {
            var results = new List<int>();
            var cache = new MemoizingMRUCache<Tuple<Composition, IonType>, int>((a, b) => { results.Add(0); return 0; }, 10);

            var ionTypeFactory = new IonTypeFactory(10);

            var composition = new Composition(3, 6, 0, 0, 0);
            var ionType = ionTypeFactory.GetIonType("b");
            var tuple = new Tuple<Composition, IonType>(composition, ionType);
            Assert.True(results.Count == 0);

            cache.Get(tuple);

            Assert.True(results.Count == 1);

            var tuple2 = new Tuple<Composition, IonType>(composition, ionType);

            cache.Get(tuple2);

            Assert.True(results.Count == 1);
        }
    }
}
