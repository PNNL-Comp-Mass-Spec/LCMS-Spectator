namespace LcmsSpectator.Models
{
    using System.Collections.Generic;
    
    public interface IFeatureExtractor
    {
        /// <summary>
        /// Get feature list.
        /// </summary>
        /// <param name="abundanceThreshold">Minimum abundance threshold to filter by.</param>
        /// <param name="maxPoints">Maximum number of features.</param>
        /// <returns>The features.</returns>
        IEnumerable<Feature> GetFeatures(double abundanceThreshold, int maxPoints);

        /// <summary>
        /// Set the PRSM Identifications.
        /// </summary>
        /// <param name="ids">The PRSM identifications.</param>
        /// <param name="massTolerance">Mass tolerance.</param>
        void SetIds(IEnumerable<PrSm> ids, double massTolerance = 0.1);

        /// <summary>
        /// Get a feature given a feature point.
        /// </summary>
        /// <param name="featurePoint">The feature point.</param>
        /// <returns>The feature associated with the feature point.</returns>
        Feature GetFeatureFromPoint(Feature.FeaturePoint featurePoint);
    }
}
