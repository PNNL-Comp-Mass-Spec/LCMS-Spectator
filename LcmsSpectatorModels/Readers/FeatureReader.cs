using System;
using System.Collections.Generic;
using System.IO;
using InformedProteomics.Backend.Data.Biology;
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
            var parts = line.Split(delimeter);
            var mass = Convert.ToDouble(parts[headers["monoisotopic_mw"]]);
            var abundance = Convert.ToDouble(parts[headers["abundance"]]);
            var score = Convert.ToDouble(parts[headers["summed_envelope_corr"]]);
            var isotopes = ReadIsotopicEnvelope(parts[headers["isotopic_envelope"]]);
            var minCharge = Convert.ToInt32(parts[headers["min_charge"]]);
            var maxCharge = Convert.ToInt32(parts[headers["max_charge"]]);

            var minPoint = new FeaturePoint
            {
                Mass = mass,
                Scan = Convert.ToInt32(parts[headers["min_scan_num"]]),
                Mz = (mass + (minCharge * Constants.Proton)) / minCharge,
                Charge = minCharge,
                Abundance = abundance,
                Score = score,
                Isotopes = isotopes,
            };
            var maxPoint = new FeaturePoint
            {
                Mass = mass,
                Scan = Convert.ToInt32(parts[headers["max_scan_num"]]),
                Mz = (mass + (maxCharge * Constants.Proton)) / maxCharge,
                Charge = maxCharge,
                Abundance = abundance,
                Score = score,
                Isotopes = isotopes
            };
            return new Feature {MinPoint = minPoint, MaxPoint = maxPoint};
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
