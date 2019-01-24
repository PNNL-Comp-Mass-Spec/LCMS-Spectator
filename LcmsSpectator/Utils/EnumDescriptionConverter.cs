using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace LcmsSpectator.Utils
{
    // Extract enum description
    public class EnumDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            var memberInfos = value.GetType().GetMember(value.ToString());

            if (memberInfos.Length > 0)
            {
                var attrs = memberInfos[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attrs.Length > 0)
                    return ((DescriptionAttribute)attrs[0]).Description;
            }

            return value;

            // or maybe just
            //throw new InvalidEnumArgumentException(string.Format("no description found for enum {0}", value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
