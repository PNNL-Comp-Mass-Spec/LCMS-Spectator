// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProMexModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Model for PROMEX features.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using InformedProteomics.Backend.Data.Biology;
    using InformedProteomics.Backend.MassSpecData;
    using LcmsSpectator.Readers;

    /// <summary>
    /// Model for PROMEX features.
    /// </summary>
    public class ProMexModel
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
            this.Features = FeatureReader.Read(featureFilePath).ToArray();
            this.FeatureFilePath = featureFilePath;
            this.SetFeatures();
        }

        /// <summary>
        /// Filter feature list.
        /// </summary>
        /// <param name="abundanceThreshold">Minimum abundance threshold to filter by.</param>
        /// <param name="maxPoints">Maximum number of features.</param>
        /// <returns>Filtered features.</returns>
        public IList<Feature> GetFilteredFeatures(double abundanceThreshold, int maxPoints)
        {
            var maxAbundance = Math.Pow(abundanceThreshold, 10);
            if (this.Features == null)
            {
                return new List<Feature>();
            }

            var filteredFeatures =
                this.Features.Where(feature => feature.MinPoint.Abundance <= maxAbundance) ////&& feature.MinPoint.Score >= scoreThreshold)
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
            if (this.Features == null || this.Features.Length == 0)
            {
                return;
            }

            var idList = ids.ToList();
            var prsmScanComp = new PrSm.PrSmScanComparer();
            idList.Sort(prsmScanComp);
            var minPrSm = new PrSm();
            var maxPrSm = new PrSm();
            foreach (var feature in this.Features.Where(feature => feature.AssociatedMs2.Count > 0))
            {   // Associate IDs with features
                minPrSm.Scan = feature.AssociatedMs2[0];
                maxPrSm.Scan = feature.AssociatedMs2[feature.AssociatedMs2.Count - 1];

                // Find IDs in the scan range of the feature.
                var minIdIndex = idList.BinarySearch(minPrSm, prsmScanComp);
                var maxIdIndex = idList.BinarySearch(maxPrSm, prsmScanComp);
                minIdIndex = minIdIndex < 0 ? minIdIndex * -1 : minIdIndex;
                maxIdIndex = maxIdIndex < 0 ? maxIdIndex * -1 : maxIdIndex;
                minIdIndex = Math.Min(Math.Max(minIdIndex - 1, 0), idList.Count - 1);
                maxIdIndex = Math.Min(Math.Max(maxIdIndex, 0) + 1, idList.Count - 1);

                // Find identified MS/MS scans associated with the feature
                var explainedMs2Scans = new HashSet<int>();
                feature.AssociatedPrSms.Clear();
                for (int i = minIdIndex; i < maxIdIndex; i++)
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
                        feature.AssociatedPrSms.Add(new PrSm { LcMs = this.lcms, Scan = scan, Mass = feature.MinPoint.Mass });
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
            if (this.featurePointToFeature.ContainsKey(featurePoint))
            {
                feature = this.featurePointToFeature[featurePoint];
            }

            return feature;
        }

        /// <summary>
        /// Set the features.
        /// </summary>
        private void SetFeatures()
        {
            this.featurePointToFeature = new Dictionary<Feature.FeaturePoint, Feature>();
            foreach (var feature in this.Features)
            {
                this.featurePointToFeature.Add(feature.MinPoint, feature);
                this.featurePointToFeature.Add(feature.MaxPoint, feature);
                feature.MinPoint.RetentionTime = this.lcms.GetElutionTime(feature.MinPoint.Scan);
                feature.MaxPoint.RetentionTime = this.lcms.GetElutionTime(feature.MaxPoint.Scan);

                for (int c = feature.MinPoint.Charge; c <= feature.MaxPoint.Charge; c++)
                {
                    var mz = (feature.MinPoint.Mass + (c * Constants.Proton)) / c;
                    feature.AssociatedMs2.AddRange(this.lcms.GetFragmentationSpectraScanNums(mz)
                                           .Where(s => s >= feature.MinPoint.Scan && s <= feature.MaxPoint.Scan));
                }

                feature.AssociatedMs2.Sort();
            }

            this.AbsoluteAbundanceMaximum = this.Features.Max(f => f.MinPoint.Abundance);
            this.AbsoluteAbundanceMinimum = this.Features.Min(f => f.MinPoint.Abundance);
        }
    }
}
