using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Gnoj_Ham;

namespace Gnoj_HamView.Converters
{
    internal class FansToFontStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var y = (YakuPivot)value;
            return y.FanCount == 0
                ? FontStyles.Italic
                : FontStyles.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
