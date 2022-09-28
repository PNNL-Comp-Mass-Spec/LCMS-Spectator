using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace LcmsSpectatorTests
{
    using System.Collections.Concurrent;
    using System.IO;

    using InformedProteomics.Backend.Data.Sequence;
    using InformedProteomics.Backend.Data.Spectrometry;
    using InformedProteomics.Backend.MassSpecData;

    [TestFixture]
    public class CreateFigures
    {
        private const string datasetDir = @"\\proto-2\unitTest_Files\InformedProteomics_TestFiles\TopDownTraining";

        private const string outputFileDir = @"\\proto-2\unitTest_Files\InformedProteomics_TestFiles\TopDownTraining\plotFiles";

        [Test]
        [Category("PNL_Domain")]
        public void WritePlots()
        {
            var plotFileDir = new DirectoryInfo(outputFileDir);

            if (!plotFileDir.Exists)
            {
                Assert.Ignore("Skipping, directory not found: " + outputFileDir);
            }

            var resultFiles = plotFileDir.GetFiles("result*.tsv");
            if (resultFiles.Length == 0)
            {
                Assert.Ignore("Skipping, result .tsv files not found in " + outputFileDir);
            }

            foreach (var resultFile in resultFiles)
            {

                var results = new List<ResultLine>();
                foreach (var line in File.ReadLines(resultFile.FullName))
                {
                    var parts = line.Split('\t');
                    if (parts.Length < 5) continue;

                    results.Add(new ResultLine
                    {
                        Corr = Convert.ToDouble(parts[0]),
                        Cosine = Convert.ToDouble(parts[1]),
                        Error = Convert.ToDouble(parts[2]),
                        Intensity = Convert.ToDouble(parts[3]),
                        IsTarget = Convert.ToInt32(parts[4]) == 1 ? true : false,
                    });
                }

                // Corr Histogram
                var corrHistTarget = new Dictionary<int, int>();
                var corrHistDecoy = new Dictionary<int, int>();
                foreach (var result in results)
                {
                    var corrHist = result.IsTarget ? corrHistTarget : corrHistDecoy;
                    var roundedCorr = (int)(Math.Round(result.Corr, 1) * 10);
                    if (!corrHist.ContainsKey(roundedCorr))
                    {
                        corrHist.Add(roundedCorr, 0);
                    }

                    corrHist[roundedCorr]++;
                }

                // Corr roc
                var corrRoc = new Dictionary<int, List<double>>();
                for (var i = 0; i < 10; i++)
                {
                    if (!corrHistTarget.TryGetValue(i, out var targetCount))
                        targetCount = 0;

                    if (!corrHistDecoy.TryGetValue(i, out var decoyCount))
                        decoyCount = 0;

                    var divisor = decoyCount + targetCount;
                    if (divisor == 0)
                        continue;

                    var fpr = (int)Math.Round(decoyCount / (double)divisor * 100);
                    var tpr = targetCount / (double)divisor * 100;

                    if (corrRoc.TryGetValue(fpr, out var tprList))
                    {
                        tprList.Add(tpr);
                    }
                    else
                    {
                        tprList = new List<double>() {tpr};
                        corrRoc.Add(fpr, tprList);
                    }
                }

                Console.WriteLine();
                Console.WriteLine("ROC Curve data for " + resultFile.Name);
                Console.WriteLine("{0}\t{1}", "FPR", "TPR");

                foreach (var fpr in from item in corrRoc.Keys orderby item select item)
                {
                    foreach (var tpr in from item in corrRoc[fpr] orderby item select item)
                    {
                        Console.WriteLine("{0}\t{1:F1}", fpr, tpr);
                    }
                }

                // Error Histogram
                var errHistTarget = new Dictionary<int, int>();
                var errHistDecoy = new Dictionary<int, int>();
                foreach (var result in results)
                {
                    var errHist = result.IsTarget ? errHistTarget : errHistDecoy;
                    var roundedErr = (int)Math.Round(result.Error);
                    if (!errHist.ContainsKey(roundedErr))
                    {
                        errHist.Add(roundedErr, 0);
                    }

                    errHist[roundedErr]++;
                }

                Console.WriteLine();
                Console.WriteLine("Target PSMs Error Histogram");
                Console.WriteLine("{0}\t{1}", "Error", "Count");

                foreach (var roundedError in from item in errHistTarget.Keys orderby item select item)
                {
                    Console.WriteLine("{0}\t{1}", roundedError, errHistTarget[roundedError]);
                }

                Console.WriteLine();
                Console.WriteLine("Decoy PSMs Error Histogram");
                Console.WriteLine("{0}\t{1}", "Error", "Count");

                foreach (var roundedError in from item in errHistDecoy.Keys orderby item select item)
                {
                    Console.WriteLine("{0}\t{1}", roundedError, errHistDecoy[roundedError]);
                }
            }
        }

        [Test]
        [Category("PNL_Domain")]
        public void CalcFeatures()
        {
            var files = Directory.GetFiles(datasetDir);
            var rawFiles = files.Where(f => f.EndsWith(".raw"));
            var targetFiles = files.Where(f => f.EndsWith("_IcTda.tsv")).ToDictionary(Path.GetFileNameWithoutExtension, f => f);
            var decoyFiles = files.Where(f => f.EndsWith("_IcDecoy.tsv")).ToDictionary(Path.GetFileNameWithoutExtension, f => f);

            var results = new ConcurrentBag<ResultLine>();

            var datasetsProcessed = 0;
            foreach (var rawFilePath in rawFiles)
            {
                var rawFile = new FileInfo(rawFilePath);
                if (rawFile.Length / 1024.0 / 1024 / 1024 > 1)
                {
                    Console.WriteLine("Skipping, {0} since over 1 GB", rawFile.FullName);
                    continue;
                }

                var idName = $"{Path.GetFileNameWithoutExtension(rawFile.Name)}_IcTda";
                var decoyName = $"{Path.GetFileNameWithoutExtension(rawFile.Name)}_IcDecoy";

                if (!targetFiles.ContainsKey(idName))
                {
                    Console.WriteLine("Skipping, _IcTda.tsv file not found for " + rawFile.FullName);
                    continue;
                }

                if (!decoyFiles.ContainsKey(decoyName))
                {
                    Console.WriteLine("Skipping, _IcDecoy.tsv file not found for " + rawFile.FullName);
                    continue;
                }

                Console.WriteLine("Processing " + rawFile.FullName);

                var scans = new Dictionary<int, DeconvolutedSpectrum>();
                var pbfRun = (PbfLcMsRun)PbfLcMsRun.GetLcMsRun(rawFile.FullName);

                var ids = ParseIdFile(targetFiles[idName], true);
                var decoys = ParseIdFile(decoyFiles[decoyName], false);
                ids.AddRange(decoys);

                foreach (var id in ids)
                {
                    DeconvolutedSpectrum spectrum;
                    if (scans.ContainsKey(id.Scan))
                    {
                        spectrum = scans[id.Scan];
                    }
                    else
                    {
                        spectrum = GetDeconvolutedSpectrum(id.Scan, pbfRun);
                        scans.Add(id.Scan, spectrum);
                    }

                    var baseIonTypes = new[] { BaseIonType.A, BaseIonType.B, BaseIonType.C, BaseIonType.X, BaseIonType.Y, BaseIonType.Z };
                    foreach (var cleavage in id.Sequence.GetInternalCleavages())
                    {
                        foreach (var baseIonType in baseIonTypes)
                        {
                            var baseComp = baseIonType.IsPrefix ? cleavage.PrefixComposition : cleavage.SuffixComposition;
                            var composition = baseComp + baseIonType.OffsetComposition;
                            var mass = composition.Mass;
                            var peak = spectrum.FindPeak(mass, new Tolerance(10, ToleranceUnit.Ppm)) as DeconvolutedPeak;
                            if (peak == null)
                            {
                                continue;
                            }
                            else
                            {
                                var result = new ResultLine
                                {
                                    Corr = peak.Corr,
                                    Cosine = peak.Dist,
                                    Error = Math.Abs(mass - peak.Mass) / peak.Mass * 1e6,
                                    Intensity = peak.Intensity,
                                    IsTarget = id.IsTarget
                                };

                                results.Add(result);
                            }
                        }
                    }
                }

                datasetsProcessed++;

                var plotFileDir = new DirectoryInfo(outputFileDir);
                if (!plotFileDir.Exists)
                    plotFileDir.Create();

                var outputFilePath = Path.Combine(plotFileDir.FullName,
                                                  string.Format("result_{0}.tsv", Path.GetFileNameWithoutExtension(rawFile.Name)));

                using (var writer = new StreamWriter(new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
                {
                    foreach (var result in results)
                    {
                        var isTarget = result.IsTarget ? 1 : 0;
                        writer.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", result.Corr, result.Cosine, result.Error, result.Intensity, isTarget);
                    }
                }

            } //foreach

            if (datasetsProcessed == 0)
                Assert.Ignore("Did not find any valid datasets in " + datasetDir);
        }

        private DeconvolutedSpectrum GetDeconvolutedSpectrum(int scan, PbfLcMsRun pbfLcMsRun)
        {
            var spectrum = pbfLcMsRun.GetSpectrum(scan) as ProductSpectrum;
            if (spectrum == null)
            {
                return null;
            }

            return Deconvoluter.GetCombinedDeconvolutedSpectrum(spectrum, 1, 20, 2, new Tolerance(10, ToleranceUnit.Ppm), 0.7);
        }

        private List<Psm> ParseIdFile(string filePath, bool isTarget)
        {
            var aminoAcidSet = new AminoAcidSet();
            var psms = new List<Psm>();
            var headers = new Dictionary<string, int>();
            var count = 0;
            foreach (var line in File.ReadLines(filePath))
            {
                var parts = line.Split('\t');
                if (parts.Length < 15)
                {
                    continue;
                }

                if (count++ == 0)
                {
                    for (var i = 0; i < parts.Length; i++)
                    {
                        headers.Add(parts[i], i);
                    }

                    continue;
                }

                var scan = Convert.ToInt32(parts[headers["Scan"]]);
                var charge = Convert.ToInt32(parts[headers["Charge"]]);
                var cleanSeq = new Sequence(parts[headers["Sequence"]], aminoAcidSet);
                var modsString = parts[headers["Modifications"]];
                foreach (var mod in modsString.Split(','))
                {
                    var modParts = mod.Split(' ');
                    if (modParts.Length < 2)
                    {
                        continue;
                    }

                    var name = modParts[0];
                    var index = Math.Min(Convert.ToInt32(modParts[1]), cleanSeq.Count - 1);
                    cleanSeq[index] = new ModifiedAminoAcid(cleanSeq[index], Modification.Get(name));
                }

                var sequence = new Sequence(cleanSeq);
                psms.Add(new Psm(scan, charge, sequence, isTarget));
            }

            return psms;
        }

        private class Psm
        {
            public Psm(int scan, int charge, Sequence sequence, bool isTarget)
            {
                Scan = scan;
                Charge = charge;
                Sequence = sequence;
                IsTarget = isTarget;
            }

            public int Scan { get; set; }

            public int Charge { get; set; }

            public Sequence Sequence { get; set; }

            public bool IsTarget { get; set; }
        }

        private class ResultLine
        {
            public double Corr { get; set; }

            public double Error { get; set; }

            public double Cosine { get; set; }

            public double Intensity { get; set; }

            public bool IsTarget { get; set; }
        }
    }
}
