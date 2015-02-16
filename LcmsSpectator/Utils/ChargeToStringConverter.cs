using System;
using System.Globalization;
using System.Windows.Data;

namespace LcmsSpectator.Utils
{
    public class ChargeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string formatted = "";
            try
            {
                var d = (int)value;
                if (d > 0) formatted = String.Format("{0}+", d);
                else formatted = "N/A";
            }
            catch (InvalidCastException)
            {
                formatted = "";
            }
            return formatted;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int converted;
            try
            {
                var s = (string) value;
                var pos = s.IndexOf('+');
                if (pos != -1) s = s.Remove(pos, 1);
                converted = System.Convert.ToInt32(s);
            }
            catch (Exception)
            {
                converted = -1;
            }
            return converted;
        }
    }

    public class NumToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string formatted = "";
            try
            {
                var d = (int)value;
                if (d > 0) formatted = d.ToString(CultureInfo.InvariantCulture);
                else formatted = "N/A";
            }
            catch (InvalidCastException)
            {
                formatted = "";
            }
            return formatted;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int converted;
            try
            {
                var s = (string)value;
                var pos = s.IndexOf('+');
                if (pos != -1) s = s.Remove(pos, 1);
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
