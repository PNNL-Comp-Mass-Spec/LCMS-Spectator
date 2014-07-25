using System;
using System.Globalization;
using System.Windows.Data;
using InformedProteomics.Backend.Data.Spectrometry;

namespace LcmsSpectator.Utils
{
    public class NeutralLossToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var neutralLoss = (NeutralLoss) value;
                var name = neutralLoss.Name;
                return (neutralLoss == NeutralLoss.NoLoss) ? "No Loss" : name.Remove(0, 1);
            }
            catch (InvalidCastException)
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
