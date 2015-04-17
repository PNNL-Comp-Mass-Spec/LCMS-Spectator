// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScoreToStringConverter.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Converter for converting a score to a string.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Utils
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    
    /// <summary>
    /// Converter for converting a double to scientific notation.
    /// </summary>
    public class DoubleToStringConverterSci : IValueConverter
    {
        /// <summary>
        /// Convert score to string.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        /// <param name="targetType">Type to convert to.</param>
        /// <param name="parameter">Parameter of value to convert.</param>
        /// <param name="culture">Culture of value to convert.</param>
        /// <returns>Converted value.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string formatted;
            try
            {
                var d = (double)value;
                if (d.Equals(-1.0) || d.Equals(double.NaN))
                {
                    formatted = "N/A";
                }
                else if (d > 1000 || d < 0.001)
                {
                    formatted = string.Format("{0:0.###EE0}", d);
                }
                else
                {
                    formatted = Math.Round(d, 3).ToString(CultureInfo.InvariantCulture);
                }
            }
            catch (InvalidCastException)
            {
                formatted = "N/A";
            }

            return formatted;
        }

        /// <summary>
        /// Convert string to core.
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

    /// <summary>
    /// Converter for converting a double to show correct 3 decimal places.
    /// </summary>
    public class DoubleToStringConverter : IValueConverter
    {
        /// <summary>
        /// Convert double to string.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        /// <param name="targetType">Type to convert to.</param>
        /// <param name="parameter">Parameter of value to convert.</param>
        /// <param name="culture">Culture of value to convert.</param>
        /// <returns>Converted value.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string formatted;
            try
            {
                var d = (double)value;
                if (d.Equals(-1.0) || d.Equals(double.NaN))
                {
                    formatted = "N/A";
                }
                else if (d > 1000 || d < 0.001)
                {
                    formatted = string.Format("{0:0.###}", d);
                }
                else
                {
                    formatted = Math.Round(d, 3).ToString(CultureInfo.InvariantCulture);
                }
            }
            catch (InvalidCastException)
            {
                formatted = "N/A";
            }

            return formatted;
        }

        /// <summary>
        /// Convert string to double.
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

    /// <summary>
    /// Converter for converting a QValue to show correct 3 decimal places.
    /// </summary>
    public class QValueToStringConverter : IValueConverter
    {
        /// <summary>
        /// Convert score to string.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        /// <param name="targetType">Type to convert to.</param>
        /// <param name="parameter">Parameter of value to convert.</param>
        /// <param name="culture">Culture of value to convert.</param>
        /// <returns>Converted value.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string formatted;
            try
            {
                var d = (double)value;
                if (d.Equals(0.0))
                {
                    formatted = "0";
                }
                else if (d.Equals(-1.0))
                {
                    formatted = "N/A";
                }
                else if (d < 0.001)
                {
                    formatted = string.Format("{0:0.###EE0}", d);
                }
                else
                {
                    formatted = Math.Round(d, 3).ToString(CultureInfo.InvariantCulture);
                }
            }
            catch (InvalidCastException)
            {
                formatted = "N/A";
            }

            return formatted;
        }

        /// <summary>
        /// Convert string to score.
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
