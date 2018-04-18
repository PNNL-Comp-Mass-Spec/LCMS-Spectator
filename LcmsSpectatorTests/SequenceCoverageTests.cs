using System;
using System.Collections.Generic;
using System.Linq;

namespace LcmsSpectatorTests
{
    using System.IO;

    using InformedProteomics.Backend.Data.Spectrometry;

    using NUnit.Framework;

    [TestFixture]
    public class SequenceCoverageTests
    {
        private class FoundIon
        {
            public string IonType { get; set; }
            public int Index { get; set; }
        }

        [Test]
        //[TestCase(@"C:\Users\wilk011\Documents\DataFiles\UVPD\defaultCorrelationPeakList.tsv", @"C:\Users\wilk011\Documents\DataFiles\UVPD\uvpd.tsv")]
        [TestCase(@"C:\Users\wilk011\Documents\DataFiles\UVPD\zeroCorrelationPeakList.tsv", @"C:\Users\wilk011\Documents\DataFiles\UVPD\uvpd.tsv")]
        //[TestCase(@"C:\Users\wilk011\Documents\DataFiles\UVPD\neutralZeroCorrelationPeakList.tsv", @"C:\Users\wilk011\Documents\DataFiles\UVPD\uvpd.tsv")]
        public void CompareFragments(string lcmsSpecPeakList, string prosightAnnotations)
        {
            Console.WriteLine("{0}", Path.GetFileNameWithoutExtension(lcmsSpecPeakList));
            var lcmsSpecResults = ReadLcmsSpectatorPeakList(lcmsSpecPeakList);
            var prosightResults = ReadProsightResults(prosightAnnotations);

            var ionTypeCount = new Dictionary<string, int>();
            var notFoundIons = new Dictionary<string, int>();
            var notFoundCount = 0;
            foreach (var prosightResult in prosightResults)
            {
                if (!ionTypeCount.ContainsKey(prosightResult.IonType))
                {
                    ionTypeCount.Add(prosightResult.IonType, 0);
                }

                ionTypeCount[prosightResult.IonType]++;

                var found = false;
                foreach (var lcmsSpecResult in lcmsSpecResults)
                {
                    if (lcmsSpecResult.IonType == prosightResult.IonType && lcmsSpecResult.Index == prosightResult.Index)
                    {
                        found = true;
                    }
                }

                if (!found)
                {
                    if (!notFoundIons.ContainsKey(prosightResult.IonType))
                    {
                        notFoundIons.Add(prosightResult.IonType, 0);
                    }

                    notFoundIons[prosightResult.IonType]++;

                    notFoundCount++;
                    Console.WriteLine("Ion type not found: {0}({1})", prosightResult.IonType, prosightResult.Index);
                }
            }

            Console.WriteLine("Not found fragments: {0}/{1}, ", notFoundCount, prosightResults.Count);
            Console.Write("Missing ion types: ");
            foreach (var ionType in notFoundIons)
            {
                Console.WriteLine("\t{0}({1}%) ", ionType.Key, Math.Round(100.0 * ionType.Value / ionTypeCount[ionType.Key], 2));
            }
        }

        private List<FoundIon> ReadLcmsSpectatorPeakList(string lcmsSpecPeakList)
        {
            var baseIonTypes = BaseIonType.AllBaseIonTypes.ToList();

            var results = new List<FoundIon>();
            var lineCount = 0;
            var headerToColumn = new Dictionary<string, int>();
            foreach (var line in File.ReadLines(lcmsSpecPeakList))
            {
                var parts = line.Split('\t');
                if (parts.Length < 7)
                {
                    continue;
                }

                if (lineCount++ == 0)
                {
                    for (var i = 0; i < parts.Length; i++)
                    {
                        headerToColumn.Add(parts[i], i);
                    }

                    continue;
                }

                var ionName = parts[headerToColumn["Ion"]];
                var parsedIonName = string.Empty;

                // Get ion type
                foreach (var ionType in baseIonTypes)
                {
                    if (ionName.StartsWith(ionType.Symbol))
                    {
                        parsedIonName = ionType.Symbol;
                    }
                }

                Assert.False(string.IsNullOrEmpty(parsedIonName));

                var indexStr = ionName.Substring(parsedIonName.Length, ionName.Length - parsedIonName.Length);
                var index = Convert.ToInt32(indexStr);

                results.Add(new FoundIon { IonType = parsedIonName, Index = index });
            }

            return results;
        }

        private List<FoundIon> ReadProsightResults(string prosightResultsFile)
        {
            var ionTypeMapping = new Dictionary<string, string>
            {
                { "A", "a" },
                { "A+", "a." },
                { "B", "b" },
                { "C", "c" },
                { "D", "d" },
                { "V", "v" },
                { "W", "w" },
                { "X", "x" },
                { "X+", "x." },
                { "Y", "y" },
                { "Y-", "y-1" },
                { "Z", "z" },
                { "Z+", "z." },
            };

            var results = new List<FoundIon>();
            var lineCount = 0;
            var headerToColumn = new Dictionary<string, int>();
            foreach (var line in File.ReadLines(prosightResultsFile))
            {
                var parts = line.Split('\t');
                if (lineCount++ == 0)
                {
                    for (var i = 0; i < parts.Length; i++)
                    {
                        headerToColumn.Add(parts[i], i);
                    }

                    continue;
                }

                var ionNumber = Convert.ToInt32(parts[headerToColumn["Ion Number"]]);
                var ionName = parts[headerToColumn["Name"]];
                var parsedIonName = string.Empty;

                // Get ion type
                foreach (var ionType in ionTypeMapping.Keys)
                {
                    if (ionName.StartsWith(ionType))
                    {
                        parsedIonName = ionTypeMapping[ionType];
                    }
                }

                Assert.False(string.IsNullOrEmpty(parsedIonName));

                results.Add(new FoundIon
                {
                    IonType = parsedIonName,
                    Index = ionNumber
                });
            }

            return results;
        }
    }
}
