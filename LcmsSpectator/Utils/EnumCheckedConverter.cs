﻿using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace LcmsSpectator.Utils
{
    // Check if currently selected
    public class EnumCheckedConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return !values.Contains(null) && values[0].ToString().Equals(values[1].ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
