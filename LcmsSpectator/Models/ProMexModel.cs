// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProMexModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Model for PROMEX features.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using InformedProteomics.Backend.Data.Biology;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.Readers;
using QuadTreeLib;

namespace LcmsSpectator.Models
{
    /// <summary>
    /// Model for PROMEX features.
    /// </summary>
    public class ProMexModel : IFeatureExtractor
    {
        /// <summary>
        /// The LCMSRun for the data set this feature map shows.
        /// </summary>
        private readonly LcMsRun lcms;

        /// <summary>
        /// Maps feature points to features.
        /// </summary>
        private Dictionary<Feature.FeaturePoint, Feature> featurePointToFeature;

        /// <summary>
        /// Spatial representation of features.
        /// </summary>
        private QuadTree<Feature> featureTree;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProMexModel"/> class.
        /// </summary>
        /// <param name="lcms">The LCMSRun for this feature set.</param>
        public ProMexModel(LcMsRun lcms)
        {
            this.lcms = lcms;
        }

        /// <summary>
        /// Gets all unfiltered features.
        /// </summary>
        public Feature[] Features { get; private set; }

        /// <summary>
        /// Gets the feature file path.
        /// </summary>
        public string FeatureFilePath { get; private set; }

        /// <summary>
        /// Gets the highest abundance in the unfiltered features.
        /// </summary>
        public double AbsoluteAbundanceMaximum { get; private set; }

        /// <summary>
        /// Gets the lowest abundance in the unfiltered features.
        /// </summary>
        public double AbsoluteAbundanceMinimum { get; private set; }

        /// <summary>
        /// Read features from feature file.
        /// </summary>
        /// <param name="featureFilePath">The feature file path.</param>
        public void ReadFeatures(string featureFilePath)
        {
            Features = FeatureReader.Read(featureFilePath).ToArray();
            FeatureFilePath = featureFilePath;
            SetFeatures();
        }

        /// <summary>
        /// Filter feature list.
        /// </summary>
        /// <param name="abundanceThreshold">Minimum abundance threshold to filter by.</param>
        /// <param name="maxPoints">Maximum number of features.</param>
        /// <returns>Filtered features.</returns>
        public IEnumerable<Feature> GetFeatures(double abundanceThreshold, int maxPoints)
        {
            var maxAbundance = Math.Pow(abundanceThreshold, 10);
            if (Features == null)
            {
                return new List<Feature>();
            }

            var filteredFeatures =
                Features.Where(feature => feature.MinPoint.Abundance <= maxAbundance) ////&& feature.MinPoint.Score >= scoreThreshold)
                         .OrderByDescending(feature => feature.MinPoint.Abundance).ToList();
            var numDisplayed = Math.Min(maxPoints, filteredFeatures.Count);
            var topNPoints = filteredFeatures.GetRange(0, numDisplayed);
            return topNPoints;
        }

        /// <summary>
        /// Set the PRSM Identifications.
        /// </summary>
        /// <param name="ids">The PRSM identifications.</param>
        /// <param name="massTolerance">Mass tolerance.</param>
        public void SetIds(IEnumerable<PrSm> ids, double massTolerance = 0.1)
        {
            if (Features == null || Features.Length == 0)
            {
                return;
            }

            var idList = ids.ToList();
            var prsmScanComp = new PrSm.PrSmScanComparer();
            idList.Sort(prsmScanComp);
            var minPrSm = new PrSm();
            var maxPrSm = new PrSm();
            foreach (var feature in Features.Where(feature => feature.AssociatedMs2.Count > 0))
            {   // Associate IDs with features
                minPrSm.Scan = feature.AssociatedMs2[0];
                maxPrSm.Scan = feature.AssociatedMs2[feature.AssociatedMs2.Count - 1];

                // Find IDs in the scan range of the feature.
                var minIdIndex = Math.Abs(idList.BinarySearch(minPrSm, prsmScanComp));
                var maxIdIndex = Math.Abs(idList.BinarySearch(maxPrSm, prsmScanComp));
                minIdIndex = Math.Min(Math.Max(minIdIndex - 1, 0), idList.Count - 1);
                maxIdIndex = Math.Min(Math.Max(maxIdIndex, 0) + 1, idList.Count - 1);

                // Find identified MS/MS scans associated with the feature
                var explainedMs2Scans = new HashSet<int>();
                feature.AssociatedPrSms.Clear();
                for (var i = minIdIndex; i < maxIdIndex; i++)
                {
                    if (Math.Abs(idList[i].Mass - feature.MinPoint.Mass) < massTolerance)
                    {
                        feature.AssociatedPrSms.Add(idList[i]);
                        if (!explainedMs2Scans.Contains(idList[i].Scan))
                        {
                            explainedMs2Scans.Add(idList[i].Scan);
                        }
                    }
                }

                // Find unidentified MS/MS scans associated with the feature
                foreach (var scan in feature.AssociatedMs2)
                {
                    if (!explainedMs2Scans.Contains(scan))
                    {
                        feature.AssociatedPrSms.Add(new PrSm { LcMs = lcms, Scan = scan, Mass = feature.MinPoint.Mass });
                    }
                }
            }
        }

        /// <summary>
        /// Get a feature given a feature point.
        /// </summary>
        /// <param name="featurePoint">The feature point.</param>
        /// <returns>The feature associated with the feature point.</returns>
        public Feature GetFeatureFromPoint(Feature.FeaturePoint featurePoint)
        {
            Feature feature = null;
            if (featurePointToFeature.ContainsKey(featurePoint))
            {
                feature = featurePointToFeature[featurePoint];
            }

            return feature;
        }

        /// <summary>
        /// Set the features.
        /// </summary>
        private void SetFeatures()
        {
            featurePointToFeature = new Dictionary<Feature.FeaturePoint, Feature>();
            foreach (var feature in Features)
            {
                featurePointToFeature.Add(feature.MinPoint, feature);
                featurePointToFeature.Add(feature.MaxPoint, feature);
                feature.MinPoint.RetentionTime = lcms.GetElutionTime(feature.MinPoint.Scan);
                feature.MaxPoint.RetentionTime = lcms.GetElutionTime(feature.MaxPoint.Scan);

                for (var c = feature.MinPoint.Charge; c <= feature.MaxPoint.Charge; c++)
                {
                    var mz = (feature.MinPoint.Mass + (c * Constants.Proton)) / c;
                    feature.AssociatedMs2.AddRange(lcms.GetFragmentationSpectraScanNums(mz)
                                           .Where(s => s >= feature.MinPoint.Scan && s <= feature.MaxPoint.Scan));
                }

                feature.AssociatedMs2.Sort();
            }

            AbsoluteAbundanceMaximum = Features.Max(f => f.MinPoint.Abundance);
            AbsoluteAbundanceMinimum = Features.Min(f => f.MinPoint.Abundance);

            InitFeatureTree();
        }

        /// <summary>
        /// Initialize feature tree with feature data.
        /// </summary>
        private void InitFeatureTree()
        {
            var minRt = (float)lcms.GetElutionTime(lcms.MinLcScan);
            var maxRt = (float)lcms.GetElutionTime(lcms.MaxLcScan);

            var rectangle = new RectangleF
            {
                X = minRt,
                Width = maxRt - minRt,
                Y = 0,
                Height = (float)Features.Max(feature => feature.MinPoint.Abundance)
            };

            featureTree = new QuadTree<Feature>(rectangle);

            foreach (var feature in Features)
            {
                featureTree.Insert(feature);
            }
        }
    }
}
