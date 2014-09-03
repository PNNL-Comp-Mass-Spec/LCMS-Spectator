using System;
using System.Globalization;
using System.Windows.Data;

namespace LcmsSpectator.Utils
{
    public class ScoreToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string formatted;
            try
            {
                var d = (double) value;
                if (d.Equals(-1.0) || d.Equals(Double.NaN)) formatted = "N/A";
                else if (d > 1000 || d < 0.001) formatted = String.Format("{0:0.###EE0}", d);
                else formatted = Math.Round(d, 3).ToString(CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException)
            {
                formatted = "N/A";
            }
            return formatted;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class QValueToStringConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string formatted;
            try
            {
                var d = (double)value;
                if (d.Equals(0.0)) formatted = "0";
                else if (d.Equals(1.0)) formatted = "N/A";
                else if (d < 0.001) formatted = String.Format("{0:0.###EE0}", d);
                else formatted = Math.Round(d, 3).ToString(CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException)
            {
                formatted = "N/A";
            }
            return formatted;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
