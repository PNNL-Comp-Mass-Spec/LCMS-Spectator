using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.Readers;
using LcmsSpectator.Utils;
using NUnit.Framework;
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
        [TestCase(@"\\proto-2\unitTest_Files\LcMsSpectator\TopDown\Anil\QC_Shew_IntactProtein_new_CID-30CE-4Sep14_Bane_C2Column_3.raw",
                  @"\\proto-2\unitTest_Files\LcMsSpectator\TopDown\Anil\QC_Shew_IntactProtein_new_CID-30CE-4Sep14_Bane_C2Column_3_IcTda.tsv")]
        // bottom up (dia) data
        // [TestCase(@"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.raw",
        //           @"\\protoapps\UserData\Wilkins\BottomUp\DIA_10mz\data\Q_2014_0523_50_10_fmol_uL_10mz.tsv")]
        // bottom up (dda) data
        // [TestCase(@"\\protoapps\UserData\Wilkins\BottomUp\DDA\Q_2014_0523_1_0_amol_uL_DDA.raw",
        //           @"\\protoapps\UserData\Wilkins\BottomUp\DDA\Q_2014_0523_1_0_amol_uL_DDA_IcTda.tsv")]
        [Category("PNL_Domain")]
        [Category("Long_Running")]
        public void TestIsotopePeakAlignment(string rawFilePath, string idFilePath)
        {
            var idFileReader = IdFileReaderFactory.CreateReader(idFilePath);
            var lcms = PbfLcMsRun.GetLcMsRun(rawFilePath);
            var ids = idFileReader.Read();
            var idList = ids.ToList();

            var rawFileName = Path.GetFileNameWithoutExtension(rawFilePath);
            foreach (var id in idList)
            {
                id.LcMs = lcms;
                id.RawFileName = rawFileName;
            }

            var prsms = idList.Where(prsm => prsm.Sequence.Count > 0);

            const double relIntThres = 0.1;
            var tolerance = new Tolerance(10, ToleranceUnit.Ppm);
            var toleranceValue = tolerance.GetValue();

            const int maxCharge = 15;
            var ionTypeFactory = new IonTypeFactory(maxCharge);
            var ionTypes = ionTypeFactory.GetAllKnownIonTypes().ToArray();

            var psmsValidated = 0;
            var ionsValidated = 0;
            var validationErrors = new List<string>();

            foreach (var prsm in prsms)
            {
                foreach (var ionType in ionTypes)
                {
                    var composition = prsm.Sequence.Aggregate(Composition.Zero, (current, aa) => current + aa.Composition);
                    var ion = ionType.GetIon(composition);
                    var observedPeaks = prsm.Ms2Spectrum.GetAllIsotopePeaks(ion, tolerance, relIntThres);
                    if (observedPeaks == null)
                        continue;

                    var errors = IonUtils.GetIsotopePpmError(observedPeaks, ion, relIntThres);
                    foreach (var error in errors)
                    {
                        if (error == null) continue;
                        if (error > toleranceValue)
                        {
                            validationErrors.Add(string.Format("In scan {0}, PSM {1} has error {2:F1} for ion at {3} m/z",
                                                               prsm.Scan, prsm.SequenceText, error, ion.GetIsotopeMz(0)));
                        }

                        ionsValidated++;
                    }
                }

                psmsValidated++;
            }

            Console.WriteLine("Validated {0:N0} ions for {1:N0} PSMs", ionsValidated, psmsValidated);

            if (validationErrors.Count <= 0)
                return;

            var validationMsg = string.Format("{0} ions had errors greater than {1} ppm", validationErrors.Count, tolerance);
            Console.WriteLine(validationMsg);
            foreach (var item in validationErrors.Take(10))
            {
                Console.WriteLine(item);
            }

            Assert.Fail(validationMsg);
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
