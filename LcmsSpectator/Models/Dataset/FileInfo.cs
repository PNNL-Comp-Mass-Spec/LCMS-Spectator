// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FileInfo.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <summary>
//   Enumeration of all the file types that are supported by the application.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.IO;
using InformedProteomics.Backend.MassSpecData;

namespace LcmsSpectator.Models.Dataset
{
    using System.Linq;
    using LcmsSpectator.Utils;
    
    /// <summary>
    /// Enumeration of all the file types that are supported by the application.
    /// </summary>
    public enum FileTypes
    {
        /// <summary>
        /// Represents a spectrum file (*.MZML, *.RAW)
        /// </summary>
        SpectrumFile,

        /// <summary>
        /// Represents a identification results file (*.MZID, or .tsv - MSPF/MS-GF+ results)
        /// </summary>
        IdentificationFile,

        /// <summary>
        /// Represents a feature finder results file (*.MS1FT - ProMex)
        /// </summary>
        FeatureFile,

        /// <summary>
        /// Represents a FASTA database file. (*.fasta)
        /// </summary>
        FastaFile,

        /// <summary>
        /// Represents a MSPathFinder parameter file. (*.param)
        /// </summary>
        ParamFile,

        /// <summary>
        /// Represents a file with unknown file type.
        /// </summary>
        Unknown,
    }

    /// <summary>
    /// A class that represents the information for a single file.
    /// </summary>
    public class FileInfo
    {
        /// <summary>
        /// Gets or sets the path to the file.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the type of file.
        /// </summary>
        public FileTypes FileType { get; set; }

        /// <summary>
        /// Get a FileInfo object for a given file path.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        /// <returns>FileInfo with identified file type.</returns>
        public static FileInfo GetFileInfo(string filePath)
        {
            var extension = System.IO.Path.GetExtension(filePath);
            var fileInfo = new FileInfo { FilePath = filePath, FileType = FileTypes.Unknown };

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                throw new FileNotFoundException(string.Format("Cannot find file {0}", filePath));
            }


            if (string.IsNullOrEmpty(extension))
            {
                fileInfo.FileType = FileTypes.Unknown;
                return fileInfo;
            }

            extension = extension.ToLower();
            if (MassSpecDataReaderFactory.MassSpecDataTypeFilterList.Contains(extension))
            {
                fileInfo.FileType = FileTypes.SpectrumFile;
                fileInfo.FilePath = MassSpecDataReaderFactory.NormalizeDatasetPath(filePath);
            }
            else if (FileConstants.IdFileExtensions.Contains(extension))
            {
                fileInfo.FileType = FileTypes.IdentificationFile;
            }
            else if (FileConstants.FeatureFileExtensions.Contains(extension))
            {
                fileInfo.FileType = FileTypes.FeatureFile;
            }
            else if (extension != null && extension.ToLower() == ".fasta")
            {
                fileInfo.FileType = FileTypes.FastaFile;
            }

            return fileInfo;
        }
    }
}
