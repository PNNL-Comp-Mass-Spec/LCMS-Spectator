// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChargeToStringConverter.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Converter for converting a charge to a string.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Windows.Data;

namespace LcmsSpectator.Utils
{
    /// <summary>
    /// Converter for converting a charge to a string.
    /// </summary>
    public class ChargeToStringConverter : IValueConverter
    {
        /// <summary>
        /// Convert charge to string.
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
                var d = (int)value;
                if (d > 0)
                {
                    formatted = string.Format("{0}+", d);
                }
                else
                {
                    formatted = "N/A";
                }
            }
            catch (InvalidCastException)
            {
                formatted = string.Empty;
            }

            return formatted;
        }

        /// <summary>
        /// Convert string to charge.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        /// <param name="targetType">Type to convert to.</param>
        /// <param name="parameter">Parameter of value to convert.</param>
        /// <param name="culture">Culture of value to convert.</param>
        /// <returns>Converted value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int converted;
            try
            {
                var s = (string)value;
                var pos = s.IndexOf('+');
                if (pos != -1)
                {
                    s = s.Remove(pos, 1);
                }

                converted = System.Convert.ToInt32(s);
            }
            catch (Exception)
            {
                converted = -1;
            }

            return converted;
        }
    }

    /// <summary>
    /// Converter for converting an integer to a string.
    /// </summary>
    public class NumToStringConverter : IValueConverter
    {
        /// <summary>
        /// Convert an integer to string.
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
                var d = (int)value;
                if (d > 0)
                {
                    formatted = d.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    formatted = "N/A";
                }
            }
            catch (InvalidCastException)
            {
                formatted = string.Empty;
            }

            return formatted;
        }

        /// <summary>
        /// Convert a string to an integer
        /// </summary>
        /// <param name="value">Value to convert.</param>
        /// <param name="targetType">Type to convert to.</param>
        /// <param name="parameter">Parameter of value to convert.</param>
        /// <param name="culture">Culture of value to convert.</param>
        /// <returns>Converted value.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int converted;
            try
            {
                var s = (string)value;
                var pos = s.IndexOf('+');
                if (pos != -1)
                {
                    s = s.Remove(pos, 1);
                }

                converted = System.Convert.ToInt32(s);
            }
            catch (Exception)
            {
                converted = -1;
            }

            return converted;
        }
    }
}
