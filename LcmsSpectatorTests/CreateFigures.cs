using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private const string datasetDir = @"D:\Data\TopDownTraining\UVPD_Yeast";

        private const string outputFile = @"D:\Data\TopDownTraining\UVPD_Yeast\plotFiles\results.tsv";

        [Test]
        public void WritePlots()
        {
            var results = new List<ResultLine>();
            foreach (var line in File.ReadLines(outputFile))
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
            var corrHistTar = new Dictionary<int, int>();
            var corrHistDec = new Dictionary<int, int>();
            foreach (var result in results)
            {
                var corrHist = result.IsTarget ? corrHistTar : corrHistDec;
                int roundedCorr = (int)Math.Round(result.Corr, 1) * 10;
                if (!corrHist.ContainsKey(roundedCorr))
                {
                    corrHist.Add(roundedCorr, 0);
                }

                corrHist[roundedCorr]++;
            }

            // Corr roc
            var corrRoc = new Dictionary<int, double>();
            for (int i = 0; i < 10; i++)
            {
                var fpr = (int) Math.Round(100.0*corrHistDec[i] / (corrHistDec[i] + corrHistTar[i]));
                var tpr = 100.0 * corrHistTar[i] / (corrHistDec[i] + corrHistTar[i]);
                //corrRoc.Add();
            }

            // Error Histogram
            var errHistTar = new Dictionary<int, int>();
            var errHistDec = new Dictionary<int, int>();
            foreach (var result in results)
            {
                var errHist = result.IsTarget ? errHistTar : errHistDec;
                int roundedErr = (int)Math.Round(result.Error);
                if (!errHist.ContainsKey(roundedErr))
                {
                    errHist.Add(roundedErr, 0);
                }

                errHist[roundedErr]++;
            }
        }

        [Test]
        public void CalcFeatures()
        {
            var files = Directory.GetFiles(datasetDir);
            var rawFiles = files.Where(f => f.EndsWith(".raw"));
            var targetFiles = files.Where(f => f.EndsWith("_IcTda.tsv")).ToDictionary(Path.GetFileNameWithoutExtension, f => f);
            var decoyFiles = files.Where(f => f.EndsWith("_IcDecoy.tsv")).ToDictionary(Path.GetFileNameWithoutExtension, f => f);

            var results = new ConcurrentBag<ResultLine>();

            foreach (var rawFile in rawFiles)
            {
                var scans = new Dictionary<int, DeconvolutedSpectrum>();
                PbfLcMsRun pbfRun = (PbfLcMsRun) PbfLcMsRun.GetLcMsRun(rawFile);

                var idName = $"{Path.GetFileNameWithoutExtension(rawFile)}_IcTda";
                var ids = this.ParseIdFile(targetFiles[idName], true);
                var decoyName = $"{Path.GetFileNameWithoutExtension(rawFile)}_IcDecoy";
                var decoys = this.ParseIdFile(decoyFiles[decoyName], false);
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
                        spectrum = this.GetDeconvolutedSpectrum(id.Scan, pbfRun);
                        scans.Add(id.Scan, spectrum);
                    }

                    var baseIonTypes = new[] { BaseIonType.A, BaseIonType.B, BaseIonType.C, BaseIonType.X, BaseIonType.Y, BaseIonType.Z };
                    var cleavages = id.Sequence.GetInternalCleavages();
                    foreach (var cleavage in cleavages)
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
                using (var writer = new StreamWriter(outputFile))
                {
                    foreach (var result in results)
                    {
                        int isTarget = result.IsTarget ? 1 : 0;
                        writer.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", result.Corr, result.Cosine, result.Error, result.Intensity, isTarget);
                    }
                }
            }
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
            int count = 0;
            foreach (var line in File.ReadLines(filePath))
            {
                var parts = line.Split('\t');
                if (parts.Length < 18)
                {
                    continue;
                }

                if (count++ == 0)
                {
                    for (int i = 0; i < parts.Length; i++)
                    {
                        headers.Add(parts[i], i);
                    }

                    continue;
                }

                int scan = Convert.ToInt32(parts[headers["Scan"]]);
                int charge = Convert.ToInt32(parts[headers["Charge"]]);
                Sequence cleanSeq = new Sequence(parts[headers["Sequence"]], aminoAcidSet);
                string modsString = parts[headers["Modifications"]];
                var mods = modsString.Split(',');

                foreach (var mod in mods)
                {
                    var modParts = mod.Split(' ');
                    if (modParts.Length < 2)
                    {
                        continue;
                    }

                    string name = modParts[0];
                    int index = Math.Min(Convert.ToInt32(modParts[1]), cleanSeq.Count - 1);
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
                this.Scan = scan;
                this.Charge = charge;
                this.Sequence = sequence;
                this.IsTarget = isTarget;
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
