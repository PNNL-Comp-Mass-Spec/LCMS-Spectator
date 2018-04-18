// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IDataReader.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   The DataReader is an interface for reading raw files, ID files, and feature files.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Readers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using InformedProteomics.Backend.Data.Sequence;

    using Models;
    using ViewModels.Data;

    /// <summary>
    /// The DataReader is an interface for reading raw files, ID files, and feature files.
    /// </summary>
    public interface IDataReader
    {
        /// <summary>
        /// Gets a value indicating whether or not the data reader is currently reading raw files.
        /// </summary>
        bool ReadingRawFiles { get; }

        /// <summary>
        /// Gets a value indicating whether or not the data reader is currently reading id files.
        /// </summary>
        bool ReadingIdFiles { get; }

        /// <summary>
        /// Gets a value indicating whether or not the data reader is currently reading feature files.
        /// </summary>
        bool ReadingFeatureFiles { get; }

        /// <summary>
        /// Open and read ID file and add IDs to data set.
        /// </summary>
        /// <param name="dataSetViewModel">DataSetViewModel to add IDs to.</param>
        /// <param name="idFilePath">Path for ID file to read.</param>
        /// <param name="modIgnoreList">Modifications to ignore in identifications.</param>
        /// <returns>Task that reads ID file.</returns>
        Task ReadIdFile(DataSetViewModel dataSetViewModel, string idFilePath, IEnumerable<string> modIgnoreList = null);

        /// <summary>
        /// Open a data set given raw file, id file, and feature file.
        /// </summary>
        /// <param name="dataSetViewModel">DataSetViewModel to associate open dataset with.</param>
        /// <param name="rawFilePath">Path to raw file to open.</param>
        /// <param name="idFilePath">Path to MS-GF+ or MS-PathFinder results file.</param>
        /// <param name="featureFilePath">Path to feature list file.</param>
        /// <param name="paramFilePath">Path to MSPathFinder parameter file.</param>
        /// <param name="toolType">Type of ID tool used for this data set.</param>
        /// <param name="modIgnoreList">Modifications to ignore if found in ID list.</param>
        /// <returns>Task that opens the data set.</returns>
        Task OpenDataSet(
                        DataSetViewModel dataSetViewModel,
                        string rawFilePath,
                        string idFilePath = "",
                        string featureFilePath = "",
                        string paramFilePath = "",
                        ToolType? toolType = ToolType.MsPathFinder,
                        IEnumerable<string> modIgnoreList = null);

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
        List<string> GetRawFilesByDataSetName(string directoryPath, string datasetName);

        /// <summary>
        /// Reads a FASTA database file.
        /// </summary>
        /// <param name="filePath">The path to the FASTA database file.</param>
        /// <returns>Task that returns IEnumerable of FASTA entries.</returns>
        Task<IEnumerable<FastaEntry>> ReadFastaFile(string filePath);

        IList<Modification> Modifications { get; }
    }
}
