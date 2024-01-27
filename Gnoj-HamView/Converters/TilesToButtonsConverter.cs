using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Gnoj_Ham;

namespace Gnoj_HamView.Converters
{
    internal class TilesToButtonsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            double rate = 1;
            if (parameter != null)
                double.TryParse(parameter.ToString(), out rate);

            return ((IEnumerable<TilePivot>)value).Select(x => GraphicTools.GenerateTileButton(x, rate: rate));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
