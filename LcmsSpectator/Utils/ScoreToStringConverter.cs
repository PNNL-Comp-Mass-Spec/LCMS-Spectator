using System;
using System.Globalization;
using System.Windows.Data;

namespace LcmsSpectator.Utils
{
    public class ScoreToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string formatted = "";
            try
            {
                var d = (double) value;
                if (d > 1000 || d < 0.001) formatted = String.Format("{0:0.###E0}", d);
                else formatted = Math.Round(d, 3).ToString(CultureInfo.InvariantCulture);
                return formatted;
            }
            catch (InvalidCastException)
            {
                return formatted;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
