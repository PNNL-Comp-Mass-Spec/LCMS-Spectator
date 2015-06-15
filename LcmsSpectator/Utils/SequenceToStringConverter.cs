using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LcmsSpectator.Utils
{
    using System.Globalization;
    using System.Windows.Data;

    using InformedProteomics.Backend.Data.Sequence;

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
            string formatted = string.Empty;
            var sequence = value as Sequence;

            var stringBuilder = new StringBuilder();

            if (sequence != null)
            {
                int pos = 0;
                foreach (var aa in sequence)
                {
                    if (pos > 0 && pos % LineLength == 0)
                    {
                        stringBuilder.Append('\n');
                    }

                    pos++;

                    var modAa = aa as ModifiedAminoAcid;
                    var aminoAcidText = modAa == null
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
