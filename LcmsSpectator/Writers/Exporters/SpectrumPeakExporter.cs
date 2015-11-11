using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LcmsSpectator.Writers.Exporters
{
    using System.IO;
    using System.Windows.Input;
    using System.Windows.Shapes;

    using InformedProteomics.Backend.Data.Sequence;
    using InformedProteomics.Backend.Data.Spectrometry;
    using InformedProteomics.Backend.Utils;

    using LcmsSpectator.Models;
    using LcmsSpectator.PlotModels;

    using Path = System.IO.Path;

    public class SpectrumPeakExporter
    {
        private readonly IEnumerable<IonType> ionTypes;

        private readonly string outputFile;

        private readonly HashSet<BaseIonType> baseIonTypes;

        private readonly Tolerance tolerance;

        public SpectrumPeakExporter(string outputFile, IEnumerable<BaseIonType> baseIonTypes = null, Tolerance tolerance = null)
        {
            this.outputFile = outputFile;
            baseIonTypes = baseIonTypes ?? new HashSet<BaseIonType> { BaseIonType.B, BaseIonType.Y };
            this.baseIonTypes = new HashSet<BaseIonType>();
            this.baseIonTypes.UnionWith(baseIonTypes);

            var ionTypeFactory = new IonTypeFactory(100);
            var allIonTypes = ionTypeFactory.GetAllKnownIonTypes();
            this.ionTypes = allIonTypes.Where(ionType => this.baseIonTypes.Contains(ionType.BaseIonType));
            this.tolerance = tolerance ?? new Tolerance(10, ToleranceUnit.Ppm);
        }

        public void Export(IList<PrSm> ids, IProgress<ProgressData> progress = null)
        {
            progress = progress ?? new Progress<ProgressData>();
            var progressData = new ProgressData();
            int i = 1;
            foreach (var id in ids)
            {
                var outPath = Path.Combine(this.outputFile, string.Format("Scan_{0}.tsv", id.Scan));
                this.Export(id, outPath);
                progress.Report(progressData.UpdatePercent((100.0 * i++) / ids.Count));
            }
        }

        public async Task ExportAsync(IList<PrSm> ids, IProgress<ProgressData> progress = null)
        {
            progress = progress ?? new Progress<ProgressData>();
            var progressData = new ProgressData();
            int i = 1;
            foreach (var id in ids)
            {
                var outPath = Path.Combine(this.outputFile, string.Format("Scan_{0}.tsv", id.Scan));
                await this.ExportAsync(id, outPath);
                progress.Report(progressData.UpdatePercent((100.0 * i++) / ids.Count));
            }
        }

        public async Task ExportAsync(Spectrum spectrum, PeakDataPoint[] peakDataPoints, string outputFilePath)
        {
            using (var writer = new StreamWriter(outputFilePath))
            {
                await writer.WriteLineAsync(this.GetHeaders());
                foreach (var peak in spectrum.Peaks)
                {
                    var match = await Task.Run(() => this.GetMatch(peak, peakDataPoints));
                    await writer.WriteLineAsync(this.GetLine(match));
                }
            }
        }

        public void Export(Spectrum spectrum, PeakDataPoint[] peakDataPoints, string outputFilePath)
        {
            using (var writer = new StreamWriter(outputFilePath))
            {
                writer.WriteLine(this.GetHeaders());
                foreach (var peak in spectrum.Peaks)
                {
                    var match = this.GetMatch(peak, peakDataPoints);
                    writer.WriteLine(this.GetLine(match));
                }
            }
        }

        public void Export(PrSm id, string outputFilePath)
        {
            using (var writer = new StreamWriter(outputFilePath))
            {
                var matches = this.GetMappedPeaks(id);
                writer.WriteLine(this.GetHeaders());
                foreach (var match in matches)
                {
                    writer.WriteLine(this.GetLine(match)); 
                }
            }
        }

        public async Task ExportAsync(PrSm id, string outputFilePath)
        {
            using (var writer = new StreamWriter(outputFilePath))
            {
                var matches = await this.GetMappedPeaksAsync(id);
                await writer.WriteLineAsync(this.GetHeaders());
                foreach (var match in matches)
                {
                    await writer.WriteLineAsync(this.GetLine(match));
                }
            }
        }

        private async Task<IEnumerable<Tuple<Peak, PeakDataPoint>>> GetMappedPeaksAsync(PrSm id)
        {
            return await Task.Run(() => this.GetMappedPeaks(id));
        }

        private IEnumerable<Tuple<Peak, PeakDataPoint>> GetMappedPeaks(PrSm id)
        {
            var lcms = id.LcMs;
            var fragSequence = id.GetFragmentationSequence();
            var fragments = lcms.GetMsLevel(id.Scan) == 2 ? 
                            fragSequence.GetFragmentLabels(this.ionTypes.Where(ionType => ionType.Charge <= id.Charge).ToList()) :
                            fragSequence.GetChargePrecursorLabels();
            var spectrum = lcms.GetSpectrum(id.Scan, true);

            // Get the fragment peaks
            var fragmentPeaks = fragments.SelectMany(fragment => fragment.GetPeaks(spectrum, false, false))
                                         .OrderBy(peakDataPoint => peakDataPoint.X)
                                         .ToArray();

            // Set the correct residue for the fragment peaks
            foreach (var peak in fragmentPeaks)
            {
                peak.Residue = this.GetResidue(id.Sequence, peak.Index, peak.IonType.IsPrefixIon);
            }

            return spectrum.Peaks.Select(peak => this.GetMatch(peak, fragmentPeaks));
        }

        private Tuple<Peak, PeakDataPoint> GetMatch(Peak peak, PeakDataPoint[] fragmentPeaks)
        {
            var peakDataPoint = new PeakDataPoint(peak.Mz, peak.Intensity, 0, 0, string.Empty);
            var index = Array.BinarySearch(fragmentPeaks, peakDataPoint, new PeakDataPoint.MzComparer());
            PeakDataPoint matchedPeak = null;
            if (index >= 0)
            {
                matchedPeak = fragmentPeaks[index];
            }

            return new Tuple<Peak, PeakDataPoint>(peak, matchedPeak);
        }

        private string GetLine(Tuple<Peak, PeakDataPoint> match)
        {
            string line;
            var peak = match.Item1;
            var peakDataPoint = match.Item2;

            if (peakDataPoint != null)
            {
                line = string.Format(
                                     "{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}",
                                     peak.Mz,
                                     peak.Intensity,
                                     this.GetIonName(peakDataPoint.IonType, peakDataPoint.Index),
                                     peakDataPoint.IonType.Charge,
                                     peakDataPoint.Residue,
                                     peakDataPoint.Error,
                                     peakDataPoint.Correlation);
            }
            else
            {
                line = string.Format("{0}\t{1}", peak.Mz, peak.Intensity);
            }

            return line;
        }

        private string GetHeaders()
        {
            return "M/Z\tIntensity\tIon\tCharge\tResidue\tError(ppm)\tCorrelation";
        }

        private string GetIonName(IonType ionType, int index)
        {
            return string.Format("{0}{1}", ionType.BaseIonType.Symbol, index);
        }

        private char GetResidue(Sequence sequence, int index, bool isPrefix)
        {
            var sequenceIndex = isPrefix ? index - 1 : sequence.Count - index;
            return sequence[sequenceIndex].Residue;
        }
    }
}
