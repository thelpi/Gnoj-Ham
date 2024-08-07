﻿using System.Globalization;
using System.Windows.Data;
using Gnoj_Ham_Library;

namespace Gnoj_Ham_View.Converters;

internal class YakuToolTipConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var y = (YakuPivot)value;
        if (y.ConcealedFanCount == 13 && y.FanCount == 0)
            return "Yakuman. Main fermée uniquement.";
        else if (y.FanCount == 0)
            return "Main fermée uniquement.";
        else if (y.ConcealedBonusFanCount > 0)
            return "Bonus si main fermée.";

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
