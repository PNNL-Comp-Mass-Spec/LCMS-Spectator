using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.Data.Spectrometry;
using InformedProteomics.Backend.MassSpecData;
using PRISM;

namespace LcmsSpectator.Readers
{
    /// <summary>
    /// Provides an abstraction to allow viewing spectrum data without creating a PBF file, but also permit creating/using one for the XIC and Feature views
    /// </summary>
    public class DatasetReaderWrapper : ILcMsRun
    {
        private string datasetPath;
        private string pbfPath = "";
        private ISpectrumAccessor specReader;
        private ILcMsRun xicReader = null;

        public bool IsXicDataAvailable => xicReader != null;

        /// <summary>
        /// Open up the necessary files
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="progress"></param>
        public DatasetReaderWrapper(string filePath, IProgress<ProgressData> progress = null)
        {
            filePath = MassSpecDataReaderFactory.NormalizeDatasetPath(filePath);
            datasetPath = filePath;
            // Conditions: If a pbf file exists (and is valid), always open it. If the file is a Thermo .raw file (or is associated with a present Thermo .raw file), also open the raw file for reading extra peak data (i.e., noise)
            if (filePath.EndsWith(".pbf", StringComparison.OrdinalIgnoreCase))
            {
                pbfPath = filePath;
                var testRawPath = Path.ChangeExtension(filePath, "raw");
                if (File.Exists(testRawPath))
                {
                    datasetPath = testRawPath;
                }
            }
            else
            {
                var testPbfPath = PbfLcMsRun.GetPbfFileName(filePath);
                if (File.Exists(testPbfPath) && PbfLcMsRun.CheckFileFormatVersion(testPbfPath, out _))
                {
                    pbfPath = testPbfPath;
                    if (filePath.EndsWith(".raw", StringComparison.OrdinalIgnoreCase) && File.Exists(filePath))
                    {
                        datasetPath = filePath;
                    }
                    else
                    {
                        datasetPath = pbfPath;
                    }
                }
            }

            // Got files figured out, open them up.
            if (pbfPath.Equals(datasetPath))
            {
                xicReader = PbfLcMsRun.GetLcMsRun(pbfPath, 0, 0, progress);
                specReader = xicReader;
            }
            else
            {
                var progressData = new ProgressData(progress);
                if (!string.IsNullOrWhiteSpace(pbfPath))
                {
                    progressData.StepRange(50);
                }

                progressData.Status = "Caching scan metadata...";
                specReader = MassSpecDataReaderFactory.GetMassSpecDataAccessor(datasetPath, new Progress<ProgressData>(x => progressData.Report(x.Percent)));
                if (!string.IsNullOrWhiteSpace(pbfPath))
                {
                    progressData.StepRange(100);
                    progressData.Status = "Opening PBF file...";
                    xicReader = PbfLcMsRun.GetLcMsRun(pbfPath, 0, 0, new Progress<ProgressData>(x => progressData.Report(x.Percent)));
                }
            }
        }

        public async void GeneratePbfFileIfNeeded(IProgress<ProgressData> progress = null)
        {
            await GeneratePbfFileIfNeededAsync(progress);
        }

        public async Task GeneratePbfFileIfNeededAsync(IProgress<ProgressData> progress = null)
        {
            if (xicReader != null)
            {
                return;
            }

            pbfPath = PbfLcMsRun.GetPbfFileName(datasetPath);
            //xicReader = PbfLcMsRun.GetLcMsRun(datasetPath, 0, 0, progress);
            xicReader = await Task.Run(() => PbfLcMsRun.GetLcMsRun(datasetPath, 0, 0, progress)).ConfigureAwait(false);

            // If the original file is a Thermo .raw file, continue reading it directly for spectra; otherwise, close the specReader and set it to xicReader.
            if (!datasetPath.EndsWith(".raw", StringComparison.OrdinalIgnoreCase))
            {
                // TODO: implement some kind of lock here to prevent disposal of old specReader while still in use; using the 1-second delay will lead to occasional race conditions, but will fix most cases
                var oldReader = specReader;
                specReader = xicReader;
                datasetPath = pbfPath;
                Thread.Sleep(1000); // delay 1 second, let existing uses of specReader (hopefully) exit.
                oldReader.Dispose();
            }
        }

        private void CheckXicReader()
        {
            // Avoid exceptions - painful to do this just because a function that needs it was called,
            // but doing this will avoid the need to find all usages and implement exception handling
            if (xicReader == null)
            {
                // TODO: Some kind of prompt and progress bar implementation...
                GeneratePbfFileIfNeeded();
            }
        }

        #region ISpectrumAccessor Methods

        /// <inheritdoc />
        public Spectrum GetSpectrum(int scanNum, bool includePeaks = true)
        {
            return specReader.GetSpectrum(scanNum, includePeaks);
        }

        /// <inheritdoc />
        public double GetElutionTime(int scanNum)
        {
            return specReader.GetElutionTime(scanNum);
        }

        /// <inheritdoc />
        public int GetMsLevel(int scanNum)
        {
            return specReader.GetMsLevel(scanNum);
        }

        /// <inheritdoc />
        public int GetPrevScanNum(int scanNum, int msLevel)
        {
            return specReader.GetPrevScanNum(scanNum, msLevel);
        }

        /// <inheritdoc />
        public int GetNextScanNum(int scanNum, int msLevel)
        {
            return specReader.GetNextScanNum(scanNum, msLevel);
        }

        /// <inheritdoc />
        public IList<int> GetScanNumbers(int msLevel)
        {
            return specReader.GetScanNumbers(msLevel);
        }

        /// <inheritdoc />
        public IsolationWindow GetIsolationWindow(int scanNum)
        {
            return specReader.GetIsolationWindow(scanNum);
        }

        /// <inheritdoc />
        public int MinLcScan => specReader.MinLcScan;

        /// <inheritdoc />
        public int MaxLcScan => specReader.MaxLcScan;

        /// <inheritdoc />
        public int NumSpectra => specReader.NumSpectra;

        #endregion

        #region ILcmsRun Methods

        /// <inheritdoc />
        public bool IsDia
        {
            get
            {
                if (xicReader != null)
                {
                    return xicReader.IsDia;
                }

                return false;
            }
        }

        /// <inheritdoc />
        public int[] GetFragmentationSpectraScanNums(Ion precursorIon)
        {
            CheckXicReader();

            return xicReader.GetFragmentationSpectraScanNums(precursorIon);
        }

        /// <inheritdoc />
        public int[] GetFragmentationSpectraScanNums(double mostAbundantIsotopeMz)
        {
            CheckXicReader();

            return xicReader.GetFragmentationSpectraScanNums(mostAbundantIsotopeMz);
        }

        #endregion

        #region IChromatogramExtractor Methods

        /// <inheritdoc />
        public Xic GetFullPrecursorIonExtractedIonChromatogram(double mz, Tolerance tolerance)
        {
            CheckXicReader();

            return xicReader.GetFullPrecursorIonExtractedIonChromatogram(mz, tolerance);
        }

        /// <inheritdoc />
        public Xic GetFullPrecursorIonExtractedIonChromatogram(double minMz, double maxMz)
        {
            CheckXicReader();

            return xicReader.GetFullPrecursorIonExtractedIonChromatogram(minMz, maxMz);
        }

        /// <inheritdoc />
        public Xic GetFullProductExtractedIonChromatogram(double mz, Tolerance tolerance, double precursorIonMz)
        {
            CheckXicReader();

            return xicReader.GetFullProductExtractedIonChromatogram(mz, tolerance, precursorIonMz);
        }

        /// <inheritdoc />
        public Xic GetFullProductExtractedIonChromatogram(double minMz, double maxMz, double precursorIonMz)
        {
            CheckXicReader();

            return xicReader.GetFullProductExtractedIonChromatogram(minMz, maxMz, precursorIonMz);
        }

        #endregion

        /// <inheritdoc />
        public void Dispose()
        {
            // Avoid double-disposal of a single object
            if (!ReferenceEquals(specReader, xicReader))
            {
                specReader?.Dispose();
            }
            xicReader?.Dispose();
        }
    }
}
