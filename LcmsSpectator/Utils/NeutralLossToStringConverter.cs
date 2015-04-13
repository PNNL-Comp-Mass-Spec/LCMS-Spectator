// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NeutralLossToStringConverter.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Converter that formats a neutral loss to a string.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Utils
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using InformedProteomics.Backend.Data.Spectrometry;
    
    /// <summary>
    /// Converter that formats a neutral loss to a string.
    /// </summary>
    public class NeutralLossToStringConverter : IValueConverter
    {
        /// <summary>
        /// Convert neutral loss to a string.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        /// <param name="targetType">Type to convert to.</param>
        /// <param name="parameter">Parameter of value to convert.</param>
        /// <param name="culture">Culture of value to convert.</param>
        /// <returns>Converted value.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var neutralLoss = (NeutralLoss)value;
                var name = neutralLoss.Name;
                return (neutralLoss == NeutralLoss.NoLoss) ? "No Loss" : name.Remove(0, 1);
            }
            catch (InvalidCastException)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Convert string to neutral loss.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        /// <param name="targetType">Type to convert to.</param>
        /// <param name="parameter">Parameter of value to convert.</param>
        /// <param name="culture">Culture of value to convert.</param>
        /// <returns>Converted value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
