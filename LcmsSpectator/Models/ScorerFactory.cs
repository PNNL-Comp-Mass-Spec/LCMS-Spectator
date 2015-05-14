// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScorerFactory.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Scorer factory that creates the correct type of scorer based on the parameters supplied
//   in its constructor.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Models
{
    using InformedProteomics.Backend.Data.Spectrometry;
    using InformedProteomics.TopDown.Scoring;

    /// <summary>
    /// Scorer factory that creates the correct type of scorer based on the parameters supplied
    /// in its constructor.
    /// </summary>
    public class ScorerFactory
    {
        /// <summary>
        /// The maximum peak error tolerance.
        /// </summary>
        private readonly Tolerance tolerance;

        /// <summary>
        /// The lowest charge state to consider.
        /// </summary>
        private readonly int minCharge;

        /// <summary>
        /// The highest charge state to consider.
        /// </summary>
        private readonly int maxCharge;

        /// <summary>
        /// The Pearson correlation score threshold.
        /// </summary>
        private readonly double corrScoreThreshold;

        /// <summary>
        /// The scoring model to use for likelihood scoring (not used for peak counting).
        /// </summary>
        private readonly LikelihoodScoringModel model;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScorerFactory"/> class.
        /// </summary>
        /// <param name="tolerance">The maximum peak error tolerance.</param>
        /// <param name="minCharge">The lowest charge state to consider.</param>
        /// <param name="maxCharge">The highest charge state to consider.</param>
        /// <param name="corrScoreThreshold">The Pearson correlation score threshold.</param>
        /// <param name="model">The scoring model to use for likelihood scoring (not used for peak counting).</param>
        public ScorerFactory(
            Tolerance tolerance,
            int minCharge,
            int maxCharge,
            double corrScoreThreshold = 0.7,
            LikelihoodScoringModel model = null)
        {
            this.tolerance = tolerance;
            this.minCharge = minCharge;
            this.maxCharge = maxCharge;
            this.corrScoreThreshold = corrScoreThreshold;
            this.model = model;
        }

        /// <summary>
        /// Get the correct scorer based on the parameters supplied to this factory.
        /// </summary>
        /// <param name="productSpectrum">The spectrum that the scorer should calculate scores from.</param>
        /// <returns>A scorer.</returns>
        public IScorer GetScorer(ProductSpectrum productSpectrum)
        {
            if (this.model == null)
            {
                return new CorrMatchedPeakCounter(productSpectrum, this.tolerance, this.minCharge, this.maxCharge, this.corrScoreThreshold);
            }

            return new LikelihoodScorer(this.model, productSpectrum, this.tolerance, this.minCharge, this.maxCharge);
        }
    }
}
