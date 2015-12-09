// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MsPfParameters.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   A class representing a parsed MSPathFinder parameter file.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Models
{
    using System;
    using System.IO;
    using InformedProteomics.Backend.Data.Composition;
    using InformedProteomics.Backend.Data.Enum;
    using InformedProteomics.Backend.Data.Sequence;
    using InformedProteomics.Backend.Data.Spectrometry;
    using LcmsSpectator.Config;
    using ReactiveUI;
    
    /// <summary>
    /// A class representing a parsed MSPathFinder parameter file.
    /// </summary>
    public class MsPfParameters
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MsPfParameters"/> class.
        /// </summary>
        public MsPfParameters()
        {
            this.Modifications = new ReactiveList<SearchModification>();
        }

        /// <summary>
        /// Gets or sets the path for the PU Spec file.
        /// </summary>
        public string PuSpecFile { get; set; }

        /// <summary>
        /// Gets or sets the path for the FASTA file.
        /// </summary>
        public string DatabaseFile { get; set; }

        /// <summary>
        /// Gets or sets the path for the MS1 feature file.
        /// </summary>
        public string FeatureFile { get; set; }

        /// <summary>
        /// Gets or sets the MSPathFinder search mode.
        /// </summary>
        public int SearchMode { get; set; }

        /// <summary>
        /// Gets or sets the TDA.
        /// </summary>
        public string Tda { get; set; }

        /// <summary>
        /// Gets or sets the precursor ion tolerance in ppm.
        /// </summary>
        public Tolerance PrecursorTolerancePpm { get; set; }

        /// <summary>
        /// Gets or sets the product ion tolerance in ppm.
        /// </summary>
        public Tolerance ProductIonTolerancePpm { get; set; }

        /// <summary>
        /// Gets or sets the minimum length of a sequence.
        /// </summary>
        public int MinSequenceLength { get; set; }

        /// <summary>
        /// Gets or sets the maximum length of a sequence.
        /// </summary>
        public int MaxSequenceLength { get; set; }

        /// <summary>
        /// Gets or sets the minimum possible precursor ion charge state.
        /// </summary>
        public int MinPrecursorIonCharge { get; set; }

        /// <summary>
        /// Gets or sets the maximum possible precursor ion charge state.
        /// </summary>
        public int MaxPrecursorIonCharge { get; set; }

        /// <summary>
        /// Gets or sets the minimum possible product ion charge state.
        /// </summary>
        public int MinProductIonCharge { get; set; }

        /// <summary>
        /// Gets or sets the maximum possible product ion charge state.
        /// </summary>
        public int MaxProductIonCharge { get; set; }

        /// <summary>
        /// Gets or sets the minimum possible sequence mass.
        /// </summary>
        public double MinSequenceMass { get; set; }

        /// <summary>
        /// Gets or sets the maximum possible sequence mass.
        /// </summary>
        public double MaxSequenceMass { get; set; }

        /// <summary>
        /// Gets or sets the minimum possible MS1 feature probability threshold.
        /// </summary>
        public double MinFeatureProbablility { get; set; }

        /// <summary>
        /// Gets or sets the maximum possible modification combinations per sequence.
        /// </summary>
        public int MaxDynamicModificationsPerSequence { get; set; }

        /// <summary>
        /// Gets or sets a list containing the post-translational modifications used for database search.
        /// </summary>
        public ReactiveList<SearchModification> Modifications { get; set; }

        /// <summary>
        /// Opens parameter file.
        /// </summary>
        /// <param name="filePath">The path of the parameter file or ID file.</param>
        /// <returns>
        /// Parsed MSPathFinder parameters. Returns null if file does not exist.
        /// Throws exception if file is not formatted correctly.
        /// </returns>
        public static MsPfParameters ReadFromFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();

            if (!string.IsNullOrEmpty(extension) && extension == ".param")
            {
                return ReadFromParameterFile(filePath);
            }

            return ReadFromIdFilePath(filePath);
        }

        /// <summary>
        /// Opens parameter file from .PARAM file..
        /// </summary>
        /// <param name="filePath">The path of the parameter file.</param>
        /// <returns>
        /// Parsed MSPathFinder parameters. Returns null if file does not exist.
        /// Throws exception if file is not formatted correctly.
        /// </returns>
        private static MsPfParameters ReadFromParameterFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var file = File.ReadAllLines(filePath);

            var param = new MsPfParameters();

            foreach (var line in file)
            {
                var parts = line.Split('\t');
                if (parts.Length < 2)
                {
                    continue;
                }

                switch (parts[0])
                {
                    case "SpecFile":
                        param.PuSpecFile = parts[1];
                        break;
                    case "DatabaseFile":
                        param.DatabaseFile = parts[1];
                        break;
                    case "FeatureFile":
                        param.FeatureFile = parts[1];
                        break;
                    case "SearchMode":
                        param.SearchMode = Convert.ToInt32(parts[1]);
                        break;
                    case "Tda":
                        param.Tda = parts[1];
                        break;
                    case "PrecursorIonTolerancePpm":
                        param.PrecursorTolerancePpm = new Tolerance(Convert.ToDouble(parts[1]), ToleranceUnit.Ppm);
                        break;
                    case "ProductIonTolerancePpm":
                        param.ProductIonTolerancePpm = new Tolerance(Convert.ToDouble(parts[1]), ToleranceUnit.Ppm);
                        break;
                    case "MinSequenceLength":
                        param.MinSequenceLength = Convert.ToInt32(parts[1]);
                        break;
                    case "MaxSequenceLength":
                        param.MaxSequenceLength = Convert.ToInt32(parts[1]);
                        break;
                    case "MinPrecursorIonCharge":
                        param.MinPrecursorIonCharge = Convert.ToInt32(parts[1]);
                        break;
                    case "MaxPrecursorIonCharge":
                        param.MaxPrecursorIonCharge = Convert.ToInt32(parts[1]);
                        break;
                    case "MinProductIonCharge":
                        param.MinProductIonCharge = Convert.ToInt32(parts[1]);
                        break;
                    case "MaxProductIonCharge":
                        param.MaxProductIonCharge = Convert.ToInt32(parts[1]);
                        break;
                    case "MinSequenceMass":
                        param.MinSequenceMass = Convert.ToDouble(parts[1]);
                        break;
                    case "MaxSequenceMass":
                        param.MaxSequenceMass = Convert.ToDouble(parts[1]);
                        break;
                    case "MinFeatureProbability":
                        param.MinFeatureProbablility = Convert.ToDouble(parts[1]);
                        break;
                    case "MaxDynamicModificationsPerSequence":
                        param.MaxDynamicModificationsPerSequence = Convert.ToInt32(parts[1]);
                        break;
                    case "Modification":
                        param.Modifications.Add(ParseModification(parts[1]));
                        break;
                }
            }

            if (param.PrecursorTolerancePpm == null && param.ProductIonTolerancePpm != null)
            {
                param.PrecursorTolerancePpm = param.ProductIonTolerancePpm;
            }

            if (param.PrecursorTolerancePpm != null && param.ProductIonTolerancePpm == null)
            {
                param.ProductIonTolerancePpm = param.PrecursorTolerancePpm;
            }

            return param;
        }

        /// <summary>
        /// Looks for and opens a parameter file in the same directory as MSPathFinder
        /// results if it exists.
        /// </summary>
        /// <param name="idFilePath">Path to an MSPathFinder results file</param>
        /// <returns>
        /// Parsed MSPathFinder parameters. Returns null if file does not exist.
        /// Throws exception if file is not formatted correctly.
        /// </returns>
        private static MsPfParameters ReadFromIdFilePath(string idFilePath)
        {
            var path = Path.GetDirectoryName(idFilePath);
            var fileName = Path.GetFileNameWithoutExtension(idFilePath);
            if (fileName == null)
            {
                return null;
            }

            string dataSetName = fileName;
            if (fileName.EndsWith("_IcTsv") || fileName.EndsWith("_IcTda"))
            {
                dataSetName = fileName.Substring(0, fileName.Length - 6);
            }

            var paramFilePath = string.Format(@"{0}\{1}.param", path, dataSetName);
            return ReadFromParameterFile(paramFilePath);
        }

        /// <summary>
        /// The parse modification.
        /// </summary>
        /// <param name="modificationStr">The modification string.</param>
        /// <returns>
        /// The <see cref="SearchModification"/>.
        /// </returns>
        /// <exception cref="FormatException">
        /// Throws exception if modification string is not in the format: composition,amino acid,fixed,location,name
        /// </exception>
        private static SearchModification ParseModification(string modificationStr)
        {
            var parts = modificationStr.Split(',');
            if (parts.Length < 5)
            {
                throw new FormatException(string.Format("Modification is improperly formatted:\n{0}", modificationStr));
            }

            var composition = Composition.Parse(parts[0]);

            if (string.IsNullOrEmpty(parts[1]) || parts[1].Length > 1)
            {
                throw new FormatException(string.Format("Invalid amino acid found: {0}", parts[1]));
            }

            var aminoAcid = parts[1][0];

            var isFixed = parts[2] != "opt";

            var sequenceLocation = (SequenceLocation)Enum.Parse(typeof(SequenceLocation), parts[3]);

            var name = parts[4];

            Modification modification = Modification.Get(name) ??
                                        IcParameters.Instance.RegisterModification(name, composition);

            return new SearchModification(modification, aminoAcid, sequenceLocation, isFixed);
        }
    }
}
