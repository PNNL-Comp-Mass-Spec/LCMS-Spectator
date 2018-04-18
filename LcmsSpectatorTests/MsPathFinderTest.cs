using System;
using System.Collections.Generic;
using System.IO;

namespace LcmsSpectatorTests
{
    using InformedProteomics.Backend.Data.Composition;
    using InformedProteomics.Backend.Data.Enum;
    using InformedProteomics.Backend.Data.Sequence;
    using InformedProteomics.Backend.Data.Spectrometry;
    using InformedProteomics.Backend.MassSpecData;

    using NUnit.Framework;

    [TestFixture]
    public class MsPathFinderTest
    {

        /// <summary>
        /// Create a directory in the user's temp directory to hold test result files
        /// </summary>
        /// <param name="testCategory"></param>
        /// <param name="deleteExistingFiles"></param>
        /// <returns></returns>
        public static DirectoryInfo CreateTestResultDirectory(string testCategory, bool deleteExistingFiles = false)
        {
            string dirPath;
            if (string.IsNullOrWhiteSpace(testCategory))
                dirPath = Path.Combine(Path.GetTempPath(), "TestResults");
            else
                dirPath = Path.Combine(Path.GetTempPath(), "TestResults", testCategory);

            var outputDir = new DirectoryInfo(dirPath);
            if (!outputDir.Exists)
            {
                outputDir.Create();
            }

            if (!deleteExistingFiles)
                return outputDir;

            try
            {
                foreach (var item in outputDir.GetFileSystemInfos("*"))
                {
                    item.Delete();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleting files in {0}: {1}", outputDir.FullName, ex);
            }

            return outputDir;
        }

        [Test]
        [Ignore("Missing data file")]
        public void TestIanCidData()
        {
            const string specFilePath =
                @"\\protoapps\userdata\Wilkins\UIMF Files\9 pep mix 365 mTorr with ims 45V CID\9 pep mix 365 mTorr with ims 45V CID.UIMF";
            const string fastaFilePath = @"\\protoapps\userdata\Wilkins\UIMF Files\melittin.fasta";
            const string outputDirectory =
                @"\\protoapps\userdata\Wilkins\UIMF Files\9 pep mix 365 mTorr with ims 45V CID";

            const double correlationThreshold = 0.7;

            // Add missing modifications

            // Initialize search modifications
            var searchModifications = new List<SearchModification> { };
            var aminoAcidSet = new AminoAcidSet(searchModifications, 1);

            // Initialize spectrum file
            var lcmsRun = PbfLcMsRun.GetLcMsRun(specFilePath);
            var scanNumbers = lcmsRun.GetScanNumbers(2);

            // Initialize MSPathFinder
            //var launcher = new IcTopDownLauncher(
            //    specFilePath,
            //    fastaFilePath,
            //    outputDirectory,
            //    aminoAcidSet)
            //{
            //    MinSequenceLength = 1,
            //    MaxSequenceLength = 100,
            //    MaxNumNTermCleavages = 1,
            //    MaxNumCTermCleavages = 0,
            //    MinPrecursorIonCharge = 1,
            //    MaxPrecursorIonCharge = 20,
            //    MinProductIonCharge = 1,
            //    MaxProductIonCharge = 20,
            //    MinSequenceMass = 1,
            //    MaxSequenceMass = 30000,
            //    PrecursorIonTolerancePpm = 100,
            //    ProductIonTolerancePpm = 100,
            //    RunTargetDecoyAnalysis = DatabaseSearchMode.Both,
            //    SearchMode = InternalCleavageType.NoInternalCleavage,
            //    MaxNumThreads = 4,
            //    ScanNumbers = scanNumbers,
            //    NumMatchesPerSpectrum = 1,
            //    TagBasedSearch = false,
            //};

            //launcher.RunSearch(correlationThreshold);
        }

        [TestCase(176, "GIGAVLKVLTTGLPALISWIKRKRQQ")]
        [Ignore("Missing data file")]
        public void ScorePSM(int scan, string sequenceStr)
        {
            // Set input file paths.
            const string specFilePath =
                @"\\protoapps\userdata\Wilkins\UIMF Files\9 pep mix 365 mTorr with ims 45V CID\9 pep mix 365 mTorr with ims 45V CID.pbf";

            var lcmsRun = PbfLcMsRun.GetLcMsRun(specFilePath);

            var productSpectrum = lcmsRun.GetSpectrum(scan) as ProductSpectrum;
            Assert.NotNull(productSpectrum);

            var modification = Modification.RegisterAndGetModification("oh->nh2", new Composition(0, 1, 1, -1, 0));
            Assert.NotNull(modification);

            var searchModifications = new List<SearchModification>
            {
                new SearchModification(Modification.Get("oh->nh2"), 'M', SequenceLocation.ProteinCTerm, true),
                new SearchModification(Modification.Get("oh->nh2"), 'Q', SequenceLocation.ProteinCTerm, true),
                new SearchModification(Modification.PyroGluE, 'R', SequenceLocation.ProteinNTerm, true)
            };

            var aminoAcidSet = new AminoAcidSet(searchModifications, 1);
            var sequence = LoadSequence(sequenceStr, aminoAcidSet);

            //var scorerFactory = new ScorerFactory(new Tolerance(30, ToleranceUnit.Ppm), 1, 5);
            //var scorer = scorerFactory.GetScorer(productSpectrum);

            //var score = IonUtils.ScoreSequence(scorer, sequence);

            //Console.WriteLine(score);
        }

        [TestCase(0.7, 1)]
        [Ignore("Missing data file")]
        public void BruteForceSearch(double minCorrelation, int idsPerSpectrum)
        {
        //    // Set input file paths.
        //    const string specFilePath =
        //        @"\\protoapps\userdata\Wilkins\UIMF Files\9 pep mix 365 mTorr with ims 45V CID\9 pep mix 365 mTorr with ims 45V CID.pbf";
        //    const string fastaFilePath = @"\\protoapps\userdata\Wilkins\UIMF Files\melittin.fasta";
        //    const string outputDirectory =
        //        @"\\protoapps\userdata\Wilkins\UIMF Files\9 pep mix 365 mTorr with ims 45V CID";
        //    const string paramFilePath =
        //        @"\\protoapps\userdata\Wilkins\UIMF Files\9 pep mix 365 mTorr with ims 45V CID\9 pep mix 365 mTorr with ims 45V CID.param";

        //    // Create output file path
        //    var specFileName = Path.GetFileNameWithoutExtension(specFilePath);
        //    var fastaFileName = Path.GetFileNameWithoutExtension(fastaFilePath);
        //    string outputFilePath = Path.Combine(outputDirectory, string.Format("{0}_{1}.tsv", specFileName, fastaFileName));

        //    // Read input files
        //    var parameters = MsPfParameters.ReadFromFile(paramFilePath);
        //    var lcmsRun = PbfLcMsRun.GetLcMsRun(specFilePath);
        //    var fastaEntries = FastaReaderWriter.ReadFastaFile(fastaFilePath).ToList();

        //    // Initialize components for scoring
        //    var aminoAcidSet = new AminoAcidSet(parameters.Modifications, parameters.MaxDynamicModificationsPerSequence);
        //    var scorerFactory = new ScorerFactory(
        //                              parameters.ProductIonTolerancePpm,
        //                              parameters.MinProductIonCharge,
        //                              parameters.MaxProductIonCharge,
        //                              minCorrelation);

        //    var scans = lcmsRun.GetScanNumbers(2);
        //    var scanToIdsMap = scans.ToDictionary(scan => scan, scan => new List<SearchIdentification>());

        //    // Score all scans against all sequences.
        //    foreach (var scan in scans)
        //    {
        //        // Get spectrum
        //        var productSpectrum = lcmsRun.GetSpectrum(scan) as ProductSpectrum;
        //        if (productSpectrum == null)
        //        {
        //            continue;
        //        }

        //        // Get scorer
        //        var scorer = scorerFactory.GetScorer(productSpectrum);

        //        foreach (var entry in fastaEntries)
        //        {
        //            // Get sequence
        //            var sequence = this.LoadSequence(entry.ProteinSequenceText, aminoAcidSet);

        //            // Finally score the spectrum against the sequence
        //            var score = IonUtils.ScoreSequence(scorer, sequence);

        //            if (scan == 176 && entry.ProteinName == "Melittin")
        //            {
        //                Console.WriteLine();
        //            }

        //            if (score > 0.0)
        //            {
        //                scanToIdsMap[scan].Add(new SearchIdentification
        //                {
        //                    Scan = scan,
        //                    ProteinName = entry.ProteinName,
        //                    ProteinDesc = entry.ProteinDescription,
        //                    Sequence = sequence,
        //                    Score = score
        //                });
        //            }
        //        }
        //    }

        //    // Remove redundant IDs
        //    foreach (var scanIds in scanToIdsMap)
        //    {
        //        if (scanIds.Value.Count > idsPerSpectrum)
        //        {
        //            var alterIds = new List<SearchIdentification>(scanIds.Value);
        //            var actualIds = alterIds.OrderByDescending(id => id.Score).Take(idsPerSpectrum).ToList();
        //            scanToIdsMap[scanIds.Key].Clear();
        //            scanToIdsMap[scanIds.Key].AddRange(actualIds);
        //        }
        //    }

        //    // Write results
        //    var ids = scanToIdsMap.SelectMany(scanIds => scanIds.Value);
        //    using (var writer = new StreamWriter(outputFilePath))
        //    {
        //        writer.WriteLine("Scan\tSequence\tProtein\tDescription\tScore\tFdr\tQValue");
        //        foreach (var id in ids)
        //        {
        //            writer.WriteLine(
        //                             "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}",
        //                             id.Scan,
        //                             this.GetSequenceString(id.Sequence),
        //                             id.ProteinName,
        //                             id.ProteinDesc,
        //                             id.Score,
        //                             id.Fdr,
        //                             id.Qvalue);
        //        }
        //    }
        }

        private string GetSequenceString(Sequence sequence)
        {
            var sequenceStr = string.Empty;
            foreach (var aa in sequence)
            {
                sequenceStr += aa.Residue;
                if (aa is ModifiedAminoAcid)
                {
                    var modAa = aa as ModifiedAminoAcid;
                    sequenceStr += "[" + modAa.Modification.Name + "]";
                }
            }

            return sequenceStr;
        }

        private Sequence LoadSequence(string sequenceStr, AminoAcidSet aminoAcidSet)
        {
            var sequence = new List<AminoAcid>();
            for (var i = 0; i < sequenceStr.Length; i++)
            {
                SequenceLocation location;
                if (i == 0)
                {
                    location = SequenceLocation.ProteinNTerm;
                }
                else if (i == sequenceStr.Length - 1)
                {
                    location = SequenceLocation.ProteinCTerm;
                }
                else
                {
                    location = SequenceLocation.Everywhere;
                }

                var aa = aminoAcidSet.GetAminoAcid(sequenceStr[i], location);
                sequence.Add(aa);
            }

            return new Sequence(sequence);
        }

        private class SearchIdentification
        {
            public int Scan { get; set; }
            public string ProteinName { get; set; }
            public string ProteinDesc { get; set; }
            public Sequence Sequence { get; set; }
            public double Score { get; set; }
            public double Fdr { get; set; }
            public double Qvalue { get; set; }
        }
    }
}
