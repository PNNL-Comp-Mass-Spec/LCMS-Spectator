// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataReader.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   The DataReader is a class that encapsulates reading raw files, ID files, and feature files.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Readers
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using InformedProteomics.Backend.Data.Sequence;

    using LcmsSpectator.Config;
    using LcmsSpectator.Models;
    using LcmsSpectator.Utils;
    using LcmsSpectator.ViewModels;
    using LcmsSpectator.ViewModels.Data;
    using ReactiveUI;

    /// <summary>
    /// The DataReader is a class that encapsulates reading raw files, ID files, and feature files.
    /// </summary>
    public class DataReader : ReactiveObject, IDataReader
    {
        /// <summary>
        /// Value indicating whether or not the data reader is currently reading raw files.
        /// </summary>
        private bool readingRawFiles;

        /// <summary>
        /// Value indicating whether or not the data reader is currently reading raw files.
        /// </summary>
        private bool readingIdFiles;

        /// <summary>
        /// Value indicating whether or not the data reader is currently reading feature files.
        /// </summary>
        private bool readingFeatureFiles;

        /// <summary>
        /// Initializes a new instance of the DataReader class. 
        /// </summary>
        public DataReader()
        {
            this.ReadingRawFiles = false;
            this.ReadingIdFiles = false;
            this.ReadingFeatureFiles = false;
            this.Modifications = new List<Modification>();
        }

        /// <summary>
        /// Gets a value indicating whether or not the data reader is currently reading raw files.
        /// </summary>
        public bool ReadingRawFiles
        {
            get { return this.readingRawFiles; }
            private set { this.RaiseAndSetIfChanged(ref this.readingRawFiles, value); }
        }

        /// <summary>
        /// Gets a value indicating whether or not the data reader is currently reading id files.
        /// </summary>
        public bool ReadingIdFiles
        {
            get { return this.readingIdFiles; }
            private set { this.RaiseAndSetIfChanged(ref this.readingIdFiles, value); }
        }

        /// <summary>
        /// Gets a value indicating whether or not the data reader is currently reading feature files.
        /// </summary>
        public bool ReadingFeatureFiles
        {
            get { return this.readingFeatureFiles; }
            private set { this.RaiseAndSetIfChanged(ref this.readingFeatureFiles, value); }
        }

        /// <summary>
        /// Open and read ID file and add IDs to data set.
        /// </summary>
        /// <param name="dataSetViewModel">DataSetViewModel to add IDs to.</param>
        /// <param name="idFilePath">Path for ID file to read.</param>
        /// <param name="modIgnoreList">Modifications to ignore in identifications.</param>
        /// <returns>Task that reads ID file.</returns>
        public async Task ReadIdFile(DataSetViewModel dataSetViewModel, string idFilePath, IEnumerable<string> modIgnoreList = null)
        {
            var reader = IdFileReaderFactory.CreateReader(idFilePath);
            var ids = await reader.ReadAsync(modIgnoreList);
            var idList = ids.ToList();
            foreach (var id in idList)
            {
                id.RawFileName = dataSetViewModel.Title;
                id.LcMs = dataSetViewModel.LcMs;
            }

            this.Modifications = reader.Modifications;
            dataSetViewModel.ScanViewModel.Data.AddRange(idList);
            dataSetViewModel.IdFileOpen = true;
        }

        /// <summary>
        /// Open a data set given raw file, id file, and feature file.
        /// </summary>
        /// <param name="dataSetViewModel">DataSetViewModel to associate open dataset with</param>
        /// <param name="rawFilePath">Path to raw file to open</param>
        /// <param name="idFilePath">Path to MS-GF+ or MS-PathFinder results file</param>
        /// <param name="featureFilePath">Path to feature list file</param>
        /// <param name="paramFilePath">Path to MSPathFinder parameter file.</param>
        /// <param name="toolType">Type of ID tool used for this data set</param>
        /// <param name="modIgnoreList">Modifications to ignore if found in ID list.</param>
        /// <returns>Task that opens the data set.</returns>
        public async Task OpenDataSet(
                                    DataSetViewModel dataSetViewModel,
                                    string rawFilePath,
                                    string idFilePath = "", 
                                    string featureFilePath = "", 
                                    string paramFilePath = "",
                                    ToolType? toolType = ToolType.MsPathFinder,
                                    IEnumerable<string> modIgnoreList = null)
        {
            // Open raw file, if not already open.
            if (!string.IsNullOrEmpty(rawFilePath) && dataSetViewModel.LcMs == null)
            {
                this.ReadingRawFiles = true;
                await dataSetViewModel.InitializeAsync(rawFilePath);
                this.ReadingRawFiles = false;
            }

            // Show neighboring charge state XICs by default for MSPathFinder results
            if (toolType != null && toolType == ToolType.MsPathFinder)
            {
                var precFragSeq = dataSetViewModel.XicViewModel.PrecursorPlotViewModel.FragmentationSequenceViewModel as PrecursorSequenceIonViewModel;
                if (precFragSeq != null)
                {
                    precFragSeq.PrecursorViewMode = PrecursorViewMode.Charges;
                }
            }

            // Open ID file
            if (!string.IsNullOrEmpty(idFilePath))
            {
                this.ReadingIdFiles = true;

                if (dataSetViewModel.MsPfParameters == null)
                {
                    dataSetViewModel.SetMsPfParameters(string.IsNullOrWhiteSpace(paramFilePath) ? idFilePath : paramFilePath);   
                }

                await this.ReadIdFile(dataSetViewModel, idFilePath, modIgnoreList);
                this.ReadingIdFiles = false;
            }

            // Open feature file
            if (!string.IsNullOrEmpty(featureFilePath))
            {
                this.ReadingFeatureFiles = true;
                dataSetViewModel.FeatureMapViewModel.OpenFeatureFile(featureFilePath);
                dataSetViewModel.FeatureMapViewModel.UpdateIds(dataSetViewModel.ScanViewModel.FilteredData);
                this.ReadingRawFiles = false;
            }
        }

        /// <summary>
        /// Get raw files in a given directory.
        /// This method is not recursive.
        /// </summary>
        /// <param name="directoryPath">Path for directory to search for raw files in.</param>
        /// <param name="datasetName">
        /// Path of file to search for raw files in the same directory.
        /// This is the full path.
        /// </param>
        /// <returns>IEnumerable of raw file names.</returns>
        public List<string> GetRawFilesByDataSetName(string directoryPath, string datasetName)
        {
            var rawFiles = new List<string>();
            var directory = Directory.GetFiles(directoryPath);
            foreach (var filePath in directory)
            {
                // Raw file in same directory as tsv file?
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                rawFiles.AddRange(from extension in FileConstants.RawFileExtensions 
                                  where fileName == datasetName && filePath.EndsWith(extension, true, CultureInfo.InvariantCulture) 
                                  select filePath);
            }

            return rawFiles;
        }

        /// <summary>
        /// Reads a FASTA database file.
        /// </summary>
        /// <param name="filePath">The path to the FASTA database file.</param>
        /// <returns>Task that returns IEnumerable of FASTA entries.</returns>
        public Task<IEnumerable<FastaEntry>> ReadFastaFile(string filePath)
        {
            return Task.Run(() => FastaReaderWriter.ReadFastaFile(filePath));
        }

        public IList<Modification> Modifications { get; private set; }
    }
}
