using System;
using System.Globalization;
using System.Windows.Data;
using Gnoj_Ham;

namespace Gnoj_HamView.Converters
{
    internal class YakuToolTipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var y = (YakuPivot)value;
            if (y.ConcealedFanCount == 13 && y.FanCount == 0)
                return "Yakuman. Must be concealed.";
            else if (y.FanCount == 0)
                return "Must be concealed.";
            else if (y.ConcealedBonusFanCount > 0)
                return "Bonus if concealed.";

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
