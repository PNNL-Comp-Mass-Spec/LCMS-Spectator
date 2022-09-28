using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using InformedProteomics.Backend.Data.Sequence;

namespace LcmsSpectator.Utils
{
    /// <summary>
    /// Converter for converting an InformedProteomics sequence to a string.
    /// </summary>
    public class SequenceToStringConverter : IValueConverter
    {
        /// <summary>
        /// The maximum number of amino acids per line.
        /// </summary>
        public const int LineLength = 60;

        /// <summary>
        /// Convert InformedProteomics sequence to string.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        /// <param name="targetType">Type to convert to.</param>
        /// <param name="parameter">Parameter of value to convert.</param>
        /// <param name="culture">Culture of value to convert.</param>
        /// <returns>Converted value.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var formatted = string.Empty;

            var stringBuilder = new StringBuilder();

            if (value is Sequence sequence)
            {
                var pos = 0;
                foreach (var aa in sequence)
                {
                    if (pos > 0 && pos % LineLength == 0)
                    {
                        stringBuilder.Append('\n');
                    }

                    pos++;

                    var aminoAcidText = !(aa is ModifiedAminoAcid modAa)
                                     ? aa.Residue.ToString()
                                     : string.Format("{0}[{1}]", modAa.Residue, modAa.Modification.Name);
                    stringBuilder.Append(aminoAcidText);
                }

                formatted = stringBuilder.ToString();
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
            throw new NotImplementedException();
        }
    }
}
