// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FileConstants.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Contains constants for format strings for opening data sets from open file dialogs.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Utils
{
    /// <summary>
    /// Contains constants for format strings for opening data sets from open file dialogs.
    /// </summary>
    public class FileConstants
    {
        /// <summary>
        /// Format string for selecting MSPF parameter files from open file dialogs.
        /// </summary>
        public const string ParamFileFormatString = @"MSPF Parameter Files (*.param)|*.param";

        /// <summary>
        /// Format string for selecting ID/database search result files from open file dialogs.
        /// </summary>
        public const string IdFileFormatString =
            @"Supported Files|*.txt;*.tsv;*.mzId;*.mzId.gz;*.mtdb|TSV Files (*.txt; *.tsv)|*.txt;*.tsv|MzId Files (*.mzId[.gz])|*.mzId;*.mzId.gz|MTDB Files (*.mtdb)|*.mtdb";

        /// <summary>
        /// Format string for selecting FASTA database files from open file dialogs.
        /// </summary>
        public const string FastaFileFormatString = @"Fasta DB Files (*.fasta)|*.fasta";

        /// <summary>
        /// Format string for selecting raw and MZML files from open file dialogs.
        /// </summary>
        public const string RawFileFormatString =
            @"Supported Files|*.raw;*.mzML;*.mzML.gz|Raw Files (*.raw)|*.raw|MzMl Files (*.mzMl[.gz])|*.mzMl;*.mzML.gz";

        /// <summary>
        /// Format string for selecting feature files from open file dialogs.
        /// </summary>
        public const string FeatureFileFormatString = @"Ms1FT Files (*.ms1ft)|*.ms1ft";

        /// <summary>
        /// Extensions for raw and MZML files.
        /// </summary>
        public static readonly string[] RawFileExtensions = { ".raw", ".mzml", ".mzml.gz" };

        /// <summary>
        /// Extensions for ID files.
        /// </summary>
        public static readonly string[] IdFileExtensions = { ".tsv", ".txt", ".mzid", ".mzid.gz", ".mtdb" };

        /// <summary>
        /// Extensions for feature files.
        /// </summary>
        public static readonly string[] FeatureFileExtensions = { ".ms1ft" };
    }
}
