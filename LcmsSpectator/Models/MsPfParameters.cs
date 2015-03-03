using System;
using System.Collections.Generic;
using System.IO;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Enum;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.Config;
using ReactiveUI;

namespace LcmsSpectator.Models
{
    public class MsPfParameters
    {
        public MsPfParameters()
        {
            Modifications = new ReactiveList<SearchModification>();
        }

        public string PuSpecFile { get; set; }
        public string DatabaseFile { get; set; }
        public string FeatureFile { get; set; }
        public int SearchMode { get; set; }
        public string Tda { get; set; }
        public Tolerance PrecursorTolerancePpm { get; set; }
        public Tolerance ProductIonTolerancePpm { get; set; }
        public int MinSequenceLength { get; set; }
        public int MaxSequenceLength { get; set; }
        public int MinPrecursorIonCharge { get; set; }
        public int MaxPrecursorIonCharge { get; set; }
        public int MinProductIonCharge { get; set; }
        public int MaxProductIonCharge { get; set; }
        public double MinSequenceMass { get; set; }
        public double MaxSequenceMass { get; set; }
        public double MinFeatureProbablility { get; set; }
        public int MaxDynamicModificationsPerSequence { get; set; }
        public ReactiveList<SearchModification> Modifications { get; set; }

        /// <summary>
        /// Looks for and opens a parameter file in the same directory as MS PathFinder
        /// results if it exists.
        /// </summary>
        /// <param name="idFilePath">Path to an MSPathFinder results file</param>
        /// <returns>
        /// Parsed MsPathFinder parameters. Returns null if file does not exist.
        /// Throws exception if file is not formatted correctly.
        /// </returns>
        public static MsPfParameters ReadFromIdFilePath(string idFilePath)
        {
            var path = Path.GetDirectoryName(idFilePath);
            var fileName = Path.GetFileNameWithoutExtension(idFilePath);
            if (fileName == null) return null;
            string dataSetName;
            if (fileName.EndsWith("_IcTsv") || fileName.EndsWith("_IcTda"))
            {
                dataSetName = fileName.Substring(0, fileName.Length - 6);
            }
            else return null;

            var paramFilePath = String.Format(@"{0}\{1}.param", path, dataSetName);
            return ReadFromFile(paramFilePath);
        }

        /// <summary>
        /// Opens parameter file.
        /// </summary>
        /// <param name="filePath">The path of the parameter file.</param>
        /// <returns>
        /// Parsed MsPathFinder parameters. Returns null if file does not exist.
        /// Throws exception if file is not formatted correctly.
        /// </returns>
        public static MsPfParameters ReadFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            var file = File.ReadAllLines(filePath);

            var param = new MsPfParameters();

            foreach (var line in file)
            {
                var parts = line.Split('\t');
                if (parts.Length < 2) continue;
                switch (parts[0])
                {
                    case "puSpecFile":
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
                    case "ProductIonTolerance":
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

            return param;
        }

        private static SearchModification ParseModification(string modificationStr)
        {
            var parts = modificationStr.Split(',');
            if (parts.Length < 5) throw new FormatException(String.Format("Modification is improperly formatted:\n{0}", modificationStr));

            var composition = Composition.Parse(parts[0]);

            if (String.IsNullOrEmpty(parts[1]) || parts[1].Length > 1) throw new FormatException(String.Format("Invalid amino acid found: {0}", parts[1]));
            var aminoAcid = parts[1][0];

            var isFixed = parts[2] == "opt";

            var sequenceLocation = (SequenceLocation) Enum.Parse(typeof(SequenceLocation), parts[3]);

            var name = parts[4];

            Modification modification = Modification.Get(name) ??
                                        IcParameters.Instance.RegisterModification(name, composition);

            return new SearchModification(modification, aminoAcid, sequenceLocation, isFixed);
        }
    }
}
