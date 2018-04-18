using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using PSI_Interface.IdentData;

namespace LcmsSpectatorTests
{
    using InformedProteomics.Backend.Data.Biology;
    using InformedProteomics.Backend.Data.Composition;
    using InformedProteomics.Backend.Data.Sequence;
    using InformedProteomics.Backend.Data.Spectrometry;
    using InformedProteomics.Backend.MassSpecData;

    using LcmsSpectator.Readers;

    [TestFixture]
    class ComparisonTests
    {
        [Test]
        [TestCase(@"Biodiversity_B_cereus_ATCC14579_LB_aerobic_1_17July16_Samwise_16-04-10_msgfplus.mzid.gz", @"Bacillus_cereus_ATCC14579_300MZ_infeng.tsv")]
        [TestCase(@"Biodiversity_B_cereus_T_LB_aerobic_3_17July16_Samwise_16-04-10_msgfplus.mzid.gz", @"Biodiversity_B_cereus_T_LB_aerobic_3_17July16_Samwise_16-04-10_300MZ_infeng.tsv")]
        [TestCase(@"Biodiversity_B_fragilis_CMcarb_anaerobic_01_01Feb16_Arwen_15-07-13_msgfplus.mzid.gz", @"Biodiversity_B_fragilis_CMcarb_anaerobic_01_01Feb16_Arwen_15-07-13_300MZ_infeng.tsv")]
        [Ignore("Missing data file")]
        public void TestFlashComparison(string mzidFile, string infengineResults)
        {
            //string directoryPath = @"C:\Users\wilk011\Documents\DataFiles\FlashUnitTest";
            //mzidFile = Path.Combine(directoryPath, mzidFile);
            //infengineResults = Path.Combine(directoryPath, infengineResults);

            //var mzids = this.ParseMzid(mzidFile);
            //var infengids = this.ParseInferenceEngineResults(infengineResults);

            //var missingIds = new List<Tuple<int, string>>();

            //int totalMatched = 0;
            //foreach (var mzid in mzids)
            //{
            //    if (!infengids.ContainsKey(mzid.Key))
            //    {
            //        missingIds.Add(new Tuple<int, string>(mzid.Key, mzid.Value));
            //        continue;
            //    }

            //    var infEngResults = infengids[mzid.Key];
            //    if (infEngResults.Contains(mzid.Value))
            //    {
            //        totalMatched++;
            //    }
            //    else
            //    {
            //        missingIds.Add(new Tuple<int, string>(mzid.Key, mzid.Value));
            //    }
            //}

            //string missingIdPath = Path.Combine(directoryPath, Path.ChangeExtension(infengineResults, "_missingIds.tsv"));
            //this.WriteMissingIds(missingIds, missingIdPath);

            //var result = Math.Round(100.0 * totalMatched / mzids.Count, 3);
            //Console.WriteLine(result);
        }

        [Test]
        [TestCase(@"FLASH_1_B_ATCC_VS_B_ATCC", @"1_B_ATCC_VS_B_ATCC.mclf", @"Same_missingIds.tsv")]
        [Ignore("Missing data file")]
        public void TestCountMissingIds(string flashFile, string mclfFile, string missingIdsFile)
        {
            // Resolve full file paths
            var directoryPath = @"C:\Users\wilk011\Documents\DataFiles\FlashUnitTest";
            flashFile = Path.Combine(directoryPath, flashFile);
            mclfFile = Path.Combine(directoryPath, mclfFile);
            missingIdsFile = Path.Combine(directoryPath, missingIdsFile);

            // Parse Flash File
            // mapping between query matrix index and BSF score
            var flashResults = File.ReadLines(flashFile)
                                                    .Select(line => line.Split(','))
                                                    .GroupBy(t => t[0])
                                                    .ToDictionary(
                                                        t => Convert.ToInt32(t.Key),
                                                        t => t.Select(x => Convert.ToInt32(x[2]))
                                                              .OrderByDescending(g => g).First());

            // Parse MclfFile
            // mapping between scan and BSF score
            var mclfMap = File.ReadLines(mclfFile)
                                               .Where(line => line.StartsWith("Biodiversity"))
                                               .Select(line => line.Split('\t'))
                                               .Select(l => new Tuple<int, int>(Convert.ToInt32(l[1]), Convert.ToInt32(l[2])))
                                               .Where(l => flashResults.ContainsKey(l.Item2))
                                               .ToDictionary(t => t.Item1, t => flashResults[t.Item2]);

            // Parse missing ids file
            // mapping between scan and sequence
            var missingIds = File.ReadLines(missingIdsFile)
                                                     .Select(line => line.Split('\t'))
                                                     .ToDictionary(t => Convert.ToInt32(t[0]), t => t[1]);

            // Count missing IDS with BSF score > 15
            var count = missingIds.Count(id => !mclfMap.ContainsKey(id.Key) || mclfMap[id.Key] >= 15);

            Console.WriteLine(count);
        }

        [Test]
        [TestCase(@"BC v BioDiv_uniqueFlashIds.tsv", @"Bacillus_cereus_ATCC14579_NCBI_06132016.fasta")]
        [Ignore("Missing data file")]
        public void CountFlashIdsInFasta(string uniqueIdFile, string fastaFile)
        {
            var directoryPath = @"C:\Users\wilk011\Documents\DataFiles\FlashUnitTest";
            uniqueIdFile = Path.Combine(directoryPath, uniqueIdFile);
            fastaFile = Path.Combine(directoryPath, fastaFile);

            var sequences = File.ReadLines(uniqueIdFile).Select(line => line.Split('\t')).Select(p => p[1]).ToList();
            var fastas = FastaReaderWriter.ReadFastaFile(fastaFile).ToList();

            var numUniqueInFasta = 0;
            foreach (var sequence in sequences)
            {
                if (fastas.Any(fasta => fasta.ProteinSequenceText.Contains(sequence)))
                {
                    numUniqueInFasta++;
                }
            }

            Console.WriteLine(@"{0} of {1} in {2}", numUniqueInFasta, sequences.Count, Path.GetFileNameWithoutExtension(fastaFile));
        }

        [Test]
        [TestCase(@"BC v BioDiv_missingIds.tsv", @"Bacillus_cereus_ATCC14579_NCBI_06132016.fasta")]
        [Ignore("Missing data file")]
        public void CountMissingIdsInFasta(string missingIdsFile, string fastaFile)
        {
            var directoryPath = @"C:\Users\wilk011\Documents\DataFiles\FlashUnitTest";
            missingIdsFile = Path.Combine(directoryPath, missingIdsFile);
            fastaFile = Path.Combine(directoryPath, fastaFile);

            // Parse missing ids file
            var missingProteins = new List<string[]>(); // each element of the list corresponds to an ID. Each element of the array corresponds to a protein accession
            foreach (var line in File.ReadLines(missingIdsFile))
            {
                var parts = line.Split('\t');
                if (parts.Length < 5)
                {
                    continue;
                }

                var proteins = parts[2].Split(';').Where(part => !string.IsNullOrWhiteSpace(part)).ToArray();
                missingProteins.Add(proteins);
            }

            // Parse FASTA file
            var fastas = FastaReaderWriter.ReadFastaFile(fastaFile).ToDictionary(f => f.ProteinName, f => f);

            // Count number of proteins
            int numInFasta = 0, numNotInFasta = 0;
            foreach (var proteinSet in missingProteins)
            {
                if (proteinSet.Any(protein => fastas.ContainsKey(protein)))
                {
                    numInFasta++;
                }
                else
                {
                    numNotInFasta++;
                }
            }

            Console.WriteLine("Total: "+ missingProteins.Count);
            Console.WriteLine("# In FASTA:" + numInFasta);
            Console.WriteLine("# Not in FASTA: " + numNotInFasta);
        }

        [Test]
        [Ignore("Missing data file")]
        public void CountScansTest()
        {
            var file = @"C:\Users\wilk011\Documents\DataFiles\MSPF\Ecoli_Ribosome\Ecoli_intact_UVPD-3pulse0p5mJ_05-20-2017.pbf";
            var pbfLcmsRun = PbfLcMsRun.GetLcMsRun(file);
            var deconvoluter = new Deconvoluter(1, 20, 2, 0.1, new Tolerance(10, ToleranceUnit.Ppm));
            var lcmsRunDecon = new LcmsRunDeconvoluter(pbfLcmsRun, deconvoluter, 2, 6);
            var dlcms = new DPbfLcMsRun(file, lcmsRunDecon, keepDataReaderOpen: true);
            var count = 0;
            var scans = pbfLcmsRun.GetScanNumbers(2);
            foreach (var scan in scans)
            {
                if (pbfLcmsRun.GetSpectrum(scan) is ProductSpectrum spectrum &&
                    (spectrum.Peaks.Length < 50 || spectrum.IsolationWindow.Charge == null))
                {
                    continue;
                }

                count++;
            }
            Console.WriteLine(count);
        }

        [Test]
        //[TestCase("Zero", @"Biodiversity_B_cereus_ATCC14579_LB_aerobic_1_17July16_Samwise_16-04-10_msgfplus.mzid.gz", @"0_ATCC_VS_ATCC_REGRESSION_infeng.tsv", @"Biodiversity_B_cereus_ATCC14579_LB_aerobic_1_17July16_Samwise_16-04-10.mzML.gz")]
        //[TestCase("Same", @"Biodiversity_B_cereus_ATCC14579_LB_aerobic_1_17July16_Samwise_16-04-10_msgfplus.mzid.gz", @"1_B_ATCC_VS_B_ATCC_infeng.tsv", @"Biodiversity_B_cereus_ATCC14579_LB_aerobic_1_17July16_Samwise_16-04-10.mzML.gz")]
        //[TestCase("Similar", @"Biodiversity_B_cereus_T_LB_aerobic_3_17July16_Samwise_16-04-10_msgfplus.mzid.gz", @"2_B_CER_T_VS_B_ATCC_infeng.tsv", @"Biodiversity_B_cereus_T_LB_aerobic_3_17July16_Samwise_16-04-10.mzML.gz")]
        //[TestCase("Different", @"Biodiversity_B_fragilis_CMcarb_anaerobic_01_01Feb16_Arwen_15-07-13_msgfplus.mzid.gz", @"3_B_THETA_VS_B_ATCC_infeng.tsv", @"Biodiversity_B_fragilis_CMcarb_anaerobic_01_01Feb16_Arwen_15-07-13.mzML.gz")]
        //[TestCase("BC v BioDiv", @"Biodiversity_B_cereus_ATCC14579_LB_aerobic_1_17July16_Samwise_16-04-10_msgfplus.mzid.gz", @"4_B_ATCC_VS_BIODIV_infeng.tsv", @"Biodiversity_B_cereus_ATCC14579_LB_aerobic_1_17July16_Samwise_16-04-10.mzML.gz")]
        //[TestCase("Human v BC", @"CPTAC3_harmonization_P33_07_18Apr17_Samwise_REP_17-02-02_msgfplus.mzid.gz", @"5_HUMAN_VS_B_ATCC_infeng.tsv", @"CPTAC3_harmonization_P33_07_18Apr17_Samwise_REP_17-02-02.mzML")]
        [TestCase("Human v BioDiv", @"CPTAC3_harmonization_P33_07_18Apr17_Samwise_REP_17-02-02_msgfplus.mzid.gz", @"6_HUMAN_VS_BIODIV_infeng.tsv", @"CPTAC3_harmonization_P33_07_18Apr17_Samwise_REP_17-02-02.mzML")]
        [Ignore("Missing data file")]
        public void TestFlashSpectrumBased(string testCaseTitle, string mzidFile, string infengineResults, string mzmlFile)
        {
            var directoryPath = @"C:\Users\wilk011\Documents\DataFiles\FlashUnitTest";
            mzidFile = Path.Combine(directoryPath, mzidFile);
            infengineResults = Path.Combine(directoryPath, infengineResults);
            mzmlFile = Path.Combine(directoryPath, mzmlFile);

            var mzids = ParseMzid(mzidFile);
            var infengids = ParseInferenceEngineResults(infengineResults);

            var idsInBoth = new HashSet<int>();
            var idsWithDifferentAnnotation = new HashSet<int>();

            // Calculate msgf ids that aren't in infeng
            var msgfUniqueIds = new List<Tuple<int, SimpleMZIdentMLReader.SpectrumIdItem>>();
            foreach (var id in mzids)
            {
                if (infengids.ContainsKey(id.Key))
                {
                    var infEng = infengids[id.Key];
                    if (infEng.Contains(id.Value.Peptide.Sequence))
                    {
                        idsInBoth.Add(id.Key);
                    }
                    else
                    {
                        idsWithDifferentAnnotation.Add(id.Key);
                    }
                }
                else
                {
                    msgfUniqueIds.Add(new Tuple<int, SimpleMZIdentMLReader.SpectrumIdItem>(id.Key, id.Value));
                }
            }

            // Calculate infeng ids that aren't in mzid
            var infEngOnlyCount = 0;
            var infEngUniqueIds = new Dictionary<int, HashSet<string>>();
            foreach (var id in infengids)
            {
                if (!mzids.ContainsKey(id.Key))
                {
                    infEngOnlyCount += id.Value.Count;
                    infEngUniqueIds.Add(id.Key, id.Value);
                }
            }

            var filteredMsgfIds = FilterByPpm(msgfUniqueIds, mzmlFile);
            var fileName = string.Format("{0}_missingIds.tsv", testCaseTitle);
            var filePath = Path.Combine(directoryPath, fileName);
            WriteMissingIds(filteredMsgfIds, filePath);

            var flashOnlyIdFile = string.Format("{0}_uniqueFlashIds.tsv", testCaseTitle);
            var uniqueIdFilePath = Path.Combine(directoryPath, flashOnlyIdFile);
            WriteUniqueFlashEntries(infEngUniqueIds, uniqueIdFilePath);

            Console.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", testCaseTitle, filteredMsgfIds.Count, idsInBoth.Count, idsWithDifferentAnnotation.Count, infEngOnlyCount);
        }

        private List<SimpleMZIdentMLReader.SpectrumIdItem> FilterByPpm(List<Tuple<int, SimpleMZIdentMLReader.SpectrumIdItem>> ids, string mzmlPath)
        {
            int notProductSpectrum = 0, chargeNotFound = 0, ppmGreaterThan10 = 0;
            var keepList = new List<SimpleMZIdentMLReader.SpectrumIdItem>();
            var lcmsRun = PbfLcMsRun.GetLcMsRun(mzmlPath);
            foreach (var id in ids)
            {
                if (!(lcmsRun.GetSpectrum(id.Item1) is ProductSpectrum spectrum))
                {
                    notProductSpectrum++;
                    continue;
                }
                if (spectrum.IsolationWindow?.MonoisotopicMz == null)
                {
                    chargeNotFound++;
                    continue;
                }
                //if (spectrum?.IsolationWindow?.MonoisotopicMz == null) continue;
                var isolationMz = spectrum.IsolationWindow.MonoisotopicMz.Value;
                var sequence = new Sequence(id.Item2.Peptide.Sequence, new AminoAcidSet());
                var ion = new Ion(sequence.Composition + Composition.H2O, id.Item2.Charge);
                var ionMz = ion.GetMonoIsotopicMz();
                var ppm = Math.Abs(ionMz - isolationMz) / ionMz * 1e6;
                if (ppm > 10)
                {
                    ppmGreaterThan10++;
                    continue;
                }
                keepList.Add(id.Item2);
            }

            Console.WriteLine("Not Product Spectrum: "+ notProductSpectrum);
            Console.WriteLine("Charge Not Found: "+ chargeNotFound);
            Console.WriteLine("ERROR > 10PPM: " + ppmGreaterThan10);

            return keepList;
        }

        private void WriteMissingIds(List<SimpleMZIdentMLReader.SpectrumIdItem> mzids, string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            {
                foreach (var id in mzids)
                {
                    var proteins = id.PepEvidence.Select(pepEv => pepEv.DbSeq.Accession).Aggregate(string.Empty, (l, r) => l + ";" + r);
                    writer.WriteLine(id.ScanNum + "\t" + id.Peptide.Sequence + "\t" + proteins + "\t" + id.SpecEv + "\t" + id.QValue);
                }
            }
        }

        private void WriteUniqueFlashEntries(Dictionary<int, HashSet<string>> flashEntries, string filePath)
        {
            using (var file = new StreamWriter(filePath))
            {
                foreach (var entry in flashEntries)
                {
                    foreach (var sequence in entry.Value)
                    {
                        file.WriteLine($"{entry.Key}\t{sequence}");
                    }
                }
            }
        }

        private Dictionary<int, SimpleMZIdentMLReader.SpectrumIdItem> ParseMzid(string mzid)
        {
            var mzIdReader = new SimpleMZIdentMLReader();
            var ids = mzIdReader.Read(mzid).Identifications
                .Where(id => id.QValue <= 1e-5)
                .Where(id => id.SpecEv <= 1e-10)
                .Where(id => id.PepEvidence.Any(pepEv => !pepEv.IsDecoy))
                .Where(id => id.Peptide.Mods.Count == 0)
                .GroupBy(id => id.ScanNum);

            var mzids = new Dictionary<int, SimpleMZIdentMLReader.SpectrumIdItem>();
            foreach (var idGroup in ids)
            {
                var id = idGroup.OrderBy(sp => sp.SpecEv).First();
                mzids.Add(id.ScanNum, id);
            }

            return mzids;
        }

        private class InferenceEngineResult
        {
            public string Sequence { get; set; }
            public int BSFScore { get; set; }
        }

        private Dictionary<int, HashSet<string>> ParseInferenceEngineResults(string infengine)
        {
            var results = new Dictionary<int, HashSet<string>>();
            var count = 0;
            foreach (var line in File.ReadLines(infengine))
            {
                if (count++ == 0)
                {
                    continue;
                }

                var parts = line.Split('\t');
                if (parts.Length < 8)
                {
                    continue;
                }

                var scan = Convert.ToInt32(parts[2]);
                var annotation = parts[3];

                if (!results.ContainsKey(scan))
                {
                    results.Add(scan, new HashSet<string>());
                }

                results[scan].Add(annotation);
            }

            return results;
        }
    }
}
