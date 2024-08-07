﻿using System.Globalization;
using System.Windows.Data;
using Gnoj_Ham_Library;

namespace Gnoj_Ham_View.Converters;

internal class FansToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var y = (YakuPivot)value;
        return y.FanCount > 0 && y.ConcealedBonusFanCount > 0
            ? $"{y.FanCount} (+{y.ConcealedBonusFanCount})"
            : y.ConcealedFanCount.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
