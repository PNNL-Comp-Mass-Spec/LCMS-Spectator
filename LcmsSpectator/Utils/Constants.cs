// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Contains constants used when creating precursor and fragment ions.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Utils
{
    /// <summary>
    /// Contains constants used when creating precursor and fragment ions.
    /// </summary>
    public class Constants
    {
        /// <summary>
        /// Minimum possible charge for product ions.
        /// </summary>
        public const int MinCharge = 1;

        /// <summary>
        /// Maximum possible charge for product ions.
        /// </summary>
        public const int MaxCharge = 50;

        /// <summary>
        /// Minimum possible charge for product ions.
        /// </summary>
        public const int IsotopeOffsetTolerance = 2;

        /// <summary>
        /// Minimum possible isotope index.
        /// </summary>
        public const int MinIsotopeIndex = -1;

        /// <summary>
        /// Maximum possible isotope index.
        /// </summary>
        public const int MaxIsotopeIndex = 2;
    }
}
