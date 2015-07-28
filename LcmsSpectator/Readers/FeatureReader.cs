// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FeatureReader.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   The reader for MS1 feature files.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Readers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using InformedProteomics.Backend.Data.Composition;
    using InformedProteomics.Backend.Data.Spectrometry;
    using LcmsSpectator.Models;

    /// <summary>
    /// The reader for MS1 feature files.
    /// </summary>
    public class FeatureReader
    {
        /// <summary>Read a MS1 feature file.</summary>
        /// <param name="filePath">The feature file path.</param>
        /// <param name="delimeter">The Delimiter character of feature file.</param>
        /// <returns>The list of MS1 features..</returns>
        public static IList<Feature> Read(string filePath, char delimeter = '\t')
        {
            var features = new List<Feature>();
            var file = new StreamReader(File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            ////var file = File.ReadLines(_tsvFile);
            var headers = new Dictionary<string, int>();
            var lineCount = 0;
            while (!file.EndOfStream)
            {
                var line = file.ReadLine();
                lineCount++;
                if (lineCount == 1 && line != null)
                { // first line
                    var parts = line.Split(delimeter);
                    for (int i = 0; i < parts.Length; i++)
                    {
                        headers.Add(parts[i], i);
                    }

                    continue;
                }

                var idData = ReadFeature(line, delimeter, headers);
                if (idData != null)
                {
                    features.Add(idData);
                }
            }

            file.Close();

            return features;
        }

        /// <summary>
        /// Read a line from the feature file containing a single feature.
        /// </summary>
        /// <param name="line">The line from the feature file.</param>
        /// <param name="delimeter">The delimiter used in feature file.</param>
        /// <param name="headers">The headers of the feature file columns.</param>
        /// <returns>Parsed feature.</returns>
        private static Feature ReadFeature(string line, char delimeter, IReadOnlyDictionary<string, int> headers)
        {
            var expectedHeaders = new List<string>
            {
                "MonoMass",
                "Abundance",
                "LikelihoodRatio",
                "Envelope",
                "MinCharge",
                "MaxCharge",
                ////"SummedCorr",
                "MinScan",
                "MaxScan"
            };

            string likelihoodVarHeader = "LikelihoodRatio";

            foreach (var header in expectedHeaders.Where(header => !headers.ContainsKey(header)))
            {
                if (header == "LikelihoodRatio" && headers.ContainsKey("Probability"))
                {
                    likelihoodVarHeader = "Probability";
                }
                else
                {
                    throw new KeyNotFoundException(string.Format("Missing expected column header \"{0}\" in feature file.", header));   
                }
            }

            var parts = line.Split(delimeter);
            var mass = Convert.ToDouble(parts[headers["MonoMass"]]);
            var abundance = Convert.ToDouble(parts[headers["Abundance"]]);
            var score = Convert.ToDouble(parts[headers[likelihoodVarHeader]]);
            var isotopes = ReadIsotopicEnvelope(parts[headers["Envelope"]]);
            var minCharge = Convert.ToInt32(parts[headers["MinCharge"]]);
            var maxCharge = Convert.ToInt32(parts[headers["MaxCharge"]]);
            int id = -1;
            if (headers.ContainsKey("FeatureID"))
            {
                id = Convert.ToInt32(parts[headers["FeatureID"]]);
            }

            var summedCorr = headers.ContainsKey("SummedCorr") ? Convert.ToDouble(parts[headers["SummedCorr"]]) : 0.0;

            int mostAbundantIsotopeIndex = Averagine.GetIsotopomerEnvelope(mass).MostAbundantIsotopeIndex;
            List<Peak> minIsotopicProfile = Averagine.GetTheoreticalIsotopeProfile(mass, minCharge, 0);
            List<Peak> maxIsotopicProfile = Averagine.GetTheoreticalIsotopeProfile(mass, maxCharge, 0);

            var minPoint = new Feature.FeaturePoint
            {
                Id = id,
                Mass = mass,
                Scan = Convert.ToInt32(parts[headers["MinScan"]]),
                Mz = minIsotopicProfile[mostAbundantIsotopeIndex].Mz,
                Charge = minCharge,
                Abundance = abundance,
                Score = score,
                Isotopes = isotopes,
                Correlation = summedCorr
            };
            var maxPoint = new Feature.FeaturePoint
            {
                Id = id,
                Mass = mass,
                Scan = Convert.ToInt32(parts[headers["MaxScan"]]),
                Mz = maxIsotopicProfile[mostAbundantIsotopeIndex].Mz,
                Charge = maxCharge,
                Abundance = abundance,
                Score = score,
                Isotopes = isotopes,
                Correlation = summedCorr,
            };
            return new Feature(minPoint, maxPoint) { Id = id };
        }

        /// <summary>
        /// Parse isotopic envelope string for feature.
        /// </summary>
        /// <param name="env">Isotopic envelope string.</param>
        /// <returns>Array of isotopes.</returns>
        private static Isotope[] ReadIsotopicEnvelope(string env)
        {
            var isotopePeaks = env.Split(';');
            if (isotopePeaks.Length == 0)
            {
                return null;
            }

            var isotopes = new Isotope[isotopePeaks.Length];
            for (int i = 0; i < isotopePeaks.Length; i++)
            {
                var parts = isotopePeaks[i].Split(',');
                if (parts.Length < 2)
                {
                    continue;
                }

                int index;
                double relativeIntensity;
                if (!int.TryParse(parts[0], out index) || !double.TryParse(parts[1], out relativeIntensity))
                {
                    continue;
                }

                isotopes[i] = new Isotope(index, relativeIntensity);
            }

            return isotopes;
        }
    }
}
