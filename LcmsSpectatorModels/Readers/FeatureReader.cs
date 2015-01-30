using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography.X509Certificates;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectatorModels.Models;

namespace LcmsSpectatorModels.Readers
{
    public class FeatureReader
    {

        public static IList<Feature> Read(string fileName, char delimeter='\t')
        {
            var features = new List<Feature>();
            var file = new StreamReader(File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            //var file = File.ReadLines(_tsvFile);
            var headers = new Dictionary<string, int>();
            var lineCount = 0;
            while (!file.EndOfStream)
            {
                var line = file.ReadLine();
                lineCount++;
                if (lineCount == 1 && line != null) // first line
                {
                    var parts = line.Split(delimeter);
                    for (int i = 0; i < parts.Length; i++)
                    {
                        headers.Add(parts[i], i);
                    }
                    continue;
                }
                var idData = ReadFeature(line, delimeter, headers);
                if (idData != null) features.Add(idData);
            }
            file.Close();

            return features;
        }

        private static Feature ReadFeature(string line, char delimeter, Dictionary<string, int> headers)
        {
            var expectedHeaders = new List<string>
            {
                "MonoMass",
                "Abundance",
                "Probability",
                "Envelope",
                "MinCharge",
                "MaxCharge",
                "SummedCorr",
                "MinScan",
                "MaxScan"
            };

            foreach (var header in expectedHeaders.Where(header => !headers.ContainsKey(header)))
            {
                throw new KeyNotFoundException(String.Format("Missing expected column header \"{0}\" in feature file.", header));
            }

            var parts = line.Split(delimeter);
            var mass = Convert.ToDouble(parts[headers["MonoMass"]]);
            var abundance = Convert.ToDouble(parts[headers["Abundance"]]);
            var score = Convert.ToDouble(parts[headers["Probability"]]);
            var isotopes = ReadIsotopicEnvelope(parts[headers["Envelope"]]);
            var minCharge = Convert.ToInt32(parts[headers["MinCharge"]]);
            var maxCharge = Convert.ToInt32(parts[headers["MaxCharge"]]);
            //var id = Convert.ToInt32(parts[headers["FeatureID"]]);
            var summedCorr = Convert.ToDouble(parts[headers["SummedCorr"]]);

            int mostAbundantIsotopeIndex;
            List<Peak> minIsotopicProfile;
            List<Peak> maxIsotopicProfile;

            mostAbundantIsotopeIndex = Averagine.GetIsotopomerEnvelope(mass).MostAbundantIsotopeIndex;
            minIsotopicProfile = Averagine.GetTheoreticalIsotopeProfile(mass, minCharge, 0);
            maxIsotopicProfile = Averagine.GetTheoreticalIsotopeProfile(mass, maxCharge, 0);

            var minPoint = new FeaturePoint
            {
                Mass = mass,
                Scan = Convert.ToInt32(parts[headers["MinScan"]]),
                Mz = minIsotopicProfile[mostAbundantIsotopeIndex].Mz,
                Charge = minCharge,
                Abundance = abundance,
                Score = score,
                Isotopes = isotopes,
                Correlation = summedCorr
            };
            var maxPoint = new FeaturePoint
            {
                Mass = mass,
                Scan = Convert.ToInt32(parts[headers["MaxScan"]]),
                Mz = maxIsotopicProfile[mostAbundantIsotopeIndex].Mz,
                Charge = maxCharge,
                Abundance = abundance,
                Score = score,
                Isotopes = isotopes,
                Correlation = summedCorr,
            };
            return new Feature { MinPoint = minPoint, MaxPoint = maxPoint };
        }

        private static Isotope[] ReadIsotopicEnvelope(string env)
        {
            var isotopePeaks = env.Split(';');
            if (isotopePeaks.Length == 0) return null;
            var isotopes = new Isotope[isotopePeaks.Length];
            for (int i = 0; i < isotopePeaks.Length; i++)
            {
                var parts = isotopePeaks[i].Split(',');
                if (parts.Length < 2) continue;
                int index;
                double relativeIntensity;
                if (!Int32.TryParse(parts[0], out index) || !Double.TryParse(parts[1], out relativeIntensity))
                    continue;
                isotopes[i] = new Isotope(index, relativeIntensity);
            }
            return isotopes;
        }
    }
}
